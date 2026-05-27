using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// Arregla luces y ambiente en Tecnico.unity:
///   1. Elimina las Spot/Point sobrantes del Japan Office
///   2. Reduce la Directional a intensidad 1 con sombras suaves
///   3. Reduce luz ambiental (evita objetos completamente blancos)
/// Para materiales rosados usar: Tools/TITA/Fix Workstation Materials
public static class TecnicoSceneFixer
{
    [MenuItem("Tools/TITA/Fix Tecnico Scene (luces + sombras)")]
    static void Fix()
    {
        Scene active = SceneManager.GetActiveScene();
        if (!active.name.Equals("Tecnico", System.StringComparison.OrdinalIgnoreCase))
        {
            EditorUtility.DisplayDialog("Error",
                "Abre Assets/Scenes/Tecnico.unity antes de continuar.", "OK");
            return;
        }

        int  lightsRemoved = 0;
        bool dirFixed      = false;

        // ── 1. Directional: intensidad moderada ──────────────────────────────
        var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include);
        foreach (var light in lights)
        {
            if (light.type == LightType.Directional)
            {
                Undo.RecordObject(light, "Fix Directional Light");
                light.intensity      = 0.8f;
                light.shadowStrength = 0.6f;
                light.shadows        = LightShadows.Soft;
                dirFixed = true;
                EditorUtility.SetDirty(light);
            }
            else if (light.type == LightType.Spot || light.type == LightType.Point)
            {
                Undo.DestroyObjectImmediate(light.gameObject);
                lightsRemoved++;
            }
        }

        // ── 2. Ambiente: color plano gris oscuro (evita lavado blanco) ───────
        Undo.RecordObject(null, "Fix Ambient Light");   // RenderSettings no es Object, no necesita Undo
        RenderSettings.ambientMode      = AmbientMode.Flat;
        RenderSettings.ambientLight     = new Color(0.15f, 0.15f, 0.15f);
        RenderSettings.ambientIntensity = 1f;

        EditorSceneManager.MarkSceneDirty(active);

        string msg = $"Directional ajustada: {(dirFixed ? "sí (intensidad 0.8)" : "no encontrada")}\n" +
                     $"Luces Spot/Point eliminadas: {lightsRemoved}\n" +
                     $"Luz ambiental: Flat, gris oscuro (0.15)\n\n" +
                     "Guarda con Ctrl+S, luego corre Fix Workstation Materials si los\n" +
                     "componentes siguen blancos.";
        EditorUtility.DisplayDialog("Fix Tecnico Scene", msg, "OK");
        Debug.Log($"[TecnicoSceneFixer] {msg}");
    }
}
