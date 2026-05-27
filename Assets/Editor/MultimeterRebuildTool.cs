#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Reconstruye Multimeter_VR.prefab:
///   • Desactiva el renderer del contenedor Body (que causaba el cubo gigante)
///   • Aplica materiales guardados como assets a Body_Main / Body_Band / Display_Back
///   • Puntas físicas: Probe_Red_Tip y Probe_Black_Tip con MultimeterProbe + trigger
///   • Botón de modo: cicla DCVoltage → DCCurrent → Resistance
///
/// Menú: Tools → TITA → Multímetro → Reconstruir Multimeter_VR
/// </summary>
public static class MultimeterRebuildTool
{
    const string PREFAB_PATH = "Assets/Prefabs/Multimeter_VR.prefab";
    const string MAT_FOLDER  = "Assets/Materials/Multimeter";

    [MenuItem("Tools/TITA/Multímetro/Reconstruir Multimeter_VR")]
    public static void Rebuild()
    {
        var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
        if (prefabAsset == null)
        {
            EditorUtility.DisplayDialog("Error",
                $"No se encontró {PREFAB_PATH}.\n" +
                "Asegúrate de que el prefab existe antes de reconstruirlo.", "OK");
            return;
        }

        EnsureMatFolder();

        var go = PrefabUtility.LoadPrefabContents(PREFAB_PATH);

        FixBody(go);
        EnsureProbes(go);
        EnsureModeButton(go);

        var rb = go.GetComponent<Rigidbody>() ?? go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;

        PrefabUtility.SaveAsPrefabAsset(go, PREFAB_PATH);
        PrefabUtility.UnloadPrefabContents(go);
        AssetDatabase.Refresh();

        var result = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
        if (result != null) EditorGUIUtility.PingObject(result);

        EditorUtility.DisplayDialog(
            "Multímetro reconstruido",
            "Multimeter_VR.prefab actualizado:\n\n" +
            "  ✓ Body visual (cubo negro, franja naranja, pantalla)\n" +
            "  ✓ Probe_Red_Tip   — punta roja física\n" +
            "  ✓ Probe_Black_Tip — punta negra física\n" +
            "  ✓ Mode_Button     — cicla V / A / Ω\n\n" +
            "SIGUIENTE PASO:\n" +
            "Asignar en el Inspector de Multimeter.cs:\n" +
            "  indicatorRed, indicatorBlack\n" +
            "  txtVoltage, txtCurrent, txtStatus, txtMode",
            "OK");

        Debug.Log("[MultimeterRebuildTool] ✓ Multimeter_VR.prefab reconstruido.");
    }

    // ─────────────────────────────────────────────
    //  Body visual
    // ─────────────────────────────────────────────

    // La jerarquía del prefab original es:
    //   Body (escala 1,1,1 — contenedor, tiene su propio Cube mesh → lo desactivamos)
    //     Body_Main    (0.075 × 0.13 × 0.025) ← cuerpo real, color negro
    //     Body_Band    (0.075 × 0.025 × 0.001) at y=0.05 z=-0.013 ← franja naranja
    //     Display_Back (0.055 × 0.035 × 0.001) at y=0.02 z=-0.013 ← fondo de pantalla
    //     Deco_Bareboard (prefab Vol.2 — ya está)
    static void FixBody(GameObject root)
    {
        var body = root.transform.Find("Body");
        if (body == null)
        {
            Debug.LogWarning("[MultimeterRebuildTool] No se encontró el hijo 'Body' en el prefab.");
            return;
        }

        // El contenedor Body tiene su propio Cube mesh a escala 1×1×1 que rodea todo
        // → desactivar su renderer para que no se vea ese cubo negro enorme
        var bodyRenderer = body.GetComponent<Renderer>();
        if (bodyRenderer != null) bodyRenderer.enabled = false;

        // Cuerpo principal: negro oscuro
        ApplyColor(body, "Body_Main",    new Color(0.12f, 0.12f, 0.12f), "mat_body_dark");
        // Franja superior naranja
        ApplyColor(body, "Body_Band",    new Color(1f, 0.55f, 0f),       "mat_body_orange");
        // Fondo verde oscuro de la pantalla
        ApplyColor(body, "Display_Back", new Color(0.04f, 0.10f, 0.04f), "mat_body_display");
    }

    static void ApplyColor(Transform parent, string childName, Color color, string matName)
    {
        var child = parent.Find(childName);
        if (child == null) return;
        SetColor(child.gameObject, color, matName);
    }

    // ─────────────────────────────────────────────
    //  Puntas físicas
    // ─────────────────────────────────────────────

    static void EnsureProbes(GameObject root)
    {
        var old = root.transform.Find("Probes");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        var probesGO = new GameObject("Probes");
        probesGO.transform.SetParent(root.transform);
        probesGO.transform.localPosition = Vector3.zero;
        probesGO.transform.localRotation = Quaternion.identity;
        probesGO.transform.localScale    = Vector3.one;

        CreateProbe(probesGO, "Probe_Red_Tip",
                    new Vector3( 0.018f, -0.075f, 0f),
                    new Color(0.9f, 0.1f, 0.1f), ProbeType.Red);

        CreateProbe(probesGO, "Probe_Black_Tip",
                    new Vector3(-0.018f, -0.075f, 0f),
                    new Color(0.1f, 0.1f, 0.1f), ProbeType.Black);
    }

    static void CreateProbe(GameObject parent, string name, Vector3 localPos,
                            Color color, ProbeType probeType)
    {
        // Varilla (cable)
        var rod = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rod.name = name + "_Rod";
        rod.transform.SetParent(parent.transform);
        rod.transform.localPosition = localPos + new Vector3(0f, -0.01f, 0f);
        rod.transform.localRotation = Quaternion.identity;
        rod.transform.localScale    = new Vector3(0.004f, 0.015f, 0.004f);
        SetColor(rod, new Color(color.r * 0.6f, color.g * 0.6f, color.b * 0.6f),
                 name + "_rod");
        Object.DestroyImmediate(rod.GetComponent<Collider>());

        // Punta esférica con trigger
        var tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        tip.name = name;
        tip.transform.SetParent(parent.transform);
        tip.transform.localPosition = localPos + new Vector3(0f, -0.028f, 0f);
        tip.transform.localRotation = Quaternion.identity;
        tip.transform.localScale    = new Vector3(0.008f, 0.008f, 0.008f);
        SetColor(tip, color, name + "_tip");

        var col = tip.GetComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius    = 1.2f;

        var probe = tip.AddComponent<MultimeterProbe>();
        probe.probeType = probeType;
    }

    // ─────────────────────────────────────────────
    //  Botón de modo
    // ─────────────────────────────────────────────

    static void EnsureModeButton(GameObject root)
    {
        var existing = root.transform.Find("Mode_Button");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        var btn = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        btn.name = "Mode_Button";
        btn.transform.SetParent(root.transform);
        btn.transform.localPosition = new Vector3(0.025f, 0.04f, -0.014f);
        btn.transform.localScale    = new Vector3(0.012f, 0.006f, 0.012f);
        SetColor(btn, new Color(1f, 0.85f, 0f), "mat_btn_voltage");

        Object.DestroyImmediate(btn.GetComponent<Collider>());
        var boxCol = btn.AddComponent<BoxCollider>();
        boxCol.size = new Vector3(1f, 2f, 1f);

        btn.AddComponent<XRSimpleInteractable>();
        btn.AddComponent<MultimeterModeButton>();
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────

    static void EnsureMatFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(MAT_FOLDER))
            AssetDatabase.CreateFolder("Assets/Materials", "Multimeter");
    }

    // Crea el material como asset persistente para que sobreviva al SaveAsPrefabAsset.
    // Un 'new Material()' sin guardarlo en AssetDatabase se pierde al serializar el prefab.
    // Shader buscado por GUID hardcodeado para garantizar URP/Lit correcto en este proyecto.
    static void SetColor(GameObject go, Color color, string matName)
    {
        var r = go.GetComponent<Renderer>();
        if (r == null) return;

        string path = $"{MAT_FOLDER}/{matName}.mat";

        // GUID verificado de Universal Render Pipeline/Lit en este proyecto.
        // Fallback a Shader.Find si el GUID no resuelve (distinta instalación URP).
        const string urpLitGuid = "933532a4fcc9baf4fa0491de14d08ed7";
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(
                         AssetDatabase.GUIDToAssetPath(urpLitGuid))
                  ?? Shader.Find("Universal Render Pipeline/Lit")
                  ?? Shader.Find("Standard");

        var mat = new Material(shader);
        mat.SetColor("_BaseColor", color);
        mat.color = color;

        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
            AssetDatabase.DeleteAsset(path);

        AssetDatabase.CreateAsset(mat, path);
        r.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
    }
}
#endif
