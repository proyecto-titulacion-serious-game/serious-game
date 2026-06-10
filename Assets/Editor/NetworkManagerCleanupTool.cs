#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Detecta y limpia ConnectionManager / GameSession duplicados en la escena activa.
///
/// Contexto del proyecto TITA:
///   - Tecnico.unity tiene 2 GOs "NetworkManager" (uno stray en posición lejana sin playerPrefab,
///     uno dentro de GameManager_System con playerPrefab asignado).
///   - NoonA.unity (cargado aditivamente) puede tener un tercero de una sesión anterior.
///   - Al correr con 3 instancias, FindAnyObjectByType<ConnectionManager>() puede devolver
///     el incorrecto y fallar la conexión.
///
/// Menú: Tools → TITA → Red → Limpiar NetworkManagers duplicados
/// </summary>
public class NetworkManagerCleanupTool : EditorWindow
{
    private Vector2 _scroll;
    private List<ConnectionManager> _found = new();

    [MenuItem("Tools/TITA/Red/Limpiar NetworkManagers duplicados")]
    static void ShowWindow() => GetWindow<NetworkManagerCleanupTool>("NetworkManager Cleanup");

    void OnEnable() => Scan();

    void Scan()
    {
        _found.Clear();
        var all = FindObjectsByType<ConnectionManager>(FindObjectsInactive.Include);
        _found.AddRange(all);
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("NetworkManager / ConnectionManager — Detector", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(
            "Encontrados en TODAS las escenas cargadas actualmente.",
            EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.Space(6);

        if (GUILayout.Button("Escanear de nuevo", GUILayout.Height(26))) Scan();
        EditorGUILayout.Space(6);

        if (_found.Count == 0)
        {
            EditorGUILayout.HelpBox("No se encontró ningún ConnectionManager.", MessageType.Warning);
            return;
        }

        // Detectar el "mejor" (tiene playerPrefab válido)
        ConnectionManager best = null;
        foreach (var cm in _found)
        {
            var so   = new SerializedObject(cm);
            var prop = so.FindProperty("playerPrefab");
            bool hasRef = prop != null && prop.FindPropertyRelative("AssetGuidLow")?.longValue != 0;
            if (hasRef) { best = cm; break; }
        }

        if (_found.Count == 1)
        {
            EditorGUILayout.HelpBox("Solo hay un ConnectionManager — sin duplicados.", MessageType.Info);
        }
        else
        {
            var warnColor = new GUIStyle(EditorStyles.label) { normal = { textColor = new Color(1f, 0.6f, 0.1f) } };
            EditorGUILayout.LabelField($"⚠ Se encontraron {_found.Count} instancias:", warnColor);
        }

        EditorGUILayout.Space(4);
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        for (int i = 0; i < _found.Count; i++)
        {
            var cm = _found[i];
            if (cm == null) continue;

            bool isBest     = cm == best;
            var  so         = new SerializedObject(cm);
            var  propPrefab = so.FindProperty("playerPrefab");
            bool hasRef     = propPrefab != null &&
                              propPrefab.FindPropertyRelative("AssetGuidLow")?.longValue != 0;
            var  propOffline = so.FindProperty("modoOffline");
            bool offline    = propOffline?.boolValue ?? false;

            Color bgColor = isBest ? new Color(0.2f, 0.5f, 0.2f, 0.3f) : new Color(0.5f, 0.2f, 0.2f, 0.3f);
            var  bgRect   = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(bgRect, bgColor);

            using (new EditorGUILayout.HorizontalScope())
            {
                string status = isBest ? "[MANTENER]" : "[DUPLICADO]";
                var    col    = isBest ? new Color(0.3f, 1f, 0.3f) : new Color(1f, 0.4f, 0.4f);
                var    style  = new GUIStyle(EditorStyles.boldLabel) { normal = { textColor = col } };
                EditorGUILayout.LabelField(status, style, GUILayout.Width(100));
                EditorGUILayout.LabelField(cm.gameObject.name, GUILayout.Width(130));
                EditorGUILayout.LabelField($"Escena: {cm.gameObject.scene.name}", GUILayout.Width(120));
                EditorGUILayout.LabelField($"prefab={hasRef} offline={offline}", EditorStyles.miniLabel);
            }

            // Jerarquía completa
            EditorGUILayout.LabelField(
                "  Path: " + GetPath(cm.transform),
                EditorStyles.miniLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Seleccionar", GUILayout.Height(20)))
                {
                    Selection.activeGameObject = cm.gameObject;
                    EditorGUIUtility.PingObject(cm.gameObject);
                }

                if (!isBest)
                {
                    GUI.color = new Color(1f, 0.4f, 0.4f);
                    if (GUILayout.Button("Eliminar este duplicado", GUILayout.Height(20)))
                    {
                        if (EditorUtility.DisplayDialog("Confirmar eliminación",
                            $"¿Eliminar '{cm.gameObject.name}' de la escena '{cm.gameObject.scene.name}'?\n\n" +
                            $"Path: {GetPath(cm.transform)}",
                            "Eliminar", "Cancelar"))
                        {
                            Undo.DestroyObjectImmediate(cm.gameObject);
                            EditorSceneManager.MarkSceneDirty(cm.gameObject.scene);
                            Scan();
                        }
                    }
                    GUI.color = Color.white;
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(8);

        if (_found.Count > 1)
        {
            GUI.color = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("Eliminar TODOS los duplicados (mantener el mejor)", GUILayout.Height(32)))
            {
                if (EditorUtility.DisplayDialog("Eliminar duplicados",
                    $"Se eliminarán {_found.Count - 1} NetworkManager(s) duplicados.\n" +
                    $"Se mantendrá: {(best != null ? GetPath(best.transform) : "ninguno detectado como mejor")}",
                    "Eliminar", "Cancelar"))
                {
                    foreach (var cm in _found)
                    {
                        if (cm == null || cm == best) continue;
                        EditorSceneManager.MarkSceneDirty(cm.gameObject.scene);
                        Undo.DestroyObjectImmediate(cm.gameObject);
                    }
                    Scan();
                }
            }
            GUI.color = Color.white;
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox(
            "Tras eliminar duplicados, guarda las escenas (Ctrl+S o Save All).\n" +
            "El ConnectionManager correcto es el que tiene playerPrefab asignado.",
            MessageType.Info);
    }

    static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null) { t = t.parent; path = t.name + "/" + path; }
        return path;
    }
}
#endif
