using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Crea visualmente una mano (palma + dedos articulados) sobre el controlador VR y ANIMA los
/// dedos según el grip/gatillo del mando. Añadir a LeftHand / RightHand (los GO con
/// TrackedPoseDriver que siguen al controlador).
///
///   - El TrackedPoseDriver mueve el GameObject → la mano sigue al mando exacto.
///   - Cada dedo cuelga de una articulación (joint) en el nudillo, así se puede cerrar.
///   - grip  → cierra los 4 dedos.  gatillo → cierra extra el índice + pulgar.
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

    [Header("Animación de dedos")]
    [Tooltip("Anima los dedos con el grip/gatillo del mando.")]
    public bool animarDedos = true;
    [Tooltip("Ángulo máximo de cierre (grados). Si los dedos se cierran AL REVÉS, ponlo negativo.")]
    public float curlMaximo = 80f;
    [Tooltip("Suavizado del cierre.")]
    public float velocidadCurl = 14f;

    // ─────────────────────────────────────────────
    //  Datos anatómicos de dedos (mano izquierda — se espeja para la derecha)
    // ─────────────────────────────────────────────
    static readonly float[] FingerX     = { -0.029f, -0.010f,  0.009f,  0.028f };
    static readonly float[] FingerLen0  = {  0.042f,  0.047f,  0.043f,  0.032f }; // proximal
    static readonly float[] FingerLen1  = {  0.028f,  0.030f,  0.028f,  0.022f }; // media+distal
    static readonly float[] FingerRad   = {  0.0075f, 0.0080f, 0.0075f, 0.0065f };

    // Articulaciones (para animar el cierre)
    readonly List<Transform> _jointsDedos = new();
    Transform _jointPulgar;
    Transform _pivot;   // raíz de la mano (para ajustar rotationOffset en vivo)

    // Input XR
    readonly List<InputDevice> _devices = new();
    float _curlDedos, _curlPulgar, _curlIndice;

    void Start()
    {
        RemovePlaceholders();
        BuildHand();
        CacheDevices();
    }

    void Update()
    {
        // Aplicar rotationOffset en vivo → se puede ajustar la orientación de la mano en Play.
        if (_pivot != null) _pivot.localRotation = Quaternion.Euler(rotationOffset);

        if (!animarDedos) return;
        if (_devices.Count == 0) CacheDevices();

        float grip = ReadFeature(CommonUsages.grip);
        float trig = ReadFeature(CommonUsages.trigger);

        float dt = Time.deltaTime;
        _curlDedos  = Mathf.Lerp(_curlDedos,  grip,                 velocidadCurl * dt);
        _curlIndice = Mathf.Lerp(_curlIndice, Mathf.Max(grip, trig), velocidadCurl * dt);
        _curlPulgar = Mathf.Lerp(_curlPulgar, Mathf.Max(grip, trig), velocidadCurl * dt);

        // Dedos 0..3 (índice=1 usa su propio valor con el gatillo).
        for (int i = 0; i < _jointsDedos.Count; i++)
        {
            float c = (i == 1 ? _curlIndice : _curlDedos) * curlMaximo;
            _jointsDedos[i].localRotation = Quaternion.Euler(c, 0f, 0f);
        }
        if (_jointPulgar != null)
            _jointPulgar.localRotation = Quaternion.Euler(0f, 0f, -_curlPulgar * curlMaximo * 0.6f);
    }

    // ─────────────────────────────────────────────
    //  Input
    // ─────────────────────────────────────────────
    void CacheDevices()
    {
        var car = (hand == HandSide.Left ? InputDeviceCharacteristics.Left : InputDeviceCharacteristics.Right)
                  | InputDeviceCharacteristics.Controller;
        InputDevices.GetDevicesWithCharacteristics(car, _devices);
    }

    float ReadFeature(InputFeatureUsage<float> usage)
    {
        foreach (var d in _devices)
            if (d.isValid && d.TryGetFeatureValue(usage, out float v))
                return v;
        return 0f;
    }

    // ─────────────────────────────────────────────
    //  Construcción
    // ─────────────────────────────────────────────
    void RemovePlaceholders()
    {
        foreach (Transform child in transform)
            if (child.name == "HandPlaceholder_Replace")
                Destroy(child.gameObject);
    }

    [ContextMenu("Reconstruir Mano")]
    public void RebuildInEditor()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            if (transform.GetChild(i).name.StartsWith("Hand_"))
                DestroyImmediate(transform.GetChild(i).gameObject);
        _jointsDedos.Clear();
        _jointPulgar = null;
        BuildHand();
    }

    void BuildHand()
    {
        if (handMaterial == null) handMaterial = CreateSkinMaterial();

        var pivot = new GameObject("Hand_Pivot");
        pivot.transform.SetParent(transform, false);
        pivot.transform.localPosition = Vector3.zero;
        pivot.transform.localRotation = Quaternion.Euler(rotationOffset);
        if (hand == HandSide.Right)
            pivot.transform.localScale = new Vector3(-1f, 1f, 1f);
        _pivot = pivot.transform;

        // Palma
        AddPart(pivot, "Hand_Palm", new Vector3(0f, 0f, 0.038f), Quaternion.identity,
            new Vector3(0.078f, 0.020f, 0.090f), PrimitiveType.Cube);
        AddPart(pivot, "Hand_PalmRound", new Vector3(0f, 0f, 0.038f), Quaternion.identity,
            new Vector3(0.076f, 0.018f, 0.092f), PrimitiveType.Sphere);

        // 4 dedos, cada uno colgando de una articulación en el nudillo.
        float zBase = 0.083f;
        for (int i = 0; i < 4; i++)
        {
            float x = FingerX[i], r = FingerRad[i], l0 = FingerLen0[i], l1 = FingerLen1[i];

            var joint = new GameObject($"Hand_Finger{i}_Joint");
            joint.transform.SetParent(pivot.transform, false);
            joint.transform.localPosition = new Vector3(x, 0f, zBase);
            _jointsDedos.Add(joint.transform);

            AddPart(joint, $"Hand_Finger{i}A", new Vector3(0f, 0f, l0 * 0.5f),
                Quaternion.Euler(90f, 0f, 0f), new Vector3(r * 2f, l0 * 0.5f, r * 2f), PrimitiveType.Capsule);
            AddPart(joint, $"Hand_Finger{i}B", new Vector3(0f, 0f, l0 + l1 * 0.5f),
                Quaternion.Euler(90f, 0f, 0f), new Vector3(r * 1.75f, l1 * 0.5f, r * 1.75f), PrimitiveType.Capsule);
            AddPart(joint, $"Hand_Knuckle{i}", new Vector3(0f, 0.004f, 0f),
                Quaternion.identity, new Vector3(r * 2.2f, r * 1.8f, r * 2.2f), PrimitiveType.Sphere);
        }

        // Pulgar con su articulación.
        var jp = new GameObject("Hand_Thumb_Joint");
        jp.transform.SetParent(pivot.transform, false);
        jp.transform.localPosition = new Vector3(0.043f, 0.003f, 0.015f);
        _jointPulgar = jp.transform;
        AddPart(jp, "Hand_ThumbBase", new Vector3(0f, 0f, 0f),
            Quaternion.Euler(90f, 0f, -40f), new Vector3(0.017f, 0.021f, 0.017f), PrimitiveType.Capsule);
        AddPart(jp, "Hand_ThumbTip", new Vector3(0.017f, 0f, 0.025f),
            Quaternion.Euler(90f, 0f, -30f), new Vector3(0.015f, 0.016f, 0.015f), PrimitiveType.Capsule);
    }

    void AddPart(GameObject parent, string partName,
                 Vector3 localPos, Quaternion localRot, Vector3 scale, PrimitiveType type)
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

    Material CreateSkinMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                     ?? Shader.Find("URP/Lit") ?? Shader.Find("Lit") ?? Shader.Find("Standard");
        if (shader == null)
        {
            Debug.LogWarning("[HandModelController] Shader URP no encontrado. Asigna un material manual.");
            return new Material(Shader.Find("Hidden/InternalErrorShader"));
        }
        var mat = new Material(shader);
        var baseColor = new Color(0.88f, 0.68f, 0.52f);
        mat.SetColor("_BaseColor", baseColor);
        mat.SetColor("_Color",     baseColor);
        mat.SetFloat("_Smoothness", 0.35f);
        mat.SetFloat("_Glossiness", 0.35f);
        mat.SetFloat("_Metallic",   0f);
        mat.SetFloat("_GlossyReflections", 1f);
        mat.SetFloat("_Cull",     0f);
        mat.SetFloat("_CullMode", 0f);
        return mat;
    }
}
