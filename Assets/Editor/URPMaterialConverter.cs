using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Convierte materiales con shader Standard (Built-in) al shader
/// Universal Render Pipeline/Lit, preservando texturas, colores y modos de renderizado.
///
/// Uso: Tools → TITA → Convertir Materiales a URP
/// </summary>
public class URPMaterialConverter : EditorWindow
{
    private List<Material> _candidates = new List<Material>();
    private Vector2        _scroll;
    private bool           _scanned;

    [MenuItem("Tools/TITA/Convertir Materiales a URP")]
    static void Open() => GetWindow<URPMaterialConverter>("Conversor URP").minSize = new Vector2(420, 480);

    // ─────────────────────────────────────────────────────────
    //  GUI
    // ─────────────────────────────────────────────────────────

    void OnGUI()
    {
        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Conversor: Standard  →  Universal Render Pipeline/Lit",
            EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Detecta todos los materiales con shader Standard (Built-in) y los migra a " +
            "URP/Lit preservando texturas, colores, normal maps, oclusión, emisión y modo de superficie.",
            MessageType.Info);
        EditorGUILayout.Space(4);

        // ── Paso 1 ──────────────────────────────────────────
        if (GUILayout.Button("1.  Escanear materiales con shader Standard", GUILayout.Height(28)))
            Scan();

        if (!_scanned) return;

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField($"Encontrados: {_candidates.Count} material(es) para convertir",
            EditorStyles.miniLabel);

        // Lista de materiales
        _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
        foreach (var mat in _candidates)
            EditorGUILayout.ObjectField(mat, typeof(Material), false);
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(4);

        // ── Paso 2 ──────────────────────────────────────────
        if (_candidates.Count == 0)
        {
            EditorGUILayout.HelpBox("No hay materiales Standard en el proyecto. ¡Todo correcto!",
                MessageType.Info);
            return;
        }

        using (new EditorGUI.DisabledScope(_candidates.Count == 0))
        {
            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.55f, 0.85f, 0.55f);
            if (GUILayout.Button($"2.  Convertir {_candidates.Count} material(es) a URP/Lit",
                    GUILayout.Height(32)))
                ConvertAll();
            GUI.backgroundColor = prev;
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox(
            "Nota: los materiales de Skybox (fileID 104/106) se omiten automáticamente " +
            "porque son compatibles con URP sin cambios.",
            MessageType.None);
    }

    // ─────────────────────────────────────────────────────────
    //  Escaneo
    // ─────────────────────────────────────────────────────────

    void Scan()
    {
        _candidates.Clear();
        Shader standard    = Shader.Find("Standard");
        Shader standardSpec = Shader.Find("Standard (Specular setup)");

        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
        foreach (string guid in guids)
        {
            string   path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat  = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;
            if (mat.shader == standard || mat.shader == standardSpec)
                _candidates.Add(mat);
        }

        _scanned = true;
        Repaint();
    }

    // ─────────────────────────────────────────────────────────
    //  Conversión
    // ─────────────────────────────────────────────────────────

    void ConvertAll()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            EditorUtility.DisplayDialog("Error",
                "No se encontró el shader 'Universal Render Pipeline/Lit'.\n" +
                "Verifica que el paquete Universal RP esté instalado.", "OK");
            return;
        }

        int ok = 0, failed = 0;
        var log = new System.Text.StringBuilder();

        try
        {
            for (int i = 0; i < _candidates.Count; i++)
            {
                Material mat = _candidates[i];
                EditorUtility.DisplayProgressBar(
                    "Convirtiendo materiales…",
                    mat.name,
                    (float)i / _candidates.Count);

                try
                {
                    ConvertMaterial(mat, urpLit);
                    EditorUtility.SetDirty(mat);
                    log.AppendLine($"  ✓  {mat.name}");
                    ok++;
                }
                catch (System.Exception ex)
                {
                    log.AppendLine($"  ✗  {mat.name}  —  {ex.Message}");
                    failed++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        Debug.Log($"[URPConverter] Conversión completa — OK: {ok}  |  Errores: {failed}\n{log}");

        string msg = failed == 0
            ? $"Se convirtieron {ok} material(es) correctamente.\nRevisa la consola para el detalle."
            : $"Convertidos: {ok}  |  Con error: {failed}\nRevisa la consola para ver cuáles fallaron.";
        EditorUtility.DisplayDialog("Conversión terminada", msg, "OK");

        Scan(); // actualizar lista
    }

    // ─────────────────────────────────────────────────────────
    //  Lógica de conversión por material
    // ─────────────────────────────────────────────────────────

    static void ConvertMaterial(Material mat, Shader urpLit)
    {
        // ── Leer propiedades ANTES de cambiar el shader ──────
        Color   baseColor      = GetColor(mat, "_Color",          Color.white);
        Texture mainTex        = GetTex(mat, "_MainTex");
        Vector2 mainTexScale   = mat.HasProperty("_MainTex") ? mat.GetTextureScale("_MainTex")  : Vector2.one;
        Vector2 mainTexOffset  = mat.HasProperty("_MainTex") ? mat.GetTextureOffset("_MainTex") : Vector2.zero;

        Texture bumpMap        = GetTex(mat, "_BumpMap");
        float   bumpScale      = GetFloat(mat, "_BumpScale", 1f);

        Texture metallicMap    = GetTex(mat, "_MetallicGlossMap");
        float   metallic       = GetFloat(mat, "_Metallic",       0f);
        float   glossiness     = GetFloat(mat, "_Glossiness",     0.5f);
        float   glossMapScale  = GetFloat(mat, "_GlossMapScale",  1f);

        Texture specGlossMap   = GetTex(mat, "_SpecGlossMap");
        Color   specColor      = GetColor(mat, "_SpecColor",      Color.white);

        Texture occlusionMap   = GetTex(mat, "_OcclusionMap");
        float   occlusion      = GetFloat(mat, "_OcclusionStrength", 1f);

        Texture emissionMap    = GetTex(mat, "_EmissionMap");
        Color   emissionColor  = GetColor(mat, "_EmissionColor",  Color.black);
        bool    hasEmission    = mat.IsKeywordEnabled("_EMISSION");

        float   renderMode     = GetFloat(mat, "_Mode",   0f);   // 0 Opaque 1 Cutout 2 Fade 3 Transparent
        float   cutoff         = GetFloat(mat, "_Cutoff", 0.5f);

        bool isSpecWorkflow = mat.shader.name.Contains("Specular");

        // ── Cambiar shader ───────────────────────────────────
        mat.shader = urpLit;

        // ── Asignar propiedades URP ──────────────────────────

        // Color base y albedo
        mat.SetColor("_BaseColor", baseColor);
        if (mainTex != null)
        {
            mat.SetTexture("_BaseMap", mainTex);
            mat.SetTextureScale("_BaseMap",  mainTexScale);
            mat.SetTextureOffset("_BaseMap", mainTexOffset);
        }

        // Normal map
        if (bumpMap != null)
        {
            mat.SetTexture("_BumpMap", bumpMap);
            mat.SetFloat("_BumpScale", bumpScale);
            mat.EnableKeyword("_NORMALMAP");
        }

        // Flujo de trabajo: Metallic vs Specular
        if (isSpecWorkflow)
        {
            mat.SetFloat("_WorkflowMode", 0); // Specular
            if (specGlossMap != null)
            {
                mat.SetTexture("_SpecGlossMap", specGlossMap);
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            mat.SetColor("_SpecColor", specColor);
            mat.SetFloat("_Smoothness", glossiness);
        }
        else
        {
            mat.SetFloat("_WorkflowMode", 1); // Metallic
            if (metallicMap != null)
            {
                mat.SetTexture("_MetallicGlossMap", metallicMap);
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                mat.SetFloat("_Smoothness", glossMapScale);
            }
            else
            {
                mat.SetFloat("_Metallic",   metallic);
                mat.SetFloat("_Smoothness", glossiness);
            }
        }

        // Oclusión
        if (occlusionMap != null)
        {
            mat.SetTexture("_OcclusionMap", occlusionMap);
            mat.SetFloat("_OcclusionStrength", occlusion);
        }

        // Emisión
        if (hasEmission || emissionMap != null)
        {
            mat.SetTexture("_EmissionMap", emissionMap);
            mat.SetColor("_EmissionColor", emissionColor);
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
        }

        // Modo de superficie (Surface Type)
        ApplySurfaceType(mat, (int)renderMode, cutoff);
    }

    static void ApplySurfaceType(Material mat, int mode, float cutoff)
    {
        switch (mode)
        {
            case 0: // Opaque
                mat.SetFloat("_Surface",   0);
                mat.SetFloat("_AlphaClip", 0);
                mat.SetFloat("_Blend",     0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.DisableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = (int)RenderQueue.Geometry;
                SetBlendOpaqueState(mat);
                break;

            case 1: // Cutout / AlphaTest
                mat.SetFloat("_Surface",   0);
                mat.SetFloat("_AlphaClip", 1);
                mat.SetFloat("_Cutoff",    cutoff);
                mat.EnableKeyword("_ALPHATEST_ON");
                mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = (int)RenderQueue.AlphaTest;
                SetBlendOpaqueState(mat);
                break;

            case 2: // Fade → en URP se mapea a Transparent Alpha
            case 3: // Transparent
                mat.SetFloat("_Surface",   1);
                mat.SetFloat("_AlphaClip", 0);
                mat.SetFloat("_Blend",     0); // Alpha blend
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.renderQueue = (int)RenderQueue.Transparent;
                // Blend state para transparente
                mat.SetInt("_SrcBlend",       (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend",       (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_SrcBlendAlpha",  (int)UnityEngine.Rendering.BlendMode.One);
                mat.SetInt("_DstBlendAlpha",  (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite",         0);
                break;
        }
    }

    static void SetBlendOpaqueState(Material mat)
    {
        mat.SetInt("_SrcBlend",      (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_DstBlend",      (int)UnityEngine.Rendering.BlendMode.Zero);
        mat.SetInt("_SrcBlendAlpha", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_DstBlendAlpha", (int)UnityEngine.Rendering.BlendMode.Zero);
        mat.SetInt("_ZWrite",        1);
    }

    // ─────────────────────────────────────────────────────────
    //  Helpers de lectura segura
    // ─────────────────────────────────────────────────────────

    static Color   GetColor(Material m, string prop, Color   def) =>
        m.HasProperty(prop) ? m.GetColor(prop)   : def;

    static float   GetFloat(Material m, string prop, float   def) =>
        m.HasProperty(prop) ? m.GetFloat(prop)   : def;

    static Texture GetTex  (Material m, string prop)              =>
        m.HasProperty(prop) ? m.GetTexture(prop) : null;
}
