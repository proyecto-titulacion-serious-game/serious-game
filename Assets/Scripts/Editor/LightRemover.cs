using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LightRemover
{
    static readonly string[] ProjectScenePaths =
    {
        "Assets/Scenes/Tecnico.unity",
        "Assets/Scenes/Explorador.unity",
        "Assets/Scenes/IntegratedDemo.unity",
        "Assets/MapVR.unity",
        "Assets/serious game.unity",
        "Assets/circuit/main.unity",
    };

    [MenuItem("Tools/TITA/Remove Point and Spot Lights (All Scenes)")]
    public static void RemoveAllPointAndSpotLights()
    {
        Scene originalScene = SceneManager.GetActiveScene();
        string originalPath = originalScene.path;

        int totalRemoved = 0;
        var log = new System.Text.StringBuilder();

        bool proceed = EditorUtility.DisplayDialog(
            "Eliminar Point y Spot Lights",
            $"Se eliminarán todas las Point y Spot Lights de {ProjectScenePaths.Length} escenas.\n" +
            "¿Continuar? (se guardará cada escena automáticamente)",
            "Sí, eliminar", "Cancelar");

        if (!proceed) return;

        foreach (string scenePath in ProjectScenePaths)
        {
            if (!System.IO.File.Exists(scenePath))
            {
                log.AppendLine($"  SKIP (no existe): {scenePath}");
                continue;
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            int removed = RemoveLightsFromScene(scene);
            totalRemoved += removed;

            if (removed > 0)
            {
                EditorSceneManager.SaveScene(scene);
                log.AppendLine($"  OK   {System.IO.Path.GetFileName(scenePath)} — {removed} luces eliminadas");
            }
            else
            {
                log.AppendLine($"  SKIP {System.IO.Path.GetFileName(scenePath)} — sin Point/Spot lights");
            }
        }

        if (!string.IsNullOrEmpty(originalPath))
            EditorSceneManager.OpenScene(originalPath, OpenSceneMode.Single);

        Debug.Log($"[LightRemover] Total eliminadas: {totalRemoved}\n{log}");
        EditorUtility.DisplayDialog("Completado",
            $"Se eliminaron {totalRemoved} luces Point/Spot.\nRevisa la consola para el detalle.", "OK");
    }

    static int RemoveLightsFromScene(Scene scene)
    {
        int count = 0;
        var toDestroy = new List<Object>();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Light light in root.GetComponentsInChildren<Light>(true))
            {
                if (light.type == LightType.Point || light.type == LightType.Spot)
                {
                    // If the GameObject has no other meaningful components, destroy it entirely
                    var comps = light.gameObject.GetComponents<Component>();
                    bool onlyLight = true;
                    foreach (var c in comps)
                    {
                        if (c is Transform || c is Light) continue;
                        onlyLight = false;
                        break;
                    }

                    if (onlyLight && light.gameObject.transform.childCount == 0)
                        toDestroy.Add(light.gameObject);
                    else
                        toDestroy.Add(light);

                    count++;
                }
            }
        }

        foreach (var obj in toDestroy)
            Undo.DestroyObjectImmediate(obj);

        return count;
    }
}
