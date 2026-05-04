using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

/// <summary>
/// Ventana de validacion de configuracion VR para Meta Quest 3.
/// Acceso: Tools > VR > Validar Configuracion
/// </summary>
public class VRValidator : EditorWindow
{
    // ─────────────────────────────────────────────
    //  Tipos internos
    // ─────────────────────────────────────────────

    enum Severity { OK, Warning, Error }

    class CheckResult
    {
        public Severity severity;
        public string   label;
        public string   detail;
        public Action   autoFix;   // null = sin auto-fix
    }

    // ─────────────────────────────────────────────
    //  Estado de la ventana
    // ─────────────────────────────────────────────

    readonly List<CheckResult> _results = new();
    Vector2 _scroll;
    bool    _hasRun;

    static readonly Color ColorOK      = new(0.25f, 0.75f, 0.25f);
    static readonly Color ColorWarning = new(1.00f, 0.60f, 0.10f);
    static readonly Color ColorError   = new(0.90f, 0.20f, 0.20f);

    // ─────────────────────────────────────────────
    //  Menu
    // ─────────────────────────────────────────────

    [MenuItem("Tools/VR/Validar Configuracion VR")]
    static void Open()
    {
        var w = GetWindow<VRValidator>("Validador VR");
        w.minSize = new Vector2(500, 520);
        w.RunAll();
    }

    // ─────────────────────────────────────────────
    //  GUI
    // ─────────────────────────────────────────────

    void OnGUI()
    {
        GUILayout.Space(8);
        EditorGUILayout.LabelField("Validador de Configuracion VR — Meta Quest 3",
            EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Unity 6 + OpenXR + XRI 3.4",
            EditorStyles.miniLabel);
        GUILayout.Space(6);

        if (GUILayout.Button("Ejecutar Validacion", GUILayout.Height(32)))
            RunAll();

        if (!_hasRun) return;

        GUILayout.Space(8);

        // Resumen
        int errors   = _results.Count(r => r.severity == Severity.Error);
        int warnings = _results.Count(r => r.severity == Severity.Warning);
        int oks      = _results.Count(r => r.severity == Severity.OK);

        var summaryColor = errors > 0 ? ColorError : warnings > 0 ? ColorWarning : ColorOK;
        var prevColor = GUI.color;
        GUI.color = summaryColor;
        EditorGUILayout.LabelField(
            $"Resultado: {errors} errores  |  {warnings} advertencias  |  {oks} correctos",
            EditorStyles.boldLabel);
        GUI.color = prevColor;

        // Boton corregir todo
        var fixable = _results
            .Where(r => r.severity != Severity.OK && r.autoFix != null)
            .ToList();

        if (fixable.Count > 0)
        {
            GUILayout.Space(4);
            if (GUILayout.Button($"Corregir automaticamente ({fixable.Count} problemas)",
                GUILayout.Height(28)))
            {
                foreach (var r in fixable) r.autoFix();
                AssetDatabase.SaveAssets();
                RunAll();
                return;
            }
        }

        GUILayout.Space(8);

        // Lista de resultados
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        foreach (var r in _results)
            DrawResult(r);
        EditorGUILayout.EndScrollView();

        GUILayout.Space(8);
        EditorGUILayout.HelpBox(
            "Tras aplicar correcciones automaticas guarda el proyecto (Ctrl+S) y " +
            "vuelve a ejecutar la validacion para confirmar.",
            MessageType.Info);
    }

    void DrawResult(CheckResult r)
    {
        string icon  = r.severity == Severity.OK ? "OK" : r.severity == Severity.Error ? "ERROR" : "AVISO";
        Color  color = r.severity == Severity.OK ? ColorOK : r.severity == Severity.Error ? ColorError : ColorWarning;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.BeginHorizontal();

        var labelStyle = new GUIStyle(EditorStyles.boldLabel) { richText = true };
        labelStyle.normal.textColor = color;
        GUILayout.Label($"[{icon}]  {r.label}", labelStyle, GUILayout.ExpandWidth(true));

        if (r.severity != Severity.OK && r.autoFix != null)
        {
            if (GUILayout.Button("Fix", GUILayout.Width(48), GUILayout.Height(18)))
            {
                r.autoFix();
                AssetDatabase.SaveAssets();
                RunAll();
                return;
            }
        }

        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(r.detail))
        {
            var detailStyle = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };
            detailStyle.normal.textColor = new Color(0.75f, 0.75f, 0.75f);
            GUILayout.Label(r.detail, detailStyle);
        }

        EditorGUILayout.EndVertical();
        GUILayout.Space(3);
    }

    // ─────────────────────────────────────────────
    //  Validaciones
    // ─────────────────────────────────────────────

    void RunAll()
    {
        _results.Clear();
        _hasRun = true;

        CheckAndroidXRLoader();
        CheckAutomaticLoading();
        CheckMetaQuestFeature();
        CheckControllerProfiles();
        CheckOpenXRRenderMode();
        CheckMinSdkVersion();
        CheckScriptingBackend();
        CheckTargetArchitecture();
        CheckSingleScene();

        Repaint();
    }

    // 1. OpenXR Loader en Android
    void CheckAndroidXRLoader()
    {
        var settings = XRGeneralSettingsPerBuildTarget
            .XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);

        if (settings == null)
        {
            Add(Severity.Error,
                "XR Management: sin configuracion Android",
                "No existe XRGeneralSettings para Android. " +
                "Ir a Project Settings > XR Plug-in Management y habilitar la pestana Android.",
                null);
            return;
        }

        var manager = settings.Manager;
        bool hasOpenXR = manager != null &&
                         manager.activeLoaders.Any(l => l is OpenXRLoader);

        if (hasOpenXR)
        {
            Add(Severity.OK, "OpenXR Loader asignado en Android");
        }
        else
        {
            Add(Severity.Error,
                "OpenXR Loader NO asignado en Android",
                "m_Loaders esta vacio para Android. El VR no se inicializara en el dispositivo. " +
                "Fix automatico: asigna el OpenXRLoader al XR Manager de Android.",
                () =>
                {
                    XRPackageMetadataStore.AssignLoader(
                        manager,
                        typeof(OpenXRLoader).FullName,
                        BuildTargetGroup.Android);
                    EditorUtility.SetDirty(manager);
                });
        }
    }

    // 2. Automatic Loading en Android
    void CheckAutomaticLoading()
    {
        var settings = XRGeneralSettingsPerBuildTarget
            .XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);

        var manager = settings?.Manager;
        if (manager == null) return;

        bool autoLoad = manager.automaticLoading;
        bool autoRun  = manager.automaticRunning;

        if (autoLoad && autoRun)
        {
            Add(Severity.OK, "AutomaticLoading y AutomaticRunning habilitados (Android)");
        }
        else
        {
            Add(Severity.Error,
                "AutomaticLoading/AutomaticRunning deshabilitados en Android",
                $"automaticLoading={autoLoad}, automaticRunning={autoRun}. " +
                "El XR no se iniciara solo al lanzar la app en el Quest.",
                () =>
                {
                    manager.automaticLoading = true;
                    manager.automaticRunning = true;
                    EditorUtility.SetDirty(manager);
                });
        }
    }

    // 3. Meta Quest Feature
    void CheckMetaQuestFeature()
    {
        var openXR = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        if (openXR == null)
        {
            Add(Severity.Error, "OpenXRSettings nulo para Android",
                "Verifica la instalacion del paquete com.unity.xr.openxr.", null);
            return;
        }

        var feature = openXR.GetFeature<MetaQuestFeature>();
        if (feature == null)
        {
            Add(Severity.Error,
                "MetaQuestFeature no encontrado",
                "El paquete Meta Quest Feature no esta instalado. " +
                "Instalar desde Package Manager: com.unity.xr.openxr.",
                null);
            return;
        }

        if (feature.enabled)
        {
            Add(Severity.OK, "Meta Quest Support Feature habilitado (Android)");
        }
        else
        {
            Add(Severity.Error,
                "Meta Quest Support Feature DESHABILITADO",
                "Sin esta feature el Quest 3 no puede inicializar OpenXR. " +
                "Es el error mas critico para el despliegue.",
                () =>
                {
                    feature.enabled = true;
                    EditorUtility.SetDirty(openXR);
                });
        }
    }

    // 4. Controller Profiles
    void CheckControllerProfiles()
    {
        var openXR = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        if (openXR == null) return;

        CheckFeature<MetaQuestTouchPlusControllerProfile>(openXR,
            "Meta Quest Touch Plus (Quest 3) — Android",
            "Perfil de controladores del Meta Quest 3. Sin esto los inputs no funcionan.");

        CheckFeature<OculusTouchControllerProfile>(openXR,
            "Oculus Touch Controller Profile — Android",
            "Perfil de compatibilidad con controladores Oculus/Quest 2.");

        CheckFeature<MetaQuestTouchProControllerProfile>(openXR,
            "Meta Quest Touch Pro — Android",
            "Perfil para controladores Touch Pro (opcional pero recomendado).");
    }

    void CheckFeature<T>(OpenXRSettings settings, string label, string detail) where T : OpenXRFeature
    {
        var feature = settings.GetFeature<T>();
        if (feature == null)
        {
            Add(Severity.Warning, $"{label}: no encontrado", detail, null);
            return;
        }

        if (feature.enabled)
        {
            Add(Severity.OK, $"{label}: habilitado");
        }
        else
        {
            Add(Severity.Warning,
                $"{label}: DESHABILITADO",
                detail,
                () =>
                {
                    feature.enabled = true;
                    EditorUtility.SetDirty(settings);
                });
        }
    }

    // 5. Render Mode OpenXR
    void CheckOpenXRRenderMode()
    {
        var openXR = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
        if (openXR == null) return;

        bool isSinglePass = openXR.renderMode == OpenXRSettings.RenderMode.SinglePassInstanced;

        if (isSinglePass)
        {
            Add(Severity.OK, "OpenXR Render Mode: SinglePassInstanced (Multi View)");
        }
        else
        {
            Add(Severity.Warning,
                "OpenXR Render Mode: Multi-Pass (mas lento)",
                "SinglePassInstanced renderiza ambos ojos en un solo pass. " +
                "Multi-Pass duplica el costo de GPU. Recomendado: SinglePassInstanced.",
                () =>
                {
                    openXR.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
                    EditorUtility.SetDirty(openXR);
                });
        }
    }

    // 6. Android Min SDK
    void CheckMinSdkVersion()
    {
        int current = (int)PlayerSettings.Android.minSdkVersion;
        int required = 29;

        if (current >= required)
        {
            Add(Severity.OK, $"Android Min SDK: {current} (minimo requerido: {required})");
        }
        else
        {
            Add(Severity.Error,
                $"Android Min SDK demasiado bajo: {current} (requerido: >= {required})",
                "Meta Quest 3 requiere API 29 como minimo. " +
                "Con API 25 el build sera rechazado por el Quest.",
                () => PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29);
        }
    }

    // 7. Scripting Backend IL2CPP
    void CheckScriptingBackend()
    {
        var backend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android);
        if (backend == ScriptingImplementation.IL2CPP)
        {
            Add(Severity.OK, "Scripting Backend: IL2CPP (requerido por Meta)");
        }
        else
        {
            Add(Severity.Error,
                $"Scripting Backend: {backend} — debe ser IL2CPP",
                "Meta Quest solo acepta builds IL2CPP. Mono no es compatible.",
                () =>
                {
                    PlayerSettings.SetScriptingBackend(
                        NamedBuildTarget.Android,
                        ScriptingImplementation.IL2CPP);
                });
        }
    }

    // 8. Target Architecture ARM64
    void CheckTargetArchitecture()
    {
        var arch = PlayerSettings.Android.targetArchitectures;
        bool hasArm64 = (arch & AndroidArchitecture.ARM64) != 0;

        if (hasArm64)
        {
            Add(Severity.OK, "Target Architecture: ARM64 incluido");
        }
        else
        {
            Add(Severity.Error,
                "ARM64 NO incluido en Target Architectures",
                "Meta Quest 3 es ARM64. Sin esta arquitectura el APK no correra.",
                () =>
                {
                    PlayerSettings.Android.targetArchitectures =
                        PlayerSettings.Android.targetArchitectures | AndroidArchitecture.ARM64;
                });
        }
    }

    // 9. Escena registrada en Build Settings
    void CheckSingleScene()
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).ToArray();

        if (scenes.Length == 0)
        {
            Add(Severity.Error,
                "Sin escenas en Build Settings",
                "Agregar la escena principal en File > Build Settings.",
                null);
        }
        else if (scenes.Length == 1)
        {
            Add(Severity.OK,
                $"Build Settings: 1 escena registrada ({System.IO.Path.GetFileName(scenes[0].path)})");
        }
        else
        {
            Add(Severity.Warning,
                $"Build Settings: {scenes.Length} escenas registradas",
                "Verifica que todas las escenas sean necesarias para el build.");
        }
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────

    void Add(Severity severity, string label, string detail = null, Action autoFix = null)
    {
        _results.Add(new CheckResult
        {
            severity = severity,
            label    = label,
            detail   = detail,
            autoFix  = autoFix
        });
    }
}
