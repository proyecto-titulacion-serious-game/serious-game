using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// Soluciona los objetos blancos/rosados del Technician_Workstation:
///   1. Revierte el FBX importer de circuit/models de vuelta a materiales embebidos
///   2. Asigna explícitamente los Mat_* correctos a cada MeshRenderer del Workstation
/// Menu: Tools/TITA/Fix Workstation Materials
public static class WorkstationMaterialFixer
{
    const string CIRCUIT_MODELS = "Assets/circuit/models";
    const string MAT_FOLDER     = "Assets/Materials";

    // Mapeo nombre-de-objeto → nombre-de-material
    static readonly (string keyword, string matName)[] MatMap = new[]
    {
        ("R100",          "Mat_CompR100"),
        ("R220",          "Mat_CompR220"),
        ("R330",          "Mat_CompR330"),
        ("R_Vertical",    "Mat_CompR330"),
        ("ArduinoPin",    "Mat_CompArduinoPin"),
        ("Arduino",       "Mat_CompArduinoPin"),
        ("Comp_Cap",      "Mat_CompCap"),
        ("CompCap",       "Mat_CompCap"),
        ("Capacitor",     "Mat_CompCap"),
        ("CompLED",       "Mat_CompLED"),
        ("Comp_LED",      "Mat_CompLED"),
        ("LED",           "Mat_CompLED"),
        ("Clipboard_Board","Mat_ClipboardBoard"),
        ("Clipboard",     "Mat_ClipboardBoard"),
        ("ClipMetal",     "Mat_ClipMetal"),
        ("ClipboardPaper","Mat_ClipboardPaper"),
        ("Manual_Book",   "Mat_ManualBook"),
        ("ManualBook",    "Mat_ManualBook"),
        ("Manual",        "Mat_ManualBook"),
        ("ScrollParchment","Mat_ScrollParchment"),
        ("ScrollRoll",    "Mat_ScrollRoll"),
        ("ScrollCap",     "Mat_ScrollCap"),
        ("Scroll_Label",  "Mat_ClipboardPaper"),
        ("Sending_Tray",  "Mat_SendingTray"),
        ("SendingTray",   "Mat_SendingTray"),
        ("Tray_Slot",     "Mat_SendingTray"),
        ("DeskSurface",   "Mat_DeskSurface"),
        ("Desk_Surface",  "Mat_DeskSurface"),
        ("DeskComp",      "Mat_DeskComp"),
        ("ExplorerTray",  "Mat_ExplorerTray"),
    };

    [MenuItem("Tools/TITA/Fix Workstation Materials")]
    static void Fix()
    {
        // ── 1. Revertir FBX importers a materiales embebidos ─────────────
        int fbxReverted = RevertFBXImporters();

        // ── 2. Buscar Technician_Workstation en la escena activa ──────────
        var ws = GameObject.Find("Technician_Workstation");
        if (ws == null)
        {
            EditorUtility.DisplayDialog("Error",
                "No se encontró 'Technician_Workstation' en la escena.\n" +
                "Ejecuta primero Setup Desk Kenney.", "OK");
            return;
        }

        int assigned = 0;
        int missing  = 0;
        int skipped  = 0;

        var renderers = ws.GetComponentsInChildren<MeshRenderer>(true);
        foreach (var mr in renderers)
        {
            string matName = FindMatName(mr.gameObject.name);
            if (matName == null) { skipped++; continue; }

            Material mat = LoadMat(matName);
            if (mat == null) { missing++; continue; }

            // Rellena todos los slots (algunos meshes tienen submateriales)
            var slots = new Material[Mathf.Max(1, mr.sharedMaterials.Length)];
            for (int i = 0; i < slots.Length; i++) slots[i] = mat;

            Undo.RecordObject(mr, "Assign Workstation Material");
            mr.sharedMaterials = slots;
            EditorUtility.SetDirty(mr);
            assigned++;
            Debug.Log($"[WorkstationMaterialFixer] {mr.gameObject.name} → {matName}");
        }

        // ── 3. Marcar escena sucia ────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        string msg = $"FBX revertidos: {fbxReverted}\n" +
                     $"Materiales asignados: {assigned}\n" +
                     $"Sin keyword (sin asignar): {skipped}\n" +
                     $"Materiales no encontrados: {missing}\n\n" +
                     "Revisa la Console para ver qué objetos recibieron material.\n" +
                     "Guarda con Ctrl+S.";
        EditorUtility.DisplayDialog("Fix Workstation Materials", msg, "OK");
        Debug.Log($"[WorkstationMaterialFixer] TOTAL → asignados:{assigned}  skipped:{skipped}  missing:{missing}");
    }

    [MenuItem("Tools/TITA/Diagnosticar Workstation (sin cambios)")]
    static void Diagnose()
    {
        var ws = GameObject.Find("Technician_Workstation");
        if (ws == null) { Debug.LogWarning("[WorkstationMaterialFixer] Technician_Workstation no encontrado."); return; }

        var renderers = ws.GetComponentsInChildren<MeshRenderer>(true);
        Debug.Log($"[WorkstationMaterialFixer] === DIAGNÓSTICO ({renderers.Length} renderers) ===");
        foreach (var mr in renderers)
        {
            string matched = FindMatName(mr.gameObject.name) ?? "(sin keyword)";
            string current = mr.sharedMaterial != null ? mr.sharedMaterial.name : "NULL";
            string shader  = mr.sharedMaterial?.shader?.name ?? "NULL";
            Debug.Log($"  {mr.gameObject.name}  |  keyword→{matched}  |  mat actual: {current}  |  shader: {shader}");
        }
        Debug.Log("[WorkstationMaterialFixer] === FIN DIAGNÓSTICO ===");
    }

    // ── Revierte el importer de cada FBX en circuit/models a Embedded ────
    static int RevertFBXImporters()
    {
        int count = 0;
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { CIRCUIT_MODELS });
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var imp = AssetImporter.GetAtPath(path) as ModelImporter;
            if (imp == null) continue;

            // InPrefab = materiales embebidos en el FBX (único modo válido en Unity 6)
            bool needsRevert = imp.materialLocation != ModelImporterMaterialLocation.InPrefab;
            if (!needsRevert) continue;

            imp.materialLocation   = ModelImporterMaterialLocation.InPrefab;
            imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            imp.SaveAndReimport();
            count++;
        }
        return count;
    }

    // ── Busca el primer keyword que coincida con el nombre del objeto ─────
    static string FindMatName(string goName)
    {
        foreach (var (keyword, matName) in MatMap)
            if (goName.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return matName;
        return null;
    }

    // ── Carga un Material desde Assets/Materials/ ─────────────────────────
    static Material LoadMat(string name)
    {
        string path = $"{MAT_FOLDER}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
            Debug.LogWarning($"[WorkstationMaterialFixer] No se encontró: {path}");
        return mat;
    }
}
