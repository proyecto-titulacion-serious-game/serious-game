using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Permite saltar directamente a cualquier reto con F1-F4 en Play Mode.
/// Requiere que GameManager._debugMode sea true (se activa automáticamente en Start).
/// </summary>
public class DebugLevelSkipper : MonoBehaviour
{
    private GameManager _gameManager;

    void Start()
    {
        _gameManager = FindAnyObjectByType<GameManager>();

        if (_gameManager != null)
        {
            var field = typeof(GameManager).GetField("_debugMode",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            field?.SetValue(_gameManager, true);
        }
        else
        {
            Debug.LogWarning("[DebugLevelSkipper] GameManager no encontrado.");
        }
    }

    void Update()
    {
        if (_gameManager == null) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        if      (kb.f1Key.wasPressedThisFrame) JumpTo(0);
        else if (kb.f2Key.wasPressedThisFrame) JumpTo(1);
        else if (kb.f3Key.wasPressedThisFrame) JumpTo(2);
        else if (kb.f4Key.wasPressedThisFrame) JumpTo(3);
    }

    private void JumpTo(int index)
    {
        Debug.Log($"[DEBUG] Saltando al Reto {index + 1}...");
        _gameManager.GoToLevel(index);
    }
}
