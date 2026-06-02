#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Herramienta de editor para generar la cuadrícula magnética de ProtoboardSlots.
/// Tools > TITA > Generador de Slots de Protoboard
///
/// Genera una cuadrícula de N filas × M columnas con los railIds correctos
/// (A1-A30 para filas de terminal, VCC y GND para rieles de alimentación).
/// Añade automáticamente los slots a ProtoboardSimulator.todosLosSlots.
/// </summary>
public class ProtoboardSlotGenerator : EditorWindow
{
    private ProtoboardSimulator _target;
    private int    _filas       = 10;
    private int    _columnas    = 5;
    private float  _spacing     = 0.02f;   // 2 cm entre slots
    private bool   _addPowerRails = true;
    private GameObject _slotPrefab;

    [MenuItem("Tools/TITA/Generador de Slots de Protoboard")]
    static void ShowWindow() => GetWindow<ProtoboardSlotGenerator>("Generador Protoboard");

    void OnGUI()
    {
        GUILayout.Label("Generador de Cuadrícula de Protoboard", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        _target       = (ProtoboardSimulator)EditorGUILayout.ObjectField("ProtoboardSimulator", _target, typeof(ProtoboardSimulator), true);
        _slotPrefab   = (GameObject)EditorGUILayout.ObjectField("Prefab de Slot (opcional)", _slotPrefab, typeof(GameObject), false);
        _filas        = EditorGUILayout.IntSlider("Filas (terminal strips)", _filas, 2, 30);
        _columnas     = EditorGUILayout.IntSlider("Columnas por fila", _columnas, 2, 10);
        _spacing      = EditorGUILayout.Slider("Separación (m)", _spacing, 0.005f, 0.05f);
        _addPowerRails = EditorGUILayout.Toggle("Añadir rieles VCC / GND", _addPowerRails);

        EditorGUILayout.Space();

        GUI.enabled = _target != null;
        if (GUILayout.Button("Generar cuadrícula"))
            GenerateGrid();
        GUI.enabled = true;

        if (_target == null)
            EditorGUILayout.HelpBox("Arrastra un GameObject con ProtoboardSimulator al campo de arriba.", MessageType.Info);
    }

    void GenerateGrid()
    {
        // Limpiar slots anteriores
        var oldRoot = _target.transform.Find("[ProtoboardSlots]");
        if (oldRoot != null) DestroyImmediate(oldRoot.gameObject);

        var root = new GameObject("[ProtoboardSlots]");
        root.transform.SetParent(_target.transform, false);

        var nuevosSlots = new List<ProtoboardSlot>();

        // ─── Terminal strips ────────────────────────────────
        for (int fila = 0; fila < _filas; fila++)
        {
        string railId = $"ROW_{(char)('A' + fila)}";
            for (int col = 0; col < _columnas; col++)
            {
                var slot = CreateSlot(root.transform, railId, fila, col);
                slot.transform.localPosition = new Vector3(col * _spacing, 0f, -fila * _spacing);
                nuevosSlots.Add(slot);
            }
        }

        // ─── Rieles de alimentación ─────────────────────────
        if (_addPowerRails)
        {
            float vccZ = -(_filas + 1) * _spacing;
            float gndZ = -(_filas + 2) * _spacing;

            for (int col = 0; col < _columnas; col++)
            {
                var vcc = CreateSlot(root.transform, "VCC", 99, col);
                vcc.transform.localPosition = new Vector3(col * _spacing, 0f, vccZ);
                nuevosSlots.Add(vcc);

                var gnd = CreateSlot(root.transform, "GND", 100, col);
                gnd.transform.localPosition = new Vector3(col * _spacing, 0f, gndZ);
                nuevosSlots.Add(gnd);
            }
        }

        // Asignar al CircuitSimulator
        Undo.RecordObject(_target, "Generar slots protoboard");
        _target.todosLosSlots = nuevosSlots;
        EditorUtility.SetDirty(_target);

        Debug.Log($"[ProtoboardSlotGenerator] {nuevosSlots.Count} slots generados en '{_target.gameObject.name}'.");
    }

    ProtoboardSlot CreateSlot(Transform parent, string railId, int row, int col)
    {
        GameObject go;
        if (_slotPrefab != null)
        {
            go = (GameObject)PrefabUtility.InstantiatePrefab(_slotPrefab, parent);
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.transform.SetParent(parent, false);
            go.transform.localScale = new Vector3(0.008f, 0.003f, 0.008f);
        }

        go.name = $"Slot_{railId}_{col}";

        var slot = go.GetComponent<ProtoboardSlot>() ?? go.AddComponent<ProtoboardSlot>();
        slot.railId = railId;
        slot.row    = row;
        slot.col    = col;

        return slot;
    }
}
#endif
