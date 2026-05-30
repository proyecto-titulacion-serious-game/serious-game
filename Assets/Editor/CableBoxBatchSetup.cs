#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// Punto de entrada para batch mode.
/// Uso:
///   Unity.exe -batchmode -nographics -projectPath "<ruta>" -executeMethod CableBoxBatchSetup.Run -quit
public static class CableBoxBatchSetup
{
    public static void Run()
    {
        Debug.Log("[CableBoxBatchSetup] Iniciando generación de prefabs...");

        // 1. Cable_Jumper.prefab
        var cablePrefab = CableBoxSetupTool.BuildCablePrefab();
        Debug.Log("[CableBoxBatchSetup] Cable_Jumper.prefab ✅");

        // 2. CableBox_VR.prefab (standalone)
        CableBoxSetupTool.SaveStandalonePrefab(cablePrefab);
        Debug.Log("[CableBoxBatchSetup] CableBox_VR.prefab ✅");

        // 3. Parche en ExplorerWorkstation.prefab
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CableBoxBatchSetup] Completado.");
    }
}
#endif
