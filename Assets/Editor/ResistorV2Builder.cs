#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Genera Delivered_Resistor_V2.prefab listo para asignar en
/// ComponentDeliverySystem.resistorPrefab.
///
/// Estructura:
///   Root (Cube, escala 0.022×0.040×0.022) — BoxCollider + Rigidbody + XRGrabInteractable + GrabbableComponent + Resistor
///     Visual  (Potentiometer.prefab, escala 1 heredada) — solo render, sin colisiones
///
/// El Potentiometer es el asset Vol.2 más parecido a un resistor de cuerpo cilíndrico.
///
/// Menú: Tools → TITA → Crear Delivered_Resistor_V2
/// </summary>
public static class ResistorV2Builder
{
    const string PREFAB_OUT = "Assets/Prefabs/Delivered/Delivered_Resistor_V2.prefab";
    const string V2         = "Assets/Resources Vol.2 - Electronics/Prefabs/";
    const string MAT_FOLDER = "Assets/Materials/Retos";

    [MenuItem("Tools/TITA/Crear Delivered_Resistor_V2")]
    public static void Build()
    {
        EnsureFolders();

        // ── Raíz limpia ──────────────────────────────────────────────
        var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
        root.name = "Delivered_Resistor_V2";
        root.transform.localScale = new Vector3(0.022f, 0.040f, 0.022f);

        // ── Visual Vol.2 ─────────────────────────────────────────────
        string vppath  = V2 + "Potentiometer.prefab";
        var    vprefab = AssetDatabase.LoadAssetAtPath<GameObject>(vppath);
        if (vprefab != null)
        {
            var vis = Object.Instantiate(vprefab, root.transform);
            vis.name = "Visual";
            vis.transform.localPosition = Vector3.zero;
            vis.transform.localScale    = Vector3.one;
            foreach (var col in vis.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(col);
            // Ocultar el cubo raíz cuando hay visual
            var rootRend = root.GetComponent<Renderer>();
            if (rootRend != null) rootRend.enabled = false;
        }
        else
        {
            // Sin prefab Vol.2: cubo marrón que evoca una resistencia
            SetColor(root, new Color(0.65f, 0.42f, 0.18f), "mat_resistor_v2");
        }

        // ── Colisión ─────────────────────────────────────────────────
        Object.DestroyImmediate(root.GetComponent<Collider>());
        var bc = root.AddComponent<BoxCollider>();
        bc.isTrigger = false;

        // ── Física ───────────────────────────────────────────────────
        var rb = root.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;

        // ── XR Interacción ───────────────────────────────────────────
        var grab = root.AddComponent<XRGrabInteractable>();
        grab.movementType  = XRBaseInteractable.MovementType.Kinematic;
        grab.trackPosition = true;
        grab.trackRotation = true;

        root.AddComponent<GrabbableComponent>();

        // ── Componente eléctrico ─────────────────────────────────────
        var res = root.AddComponent<Resistor>();
        res.resistance        = 100f;
        res.correctResistance = 100f;
        res.faultyResistance  = 10f;
        res.hasFault          = false;

        // ── Guardar prefab ───────────────────────────────────────────
        var saved = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_OUT);
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();

        if (saved != null) EditorGUIUtility.PingObject(saved);

        EditorUtility.DisplayDialog(
            "Prefab creado",
            $"Delivered_Resistor_V2.prefab guardado en:\n{PREFAB_OUT}\n\n" +
            "SIGUIENTE PASO:\n" +
            "Asignar este prefab en:\n  ComponentDeliverySystem → Resistor Prefab",
            "OK");

        Debug.Log("[ResistorV2Builder] ✓ Delivered_Resistor_V2.prefab creado.");
    }

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(MAT_FOLDER))
            AssetDatabase.CreateFolder("Assets/Materials", "Retos");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Delivered"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Delivered");
    }

    static void SetColor(GameObject go, Color color, string matName)
    {
        var r = go.GetComponent<Renderer>();
        if (r == null) return;
        string path   = $"{MAT_FOLDER}/{matName}.mat";
        var    shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var    mat    = new Material(shader);
        mat.SetColor("_BaseColor", color);
        mat.color = color;
        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
            AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(mat, path);
        r.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
    }
}
#endif
