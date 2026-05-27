using UnityEngine;
using UnityEngine.SceneManagement;

/// Punto central para cambiar de escena. Usar desde botones UI con UnityEvent.
public class SceneLoader : MonoBehaviour
{
    [Tooltip("Nombre de la escena a cargar (debe estar en Build Settings)")]
    public string targetScene;

    public void Load() => LoadScene(targetScene);

    public static void LoadScene(string name) => SceneManager.LoadScene(name);

    public static void LoadTecnico() => SceneManager.LoadScene("Tecnico");
    public static void LoadMapVR()   => SceneManager.LoadScene("MapVR");
}
