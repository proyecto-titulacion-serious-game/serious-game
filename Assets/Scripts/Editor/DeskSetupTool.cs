using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class DeskSetupTool
{
    const string WORKSTATION_GUID = "bd30ccc878ceffa468aaa86d1410e88d";
    const string DESK_FBX_GUID    = "cfc0af9e11535854f972e86c5688f0ea";
    const string CHAIR_FBX_GUID   = "c7e424a57d3c66b4591c741aa9dc074a";

    [MenuItem("Tools/TITA/Setup Desk Kenney en Tecnico")]
    static void SetupDesk()
    {
        // ── 1. Asegurar que Tecnico.unity está abierto ──────────────────────
        Scene active = SceneManager.GetActiveScene();
        if (!active.name.Equals("Tecnico", System.StringComparison.OrdinalIgnoreCase))
        {
            bool ok = EditorUtility.DisplayDialog(
                "Setup Desk Kenney",
                $"La escena activa es '{active.name}'.\n¿Abrir Tecnico.unity ahora?",
                "Abrir", "Cancelar");
            if (!ok) return;

            string scenePath = AssetDatabase.GUIDToAssetPath("99c9720ab356a0642a771bea13969a05");
            if (string.IsNullOrEmpty(scenePath))
                scenePath = "Assets/Scenes/Tecnico.unity";

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            active = SceneManager.GetActiveScene();
        }

        // ── 2. Instanciar Technician_Workstation.prefab ─────────────────────
        string prefabPath = AssetDatabase.GUIDToAssetPath(WORKSTATION_GUID);
        if (string.IsNullOrEmpty(prefabPath))
        {
            Debug.LogError("[DeskSetupTool] No se encontró Technician_Workstation.prefab");
            return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) { Debug.LogError("[DeskSetupTool] No se pudo cargar el prefab."); return; }

        GameObject existing = GameObject.Find(prefab.name);
        if (existing != null)
        {
            bool replace = EditorUtility.DisplayDialog("Setup Desk Kenney",
                $"Ya existe '{prefab.name}' en la escena. ¿Reemplazarlo?", "Reemplazar", "Cancelar");
            if (!replace) return;
            Undo.DestroyObjectImmediate(existing);
        }

        // Eliminar silla vieja si existía
        var oldChair = GameObject.Find("Chair_Kenney");
        if (oldChair != null) Undo.DestroyObjectImmediate(oldChair);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        Undo.RegisterCreatedObjectUndo(instance, "Setup Workstation");

        // Material URP por defecto para los objetos Kenney sin material propio
        Material defaultMat = GetOrCreateDefaultMat();

        // ── 3. Reemplazar mesh de Desk_Surface con Kenney desk.fbx ──────────
        Transform deskSurface = FindDeep(instance.transform, "Desk_Surface");
        if (deskSurface != null)
        {
            Mesh deskMesh = LoadFirstMesh(DESK_FBX_GUID, "desk");
            if (deskMesh != null)
            {
                MeshFilter mf = deskSurface.GetComponent<MeshFilter>();
                if (mf == null) mf = deskSurface.gameObject.AddComponent<MeshFilter>();
                Undo.RecordObject(mf, "Assign Kenney Desk Mesh");
                mf.sharedMesh = deskMesh;

                MeshRenderer mr = deskSurface.GetComponent<MeshRenderer>();
                if (mr == null) mr = deskSurface.gameObject.AddComponent<MeshRenderer>();

                // Rellenar todos los submesh slots con el material de escritorio
                // para que ninguno quede rosado por falta de material
                FixRendererMaterials(mr, defaultMat);

                deskSurface.localScale = Vector3.one;
                Debug.Log("[DeskSetupTool] Mesh de Desk_Surface → Kenney desk.fbx");
            }
        }
        else
        {
            Debug.LogWarning("[DeskSetupTool] 'Desk_Surface' no encontrado en el prefab.");
        }

        // ── 4. Instanciar silla Kenney con material ──────────────────────────
        Mesh chairMesh = LoadFirstMesh(CHAIR_FBX_GUID, "chairDesk");
        if (chairMesh != null)
        {
            GameObject chairGO = new GameObject("Chair_Kenney");
            Undo.RegisterCreatedObjectUndo(chairGO, "Add Kenney Chair");

            MeshFilter cmf = chairGO.AddComponent<MeshFilter>();
            cmf.sharedMesh = chairMesh;

            MeshRenderer cmr = chairGO.AddComponent<MeshRenderer>();
            FixRendererMaterials(cmr, defaultMat);

            chairGO.transform.position = instance.transform.position + new Vector3(0f, 0f, -0.8f);
            Debug.Log("[DeskSetupTool] Silla Kenney instanciada con material.");
        }

        // ── 5. Marcar escena como modificada ────────────────────────────────
        EditorSceneManager.MarkSceneDirty(active);
        Debug.Log("[DeskSetupTool] Listo. Guarda con Ctrl+S.");
    }

    // ── Asigna un material a todos los slots del renderer ──────────────────
    static void FixRendererMaterials(MeshRenderer mr, Material mat)
    {
        if (mat == null) return;
        int count = Mathf.Max(1, mr.sharedMaterials.Length);
        // Rellenar slots vacíos o nulos con el material por defecto
        var mats = new Material[count];
        for (int i = 0; i < count; i++)
            mats[i] = (mr.sharedMaterials.Length > i && mr.sharedMaterials[i] != null)
                      ? mr.sharedMaterials[i]
                      : mat;
        mr.sharedMaterials = mats;
    }

    // ── Crea o reutiliza un material URP/Lit gris neutro ──────────────────
    static Material GetOrCreateDefaultMat()
    {
        const string path = "Assets/Materials/Mat_Kenney_Default.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null) lit = Shader.Find("Standard");

        var mat = new Material(lit);
        mat.name = "Mat_Kenney_Default";
        mat.SetColor("_BaseColor", new Color(0.8f, 0.75f, 0.65f)); // beige claro
        AssetDatabase.CreateAsset(mat, path);
        AssetDatabase.SaveAssets();
        return mat;
    }

    static Transform FindDeep(Transform root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    static Mesh LoadFirstMesh(string guid, string hint)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(path)) return null;

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        Mesh best = null;
        foreach (var a in assets)
        {
            if (a is Mesh m)
            {
                if (best == null || m.name.IndexOf(hint, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    best = m;
            }
        }
        return best;
    }
}
