using UnityEngine;

/// <summary>
/// Herramienta de desarrollo para saltar de nivel rápidamente.
/// Ideal para probar el Reto 4 sin pasar por los anteriores.
/// </summary>
public class DebugLevelSkipper : MonoBehaviour
{
    private GameManager _gameManager;

    void Start()
    {
        _gameManager = FindAnyObjectByType<GameManager>();
    }

    void Update()
    {
        // Al presionar la tecla F4, salta directamente al Reto 4 (Arduino)
        if (Input.GetKeyDown(KeyCode.F4))
        {
            if (_gameManager != null)
            {
                Debug.Log("[DEBUG] Forzando el salto al Reto 4 (Arduino)...");
                
                // El Reto 4 corresponde al índice 3 en el arreglo del GameManager
                _gameManager.GoToLevel(3); 
            }
            else
            {
                Debug.LogWarning("[DEBUG] No se encontró el GameManager en la escena.");
            }
        }
    }
}