#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Punto de entrada para batch mode: aplica el modelo Arduino OBJ y ejecuta el Auto-Setup del Reto 4.
/// Invocado con: Unity -batchmode -executeMethod ArduinoBatchSetup.Run
/// </summary>
public static class ArduinoBatchSetup
{
    const string MODEL_PATH =
        "Assets/Art/Arduino_Modelo/Meshy_AI_Arduino_Uno_Board_Ill_0529053438_texture.obj";

    const float PCB_W = 0.0686f;
    const float PCB_D = 0.0534f;
    const float PCB_H = 0.0016f;
    const float PIN_S = 0.00254f;
    const float HDR_H = 0.0085f;

    [MenuItem("Tools/TITA/[Batch] Aplicar Modelo Arduino + Auto-Setup Reto4")]
    public static void Run()
    {
        Debug.Log("[ArduinoBatchSetup] Iniciando...");

        // ── Cargar el modelo OBJ ──────────────────────────────────────────
        var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PATH);
        if (modelPrefab == null)
        {
            Debug.LogError($"[ArduinoBatchSetup] Modelo no encontrado en: {MODEL_PATH}");
            if (Application.isBatchMode) EditorApplication.Exit(1);
            return;
        }

        // ── Buscar o crear GO del Arduino ─────────────────────────────────
        GameObject arduinoGO = null;
        var core = Object.FindAnyObjectByType<ArduinoCore>();
        if (core != null)
            arduinoGO = core.gameObject;
        else
        {
            arduinoGO = new GameObject("Arduino_VR");
            arduinoGO.AddComponent<ArduinoCore>();
            arduinoGO.AddComponent<ArduinoNetworkBridge>();
            Debug.Log("[ArduinoBatchSetup] Arduino_VR creado.");
        }

        // ── Eliminar modelo anterior ──────────────────────────────────────
        var oldModel = arduinoGO.transform.Find("[Arduino_Model]");
        if (oldModel != null) Object.DestroyImmediate(oldModel.gameObject);

        // ── Instanciar el modelo ──────────────────────────────────────────
        var modelGO = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, arduinoGO.transform);
        modelGO.name = "[Arduino_Model]";
        modelGO.transform.localPosition = Vector3.zero;
        modelGO.transform.localRotation = Quaternion.identity;

        // Auto-escalar al tamaño real (68.6 mm)
        var renderers = modelGO.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            float maxDim = Mathf.Max(bounds.size.x, bounds.size.z);
            if (maxDim > 0.001f)
                modelGO.transform.localScale = Vector3.one * (PCB_W / maxDim);
        }

        // ── Reubicar nodos eléctricos ─────────────────────────────────────
        float pinTopZ = -0.0025f;
        float pinBotZ = -PCB_D - 0.0025f;
        Vector3 posP13 = new Vector3(PCB_W - PIN_S * 0.5f,           HDR_H + PCB_H, pinTopZ);
        Vector3 posGND = new Vector3(PCB_W - PIN_S * 0.5f - PIN_S*7, HDR_H + PCB_H, pinTopZ);
        Vector3 posA0  = new Vector3(PCB_W * 0.90f,                   HDR_H + PCB_H, pinBotZ);

        foreach (Transform child in arduinoGO.transform)
        {
            if (child.name == "Nodo_P13") child.localPosition = posP13;
            if (child.name == "Nodo_GND") child.localPosition = posGND;
            if (child.name == "Nodo_A0")  child.localPosition = posA0;
        }

        // ── Guardar escena ────────────────────────────────────────────────
        EditorUtility.SetDirty(arduinoGO);
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[ArduinoBatchSetup] Modelo Arduino aplicado en '{arduinoGO.name}'. ¡Listo!");

        if (Application.isBatchMode) EditorApplication.Exit(0);
    }
}
#endif
