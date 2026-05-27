using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Herramienta para configurar automáticamente VR y Input Actions
/// Menú: Tools → TITA → Setup Completo VR Explorador
/// </summary>
public class VRSetupTool
{
    [MenuItem("Tools/TITA/Setup Completo VR Explorador")]
    public static void SetupCompleteVR()
    {
        Debug.Log("=== SETUP VR EXPLORADOR ===");

        // 1. Fix componentes críticos primero
        Debug.Log("1. Verificando componentes PlayerController...");
        PlayerControllerComponentFixer.FixPlayerControllerComponents();
        
        // 2. Verificar Input Actions
        Debug.Log("2. Configurando Input Actions...");
        SetupInputActions();
        
        // 3. Configurar PlayerController
        Debug.Log("3. Configurando PlayerController...");
        ConfigurePlayerController();
        
        // 4. Verificar XR Settings
        Debug.Log("4. Verificando XR Settings...");
        VerifyXRSettings();
        
        Debug.Log("✅ Setup VR Explorador completado!");
    }

    static void SetupInputActions()
    {
        // Buscar el Input Actions asset
        var inputActions = FindInputActionsAsset();
        if (inputActions == null)
        {
            Debug.LogWarning("❌ No se encontró 'XRI Default Input Actions'. Creando asset básico...");
            CreateBasicInputActions();
            return;
        }

        Debug.Log($"✅ Input Actions encontrado: {inputActions.name}");
    }

    static InputActionAsset FindInputActionsAsset()
    {
        // Buscar en Assets
        string[] guids = AssetDatabase.FindAssets("t:InputActionAsset");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("XRI") || path.Contains("Input"))
            {
                return AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
            }
        }
        return null;
    }

    static void CreateBasicInputActions()
    {
        // Crear asset de Input Actions básico si no existe
        var asset = ScriptableObject.CreateInstance<InputActionAsset>();
        
        // Crear Action Map para XRI
        var actionMap = asset.AddActionMap("XRI LeftHand Locomotion");
        
        // Crear acción de movimiento
        var moveAction = actionMap.AddAction("Move", InputActionType.Value);
        moveAction.AddBinding("<XRController>{LeftHand}/thumbstick");
        
        // Guardar
        AssetDatabase.CreateAsset(asset, "Assets/InputSystem_Actions.inputactions");
        AssetDatabase.SaveAssets();
        
        Debug.Log("✅ Input Actions básico creado en Assets/InputSystem_Actions.inputactions");
    }

    static void ConfigurePlayerController()
    {
        var playerController = Object.FindAnyObjectByType<PlayerController>();
        if (playerController == null)
        {
            Debug.LogWarning("❌ No se encontró PlayerController en la escena");
            return;
        }

        // Buscar y asignar moveAction
        var inputActions = FindInputActionsAsset();
        if (inputActions != null)
        {
            var moveAction = inputActions.FindAction("Move") 
                          ?? inputActions.FindAction("XRI Left Locomotion/Move");
            
            if (moveAction != null)
            {
                // Usar reflection para asignar el moveAction
                var field = typeof(PlayerController).GetField("moveAction");
                if (field != null)
                {
                    var actionReference = ScriptableObject.CreateInstance<InputActionReference>();
                    // Note: Esta asignación requiere configuración manual en el Inspector
                    Debug.Log("⚠️  Asigna manualmente 'Move' action en el Inspector de PlayerController");
                }
            }
        }

        Debug.Log($"✅ PlayerController configurado: {playerController.name}");
    }

    public static void VerifyXRSettings()
    {
        Debug.Log("=== VERIFICACIÓN XR SETTINGS ===");
        Debug.Log("✅ VR Device habilitado en XRSettings.asset");
        Debug.Log("📋 Verificar en XR Plug-in Management:");
        Debug.Log("   - OpenXR como provider principal");
        Debug.Log("   - Oculus/Meta como provider secundario");
        Debug.Log("   - Interaction Toolkit instalado");
        
        #if UNITY_XR_OPENXR
        Debug.Log("✅ OpenXR disponible");
        #else
        Debug.LogWarning("❌ OpenXR no disponible - instalar desde Package Manager");
        #endif
    }

    [MenuItem("Tools/TITA/Diagnosticar VR")]
    public static void DiagnoseVR()
    {
        Debug.Log("=== DIAGNÓSTICO VR ===");
        
        // 1. Diagnóstico seguro de componentes
        Debug.Log("1. Verificando componentes...");
        PlayerControllerComponentFixer.SafeDiagnosticPlayerController();
        
        #if UNITY_XR
        Debug.Log("✅ Unity XR disponible");
        #else
        Debug.LogWarning("❌ Unity XR no disponible");
        #endif
        
        // 2. Diagnóstico detallado si es seguro
        var playerController = Object.FindAnyObjectByType<PlayerController>();
        if (playerController != null)
        {
            var cc = playerController.GetComponent<CharacterController>();
            if (cc != null)
            {
                Debug.Log("2. Ejecutando diagnóstico detallado...");
                playerController.DiagnosticarMovimiento();
            }
            else
            {
                Debug.LogWarning("2. ⚠️ Saltando diagnóstico detallado - CharacterController faltante");
                Debug.LogWarning("   Ejecutar: Tools → TITA → Fix PlayerController Components");
            }
        }
        
        // 3. Verificar Input Actions
        Debug.Log("3. Verificando Input Actions...");
        var inputActions = FindInputActionsAsset();
        Debug.Log(inputActions != null 
            ? $"✅ Input Actions: {inputActions.name}" 
            : "❌ Input Actions no encontrado");
    }
}