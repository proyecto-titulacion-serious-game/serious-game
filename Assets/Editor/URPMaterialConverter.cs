using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Pestaña 0: Standard (Built-in) → URP/Lit
/// Pestaña 1: HDRP/Lit → URP/Lit   (para assets como Unity Japan Office)
/// Uso: Tools → TITA → Convertir Materiales a URP
/// </summary>
public class URPMaterialConverter : EditorWindow
{
    // ── Tab 0: Standard ─────────────────────────────────────────
    private List<Material> _candidates = new List<Material>();
    private Vector2        _scroll;
    private bool           _scanned;

    // ── Tab 1: HDRP ─────────────────────────────────────────────
    private List<Material> _hdrpCandidates = new List<Material>();
    private Vector2        _hdrpScroll;
    private bool           _hdrpScanned;

    private int _tab;
    private static readonly string[] _tabs = { "Standard → URP", "HDRP → URP" };

    [MenuItem("Tools/TITA/Convertir Materiales a URP")]
    static void Open() => GetWindow<URPMaterialConverter>("Conversor URP").minSize = new Vector2(440, 540);

    // ─────────────────────────────────────────────────────────────
    //  GUI
    // ─────────────────────────────────────────────────────────────

    void OnGUI()
    {
        EditorGUILayout.Space(6);
        _tab = GUILayout.Toolbar(_tab, _tabs);
        EditorGUILayout.Space(6);

        if (_tab == 0) DrawStandardTab();
        else           DrawHDRPTab();
    }

    // ─────────────────────────────────────────────────────────────
    //  TAB 0 — Standard → URP
    // ─────────────────────────────────────────────────────────────

    void DrawStandardTab()
    {
        EditorGUILayout.LabelField("Conversor: Standard  →  Universal Render Pipeline/Lit",
            EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Detecta todos los materiales con shader Standard (Built-in) y los migra a " +
            "URP/Lit preservando texturas, colores, normal maps, oclusión, emisión y modo de superficie.",
            MessageType.Info);
        EditorGUILayout.Space(4);

        if (GUILayout.Button("1.  Escanear materiales Standard", GUILayout.Height(28)))
            Scan();
        if (!_scanned) return;

        EditorGUILayout.LabelField($"Encontrados: {_candidates.Count} material(es)", EditorStyles.miniLabel);
        _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
        foreach (var mat in _candidates)
            EditorGUILayout.ObjectField(mat, typeof(Material), false);
        EditorGUILayout.EndScrollView();

        if (_candidates.Count == 0)
        {
            EditorGUILayout.HelpBox("No hay materiales Standard. ¡Todo correcto!", MessageType.Info);
            return;
        }

        using (new EditorGUI.DisabledScope(_candidates.Count == 0))
        {
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.55f, 0.85f, 0.55f);
            if (GUILayout.Button($"2.  Convertir {_candidates.Count} material(es) a URP/Lit", GUILayout.Height(32)))
                ConvertAll();
            GUI.backgroundColor = prev;
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox(
            "Los materiales Skybox se omiten (son compatibles con URP sin cambios).",
            MessageType.None);
    }

    // ─────────────────────────────────────────────────────────────
    //  TAB 1 — HDRP → URP
    // ─────────────────────────────────────────────────────────────

    void DrawHDRPTab()
    {
        EditorGUILayout.LabelField("Conversor: HDRP  →  Universal Render Pipeline/Lit",
            EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Detecta materiales con shaders HDRP que aparecen en rosa/magenta en proyectos URP " +
            "(ej. Unity Japan Office) y los migra a URP/Lit.\n\n" +
            "Se preservan: albedo (_BaseColorMap), normal map, AO (_AOMap), emisión y colores.\n" +
            "Limitación: el MaskMap de HDRP (packing MADS) no se repaquetiza; metalicidad y " +
            "suavidad se toman de los valores float del material.",
            MessageType.Info);
        EditorGUILayout.Space(4);

        if (GUILayout.Button("1.  Escanear materiales HDRP (rosa)", GUILayout.Height(28)))
            ScanHDRP();
        if (!_hdrpScanned) return;

        EditorGUILayout.LabelField($"Encontrados: {_hdrpCandidates.Count} material(es) HDRP", EditorStyles.miniLabel);
        _hdrpScroll = EditorGUILayout.BeginScrollView(_hdrpScroll, GUILayout.ExpandHeight(true));
        foreach (var mat in _hdrpCandidates)
            EditorGUILayout.ObjectField(mat, typeof(Material), false);
        EditorGUILayout.EndScrollView();

        if (_hdrpCandidates.Count == 0)
        {
            EditorGUILayout.HelpBox("No se encontraron materiales HDRP con shader faltante.", MessageType.Info);
            return;
        }

        using (new EditorGUI.DisabledScope(_hdrpCandidates.Count == 0))
        {
            var prev = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.85f, 0.70f, 0.30f);
            if (GUILayout.Button($"2.  Convertir {_hdrpCandidates.Count} material(es) HDRP → URP/Lit",
                    GUILayout.Height(32)))
                ConvertHDRPAll();
            GUI.backgroundColor = prev;
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Standard: Escaneo y conversión
    // ─────────────────────────────────────────────────────────────

    void Scan()
    {
        _candidates.Clear();
        Shader standard    = Shader.Find("Standard");
        Shader standardSpec = Shader.Find("Standard (Specular setup)");

        foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets" }))
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

    void ConvertAll()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            EditorUtility.DisplayDialog("Error",
                "No se encontró 'Universal Render Pipeline/Lit'.\nVerifica que el paquete Universal RP esté instalado.", "OK");
            return;
        }

        int ok = 0, failed = 0;
        var log = new System.Text.StringBuilder();
        try
        {
            for (int i = 0; i < _candidates.Count; i++)
            {
                var mat = _candidates[i];
                EditorUtility.DisplayProgressBar("Convirtiendo materiales…", mat.name, (float)i / _candidates.Count);
                try { ConvertMaterial(mat, urpLit); EditorUtility.SetDirty(mat); log.AppendLine($"  ✓  {mat.name}"); ok++; }
                catch (System.Exception ex) { log.AppendLine($"  ✗  {mat.name} — {ex.Message}"); failed++; }
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        finally { EditorUtility.ClearProgressBar(); }

        Debug.Log($"[URPConverter] Standard→URP: OK={ok} | Errores={failed}\n{log}");
        EditorUtility.DisplayDialog("Conversión terminada",
            $"Convertidos: {ok} | Errores: {failed}\nRevisa la consola para el detalle.", "OK");
        Scan();
    }

    // ─────────────────────────────────────────────────────────────
    //  HDRP: Escaneo y conversión
    // ─────────────────────────────────────────────────────────────

    void ScanHDRP()
    {
        _hdrpCandidates.Clear();
        string[] matGuids = AssetDatabase.FindAssets("t:Material", new string[] { "Assets" });
        foreach (string guid in matGuids)
        {
            string   path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat  = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            // Saltar materiales ya en URP
            string sName = mat.shader.name;
            if (sName.Contains("Universal Render Pipeline") || sName.StartsWith("URP/")) continue;

            // Detectar HDRP leyendo directamente el YAML del .mat (no depende del nombre del shader)
            try
            {
                string yaml = File.ReadAllText(path);
                if (yaml.Contains("_HdrpVersion"))
                    _hdrpCandidates.Add(mat);
            }
            catch { /* skip archivos no legibles */ }
        }

        Debug.Log("[URPConverter] ScanHDRP: " + _hdrpCandidates.Count + " HDRP encontrados de " + matGuids.Length + " totales.");
        _hdrpScanned = true;
        Repaint();
    }

    void ConvertHDRPAll()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            EditorUtility.DisplayDialog("Error", "No se encontró 'Universal Render Pipeline/Lit'.", "OK");
            return;
        }

        int ok = 0, failed = 0;
        var log = new System.Text.StringBuilder();
        try
        {
            for (int i = 0; i < _hdrpCandidates.Count; i++)
            {
                var mat  = _hdrpCandidates[i];
                string path = AssetDatabase.GetAssetPath(mat);
                EditorUtility.DisplayProgressBar("Convirtiendo HDRP…", mat.name, (float)i / _hdrpCandidates.Count);
                try
                {
                    ConvertHDRPMaterial(mat, path, urpLit);
                    EditorUtility.SetDirty(mat);
                    log.AppendLine($"  ✓  {mat.name}");
                    ok++;
                }
                catch (System.Exception ex)
                {
                    log.AppendLine($"  ✗  {mat.name} — {ex.Message}");
                    failed++;
                }
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        finally { EditorUtility.ClearProgressBar(); }

        Debug.Log($"[URPConverter] HDRP→URP: OK={ok} | Errores={failed}\n{log}");
        EditorUtility.DisplayDialog("Conversión HDRP terminada",
            $"Convertidos: {ok} | Errores: {failed}\nRevisa la consola para detalles.", "OK");
        ScanHDRP();
    }

    // ─────────────────────────────────────────────────────────────
    //  Lógica de conversión HDRP → URP/Lit
    // ─────────────────────────────────────────────────────────────

    static void ConvertHDRPMaterial(Material mat, string assetPath, Shader urpLit)
    {
        string yaml = File.ReadAllText(assetPath);

        // ── Leer texturas ────────────────────────────────────────
        Texture2D baseColorMap = LoadTexFromYaml(yaml, "_BaseColorMap");
        Texture2D mainTexFb    = LoadTexFromYaml(yaml, "_MainTex");        // fallback para custom shaders
        Texture2D normalMap    = LoadTexFromYaml(yaml, "_NormalMap");
        Texture2D aoMap        = LoadTexFromYaml(yaml, "_AOMap");
        Texture2D emissiveMap  = LoadTexFromYaml(yaml, "_EmissiveColorMap");

        // ── Leer floats ──────────────────────────────────────────
        float metallic          = ParseYamlFloat(yaml, "_Metallic",          0f);
        float smoothness        = ParseYamlFloat(yaml, "_SmoothnessRemapMax", 0.5f);
        float normalScale       = ParseYamlFloat(yaml, "_NormalScale",        1f);
        float surfaceType       = ParseYamlFloat(yaml, "_SurfaceType",        0f);
        float alphaCutoff       = ParseYamlFloat(yaml, "_AlphaCutoff",        0.5f);
        float alphaCutoffEnable = ParseYamlFloat(yaml, "_AlphaCutoffEnable",  0f);

        // ── Leer colores ─────────────────────────────────────────
        Color baseColor     = ParseYamlColor(yaml, "_BaseColor",     Color.white);
        Color emissiveColor = ParseYamlColor(yaml, "_EmissiveColor", Color.black);

        // ── Cambiar shader ───────────────────────────────────────
        mat.shader = urpLit;

        // Color base + albedo
        mat.SetColor("_BaseColor", baseColor);
        Texture2D albedo = baseColorMap != null ? baseColorMap : mainTexFb;
        if (albedo != null) mat.SetTexture("_BaseMap", albedo);

        // Normal map
        if (normalMap != null)
        {
            mat.SetTexture("_BumpMap", normalMap);
            mat.SetFloat("_BumpScale", normalScale);
            mat.EnableKeyword("_NORMALMAP");
        }

        // Oclusión ambiental
        if (aoMap != null)
        {
            mat.SetTexture("_OcclusionMap", aoMap);
            mat.SetFloat("_OcclusionStrength", 1f);
        }

        // Metalicidad y suavidad (los valores float, MaskMap no se repaquetiza)
        mat.SetFloat("_Metallic",   Mathf.Clamp01(metallic));
        mat.SetFloat("_Smoothness", Mathf.Clamp01(smoothness));

        // Emisión
        bool hasEmission = emissiveMap != null || emissiveColor.maxColorComponent > 0.01f;
        if (hasEmission)
        {
            if (emissiveMap != null) mat.SetTexture("_EmissionMap", emissiveMap);
            mat.SetColor("_EmissionColor", emissiveColor);
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
        }

        // Modo de superficie
        bool isTransparent = surfaceType >= 1f;
        bool isAlphaClip   = alphaCutoffEnable >= 1f;
        int  mode          = isTransparent ? 3 : isAlphaClip ? 1 : 0;
        ApplySurfaceType(mat, mode, alphaCutoff);
    }

    // ─────────────────────────────────────────────────────────────
    //  Lógica de conversión Standard → URP/Lit (existente)
    // ─────────────────────────────────────────────────────────────

    static void ConvertMaterial(Material mat, Shader urpLit)
    {
        Color   baseColor     = GetColor(mat, "_Color",          Color.white);
        Texture mainTex       = GetTex(mat, "_MainTex");
        Vector2 mainTexScale  = mat.HasProperty("_MainTex") ? mat.GetTextureScale("_MainTex")  : Vector2.one;
        Vector2 mainTexOffset = mat.HasProperty("_MainTex") ? mat.GetTextureOffset("_MainTex") : Vector2.zero;

        Texture bumpMap       = GetTex(mat, "_BumpMap");
        float   bumpScale     = GetFloat(mat, "_BumpScale", 1f);

        Texture metallicMap   = GetTex(mat, "_MetallicGlossMap");
        float   metallic      = GetFloat(mat, "_Metallic",      0f);
        float   glossiness    = GetFloat(mat, "_Glossiness",    0.5f);
        float   glossMapScale = GetFloat(mat, "_GlossMapScale", 1f);

        Texture specGlossMap  = GetTex(mat, "_SpecGlossMap");
        Color   specColor     = GetColor(mat, "_SpecColor", Color.white);

        Texture occlusionMap  = GetTex(mat, "_OcclusionMap");
        float   occlusion     = GetFloat(mat, "_OcclusionStrength", 1f);

        Texture emissionMap   = GetTex(mat, "_EmissionMap");
        Color   emissionColor = GetColor(mat, "_EmissionColor", Color.black);
        bool    hasEmission   = mat.IsKeywordEnabled("_EMISSION");

        float renderMode = GetFloat(mat, "_Mode",   0f);
        float cutoff     = GetFloat(mat, "_Cutoff", 0.5f);

        bool isSpecWorkflow = mat.shader.name.Contains("Specular");

        mat.shader = urpLit;

        mat.SetColor("_BaseColor", baseColor);
        if (mainTex != null)
        {
            mat.SetTexture("_BaseMap", mainTex);
            mat.SetTextureScale("_BaseMap",  mainTexScale);
            mat.SetTextureOffset("_BaseMap", mainTexOffset);
        }

        if (bumpMap != null)
        {
            mat.SetTexture("_BumpMap", bumpMap);
            mat.SetFloat("_BumpScale", bumpScale);
            mat.EnableKeyword("_NORMALMAP");
        }

        if (isSpecWorkflow)
        {
            mat.SetFloat("_WorkflowMode", 0);
            if (specGlossMap != null) { mat.SetTexture("_SpecGlossMap", specGlossMap); mat.EnableKeyword("_METALLICSPECGLOSSMAP"); }
            mat.SetColor("_SpecColor", specColor);
            mat.SetFloat("_Smoothness", glossiness);
        }
        else
        {
            mat.SetFloat("_WorkflowMode", 1);
            if (metallicMap != null) { mat.SetTexture("_MetallicGlossMap", metallicMap); mat.EnableKeyword("_METALLICSPECGLOSSMAP"); mat.SetFloat("_Smoothness", glossMapScale); }
            else { mat.SetFloat("_Metallic", metallic); mat.SetFloat("_Smoothness", glossiness); }
        }

        if (occlusionMap != null) { mat.SetTexture("_OcclusionMap", occlusionMap); mat.SetFloat("_OcclusionStrength", occlusion); }

        if (hasEmission || emissionMap != null)
        {
            mat.SetTexture("_EmissionMap", emissionMap);
            mat.SetColor("_EmissionColor", emissionColor);
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
        }

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
            case 1: // AlphaTest / Cutout
                mat.SetFloat("_Surface",   0);
                mat.SetFloat("_AlphaClip", 1);
                mat.SetFloat("_Cutoff",    cutoff);
                mat.EnableKeyword("_ALPHATEST_ON");
                mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = (int)RenderQueue.AlphaTest;
                SetBlendOpaqueState(mat);
                break;
            default: // Fade / Transparent
                mat.SetFloat("_Surface",   1);
                mat.SetFloat("_AlphaClip", 0);
                mat.SetFloat("_Blend",     0);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.renderQueue = (int)RenderQueue.Transparent;
                mat.SetInt("_SrcBlend",      (int)BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend",      (int)BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_SrcBlendAlpha", (int)BlendMode.One);
                mat.SetInt("_DstBlendAlpha", (int)BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite",        0);
                break;
        }
    }

    static void SetBlendOpaqueState(Material mat)
    {
        mat.SetInt("_SrcBlend",      (int)BlendMode.One);
        mat.SetInt("_DstBlend",      (int)BlendMode.Zero);
        mat.SetInt("_SrcBlendAlpha", (int)BlendMode.One);
        mat.SetInt("_DstBlendAlpha", (int)BlendMode.Zero);
        mat.SetInt("_ZWrite",        1);
    }

    // ─────────────────────────────────────────────────────────────
    //  Helpers de lectura segura (Standard)
    // ─────────────────────────────────────────────────────────────

    static Color   GetColor(Material m, string p, Color   def) => m.HasProperty(p) ? m.GetColor(p)   : def;
    static float   GetFloat(Material m, string p, float   def) => m.HasProperty(p) ? m.GetFloat(p)   : def;
    static Texture GetTex  (Material m, string p)              => m.HasProperty(p) ? m.GetTexture(p) : null;

    // ─────────────────────────────────────────────────────────────
    //  Helpers de parseo YAML (HDRP)
    // ─────────────────────────────────────────────────────────────

    static Texture2D LoadTexFromYaml(string yaml, string propName)
    {
        // Busca:   - _PropName:\n        m_Texture: {fileID: N, guid: GUID, type: N}
        string pattern = propName + @":\s*\n\s*m_Texture:\s*\{[^}]*guid:\s*([a-f0-9]{32})";
        var m = Regex.Match(yaml, pattern);
        if (!m.Success) return null;

        string guid = m.Groups[1].Value;
        if (guid == "00000000000000000000000000000000") return null;

        string path = AssetDatabase.GUIDToAssetPath(guid);
        return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static float ParseYamlFloat(string yaml, string propName, float def)
    {
        // Busca:   - _PropName: 0.5
        var m = Regex.Match(yaml, $@"-\s*{Regex.Escape(propName)}:\s*([-\d\.eE]+)");
        return m.Success && float.TryParse(m.Groups[1].Value,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out float v) ? v : def;
    }

    static Color ParseYamlColor(string yaml, string propName, Color def)
    {
        // Busca:   - _PropName: {r: 1, g: 1, b: 1, a: 1}
        var m = Regex.Match(yaml,
            $@"-\s*{Regex.Escape(propName)}:\s*\{{r:\s*([\d\.]+),\s*g:\s*([\d\.]+),\s*b:\s*([\d\.]+),\s*a:\s*([\d\.]+)\}}");
        if (!m.Success) return def;

        float r = float.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        float g = float.Parse(m.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
        float b = float.Parse(m.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture);
        float a = float.Parse(m.Groups[4].Value, System.Globalization.CultureInfo.InvariantCulture);
        return new Color(r, g, b, a);
    }
}
