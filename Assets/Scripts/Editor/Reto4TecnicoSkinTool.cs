using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Aplica el skin "AXISTUDIO" del boceto Técnico (Reto 4) al HUD existente:
///   - Añade un fondo ambiente oscuro-neón detrás de todo (no intercepta clics).
///   - Genera los paneles SERIAL MONITOR (abajo-izq) y NETWORK DATA INTERCEPT (abajo-der)
///     con su UI ya visible en el Editor y cableada al componente que los alimenta
///     con datos reales de red en Play.
///
/// No modifica ni reordena los elementos del IDE existente: solo agrega. Idempotente
/// (no duplica si ya se aplicó). Menú: Tools → TITA → Reto 4 → Aplicar skin AXISTUDIO.
/// </summary>
public static class Reto4TecnicoSkinTool
{
    [MenuItem("Tools/TITA/Reto 4/Aplicar skin AXISTUDIO (Tecnico)")]
    public static void Apply()
    {
        var ide = Object.FindAnyObjectByType<ArduinoIDEUI>(FindObjectsInactive.Include);
        Canvas canvas = ide != null
            ? ide.GetComponentInParent<Canvas>()
            : Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);

        if (canvas == null)
        {
            EditorUtility.DisplayDialog("AXISTUDIO",
                "No se encontró ningún Canvas (ni ArduinoIDEUI) en la escena del Técnico.\n" +
                "Abre Tecnico.unity (o el HUD del monitor) y reintenta.", "Cerrar");
            return;
        }

        AddBackdrop(canvas);
        BuildSerialMonitor(canvas);
        BuildNetworkIntercept(canvas);

        EditorUtility.SetDirty(canvas);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Selection.activeObject = canvas.gameObject;
        Debug.Log("[AXISTUDIO] Skin aplicado al HUD del Técnico. Guarda la escena (Ctrl+S). " +
                  "Los paneles se llenan con datos reales de red en Play.");
    }

    // ─── Paneles ─────────────────────────────────────────────────────────
    static void BuildSerialMonitor(Canvas canvas)
    {
        if (canvas.GetComponentInChildren<SerialMonitorFeed>(true) != null)
        {
            Debug.Log("[AXISTUDIO] SerialMonitorFeed ya existe — omitido.");
            return;
        }
        var host = NewStretchChild(canvas.transform, "SerialMonitor (AXISTUDIO)");
        var feed = host.AddComponent<SerialMonitorFeed>();
        TMP_Text body = AxiStudioTheme.BuildPanel(host.transform,
            "SERIAL MONITOR  -  Node 7", new Vector2(0.012f, 0.02f), new Vector2(0.40f, 0.36f));
        Wire(feed, "bodyText", body);
    }

    static void BuildNetworkIntercept(Canvas canvas)
    {
        if (canvas.GetComponentInChildren<NetworkDataIntercept>(true) != null)
        {
            Debug.Log("[AXISTUDIO] NetworkDataIntercept ya existe — omitido.");
            return;
        }
        var host = NewStretchChild(canvas.transform, "NetworkDataIntercept (AXISTUDIO)");
        var net = host.AddComponent<NetworkDataIntercept>();
        TMP_Text body = AxiStudioTheme.BuildPanel(host.transform,
            "NETWORK DATA INTERCEPT", new Vector2(0.60f, 0.02f), new Vector2(0.988f, 0.40f), 12);
        Wire(net, "bodyText", body);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────
    static void AddBackdrop(Canvas canvas)
    {
        if (canvas.transform.Find("AXISTUDIO_Backdrop") != null) return;
        var go = NewStretchChild(canvas.transform, "AXISTUDIO_Backdrop");
        go.transform.SetAsFirstSibling();
        var img = go.AddComponent<Image>();
        img.color = new Color(0.01f, 0.04f, 0.06f, 0.55f);
        img.raycastTarget = false;
    }

    static GameObject NewStretchChild(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        return go;
    }

    static void Wire(Component target, string field, Object value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(field);
        if (prop != null) { prop.objectReferenceValue = value; so.ApplyModifiedProperties(); }
        else Debug.LogWarning($"[AXISTUDIO] Campo '{field}' no encontrado en {target.GetType().Name}.");
    }
}
