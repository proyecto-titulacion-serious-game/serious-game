using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// ETAPA 1 del refactor: separa la estación de trabajo del Explorador (bandeja, validación,
/// clipboard, panel + su lógica de recepción) de la jerarquía del jugador, agrupándola bajo
/// un nuevo root <c>Explorer_Workstation</c> fijo en el mundo.
///
///   Tools → TITA → Explorador → Separar estación → Explorer_Workstation
///
/// Se hace en tiempo de EDICIÓN, dentro de la escena: Unity conserva TODAS las referencias por
/// fileID (sistema de entrega → receptor, GameManager, HUD gating…), así que no rompe nada.
/// Conserva la pose mundial de cada pieza. Reversible con Undo.
/// </summary>
public static class EstacionDesanclarTool
{
    const string WORKSTATION = "Explorer_Workstation";

    static readonly string[] Nombres =
    {
        "Bandeja_Recepcion",
        "ValidationStation_VR",
        "Clipboard_VR",
        "Explorer_StatusPanel",
    };

    [MenuItem("Tools/TITA/Explorador/Separar estación → Explorer_Workstation")]
    static void SepararEstacion()
    {
        // Raíz del jugador (Explorer_Player).
        var ea = Object.FindAnyObjectByType<ExplorerAvatar>(FindObjectsInactive.Include);
        Transform root = ea != null ? ea.transform : null;
        if (root == null)
        {
            var pc = Object.FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
            root = pc != null ? pc.transform : null;
        }
        if (root == null)
        {
            EditorUtility.DisplayDialog("TITA — Estación",
                "No se encontró Explorer_Player (ExplorerAvatar/PlayerController). Abre Explorador.unity.", "OK");
            return;
        }

        // Recolectar las piezas de estación que cuelgan del jugador (por nombre, a cualquier nivel).
        var piezas = new List<Transform>();
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t == root) continue;
            if (Coincide(t.name) && t.IsChildOf(root))
                piezas.Add(t);
        }

        // Crear/obtener el root Explorer_Workstation (en la raíz de la escena).
        var wsGo = GameObject.Find(WORKSTATION);
        if (wsGo == null)
        {
            wsGo = new GameObject(WORKSTATION);
            Undo.RegisterCreatedObjectUndo(wsGo, "Crear Explorer_Workstation");
            // Posicionarlo donde está la bandeja (si existe) para que quede ordenado.
            var bandeja = piezas.Find(p => p.name.IndexOf("Bandeja", System.StringComparison.OrdinalIgnoreCase) >= 0);
            if (bandeja != null) wsGo.transform.position = bandeja.position;
        }

        int movidas = 0;
        var nombresMovidos = new List<string>();
        foreach (var t in piezas)
        {
            if (t == wsGo.transform || t.IsChildOf(wsGo.transform)) continue; // ya está dentro
            Undo.SetTransformParent(t, wsGo.transform, "Separar estación al mundo");
            movidas++;
            nombresMovidos.Add(t.name);
        }

        EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
        Selection.activeGameObject = wsGo;

        EditorUtility.DisplayDialog("TITA — Estación",
            movidas == 0
                ? $"No había piezas que mover (¿ya están bajo {WORKSTATION}?)."
                : $"{movidas} pieza(s) movida(s) a '{WORKSTATION}' (fijo en el mundo):\n  • " +
                  string.Join("\n  • ", nombresMovidos) +
                  $"\n\nYa NO siguen al jugador y conservan su posición.\n" +
                  "Las referencias (recepción de componentes, HUD, GameManager) se conservan.\n" +
                  "Recuerda GUARDAR la escena (Ctrl+S).", "OK");

        Debug.Log($"[EstacionDesanclarTool] {movidas} pieza(s) movidas a {WORKSTATION}: {string.Join(", ", nombresMovidos)}.");
    }

    static bool Coincide(string nombre)
    {
        foreach (var n in Nombres)
            if (nombre.IndexOf(n, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        return false;
    }
}
