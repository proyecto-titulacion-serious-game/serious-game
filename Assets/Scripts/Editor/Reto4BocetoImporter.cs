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

            ColocarPanel(go);
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

    /// <summary>
    /// Ancla el panel en un punto visible y estable para el Explorador. Prioridad:
    /// 1) sobre la protoboard (zona de trabajo del Reto 4) — el lugar natural para mirar el boceto;
    /// 2) frente a la cámara del Explorador; 3) fallback fijo (0,1.5,1.5).
    /// Orienta el panel para que mire hacia el jugador (cámara) o, si no hay, hacia -Z.
    /// </summary>
    private static void ColocarPanel(GameObject go)
    {
        var cam   = FindExplorerCamera();
        var proto = Object.FindAnyObjectByType<ProtoboardSimulator>();

        Vector3 pos;
        if (proto != null)
        {
            // Sobre la protoboard, elevado y un poco atrás para no tapar los slots.
            pos = proto.transform.position + Vector3.up * 0.45f - Vector3.forward * 0.18f;
        }
        else if (cam != null)
        {
            Vector3 fwd = cam.transform.forward; fwd.y = 0f; fwd.Normalize();
            pos = cam.transform.position + fwd * 1.2f + Vector3.up * 0.1f;
        }
        else
        {
            pos = new Vector3(0f, 1.5f, 1.5f);
            Debug.LogWarning("[Reto4Boceto] Sin ProtoboardSimulator ni ExplorerCamera. " +
                "Panel en (0,1.5,1.5) — muévelo donde el Explorador lo vea.", go);
        }

        go.transform.position = pos;

        // Mirar hacia el jugador (cámara) si existe; si no, hacia -Z.
        Vector3 lookTarget = cam != null ? cam.transform.position : pos + Vector3.back;
        Vector3 dir = lookTarget - pos; dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            go.transform.rotation = Quaternion.LookRotation(-dir.normalized, Vector3.up);
    }

    /// <summary>Busca una cámara que parezca la del Explorador (por nombre).</summary>
    private static Camera FindExplorerCamera()
    {
        foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
        {
            string n = c.name.ToLowerInvariant();
            if (n.Contains("explorer") || n.Contains("explorador") || n.Contains("centereye") || n.Contains("xr"))
                return c;
        }
        return Camera.main;
    }
}
