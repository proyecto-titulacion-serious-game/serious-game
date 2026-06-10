#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Rellena automáticamente las referencias vacías en la escena del Explorador (Explorador.unity).
///
/// Referencias que cubre:
///   ConnectionManager        → gameManager
///   ObjectiveSystem          → gameManager, performance
///   VRValidationButton       → haptics
///   ExplorerCircuitPanel     → gameManager, circuitManager
///   PlayerInteraction        → gameManager, circuit, multimeter, playerController, haptics
///   ToolboxController        → haptics
///   ExplorerComponentReceiver→ delivery (ComponentDeliverySystem)
///   ArduinoCore              → nodoP13, nodoGND, nodoA0 (por nombre en jerarquía)
///   ArduinoNetworkBridge     → arduinoCore
///
/// NOTA: Si GameManager está en Tecnico.unity (cargada aditiva), también lo encuentra.
///
/// Menú: Tools → TITA → Red → Rellenar referencias escena Explorador
/// </summary>
public static class ExploradorSceneSetupTool
{
    [MenuItem("Tools/TITA/Red/Rellenar referencias escena Explorador")]
    static void Run()
    {
        var log    = new System.Text.StringBuilder("=== ExploradorSceneSetupTool ===\n\n");
        int fixed_ = 0;

        // ─────────────────────────────────────────────
        //  Buscar componentes clave en todas las escenas cargadas
        // ─────────────────────────────────────────────
        var gm          = Find<GameManager>();
        var perfTracker = Find<PerformanceTracker>();
        var haptics     = Find<HapticFeedback>();
        var multimeter  = Find<Multimeter>();
        var playerCtrl  = Find<PlayerController>();
        var delivery    = Find<ComponentDeliverySystem>();
        var arduino     = Find<ArduinoCore>();
        var bridge      = Find<ArduinoNetworkBridge>();
        var connMgr     = Find<ConnectionManager>();

        // CircuitManagers (puede haber varios — uno por reto)
        var allCircuits = Object.FindObjectsByType<CircuitManager>(FindObjectsInactive.Include);

        // ExplorerCircuitPanels (puede haber varios)
        var allPanels = Object.FindObjectsByType<ExplorerCircuitPanel>(FindObjectsInactive.Include);

        // Nodos eléctricos del Arduino (por nombre de GO)
        var nodoP13 = FindNodeByName("Nodo_P13", "Nodo_p13", "nodo_P13", "NodoP13");
        var nodoGND = FindNodeByName("Nodo_GND", "Nodo_gnd", "nodo_GND", "NodoGND");
        var nodoA0  = FindNodeByName("Nodo_A0",  "Nodo_a0",  "nodo_A0",  "NodoA0");

        // ─────────────────────────────────────────────
        //  1. ConnectionManager → gameManager
        // ─────────────────────────────────────────────
        if (connMgr != null && gm != null)
        {
            var so = new SerializedObject(connMgr);
            var p  = so.FindProperty("gameManager");
            if (p != null && p.objectReferenceValue == null)
            {
                p.objectReferenceValue = gm;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(connMgr);
                log.AppendLine($"[OK] ConnectionManager.gameManager → {gm.gameObject.name}");
                fixed_++;
            }
        }
        else if (connMgr == null) log.AppendLine("[??] ConnectionManager no encontrado.");
        else if (gm == null) log.AppendLine("[??] GameManager no encontrado (puede estar en Tecnico.unity).");

        // ─────────────────────────────────────────────
        //  2. ObjectiveSystem → gameManager, performance
        // ─────────────────────────────────────────────
        var objSys = Find<ObjectiveSystem>();
        if (objSys != null)
        {
            if (objSys.gameManager == null && gm != null)
            {
                Undo.RecordObject(objSys, "Asignar gameManager");
                objSys.gameManager = gm;
                EditorUtility.SetDirty(objSys);
                log.AppendLine($"[OK] ObjectiveSystem.gameManager → {gm.gameObject.name}");
                fixed_++;
            }
            if (objSys.performance == null && perfTracker != null)
            {
                Undo.RecordObject(objSys, "Asignar performance");
                objSys.performance = perfTracker;
                EditorUtility.SetDirty(objSys);
                log.AppendLine($"[OK] ObjectiveSystem.performance → {perfTracker.gameObject.name}");
                fixed_++;
            }
        }

        // ─────────────────────────────────────────────
        //  3. VRValidationButton → haptics (+ gameManager vía SerializedObject)
        // ─────────────────────────────────────────────
        var vrBtn = Find<VRValidationButton>();
        if (vrBtn != null)
        {
            if (vrBtn.haptics == null && haptics != null)
            {
                Undo.RecordObject(vrBtn, "Asignar haptics");
                vrBtn.haptics = haptics;
                EditorUtility.SetDirty(vrBtn);
                log.AppendLine($"[OK] VRValidationButton.haptics → {haptics.gameObject.name}");
                fixed_++;
            }

            // gameManager no es campo público visible — intentar vía SerializedObject
            var soBtn = new SerializedObject(vrBtn);
            var pGM   = soBtn.FindProperty("gameManager") ?? soBtn.FindProperty("_gm");
            if (pGM != null && pGM.objectReferenceValue == null && gm != null)
            {
                pGM.objectReferenceValue = gm;
                soBtn.ApplyModifiedProperties();
                EditorUtility.SetDirty(vrBtn);
                log.AppendLine($"[OK] VRValidationButton.gameManager → {gm.gameObject.name}");
                fixed_++;
            }
        }
        else log.AppendLine("[??] VRValidationButton no encontrado.");

        // ─────────────────────────────────────────────
        //  4. ExplorerCircuitPanel → gameManager + circuitManager
        // ─────────────────────────────────────────────
        foreach (var panel in allPanels)
        {
            if (panel == null) continue;

            if (panel.gameManager == null && gm != null)
            {
                Undo.RecordObject(panel, "Asignar gameManager");
                panel.gameManager = gm;
                EditorUtility.SetDirty(panel);
                log.AppendLine($"[OK] ExplorerCircuitPanel ({panel.gameObject.name}).gameManager → {gm.gameObject.name}");
                fixed_++;
            }

            if (panel.circuitManager == null && allCircuits.Length > 0)
            {
                // Asignar el CircuitManager más cercano en la jerarquía
                var closest = FindClosestInHierarchy(panel.transform, allCircuits);
                if (closest != null)
                {
                    Undo.RecordObject(panel, "Asignar circuitManager");
                    panel.circuitManager = closest;
                    EditorUtility.SetDirty(panel);
                    log.AppendLine($"[OK] ExplorerCircuitPanel ({panel.gameObject.name}).circuitManager → {closest.gameObject.name}");
                    fixed_++;
                }
            }
        }

        // ─────────────────────────────────────────────
        //  5. PlayerInteraction → gameManager, circuit, multimeter, playerController, haptics
        // ─────────────────────────────────────────────
        var playerInteraction = Find<PlayerInteraction>();
        if (playerInteraction != null)
        {
            if (playerInteraction.gameManager == null && gm != null)
            { Undo.RecordObject(playerInteraction, "Asignar gameManager"); playerInteraction.gameManager = gm; EditorUtility.SetDirty(playerInteraction); log.AppendLine($"[OK] PlayerInteraction.gameManager → {gm.gameObject.name}"); fixed_++; }

            if (playerInteraction.circuit == null && allCircuits.Length > 0)
            { Undo.RecordObject(playerInteraction, "Asignar circuit"); playerInteraction.circuit = allCircuits[0]; EditorUtility.SetDirty(playerInteraction); log.AppendLine($"[OK] PlayerInteraction.circuit → {allCircuits[0].gameObject.name}"); fixed_++; }

            if (playerInteraction.multimeter == null && multimeter != null)
            { Undo.RecordObject(playerInteraction, "Asignar multimeter"); playerInteraction.multimeter = multimeter; EditorUtility.SetDirty(playerInteraction); log.AppendLine($"[OK] PlayerInteraction.multimeter → {multimeter.gameObject.name}"); fixed_++; }

            if (playerInteraction.playerController == null && playerCtrl != null)
            { Undo.RecordObject(playerInteraction, "Asignar playerController"); playerInteraction.playerController = playerCtrl; EditorUtility.SetDirty(playerInteraction); log.AppendLine($"[OK] PlayerInteraction.playerController → {playerCtrl.gameObject.name}"); fixed_++; }

            if (playerInteraction.haptics == null && haptics != null)
            { Undo.RecordObject(playerInteraction, "Asignar haptics"); playerInteraction.haptics = haptics; EditorUtility.SetDirty(playerInteraction); log.AppendLine($"[OK] PlayerInteraction.haptics → {haptics.gameObject.name}"); fixed_++; }
        }
        else log.AppendLine("[??] PlayerInteraction no encontrado.");

        // ─────────────────────────────────────────────
        //  6. ToolboxController → haptics
        // ─────────────────────────────────────────────
        var toolbox = Find<ToolboxController>();
        if (toolbox != null && toolbox.haptics == null && haptics != null)
        {
            Undo.RecordObject(toolbox, "Asignar haptics");
            toolbox.haptics = haptics;
            EditorUtility.SetDirty(toolbox);
            log.AppendLine($"[OK] ToolboxController.haptics → {haptics.gameObject.name}");
            fixed_++;
        }

        // ─────────────────────────────────────────────
        //  7. ExplorerComponentReceiver → delivery
        // ─────────────────────────────────────────────
        var receiver = Find<ExplorerComponentReceiver>();
        if (receiver != null && receiver.delivery == null && delivery != null)
        {
            Undo.RecordObject(receiver, "Asignar delivery");
            receiver.delivery = delivery;
            EditorUtility.SetDirty(receiver);
            log.AppendLine($"[OK] ExplorerComponentReceiver.delivery → {delivery.gameObject.name}");
            fixed_++;
        }

        // ─────────────────────────────────────────────
        //  8. ArduinoCore → nodoP13, nodoGND, nodoA0
        // ─────────────────────────────────────────────
        if (arduino != null)
        {
            if (arduino.nodoP13 == null && nodoP13 != null)
            { Undo.RecordObject(arduino, "Asignar nodoP13"); arduino.nodoP13 = nodoP13; EditorUtility.SetDirty(arduino); log.AppendLine($"[OK] ArduinoCore.nodoP13 → {nodoP13.gameObject.name}"); fixed_++; }
            else if (arduino.nodoP13 == null)
                log.AppendLine("[??] ArduinoCore.nodoP13: GO 'Nodo_P13' no encontrado en escena.");

            if (arduino.nodoGND == null && nodoGND != null)
            { Undo.RecordObject(arduino, "Asignar nodoGND"); arduino.nodoGND = nodoGND; EditorUtility.SetDirty(arduino); log.AppendLine($"[OK] ArduinoCore.nodoGND → {nodoGND.gameObject.name}"); fixed_++; }
            else if (arduino.nodoGND == null)
                log.AppendLine("[??] ArduinoCore.nodoGND: GO 'Nodo_GND' no encontrado en escena.");

            if (arduino.nodoA0 == null && nodoA0 != null)
            { Undo.RecordObject(arduino, "Asignar nodoA0"); arduino.nodoA0 = nodoA0; EditorUtility.SetDirty(arduino); log.AppendLine($"[OK] ArduinoCore.nodoA0 → {nodoA0.gameObject.name}"); fixed_++; }
            else if (arduino.nodoA0 == null)
                log.AppendLine("[??] ArduinoCore.nodoA0: GO 'Nodo_A0' no encontrado en escena.");
        }
        else log.AppendLine("[--] ArduinoCore no encontrado (puede que no esté en la escena activa).");

        // ─────────────────────────────────────────────
        //  9. ArduinoNetworkBridge — info (se conecta en runtime vía OnBridgeReady)
        // ─────────────────────────────────────────────
        if (bridge != null)
            log.AppendLine($"[--] ArduinoNetworkBridge encontrado: '{bridge.gameObject.name}'.\n" +
                           "     Se conecta en runtime vía OnBridgeReady (Opción A).");
        else
            log.AppendLine("[??] ArduinoNetworkBridge no encontrado — es spawneado por Fusion en runtime.");

        // ─────────────────────────────────────────────
        //  10. Marcar escenas dirty y guardar
        // ─────────────────────────────────────────────
        EditorSceneManager.MarkAllScenesDirty();

        // ─────────────────────────────────────────────
        //  Reporte final
        // ─────────────────────────────────────────────
        log.AppendLine($"\n{"─",40}");
        log.AppendLine($"Total: {fixed_} referencias rellenas.");

        var pending = new List<string>();
        if (gm           == null) pending.Add("GameManager no encontrado — abre Tecnico.unity aditivamente o asigna a mano.");
        if (haptics      == null) pending.Add("HapticFeedback no encontrado — verifica que el XR Rig tiene el componente.");
        if (playerCtrl   == null) pending.Add("PlayerController no encontrado.");
        if (multimeter   == null) pending.Add("Multimeter no encontrado en la escena.");
        if (delivery     == null) pending.Add("ComponentDeliverySystem no encontrado.");
        if (vrBtn        == null) pending.Add("VRValidationButton no encontrado — asígnalo manualmente.");
        if (nodoP13      == null) pending.Add("Nodo_P13 no encontrado — crea un GO con ElectricalNode y nómbralo 'Nodo_P13'.");
        if (nodoGND      == null) pending.Add("Nodo_GND no encontrado.");
        if (nodoA0       == null) pending.Add("Nodo_A0 no encontrado.");

        if (pending.Count > 0)
        {
            log.AppendLine("\nPendientes (requieren acción manual):");
            foreach (var p in pending) log.AppendLine($"  • {p}");
        }

        Debug.Log(log.ToString());
        EditorUtility.DisplayDialog(
            $"Explorador Setup ({fixed_} refs rellenas)",
            log.ToString(), "OK");
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────

    static T Find<T>() where T : Component
        => Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);

    static ElectricalNode FindNodeByName(params string[] names)
    {
        foreach (var t in Object.FindObjectsByType<ElectricalNode>(FindObjectsInactive.Include))
        {
            if (t == null) continue;
            foreach (var n in names)
                if (t.gameObject.name.Equals(n, System.StringComparison.OrdinalIgnoreCase))
                    return t;
        }
        return null;
    }

    /// <summary>De un array de T, devuelve el que comparte más ancestros con 'origin'.</summary>
    static T FindClosestInHierarchy<T>(Transform origin, T[] candidates) where T : Component
    {
        T best    = null;
        int bestD = int.MaxValue;

        foreach (var c in candidates)
        {
            if (c == null) continue;
            int d = HierarchyDistance(origin, c.transform);
            if (d < bestD) { bestD = d; best = c; }
        }
        return best;
    }

    static int HierarchyDistance(Transform a, Transform b)
    {
        // Distancia = pasos para llegar al ancestro común más cercano
        var ancestorsA = new HashSet<Transform>();
        for (var t = a; t != null; t = t.parent) ancestorsA.Add(t);
        int steps = 0;
        for (var t = b; t != null; t = t.parent, steps++)
            if (ancestorsA.Contains(t)) return steps;
        return int.MaxValue;
    }
}
#endif
