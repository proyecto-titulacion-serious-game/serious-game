#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Auto-asigna todas las referencias del Inspector en TechnicianMonitorHUD
/// (prefab instanciado en la escena Tecnico.unity).
///
/// Resuelve los 3 pendientes de la memoria:
///   • TechnicianHUDController.gameManager
///   • ArduinoIDEUI.bridge
///   • TechnicianTelemetryUI.circuit  +  TechnicianTelemetryUI.arduinoBridge
///
/// También añade TechnicianCameraController a Pc_Camera si no lo tiene.
///
/// Menú: Tools → TITA → Conectar HUD Monitor Técnico
/// </summary>
public static class TechnicianMonitorConnector
{
    [MenuItem("Tools/TITA/Conectar HUD Monitor Técnico")]
    static void Connect()
    {
        int fixed_ = 0;
        var log = new System.Text.StringBuilder();

        // ── Buscar TechnicianMonitorHUD en escena ────────────────────────
        var hud  = Object.FindAnyObjectByType<TechnicianHUDController>();
        var tele = Object.FindAnyObjectByType<TechnicianTelemetryUI>();
        var ide  = Object.FindAnyObjectByType<ArduinoIDEUI>();

        if (hud == null && tele == null && ide == null)
        {
            EditorUtility.DisplayDialog("Monitor no encontrado",
                "No se encontró TechnicianHUDController, TechnicianTelemetryUI\n" +
                "ni ArduinoIDEUI en la escena activa.\n\n" +
                "Asegúrate de haber instanciado TechnicianMonitorHUD.prefab.", "OK");
            return;
        }

        // ── GameManager ──────────────────────────────────────────────────
        var gm = Object.FindAnyObjectByType<GameManager>();
        if (hud != null && gm != null && hud.gameManager == null)
        {
            Undo.RecordObject(hud, "Asignar GameManager");
            hud.gameManager = gm;
            EditorUtility.SetDirty(hud);
            log.AppendLine("✅ TechnicianHUDController.gameManager → " + gm.name);
            fixed_++;
        }
        else if (hud != null && hud.gameManager != null)
            log.AppendLine("✓  TechnicianHUDController.gameManager ya asignado.");
        else if (gm == null)
            log.AppendLine("⚠  GameManager no encontrado en escena.");

        // ── ArduinoNetworkBridge ─────────────────────────────────────────
        var bridge = Object.FindAnyObjectByType<ArduinoNetworkBridge>();
        if (ide != null && bridge != null && ide.bridge == null)
        {
            Undo.RecordObject(ide, "Asignar Bridge");
            ide.bridge = bridge;
            EditorUtility.SetDirty(ide);
            log.AppendLine("✅ ArduinoIDEUI.bridge → " + bridge.name);
            fixed_++;
        }
        else if (ide != null && ide.bridge != null)
            log.AppendLine("✓  ArduinoIDEUI.bridge ya asignado.");
        else if (bridge == null)
            log.AppendLine("⚠  ArduinoNetworkBridge no encontrado en escena.");

        if (tele != null && bridge != null && tele.arduinoBridge == null)
        {
            Undo.RecordObject(tele, "Asignar Bridge a Telemetría");
            tele.arduinoBridge = bridge;
            EditorUtility.SetDirty(tele);
            log.AppendLine("✅ TechnicianTelemetryUI.arduinoBridge → " + bridge.name);
            fixed_++;
        }

        // ── CircuitSimulator ─────────────────────────────────────────────
        var sim = Object.FindAnyObjectByType<CircuitSimulator>();
        if (tele != null && sim != null && tele.circuit == null)
        {
            Undo.RecordObject(tele, "Asignar CircuitSimulator");
            tele.circuit = sim;
            EditorUtility.SetDirty(tele);
            log.AppendLine("✅ TechnicianTelemetryUI.circuit → " + sim.name);
            fixed_++;
        }
        else if (tele != null && tele.circuit != null)
            log.AppendLine("✓  TechnicianTelemetryUI.circuit ya asignado.");
        else if (sim == null)
            log.AppendLine("⚠  CircuitSimulator no encontrado en escena.");

        // ── TechnicianCameraController en Pc_Camera ──────────────────────
        var pcCam = FindCameraByName("Pc_Camera", "PC_Camera", "pcCamera", "pc_camera");
        if (pcCam != null)
        {
            if (pcCam.GetComponent<TechnicianCameraController>() == null)
            {
                Undo.AddComponent<TechnicianCameraController>(pcCam.gameObject);
                log.AppendLine("✅ TechnicianCameraController añadido a " + pcCam.name);
                fixed_++;
            }
            else
                log.AppendLine("✓  TechnicianCameraController ya existe en " + pcCam.name);
        }
        else
            log.AppendLine("⚠  Pc_Camera no encontrada — añade TechnicianCameraController manualmente.");

        // ── WorldCamera en TechnicianMonitorHUD ──────────────────────────
        if (hud != null)
        {
            var canvas = hud.GetComponent<Canvas>();
            if (canvas != null && canvas.renderMode == UnityEngine.RenderMode.WorldSpace
                && canvas.worldCamera == null && pcCam != null)
            {
                Undo.RecordObject(canvas, "Asignar worldCamera");
                canvas.worldCamera = pcCam;
                EditorUtility.SetDirty(canvas);
                log.AppendLine("✅ Canvas.worldCamera → " + pcCam.name);
                fixed_++;
            }
        }

        // PCMonitorInteract.monitorHUD es [NonSerialized] — cross-scene safe.
        // Se auto-asigna en Start() via FindAnyObjectByType. No requiere asignación aquí.
        var monitorInteract = Object.FindAnyObjectByType<PCMonitorInteract>();
        if (monitorInteract != null)
            log.AppendLine("✓  PCMonitorInteract presente — monitorHUD se asigna en runtime.");
        else
            log.AppendLine("⚠  PCMonitorInteract no encontrado — añádelo al mesh del monitor (GO 'Monitor' en NoonA).");

        // ── Guardar escena ────────────────────────────────────────────────
        if (fixed_ > 0)
            EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        string title = fixed_ > 0
            ? $"HUD conectado ({fixed_} referencia(s) asignada(s))"
            : "HUD Monitor — sin cambios";

        EditorUtility.DisplayDialog(title, log.ToString(), "OK");
        Debug.Log("[TechnicianMonitorConnector]\n" + log);
    }

    static Camera FindCameraByName(params string[] names)
    {
        foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
            foreach (var n in names)
                if (cam.name.Equals(n, System.StringComparison.OrdinalIgnoreCase))
                    return cam;
        return null;
    }
}
#endif
