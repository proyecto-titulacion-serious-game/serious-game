#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Aplica el modelo 3D real del Arduino Uno (OBJ importado desde Meshy AI) al GO
/// que tiene ArduinoCore y reposiciona los nodos eléctricos a sus pines físicos.
///
/// El modelo OBJ se encuentra en:
///   Assets/Art/Arduino_Modelo/Meshy_AI_Arduino_Uno_Board_Ill_0529053438_texture.obj
///
/// Tools > TITA > Aplicar Modelo 3D Arduino Uno
/// </summary>
public static class ArduinoModelCreator
{
    const string MODEL_PATH =
        "Assets/Art/Arduino_Modelo/Meshy_AI_Arduino_Uno_Board_Ill_0529053438_texture.obj";

    // Dimensiones reales Arduino Uno para calcular posiciones de nodos (metros)
    const float PCB_W = 0.0686f;
    const float PCB_D = 0.0534f;
    const float PCB_H = 0.0016f;
    const float PIN_S = 0.00254f;
    const float HDR_H = 0.0085f;

    [MenuItem("Tools/TITA/Aplicar Modelo 3D Arduino Uno")]
    static void Crear()
    {
        // ── Cargar el modelo importado ────────────────────────────────────
        var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MODEL_PATH);
        if (modelPrefab == null)
        {
            EditorUtility.DisplayDialog("Modelo no encontrado",
                $"No se encontró el modelo en:\n{MODEL_PATH}\n\n" +
                "Asegúrate de que el archivo .obj esté importado en esa ruta.",
                "OK");
            return;
        }

        // ── Buscar GO del Arduino en la escena ────────────────────────────
        GameObject arduinoGO = null;

        if (Selection.activeGameObject != null &&
            Selection.activeGameObject.GetComponent<ArduinoCore>() != null)
            arduinoGO = Selection.activeGameObject;
        else
            arduinoGO = Object.FindAnyObjectByType<ArduinoCore>()?.gameObject;

        if (arduinoGO == null)
        {
            bool crear = EditorUtility.DisplayDialog("Arduino GO no encontrado",
                "No se encontró ningún GO con ArduinoCore en la escena.\n\n" +
                "¿Crear un nuevo GO 'Arduino_Uno' como raíz?",
                "Crear", "Cancelar");
            if (!crear) return;

            arduinoGO = new GameObject("Arduino_Uno");
            Undo.RegisterCreatedObjectUndo(arduinoGO, "Crear Arduino_Uno");
        }

        Undo.RecordObject(arduinoGO.transform, "Aplicar modelo Arduino");

        // ── Eliminar modelo anterior si existe ────────────────────────────
        var oldModel = arduinoGO.transform.Find("[Arduino_Model]");
        if (oldModel != null) Undo.DestroyObjectImmediate(oldModel.gameObject);

        // ── Instanciar el modelo OBJ como hijo ────────────────────────────
        var modelGO = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, arduinoGO.transform);
        Undo.RegisterCreatedObjectUndo(modelGO, "Modelo Arduino OBJ");
        modelGO.name = "[Arduino_Model]";
        modelGO.transform.localPosition = Vector3.zero;
        modelGO.transform.localRotation = Quaternion.identity;

        // Escalar de unidades del OBJ a metros reales (el OBJ de Meshy viene en mm)
        // Arduino Uno real: 68.6 mm de ancho. Ajustar según bounding box del modelo.
        var renderers = modelGO.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            float maxDim = Mathf.Max(bounds.size.x, bounds.size.z);
            if (maxDim > 0.001f)
            {
                float targetSize = PCB_W;  // 68.6 mm en la dimensión mayor
                float scale = targetSize / maxDim;
                modelGO.transform.localScale = Vector3.one * scale;
            }
        }

        // ── Reubicar Nodo_P13, Nodo_GND, Nodo_A0 a pines físicos reales ──
        float pinTopZ = -0.0025f;
        float pinBotZ = -PCB_D - 0.0025f;

        Vector3 posP13 = new Vector3(PCB_W - PIN_S * 0.5f,           HDR_H + PCB_H, pinTopZ);
        Vector3 posGND = new Vector3(PCB_W - PIN_S * 0.5f - PIN_S*7, HDR_H + PCB_H, pinTopZ);
        Vector3 posA0  = new Vector3(PCB_W * 0.90f,                   HDR_H + PCB_H, pinBotZ);

        int reubicados = 0;
        foreach (Transform child in arduinoGO.transform)
        {
            if (child.name == "Nodo_P13") { child.localPosition = posP13; reubicados++; }
            if (child.name == "Nodo_GND") { child.localPosition = posGND; reubicados++; }
            if (child.name == "Nodo_A0")  { child.localPosition = posA0;  reubicados++; }
        }

        // ── Finalizar ─────────────────────────────────────────────────────
        EditorUtility.SetDirty(arduinoGO);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Selection.activeGameObject = arduinoGO;

        string msg = $"Modelo Arduino Uno aplicado en '{arduinoGO.name}'.\n\n" +
                     $"Modelo: {MODEL_PATH}\n\n";
        msg += reubicados > 0
            ? $"{reubicados}/3 nodos eléctricos reposicionados a pines físicos reales.\n\n"
            : "No se encontraron Nodo_P13/GND/A0 como hijos directos.\n" +
              "  Ejecuta primero el Wizard para crearlos y vuelve a correr este tool.\n\n";
        msg += "Ajusta la posición del GO padre sobre la mesa del Explorador.";

        EditorUtility.DisplayDialog("Arduino Uno aplicado", msg, "OK");
    }
}
#endif
