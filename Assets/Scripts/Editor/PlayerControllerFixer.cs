using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;

/// <summary>
/// Script para configurar automáticamente el PlayerController con las Input Actions correctas
/// </summary>
public class PlayerControllerFixer
{
    [MenuItem("Tools/TITA/Fix PlayerController Input Actions")]
    public static void FixPlayerControllerInputs()
    {
        var playerController = Object.FindFirstObjectByType<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("❌ No se encontró PlayerController en la escena actual");
            return;
        }

        Debug.Log("🔧 Configurando PlayerController Input Actions...");

        // Buscar el XRI Default Input Actions
        string xriPath = "Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/XRI Default Input Actions.inputactions";
        var xriAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(xriPath);
        
        if (xriAsset == null)
        {
            // Fallback al InputSystem_Actions
            string fallbackPath = "Assets/InputSystem_Actions.inputactions";
            xriAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(fallbackPath);
        }

        if (xriAsset == null)
        {
            Debug.LogError("❌ No se encontró ningún Input Action Asset");
            return;
        }

        // Buscar la acción Move en XRI Left Locomotion
        var moveAction = xriAsset.FindAction("XRI Left Locomotion/Move");
        if (moveAction == null)
        {
            // Fallback a Explorer/Move
            moveAction = xriAsset.FindAction("Explorer/Move");
        }

        if (moveAction == null)
        {
            Debug.LogError("❌ No se encontró la acción 'Move' en el Input Asset");
            return;
        }

        // Crear InputActionReference
        var moveReference = ScriptableObject.CreateInstance<InputActionReference>();
        
        // Usar SerializedObject para asignar el moveAction
        var serializedController = new SerializedObject(playerController);
        var moveActionProperty = serializedController.FindProperty("moveAction");
        
        if (moveActionProperty != null)
        {
            // Crear el InputActionReference asset
            string referencePath = "Assets/Scripts/InputReferences/MoveActionReference.asset";
            
            // Crear directorio si no existe
            System.IO.Directory.CreateDirectory("Assets/Scripts/InputReferences");
            
            // Configurar la referencia
            var reference = CreateInputActionReference(moveAction, xriAsset);
            AssetDatabase.CreateAsset(reference, referencePath);
            AssetDatabase.SaveAssets();
            
            // Asignar al PlayerController
            moveActionProperty.objectReferenceValue = reference;
            serializedController.ApplyModifiedProperties();
            
            Debug.Log($"✅ moveAction asignado: {moveAction.actionMap.name}/{moveAction.name}");
        }
        else
        {
            Debug.LogError("❌ No se encontró la propiedad 'moveAction' en PlayerController");
        }

        EditorUtility.SetDirty(playerController);
        AssetDatabase.SaveAssets();
        
        Debug.Log("✅ PlayerController configurado correctamente!");
    }

    static InputActionReference CreateInputActionReference(InputAction action, InputActionAsset asset)
    {
        var reference = ScriptableObject.CreateInstance<InputActionReference>();
        
        // Usar reflection para configurar la referencia
        var assetField = typeof(InputActionReference).GetField("m_Asset", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var actionNameField = typeof(InputActionReference).GetField("m_ActionName",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (assetField != null && actionNameField != null)
        {
            assetField.SetValue(reference, asset);
            actionNameField.SetValue(reference, $"{action.actionMap.name}/{action.name}");
        }
        
        return reference;
    }

    [MenuItem("Tools/TITA/Verificar Input Actions")]
    public static void VerifyInputActions()
    {
        Debug.Log("=== VERIFICACIÓN INPUT ACTIONS ===");
        
        var playerController = Object.FindFirstObjectByType<PlayerController>();
        if (playerController != null)
        {
            playerController.DiagnosticarMovimiento();
        }
        else
        {
            Debug.LogWarning("❌ No hay PlayerController en la escena");
        }
    }
}