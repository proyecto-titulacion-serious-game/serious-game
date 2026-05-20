using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Configura el Canvas del manual_Overlay en modo World Space.
/// Menú: TITA → Setup Manual Overlay (World Space)
/// </summary>
public class ManualOverlaySetupTool : EditorWindow
{
    // ─────────────────────────────────────────────
    //  Parámetros configurables
    // ─────────────────────────────────────────────

    GameObject _targetCanvas;

    // Dimensiones del Canvas (unidades UI)
    float _canvasWidth  = 900f;
    float _canvasHeight = 650f;

    // Escala World Space: 0.001 → 1 unidad UI = 1 mm real
    float _canvasScale  = 0.001f;

    // Transform en el mundo
    Vector3 _worldPosition = new Vector3(0f, 1.35f, 0.4f);
    Vector3 _worldRotation = new Vector3(0f, 0f, 0f);

    // Calidad de texto TMP en World Space
    float _dynamicPixelsPerUnit = 10f;

    // Flags
    bool _assignEventCamera = true;
    bool _showInfo          = true;

    // ─────────────────────────────────────────────
    //  Apertura
    // ─────────────────────────────────────────────

    [MenuItem("TITA/Setup Manual Overlay (World Space)")]
    static void ShowWindow()
    {
        var w = GetWindow<ManualOverlaySetupTool>("Manual Overlay Setup");
        w.minSize = new Vector2(360, 460);
    }

    // ─────────────────────────────────────────────
    //  GUI
    // ─────────────────────────────────────────────

    void OnGUI()
    {
        GUILayout.Label("Manual Overlay — Configuración World Space", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        // ── Canvas objetivo ──────────────────────
        EditorGUILayout.LabelField("Canvas objetivo", EditorStyles.miniBoldLabel);
        _targetCanvas = (GameObject)EditorGUILayout.ObjectField(
            "manual_Overlay", _targetCanvas, typeof(GameObject), true);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Auto-buscar en escena"))
                AutoFindCanvas();
        }

        EditorGUILayout.Space(6);

        // ── Dimensiones ──────────────────────────
        EditorGUILayout.LabelField("Dimensiones del Canvas", EditorStyles.miniBoldLabel);
        _canvasWidth  = EditorGUILayout.FloatField("Ancho  (unidades UI)", _canvasWidth);
        _canvasHeight = EditorGUILayout.FloatField("Alto   (unidades UI)", _canvasHeight);
        _canvasScale  = EditorGUILayout.FloatField("Escala World Space",   _canvasScale);

        float realW = _canvasWidth  * _canvasScale;
        float realH = _canvasHeight * _canvasScale;
        EditorGUILayout.HelpBox(
            $"Tamaño real en el mundo: {realW:F2} m × {realH:F2} m\n" +
            $"(1 unidad UI = {_canvasScale * 1000:F1} mm)",
            MessageType.Info);

        // Presets
        EditorGUILayout.LabelField("Presets rápidos", EditorStyles.miniLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Documento (60×45 cm)"))  SetPreset(600, 450, 0.001f);
            if (GUILayout.Button("Pantalla  (90×65 cm)"))  SetPreset(900, 650, 0.001f);
            if (GUILayout.Button("Pizarra   (120×80 cm)")) SetPreset(600, 400, 0.002f);
        }

        EditorGUILayout.Space(6);

        // ── Transform ────────────────────────────
        EditorGUILayout.LabelField("Posición y rotación en la escena", EditorStyles.miniBoldLabel);
        _worldPosition = EditorGUILayout.Vector3Field("Posición (m)", _worldPosition);
        _worldRotation = EditorGUILayout.Vector3Field("Rotación (°)", _worldRotation);

        EditorGUILayout.HelpBox(
            "Posición sugerida para mesa del Técnico:\n" +
            "• Sobre la mesa y ligeramente inclinado: Y≈1.35, Z≈0.4, RotX≈-15\n" +
            "• Suspendido frente al jugador:           Y≈1.5, Z≈0.8, RotX≈0",
            MessageType.None);

        EditorGUILayout.Space(4);

        // ── Opciones ─────────────────────────────
        _assignEventCamera     = EditorGUILayout.Toggle("Asignar Event Camera (Main Camera)", _assignEventCamera);
        _dynamicPixelsPerUnit  = EditorGUILayout.FloatField("DynamicPixelsPerUnit (TMP)",     _dynamicPixelsPerUnit);

        EditorGUILayout.Space(8);

        // ── Acciones ─────────────────────────────
        GUI.backgroundColor = new Color(0.4f, 0.85f, 0.4f);
        if (GUILayout.Button("✓  Aplicar configuración completa", GUILayout.Height(34)))
            ApplyFullSetup();

        GUI.backgroundColor = Color.white;
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Solo Transform"))   ApplyTransformOnly();
            if (GUILayout.Button("Solo Dimensiones")) ApplyDimensionsOnly();
        }

        // ── Info del canvas actual ────────────────
        EditorGUILayout.Space(6);
        _showInfo = EditorGUILayout.Foldout(_showInfo, "Estado actual del Canvas");
        if (_showInfo) DrawCanvasInfo();
    }

    // ─────────────────────────────────────────────
    //  Presets
    // ─────────────────────────────────────────────

    void SetPreset(float w, float h, float s)
    {
        _canvasWidth  = w;
        _canvasHeight = h;
        _canvasScale  = s;
    }

    // ─────────────────────────────────────────────
    //  Auto-búsqueda
    // ─────────────────────────────────────────────

    void AutoFindCanvas()
    {
        // 1. ManualScroll.manualOverlay
        var scroll = FindAnyObjectByType<ManualScroll>();
        if (scroll != null && scroll.manualOverlay != null)
        { _targetCanvas = scroll.manualOverlay; Repaint(); return; }

        // 2. ManualBookOpener.manualOverlay
        var opener = FindAnyObjectByType<ManualBookOpener>();
        if (opener != null && opener.manualOverlay != null)
        { _targetCanvas = opener.manualOverlay; Repaint(); return; }

        // 3. Por nombre exacto
        foreach (string name in new[] { "manual_Overlay", "Manual_Overlay", "ManualOverlay", "Manual Overlay" })
        {
            var go = GameObject.Find(name);
            if (go != null) { _targetCanvas = go; Repaint(); return; }
        }

        // 4. Canvas que contenga TechnicianManualDisplay
        var display = FindAnyObjectByType<TechnicianManualDisplay>();
        if (display != null)
        {
            var c = display.GetComponentInParent<Canvas>();
            _targetCanvas = c != null ? c.gameObject : display.gameObject;
            Repaint();
            return;
        }

        EditorUtility.DisplayDialog("No encontrado",
            "No se encontró el Canvas automáticamente.\n" +
            "Arrástralo manualmente al campo 'manual_Overlay'.", "OK");
    }

    // ─────────────────────────────────────────────
    //  Aplicar configuración completa
    // ─────────────────────────────────────────────

    void ApplyFullSetup()
    {
        if (!ValidateTarget()) return;

        Undo.RegisterFullObjectHierarchyUndo(_targetCanvas, "Setup Manual Overlay World Space");

        // Canvas component
        var canvas = EnsureComponent<Canvas>(_targetCanvas);
        canvas.renderMode = RenderMode.WorldSpace;
        if (_assignEventCamera && Camera.main != null)
            canvas.worldCamera = Camera.main;

        // RectTransform — tamaño
        var rt = _targetCanvas.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.sizeDelta = new Vector2(_canvasWidth, _canvasHeight);
            rt.pivot     = new Vector2(0.5f, 0.5f);
        }

        // Transform — escala y posición
        var t = _targetCanvas.transform;
        t.position    = _worldPosition;
        t.eulerAngles = _worldRotation;
        t.localScale  = Vector3.one * _canvasScale;

        // CanvasScaler
        var scaler = EnsureComponent<CanvasScaler>(_targetCanvas);
        scaler.dynamicPixelsPerUnit = _dynamicPixelsPerUnit;

        // GraphicRaycaster (necesario para botones en World Space)
        EnsureComponent<GraphicRaycaster>(_targetCanvas);

        EditorUtility.SetDirty(_targetCanvas);
        Debug.Log($"[ManualOverlaySetup] Canvas '{_targetCanvas.name}' configurado: " +
                  $"{_canvasWidth}×{_canvasHeight} UI  |  escala {_canvasScale}  |  " +
                  $"tamaño real {_canvasWidth * _canvasScale:F2}m × {_canvasHeight * _canvasScale:F2}m");

        EditorUtility.DisplayDialog("¡Configuración aplicada!",
            $"Canvas: {_targetCanvas.name}\n" +
            $"Modo: World Space\n" +
            $"Tamaño real: {_canvasWidth * _canvasScale:F2} m × {_canvasHeight * _canvasScale:F2} m\n" +
            $"Event Camera: {(canvas.worldCamera != null ? canvas.worldCamera.name : "⚠ no asignada")}\n\n" +
            "Ajusta la posición arrastrando el Canvas en la escena.", "OK");
    }

    void ApplyTransformOnly()
    {
        if (!ValidateTarget()) return;
        Undo.RecordObject(_targetCanvas.transform, "Move Manual Overlay");
        var t = _targetCanvas.transform;
        t.position    = _worldPosition;
        t.eulerAngles = _worldRotation;
        t.localScale  = Vector3.one * _canvasScale;
        EditorUtility.SetDirty(_targetCanvas);
    }

    void ApplyDimensionsOnly()
    {
        if (!ValidateTarget()) return;
        var rt = _targetCanvas.GetComponent<RectTransform>();
        if (rt == null) { Debug.LogWarning("Sin RectTransform."); return; }
        Undo.RecordObject(rt, "Resize Manual Overlay");
        rt.sizeDelta = new Vector2(_canvasWidth, _canvasHeight);
        EditorUtility.SetDirty(_targetCanvas);
    }

    // ─────────────────────────────────────────────
    //  Info del canvas actual
    // ─────────────────────────────────────────────

    void DrawCanvasInfo()
    {
        if (_targetCanvas == null)
        { EditorGUILayout.HelpBox("Ningún Canvas seleccionado.", MessageType.None); return; }

        var canvas = _targetCanvas.GetComponent<Canvas>();
        var rt     = _targetCanvas.GetComponent<RectTransform>();

        if (canvas == null)
        { EditorGUILayout.HelpBox("El objeto no tiene componente Canvas.", MessageType.Warning); return; }

        float realW = rt != null ? rt.sizeDelta.x * _targetCanvas.transform.localScale.x : 0f;
        float realH = rt != null ? rt.sizeDelta.y * _targetCanvas.transform.localScale.y : 0f;

        string modeIcon = canvas.renderMode == RenderMode.WorldSpace ? "✓" : "⚠";
        string camInfo  = canvas.worldCamera != null ? canvas.worldCamera.name : "NO ASIGNADA ← necesario";

        EditorGUILayout.HelpBox(
            $"Render Mode: {modeIcon} {canvas.renderMode}\n" +
            $"Tamaño UI:   {(rt != null ? rt.sizeDelta.x : 0)}×{(rt != null ? rt.sizeDelta.y : 0)}\n" +
            $"Escala:      {_targetCanvas.transform.localScale.x:F4}\n" +
            $"Tamaño real: {realW:F2} m × {realH:F2} m\n" +
            $"Event Camera: {camInfo}\n" +
            $"Posición:    {_targetCanvas.transform.position}",
            canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera != null
                ? MessageType.Info : MessageType.Warning);
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────

    bool ValidateTarget()
    {
        if (_targetCanvas != null) return true;
        EditorUtility.DisplayDialog("Sin Canvas", "Asigna o auto-busca el Canvas primero.", "OK");
        return false;
    }

    T EnsureComponent<T>(GameObject go) where T : Component
    {
        var c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }
}
