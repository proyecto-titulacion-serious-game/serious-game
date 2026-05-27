using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// Construye la habitación del técnico con mobiliario Kenney:
///   · Suelo, paredes y techo con primitivas
///   · Escritorio, silla, monitor, teclado, ratón, librería, lámpara
/// Menu: Tools/TITA/Setup Kenney Room en Tecnico
public static class KenneyRoomSetupTool
{
    // ── GUIDs de los FBX de Kenney ────────────────────────────────────────
    const string GUID_DESK          = "cfc0af9e11535854f972e86c5688f0ea";
    const string GUID_DESK_CORNER   = "784fbb5d8d690734bae78610cd776fae";
    const string GUID_CHAIR         = "c7e424a57d3c66b4591c741aa9dc074a";
    const string GUID_MONITOR       = "678a8800673058948b40b9640bb41c23";
    const string GUID_KEYBOARD      = "9045dca3c5d78014f99a7ccc5b1f729d";
    const string GUID_MOUSE         = "d6284fdc0a1013e4fbd7560b8d094c4d";
    const string GUID_BOOKCASE_OPEN = "fdeddd397e678844186eb5c7fcf1006f";
    const string GUID_BOOKCASE_SHUT = "c9c64a9033bccb24e89f7dd1c5315b38";
    const string GUID_LAMP_CEIL     = "29606d4d87d514c4485120a6f7436abf";
    const string GUID_LAMP_FLOOR    = "f59e1bf6a9e9dac45b8daf2c121f23a8";
    const string GUID_TABLE         = "fa3f38f915ba8474cbe927250f66e7fb";

    // Dimensiones del cuarto (metros)
    const float ROOM_W = 8f;
    const float ROOM_D = 8f;
    const float ROOM_H = 3f;

    [MenuItem("Tools/TITA/Setup Kenney Room en Tecnico")]
    static void Setup()
    {
        Scene active = SceneManager.GetActiveScene();
        if (!active.name.Equals("Tecnico", System.StringComparison.OrdinalIgnoreCase))
        {
            bool ok = EditorUtility.DisplayDialog("Setup Kenney Room",
                $"La escena activa es '{active.name}'.\n¿Abrir Tecnico.unity?",
                "Abrir", "Cancelar");
            if (!ok) return;
            string path = AssetDatabase.GUIDToAssetPath("99c9720ab356a0642a771bea13969a05");
            if (string.IsNullOrEmpty(path)) path = "Assets/Scenes/Tecnico.unity";
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            active = SceneManager.GetActiveScene();
        }

        // Eliminar cuarto anterior si existe
        var old = GameObject.Find("KenneyRoom");
        if (old != null) Undo.DestroyObjectImmediate(old);

        // Raíz del cuarto
        var root = new GameObject("KenneyRoom");
        Undo.RegisterCreatedObjectUndo(root, "Setup Kenney Room");
        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        // ── Materiales ────────────────────────────────────────────────────
        Material matFloor   = GetOrCreateMat("KenneyFloor",   new Color(0.55f, 0.45f, 0.35f));
        Material matWall    = GetOrCreateMat("KenneyWall",    new Color(0.85f, 0.82f, 0.76f));
        Material matCeiling = GetOrCreateMat("KenneyCeiling", new Color(0.9f,  0.9f,  0.88f));
        Material matWood    = GetOrCreateMat("KenneyWood",    new Color(0.60f, 0.42f, 0.22f));
        Material matDark    = GetOrCreateMat("KenneyDark",    new Color(0.15f, 0.15f, 0.18f));
        Material matLight   = GetOrCreateMat("KenneyLight",   new Color(0.92f, 0.92f, 0.88f));

        // ── Geometría de la habitación ────────────────────────────────────
        BuildRoom(root.transform, matFloor, matWall, matCeiling);

        // ── Escritorio del técnico (Technician_Workstation ya existe) ─────
        // Posicionado en la pared norte, centrado
        Vector3 deskPos = new Vector3(0f, 0f, ROOM_D * 0.5f - 1.2f);

        // ── Segundo escritorio esquina (para el monitor/computadora) ──────
        PlaceFurniture(GUID_DESK_CORNER, "Desk_Corner", root.transform,
            new Vector3(-ROOM_W * 0.5f + 1.2f, 0f, ROOM_D * 0.5f - 1.2f),
            Quaternion.Euler(0, -90, 0), matWood);

        // ── Monitor, teclado, ratón sobre el escritorio esquinero ─────────
        float deskH = 0.74f; // altura superfice Kenney desk
        PlaceFurniture(GUID_MONITOR, "Computer_Monitor", root.transform,
            new Vector3(-ROOM_W * 0.5f + 1.2f, deskH, ROOM_D * 0.5f - 1.0f),
            Quaternion.Euler(0, 180, 0), matDark);
        PlaceFurniture(GUID_KEYBOARD, "Computer_Keyboard", root.transform,
            new Vector3(-ROOM_W * 0.5f + 1.2f, deskH, ROOM_D * 0.5f - 1.5f),
            Quaternion.Euler(0, 180, 0), matDark);
        PlaceFurniture(GUID_MOUSE, "Computer_Mouse", root.transform,
            new Vector3(-ROOM_W * 0.5f + 0.7f, deskH, ROOM_D * 0.5f - 1.5f),
            Quaternion.Euler(0, 180, 0), matDark);

        // ── Librerías en la pared oeste ───────────────────────────────────
        PlaceFurniture(GUID_BOOKCASE_SHUT, "Bookcase_A", root.transform,
            new Vector3(-ROOM_W * 0.5f + 0.35f, 0f, 0.5f),
            Quaternion.Euler(0, 90, 0), matWood);
        PlaceFurniture(GUID_BOOKCASE_OPEN, "Bookcase_B", root.transform,
            new Vector3(-ROOM_W * 0.5f + 0.35f, 0f, -0.5f),
            Quaternion.Euler(0, 90, 0), matWood);
        PlaceFurniture(GUID_BOOKCASE_SHUT, "Bookcase_C", root.transform,
            new Vector3(-ROOM_W * 0.5f + 0.35f, 0f, -1.5f),
            Quaternion.Euler(0, 90, 0), matWood);

        // ── Mesa central ──────────────────────────────────────────────────
        PlaceFurniture(GUID_TABLE, "Table_Center", root.transform,
            new Vector3(1.5f, 0f, 0f),
            Quaternion.identity, matWood);

        // ── Lámpara de techo (centro del cuarto) ──────────────────────────
        PlaceFurniture(GUID_LAMP_CEIL, "Lamp_Ceiling", root.transform,
            new Vector3(0f, ROOM_H - 0.05f, 0f),
            Quaternion.identity, matLight);

        // ── Lámpara de pie (esquina) ──────────────────────────────────────
        PlaceFurniture(GUID_LAMP_FLOOR, "Lamp_Floor", root.transform,
            new Vector3(ROOM_W * 0.5f - 0.5f, 0f, -ROOM_D * 0.5f + 0.5f),
            Quaternion.identity, matLight);

        EditorSceneManager.MarkSceneDirty(active);
        EditorUtility.DisplayDialog("Kenney Room listo",
            "Habitación creada en KenneyRoom.\n\n" +
            "· Ajusta posiciones en el Hierarchy según necesites.\n" +
            "· El Technician_Workstation va contra la pared norte.\n" +
            "Guarda con Ctrl+S.", "OK");
        Debug.Log("[KenneyRoomSetupTool] Habitación Kenney creada.");
    }

    // ── Construye suelo, paredes y techo ──────────────────────────────────
    static void BuildRoom(Transform parent, Material floor, Material wall, Material ceiling)
    {
        // Suelo
        MakePlane("Floor", parent, new Vector3(0, 0, 0),
            new Vector3(ROOM_W, 0.1f, ROOM_D), floor);

        // Techo
        MakePlane("Ceiling", parent, new Vector3(0, ROOM_H, 0),
            new Vector3(ROOM_W, 0.1f, ROOM_D), ceiling);

        // Paredes
        MakePlane("Wall_North", parent, new Vector3(0, ROOM_H * 0.5f,  ROOM_D * 0.5f),
            new Vector3(ROOM_W, ROOM_H, 0.1f), wall);
        MakePlane("Wall_South", parent, new Vector3(0, ROOM_H * 0.5f, -ROOM_D * 0.5f),
            new Vector3(ROOM_W, ROOM_H, 0.1f), wall);
        MakePlane("Wall_West",  parent, new Vector3(-ROOM_W * 0.5f, ROOM_H * 0.5f, 0),
            new Vector3(0.1f, ROOM_H, ROOM_D), wall);
        MakePlane("Wall_East",  parent, new Vector3( ROOM_W * 0.5f, ROOM_H * 0.5f, 0),
            new Vector3(0.1f, ROOM_H, ROOM_D), wall);
    }

    static void MakePlane(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.localPosition = pos;
        go.transform.localScale    = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        // Sin collider en paredes para no interferir con el CharacterController
        Object.DestroyImmediate(go.GetComponent<BoxCollider>());
        Undo.RegisterCreatedObjectUndo(go, "Room piece");
    }

    // ── Instancia un modelo Kenney y le asigna un material ───────────────
    static void PlaceFurniture(string guid, string goName, Transform parent,
        Vector3 pos, Quaternion rot, Material mat)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning($"[KenneyRoom] GUID no encontrado para {goName}: {guid}");
            return;
        }

        // Cargar el primer mesh del FBX
        var assets = AssetDatabase.LoadAllAssetsAtPath(path);
        Mesh mesh = null;
        foreach (var a in assets) if (a is Mesh m) { mesh = m; break; }
        if (mesh == null)
        {
            Debug.LogWarning($"[KenneyRoom] Sin mesh en {path}");
            return;
        }

        var go = new GameObject(goName);
        Undo.RegisterCreatedObjectUndo(go, "Kenney furniture");
        go.transform.SetParent(parent);
        go.transform.SetLocalPositionAndRotation(pos, rot);

        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
    }

    // ── Crea o reutiliza un material URP/Lit de color plano ───────────────
    static Material GetOrCreateMat(string name, Color color)
    {
        string path = $"Assets/Materials/{name}.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            existing.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        Shader lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat = new Material(lit) { name = name };
        mat.SetColor("_BaseColor", color);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
}
