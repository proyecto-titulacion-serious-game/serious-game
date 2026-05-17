using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;

/// <summary>
/// Importa el sample "XR Device Simulator" del paquete XRI, que contiene el prefab
/// correctamente configurado con los InputActionAssets que necesita XRDeviceSimulator.
///
/// Menu: Tools → TITA → Simulador VR Editor → Importar Action Assets (fix errores)
///
/// Resuelve:
///   "No Device Simulator Action Asset has been defined"
///   "No Controller Action Asset has been defined"
///   "No Hand Action Asset has been defined"
/// </summary>
public static class VRSimulatorActionsSetup
{
    private const string XRI_PACKAGE   = "com.unity.xr.interaction.toolkit";
    private const string SAMPLE_NAME   = "XR Device Simulator";
    private const string PREFAB_NAME   = "XR Device Simulator.prefab";
    private const string SETTINGS_PATH = "Assets/XRI/Settings/Resources/XRDeviceSimulatorSettings.asset";
    private const string OUR_BLANK_PREFAB = "Assets/XRI/XR Device Simulator.prefab";

    [MenuItem("Tools/TITA/Simulador VR Editor/Importar Action Assets (fix errores)")]
    static void ImportSimulatorSample()
    {
        // ── 1. Buscar info del paquete XRI ──────────────────────────────────
        var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(
            typeof(UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor).Assembly);

        if (packageInfo == null)
        {
            EditorUtility.DisplayDialog("Error",
                "No se encontró el paquete XR Interaction Toolkit.", "OK");
            return;
        }

        // ── 2. Buscar el sample "XR Device Simulator" ────────────────────────
        var samples = Sample.FindByPackage(XRI_PACKAGE, packageInfo.version);
        var sample  = samples.FirstOrDefault(s => s.displayName == SAMPLE_NAME);

        if (string.IsNullOrEmpty(sample.displayName))
        {
            EditorUtility.DisplayDialog("Sample no encontrado",
                $"No se encontró el sample '{SAMPLE_NAME}' en XRI {packageInfo.version}.\n\n" +
                "Importa manualmente:\n" +
                "Window → Package Manager → XR Interaction Toolkit\n→ Samples → XR Device Simulator → Import",
                "OK");
            return;
        }

        // ── 3. Importar si no está importado ─────────────────────────────────
        if (!sample.isImported)
        {
            sample.Import(Sample.ImportOptions.OverridePreviousImports);
            AssetDatabase.Refresh();
            Debug.Log($"[VRSimulatorActionsSetup] Sample '{SAMPLE_NAME}' importado en: {sample.importPath}");
        }
        else
        {
            Debug.Log($"[VRSimulatorActionsSetup] Sample '{SAMPLE_NAME}' ya estaba importado en: {sample.importPath}");
        }

        // ── 4. Buscar el prefab configurado dentro del sample importado ───────
        var guids = AssetDatabase.FindAssets($"{PREFAB_NAME} t:Prefab");
        string prefabPath = null;

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            // Preferir el que está en la carpeta del sample, no en Samples~
            if (path.StartsWith("Assets/") && path.Contains("XR Device Simulator") && !path.Contains("Samples~"))
            {
                prefabPath = path;
                break;
            }
        }

        if (prefabPath == null)
        {
            EditorUtility.DisplayDialog("Prefab no encontrado",
                "El sample fue importado pero no se encontró el prefab.\n\n" +
                "Busca manualmente 'XR Device Simulator.prefab' en la carpeta\n" +
                $"Assets/Samples/XR Interaction Toolkit/{packageInfo.version}/{SAMPLE_NAME}/\n\n" +
                "y asígnalo en:\n" +
                "Edit → Project Settings → XR Plug-in Management → XR Interaction Toolkit\n" +
                "→ XR Device Simulator Prefab",
                "OK");
            return;
        }

        Debug.Log($"[VRSimulatorActionsSetup] Prefab encontrado: {prefabPath}");

        // ── 5. Asignar el prefab configurado en XRDeviceSimulatorSettings ────
        var settings = AssetDatabase.LoadAssetAtPath<ScriptableObject>(SETTINGS_PATH);
        if (settings != null)
        {
            var prefabGO = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var so = new SerializedObject(settings);
            so.FindProperty("m_SimulatorPrefab").objectReferenceValue = prefabGO;
            so.FindProperty("m_AutomaticallyInstantiateSimulatorPrefab").boolValue = true;
            so.FindProperty("m_AutomaticallyInstantiateInEditorOnly").boolValue    = true;
            so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            Debug.Log($"[VRSimulatorActionsSetup] Prefab asignado en XRDeviceSimulatorSettings.");
        }

        // ── 6. Eliminar nuestro prefab vacío que causaba el problema ──────────
        if (File.Exists(OUR_BLANK_PREFAB))
        {
            AssetDatabase.DeleteAsset(OUR_BLANK_PREFAB);
            Debug.Log($"[VRSimulatorActionsSetup] Prefab vacío eliminado: {OUR_BLANK_PREFAB}");
        }

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Simulador VR configurado",
            $"El sample '{SAMPLE_NAME}' fue importado y el prefab configurado.\n\n" +
            $"Prefab asignado:\n{prefabPath}\n\n" +
            "Los errores de Action Asset deberían desaparecer al entrar en Play Mode.\n" +
            "Si persisten, cierra y reabre Unity.", "OK");
    }
}
