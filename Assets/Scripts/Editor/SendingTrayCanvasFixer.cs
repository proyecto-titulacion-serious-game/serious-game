using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Herramienta de editor para reparar automáticamente las referencias del Canvas
/// del ComponentSendingTray en la escena Técnico.
///
/// USO: Tools → TITA → Bandeja → Reparar Canvas SendingTray
/// </summary>
public static class SendingTrayCanvasFixer
{
    [MenuItem("Tools/TITA/Bandeja/Reparar Canvas SendingTray")]
    static void Reparar()
    {
        var tray = Object.FindAnyObjectByType<ComponentSendingTray>(FindObjectsInactive.Include);
        if (tray == null)
        {
            EditorUtility.DisplayDialog("Error",
                "ComponentSendingTray no encontrado.\n" +
                "Abre la escena Tecnico.unity y vuelve a ejecutar.", "OK");
            return;
        }

        Undo.RecordObject(tray, "Reparar ComponentSendingTray canvas");
        int fixes = 0;

        // ── Componente único por tipo ──────────────────────────────────────────

        if (tray.inputValor == null)
        {
            var found = tray.GetComponentInChildren<TMP_InputField>(true);
            if (found != null) { tray.inputValor = found; fixes++; Log($"inputValor → {found.name}"); }
            else Warn("TMP_InputField no encontrado en hijos del Tray. Créalo manualmente.");
        }

        if (tray.togglePolaridad == null)
        {
            var found = tray.GetComponentInChildren<Toggle>(true);
            if (found != null) { tray.togglePolaridad = found; fixes++; Log($"togglePolaridad → {found.name}"); }
            else Warn("Toggle no encontrado. Créalo manualmente en Tray_Canvas.");
        }

        if (tray.btnEnviar == null)
        {
            var found = tray.GetComponentInChildren<Button>(true);
            if (found != null) { tray.btnEnviar = found; fixes++; Log($"btnEnviar → {found.name}"); }
            else Warn("Button no encontrado. Créalo manualmente en Tray_Canvas.");
        }

        // ── TMP_Text: distinción por nombre ────────────────────────────────────
        var allTexts = tray.GetComponentsInChildren<TMP_Text>(true);
        foreach (var t in allTexts)
        {
            // Ignorar textos que son hijos de un Button o TMP_InputField
            if (t.GetComponentInParent<TMP_InputField>() != null) continue;
            if (t.GetComponentInParent<Button>()         != null) continue;
            if (t.GetComponentInParent<Toggle>()         != null) continue;

            string n = t.gameObject.name.ToLowerInvariant();

            if (tray.txtComponenteEnBandeja == null &&
                (n.Contains("nombre") || n.Contains("componente") || n.Contains("bandeja")))
            { tray.txtComponenteEnBandeja = t; fixes++; Log($"txtComponenteEnBandeja → {t.name}"); continue; }

            if (tray.txtDescripcion == null && n.Contains("desc"))
            { tray.txtDescripcion = t; fixes++; Log($"txtDescripcion → {t.name}"); continue; }

            if (tray.txtInputLabel == null &&
                (n.Contains("inputlabel") || n.Contains("input_label") ||
                 (n.Contains("label") && !n.Contains("toggle") && !n.Contains("toggle"))))
            { tray.txtInputLabel = t; fixes++; Log($"txtInputLabel → {t.name}"); continue; }

            if (tray.txtToggleLabel == null &&
                (n.Contains("togglelabel") || n.Contains("toggle_label") || n.Contains("polari")))
            { tray.txtToggleLabel = t; fixes++; Log($"txtToggleLabel → {t.name}"); continue; }

            if (tray.txtFeedback == null &&
                (n.Contains("feedback") || n.Contains("estado") || n.Contains("status") || n.Contains("result")))
            { tray.txtFeedback = t; fixes++; Log($"txtFeedback → {t.name}"); continue; }
        }

        // ── Referencias de sistema ─────────────────────────────────────────────

        if (tray.delivery == null)
        {
            var cds = Object.FindAnyObjectByType<ComponentDeliverySystem>(FindObjectsInactive.Include);
            if (cds != null) { tray.delivery = cds; fixes++; Log($"delivery → {cds.name}"); }
            else Warn("ComponentDeliverySystem no encontrado en la escena.");
        }

        if (tray.gameManager == null)
        {
            var gm = Object.FindAnyObjectByType<GameManager>(FindObjectsInactive.Include);
            if (gm != null) { tray.gameManager = gm; fixes++; Log($"gameManager → {gm.name}"); }
        }

        if (tray.technicianActions == null)
        {
            var ta = Object.FindAnyObjectByType<TechnicianActions>(FindObjectsInactive.Include);
            if (ta != null) { tray.technicianActions = ta; fixes++; Log($"technicianActions → {ta.name}"); }
        }

        // ── Reparar DeskComponents: asignar el tray si falta ──────────────────
        var deskComponents = Object.FindObjectsByType<DeskComponent>(FindObjectsInactive.Include);
        foreach (var dc in deskComponents)
        {
            if (dc.tray == null)
            {
                Undo.RecordObject(dc, "Asignar tray a DeskComponent");
                dc.tray = tray;
                EditorUtility.SetDirty(dc);
                fixes++;
                Log($"DeskComponent '{dc.name}' → tray asignado");
            }
        }

        // ── Verificar componentType de DeskComponents ─────────────────────────
        foreach (var dc in deskComponents)
        {
            string n = dc.gameObject.name.ToLowerInvariant();
            ComponentType expected = ComponentType.Resistor;

            if      (n.Contains("led"))        expected = ComponentType.LED;
            else if (n.Contains("cap"))        expected = ComponentType.Capacitor;
            else if (n.Contains("arduino") || n.Contains("pin")) expected = ComponentType.ArduinoPin;
            else                               expected = ComponentType.Resistor;

            if (dc.componentType != expected)
            {
                Undo.RecordObject(dc, "Corregir componentType");
                Debug.LogWarning($"[Tray Fixer] '{dc.name}' tenía componentType={dc.componentType}, " +
                                 $"corregido a {expected} según nombre. " +
                                 $"Verifica manualmente si el nombre no indica el tipo.");
                dc.componentType = expected;
                EditorUtility.SetDirty(dc);
                fixes++;
            }
        }

        // ── Canvas WorldSpace: asegurar GraphicRaycaster ──────────────────────
        var canvases = tray.GetComponentsInChildren<Canvas>(true);
        foreach (var c in canvases)
        {
            if (c.renderMode == RenderMode.WorldSpace && c.GetComponent<GraphicRaycaster>() == null)
            {
                Undo.AddComponent<GraphicRaycaster>(c.gameObject);
                fixes++;
                Log($"GraphicRaycaster añadido a Canvas '{c.name}'");
            }
        }

        EditorUtility.SetDirty(tray);
        EditorSceneManager.MarkSceneDirty(tray.gameObject.scene);

        string summary = fixes > 0
            ? $"{fixes} referencias reparadas.\nRevisa la Consola para el detalle completo.\n\n" +
              "IMPORTANTE: Verifica manualmente en el Inspector que los TMP_Text " +
              "quedaron asignados a los campos correctos, ya que la detección es por nombre."
            : "Todas las referencias ya estaban asignadas correctamente.";

        EditorUtility.DisplayDialog("SendingTray Canvas Fixer", summary, "OK");
    }

    static void Log (string msg) => Debug.Log ($"[Tray Fixer] {msg}");
    static void Warn(string msg) => Debug.LogWarning($"[Tray Fixer] {msg}");
}
