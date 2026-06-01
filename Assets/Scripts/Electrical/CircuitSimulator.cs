using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// ─────────────────────────────────────────────
//  Resultado de la validación sandbox (Reto 4)
// ─────────────────────────────────────────────

/// <summary>
/// Resultado del validador dinámico del Reto 4 (sandbox LED blink).
/// Emitido via <see cref="ProtoboardSimulator.OnSandboxValidated"/>.
/// </summary>
public struct SandboxValidationResult
{
    /// <summary>True si el circuito cumple TODOS los criterios del objetivo.</summary>
    public bool   success;
    /// <summary>Número de pin digital activado por ArduinoCore.</summary>
    public int    activatedPin;
    /// <summary>True si ArduinoCore tiene blinkEnabled activo.</summary>
    public bool   blinkEnabled;
    /// <summary>True si el DFS encontró un camino desde el pin hasta GND.</summary>
    public bool   pathFound;
    /// <summary>True si el camino contiene al menos un LED.</summary>
    public bool   hasLED;
    /// <summary>True si el LED está con la polaridad correcta (no invertido).</summary>
    public bool   ledForwardBiased;
    /// <summary>True si hay una resistencia >= 100 Ω en el camino.</summary>
    public bool   hasProtection;
    /// <summary>Corriente estimada en mA según resistencia total del camino.</summary>
    public float  currentMa;
    /// <summary>Mensaje legible para mostrar en el HUD o consola del IDE.</summary>
    public string message;
}

/// <summary>
/// Motor matemático de la protoboard sandbox (Reto 4).
/// Monitorea la matriz de ProtoboardSlots, construye el grafo eléctrico por railId
/// y calcula V, I y P mediante análisis nodal simplificado.
///
/// Diferencia con CircuitSimulator (Gameplay/): la topología se deduce dinámicamente
/// de qué componentes/cables están colocados en qué slots, sin listas hardcodeadas.
///
/// SETUP: añadir este script al GameObject padre de la protoboard y rellenar
/// todosLosSlots usando Tools > TITA > Generador de Slots.
/// </summary>
public class ProtoboardSimulator : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Protoboard")]
    [Tooltip("Todos los ProtoboardSlots de la cuadrícula. Rellenar con el generador de Editor.")]
    public List<ProtoboardSlot> todosLosSlots = new List<ProtoboardSlot>();

    [Header("Telemetría (solo lectura)")]
    [SerializeField] private float _sourceVoltage;
    [SerializeField] private float _totalCurrentmA;   // miliamperios
    [SerializeField] private float _totalPowerW;      // Watts
    [SerializeField] private bool  _isShortCircuited;
    [SerializeField] private bool  _isOpenCircuit;

    [Header("Simulación")]
    [SerializeField] private float _interval = 0.05f; // 20 Hz

    // ─────────────────────────────────────────────
    //  Propiedades públicas (lectura de telemetría)
    // ─────────────────────────────────────────────
    public float sourceVoltage    => _sourceVoltage;
    /// <summary>Corriente total en miliamperios (mA).</summary>
    public float totalCurrentmA   => _totalCurrentmA;
    /// <summary>Potencia total disipada en Watts (W).</summary>
    public float totalPowerW      => _totalPowerW;
    public bool  isShortCircuited => _isShortCircuited;
    public bool  isOpenCircuit    => _isOpenCircuit;

    // ─────────────────────────────────────────────
    //  Eventos
    // ─────────────────────────────────────────────
    /// <summary>Dispara cada vez que el simulador recalcula el circuito.</summary>
    public static event Action OnCircuitChanged;

    /// <summary>
    /// Dispara cuando el resultado de la validación sandbox cambia.
    /// Suscribirse en <see cref="InstructionSystem"/> y <see cref="GameManager"/>
    /// para la condición de victoria desacoplada del Reto 4.
    /// </summary>
    public static event Action<SandboxValidationResult> OnSandboxValidated;

    // ─────────────────────────────────────────────
    //  Estado interno
    // ─────────────────────────────────────────────
    private bool _dirty = true;
    private ArduinoCore _arduino;
    private SandboxValidationResult _lastSandboxResult;

    // ─────────────────────────────────────────────
    //  Unity
    // ─────────────────────────────────────────────
    void OnEnable()
    {
        StartCoroutine(SimLoop());
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    // ─────────────────────────────────────────────
    //  API pública
    // ─────────────────────────────────────────────
    /// <summary>Solicita una nueva simulación en el próximo tick.</summary>
    public void MarkDirty() => _dirty = true;

    // ─────────────────────────────────────────────
    //  Bucle de simulación
    // ─────────────────────────────────────────────
    IEnumerator SimLoop()
    {
        var wait = new WaitForSeconds(_interval);
        while (true)
        {
            if (_dirty)
            {
                _dirty = false;
                RunSimulation();
                OnCircuitChanged?.Invoke();
                ValidateSandboxObjective();
            }
            yield return wait;
        }
    }

    // ─────────────────────────────────────────────
    //  Núcleo: construcción del mapa de nodos
    // ─────────────────────────────────────────────

    /// <summary>
    /// Agrupa los slots por railId. El primer slot de cada grupo actúa como nodo
    /// representativo (se le añade o recicla su ElectricalNode component).
    /// </summary>
    void BuildNodeMap()
    {
        var representatives = new Dictionary<string, ElectricalNode>();

        foreach (var slot in todosLosSlots)
        {
            if (string.IsNullOrEmpty(slot.railId)) continue;

            if (!representatives.TryGetValue(slot.railId, out ElectricalNode node))
            {
                node = slot.GetComponent<ElectricalNode>();
                if (node == null) node = slot.gameObject.AddComponent<ElectricalNode>();
                node.voltage = 0f;
                node.current = 0f;
                representatives[slot.railId] = node;
            }

            slot.assignedNode = node;
        }
    }

    // ─────────────────────────────────────────────
    //  Núcleo: simulación eléctrica
    // ─────────────────────────────────────────────
    void RunSimulation()
    {
        BuildNodeMap();

        var allComps = GetComponentsInChildren<ElectricalComponent>(true)
            .Where(c => c.nodeA != null && c.nodeB != null)
            .ToList();

        var source = allComps.OfType<VoltageSource>().FirstOrDefault();
        float srcV;
        ElectricalNode srcNodeA = null, srcNodeB = null;

        if (source != null)
        {
            srcV     = source.voltage;
            srcNodeA = source.nodeA;
            srcNodeB = source.nodeB;
        }
        else
        {
            // Sandbox Arduino: ArduinoCore actúa como fuente (cualquier pin digital → GND)
            if (_arduino == null) _arduino = GetComponentInChildren<ArduinoCore>(true)
                                          ?? FindAnyObjectByType<ArduinoCore>();
            if (_arduino == null || _arduino.activePinMode != PinMode.OUTPUT)
            {
                ClearTelemetry(openCircuit: true);
                return;
            }
            srcNodeA = _arduino.PinToNode(_arduino.activePinNumber);
            if (srcNodeA == null) { ClearTelemetry(openCircuit: true); return; }

            // Usar 5 V cuando blink activo (simula HIGH para telemetría educativa estable)
            srcV     = _arduino.blinkEnabled ? 5f : _arduino.OutputVoltage;
            srcNodeB = _arduino.nodoGND;
            if (srcNodeB == null) { ClearTelemetry(openCircuit: true); return; }
        }

        if (srcV <= 0.001f || allComps.Count <= 1)
        {
            ClearTelemetry(openCircuit: true);
            return;
        }

        _sourceVoltage = srcV;

        // Componentes pasivos: todos excepto VoltageSource (que es condición de frontera)
        var passiveComps = allComps.Where(c => !(c is VoltageSource)).ToList();

        // ── MNA: Modified Nodal Analysis ───────────────────────────────────────────
        // Soporta topologías serie, paralelo y mixtas sin asumir ningún orden.
        // Escribe ElectricalNode.voltage en todos los nodos y comp.current en todos los
        // componentes — el Multimeter los leerá directamente sin cambios.
        bool solved = CircuitGraphAnalyzer.SolveMNA(passiveComps, srcNodeA, srcV, srcNodeB);

        if (!solved)
        {
            // Matriz singular → cortocircuito ideal
            _isShortCircuited = true;
            _isOpenCircuit    = false;
            float faultI      = srcV / 0.1f;
            _totalCurrentmA   = faultI * 1000f;
            _totalPowerW      = srcV * faultI;
            foreach (var c in passiveComps) { c.current = faultI; c.voltageDrop = 0f; }
            return;
        }

        // Corriente total = suma de corrientes que SALEN del nodo fuente hacia sus vecinos
        float totalI = 0f;
        if (srcNodeA != null)
        {
            foreach (var c in passiveComps)
            {
                // c.current = (nodeA.voltage - nodeB.voltage) / R  →  positivo = fluye de A a B
                if      (c.nodeA == srcNodeA) totalI += Mathf.Max(0f,  c.current);
                else if (c.nodeB == srcNodeA) totalI += Mathf.Max(0f, -c.current);
            }
        }

        _isShortCircuited = totalI > 1.0f;           // > 1 A = cortocircuito práctico
        _isOpenCircuit    = totalI < 0.0001f;         // < 0.1 mA = circuito abierto
        _totalCurrentmA   = totalI * 1000f;
        _totalPowerW      = srcV * totalI;

        // Calculate() actualiza estado visual de cada componente (LED encendido/apagado, etc.)
        foreach (var comp in passiveComps) comp.Calculate();
    }

    void ClearTelemetry(bool openCircuit)
    {
        _sourceVoltage    = 0f;
        _totalCurrentmA   = 0f;
        _totalPowerW      = 0f;
        _isShortCircuited = false;
        _isOpenCircuit    = openCircuit;
    }

    // ═════════════════════════════════════════════
    //  SANDBOX VALIDATION — Reto 4 dinámico
    // ═════════════════════════════════════════════

    /// <summary>
    /// Punto de entrada del validador sandbox.
    /// Se llama automáticamente tras cada simulación. Emite <see cref="OnSandboxValidated"/>
    /// solo cuando el resultado cambia, para no saturar suscriptores.
    /// </summary>
    void ValidateSandboxObjective()
    {
        if (_arduino == null) _arduino = FindAnyObjectByType<ArduinoCore>();
        if (_arduino == null) return;

        var result = EvaluateSandbox(_arduino);

        // Solo disparar si el resultado cambió (éxito o mensaje distinto)
        if (result.success == _lastSandboxResult.success &&
            result.message == _lastSandboxResult.message) return;

        _lastSandboxResult = result;
        OnSandboxValidated?.Invoke(result);
    }

    /// <summary>
    /// Evalúa si el circuito cumple el objetivo sandbox:
    /// "Haz parpadear un LED de forma segura desde cualquier pin digital."
    ///
    /// Algoritmo DFS:
    ///   1. Verificar que el sketch tiene BLINK + OUTPUT activos en ArduinoCore.
    ///   2. Resolver el nodo del pin activado vía <see cref="ArduinoCore.PinToNode"/>.
    ///   3. DFS desde ese nodo hasta nodoGND buscando un camino que contenga:
    ///        a) Al menos un LED con polaridad correcta (ánodo → cátodo).
    ///        b) Al menos una resistencia >= 100 Ω (protección del LED).
    ///   4. Verificar que la corriente estimada está en rango seguro (5–20 mA).
    /// </summary>
    SandboxValidationResult EvaluateSandbox(ArduinoCore arduino)
    {
        var r = new SandboxValidationResult { activatedPin = arduino.activePinNumber };

        // ── Condición 1: sketch con BLINK y OUTPUT ───────────────────────
        if (arduino.activePinMode != PinMode.OUTPUT)
            return Fail(r, "El pin del Arduino no está en modo OUTPUT. " +
                            "Agrega 'pinMode(X, OUTPUT)' en setup().");

        r.blinkEnabled = arduino.blinkEnabled;
        if (!arduino.blinkEnabled)
            return Fail(r, $"Pin D{arduino.activePinNumber} en OUTPUT pero sin BLINK. " +
                            "El objetivo es hacer parpadear el LED.");

        // ── Condición 2: nodo del pin activo existe ───────────────────────
        ElectricalNode startNode = arduino.PinToNode(arduino.activePinNumber);
        ElectricalNode gndNode   = arduino.nodoGND;

        if (startNode == null)
            return Fail(r, $"Pin D{arduino.activePinNumber} no tiene nodo eléctrico asignado " +
                            "en el modelo 3D del Arduino. Usa el Inspector de ArduinoCore " +
                            "para añadir el mapeo en 'Pin Node Map'.");

        if (gndNode == null)
            return Fail(r, "Nodo GND del Arduino no asignado en el Inspector de ArduinoCore.");

        // ── Condición 3: recorrido DFS por el grafo de la protoboard ─────
        var allComps = GetComponentsInChildren<ElectricalComponent>(true)
            .Where(c => c.nodeA != null && c.nodeB != null && !(c is VoltageSource))
            .ToList();

        if (allComps.Count == 0)
            return Fail(r, "No hay componentes colocados en la protoboard.");

        var adj = BuildAdjacency(allComps);
        if (!adj.ContainsKey(startNode))
            return Fail(r, $"Ningún componente conectado al pin D{arduino.activePinNumber} " +
                            "en la protoboard. Conecta un cable desde ese pin.");

        var pathFound = new List<ElectricalComponent>();
        bool reached = DFS(startNode, gndNode, adj,
                           new HashSet<ElectricalNode>(),
                           new List<ElectricalComponent>(),
                           pathFound);

        if (!reached)
            return Fail(r, $"El circuito desde D{arduino.activePinNumber} no llega a GND. " +
                            "Asegúrate de cerrar el circuito: Pin → LED → Resistencia → GND.");

        r.pathFound = true;

        // ── Condición 4: LED con polaridad correcta ───────────────────────
        var leds = pathFound.OfType<LED>().ToList();
        if (leds.Count == 0)
            return Fail(r, "No se detectó ningún LED en el camino Pin → GND. " +
                            "Coloca un LED entre el pin y GND.");

        var forwardLED = leds.FirstOrDefault(l => !l.polarityInverted);
        if (forwardLED == null)
            return Fail(r, "El LED está con la polaridad invertida. " +
                            "Gíralo 180° — el ánodo (patita larga) debe apuntar al pin.");

        r.hasLED           = true;
        r.ledForwardBiased = true;

        // ── Condición 5: resistencia de protección ────────────────────────
        var protectionR = pathFound.OfType<Resistor>()
                                   .FirstOrDefault(res => res.GetResistance() >= 100f);
        if (protectionR == null)
            return Fail(r, "Falta una resistencia de al menos 100 Ω entre el pin y GND. " +
                            "Sin ella el LED puede quemarse por exceso de corriente.");

        r.hasProtection = true;

        // ── Condición 6: corriente estimada en rango seguro ───────────────
        float totalR = pathFound.Sum(c => c.GetResistance());
        if (totalR < 0.1f) totalR = 0.1f;
        float currentA = arduino.outputVoltageTTL / totalR;
        r.currentMa    = currentA * 1000f;

        if (r.currentMa > forwardLED.maxSafeCurrent * 1000f)
            return Fail(r, $"Corriente estimada {r.currentMa:F1} mA supera el límite seguro " +
                            $"del LED ({forwardLED.maxSafeCurrent * 1000f:F0} mA). " +
                            "Aumenta la resistencia (330 Ω recomendado).");

        // ── ¡Todo OK! ─────────────────────────────────────────────────────
        r.success = true;
        r.message = $"¡Circuito validado! Pin D{arduino.activePinNumber} · " +
                    $"BLINK {arduino.blinkIntervalMs}ms · " +
                    $"I ≈ {r.currentMa:F1} mA · LED encendido de forma segura.";
        return r;
    }

    // ── Utilidades de grafo ──────────────────────────────────────────────

    /// <summary>
    /// Construye lista de adyacencia bidireccional: nodo → [(vecino, componente)].
    /// Cada componente crea dos aristas (nodeA↔nodeB) para que el DFS pueda
    /// recorrer el grafo en cualquier dirección.
    /// </summary>
    static Dictionary<ElectricalNode, List<(ElectricalNode, ElectricalComponent)>>
        BuildAdjacency(List<ElectricalComponent> comps)
    {
        var adj = new Dictionary<ElectricalNode, List<(ElectricalNode, ElectricalComponent)>>();

        foreach (var c in comps)
        {
            if (!adj.ContainsKey(c.nodeA)) adj[c.nodeA] = new List<(ElectricalNode, ElectricalComponent)>();
            if (!adj.ContainsKey(c.nodeB)) adj[c.nodeB] = new List<(ElectricalNode, ElectricalComponent)>();
            adj[c.nodeA].Add((c.nodeB, c));
            adj[c.nodeB].Add((c.nodeA, c));
        }
        return adj;
    }

    /// <summary>
    /// DFS con backtracking. Busca la primera ruta desde <paramref name="current"/>
    /// hasta <paramref name="target"/> que contenga LED (no invertido) + Resistencia >= 100 Ω.
    /// Si la encuentra, la escribe en <paramref name="bestPath"/> y devuelve true.
    /// Si existe una ruta hasta GND pero sin los componentes requeridos, devuelve false
    /// con bestPath vacío para que EvaluateSandbox dé retroalimentación específica.
    /// </summary>
    static bool DFS(
        ElectricalNode current,
        ElectricalNode target,
        Dictionary<ElectricalNode, List<(ElectricalNode n, ElectricalComponent c)>> adj,
        HashSet<ElectricalNode> visited,
        List<ElectricalComponent> pathSoFar,
        List<ElectricalComponent> bestPath)
    {
        if (current == target)
        {
            bool hasForwardLED = pathSoFar.OfType<LED>().Any(l => !l.polarityInverted);
            bool hasProtection = pathSoFar.OfType<Resistor>().Any(res => res.GetResistance() >= 100f);
            if (hasForwardLED && hasProtection)
            {
                bestPath.AddRange(pathSoFar);
                return true;
            }
            return false;
        }

        visited.Add(current);

        if (adj.TryGetValue(current, out var neighbors))
        {
            foreach (var (next, comp) in neighbors)
            {
                if (visited.Contains(next)) continue;
                pathSoFar.Add(comp);
                if (DFS(next, target, adj, visited, pathSoFar, bestPath))
                    return true;
                pathSoFar.RemoveAt(pathSoFar.Count - 1);
            }
        }

        visited.Remove(current); // backtrack para explorar caminos alternativos
        return false;
    }

    static SandboxValidationResult Fail(SandboxValidationResult r, string msg)
    {
        r.success = false;
        r.message = msg;
        return r;
    }
}
