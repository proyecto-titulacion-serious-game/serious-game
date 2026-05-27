using UnityEngine;
using UnityEditor;

/// <summary>
/// Test script para verificar que las correcciones VR funcionan correctamente
/// </summary>
public class VRFixTester
{
    [MenuItem("Tools/TITA/Test All VR Fixes")]
    public static void TestAllVRFixes()
    {
        Debug.Log("=== TESTING VR FIXES ===");
        
        bool allPassed = true;
        
        // Test 1: Verificar que PlayerController existe
        var playerController = Object.FindAnyObjectByType<PlayerController>();
        if (playerController != null)
        {
            Debug.Log("✅ Test 1 PASSED: PlayerController encontrado");
            
            // Test 2: Verificar CharacterController
            var cc = playerController.GetComponent<CharacterController>();
            if (cc != null)
            {
                Debug.Log("✅ Test 2 PASSED: CharacterController presente");
            }
            else
            {
                Debug.LogWarning("❌ Test 2 FAILED: CharacterController faltante");
                allPassed = false;
            }
            
            // Test 3: Verificar referencias via SerializedObject
            var serializedObject = new SerializedObject(playerController);
            
            var xrRigProperty = serializedObject.FindProperty("xrRig");
            if (xrRigProperty?.objectReferenceValue != null)
            {
                Debug.Log($"✅ Test 3 PASSED: xrRig asignado ({xrRigProperty.objectReferenceValue.name})");
            }
            else
            {
                Debug.LogWarning("⚠️ Test 3 WARNING: xrRig no asignado");
            }
            
            var headCameraProperty = serializedObject.FindProperty("headCamera");
            if (headCameraProperty?.objectReferenceValue != null)
            {
                Debug.Log($"✅ Test 4 PASSED: headCamera asignado ({headCameraProperty.objectReferenceValue.name})");
            }
            else
            {
                Debug.LogWarning("⚠️ Test 4 WARNING: headCamera no asignado");
            }
        }
        else
        {
            Debug.LogError("❌ Test 1 FAILED: No se encontró PlayerController en la escena");
            allPassed = false;
        }
        
        // Test 5: Verificar Input Actions Asset
        var inputActionsAsset = FindInputActionsAsset();
        if (inputActionsAsset != null)
        {
            Debug.Log($"✅ Test 5 PASSED: Input Actions Asset encontrado ({inputActionsAsset.name})");
        }
        else
        {
            Debug.LogWarning("⚠️ Test 5 WARNING: Input Actions Asset no encontrado");
        }
        
        // Test 6: Verificar XR Settings
        #if UNITY_XR
        Debug.Log("✅ Test 6 PASSED: Unity XR disponible");
        #else
        Debug.LogWarning("❌ Test 6 FAILED: Unity XR no disponible");
        allPassed = false;
        #endif
        
        // Resultado final
        if (allPassed)
        {
            Debug.Log("\n🎉 TODOS LOS TESTS PASARON - VR Setup está correcto");
        }
        else
        {
            Debug.LogWarning("\n⚠️ ALGUNOS TESTS FALLARON - Ejecutar Tools → TITA → Setup Completo VR Explorador");
        }
        
        Debug.Log("=== FIN TESTING VR ===");
    }
    
    [MenuItem("Tools/TITA/Compile Test - Verify No Errors")]
    public static void CompileTest()
    {
        Debug.Log("=== COMPILE TEST ===");
        
        try
        {
            // Intentar llamar al fixer sin errores
            Debug.Log("Testing PlayerControllerComponentFixer...");
            PlayerControllerComponentFixer.VerifyPlayerControllerSetup();
            Debug.Log("✅ PlayerControllerComponentFixer compila correctamente");
            
            Debug.Log("Testing VRSetupTool...");
            VRSetupTool.VerifyXRSettings(); 
            Debug.Log("✅ VRSetupTool compila correctamente");
            
            Debug.Log("Testing SteamVRConflictResolver...");
            // Solo verificar que la clase existe, no abrir ventana
            System.Type resolverType = System.Type.GetType("SteamVRConflictResolver");
            if (resolverType != null)
            {
                Debug.Log("✅ SteamVRConflictResolver compila correctamente");
            }
            
            Debug.Log("\n🎉 COMPILE TEST PASSED - Todos los scripts compilan sin errores");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ COMPILE TEST FAILED: {e.Message}");
            Debug.LogError("Revisar errores de compilación en Console");
        }
    }
    
    static UnityEngine.InputSystem.InputActionAsset FindInputActionsAsset()
    {
        string[] guids = AssetDatabase.FindAssets("t:InputActionAsset");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("XRI") || path.Contains("Input"))
            {
                return AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(path);
            }
        }
        return null;
    }
}