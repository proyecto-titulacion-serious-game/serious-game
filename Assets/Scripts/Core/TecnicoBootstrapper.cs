using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Carga NoonA.unity de forma aditiva cuando el build del Técnico arranca desde Tecnico.unity.
/// Colocar en cualquier GO de Tecnico.unity (recomendado: GameManager_System).
/// No hace nada si NoonA ya está cargada (editor con ambas escenas abiertas).
/// </summary>
public class TecnicoBootstrapper : MonoBehaviour
{
    [Tooltip("Nombre exacto de la escena del entorno 3D (debe estar en Build Settings).")]
    public string environmentScene = "NoonA";

    IEnumerator Start()
    {
        if (SceneManager.GetSceneByName(environmentScene).isLoaded)
            yield break;

        var op = SceneManager.LoadSceneAsync(environmentScene, LoadSceneMode.Additive);
        if (op == null)
        {
            Debug.LogError($"[TecnicoBootstrapper] No se pudo cargar '{environmentScene}'. " +
                           "Verifica que está en File → Build Settings.");
            yield break;
        }

        yield return op;
        Debug.Log($"[TecnicoBootstrapper] '{environmentScene}' cargada.");
    }
}
