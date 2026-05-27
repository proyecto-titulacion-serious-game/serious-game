using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Corrige el Render Pipeline activo: cambia de HDRP (Japan Office) a URP.
/// Menú: Tools → TITA → [URGENTE] Fijar Pipeline → URP
/// </summary>
public static class URPPipelineFixer
{
    const string PC_URP_GUID     = "d44fcf8bb705a4d4caaa2a1d5c25dfc5"; // Assets/URPDefaultResources/PC.asset
    const string MOBILE_URP_GUID = "c81b1b23e1b9dd54880a5dd4395981b9"; // Assets/URPDefaultResources/Mobile.asset

    [MenuItem("Tools/TITA/[URGENTE] Fijar Pipeline → URP")]
    public static void FixPipeline()
    {
        string pcPath     = AssetDatabase.GUIDToAssetPath(PC_URP_GUID);
        string mobilePath = AssetDatabase.GUIDToAssetPath(MOBILE_URP_GUID);

        var pcAsset     = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(pcPath);
        var mobileAsset = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(mobilePath);

        if (pcAsset == null)
        {
            EditorUtility.DisplayDialog("Error",
                $"No se encontró PC_RPAsset.asset (GUID: {PC_URP_GUID}).\n" +
                "Verifica que exista en Assets/Settings/.", "OK");
            return;
        }

        // 1. Pipeline global (GraphicsSettings)
        GraphicsSettings.defaultRenderPipeline = pcAsset;
        EditorUtility.SetDirty(GraphicsSettings.GetGraphicsSettings());

        // 2. Override por nivel de calidad — Unity 6: iterar con SetQualityLevel
        int qualityCount  = QualitySettings.names.Length;
        int savedQuality  = QualitySettings.GetQualityLevel();
        for (int i = 0; i < qualityCount; i++)
        {
            QualitySettings.SetQualityLevel(i, applyExpensiveChanges: false);
            string name   = QualitySettings.names[i];
            bool isMobile = name.ToLower().Contains("mobile") || name.ToLower().Contains("low");
            QualitySettings.renderPipeline = (isMobile && mobileAsset != null) ? mobileAsset : pcAsset;
        }
        QualitySettings.SetQualityLevel(savedQuality, applyExpensiveChanges: false);

        AssetDatabase.SaveAssets();

        // 3. Forzar recompilación del pipeline
        UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();

        string current = GraphicsSettings.defaultRenderPipeline != null
            ? GraphicsSettings.defaultRenderPipeline.name : "NULL";

        Debug.Log($"[URPPipelineFixer] Pipeline global → {current}");
        EditorUtility.DisplayDialog(
            "Pipeline corregido",
            $"Pipeline global: {current}\n" +
            $"Niveles de calidad actualizados: {qualityCount}\n\n" +
            "Ahora ejecuta:\n" +
            "Tools → TITA → Fix Materiales Japan Office (URP)\n" +
            "Tools → TITA → Vol.2 Electronics → Recrear Materiales URP",
            "OK");
    }

    [MenuItem("Tools/TITA/[URGENTE] Fijar Pipeline → URP", true)]
    static bool Validate() => true;
}
