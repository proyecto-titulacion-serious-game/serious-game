using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

/// Convierte materiales HDRP/Built-in/2D a URP 3D Lit.
/// Cubre dos carpetas independientes:
///   · Assets/UnityJapanOffice  → "Convertir Japan Office → URP"
///   · Assets/Materials         → "Convertir Workstation Materials → URP 3D"
/// IMPORTANTE: también convierte shaders URP/2D que no responden a luz 3D.
public static class JapanOfficeURPConverter
{
    const string JO_FOLDER  = "Assets/UnityJapanOffice";
    const string MAT_FOLDER = "Assets/Materials";

    [MenuItem("Tools/TITA/Convertir Japan Office → URP")]
    static void ConvertJO() => ConvertFolder(JO_FOLDER, "Japan Office");

    [MenuItem("Tools/TITA/Convertir Workstation Materials → URP 3D")]
    static void ConvertWorkstation() => ConvertFolder(MAT_FOLDER, "Workstation Mat");

    static void ConvertFolder(string folder, string label)
    {
        Shader urpLit    = Shader.Find("Universal Render Pipeline/Lit");
        Shader urpUnlit  = Shader.Find("Universal Render Pipeline/Unlit");

        if (urpLit == null)
        {
            EditorUtility.DisplayDialog("Error",
                "No se encontró el shader 'Universal Render Pipeline/Lit'.\n" +
                "Verifica que URP esté instalado.", "OK");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { folder });
        int converted = 0, skipped = 0;

        try
        {
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                EditorUtility.DisplayProgressBar(
                    "Convirtiendo Japan Office → URP",
                    path, (float)i / guids.Length);

                Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) { skipped++; continue; }

                // Ya usa URP 3D Lit/Unlit → omitir (NO omitir shaders 2D, que tampoco funcionan en escenas 3D)
                if (mat.shader != null && IsURP3DShader(mat.shader.name))
                {
                    skipped++;
                    continue;
                }

                ConvertMaterial(mat, urpLit, urpUnlit);
                EditorUtility.SetDirty(mat);
                converted++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog($"Conversión {label} completa",
            $"Carpeta: {folder}\nConvertidos: {converted}\nOmitidos (ya URP): {skipped}\n\n" +
            "Revisa en la escena. Vidrios y materiales con transparencia pueden necesitar ajuste manual.",
            "OK");

        Debug.Log($"[JapanOfficeURPConverter] {label} — {converted} convertidos, {skipped} omitidos.");
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Conversión de un material HDRP → URP/Lit
    // ─────────────────────────────────────────────────────────────────────

    static void ConvertMaterial(Material mat, Shader urpLit, Shader urpUnlit)
    {
        // ── Leer propiedades HDRP antes de cambiar el shader ──────────────
        Texture baseMap    = GetTex(mat, "_BaseColorMap", "_MainTex");
        Color   baseColor  = GetColor(mat, "_BaseColor", "_Color", Color.white);
        Texture normalMap  = GetTex(mat, "_NormalMap");
        float   normalScale = GetFloat(mat, "_NormalScale", 1f);
        Texture emissiveMap = GetTex(mat, "_EmissiveColorMap");
        Color   emissiveColor = GetColor(mat, "_EmissiveColor", "_EmissionColor", Color.black);
        float   metallic   = GetFloat(mat, "_Metallic", 0f);
        float   smoothness = GetFloat(mat, "_Smoothness", 0.5f);
        float   alphaCutoff = GetFloat(mat, "_AlphaCutoff", 0.5f);

        // Detectar superficie HDRP: _SurfaceType 1 = transparente, _AlphaCutoffEnable 1 = cutout
        float surfaceType    = GetFloat(mat, "_SurfaceType", 0f);
        float alphaCutEnable = GetFloat(mat, "_AlphaCutoffEnable", 0f);

        // Detectar materiales de vidrio/unlit por nombre de shader
        bool isGlass = mat.shader != null &&
            (mat.shader.name.ToLower().Contains("glass") ||
             mat.name.ToLower().Contains("glass") ||
             mat.name.ToLower().Contains("transparent"));

        // ── Cambiar shader ────────────────────────────────────────────────
        mat.shader = isGlass ? urpUnlit : urpLit;

        // ── Asignar propiedades URP ───────────────────────────────────────
        if (baseMap  != null) mat.SetTexture("_BaseMap",  baseMap);
        mat.SetColor("_BaseColor", baseColor);

        if (normalMap != null)
        {
            mat.SetTexture("_BumpMap", normalMap);
            mat.SetFloat("_BumpScale", normalScale);
            mat.EnableKeyword("_NORMALMAP");
        }

        if (emissiveMap != null)
        {
            mat.SetTexture("_EmissionMap", emissiveMap);
            mat.SetColor("_EmissionColor", emissiveColor);
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }
        else if (emissiveColor != Color.black && emissiveColor.maxColorComponent > 0.01f)
        {
            mat.SetColor("_EmissionColor", emissiveColor);
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        mat.SetFloat("_Metallic",   metallic);
        mat.SetFloat("_Smoothness", smoothness);

        // ── Transparencia ────────────────────────────────────────────────
        bool isTransparent = surfaceType > 0.5f || isGlass;
        bool isCutout      = alphaCutEnable > 0.5f && !isTransparent;

        if (isTransparent)
        {
            mat.SetFloat("_Surface", 1f);       // Transparent
            mat.SetFloat("_Blend",   0f);       // Alpha
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite",   0f);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
        }
        else if (isCutout)
        {
            mat.SetFloat("_AlphaClip", 1f);
            mat.SetFloat("_Cutoff",    alphaCutoff);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.SetOverrideTag("RenderType", "TransparentCutout");
        }
        else
        {
            mat.SetFloat("_Surface", 0f);       // Opaque
            mat.SetFloat("_ZWrite",  1f);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
            mat.SetOverrideTag("RenderType", "Opaque");
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Helpers para leer propiedades con fallback entre nombres HDRP y otros
    // ─────────────────────────────────────────────────────────────────────

    static Texture GetTex(Material mat, params string[] names)
    {
        foreach (var n in names)
            if (mat.HasProperty(n)) { var t = mat.GetTexture(n); if (t != null) return t; }
        return null;
    }

    static Color GetColor(Material mat, params string[] names)
    {
        foreach (var n in names)
            if (mat.HasProperty(n)) return mat.GetColor(n);
        return Color.white;
    }

    static Color GetColor(Material mat, string a, string b, Color fallback)
    {
        if (mat.HasProperty(a)) return mat.GetColor(a);
        if (mat.HasProperty(b)) return mat.GetColor(b);
        return fallback;
    }

    static float GetFloat(Material mat, string name, float fallback)
    {
        if (mat.HasProperty(name)) return mat.GetFloat(name);
        return fallback;
    }

    // URP 3D shaders que ya son correctos para escenas 3D (no re-convertir)
    static bool IsURP3DShader(string shaderName)
    {
        if (string.IsNullOrEmpty(shaderName)) return false;
        // Excluir explícitamente los shaders 2D aunque digan "Universal Render Pipeline"
        if (shaderName.Contains("/2D/") || shaderName.Contains("Sprite") || shaderName.Contains("Mesh2D"))
            return false;
        return shaderName.StartsWith("Universal Render Pipeline") ||
               shaderName.StartsWith("Packages/com.unity.render-pipelines.universal");
    }
}
