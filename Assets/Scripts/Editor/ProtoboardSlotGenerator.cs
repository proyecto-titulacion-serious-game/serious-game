#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Herramienta de editor para generar la cuadrícula magnética de ProtoboardSlots.
/// Tools > TITA > Generador de Slots de Protoboard
///
/// CORRECCIÓN ARQUITECTÓNICA: 
/// Divide físicamente las filas en Bloque Izquierdo (L) y Bloque Derecho (R) 
/// para respetar el canal central (DIP gap) y evitar cortocircuitos lógicos en el motor MNA.
/// </summary>
public class ProtoboardSlotGenerator : EditorWindow
{
    private ProtoboardSimulator _target;
    private int    _filas       = 10;
    private int    _columnas    = 5;       // Columnas por lado (5 izq, 5 der)
    private float  _spacing     = 0.02f;   // Distancia entre huecos
    private float  _centerGap   = 0.01f;   // Distancia extra del canal central aislante
    private bool   _addPowerRails = true;
    private GameObject _slotPrefab;

    [MenuItem("Tools/TITA/Generador de Slots de Protoboard")]
    static void ShowWindow() => GetWindow<ProtoboardSlotGenerator>("Generador Protoboard");

    void OnGUI()
    {
        GUILayout.Label("Generador de Cuadrícula de Protoboard (Corregido)", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        _target       = (ProtoboardSimulator)EditorGUILayout.ObjectField("ProtoboardSimulator", _target, typeof(ProtoboardSimulator), true);
        _slotPrefab   = (GameObject)EditorGUILayout.ObjectField("Prefab de Slot (opcional)", _slotPrefab, typeof(GameObject), false);
        _filas        = EditorGUILayout.IntSlider("Filas", _filas, 2, 60);
        _columnas     = EditorGUILayout.IntSlider("Columnas por bloque", _columnas, 2, 10);
        _spacing      = EditorGUILayout.Slider("Separación (m)", _spacing, 0.005f, 0.05f);
        _centerGap    = EditorGUILayout.Slider("Canal Central (m)", _centerGap, 0.005f, 0.05f);
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
        var oldRoot = _target.transform.Find("[ProtoboardSlots]");
        if (oldRoot != null) DestroyImmediate(oldRoot.gameObject);

        var root = new GameObject("[ProtoboardSlots]");
        root.transform.SetParent(_target.transform, false);

        var nuevosSlots = new List<ProtoboardSlot>();

        // ─── Terminal strips (Dividido por el DIP Gap central) ──────────────
        for (int fila = 1; fila <= _filas; fila++)
        {
            // BLOQUE IZQUIERDO (Aislado eléctricamente)
            string railIdLeft = $"ROW_{fila}_L";
            for (int col = 0; col < _columnas; col++)
            {
                var slot = CreateSlot(root.transform, railIdLeft, fila, col);
                slot.transform.localPosition = new Vector3(col * _spacing, 0f, -fila * _spacing);
                nuevosSlots.Add(slot);
            }

            // BLOQUE DERECHO (Aislado eléctricamente)
            string railIdRight = $"ROW_{fila}_R";
            for (int col = 0; col < _columnas; col++)
            {
                var slot = CreateSlot(root.transform, railIdRight, fila, col + _columnas);
                
                // Calculamos el desplazamiento en X saltando el bloque izquierdo y el canal central
                float offsetX = (_columnas * _spacing) + _centerGap;
                slot.transform.localPosition = new Vector3(offsetX + (col * _spacing), 0f, -fila * _spacing);
                nuevosSlots.Add(slot);
            }
        }

        // ─── Rieles de alimentación ─────────────────────────
        if (_addPowerRails)
        {
            float vccZ = -(_filas + 1) * _spacing;
            float gndZ = -(_filas + 2) * _spacing;
            
            // Calculamos el ancho total para centrar los rieles de poder
            int totalCols = _columnas * 2;
            
            for (int col = 0; col < totalCols; col++)
            {
                float posX = col < _columnas 
                    ? col * _spacing 
                    : (_columnas * _spacing) + _centerGap + ((col - _columnas) * _spacing);

                var vcc = CreateSlot(root.transform, "VCC", 99, col);
                vcc.transform.localPosition = new Vector3(posX, 0f, vccZ);
                nuevosSlots.Add(vcc);

                var gnd = CreateSlot(root.transform, "GND", 100, col);
                gnd.transform.localPosition = new Vector3(posX, 0f, gndZ);
                nuevosSlots.Add(gnd);
            }
        }

        // Asignar al ProtoboardSimulator
        Undo.RecordObject(_target, "Generar slots protoboard");
        _target.todosLosSlots = nuevosSlots;
        EditorUtility.SetDirty(_target);

        Debug.Log($"[ProtoboardSlotGenerator] {nuevosSlots.Count} slots generados en '{_target.gameObject.name}'. Aislamiento central aplicado.");
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