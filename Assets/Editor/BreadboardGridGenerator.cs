#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Genera una matriz de slots magnéticos invisibles sobre la protoboard.
/// Versión legacy compatible con la API actual de ComponentSlot.
///
/// Menú: Tools → TITA → Generar Matriz Protoboard
///
/// Para generación avanzada con ProtoboardSlot usar:
///   Tools → TITA → Generador de Slots de Protoboard
/// </summary>
public class BreadboardGridGenerator : EditorWindow
{
    private GameObject _bareboard;
    private int   _filas     = 10;
    private int   _columnas  = 5;
    private float _espaciado = 0.018f;
    private ComponentSlotType _tipoSlot = ComponentSlotType.Resistor;

    [MenuItem("Tools/TITA/Generar Matriz Protoboard")]
    public static void ShowWindow()
        => GetWindow<BreadboardGridGenerator>("Generador Matriz Protoboard");

    void OnGUI()
    {
        EditorGUILayout.LabelField("Configuración de la Protoboard", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        _bareboard = (GameObject)EditorGUILayout.ObjectField(
            "Bareboard (Padre)", _bareboard, typeof(GameObject), true);

        _filas     = EditorGUILayout.IntField("Filas (largo)",    _filas);
        _columnas  = EditorGUILayout.IntField("Columnas (ancho)", _columnas);
        _espaciado = EditorGUILayout.FloatField("Espaciado (m)",  _espaciado);
        _tipoSlot  = (ComponentSlotType)EditorGUILayout.EnumPopup("Tipo de slot", _tipoSlot);

        EditorGUILayout.Space(6);
        EditorGUILayout.HelpBox(
            $"Se crearán {_filas * _columnas} slots.\n" +
            "Cada slot tiene BoxCollider (trigger) + ComponentSlot.",
            MessageType.Info);

        EditorGUILayout.Space(4);
        using (new EditorGUI.DisabledScope(_bareboard == null))
        {
            if (GUILayout.Button("Generar Matriz Magnética", GUILayout.Height(32)))
                GenerarMatriz();
        }

        if (_bareboard == null)
            EditorGUILayout.HelpBox("Asigna el GameObject padre (bareboard).", MessageType.Warning);
    }

    void GenerarMatriz()
    {
        // Limpiar contenedor anterior si existe
        var oldRoot = _bareboard.transform.Find("Slots_Matriz");
        if (oldRoot != null)
        {
            bool ok = EditorUtility.DisplayDialog(
                "Limpiar slots anteriores",
                "Ya existe 'Slots_Matriz'. ¿Eliminar y regenerar?", "Sí", "No");
            if (!ok) return;
            Undo.DestroyObjectImmediate(oldRoot.gameObject);
        }

        var contenedor = new GameObject("Slots_Matriz");
        Undo.RegisterCreatedObjectUndo(contenedor, "Generar Slots_Matriz");
        contenedor.transform.SetParent(_bareboard.transform, false);
        contenedor.transform.localPosition = Vector3.zero;

        float halfW = (_columnas - 1) * _espaciado * 0.5f;
        float halfD = (_filas    - 1) * _espaciado * 0.5f;
        Vector3 origin = new Vector3(-halfW, 0.002f, -halfD);

        for (int z = 0; z < _filas; z++)
        {
            for (int x = 0; x < _columnas; x++)
            {
                var slotGO = new GameObject($"Slot_{z}_{x}");
                Undo.RegisterCreatedObjectUndo(slotGO, "Crear slot");
                slotGO.transform.SetParent(contenedor.transform, false);
                slotGO.transform.localPosition = origin + new Vector3(x * _espaciado, 0, z * _espaciado);

                var col = slotGO.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.size      = new Vector3(0.012f, 0.01f, 0.012f);

                var slot = slotGO.AddComponent<ComponentSlot>();
                slot.acceptedType = _tipoSlot;

                var anchor = new GameObject("Anchor");
                Undo.RegisterCreatedObjectUndo(anchor, "Crear Anchor");
                anchor.transform.SetParent(slotGO.transform, false);
                anchor.transform.localPosition = Vector3.zero;
                slot.installAnchor = anchor.transform;
            }
        }

        EditorUtility.SetDirty(_bareboard);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog(
            "Matriz generada",
            $"{_filas * _columnas} slots creados en '{_bareboard.name}/Slots_Matriz'.\n\n" +
            "Ajusta la posición del contenedor para alinearla con el modelo 3D.",
            "OK");

        Debug.Log($"[BreadboardGridGenerator] {_filas * _columnas} slots creados en {_bareboard.name}.");
    }
}
#endif
