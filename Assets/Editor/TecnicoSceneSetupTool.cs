#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Rellena automáticamente las referencias vacías en la escena del Técnico
/// y limpia ConnectionManagers duplicados DE FORMA SEGURA.
///
/// SEGURIDAD: Solo elimina GOs que:
///   • Se llamen exactamente "NetworkManager" (nombre estándar del stray)
///   • Estén en posición alejada del origen (> 50 unidades) — el stray estaba en (1322,0,-8)
///   • O solo contengan componentes de red (Transform + ConnectionManager + posibles Fusion)
/// NUNCA elimina "GameManager_System" ni GOs que contengan otros componentes críticos.
///
/// Menú: Tools → TITA → Red → Rellenar referencias escena Técnico
/// </summary>
public static class TecnicoSceneSetupTool
{
    [MenuItem("Tools/TITA/Red/Rellenar referencias escena Tecnico")]
    static void Run()
    {
        var log   = new System.Text.StringBuilder();
        int fixed_ = 0;

        log.AppendLine("=== TecnicoSceneSetupTool ===\n");

        // ── 1. Buscar componentes clave (con fallback para prefab instances) ─
        var gm          = FindInScene<GameManager>();
        var multimeter  = FindInScene<Multimeter>();
        var instrSys    = FindInScene<InstructionSystem>();
        var techActions = FindInScene<TechnicianActions>();
        var hudCtrl     = FindInScene<TechnicianHUDController>();
        var perfTracker = FindInScene<PerformanceTracker>();
        var bridge      = FindInScene<ArduinoNetworkBridge>();
        var protoSim    = FindInScene<ProtoboardSimulator>();
        var circuitSim  = FindInScene<CircuitSimulator>();

        // ── 2. Limpiar ConnectionManagers duplicados (SEGURO) ─────────────
        var allCMs = Object.FindObjectsByType<ConnectionManager>(FindObjectsInactive.Include);

        ConnectionManager bestCM = null;
        foreach (var cm in allCMs)
        {
            if (cm == null) continue;
            var  so       = new SerializedObject(cm);
            var  propPref = so.FindProperty("playerPrefab");
            bool hasRef   = propPref?.FindPropertyRelative("AssetGuidLow")?.longValue != 0;
            if (hasRef) { bestCM = cm; break; }
        }
        bestCM ??= allCMs.FirstOrDefault(c => c != null);

        int removed = 0;
        foreach (var cm in allCMs)
        {
            if (cm == null || cm == bestCM) continue;

            // GUARDIA DE SEGURIDAD: solo eliminar GOs que son claramente stray
            var go = cm.gameObject;
            if (!IsSafeToDel(go, cm))
            {
                log.AppendLine($"[SKIP] CM en '{go.name}' ({go.scene.name}) NO eliminado " +
                               "— contiene otros componentes o no es un stray.");
                continue;
            }

            log.AppendLine($"[LIMPIEZA] Stray NetworkManager eliminado: '{go.name}' en {go.scene.name} " +
                           $"pos={go.transform.position}");
            EditorSceneManager.MarkSceneDirty(go.scene);
            Undo.DestroyObjectImmediate(go);
            removed++;
        }

        if (removed > 0) { log.AppendLine($"[OK] {removed} stray(s) eliminado(s)."); fixed_ += removed; }
        else              log.AppendLine("[--] Sin stray NetworkManagers para eliminar.");

        // ── 3. ConnectionManager → GameManager ───────────────────────────
        if (bestCM != null && gm != null)
        {
            var so = new SerializedObject(bestCM);
            var p  = so.FindProperty("gameManager");
            if (p != null && p.objectReferenceValue == null)
            {
                Undo.RecordObject(bestCM, "Asignar gameManager");
                p.objectReferenceValue = gm;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(bestCM);
                log.AppendLine($"[OK] ConnectionManager.gameManager → {gm.gameObject.name}");
                fixed_++;
            }
        }

        // ── 4. GameManager.multimeter ─────────────────────────────────────
        if (gm != null && multimeter != null && gm.multimeter == null)
        {
            Undo.RecordObject(gm, "Asignar multimeter");
            gm.multimeter = multimeter;
            EditorUtility.SetDirty(gm);
            log.AppendLine($"[OK] GameManager.multimeter → {multimeter.gameObject.name}");
            fixed_++;
        }

        // ── 5. GameManager.protoSim (Reto 4) ─────────────────────────────
        if (gm != null && protoSim != null && gm.protoSim == null)
        {
            Undo.RecordObject(gm, "Asignar protoSim");
            gm.protoSim = protoSim;
            EditorUtility.SetDirty(gm);
            log.AppendLine($"[OK] GameManager.protoSim → {protoSim.gameObject.name}");
            fixed_++;
        }

        // ── 6. GameManager.circuit (Retos 1-3) ───────────────────────────
        if (gm != null && circuitSim != null && gm.circuit == null)
        {
            Undo.RecordObject(gm, "Asignar circuit");
            gm.circuit = circuitSim;
            EditorUtility.SetDirty(gm);
            log.AppendLine($"[OK] GameManager.circuit → {circuitSim.gameObject.name}");
            fixed_++;
        }

        // ── 7. GameManager.performance ────────────────────────────────────
        if (gm != null && perfTracker != null && gm.performance == null)
        {
            Undo.RecordObject(gm, "Asignar performance");
            gm.performance = perfTracker;
            EditorUtility.SetDirty(gm);
            log.AppendLine($"[OK] GameManager.performance → {perfTracker.gameObject.name}");
            fixed_++;
        }

        // ── 8. GameManager.instructionSystem ─────────────────────────────
        if (gm != null && instrSys != null && gm.instructionSystem == null)
        {
            Undo.RecordObject(gm, "Asignar instructionSystem");
            gm.instructionSystem = instrSys;
            EditorUtility.SetDirty(gm);
            log.AppendLine($"[OK] GameManager.instructionSystem → {instrSys.gameObject.name}");
            fixed_++;
        }

        // ── 9. InstructionSystem ──────────────────────────────────────────
        if (instrSys != null)
        {
            if (instrSys.gameManager == null && gm != null)
            {
                Undo.RecordObject(instrSys, "Asignar gameManager");
                instrSys.gameManager = gm;
                EditorUtility.SetDirty(instrSys);
                log.AppendLine($"[OK] InstructionSystem.gameManager → {gm.gameObject.name}");
                fixed_++;
            }
            if (instrSys.multimeter == null && multimeter != null)
            {
                Undo.RecordObject(instrSys, "Asignar multimeter");
                instrSys.multimeter = multimeter;
                EditorUtility.SetDirty(instrSys);
                log.AppendLine($"[OK] InstructionSystem.multimeter → {multimeter.gameObject.name}");
                fixed_++;
            }
            if (instrSys.technicianActions == null && techActions != null)
            {
                Undo.RecordObject(instrSys, "Asignar technicianActions");
                instrSys.technicianActions = techActions;
                EditorUtility.SetDirty(instrSys);
                log.AppendLine($"[OK] InstructionSystem.technicianActions → {techActions.gameObject.name}");
                fixed_++;
            }
        }

        // ── 10. TechnicianHUDController.gameManager ───────────────────────
        if (hudCtrl != null && gm != null && hudCtrl.gameManager == null)
        {
            Undo.RecordObject(hudCtrl, "Asignar gameManager");
            hudCtrl.gameManager = gm;
            EditorUtility.SetDirty(hudCtrl);
            log.AppendLine($"[OK] TechnicianHUDController.gameManager → {gm.gameObject.name}");
            fixed_++;
        }

        // ── 11. ArduinoIDEUI.bridge ───────────────────────────────────────
        var ide = FindInScene<ArduinoIDEUI>();
        if (ide != null && bridge != null && ide.bridge == null)
        {
            Undo.RecordObject(ide, "Asignar bridge");
            ide.bridge = bridge;
            EditorUtility.SetDirty(ide);
            log.AppendLine($"[OK] ArduinoIDEUI.bridge → {bridge.gameObject.name}");
            fixed_++;
        }
        else if (ide != null && bridge == null)
            log.AppendLine("[??] ArduinoIDEUI.bridge: sin bridge en esta escena — " +
                           "Opción A (OnBridgeReady) lo conectará en runtime.");

        // ── 12. TechnicianTelemetryUI.arduinoBridge ───────────────────────
        var tele = FindInScene<TechnicianTelemetryUI>();
        if (tele != null && bridge != null && tele.arduinoBridge == null)
        {
            Undo.RecordObject(tele, "Asignar arduinoBridge");
            tele.arduinoBridge = bridge;
            EditorUtility.SetDirty(tele);
            log.AppendLine($"[OK] TechnicianTelemetryUI.arduinoBridge → {bridge.gameObject.name}");
            fixed_++;
        }
        else if (tele != null && bridge == null)
            log.AppendLine("[??] TechnicianTelemetryUI.arduinoBridge: sin bridge — " +
                           "Opción A (OnBridgeReady) lo conectará en runtime.");

        // ── 13. Marcar escena dirty ───────────────────────────────────────
        EditorSceneManager.MarkAllScenesDirty();

        // ── Reporte ───────────────────────────────────────────────────────
        log.AppendLine($"\n{"─",40}");
        log.AppendLine($"Total: {fixed_} referencias rellenas.");

        var pending = new List<string>();
        if (gm == null)         pending.Add("GameManager no encontrado — ¿está en el prefab GameManager_System? Ábrelo en el Inspector.");
        if (multimeter == null) pending.Add("Multimeter no encontrado (puede estar en Explorador.unity).");
        if (bestCM != null)
        {
            var so = new SerializedObject(bestCM);
            if (so.FindProperty("entornoExplorador")?.objectReferenceValue == null)
                pending.Add("ConnectionManager.entornoExplorador — asignar GO del entorno VR manualmente.");
        }
        if (gm?.reto4Zone == null) pending.Add("GameManager.reto4Zone (puede estar ya asignado en prefab override).");

        if (pending.Count > 0)
        {
            log.AppendLine("\nPendientes:");
            foreach (var p in pending) log.AppendLine($"  • {p}");
        }

        Debug.Log(log.ToString());
        EditorUtility.DisplayDialog($"Setup ({fixed_} refs rellenas)", log.ToString(), "OK");
    }

    // ─────────────────────────────────────────────────────────────────────
    //  FindInScene: FindAnyObjectOfType + fallback a Resources para prefab instances
    // ─────────────────────────────────────────────────────────────────────

    static T FindInScene<T>() where T : Component
    {
        // Intento 1: búsqueda estándar (funciona para GOs normales)
        var found = Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
        if (found != null) return found;

        // Intento 2: Resources.FindObjectsOfTypeAll (encuentra componentes en
        //            prefab instances "stripped" que FindAnyObjectByType puede omitir)
        var all = Resources.FindObjectsOfTypeAll<T>();
        foreach (var obj in all)
        {
            // Filtrar: solo objetos en una escena cargada (no assets de proyecto)
            if (obj.gameObject.scene.IsValid() && obj.gameObject.scene.isLoaded)
                return obj;
        }
        return null;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  IsSafeToDel: solo eliminar GOs que son claramente stray
    // ─────────────────────────────────────────────────────────────────────

    static bool IsSafeToDel(GameObject go, ConnectionManager cm)
    {
        // NUNCA eliminar GOs con nombre importante
        string nameLower = go.name.ToLowerInvariant();
        if (nameLower.Contains("gamemanager") ||
            nameLower.Contains("system")      ||
            nameLower.Contains("manager_system")) return false;

        // Solo eliminar si el GO se llama "NetworkManager" (nombre del stray)
        if (!go.name.Equals("NetworkManager", System.StringComparison.OrdinalIgnoreCase))
            return false;

        // Solo si el GO está muy lejos del origen (el stray estaba en 1322,0,-8)
        float dist = go.transform.position.magnitude;
        if (dist > 50f) return true;   // posición claramente fuera del escenario

        // Solo si el GO tiene MUY POCOS componentes (Transform + ConnectionManager + tal vez 1-2 Fusion)
        var comps = go.GetComponents<Component>();
        if (comps.Length <= 4) return true;  // Transform, CM, y poco más = seguro

        return false;
    }
}
#endif
