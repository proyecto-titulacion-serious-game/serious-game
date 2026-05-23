using System.IO;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.XR.Management;

/// <summary>
/// Menu: Tools → TITA → Configurar para Meta Quest Link
///
/// Prepara el proyecto para correr con Meta Quest via Link / Air Link en el Editor.
/// Ejecutar ANTES de darle Play con el Quest conectado.
/// </summary>
public static class QuestLinkSetup
{
    private const string SETTINGS_PATH = "Assets/XRI/Settings/Resources/XRDeviceSimulatorSettings.asset";

    [MenuItem("Tools/TITA/Configurar para Meta Quest Link")]
    static void Configure()
    {
        bool simulatorDisabled = DisableSimulator();
        bool oculusEnabled     = CheckOculusPlugin();

        string simLine   = simulatorDisabled ? "✓ XR Device Simulator deshabilitado" : "⚠ No se encontró XRDeviceSimulatorSettings (deshabilita manualmente)";
        string ocuLine   = oculusEnabled     ? "✓ Oculus XR Plugin activo en Standalone" : "⚠ Oculus XR Plugin NO encontrado en Standalone — ver instrucciones";
        string infoExtra = !oculusEnabled
            ? "\n━━━━ ACCIÓN REQUERIDA ━━━━\n" +
              "1. Edit → Project Settings → XR Plug-in Management\n" +
              "2. Pestaña PC (Windows/Standalone, ícono de monitor)\n" +
              "3. Marcar 'Oculus' o 'OpenXR' (requiere Meta Quest Link como runtime activo)\n"
            : "";

        EditorUtility.DisplayDialog(
            "Meta Quest Link — Configuración completada",
            simLine + "\n" + ocuLine + infoExtra +
            "\n━━━━ PASOS MANUALES RESTANTES ━━━━\n" +
            "1. Abre Meta Quest Link en el PC y actívalo (icono de link en barra de Quest)\n" +
            "2. En Meta Horizon Link → Configuración → General → 'Definir como runtime activo de OpenXR'\n" +
            "3. Asegúrate que ConnectionManager → modoOffline = true (si no tienes AppID de Photon)\n" +
            "4. Presiona ▶ Play en Unity — el juego abrirá en el visor del Quest",
            "Entendido");

        Debug.Log("[QuestLinkSetup] Configuración para Meta Quest Link aplicada.");
    }

    // ─────────────────────────────────────────────
    //  Deshabilitar XR Device Simulator
    // ─────────────────────────────────────────────

    static bool DisableSimulator()
    {
        var settings = AssetDatabase.LoadAssetAtPath<ScriptableObject>(SETTINGS_PATH);
        if (settings == null)
        {
            // Buscar en otros paths posibles
            var guids = AssetDatabase.FindAssets("XRDeviceSimulatorSettings t:ScriptableObject");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                settings = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (settings != null) break;
            }
        }

        if (settings == null)
        {
            Debug.LogWarning("[QuestLinkSetup] XRDeviceSimulatorSettings no encontrado. " +
                             "Ve a Tools → TITA → Simulador VR Editor → Deshabilitar manualmente.");
            return false;
        }

        var so = new SerializedObject(settings);
        so.FindProperty("m_AutomaticallyInstantiateSimulatorPrefab").boolValue = false;
        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
        Debug.Log("[QuestLinkSetup] XR Device Simulator auto-instantiation deshabilitada.");
        return true;
    }

    // ─────────────────────────────────────────────
    //  Verificar Oculus en Standalone
    // ─────────────────────────────────────────────

    static bool CheckOculusPlugin()
    {
        try
        {
            var generalSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(
                BuildTargetGroup.Standalone);

            if (generalSettings == null || generalSettings.Manager == null)
                return false;

            foreach (var loader in generalSettings.Manager.activeLoaders)
            {
                string name = loader.GetType().Name.ToLowerInvariant();
                if (name.Contains("oculus") || name.Contains("openxr"))
                    return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    // ─────────────────────────────────────────────
    //  Verificar modoOffline en escenas
    // ─────────────────────────────────────────────

    [MenuItem("Tools/TITA/Toggle modoOffline (todas las escenas)")]
    static void ToggleOfflineMode()
    {
        string msg = "¿Activar modoOffline = true en todos los ConnectionManager de las escenas?\n\n" +
                     "Esto hace que el juego corra sin Photon Fusion (útil si no tienes AppID configurado).\n\n" +
                     "Para juego en red: selecciona 'No' y configura el AppID en Fusion Hub.";

        bool activate = EditorUtility.DisplayDialog("Toggle modoOffline", msg, "Sí — sin red", "No — con red");
        Debug.Log($"[QuestLinkSetup] modoOffline = {activate}. " +
                  "Abre cada escena y cambia el valor en ConnectionManager manualmente, " +
                  "o usa el Inspector con la escena abierta.");

        EditorUtility.DisplayDialog("Instrucción",
            $"Para cambiar modoOffline en una escena:\n\n" +
            "1. Abre la escena (IntegratedDemo, Explorador, o Tecnico)\n" +
            "2. Busca el GameObject con ConnectionManager\n" +
            $"3. Cambia 'Modo Offline' a {activate} en el Inspector\n" +
            "4. Guarda la escena (Ctrl+S)",
            "OK");
    }
}
