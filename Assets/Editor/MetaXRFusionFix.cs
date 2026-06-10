#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Corrige el error:
///   "Saving Prefab to immutable folder is not allowed:
///    Packages/com.meta.xr.sdk.core/.../FusionAvatar.prefab"
///
/// El postprocessor de Photon Fusion intenta bakear NetworkObject prefabs
/// que encuentre en TODOS los assets — incluyendo los del package inmutable
/// de Meta XR SDK. Al estar en Packages/ (read-only) falla.
///
/// Solución: copiamos los prefabs de Fusion de Meta a Assets/ para que
/// Fusion los procese allí en lugar del package.
///
/// Menú: Tools → TITA → Meta XR → Fix FusionAvatar Prefabs
/// </summary>
[InitializeOnLoad]
public static class MetaXRFusionFix
{
    const string SRC  = "Packages/com.meta.xr.sdk.core/Scripts/BuildingBlocks/" +
                        "MultiplayerBlocks/PhotonFusion/NetworkedAvatar/Prefabs/";
    const string DEST = "Assets/MetaXR/PhotonFusion/Prefabs/";

    static MetaXRFusionFix()
    {
        // Solo actuar si el prefab de destino no existe (evitar bucles)
        if (!System.IO.File.Exists(DEST + "FusionAvatar.prefab"))
            EditorApplication.delayCall += CopyOnce;
    }

    [MenuItem("Tools/TITA/Meta XR/Fix FusionAvatar Prefabs")]
    public static void CopyOnce()
    {
        EditorApplication.delayCall -= CopyOnce;

        if (!AssetDatabase.IsValidFolder("Assets/MetaXR"))
            AssetDatabase.CreateFolder("Assets", "MetaXR");
        if (!AssetDatabase.IsValidFolder("Assets/MetaXR/PhotonFusion"))
            AssetDatabase.CreateFolder("Assets/MetaXR", "PhotonFusion");
        if (!AssetDatabase.IsValidFolder("Assets/MetaXR/PhotonFusion/Prefabs"))
            AssetDatabase.CreateFolder("Assets/MetaXR/PhotonFusion", "Prefabs");

        string[] prefabs = { "FusionAvatar.prefab", "FusionAvatarEntity.prefab" };
        int copied = 0;

        foreach (var p in prefabs)
        {
            string src  = SRC  + p;
            string dest = DEST + p;

            if (!System.IO.File.Exists(dest))
            {
                bool ok = AssetDatabase.CopyAsset(src, dest);
                if (ok)
                {
                    Debug.Log($"[MetaXRFix] Copiado: {p} → Assets/MetaXR/PhotonFusion/Prefabs/");
                    copied++;
                }
                else
                    Debug.LogWarning($"[MetaXRFix] No se encontró {src} — " +
                        "el prefab puede no existir en esta versión del SDK.");
            }
        }

        if (copied > 0)
        {
            AssetDatabase.Refresh();
            Debug.Log($"[MetaXRFix] {copied} prefab(s) copiado(s). El error de Fusion desaparecerá.");
        }
        else
            Debug.Log("[MetaXRFix] Prefabs ya copiados o no encontrados. Sin cambios.");
    }
}
#endif
