#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Añade el panel de resultados VR a la escena del Explorador.
/// Tools → TITA → Explorador → Añadir Panel Resultados VR
/// </summary>
public static class ResultsPanelSetup
{
    [MenuItem("Tools/TITA/Explorador/Añadir Panel Resultados VR")]
    public static void AddResultsPanel()
    {
        var existing = Object.FindAnyObjectByType<ExplorerResultsPanel>();
        if (existing != null)
        {
            bool recreate = EditorUtility.DisplayDialog("Ya existe",
                "Ya hay un ExplorerResultsPanel en la escena. ¿Recrearlo?",
                "Sí", "Cancelar");
            if (!recreate) return;
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        var go = new GameObject("ResultsPanelController");
        Undo.RegisterCreatedObjectUndo(go, "Crear ResultsPanelController");
        go.AddComponent<ExplorerResultsPanel>();

        Selection.activeGameObject = go;
        EditorUtility.SetDirty(go);

        EditorUtility.DisplayDialog("Panel de resultados añadido",
            "Se creó 'ResultsPanelController' con ExplorerResultsPanel.\n\n" +
            "El panel se construye proceduralmente cuando\nObjectiveSystem.OnSessionEnded se dispara\n" +
            "(al completar los 4 retos).\n\n" +
            "Ajustes disponibles en Inspector:\n" +
            "  · distanceFromCamera  (distancia de lectura)\n" +
            "  · verticalOffset      (altura)\n" +
            "  · Colores de evaluación\n\n" +
            "También requiere ObjectiveSystem y PerformanceTracker\nen la escena para mostrar datos completos.",
            "OK");
    }

    [MenuItem("Tools/TITA/Explorador/Añadir Panel Resultados VR", true)]
    static bool Validate() =>
        UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().IsValid();
}
#endif
