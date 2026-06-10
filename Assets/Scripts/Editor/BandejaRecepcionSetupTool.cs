#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Crea/repara la 'Bandeja_Recepcion' del Explorador y sus hijos:
///   - un ComponentSlot (donde el Explorador suelta el componente que envía el Técnico → dispara
///     la reparación del circuito; sin esto, en Retos 1-3 el componente correcto nunca se instala
///     y el circuito sigue en falla/sobrecarga),
///   - el box visible (Cube con MeshRenderer + BoxCollider) con un material URP cian semitransparente
///     (en vez del material default que sale magenta en URP),
///   - un DeliveryTrayIndicator (rótulo holográfico "MATERIALES RECIBIDOS"),
/// y cablea ComponentDeliverySystem.puntoDeEntrega a la bandeja.
///
/// Menú: Tools → TITA → Explorador → Reparar Bandeja_Recepcion (entrega + slot)
/// Batch: -executeMethod BandejaRecepcionSetupTool.RepararBatch  (abre Explorador.unity, repara y guarda)
/// </summary>
public static class BandejaRecepcionSetupTool
{
    const string MENU      = "Tools/TITA/Explorador/Reparar Bandeja_Recepcion (entrega + slot)";
    const string ESCENA    = "Assets/Scenes/Explorador.unity";
    const string MAT_PATH  = "Assets/Materials/Bandeja_Box.mat";

    [MenuItem(MENU)]
    static void Reparar()
    {
        bool ok = DoReparar(out string msg);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("TITA — Bandeja_Recepcion", msg +
            "\n\nIMPORTANTE: mueve 'Bandeja_Recepcion' a un sitio que el Explorador alcance " +
            "(sobre la mesa) y GUARDA la escena (Ctrl+S).", "OK");
        Debug.Log($"[BandejaRecepcionSetupTool] {(ok ? "OK" : "AVISO")}: {msg}");
    }

    /// <summary>
    /// Entrada batch (-executeMethod BandejaRecepcionSetupTool.RepararBatch).
    /// Abre Explorador.unity, repara, guarda la escena y sale (0 ok / 1 error).
    /// </summary>
    public static void RepararBatch()
    {
        var scene = EditorSceneManager.OpenScene(ESCENA, OpenSceneMode.Single);
        bool ok = DoReparar(out string msg);
        if (ok)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        Debug.Log($"[BandejaRecepcionSetupTool] RESULTADO: {msg}");
        EditorApplication.Exit(ok ? 0 : 1);
    }

    /// <summary>Lógica central. Devuelve true si la bandeja quedó reparada/lista.</summary>
    static bool DoReparar(out string msg)
    {
        // ── 1. Encontrar o crear la Bandeja_Recepcion ─────────────────────────
        var bandeja = GameObject.Find("Bandeja_Recepcion");
        bool creada = false;
        if (bandeja == null)
        {
            bandeja = new GameObject("Bandeja_Recepcion");
            Undo.RegisterCreatedObjectUndo(bandeja, "Crear Bandeja_Recepcion");
            bandeja.transform.position = PosicionSugerida();
            creada = true;
        }

        // ── 2. Asegurar un ComponentSlot hijo (el box visible + gesto de instalación) ─
        var slot = bandeja.GetComponentInChildren<ComponentSlot>(true);
        if (slot == null)
        {
            var slotGO = GameObject.CreatePrimitive(PrimitiveType.Cube);   // MeshRenderer + MeshFilter + BoxCollider
            slotGO.name = "Bandeja_Slot";
            Undo.RegisterCreatedObjectUndo(slotGO, "Crear Bandeja_Slot");
            slotGO.transform.SetParent(bandeja.transform, false);
            slotGO.transform.localPosition = Vector3.zero;
            slotGO.transform.localScale    = Vector3.one * 0.14f;

            // ComponentSlot.Awake pone el BoxCollider en isTrigger.
            slot = slotGO.AddComponent<ComponentSlot>();
            slot.acceptedType = ComponentSlotType.Any;   // acepta cualquier pieza → siempre dispara la reparación
        }

        // ── 2b. Material URP del box (sustituye el material default = magenta en URP) ─
        var box = slot.GetComponent<MeshRenderer>();
        if (box != null)
        {
            box.sharedMaterial = CrearMaterialBox();
            EditorUtility.SetDirty(box);
        }

        // ── 3. Cablear ComponentDeliverySystem.puntoDeEntrega ─────────────────
        var delivery = Object.FindAnyObjectByType<ComponentDeliverySystem>(FindObjectsInactive.Include);
        if (delivery != null)
        {
            Undo.RecordObject(delivery, "Cablear puntoDeEntrega");
            delivery.puntoDeEntrega = bandeja.transform;
            slot.delivery = delivery;
            EditorUtility.SetDirty(delivery);
        }

        // ── 4. Indicador holográfico ──────────────────────────────────────────
        if (bandeja.GetComponentInChildren<DeliveryTrayIndicator>(true) == null)
            Undo.AddComponent<DeliveryTrayIndicator>(bandeja);

        Selection.activeGameObject = bandeja;

        msg = (creada ? "Bandeja_Recepcion CREADA" : "Bandeja_Recepcion encontrada") +
              $" en {bandeja.transform.position}.\n" +
              $"• Box/slot: '{slot.name}' (Cube + ComponentSlot 'Any' + material URP cian) \n" +
              $"• puntoDeEntrega: {(delivery != null ? "cableado a la bandeja ✓" : "NO se encontró ComponentDeliverySystem — revisa")}\n" +
              "• DeliveryTrayIndicator: ok";
        return delivery != null;
    }

    // ── Material URP cian semitransparente con leve glow ──────────────────────
    static Material CrearMaterialBox()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>(MAT_PATH);
        if (existing != null) return existing;

        var shader = Shader.Find("Universal Render Pipeline/Lit")
                  ?? Shader.Find("Universal Render Pipeline/Unlit")
                  ?? Shader.Find("Standard");

        var mat = new Material(shader) { name = "Bandeja_Box" };
        Color cyan = new Color(0.00f, 0.85f, 1.00f, 0.35f);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", cyan);
        if (mat.HasProperty("_Color"))     mat.SetColor("_Color", cyan);

        // Transparencia (URP Lit/Unlit).
        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1f);                 // 0 = Opaque, 1 = Transparent
            if (mat.HasProperty("_Blend"))   mat.SetFloat("_Blend", 0f);   // Alpha
            if (mat.HasProperty("_SrcBlend")) mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (mat.HasProperty("_DstBlend")) mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            if (mat.HasProperty("_ZWrite"))  mat.SetFloat("_ZWrite", 0f);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)RenderQueue.Transparent;
        }

        // Glow leve.
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(0f, 0.5f, 0.7f) * 1.2f);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        AssetDatabase.CreateAsset(mat, MAT_PATH);
        AssetDatabase.SaveAssets();
        return mat;
    }

    // Posición razonable: sobre la protoboard, o frente a la cámara, o un punto por defecto.
    static Vector3 PosicionSugerida()
    {
        var proto = Object.FindAnyObjectByType<ProtoboardSimulator>();
        if (proto != null) return proto.transform.position + Vector3.up * 0.10f + proto.transform.right * 0.35f;

        var cam = Camera.main;
        if (cam != null) return cam.transform.position + cam.transform.forward * 0.6f - Vector3.up * 0.2f;

        return new Vector3(0f, 1f, 0.5f);
    }
}
#endif
