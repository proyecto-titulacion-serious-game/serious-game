#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FlexWire))]
public class FlexWireEditor : Editor
{
    static readonly Color HandleColor  = new Color(0.20f, 0.85f, 1.00f);
    static readonly Color LineColor    = new Color(0.20f, 0.85f, 1.00f, 0.45f);
    static readonly Color AddColor     = new Color(0.30f, 1.00f, 0.40f);
    static readonly Color RemoveColor  = new Color(1.00f, 0.35f, 0.25f);

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var wire = (FlexWire)target;
        var pts  = wire.Points;

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Puntos de control", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Total: {pts.Count}  |  Mínimo: 2", EditorStyles.miniLabel);

        EditorGUILayout.BeginHorizontal();

        var prevColor = GUI.color;
        GUI.color = AddColor;
        if (GUILayout.Button("＋ Añadir punto al final"))
        {
            Undo.RecordObject(wire, "Añadir punto FlexWire");
            var last = pts.Count > 0 ? pts[pts.Count - 1] : Vector3.zero;
            pts.Add(last + Vector3.right * 0.05f);
            wire.Rebuild();
            EditorUtility.SetDirty(wire);
        }

        GUI.color = pts.Count > 2 ? RemoveColor : Color.gray;
        EditorGUI.BeginDisabledGroup(pts.Count <= 2);
        if (GUILayout.Button("－ Eliminar último"))
        {
            Undo.RecordObject(wire, "Eliminar punto FlexWire");
            pts.RemoveAt(pts.Count - 1);
            wire.Rebuild();
            EditorUtility.SetDirty(wire);
        }
        EditorGUI.EndDisabledGroup();

        GUI.color = prevColor;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
        if (GUILayout.Button("↺ Reconstruir malla"))
        {
            wire.Rebuild();
            EditorUtility.SetDirty(wire);
        }
    }

    void OnSceneGUI()
    {
        var wire = (FlexWire)target;
        var pts  = wire.Points;
        if (pts == null || pts.Count < 2) return;

        bool changed = false;

        // ── Líneas de guía entre puntos de control ────────────────────
        Handles.color = LineColor;
        for (int i = 0; i < pts.Count - 1; i++)
        {
            Handles.DrawLine(
                wire.transform.TransformPoint(pts[i]),
                wire.transform.TransformPoint(pts[i + 1]), 1.5f);
        }

        // ── Handle de posición por cada punto ─────────────────────────
        Handles.color = HandleColor;
        for (int i = 0; i < pts.Count; i++)
        {
            Vector3 worldPos = wire.transform.TransformPoint(pts[i]);

            // Etiqueta con índice
            Handles.Label(worldPos + Vector3.up * 0.015f,
                $"P{i}", new GUIStyle(EditorStyles.miniLabel)
                { normal = { textColor = HandleColor } });

            EditorGUI.BeginChangeCheck();
            Vector3 newWorld = Handles.PositionHandle(worldPos, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(wire, $"Mover punto P{i} FlexWire");
                pts[i] = wire.transform.InverseTransformPoint(newWorld);
                changed = true;
            }
        }

        if (changed)
        {
            wire.Rebuild();
            EditorUtility.SetDirty(wire);
        }
    }
}
#endif
