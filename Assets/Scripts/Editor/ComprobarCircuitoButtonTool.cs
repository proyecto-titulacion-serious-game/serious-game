#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Crea y cablea el botón "Comprobar Circuito" (campo <c>ArduinoIDEUI.btnComprobarCircuito</c>)
/// dentro del prefab TechnicianMonitorHUD. El prefab se guardó antes de que ese campo existiera,
/// así que queda null: solo la tecla F5 dispara la comprobación del Reto 4. Esta herramienta
/// clona el botón "Subir código" como base de layout, le pone una etiqueta y lo asigna.
///
/// El listener onClick → ComprobarCircuito() lo añade ArduinoIDEUI en runtime (Awake), así que
/// aquí solo hace falta dejar la referencia cableada (sin listener persistente).
///
/// Menú: Tools → TITA → Reto 4 → Cablear botón Comprobar Circuito (HUD Técnico)
/// </summary>
public static class ComprobarCircuitoButtonTool
{
    const string MENU = "Tools/TITA/Reto 4/Cablear botón Comprobar Circuito (HUD Técnico)";

    [MenuItem(MENU)]
    static void Wire()
    {
        bool ok = DoWire(out string msg);
        EditorUtility.DisplayDialog("TITA — Comprobar Circuito", msg, "OK");
        Debug.Log($"[ComprobarCircuitoButtonTool] {(ok ? "OK" : "SIN CAMBIOS")}: {msg}");
    }

    /// <summary>
    /// Entrada para modo batch (-executeMethod ComprobarCircuitoButtonTool.WireBatch).
    /// No muestra diálogos; loguea el resultado y sale con código 0 (cableado/ya cableado) o 1 (error).
    /// </summary>
    public static void WireBatch()
    {
        bool ok = DoWire(out string msg);
        Debug.Log($"[ComprobarCircuitoButtonTool] RESULTADO: {msg}");
        EditorApplication.Exit(ok ? 0 : 1);
    }

    /// <summary>
    /// Lógica central. Devuelve true si el campo quedó cableado (ahora o ya lo estaba).
    /// </summary>
    static bool DoWire(out string msg)
    {
        // ── Localizar el prefab TechnicianMonitorHUD ──────────────────────────
        var guids = AssetDatabase.FindAssets("TechnicianMonitorHUD t:Prefab");
        if (guids.Length == 0)
        {
            msg = "No se encontró 'TechnicianMonitorHUD.prefab' en el proyecto.";
            return false;
        }
        string path = AssetDatabase.GUIDToAssetPath(guids[0]);

        // ── Editar el contenido del prefab de forma segura (no requiere abrirlo) ─
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var ide = root.GetComponentInChildren<ArduinoIDEUI>(true);
            if (ide == null)
            {
                msg = $"El prefab '{path}' no contiene un componente ArduinoIDEUI.";
                return false;
            }

            if (ide.btnComprobarCircuito != null)
            {
                msg = $"Ya estaba cableado a '{ide.btnComprobarCircuito.name}'. Nada que hacer.";
                return true;
            }

            if (ide.btnUploadCode == null)
            {
                msg = "No hay 'btnUploadCode' de referencia para clonar el layout. " +
                      "Cablea primero el botón de subir código, o crea el botón a mano y asígnalo.";
                return false;
            }

            // ── Clonar el botón "Subir código" como base ──────────────────────
            var src   = ide.btnUploadCode.gameObject;
            var clone = Object.Instantiate(src, src.transform.parent);
            clone.name = "Btn_ComprobarCircuito";

            // Desplazar (si el padre no tiene LayoutGroup; si lo tiene, se reposiciona solo).
            var rt = clone.GetComponent<RectTransform>();
            if (rt != null) rt.anchoredPosition += new Vector2(0f, -42f);

            // Quitar listeners persistentes heredados (no debe subir código).
            var btn = clone.GetComponent<Button>();
            for (int i = btn.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(btn.onClick, i);

            // Etiqueta del botón.
            var label = clone.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = "COMPROBAR (F5)";

            // Cablear la referencia.
            ide.btnComprobarCircuito = btn;

            // ── Guardar el prefab ─────────────────────────────────────────────
            PrefabUtility.SaveAsPrefabAsset(root, path);

            msg = $"Botón 'COMPROBAR (F5)' ('{clone.name}') creado y cableado en " +
                  $"ArduinoIDEUI.btnComprobarCircuito de '{path}'.";
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
#endif
