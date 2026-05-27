#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Añade el sistema de onboarding VR a la escena del Explorador.
/// Tools → TITA → Explorador → Añadir Onboarding VR
/// </summary>
public static class OnboardingSetup
{
    [MenuItem("Tools/TITA/Explorador/Añadir Onboarding VR")]
    public static void AddOnboarding()
    {
        // Evitar duplicados
        var existing = Object.FindAnyObjectByType<ExplorerOnboarding>();
        if (existing != null)
        {
            bool recreate = EditorUtility.DisplayDialog("Ya existe",
                "Ya hay un ExplorerOnboarding en la escena. ¿Recrearlo?",
                "Sí", "Cancelar");
            if (!recreate) return;
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        var go = new GameObject("OnboardingController");
        Undo.RegisterCreatedObjectUndo(go, "Crear OnboardingController");
        go.AddComponent<ExplorerOnboarding>();

        Selection.activeGameObject = go;
        EditorUtility.SetDirty(go);

        EditorUtility.DisplayDialog("Onboarding añadido",
            "Se creó 'OnboardingController' con ExplorerOnboarding.\n\n" +
            "Ajustes disponibles en Inspector:\n" +
            "  · distanceFromCamera  (distancia del panel)\n" +
            "  · verticalOffset      (altura)\n" +
            "  · backgroundColor / accentColor / etc.\n\n" +
            "El panel aparece al dar Play y el GameManager\n" +
            "espera a que el jugador lo complete antes de\n" +
            "activar el Reto 1.\n\n" +
            "Presionar Trigger (o Espacio) avanza las diapositivas.",
            "OK");
    }

    [MenuItem("Tools/TITA/Explorador/Añadir Onboarding VR", true)]
    static bool Validate() =>
        UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().IsValid();
}
#endif
