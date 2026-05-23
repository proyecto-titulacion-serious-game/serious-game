using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Advierte cuando Explorador.unity no es la escena activa al dar Play con
/// múltiples escenas cargadas. No modifica el estado del editor (hacerlo
/// durante ExitingEditMode aborta el Play mode).
/// </summary>
[InitializeOnLoad]
public static class VRSceneGuard
{
    const string ExploradoPath = "Assets/Scenes/Explorador.unity";

    static VRSceneGuard()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode) return;
        if (EditorSceneManager.sceneCount <= 1) return;

        Scene active = EditorSceneManager.GetActiveScene();
        if (active.path == ExploradoPath) return;

        // Solo advertir — NO llamar SetActiveScene aquí porque aborta el Play mode.
        UnityEngine.Debug.LogWarning(
            "[VRSceneGuard] La escena activa es '" + active.name + "', no 'Explorador'.\n" +
            "Si el VR no abre, haz clic derecho en 'Explorador' en el Hierarchy → " +
            "Set Active Scene, y luego vuelve a dar Play.");
    }
}
