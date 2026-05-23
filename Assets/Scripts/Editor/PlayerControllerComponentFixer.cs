using UnityEngine;
using UnityEditor;

/// <summary>
/// Fixer para problemas de componentes faltantes en PlayerController
/// </summary>
public class PlayerControllerComponentFixer
{
    [MenuItem("Tools/TITA/Fix PlayerController Components")]
    public static void FixPlayerControllerComponents()
    {
        Debug.Log("=== FIXING PLAYERCONTROLLER COMPONENTS ===");

        // Encontrar todos los PlayerController en la escena
        var playerControllers = Object.FindObjectsOfType<PlayerController>();
        
        if (playerControllers.Length == 0)
        {
            Debug.LogWarning("❌ No se encontraron objetos PlayerController en la escena");
            return;
        }

        foreach (var playerController in playerControllers)
        {
            Debug.Log($"🔧 Verificando PlayerController en: {playerController.name}");
            
            bool wasFixed = false;

            // 1. Verificar CharacterController
            var characterController = playerController.GetComponent<CharacterController>();
            if (characterController == null)
            {
                Debug.LogWarning($"❌ {playerController.name} no tiene CharacterController. Agregando...");
                characterController = playerController.gameObject.AddComponent<CharacterController>();
                
                // Configurar valores por defecto
                characterController.height = 1.8f;
                characterController.radius = 0.3f;
                characterController.stepOffset = 0.3f;
                characterController.slopeLimit = 45f;
                
                Debug.Log($"✅ CharacterController agregado a {playerController.name}");
                wasFixed = true;
            }
            else
            {
                Debug.Log($"✅ {playerController.name} ya tiene CharacterController");
            }

            // 2. Verificar referencias principales
            var serializedObject = new SerializedObject(playerController);
            
            // Verificar xrRig
            var xrRigProperty = serializedObject.FindProperty("xrRig");
            if (xrRigProperty != null && xrRigProperty.objectReferenceValue == null)
            {
                // Buscar XR Origin en la escena (búsqueda por nombre compatible con versiones)
                Transform xrOriginTransform = FindXROriginInScene();
                if (xrOriginTransform != null)
                {
                    xrRigProperty.objectReferenceValue = xrOriginTransform;
                    Debug.Log($"✅ xrRig asignado automáticamente: {xrOriginTransform.name}");
                    wasFixed = true;
                }
                else
                {
                    Debug.LogWarning($"⚠️ No se encontró XR Origin para asignar a xrRig en {playerController.name}");
                }
            }

            // Verificar headCamera
            var headCameraProperty = serializedObject.FindProperty("headCamera");
            if (headCameraProperty != null && headCameraProperty.objectReferenceValue == null)
            {
                // Buscar Main Camera o cámara VR
                var mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    headCameraProperty.objectReferenceValue = mainCamera;
                    Debug.Log($"✅ headCamera asignado automáticamente: {mainCamera.name}");
                    wasFixed = true;
                }
                else
                {
                    // Buscar cualquier cámara con tag "MainCamera"
                    var cameras = Object.FindObjectsOfType<Camera>();
                    foreach (var cam in cameras)
                    {
                        if (cam.CompareTag("MainCamera"))
                        {
                            headCameraProperty.objectReferenceValue = cam;
                            Debug.Log($"✅ headCamera asignado: {cam.name}");
                            wasFixed = true;
                            break;
                        }
                    }
                }
                
                if (headCameraProperty.objectReferenceValue == null)
                {
                    Debug.LogWarning($"⚠️ No se encontró cámara principal para {playerController.name}");
                }
            }

            // 3. Verificar que el objeto tenga un tag válido
            if (playerController.gameObject.tag == "Untagged")
            {
                playerController.gameObject.tag = "Player";
                Debug.Log($"✅ Tag 'Player' asignado a {playerController.name}");
                wasFixed = true;
            }

            // Aplicar cambios
            if (wasFixed)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(playerController);
                Debug.Log($"💾 Cambios guardados en {playerController.name}");
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"✅ PlayerController components verificados y corregidos ({playerControllers.Length} objetos procesados)");
    }

    [MenuItem("Tools/TITA/Verificar PlayerController Setup")]
    public static void VerifyPlayerControllerSetup()
    {
        Debug.Log("=== VERIFICACIÓN PLAYERCONTROLLER SETUP ===");

        var playerControllers = Object.FindObjectsOfType<PlayerController>();
        
        if (playerControllers.Length == 0)
        {
            Debug.LogWarning("❌ No hay PlayerController en la escena");
            return;
        }

        foreach (var pc in playerControllers)
        {
            Debug.Log($"\n🔍 Verificando: {pc.name}");
            
            // Verificar componentes requeridos
            var cc = pc.GetComponent<CharacterController>();
            Debug.Log($"  CharacterController: {(cc != null ? "✅" : "❌")}");
            
            // Verificar referencias via reflection para acceder a campos privados/públicos
            var fields = typeof(PlayerController).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            foreach (var field in fields)
            {
                if (field.FieldType == typeof(Transform) || field.FieldType == typeof(Camera))
                {
                    var value = field.GetValue(pc);
                    Debug.Log($"  {field.Name}: {(value != null ? "✅" : "⚠️ null")}");
                }
            }
            
            // Test diagnóstico seguro
            try
            {
                // Verificar que _cc está inicializado correctamente
                var ccField = typeof(PlayerController).GetField("_cc", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (ccField != null)
                {
                    var ccValue = ccField.GetValue(pc);
                    Debug.Log($"  _cc (private): {(ccValue != null ? "✅" : "❌ null")}");
                    
                    if (ccValue == null && cc != null)
                    {
                        Debug.LogWarning($"⚠️ _cc es null pero CharacterController existe. Puede ser problema de inicialización.");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"No se pudo verificar _cc: {e.Message}");
            }
        }
    }

    [MenuItem("Tools/TITA/Safe Diagnostic PlayerController")]
    public static void SafeDiagnosticPlayerController()
    {
        Debug.Log("=== DIAGNÓSTICO SEGURO PLAYERCONTROLLER ===");

        var playerControllers = Object.FindObjectsOfType<PlayerController>();
        
        foreach (var pc in playerControllers)
        {
            Debug.Log($"\n🔍 {pc.name}:");
            
            // Verificaciones básicas sin llamar métodos que puedan fallar
            var cc = pc.GetComponent<CharacterController>();
            Debug.Log($"  GameObject activo: {pc.gameObject.activeInHierarchy}");
            Debug.Log($"  Component activo: {pc.enabled}");
            Debug.Log($"  CharacterController presente: {cc != null}");
            
            if (cc != null)
            {
                Debug.Log($"  CharacterController habilitado: {cc.enabled}");
                Debug.Log($"  CharacterController isGrounded: {cc.isGrounded}");
            }
            
            Debug.Log($"  Transform posición: {pc.transform.position}");
            Debug.Log($"  Tag: {pc.gameObject.tag}");
        }

        Debug.Log("\n📋 Para corregir problemas encontrados, ejecutar:");
        Debug.Log("Tools → TITA → Fix PlayerController Components");
    }

    /// <summary>
    /// Busca XR Origin en la escena de manera compatible con diferentes versiones
    /// </summary>
    static Transform FindXROriginInScene()
    {
        // Estrategia 1: Buscar por nombre común
        var gameObjects = Object.FindObjectsOfType<Transform>();
        foreach (var t in gameObjects)
        {
            if (t.name.Contains("XR Origin") || t.name.Contains("XROrigin") || t.name.Contains("XR Rig") || t.name.Contains("XRRig"))
            {
                return t;
            }
        }
        
        // Estrategia 2: Buscar por componentes XR comunes
        var cameras = Object.FindObjectsOfType<Camera>();
        foreach (var cam in cameras)
        {
            if (cam.CompareTag("MainCamera") && 
                (cam.transform.parent?.name.Contains("XR") == true || cam.transform.parent?.name.Contains("Origin") == true))
            {
                return cam.transform.parent;
            }
        }
        
        // Estrategia 3: Buscar cualquier objeto con "XR" en su jerarquía padre
        foreach (var cam in cameras)
        {
            Transform current = cam.transform.parent;
            while (current != null)
            {
                if (current.name.Contains("XR") || current.name.Contains("Origin"))
                {
                    return current;
                }
                current = current.parent;
            }
        }
        
        return null;
    }
}