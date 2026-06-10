#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Arregla la ORIENTACIÓN de agarre del multímetro VR.
///
/// Problema: el XRGrabInteractable del multímetro no tiene 'Attach Transform' asignado
/// (m_AttachTransform = 0), así que al agarrarlo se orienta según el origen del modelo y
/// queda "de cabeza". La solución es un transform hijo 'Grab_Attach' con la pose con la
/// que queremos que el objeto quede en la mano; XRI alinea ese punto al mando.
///
/// El modelo (Multimeter_VR_Art) es horizontal: largo en Z, ancho en X, display en la cara
/// superior (+Y), jacks/cables hacia −Z. Para sostenerlo "de frente" (display mirando al
/// jugador) agarramos por el extremo +Z e inclinamos la pantalla hacia la cara.
///
/// Menús:
///   Tools → TITA → Multímetro → Arreglar agarre (prefabs)
///   Tools → TITA → Multímetro → Arreglar agarre (multímetro en escena)
/// </summary>
public static class MultimeterGrabPoseTool
{
    const string ATTACH_NAME = "Grab_Attach";

    static readonly string[] PREFAB_PATHS =
    {
        "Assets/Prefabs/Multimeter_VR_Art.prefab",
        "Assets/Prefabs/Multimeter_VR.prefab",
    };

    // Pose de agarre (espacio local del cuerpo del multímetro).
    //  - Posición: extremo cercano al jugador (+Z), ligeramente bajo el centro → cuelga natural.
    //  - Rotación: inclina la pantalla (+Y) hacia la cara y deja las puntas apuntando lejos.
    static readonly Vector3 GRIP_POS = new Vector3(0f, -0.006f, 0.045f);
    static readonly Vector3 GRIP_ROT = new Vector3(-35f, 0f, 0f);

    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem("Tools/TITA/Multímetro/Arreglar agarre (prefabs)")]
    public static void FixPrefabs()
    {
        int hechos = 0;
        foreach (var path in PREFAB_PATHS)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null) continue;

            var root = PrefabUtility.LoadPrefabContents(path);
            if (ApplyGrabPose(root))
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                hechos++;
                Debug.Log($"[MultimeterGrabPose] Agarre corregido en {path}");
            }
            else
            {
                Debug.LogWarning($"[MultimeterGrabPose] {path} no tiene XRGrabInteractable en la raíz; omitido.");
            }
            PrefabUtility.UnloadPrefabContents(root);
        }
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("TITA — Agarre multímetro",
            hechos > 0
              ? $"Punto de agarre '{ATTACH_NAME}' creado/actualizado en {hechos} prefab(s).\n\n" +
                "Si en VR aún queda algo girado, selecciona el hijo 'Grab_Attach' del prefab y " +
                "ajusta su Rotation (sobre todo X) hasta que el display mire al jugador."
              : "No se encontró ningún prefab de multímetro con XRGrabInteractable en la raíz.",
            "OK");
    }

    [MenuItem("Tools/TITA/Multímetro/Arreglar agarre (multímetro en escena)")]
    public static void FixSceneInstances()
    {
        var grabs = Object.FindObjectsByType<XRGrabInteractable>(FindObjectsInactive.Include);
        int hechos = 0;
        foreach (var grab in grabs)
        {
            // Solo los multímetros (por nombre o por tener el script Multimeter en la raíz).
            bool esMultimetro = grab.GetComponent<Multimeter>() != null ||
                                grab.name.ToLowerInvariant().Contains("multimeter") ||
                                grab.name.ToLowerInvariant().Contains("multimetro");
            if (!esMultimetro) continue;

            Undo.RegisterFullObjectHierarchyUndo(grab.gameObject, "Arreglar agarre multímetro");
            if (ApplyGrabPose(grab.gameObject))
            {
                hechos++;
                EditorUtility.SetDirty(grab);
                EditorSceneManager.MarkSceneDirty(grab.gameObject.scene);
            }
        }

        EditorUtility.DisplayDialog("TITA — Agarre multímetro",
            hechos > 0
              ? $"Agarre corregido en {hechos} multímetro(s) de la escena.\nGuarda la escena (Ctrl+S)."
              : "No se encontró ningún multímetro (XRGrabInteractable + Multimeter) en la escena.",
            "OK");
    }

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>Crea/actualiza el hijo 'Grab_Attach' y lo asigna como attachTransform. true si aplicó.</summary>
    static bool ApplyGrabPose(GameObject root)
    {
        var grab = root.GetComponent<XRGrabInteractable>();
        if (grab == null) return false;

        var attach = root.transform.Find(ATTACH_NAME);
        if (attach == null)
        {
            var go = new GameObject(ATTACH_NAME);
            attach = go.transform;
            attach.SetParent(root.transform, false);
        }

        attach.localPosition = GRIP_POS;
        attach.localRotation = Quaternion.Euler(GRIP_ROT);
        attach.localScale    = Vector3.one;

        grab.attachTransform = attach;
        // Pose consistente: usa el attachTransform tal cual (no calcula offset dinámico al agarrar).
        grab.useDynamicAttach = false;
        return true;
    }
}
#endif
