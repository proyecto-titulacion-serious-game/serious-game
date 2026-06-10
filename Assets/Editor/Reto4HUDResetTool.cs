#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Reconstruye TechnicianMonitorHUD en la escena con el diseño v2.
///
/// FLUJO:
///   Paso 1 — Genera el prefab v2 y fuerza su importación en el AssetDatabase.
///   Paso 2 — (diferido un frame) Carga el prefab y lo instancia en la escena.
///
/// Menú: Tools → TITA → Reto 4 → Resetear HUD Monitor (v2 - diseño completo)
/// </summary>
public static class Reto4HUDResetTool
{
    const string PREFAB_PATH = "Assets/Prefabs/TechnicianMonitorHUD.prefab";
    const string MENU        = "Tools/TITA/Reto 4/Resetear HUD Monitor (v2 - diseño completo)";

    [MenuItem(MENU)]
    static void Run()
    {
        if (!EditorUtility.DisplayDialog(
            "Resetear HUD Monitor v2",
            "Esto hará:\n" +
            "1. Regenerar el prefab TechnicianMonitorHUD con diseño v2.\n" +
            "2. Eliminar la instancia antigua del HUD en la escena.\n" +
            "3. Instanciar el nuevo HUD correctamente.\n\n" +
            "¿Continuar?", "Sí, resetear", "Cancelar"))
            return;

        // ── Paso 1: generar el prefab ──────────────────────────────────────
        bool built = BuildPrefab();
        if (!built) return;

        // Forzar importación del asset antes de usarlo
        AssetDatabase.ImportAsset(PREFAB_PATH, ImportAssetOptions.ForceUpdate);
        AssetDatabase.SaveAssets();

        // ── Paso 2: diferido un frame para que Unity procese el asset ──────
        EditorApplication.delayCall += PlaceHUDInScene;
    }

    // ─────────────────────────────────────────────
    //  Paso 1 — Construir el prefab
    // ─────────────────────────────────────────────
    static bool BuildPrefab()
    {
        try
        {
            // Llama al builder silencioso (sin dialog propio)
            TechnicianMonitorHUDBuilderV2.BuildSilent();
            Debug.Log("[Reto4HUDReset] Prefab v2 generado correctamente.");
            return true;
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error en Build",
                $"No se pudo generar el prefab:\n{e.Message}\n\n" +
                "Verifica que TechnicianMonitorHUDBuilderV2.cs compila sin errores.", "OK");
            Debug.LogException(e);
            return false;
        }
    }

    // ─────────────────────────────────────────────
    //  Paso 2 — Instanciar en escena (diferido)
    // ─────────────────────────────────────────────
    static void PlaceHUDInScene()
    {
        EditorApplication.delayCall -= PlaceHUDInScene;

        var log = new System.Text.StringBuilder("=== PlaceHUDInScene ===\n\n");

        // Cargar prefab
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
        if (prefab == null)
        {
            EditorUtility.DisplayDialog("Error",
                $"No se pudo cargar el prefab en:\n{PREFAB_PATH}\n\n" +
                "Ejecuta primero:\n" +
                "Tools → TITA → Reto 4 → Reconstruir HUD Monitor Técnico (v2)", "OK");
            return;
        }
        log.AppendLine($"[OK] Prefab cargado: {PREFAB_PATH}");

        // Eliminar instancias antiguas
        int removed = 0;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (t == null || t.name != "TechnicianMonitorHUD") continue;
            if (t.GetComponent<Canvas>() == null) continue;
            log.AppendLine($"[OK] Eliminando: {GetPath(t)}");
            Undo.DestroyObjectImmediate(t.gameObject);
            removed++;
        }
        log.AppendLine(removed > 0
            ? $"[OK] {removed} instancia(s) antigua(s) eliminada(s)."
            : "[--] Sin instancias antiguas.");

        // Limpiar paneles huérfanos fuera de cualquier Canvas
        foreach (var n in new[] { "Panel_CodeEditor", "Panel_ArduinoIDE", "Panel_Telemetria" })
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (t == null || t.name != n) continue;
                if (t.GetComponentInParent<Canvas>() != null) continue; // está dentro de algo
                log.AppendLine($"[OK] Panel huérfano eliminado: {t.name}");
                Undo.DestroyObjectImmediate(t.gameObject);
                break;
            }
        }

        // Buscar o crear Monitor_Arduino como parent del HUD
        Transform parent = FindOrCreateMonitorArduino();
        log.AppendLine($"[OK] Parent: {GetPath(parent)} (Monitor_Arduino)");

        // Instanciar
        var hud = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(hud, "Instanciar TechnicianMonitorHUD v2");

        if (parent != null) hud.transform.SetParent(parent, false);
        hud.transform.localPosition = new Vector3(0f, 0.08f, 0.15f);
        hud.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        hud.transform.localScale    = Vector3.one;
        hud.SetActive(false);
        log.AppendLine("[OK] TechnicianMonitorHUD v2 instanciado e inactivo.");

        // Asignar worldCamera
        Camera cam = FindPcCamera();
        if (cam != null)
        {
            var canvas = hud.GetComponent<Canvas>();
            if (canvas != null)
            {
                Undo.RecordObject(canvas, "worldCamera");
                canvas.worldCamera = cam;
                log.AppendLine($"[OK] worldCamera → {cam.name}");
            }
        }

        // Conectar ArduinoMonitorInteract
        var interact = Object.FindAnyObjectByType<ArduinoMonitorInteract>(FindObjectsInactive.Include);
        if (interact != null)
        {
            Undo.RecordObject(interact, "arduinoHUD");
            interact.arduinoHUD = hud;
            if (cam != null && interact.pcCamera == null)
            {
                Undo.RecordObject(interact, "pcCamera");
                interact.pcCamera = cam;
            }
            log.AppendLine("[OK] ArduinoMonitorInteract.arduinoHUD → HUD v2");
        }
        else
            log.AppendLine("[??] ArduinoMonitorInteract no encontrado.");

        // Conectar ArdityManager ↔ SerialController
        var ardity  = Object.FindAnyObjectByType<ArdityManager>(FindObjectsInactive.Include);
        var serial  = Object.FindAnyObjectByType<SerialController>(FindObjectsInactive.Include);
        if (ardity != null && serial != null)
        {
            if (ardity.serialController == null)
            {
                Undo.RecordObject(ardity, "serialController");
                ardity.serialController = serial;
            }
            if (serial.messageListener == null)
            {
                Undo.RecordObject(serial, "messageListener");
                serial.messageListener = ardity.gameObject;
            }
            log.AppendLine($"[OK] ArdityManager ↔ SerialController conectados.");
        }
        else if (ardity != null)
            log.AppendLine("[??] SerialController no encontrado — arrastra Ardity/Prefabs/SerialController a la escena.");

        // Guardar
        EditorUtility.SetDirty(hud);
        if (interact != null) EditorUtility.SetDirty(interact.gameObject);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        log.AppendLine("\n✅ LISTO — Ctrl+S para guardar la escena.");
        Debug.Log(log.ToString());
        EditorUtility.DisplayDialog("HUD v2 instanciado", log.ToString(), "OK");
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────

    /// <summary>
    /// Busca Monitor_Arduino por nombre exacto.
    /// Si no existe lo crea como hijo de Reto4_Zone (o raíz si tampoco hay).
    /// </summary>
    static Transform FindOrCreateMonitorArduino()
    {
        // 1. Buscar por nombre exacto (activo e inactivo)
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (t != null && t.name == "Monitor_Arduino")
                return t;
        }

        // 2. No existe → crear como hijo de Reto4_Zone o PC_Arduino > Monitor
        Transform parent = null;

        var reto4 = GameObject.Find("Reto4_Zone");
        if (reto4 != null)
        {
            parent = reto4.transform;
        }
        else
        {
            // Fallback: buscar Monitor dentro de PC_Arduino
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (t == null) continue;
                if (t.name == "Monitor" && t.parent != null
                    && (t.parent.name.Contains("Arduino") || t.parent.name.Contains("PC")))
                { parent = t; break; }
            }
        }

        var go = new GameObject("Monitor_Arduino");
        Undo.RegisterCreatedObjectUndo(go, "Crear Monitor_Arduino");
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale    = Vector3.one;

        Debug.Log($"[Reto4HUDReset] Monitor_Arduino creado bajo '{(parent != null ? parent.name : "raíz")}'.");
        return go.transform;
    }

    static Camera FindPcCamera()
    {
        foreach (var n in new[] { "Pc_Camera", "PC_Camera", "PcCamera", "DeskCamera" })
        {
            var go = GameObject.Find(n);
            if (go != null) { var c = go.GetComponent<Camera>(); if (c != null) return c; }
        }
        foreach (var cam in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
        {
            string n = cam.gameObject.name.ToLower();
            if (n.Contains("pc") || n.Contains("desk") || n.Contains("work")) return cam;
        }
        return Camera.main;
    }

    static string GetPath(Transform t) =>
        t.parent == null ? t.name : GetPath(t.parent) + "/" + t.name;
}
#endif
