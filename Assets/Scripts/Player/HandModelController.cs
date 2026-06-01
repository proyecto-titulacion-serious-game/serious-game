using UnityEngine;

/// <summary>
/// Crea visualmente una mano (palma + dedos) sobre el controlador VR.
/// Añadir a LeftHand_Controller y RightHand_Controller.
/// El TrackedPoseDriver ya mueve el GameObject — este script solo construye la geometría.
///
/// Correcciones vs versión anterior:
///   - La mano derecha se construye con scale.x = -1 en el pivot para que el
///     pulgar quede hacia adentro (lado correcto para cada mano).
///   - Material URP con Smoothness + Specular para aspecto de piel real.
///   - Dedos con 2 segmentos (falange proximal + media) para que no parezcan
///     palos de madera.
///   - Falanges ligeramente escalonadas por tamaño.
/// </summary>
public class HandModelController : MonoBehaviour
{
    public enum HandSide { Left, Right }

    [Header("Configuracion")]
    public HandSide hand = HandSide.Left;

    [Tooltip("Ajuste de rotacion para alinear la mano con la pose del controller Meta Quest 3.")]
    public Vector3 rotationOffset = new Vector3(-60f, 0f, 0f);

    [Header("Material")]
    [Tooltip("Material de la mano. Se crea automaticamente con shader URP si queda vacio.")]
    public Material handMaterial;

    // ─────────────────────────────────────────────
    //  Datos anatómicos de dedos (mano izquierda — se espeja para la derecha)
    //  x = posición lateral (- = lado del meñique, + = lado del índice)
    //  len0 = longitud proximal, len1 = longitud media (en metros)
    // ─────────────────────────────────────────────
    static readonly float[] FingerX     = { -0.029f, -0.010f,  0.009f,  0.028f };
    static readonly float[] FingerLen0  = {  0.042f,  0.047f,  0.043f,  0.032f }; // proximal
    static readonly float[] FingerLen1  = {  0.028f,  0.030f,  0.028f,  0.022f }; // media+distal
    static readonly float[] FingerRad   = {  0.0075f, 0.0080f, 0.0075f, 0.0065f };

    void Start()
    {
        RemovePlaceholders();
        BuildHand();
    }

    [ContextMenu("Reconstruir Mano")]
    public void RebuildInEditor()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child.name.StartsWith("Hand_"))
                DestroyImmediate(child.gameObject);
        }
        BuildHand();
    }

    void RemovePlaceholders()
    {
        foreach (Transform child in transform)
        {
            if (child.name == "HandPlaceholder_Replace")
                Destroy(child.gameObject);
        }
    }

    void BuildHand()
    {
        if (handMaterial == null) handMaterial = CreateSkinMaterial();

        // Pivot con offset de rotacion
        var pivot = new GameObject("Hand_Pivot");
        pivot.transform.SetParent(transform, false);
        pivot.transform.localPosition = Vector3.zero;
        pivot.transform.localRotation = Quaternion.Euler(rotationOffset);

        // CLAVE: escala -1 en X para la mano derecha → el pulgar queda en el lado correcto
        // El material tiene Cull=Off para que las caras invertidas se rendericen bien
        if (hand == HandSide.Right)
            pivot.transform.localScale = new Vector3(-1f, 1f, 1f);

        // ── Palma (cuboide redondeado con esfera aplastada superpuesta) ──
        AddPart(pivot, "Hand_Palm",
            new Vector3(0f, 0f, 0.038f),
            Quaternion.identity,
            new Vector3(0.078f, 0.020f, 0.090f),
            PrimitiveType.Cube);

        // Arco metacarpal (esfera aplastada que redondea la palma)
        AddPart(pivot, "Hand_PalmRound",
            new Vector3(0f, 0f, 0.038f),
            Quaternion.identity,
            new Vector3(0.076f, 0.018f, 0.092f),
            PrimitiveType.Sphere);

        // ── 4 dedos con 2 falanges cada uno ──
        for (int i = 0; i < 4; i++)
        {
            float x    = FingerX[i];
            float r    = FingerRad[i];
            float l0   = FingerLen0[i];
            float l1   = FingerLen1[i];

            // Falange proximal (arranca en el borde de la palma)
            float zBase = 0.083f;
            AddPart(pivot, $"Hand_Finger{i}A",
                new Vector3(x, 0f, zBase + l0 * 0.5f),
                Quaternion.Euler(90f, 0f, 0f),
                new Vector3(r * 2f, l0 * 0.5f, r * 2f),
                PrimitiveType.Capsule);

            // Falange media+distal (ligeramente más delgada)
            float zMid = zBase + l0;
            AddPart(pivot, $"Hand_Finger{i}B",
                new Vector3(x, 0f, zMid + l1 * 0.5f),
                Quaternion.Euler(90f, 0f, 0f),
                new Vector3(r * 1.75f, l1 * 0.5f, r * 1.75f),
                PrimitiveType.Capsule);

            // Nudillo (pequeña esfera en la articulación)
            AddPart(pivot, $"Hand_Knuckle{i}",
                new Vector3(x, 0.004f, zBase),
                Quaternion.identity,
                new Vector3(r * 2.2f, r * 1.8f, r * 2.2f),
                PrimitiveType.Sphere);
        }

        // ── Pulgar (base = lado meñique → positivo X en coord. pivote izquierdo) ──
        // Para la mano izquierda: thumb en x positivo (lado índice - meñique).
        // La escala negativa del pivot derecho lo pone en el lado correcto automáticamente.
        AddPart(pivot, "Hand_ThumbBase",
            new Vector3(0.043f, 0.003f, 0.015f),
            Quaternion.Euler(90f, 0f, -40f),
            new Vector3(0.017f, 0.021f, 0.017f),
            PrimitiveType.Capsule);

        AddPart(pivot, "Hand_ThumbTip",
            new Vector3(0.060f, 0.003f, 0.040f),
            Quaternion.Euler(90f, 0f, -30f),
            new Vector3(0.015f, 0.016f, 0.015f),
            PrimitiveType.Capsule);
    }

    void AddPart(GameObject parent, string partName,
                 Vector3 localPos, Quaternion localRot, Vector3 scale,
                 PrimitiveType type)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = partName;
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;
        go.transform.localScale    = scale;

        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sharedMaterial = handMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);
    }

    // ─────────────────────────────────────────────
    //  Material de piel URP — con Smoothness + Specular
    // ─────────────────────────────────────────────
    Material CreateSkinMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                     ?? Shader.Find("URP/Lit")
                     ?? Shader.Find("Lit")
                     ?? Shader.Find("Standard");

        if (shader == null)
        {
            Debug.LogWarning("[HandModelController] Shader URP no encontrado. " +
                             "Asigna un material manualmente en el Inspector.");
            return new Material(Shader.Find("Hidden/InternalErrorShader"));
        }

        var mat = new Material(shader);

        // Color base: piel cálida con ligero tono rosado
        var baseColor = new Color(0.88f, 0.68f, 0.52f);
        mat.SetColor("_BaseColor", baseColor);
        mat.SetColor("_Color",     baseColor);

        // Suavidad y especular: aspecto de piel (no plástico brillante, no madera mate)
        mat.SetFloat("_Smoothness",         0.35f);
        mat.SetFloat("_Glossiness",         0.35f); // Standard shader
        mat.SetFloat("_Metallic",           0.00f);
        mat.SetFloat("_GlossyReflections",  1f);

        // Backface culling desactivado: la mano derecha tiene scale.x=-1 → caras invertidas
        // Sin esto, la mano derecha es invisible desde fuera
        mat.SetFloat("_Cull",      0f);   // 0 = Off, 1 = Front, 2 = Back
        mat.SetFloat("_CullMode",  0f);

        return mat;
    }
}
