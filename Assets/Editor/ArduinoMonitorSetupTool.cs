#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Configura en un click el monitor Display_Arduino del Técnico para que al hacer
/// click en él abran los paneles del Reto 4 (ArduinoIDEUI + TechnicianTelemetryUI).
///
/// Qué hace:
///   1. Encuentra Display_Arduino en la escena activa.
///   2. Añade ArduinoMonitorInteract si no existe.
///   3. Instancia TechnicianMonitorHUD.prefab dentro de Reto4_Zone si no hay
///      ningún ArduinoIDEUI en la escena.
///   4. Posiciona y orienta el HUD frente al monitor.
///   5. Conecta ArduinoIDEUI.bridge → ArduinoNetworkBridge (si existe).
///   6. Conecta TechnicianTelemetryUI.arduinoBridge → ArduinoNetworkBridge.
///   7. Conecta canvas.worldCamera → Pc_Camera (o Camera.main).
///   8. Asigna references en ArduinoMonitorInteract.
///   9. Marca la escena dirty.
///
/// Menú: Tools → TITA → Reto 4 → Setup Monitor Arduino (Tecnico)
/// </summary>
public static class ArduinoMonitorSetupTool
{
    private const string PREFAB_PATH   = "Assets/Prefabs/TechnicianMonitorHUD.prefab";
    private const string MONITOR_NAME  = "Monitor";          // hijo de PC_Arduino
    private const string PC_ARDUINO    = "PC_Arduino";       // padre del monitor
    private const string ZONE_NAME     = "Reto4_Zone";

    [MenuItem("Tools/TITA/Reto 4/Setup Monitor Arduino (Tecnico)")]
    static void Run()
    {
        var log = new System.Text.StringBuilder();
        log.AppendLine("=== ArduinoMonitorSetupTool ===\n");

        // ── 1. Encontrar PC_Arduino > Monitor ─────────────────────────────
        var displayGO = FindMonitorGO();
        if (displayGO == null)
        {
            EditorUtility.DisplayDialog("Error",
                $"No se encontró '{PC_ARDUINO}/{MONITOR_NAME}' en la escena activa.\n\n" +
                "El GO debe llamarse exactamente 'Monitor' y ser hijo de 'PC_Arduino'.\n" +
                "Verifica que la escena Tecnico.unity esté abierta.", "OK");
            return;
        }
        log.AppendLine($"[OK] Monitor encontrado: {displayGO.transform.parent?.name}/{displayGO.name}");

        // ── 2. Añadir ArduinoMonitorInteract ──────────────────────────────
        var interact = displayGO.GetComponent<ArduinoMonitorInteract>();
        if (interact == null)
        {
            interact = Undo.AddComponent<ArduinoMonitorInteract>(displayGO);
            log.AppendLine("[OK] ArduinoMonitorInteract añadido al Display_Arduino.");
        }
        else
            log.AppendLine("[--] ArduinoMonitorInteract ya existía.");

        // ── 3. Asegurar collider ───────────────────────────────────────────
        if (displayGO.GetComponent<Collider>() == null)
        {
            var bc = Undo.AddComponent<BoxCollider>(displayGO);
            bc.size = Vector3.one;
            log.AppendLine("[OK] BoxCollider añadido (faltaba).");
        }

        // ── 4. Encontrar / instanciar TechnicianMonitorHUD ────────────────
        GameObject hudGO = FindHUDInScene();
        if (hudGO == null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
            if (prefab == null)
            {
                log.AppendLine($"[!!] No se encontró el prefab en {PREFAB_PATH}.\n" +
                               "Ejecuta primero: Tools → TITA → Crear HUD Monitor Técnico (Reto 4)");
            }
            else
            {
                var zone = GameObject.Find(ZONE_NAME);
                var parent = zone != null ? zone.transform : displayGO.transform.parent;

                hudGO = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                Undo.RegisterCreatedObjectUndo(hudGO, "Instanciar TechnicianMonitorHUD");
                hudGO.transform.SetParent(parent, false);

                // Posicionar junto al monitor, girado hacia el técnico
                hudGO.transform.localPosition = new Vector3(0f, 1.2f, 0.5f);
                hudGO.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                hudGO.transform.localScale    = Vector3.one;

                hudGO.SetActive(false);   // Empieza oculto, se abre al hacer click
                log.AppendLine($"[OK] TechnicianMonitorHUD instanciado en {parent.name}.");
            }
        }
        else
            log.AppendLine($"[--] TechnicianMonitorHUD ya existía: {hudGO.name}");

        // ── 5. Asignar HUD al interact ────────────────────────────────────
        if (hudGO != null && interact.arduinoHUD == null)
        {
            Undo.RecordObject(interact, "Asignar arduinoHUD");
            interact.arduinoHUD = hudGO;
            log.AppendLine("[OK] ArduinoMonitorInteract.arduinoHUD asignado.");
        }

        // ── 6. Asignar CircuitPanel ───────────────────────────────────────
        if (interact.circuitPanel == null)
        {
            var ecp = Object.FindAnyObjectByType<ExplorerCircuitPanel>(FindObjectsInactive.Include);
            if (ecp != null)
            {
                Undo.RecordObject(interact, "Asignar circuitPanel");
                interact.circuitPanel = ecp.gameObject;
                log.AppendLine($"[OK] circuitPanel → {ecp.gameObject.name}");
            }
        }

        // ── 7. Conectar cámara al interact y al Canvas del HUD ────────────
        var pcCam = FindPcCamera();
        if (pcCam != null)
        {
            Undo.RecordObject(interact, "Asignar pcCamera");
            interact.pcCamera = pcCam;
            log.AppendLine($"[OK] pcCamera → {pcCam.name}");

            if (hudGO != null)
            {
                foreach (var canvas in hudGO.GetComponentsInChildren<Canvas>(true))
                {
                    if (canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
                    {
                        Undo.RecordObject(canvas, "Asignar worldCamera");
                        canvas.worldCamera = pcCam;
                    }
                }
            }
        }

        // ── 8. Conectar ArduinoNetworkBridge ─────────────────────────────
        var bridge = Object.FindAnyObjectByType<ArduinoNetworkBridge>(FindObjectsInactive.Include);
        if (bridge != null && hudGO != null)
        {
            var ide = hudGO.GetComponentInChildren<ArduinoIDEUI>(true);
            if (ide != null && ide.bridge == null)
            {
                Undo.RecordObject(ide, "Asignar bridge");
                ide.bridge = bridge;
                log.AppendLine($"[OK] ArduinoIDEUI.bridge → {bridge.name}");
            }

            var tele = hudGO.GetComponentInChildren<TechnicianTelemetryUI>(true);
            if (tele != null && tele.arduinoBridge == null)
            {
                Undo.RecordObject(tele, "Asignar arduinoBridge");
                tele.arduinoBridge = bridge;
                log.AppendLine($"[OK] TechnicianTelemetryUI.arduinoBridge → {bridge.name}");
            }
        }
        else if (bridge == null)
            log.AppendLine("[??] ArduinoNetworkBridge no encontrado en escena (opcional).");

        // ── 9. GraphicRaycaster en HUD canvas ─────────────────────────────
        if (hudGO != null)
        {
            foreach (var canvas in hudGO.GetComponentsInChildren<Canvas>(true))
            {
                if (canvas.renderMode == RenderMode.WorldSpace
                    && canvas.GetComponent<GraphicRaycaster>() == null)
                {
                    Undo.AddComponent<GraphicRaycaster>(canvas.gameObject);
                    log.AppendLine($"[OK] GraphicRaycaster añadido a {canvas.name}");
                }
            }
        }

        // ── 10. Marcar dirty ──────────────────────────────────────────────
        EditorUtility.SetDirty(displayGO);
        if (hudGO != null) EditorUtility.SetDirty(hudGO);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log(log.ToString());
        EditorUtility.DisplayDialog(
            "Monitor Arduino configurado",
            log.ToString() +
            "\nHaz click en Display_Arduino en Play Mode para abrir los paneles del Reto 4.",
            "OK");
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────

    static GameObject FindMonitorGO()
    {
        // Buscar "PC_Arduino" y luego su hijo "Monitor"
        var pcArduino = GameObject.Find(PC_ARDUINO);
        if (pcArduino != null)
        {
            var monitor = pcArduino.transform.Find(MONITOR_NAME);
            if (monitor != null) return monitor.gameObject;
        }

        // Fallback: buscar "Monitor" en toda la escena
        var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
        foreach (var t in all)
            if (t.name == MONITOR_NAME && t.parent != null)
                return t.gameObject;

        return null;
    }

    static GameObject FindHUDInScene()
    {
        // Buscar un Canvas que contenga ArduinoIDEUI — ese es el HUD correcto.
        // NO usar transform.root porque en la mayoría de setups el root es PC_Arduino, no el HUD.
        var ide = Object.FindAnyObjectByType<ArduinoIDEUI>(FindObjectsInactive.Include);
        if (ide != null)
        {
            var t = ide.transform;
            while (t != null)
            {
                if (t.GetComponent<Canvas>() != null) return t.gameObject;
                t = t.parent;
            }
            // ArduinoIDEUI existe pero sin Canvas padre → ignorar, instanciar prefab
        }

        // Fallback por nombre (Canvas directamente en ese GO)
        string[] names = { "TechnicianMonitorHUD", "ArduinoHUD", "ArduinoMonitorHUD" };
        foreach (var n in names)
        {
            var go = GameObject.Find(n);
            if (go != null && go.GetComponent<Canvas>() != null) return go;
        }
        return null;
    }

    static Camera FindPcCamera()
    {
        // Intentar por nombre (nombres conocidos en este proyecto)
        string[] names = { "Pc_Camera", "PC_Camera", "PcCamera", "DeskCamera", "pcCamera" };
        foreach (var n in names)
        {
            var go = GameObject.Find(n);
            if (go != null)
            {
                var cam = go.GetComponent<Camera>();
                if (cam != null) return cam;
            }
        }
        return Camera.main;
    }
}
#endif
