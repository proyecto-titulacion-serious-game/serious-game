#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Adjunta TechnicianMonitorHUD al mesh físico del monitor del Técnico.
///
/// Pasos automáticos:
///   1. Localiza el GO "Monitor" (Japan Office PC prefab) o el seleccionado.
///   2. Hace TechnicianMonitorHUD hijo de ese GO.
///   3. Posiciona el canvas justo enfrente de la pantalla.
///   4. Orienta el canvas hacia la Pc_Camera.
///   5. Añade PCMonitorInteract + BoxCollider al GO del monitor si faltan.
///   6. Conecta PCMonitorInteract.monitorHUD al canvas.
///
/// Menú: Tools → TITA → Adjuntar HUD al Monitor Físico
/// </summary>
public class TechnicianMonitorAttacher : EditorWindow
{
    private GameObject _monitorGO;
    private GameObject _hudGO;

    // Offset local delante de la pantalla (en coordenadas del Monitor GO)
    private Vector3 _localOffset   = new Vector3(0f, 0.05f, -0.05f);
    private float   _canvasScale   = 1f / 1600f;

    [MenuItem("Tools/TITA/Adjuntar HUD al Monitor Físico")]
    static void ShowWindow()
        => GetWindow<TechnicianMonitorAttacher>("HUD → Monitor");

    void OnEnable() => AutoFind();

    void AutoFind()
    {
        // Buscar el GO "Monitor" del PC Japan Office por nombre
        if (_monitorGO == null)
            _monitorGO = FindGOByName("Monitor", "PC_Monitor", "monitor_screen", "Monitor_Screen");

        // Buscar TechnicianMonitorHUD por componente
        if (_hudGO == null)
        {
            var hud = FindAnyObjectByType<TechnicianHUDController>();
            if (hud != null) _hudGO = hud.gameObject;
        }
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Adjuntar HUD al Monitor Físico", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        // Monitor mesh
        EditorGUI.BeginChangeCheck();
        _monitorGO = (GameObject)EditorGUILayout.ObjectField(
            "Mesh del Monitor", _monitorGO, typeof(GameObject), true);
        if (EditorGUI.EndChangeCheck() && _monitorGO != null)
            AutoFindFromSelection();

        // HUD canvas
        _hudGO = (GameObject)EditorGUILayout.ObjectField(
            "TechnicianMonitorHUD", _hudGO, typeof(GameObject), true);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Ajustes de posición", EditorStyles.miniBoldLabel);
        _localOffset = EditorGUILayout.Vector3Field("Offset local (frente pantalla)", _localOffset);
        _canvasScale = EditorGUILayout.FloatField("Escala canvas (1/N)", _canvasScale);

        EditorGUILayout.Space(4);
        // Status
        bool monitorOk = _monitorGO != null;
        bool hudOk     = _hudGO     != null;

        DrawStatus("Monitor mesh encontrado", monitorOk);
        DrawStatus("TechnicianMonitorHUD encontrado", hudOk);

        // PCMonitorInteract
        bool hasPMI = monitorOk && _monitorGO.GetComponent<PCMonitorInteract>() != null;
        DrawStatus("PCMonitorInteract en monitor", hasPMI);

        EditorGUILayout.Space(8);

        using (new EditorGUI.DisabledScope(!monitorOk || !hudOk))
        {
            if (GUILayout.Button("Adjuntar HUD al Monitor", GUILayout.Height(36)))
                Attach();
        }

        if (!monitorOk)
            EditorGUILayout.HelpBox(
                "Arrastra el mesh físico del monitor (GameObject 'Monitor' del prefab PC Japan Office).",
                MessageType.Warning);
        if (!hudOk)
            EditorGUILayout.HelpBox(
                "No se encontró TechnicianMonitorHUD en la escena. Instancia el prefab primero " +
                "o ejecuta Tools → TITA → Crear HUD Monitor Técnico.",
                MessageType.Warning);

        EditorGUILayout.Space(6);
        if (GUILayout.Button("Auto-detectar ambos", GUILayout.Height(26)))
            AutoFind();

        if (GUILayout.Button("Usar selección como Monitor", GUILayout.Height(26)))
        {
            if (Selection.activeGameObject != null)
                _monitorGO = Selection.activeGameObject;
        }
    }

    void Attach()
    {
        var log = new System.Text.StringBuilder();
        int done = 0;

        // ── 1. Parentar HUD al monitor ──────────────────────────────────
        Undo.SetTransformParent(_hudGO.transform, _monitorGO.transform, "Parent HUD to Monitor");
        log.AppendLine("✅ TechnicianMonitorHUD → hijo de " + _monitorGO.name);
        done++;

        // ── 2. Posicionar delante de la pantalla ────────────────────────
        var rt = _hudGO.GetComponent<RectTransform>() ?? _hudGO.transform as RectTransform;
        if (rt != null)
        {
            Undo.RecordObject(rt, "Posicionar HUD");
            rt.localPosition = _localOffset;
            rt.localRotation = Quaternion.identity;
            rt.localScale    = Vector3.one * _canvasScale;
        }
        else
        {
            Undo.RecordObject(_hudGO.transform, "Posicionar HUD");
            _hudGO.transform.localPosition = _localOffset;
            _hudGO.transform.localRotation = Quaternion.identity;
            _hudGO.transform.localScale    = Vector3.one * _canvasScale;
        }
        log.AppendLine($"✅ Canvas → localPos={_localOffset}, scale={_canvasScale:F6}");
        done++;

        // ── 3. Orientar canvas hacia Pc_Camera ──────────────────────────
        var pcCam = FindCameraByName("Pc_Camera", "PC_Camera", "pcCamera");
        if (pcCam != null)
        {
            Undo.RecordObject(_hudGO.transform, "Orientar HUD hacia cámara");
            _hudGO.transform.rotation = Quaternion.LookRotation(
                _hudGO.transform.position - pcCam.transform.position);
            log.AppendLine("✅ Canvas orientado hacia " + pcCam.name);
            done++;

            // Asignar worldCamera al Canvas
            var canvas = _hudGO.GetComponent<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
            {
                Undo.RecordObject(canvas, "Asignar worldCamera");
                canvas.worldCamera = pcCam;
                EditorUtility.SetDirty(canvas);
                log.AppendLine("✅ Canvas.worldCamera → " + pcCam.name);
                done++;
            }
        }
        else
            log.AppendLine("⚠  Pc_Camera no encontrada — orienta el canvas manualmente.");

        // ── 4. PCMonitorInteract + Collider en el monitor ───────────────
        // NOTA: monitorHUD es [NonSerialized] — no se asigna aquí para
        // evitar cross-scene references (Monitor en NoonA, HUD en Tecnico).
        // Se auto-asigna en PCMonitorInteract.Start() via FindAnyObjectByType.
        if (_monitorGO.GetComponent<PCMonitorInteract>() == null)
        {
            if (_monitorGO.GetComponent<Collider>() == null)
            {
                Undo.AddComponent<BoxCollider>(_monitorGO);
                log.AppendLine("✅ BoxCollider añadido a " + _monitorGO.name);
                done++;
            }
            var pmi = Undo.AddComponent<PCMonitorInteract>(_monitorGO);
            if (pcCam != null) pmi.pcCamera = pcCam;
            EditorUtility.SetDirty(pmi);
            log.AppendLine("✅ PCMonitorInteract añadido (monitorHUD → runtime auto-find).");
            done++;
        }
        else
        {
            var pmi = _monitorGO.GetComponent<PCMonitorInteract>();
            if (pcCam != null && pmi.pcCamera == null)
            {
                Undo.RecordObject(pmi, "Asignar pcCamera");
                pmi.pcCamera = pcCam;
                EditorUtility.SetDirty(pmi);
            }
            log.AppendLine("✓  PCMonitorInteract ya existe (monitorHUD → runtime auto-find).");
            done++;
        }

        EditorUtility.SetDirty(_monitorGO);
        EditorUtility.SetDirty(_hudGO);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog(
            $"HUD adjuntado al Monitor ({done} acciones)",
            log.ToString() +
            "\n\nSi el HUD no queda perfectamente alineado, ajusta:\n" +
            "  • 'Offset local' en esta ventana\n" +
            "  • localPosition/localRotation en el Inspector de TechnicianMonitorHUD",
            "OK");

        Debug.Log("[TechnicianMonitorAttacher]\n" + log);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────
    void AutoFindFromSelection()
    {
        if (_monitorGO == null || _hudGO != null) return;
        var hud = FindAnyObjectByType<TechnicianHUDController>();
        if (hud != null) _hudGO = hud.gameObject;
    }

    static GameObject FindGOByName(params string[] names)
    {
        foreach (var name in names)
        {
            var go = GameObject.Find(name);
            if (go != null) return go;
        }
        foreach (var go in FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            foreach (var name in names)
                if (go.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                    return go;
        return null;
    }

    static Camera FindCameraByName(params string[] names)
    {
        foreach (var cam in FindObjectsByType<Camera>(FindObjectsInactive.Include))
            foreach (var n in names)
                if (cam.name.Equals(n, System.StringComparison.OrdinalIgnoreCase))
                    return cam;
        return null;
    }

    static void DrawStatus(string label, bool ok)
    {
        var style = new GUIStyle(EditorStyles.label)
            { normal = { textColor = ok ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.9f, 0.4f, 0.2f) } };
        EditorGUILayout.LabelField((ok ? "✅ " : "⚠  ") + label, style);
    }
}
#endif
