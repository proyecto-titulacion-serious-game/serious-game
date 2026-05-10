using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Genera el botón "Reconectar Cable" en el Canvas del Técnico y lo conecta
/// a TechnicianActions.FixLooseCable().
///
/// Uso: Tools → TITA → Generar Botón Reconectar Cable
/// </summary>
public static class LooseCableButtonGenerator
{
    private const string MENU = "Tools/TITA/Generar Botón Reconectar Cable (Reto 4)";

    [MenuItem(MENU)]
    public static void Generate()
    {
        // ── 1. Buscar TechnicianActions en la escena ──────────────────────
        var actions = Object.FindFirstObjectByType<TechnicianActions>();
        if (actions == null)
        {
            EditorUtility.DisplayDialog("Error",
                "No se encontró TechnicianActions en la escena activa.\n\n" +
                "Abre la escena del Técnico (Tecnico.unity o IntegratedDemo.unity) " +
                "y vuelve a ejecutar el menú.", "OK");
            return;
        }

        // ── 2. Buscar un Canvas en la escena para alojar el botón ─────────
        Canvas targetCanvas = FindTechnicianCanvas(actions);
        if (targetCanvas == null)
        {
            EditorUtility.DisplayDialog("Error",
                "No se encontró ningún Canvas en la jerarquía del Técnico.\n\n" +
                "Asegúrate de que la estación del Técnico tiene un Canvas " +
                "(TechnicianWorkstation → DiagnosticPanel, o similar).", "OK");
            return;
        }

        // ── 3. Evitar duplicados ──────────────────────────────────────────
        Transform existing = targetCanvas.transform.Find("Btn_ReconectarCable");
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("Duplicado",
                    "Ya existe un botón 'Btn_ReconectarCable' en ese Canvas.\n¿Recrearlo?",
                    "Sí, recrear", "Cancelar"))
                return;
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        // ── 4. Crear el GameObject del botón ─────────────────────────────
        GameObject btnGO = new GameObject("Btn_ReconectarCable");
        Undo.RegisterCreatedObjectUndo(btnGO, "Crear Botón Reconectar Cable");
        btnGO.transform.SetParent(targetCanvas.transform, false);

        // RectTransform
        var rect = btnGO.AddComponent<RectTransform>();
        rect.sizeDelta        = new Vector2(220f, 50f);
        rect.anchorMin        = new Vector2(0.5f, 0f);
        rect.anchorMax        = new Vector2(0.5f, 0f);
        rect.pivot            = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 20f);  // 20px desde abajo del canvas

        // Image (fondo del botón) — color naranja para Reto 4
        var img = btnGO.AddComponent<Image>();
        img.color = new Color(0.85f, 0.45f, 0.1f);

        // Button
        var btn = btnGO.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = new Color(1f, 0.6f, 0.2f);
        colors.pressedColor     = new Color(0.6f, 0.3f, 0.05f);
        btn.colors = colors;

        // ── 5. Texto del botón (TextMeshPro) ─────────────────────────────
        GameObject textGO = new GameObject("Label");
        textGO.transform.SetParent(btnGO.transform, false);

        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin        = Vector2.zero;
        textRect.anchorMax        = Vector2.one;
        textRect.offsetMin        = Vector2.zero;
        textRect.offsetMax        = Vector2.zero;

        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = "Reconectar Cable";
        tmp.fontSize  = 18f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;
        tmp.fontStyle = FontStyles.Bold;

        // ── 6. Conectar onClick → TechnicianActions.FixLooseCable ─────────
        UnityEventTools.AddPersistentListener(
            btn.onClick,
            actions.FixLooseCable);

        // ── 7. Marcar la escena como modificada ───────────────────────────
        EditorUtility.SetDirty(btnGO);
        UnityEngine.SceneManagement.Scene scene =
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);

        // ── 8. Seleccionar el botón creado en la jerarquía ───────────────
        Selection.activeGameObject = btnGO;
        EditorGUIUtility.PingObject(btnGO);

        Debug.Log($"[TITA] Botón 'Reconectar Cable' creado en {targetCanvas.name}. " +
                  $"onClick → TechnicianActions.FixLooseCable()");

        EditorUtility.DisplayDialog("Listo",
            $"Botón creado en:\n{GetPath(btnGO.transform)}\n\n" +
            "onClick está conectado a TechnicianActions.FixLooseCable().\n\n" +
            "Ajusta la posición en el Inspector si es necesario (RectTransform).",
            "OK");
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Busca el Canvas más cercano al Técnico.
    /// Prioridad: Canvas hijo de TechnicianWorkstation → cualquier Canvas en escena.
    /// </summary>
    static Canvas FindTechnicianCanvas(TechnicianActions actions)
    {
        // Buscar TechnicianWorkstation para encontrar el Canvas de diagnóstico
        var workstation = Object.FindFirstObjectByType<TechnicianWorkstation>();
        if (workstation != null)
        {
            var c = workstation.GetComponentInChildren<Canvas>(true);
            if (c != null) return c;
        }

        // Fallback: Canvas padre de TechnicianActions
        var parentCanvas = actions.GetComponentInParent<Canvas>();
        if (parentCanvas != null) return parentCanvas;

        // Fallback: primer Canvas en escena
        return Object.FindFirstObjectByType<Canvas>();
    }

    static string GetPath(Transform t)
    {
        if (t.parent == null) return t.name;
        return GetPath(t.parent) + "/" + t.name;
    }

    [MenuItem(MENU, true)]
    static bool Validate() =>
        UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().IsValid();
}
