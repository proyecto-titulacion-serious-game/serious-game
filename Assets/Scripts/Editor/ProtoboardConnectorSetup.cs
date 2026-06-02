using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Añade <see cref="ProtoboardConnector"/> a todos los componentes colocables (LED, resistencia,
/// jumper, capacitor) — tanto a los prefabs de Assets/Prefabs/Delivered/ (para que las copias
/// spawneadas lo tengan) como a las instancias ya presentes en la escena activa. El conector
/// engancha las patas del componente al nodo más cercano (slot o pin) en el Reto 4.
///
/// VoltageSource se omite (es condición de frontera, no se engancha).
///
/// Menú: Tools → TITA → Reto 4 → Añadir Conectores a Componentes
/// </summary>
public static class ProtoboardConnectorSetup
{
    const string DELIVERED_FOLDER = "Assets/Prefabs/Delivered";

    [MenuItem("Tools/TITA/Reto 4/Añadir Conectores a Componentes")]
    public static void AddConnectors()
    {
        var sb = new StringBuilder();
        int prefabsMod = 0, sceneMod = 0;

        // ── 1. Prefabs en Assets/Prefabs/Delivered/ ──────────────────────────
        if (AssetDatabase.IsValidFolder(DELIVERED_FOLDER))
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { DELIVERED_FOLDER }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var contents = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var comp = contents.GetComponent<ElectricalComponent>()
                            ?? contents.GetComponentInChildren<ElectricalComponent>(true);

                    if (comp == null || comp is VoltageSource) continue;
                    // El conector debe ir en el MISMO GO que el ElectricalComponent
                    if (comp.GetComponent<ProtoboardConnector>() != null) continue;

                    comp.gameObject.AddComponent<ProtoboardConnector>();
                    PrefabUtility.SaveAsPrefabAsset(contents, path);
                    prefabsMod++;
                    sb.AppendLine($"  • prefab: {System.IO.Path.GetFileName(path)}");
                }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
            }
        }
        else
        {
            sb.AppendLine($"  (carpeta {DELIVERED_FOLDER} no encontrada — se omiten prefabs)");
        }

        // ── 2. Instancias en la escena activa ────────────────────────────────
        foreach (var comp in Object.FindObjectsByType<ElectricalComponent>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (comp is VoltageSource) continue;
            if (comp.GetComponent<ProtoboardConnector>() != null) continue;

            Undo.AddComponent<ProtoboardConnector>(comp.gameObject);
            sceneMod++;
        }

        if (sceneMod > 0)
            EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        AssetDatabase.SaveAssets();

        EditorUtility.DisplayDialog("Conectores añadidos",
            $"Prefabs modificados: {prefabsMod}\n" +
            $"Instancias en escena: {sceneMod}\n\n" +
            (sb.Length > 0 ? sb.ToString() + "\n" : "") +
            "Cada componente engancha sus patas al slot/pin más cercano al simular.\n" +
            "Ajusta 'snapRadius' o asigna leadA/leadB manualmente si las patas no caen bien.\n\n" +
            "Para los cables jumper: añádeles el componente Jumper + ProtoboardConnector.", "OK");

        Debug.Log($"[ProtoboardConnectorSetup] Prefabs: {prefabsMod}, escena: {sceneMod}.");
    }
}
