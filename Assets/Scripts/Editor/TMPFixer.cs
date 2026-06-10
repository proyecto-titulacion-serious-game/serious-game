#if UNITY_EDITOR // <--- ESTO ES VITAL PARA QUE NO EXPLOTE EN META QUEST
using UnityEngine;
using UnityEditor;
using TMPro;

/// Repara el error TMP_SubMeshUI.UpdateMaterial NullReferenceException
/// que aparece cuando los recursos esenciales de TMP no están correctamente inicializados.
/// Menú: Tools → TITA → Reparar TMP
public static class TMPFixer
{
    [MenuItem("Tools/TITA/Reparar TMP (NullReference SubMeshUI)")]
    static void FixAll()
    {
        int repaired = 0;

        // 1. Reimportar carpeta TMP si existe en Assets
        string tmpAssetsPath = "Assets/TextMesh Pro";
        if (System.IO.Directory.Exists(tmpAssetsPath))
        {
            AssetDatabase.ImportAsset(tmpAssetsPath, ImportAssetOptions.ImportRecursive);
            Debug.Log("[TMPFixer] Assets/TextMesh Pro reimportado.");
            repaired++;
        }

        // 2. Buscar una fuente TMP válida en el proyecto
        TMP_FontAsset fallbackFont = null;
        string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font != null) { fallbackFont = font; break; }
        }

        if (fallbackFont == null)
        {
            Debug.LogError("[TMPFixer] No hay ningún TMP_FontAsset en el proyecto.\n" +
                           "Solución: Window → TextMeshPro → Import TMP Essential Resources");
            return;
        }

        // 3. Reparar todos los TMP_Text con font null en la escena
        var allText = Resources.FindObjectsOfTypeAll<TMP_Text>();
        foreach (var t in allText)
        {
            if (t == null || t.font != null) continue;
            t.font = fallbackFont;
            EditorUtility.SetDirty(t);
            repaired++;
            Debug.Log($"[TMPFixer] Font reasignado en: {t.gameObject.name}");
        }

        // 4. Forzar que los TMP_SubMeshUI rehagan su material
        var subMeshes = Resources.FindObjectsOfTypeAll<TMP_SubMeshUI>();
        foreach (var sm in subMeshes)
        {
            if (sm == null) continue;
            EditorUtility.SetDirty(sm);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[TMPFixer] Completado — {repaired} elementos procesados. " +
                  "Si el error persiste, reinicia Unity.");
    }

    [MenuItem("Tools/TITA/Limpiar SubMeshUI huérfanos")]
    static void RemoveOrphanSubMeshUI()
    {
        int removed = 0;
        var subMeshes = Resources.FindObjectsOfTypeAll<TMP_SubMeshUI>();
        foreach (var sm in subMeshes)
        {
            if (sm == null) continue;
            // SubMeshUI es válido solo si su padre tiene un TMP_Text
            var parentTMP = sm.GetComponentInParent<TMP_Text>();
            if (parentTMP == null)
            {
                Debug.Log($"[TMPFixer] SubMeshUI huérfano eliminado: {sm.gameObject.name}");
                Undo.DestroyObjectImmediate(sm.gameObject);
                removed++;
            }
        }
        Debug.Log($"[TMPFixer] {removed} SubMeshUI huérfanos eliminados.");
        AssetDatabase.SaveAssets();
    }
}
#endif // <--- CIERRE DEL ESCUDO