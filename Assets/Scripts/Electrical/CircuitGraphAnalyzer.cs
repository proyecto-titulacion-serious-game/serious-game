using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Motor MNA (Modified Nodal Analysis) para el simulador sandbox del Reto 4.
///
/// Resuelve el sistema lineal G·v = i, donde G es la matriz de conductancias
/// construida desde los <see cref="ElectricalComponent"/> de la protoboard.
/// Escribe los voltajes resueltos de vuelta en <see cref="ElectricalNode.voltage"/>
/// y las corrientes en <see cref="ElectricalComponent.current"/> / <see cref="ElectricalComponent.voltageDrop"/>.
///
/// Ventaja sobre el análisis lineal anterior: soporta topologías serie,
/// paralelo y mixtas sin asumir ningún orden de componentes.
///
/// Uso:
///   bool ok = CircuitGraphAnalyzer.SolveMNA(comps, pinNode, 5f, gndNode);
///   if (!ok) { /* cortocircuito o sin ruta */ }
///   // Ahora cada ElectricalNode.voltage tiene el valor correcto
/// </summary>
public static class CircuitGraphAnalyzer
{
    // Resistencia mínima para evitar división por cero (0.1 mΩ)
    private const double MIN_R = 1e-4;

    // Conductancia de regularización: 1 GΩ a GND para todos los nodos flotantes.
    // Evita que la matriz G sea singular cuando hay nodos sin componentes.
    private const double G_LEAK = 1e-9;

    /// <summary>
    /// Resuelve los voltajes nodales del circuito por Modified Nodal Analysis.
    ///
    /// Algoritmo:
    ///   1. Recopilar todos los ElectricalNode únicos de los componentes.
    ///   2. Construir la matriz de conductancias G (n×n).
    ///   3. Añadir conductancia de regularización (1 GΩ a GND) para nodos flotantes.
    ///   4. Aplicar condiciones de frontera Dirichlet: GND = 0 V, srcNode = srcVoltage.
    ///   5. Resolver G·v = b mediante eliminación gaussiana con pivoteo parcial.
    ///   6. Escribir voltajes en ElectricalNode.voltage y corrientes en cada componente.
    /// </summary>
    /// <param name="components">
    ///   Componentes pasivos (sin VoltageSource) con nodeA y nodeB válidos.
    /// </param>
    /// <param name="sourceNode">
    ///   Nodo de voltaje fijo (pin Arduino). Puede ser null → solo se fija GND.
    /// </param>
    /// <param name="sourceVoltage">Voltaje de la fuente en voltios (ej. 5 V).</param>
    /// <param name="gndNode">Nodo de tierra (siempre 0 V). Requerido.</param>
    /// <returns>
    ///   True si la solución convergió; false si la matriz es singular
    ///   (indica cortocircuito ideal o circuito degenerado).
    /// </returns>
    public static bool SolveMNA(
        List<ElectricalComponent> components,
        ElectricalNode sourceNode,
        float sourceVoltage,
        ElectricalNode gndNode)
    {
        if (gndNode == null) return false;
        if (components == null || components.Count == 0)
        {
            // Sin componentes: el nodo fuente flota, el GND es 0 V
            if (sourceNode != null) sourceNode.voltage = sourceVoltage;
            gndNode.voltage = 0f;
            return true;
        }

        // ── 1. Recopilar nodos únicos ────────────────────────────────────────
        var nodeSet = new HashSet<ElectricalNode>();
        nodeSet.Add(gndNode);
        if (sourceNode != null) nodeSet.Add(sourceNode);

        foreach (var comp in components)
        {
            if (comp == null) continue;
            if (comp.nodeA != null) nodeSet.Add(comp.nodeA);
            if (comp.nodeB != null) nodeSet.Add(comp.nodeB);
        }

        // Asignar índices: GND = 0, source = 1 (si existe), resto = 2..n-1
        var nodeList  = new List<ElectricalNode>(nodeSet);
        nodeList.Remove(gndNode);
        nodeList.Insert(0, gndNode);

        if (sourceNode != null && nodeList.Contains(sourceNode))
        {
            nodeList.Remove(sourceNode);
            nodeList.Insert(1, sourceNode);
        }

        int n = nodeList.Count;
        var idx = new Dictionary<ElectricalNode, int>(n);
        for (int i = 0; i < n; i++) idx[nodeList[i]] = i;

        // ── 2. Construir matriz G ────────────────────────────────────────────
        var G = new double[n, n];
        var b = new double[n];

        foreach (var comp in components)
        {
            if (comp == null || comp.nodeA == null || comp.nodeB == null) continue;

            float  r = Mathf.Max(comp.GetResistance(), (float)MIN_R);
            double g = 1.0 / r;

            int ia = idx[comp.nodeA];
            int ib = idx[comp.nodeB];

            G[ia, ia] += g;
            G[ib, ib] += g;
            G[ia, ib] -= g;
            G[ib, ia] -= g;
        }

        // ── 3. Regularización: conductancia de 1 GΩ de cada nodo no-GND a GND ─
        // Evita nodos flotantes que harían G singular.
        // GND row será sobreescrita por Dirichlet, pero los off-diagonals no.
        for (int i = 1; i < n; i++)
        {
            G[i, i] += G_LEAK;
            G[i, 0] -= G_LEAK;
            G[0, 0] += G_LEAK; // será sobreescrito por Dirichlet GND
            G[0, i] -= G_LEAK; // será sobreescrito por Dirichlet GND
        }

        // ── 4. Condiciones de frontera Dirichlet ─────────────────────────────
        ApplyDirichlet(G, b, n, 0, 0.0);                          // GND = 0 V

        if (sourceNode != null && idx.TryGetValue(sourceNode, out int srcIdx))
            ApplyDirichlet(G, b, n, srcIdx, sourceVoltage);       // Pin = srcV

        // ── 5. Resolver G·v = b ──────────────────────────────────────────────
        double[] v = GaussElim(G, b, n);
        if (v == null) return false; // matriz singular

        // ── 6. Escribir voltajes en los ElectricalNode ───────────────────────
        for (int i = 0; i < n; i++)
            nodeList[i].voltage = (float)v[i];

        // ── 7. Calcular corrientes por componente (I = ΔV / R) ───────────────
        foreach (var comp in components)
        {
            if (comp == null || comp.nodeA == null || comp.nodeB == null) continue;
            float r          = Mathf.Max(comp.GetResistance(), (float)MIN_R);
            comp.voltageDrop = comp.nodeA.voltage - comp.nodeB.voltage;
            comp.current     = comp.voltageDrop / r;
        }

        // Actualizar ElectricalNode.current = suma de corrientes salientes
        foreach (var node in nodeList) node.current = 0f;
        foreach (var comp in components)
        {
            if (comp.nodeA != null) comp.nodeA.current += comp.current;
            if (comp.nodeB != null) comp.nodeB.current -= comp.current;
        }

        return true;
    }

    // ── Helpers privados ──────────────────────────────────────────────────────

    static void ApplyDirichlet(double[,] G, double[] b, int n, int row, double value)
    {
        for (int j = 0; j < n; j++) G[row, j] = 0.0;
        G[row, row] = 1.0;
        b[row]      = value;
    }

    /// <summary>
    /// Eliminación gaussiana con pivoteo parcial.
    /// Devuelve null si la matriz es singular (det ≈ 0).
    /// </summary>
    static double[] GaussElim(double[,] A, double[] rhs, int n)
    {
        var M = (double[,])A.Clone();
        var r = (double[])rhs.Clone();

        for (int col = 0; col < n; col++)
        {
            // Pivoteo parcial: buscar fila con mayor valor absoluto en esta columna
            int pivot = col;
            double maxVal = Math.Abs(M[col, col]);
            for (int row = col + 1; row < n; row++)
            {
                double val = Math.Abs(M[row, col]);
                if (val > maxVal) { maxVal = val; pivot = row; }
            }

            // Intercambiar filas col y pivot
            if (pivot != col)
            {
                for (int k = 0; k < n; k++) (M[col, k], M[pivot, k]) = (M[pivot, k], M[col, k]);
                (r[col], r[pivot]) = (r[pivot], r[col]);
            }

            if (Math.Abs(M[col, col]) < 1e-14) return null; // singular

            // Eliminar columna en filas inferiores
            for (int row = col + 1; row < n; row++)
            {
                double factor = M[row, col] / M[col, col];
                r[row] -= factor * r[col];
                for (int k = col; k < n; k++) M[row, k] -= factor * M[col, k];
            }
        }

        // Sustitución regresiva
        var x = new double[n];
        for (int i = n - 1; i >= 0; i--)
        {
            x[i] = r[i];
            for (int j = i + 1; j < n; j++) x[i] -= M[i, j] * x[j];
            if (Math.Abs(M[i, i]) < 1e-14) return null;
            x[i] /= M[i, i];
        }
        return x;
    }
}
