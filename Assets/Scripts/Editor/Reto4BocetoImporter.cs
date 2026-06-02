using UnityEditor;
using UnityEngine;

/// <summary>
/// Importa el boceto del Reto 4 (Assets/Art/Reto4_Boceto.png) como Sprite y coloca un
/// panel de OBJETIVO (<see cref="Reto4GoalBlueprint"/>) en la escena del Explorador.
///
/// Rol asimétrico: el panel es del EXPLORADOR (VR). NO ejecutar en la escena del Técnico
/// — su regla de diseño prohíbe mostrarle el modelo 3D / boceto. Ver memoria
/// reto4-roles-asimetricos.
///
/// Menú: Tools → TITA → Reto 4 → Importar Boceto como Objetivo (Explorador)
/// </summary>
public static class Reto4BocetoImporter
{
    private const string SpritePath = "Assets/Art/Reto4_Boceto.png";

    [MenuItem("Tools/TITA/Reto 4/Importar Boceto como Objetivo (Explorador)")]
    public static void ImportAndPlace()
    {
        // ── 1. Importar el PNG como Sprite ───────────────────────────────────
        var importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
        if (importer == null)
        {
            EditorUtility.DisplayDialog("Boceto Reto 4",
                $"No se encontró el textura en:\n{SpritePath}\n\n" +
                "Copia 'Reto 4 boceto.png' a Assets/Art/Reto4_Boceto.png y reintenta.",
                "OK");
            return;
        }

        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType         = TextureImporterType.Sprite;
            importer.spriteImportMode    = SpriteImportMode.Single;
            importer.mipmapEnabled       = false;
            importer.SaveAndReimport();
        }

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        if (sprite == null)
        {
            EditorUtility.DisplayDialog("Boceto Reto 4",
                "El asset se importó pero no se pudo cargar como Sprite. " +
                "Revisa el Inspector de la textura (Texture Type = Sprite).", "OK");
            return;
        }

        // ── 2. Crear / reutilizar el GO del panel ────────────────────────────
        var existing = Object.FindAnyObjectByType<Reto4GoalBlueprint>();
        Reto4GoalBlueprint panel;
        if (existing != null)
        {
            panel = existing;
            Debug.Log("[Reto4Boceto] Reutilizando Reto4GoalBlueprint existente en escena.", panel);
        }
        else
        {
            var go = new GameObject("Reto4_GoalBlueprint");
            panel = go.AddComponent<Reto4GoalBlueprint>();
            Undo.RegisterCreatedObjectUndo(go, "Crear Reto4_GoalBlueprint");

            // Colocar ~1.5 m frente a la cámara del Explorador si existe
            var cam = FindExplorerCamera();
            if (cam != null)
            {
                Vector3 fwd = cam.transform.forward; fwd.y = 0f; fwd.Normalize();
                go.transform.position = cam.transform.position + fwd * 1.5f + Vector3.up * 0.0f;
                go.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
            }
            else
            {
                go.transform.position = new Vector3(0f, 1.5f, 1.5f);
                Debug.LogWarning("[Reto4Boceto] No se encontró ExplorerCamera. " +
                    "El panel se colocó en (0,1.5,1.5) — muévelo frente al Explorador.", go);
            }
        }

        panel.bocetoSprite = sprite;
        EditorUtility.SetDirty(panel);

        // ── 3. Marcar escena sucia y seleccionar ─────────────────────────────
        if (!Application.isPlaying)
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(panel.gameObject.scene);

        Selection.activeObject = panel.gameObject;
        EditorGUIUtility.PingObject(panel.gameObject);

        Debug.Log("[Reto4Boceto] Boceto importado como Sprite y panel de objetivo colocado. " +
                  "El panel se construye al entrar en Play (Toggle con tecla B o botón VR).", panel);
    }

    /// <summary>Busca una cámara que parezca la del Explorador (por nombre).</summary>
    private static Camera FindExplorerCamera()
    {
        foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            string n = c.name.ToLowerInvariant();
            if (n.Contains("explorer") || n.Contains("explorador") || n.Contains("centereye") || n.Contains("xr"))
                return c;
        }
        return Camera.main;
    }
}
