using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Upgradea todos los ElectricalNode de la escena para que sean
/// detectables por el multímetro VR del Explorador.
///
/// Qué añade a cada nodo que le falte:
///   • Sphere visual (pequeña, semitransparente) para que el Explorador la vea
///   • SphereCollider no-trigger con radio correcto para XRI raycast
///   • XRSimpleInteractable (requerido por NodeInteractable)
///   • NodeInteractable con nodeTarget y multimeter asignados
///
/// Menú: Tools → TITA → Setup Nodos de Circuito (Multímetro VR)
/// </summary>
public static class CircuitNodeSetupTool
{
    [MenuItem("Tools/TITA/Setup Nodos de Circuito (Multímetro VR)")]
    public static void SetupAllNodes()
    {
        // ── 1. Buscar el Multimeter en la escena ──────────────────────────
        var multimeter = Object.FindFirstObjectByType<Multimeter>();
        if (multimeter == null)
        {
            bool proceed = EditorUtility.DisplayDialog(
                "Multímetro no encontrado",
                "No hay ningún Multimeter en la escena activa.\n\n" +
                "Los NodeInteractable se crearán sin referencia al multímetro " +
                "(se auto-buscarán en runtime con FindFirstObjectByType).\n\n" +
                "¿Continuar de todas formas?",
                "Sí, continuar", "Cancelar");
            if (!proceed) return;
        }

        // ── 2. Buscar todos los ElectricalNode en escena ──────────────────
        var allNodes = Object.FindObjectsByType<ElectricalNode>(FindObjectsSortMode.None);
        if (allNodes.Length == 0)
        {
            EditorUtility.DisplayDialog("Sin nodos",
                "No se encontraron ElectricalNode en la escena.\n\n" +
                "Ejecuta primero:\n" +
                "Tools → TITA → Generar Zonas de Juego",
                "OK");
            return;
        }

        int upgraded = 0;
        int skipped  = 0;

        foreach (var node in allNodes)
        {
            bool changed = UpgradeNode(node, multimeter);
            if (changed) upgraded++;
            else         skipped++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog(
            "Setup completado",
            $"Nodos procesados: {allNodes.Length}\n" +
            $"  Upgradeados:  {upgraded}\n" +
            $"  Ya completos: {skipped}\n\n" +
            "Cada nodo ahora tiene:\n" +
            "  • Esfera visual (amarilla semitransparente)\n" +
            "  • SphereCollider (non-trigger) para XRI raycast\n" +
            "  • XRSimpleInteractable\n" +
            "  • NodeInteractable → nodeTarget asignado\n\n" +
            (multimeter != null
                ? $"Multímetro '{multimeter.name}' asignado a todos."
                : "Sin multímetro — se auto-buscará en runtime."),
            "OK");

        Debug.Log($"[CircuitNodeSetupTool] {upgraded} nodos upgradeados, {skipped} ya estaban completos.");
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Lógica de upgrade de un nodo individual
    // ─────────────────────────────────────────────────────────────────────

    static bool UpgradeNode(ElectricalNode node, Multimeter multimeter)
    {
        bool changed = false;
        GameObject go = node.gameObject;

        // ── A. Esfera visual ────────────────────────────────────────────
        if (go.GetComponent<MeshRenderer>() == null)
        {
            AddVisualSphere(go);
            changed = true;
        }

        // ── B. SphereCollider no-trigger para XRI raycast ───────────────
        //  El GameSceneGenerator pone isTrigger=true con radio 0.4 (local).
        //  XRI no detecta triggers por defecto → lo cambiamos a non-trigger
        //  y ajustamos el radio al espacio mundo (~1.5 cm).
        var col = go.GetComponent<SphereCollider>();
        if (col == null)
        {
            col          = go.AddComponent<SphereCollider>();
            changed      = true;
        }

        // Radio en espacio local: queremos ~1.5 cm en mundo.
        // Si la escala global es ~1, radius 0.015 da 1.5 cm.
        // Usamos la escala lossyScale para compensar escalas heredadas.
        float worldRadius = 0.015f;
        float localRadius = worldRadius / Mathf.Max(go.transform.lossyScale.x, 0.001f);
        if (col.isTrigger || Mathf.Abs(col.radius - localRadius) > 0.001f)
        {
            col.isTrigger = false;
            col.radius    = localRadius;
            changed       = true;
        }

        // ── C. XRSimpleInteractable ─────────────────────────────────────
        if (go.GetComponent<XRSimpleInteractable>() == null)
        {
            go.AddComponent<XRSimpleInteractable>();
            changed = true;
        }

        // ── D. NodeInteractable ─────────────────────────────────────────
        var ni = go.GetComponent<NodeInteractable>();
        if (ni == null)
        {
            ni      = go.AddComponent<NodeInteractable>();
            changed = true;
        }

        // Asignar referencias aunque ya existiera el componente
        if (ni.nodeTarget != node)
        {
            ni.nodeTarget = node;
            changed       = true;
        }

        if (multimeter != null && ni.multimeter != multimeter)
        {
            ni.multimeter = multimeter;
            changed       = true;
        }

        // Renderer para feedback de color (hover/selected)
        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null && ni.nodeRenderer != mr)
        {
            ni.nodeRenderer = mr;
            changed         = true;
        }

        if (changed)
            EditorUtility.SetDirty(go);

        return changed;
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Añade una esfera visual pequeña al nodo.
    /// Color: amarillo para NodeA, azul claro para NodeB.
    /// </summary>
    static void AddVisualSphere(GameObject go)
    {
        // Determinar color según el nombre del nodo
        bool isNodeA  = go.name.ToLowerInvariant().Contains("nodea") ||
                        go.name.ToLowerInvariant().Contains("node_a") ||
                        go.name.ToLowerInvariant().Contains("node a");
        Color nodeColor = isNodeA
            ? new Color(1f,   0.85f, 0.1f,  0.85f)   // amarillo dorado = terminal +
            : new Color(0.2f, 0.6f,  1f,    0.85f);   // azul = terminal -

        // Escalar la esfera al mundo: 1.2 cm de radio visual
        float worldSize = 0.024f;
        float localSize = worldSize / Mathf.Max(go.transform.lossyScale.x, 0.001f);
        go.transform.localScale = Vector3.one * localSize;

        // Mesh de esfera
        var tmpPrim = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var mesh    = tmpPrim.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(tmpPrim);

        go.AddComponent<MeshFilter>().sharedMesh = mesh;

        // Material URP semitransparente
        var shader = Shader.Find("Universal Render Pipeline/Lit")
                  ?? Shader.Find("Standard");
        var mat    = new Material(shader);
        mat.SetColor("_BaseColor", nodeColor);
        mat.SetColor("_Color",     nodeColor);

        // Transparencia en URP
        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1f);         // 0=opaque, 1=transparent
            mat.SetFloat("_Blend",   0f);          // alpha blend
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = 3000;
        }

        string matPath = $"Assets/Materials/Node_{go.name}_{go.GetInstanceID()}.mat";
        AssetDatabase.CreateAsset(mat, matPath);

        go.AddComponent<MeshRenderer>().sharedMaterial = mat;
    }

    [MenuItem("Tools/TITA/Setup Nodos de Circuito (Multímetro VR)", true)]
    static bool Validate() => EditorSceneManager.GetActiveScene().IsValid();
}
