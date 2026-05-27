using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public static class MapVRSetupTool
{
    // ─── Crear escena MapVR ─────────────────────────────────────────────────
    [MenuItem("Tools/TITA/1 – Crear escena MapVR")]
    static void CreateMapVRScene()
    {
        const string scenePath = "Assets/Scenes/MapVR.unity";

        // Limpiar escena mal ubicada si existe (creada en raíz por error)
        string wrongPath = "Assets/MapVR.unity";
        if (AssetDatabase.LoadMainAssetAtPath(wrongPath) != null)
        {
            AssetDatabase.DeleteAsset(wrongPath);
            Debug.Log("[MapVRSetupTool] Assets/MapVR.unity (ubicación incorrecta) eliminada.");
        }

        // Asegurar que Assets/Scenes/ existe
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");

        if (System.IO.File.Exists(System.IO.Path.Combine(
                System.IO.Directory.GetCurrentDirectory(), scenePath)))
        {
            bool overwrite = EditorUtility.DisplayDialog("MapVR ya existe",
                "Assets/Scenes/MapVR.unity ya existe. ¿Sobreescribir?", "Sí", "No");
            if (!overwrite) return;
        }

        // Crear escena vacía y guardarla primero
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, scenePath);

        // ── Luz direccional ────────────────────────────────────────────────
        GameObject lightGO = new GameObject("DirectionalLight");
        Light light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1f;
        lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // ── Cámara principal ────────────────────────────────────────────────
        GameObject camGO = new GameObject("Main Camera");
        Camera cam = camGO.AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.backgroundColor = new Color(0.1f, 0.12f, 0.18f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        camGO.transform.SetPositionAndRotation(
            new Vector3(0f, 8f, 0f),
            Quaternion.Euler(90f, 0f, 0f));

        // ── Canvas de mapa ──────────────────────────────────────────────────
        GameObject canvasGO = new GameObject("MapCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 1f;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();

        // Fondo semitransparente
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(canvasGO.transform, false);
        Image bg = bgGO.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.07f, 0.12f, 0.95f);
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

        // Título
        GameObject titleGO = new GameObject("TMP_Titulo");
        titleGO.transform.SetParent(canvasGO.transform, false);
        TextMeshProUGUI title = titleGO.AddComponent<TextMeshProUGUI>();
        title.text = "MAPA – TITA";
        title.fontSize = 48;
        title.alignment = TextAlignmentOptions.Center;
        title.color = Color.white;
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 0.85f);
        titleRT.anchorMax = new Vector2(1f, 1f);
        titleRT.offsetMin = titleRT.offsetMax = Vector2.zero;

        // Placeholder de mapa (imagen gris — reemplazar con textura real)
        GameObject mapImgGO = new GameObject("MapImage");
        mapImgGO.transform.SetParent(canvasGO.transform, false);
        Image mapImg = mapImgGO.AddComponent<Image>();
        mapImg.color = new Color(0.2f, 0.25f, 0.3f, 1f);
        RectTransform mapRT = mapImgGO.GetComponent<RectTransform>();
        mapRT.anchorMin = new Vector2(0.05f, 0.15f);
        mapRT.anchorMax = new Vector2(0.95f, 0.85f);
        mapRT.offsetMin = mapRT.offsetMax = Vector2.zero;

        // Etiqueta placeholder
        GameObject lblGO = new GameObject("MapLabel");
        lblGO.transform.SetParent(mapImgGO.transform, false);
        TextMeshProUGUI lbl = lblGO.AddComponent<TextMeshProUGUI>();
        lbl.text = "[ Textura del mapa aquí ]\nAsigna un Sprite en MapImage > Image > Source Image";
        lbl.fontSize = 22;
        lbl.alignment = TextAlignmentOptions.Center;
        lbl.color = new Color(0.7f, 0.7f, 0.7f);
        RectTransform lblRT = lblGO.GetComponent<RectTransform>();
        lblRT.anchorMin = Vector2.zero;
        lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;

        // Botón Volver
        GameObject btnGO = new GameObject("Button_Volver");
        btnGO.transform.SetParent(canvasGO.transform, false);
        Image btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(0.18f, 0.38f, 0.62f);
        Button btn = btnGO.AddComponent<Button>();
        RectTransform btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.35f, 0.01f);
        btnRT.anchorMax = new Vector2(0.65f, 0.1f);
        btnRT.offsetMin = btnRT.offsetMax = Vector2.zero;

        // Label del botón
        GameObject btnLblGO = new GameObject("Text");
        btnLblGO.transform.SetParent(btnGO.transform, false);
        TextMeshProUGUI btnLbl = btnLblGO.AddComponent<TextMeshProUGUI>();
        btnLbl.text = "← Volver al Workstation";
        btnLbl.fontSize = 22;
        btnLbl.alignment = TextAlignmentOptions.Center;
        btnLbl.color = Color.white;
        RectTransform btnLblRT = btnLblGO.GetComponent<RectTransform>();
        btnLblRT.anchorMin = Vector2.zero;
        btnLblRT.anchorMax = Vector2.one;
        btnLblRT.offsetMin = btnLblRT.offsetMax = Vector2.zero;

        // SceneLoader en el botón para cargar Tecnico al hacer clic
        SceneLoader loader = btnGO.AddComponent<SceneLoader>();
        loader.targetScene = "Tecnico";
        btn.onClick.AddListener(loader.Load);

        // ── EventSystem ────────────────────────────────────────────────────
        GameObject esGO = new GameObject("EventSystem");
        esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
#if ENABLE_INPUT_SYSTEM
        esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
#endif

        // ── Guardar escena ─────────────────────────────────────────────────
        EditorSceneManager.SaveScene(scene, scenePath);

        // Añadir a Build Settings si no está
        AddSceneToBuild(scenePath);

        Debug.Log("[MapVRSetupTool] MapVR.unity creada en " + scenePath);
        EditorUtility.DisplayDialog("Listo", "MapVR.unity creada.\nRevisa la etiqueta gris e ingresa tu textura de mapa.", "OK");
    }

    // ─── Agregar botón "Ver Mapa" en Tecnico.unity ─────────────────────────
    [MenuItem("Tools/TITA/2 – Agregar botón Ver Mapa en Tecnico")]
    static void AddMapButton()
    {
        // Asegurar que Tecnico está abierta
        Scene active = SceneManager.GetActiveScene();
        if (!active.name.Equals("Tecnico", System.StringComparison.OrdinalIgnoreCase))
        {
            EditorUtility.DisplayDialog("Error",
                "Abre Tecnico.unity primero (paso 1 del Setup Desk ya lo hace).", "OK");
            return;
        }

        // Buscar Tray_Canvas en la escena
        Canvas targetCanvas = null;
        foreach (var go in active.GetRootGameObjects())
        {
            targetCanvas = FindCanvasDeep(go.transform, "Tray_Canvas");
            if (targetCanvas != null) break;
        }

        if (targetCanvas == null)
        {
            EditorUtility.DisplayDialog("Error",
                "No se encontró 'Tray_Canvas' en la escena.\n" +
                "Ejecuta primero 'Setup Desk Kenney en Tecnico'.", "OK");
            return;
        }

        // Evitar duplicado
        if (FindDeep(targetCanvas.transform, "Button_VerMapa") != null)
        {
            EditorUtility.DisplayDialog("Info",
                "Button_VerMapa ya existe en Tray_Canvas.", "OK");
            return;
        }

        // ── Crear botón ────────────────────────────────────────────────────
        GameObject btnGO = new GameObject("Button_VerMapa");
        Undo.RegisterCreatedObjectUndo(btnGO, "Add Button_VerMapa");
        btnGO.transform.SetParent(targetCanvas.transform, false);

        Image btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(0.1f, 0.35f, 0.6f);
        Button btn = btnGO.AddComponent<Button>();

        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(0f, 0f);
        rt.pivot     = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(10f, 10f);
        rt.sizeDelta = new Vector2(180f, 50f);

        // Texto del botón
        GameObject lblGO = new GameObject("Text");
        lblGO.transform.SetParent(btnGO.transform, false);
        TextMeshProUGUI lbl = lblGO.AddComponent<TextMeshProUGUI>();
        lbl.text = "Ver Mapa";
        lbl.fontSize = 20;
        lbl.alignment = TextAlignmentOptions.Center;
        lbl.color = Color.white;
        RectTransform lblRT = lblGO.GetComponent<RectTransform>();
        lblRT.anchorMin = Vector2.zero;
        lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;

        // SceneLoader
        SceneLoader loader = btnGO.AddComponent<SceneLoader>();
        loader.targetScene = "MapVR";
        btn.onClick.AddListener(loader.Load);

        EditorSceneManager.MarkSceneDirty(active);
        Debug.Log("[MapVRSetupTool] Button_VerMapa añadido a Tray_Canvas. Guarda con Ctrl+S.");
        EditorUtility.DisplayDialog("Listo",
            "Button_VerMapa añadido a Tray_Canvas.\nGuarda la escena con Ctrl+S.", "OK");
    }

    // ─── Helpers ────────────────────────────────────────────────────────────

    static Canvas FindCanvasDeep(Transform root, string name)
    {
        foreach (var c in root.GetComponentsInChildren<Canvas>(true))
            if (c.name == name) return c;
        return null;
    }

    static Transform FindDeep(Transform root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    static void AddSceneToBuild(string scenePath)
    {
        var scenes = EditorBuildSettings.scenes;
        foreach (var s in scenes)
            if (s.path == scenePath) return;

        var newScenes = new EditorBuildSettingsScene[scenes.Length + 1];
        scenes.CopyTo(newScenes, 0);
        newScenes[scenes.Length] = new EditorBuildSettingsScene(scenePath, true);
        EditorBuildSettings.scenes = newScenes;
        Debug.Log("[MapVRSetupTool] MapVR.unity añadida a Build Settings.");
    }
}
