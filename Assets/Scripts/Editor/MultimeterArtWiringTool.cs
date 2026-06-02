using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Cablea el multímetro ART (Multimeter_VR_Art) en TODOS los componentes de la escena
/// que tengan un campo de tipo <see cref="Multimeter"/> (GameManager, MultimeterUI,
/// NodeInteractable, PlayerInteraction, TechnicianActions, InstructionSystem,
/// PlayerFeedbackUI, ExplorerCircuitPanel, MultimeterDebugHelper, MultimeterProbe,
/// MultimeterModeButton, etc.). Usa reflexión para no olvidar ningún campo.
///
/// Si no hay ningún <see cref="Multimeter"/> en la escena, instancia el prefab
/// Assets/Prefabs/Multimeter_VR_Art.prefab.
///
/// Menú: Tools → TITA → Multímetro → Cablear Multímetro ART en escena
/// </summary>
public static class MultimeterArtWiringTool
{
    const string ART_PREFAB_PATH = "Assets/Prefabs/Multimeter_VR_Art.prefab";

    [MenuItem("Tools/TITA/Multímetro/Cablear Multímetro ART en escena")]
    public static void Wire()
    {
        Multimeter target = ResolveTargetMultimeter();
        if (target == null) return;

        int wired = 0, components = 0;
        var touched = new List<Object>();

        foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mb == null) continue;
            bool changedThis = false;

            var so = new SerializedObject(mb);
            var it = so.GetIterator();
            while (it.NextVisible(true))
            {
                if (it.propertyType != SerializedPropertyType.ObjectReference) continue;

                var fi = FindField(mb.GetType(), it.name);
                if (fi == null || fi.FieldType != typeof(Multimeter)) continue;

                if (it.objectReferenceValue != target)
                {
                    it.objectReferenceValue = target;
                    changedThis = true;
                    wired++;
                }
            }

            if (changedThis)
            {
                so.ApplyModifiedProperties();
                touched.Add(mb);
                components++;
            }
        }

        foreach (var o in touched) EditorUtility.SetDirty(o);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Selection.activeGameObject = target.gameObject;
        EditorGUIUtility.PingObject(target.gameObject);

        EditorUtility.DisplayDialog("Multímetro ART cableado",
            $"Multímetro: '{target.name}'\n\n" +
            $"Campos 'multimeter' asignados: {wired}\n" +
            $"Componentes actualizados: {components}\n\n" +
            "Las puntas (MultimeterProbe) ya detectan ProtoboardSlot, así que el multímetro " +
            "mide en el Reto 4 tocando los slots de la protoboard.\n\n" +
            "Verifica que el prefab esté colocado horizontal sobre la mesa del Explorador.", "OK");

        Debug.Log($"[MultimeterArtWiring] {wired} campos asignados a '{target.name}' " +
                  $"en {components} componentes.");
    }

    // ─────────────────────────────────────────────
    //  Resolución del multímetro objetivo
    // ─────────────────────────────────────────────

    static Multimeter ResolveTargetMultimeter()
    {
        var all = Object.FindObjectsByType<Multimeter>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        // Preferir uno cuyo GO se llame como el prefab ART
        foreach (var m in all)
            if (m.name.Contains("Art") || m.name.Contains("VR_Art"))
                return m;

        if (all.Length == 1) return all[0];

        if (all.Length > 1)
        {
            Debug.LogWarning($"[MultimeterArtWiring] Hay {all.Length} multímetros en escena; " +
                             $"se usará '{all[0].name}'. Renombra el ART para que contenga 'Art' si no es el correcto.");
            return all[0];
        }

        // Ninguno en escena → instanciar el prefab ART
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ART_PREFAB_PATH);
        if (prefab == null)
        {
            EditorUtility.DisplayDialog("Multímetro ART no encontrado",
                $"No hay ningún Multimeter en la escena ni el prefab en:\n{ART_PREFAB_PATH}\n\n" +
                "Crea el prefab con Tools → TITA → Multímetro → Crear Multímetro desde Art Asset.", "OK");
            return null;
        }

        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(inst, "Instanciar Multimeter_VR_Art");
        inst.transform.position = new Vector3(0f, 1f, 0f);
        Debug.Log("[MultimeterArtWiring] Prefab Multimeter_VR_Art instanciado (colócalo sobre la mesa).");
        return inst.GetComponent<Multimeter>();
    }

    // ─────────────────────────────────────────────
    //  Reflexión: busca el campo en la jerarquía de tipos
    // ─────────────────────────────────────────────

    static FieldInfo FindField(System.Type type, string name)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        for (var t = type; t != null && t != typeof(MonoBehaviour); t = t.BaseType)
        {
            var fi = t.GetField(name, flags);
            if (fi != null) return fi;
        }
        return null;
    }
}
