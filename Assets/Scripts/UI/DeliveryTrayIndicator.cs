using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ZONA DE RECEPCIÓN DE COMPONENTES — Indicador holográfico WorldSpace.
///
/// Elementos generados en Awake (si no están asignados desde el Inspector):
///   • LineRenderer: borde neón rectangular sobre la mesa (4 esquinas).
///   • Label flotante: "MATERIALES RECIBIDOS" a 5 cm sobre la mesa.
///   • Cilindro holográfico semitransparente (opcional): marcador de altura.
///   • PointLight suave para la glow de la bandeja.
///
/// Comportamiento de red:
///   • Idle        → borde cyan latiendo suavemente (alpha 0.2 ↔ 0.5).
///   • OnComponent → borde cyan full-bright + pulse rápido + háptica.
///   • OnInstall   → flash verde 0.4s.
///   • OnReject    → flash rojo 0.2s.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class DeliveryTrayIndicator : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector — geometría
    // ─────────────────────────────────────────────
    [Header("Dimensiones de la bandeja (en unidades locales)")]
    [Tooltip("Ancho del rectángulo de la bandeja sobre la mesa.")]
    public float width  = 0.22f;
    [Tooltip("Profundidad del rectángulo.")]
    public float depth  = 0.16f;
    [Tooltip("Altura del borde sobre la superficie de la mesa.")]
    public float height = 0.002f;

    [Header("Label holográfico")]
    [Tooltip("Canvas WorldSpace del label flotante. Se crea automáticamente si es null.")]
    public Canvas labelCanvas;
    public TMP_Text txtLabel;

    [Header("Luz ambiental de la bandeja")]
    public Light trayLight;

    [Header("Háptica (opcional)")]
    public HapticFeedback haptics;

    // ─────────────────────────────────────────────
    //  Colores
    // ─────────────────────────────────────────────
    static Color C(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }
    static readonly Color _cyan       = C("#00E5FF");
    static readonly Color _green      = C("#00E676");
    static readonly Color _red        = C("#FF3D3D");
    static readonly Color _labelColor = C("#D8EEFF");

    // ─────────────────────────────────────────────
    //  Internos
    // ─────────────────────────────────────────────
    private LineRenderer _lr;
    private float        _pulseT;
    private bool         _pulseFast;
    private Color        _currentColor;
    private Coroutine    _flashRoutine;
    private float        _lightBaseIntensity = 0.15f;

    // ─────────────────────────────────────────────
    //  Unity
    // ─────────────────────────────────────────────
    void Awake()
    {
        _lr = GetComponent<LineRenderer>();
        BuildBorderRect();
        EnsureLabel();
        if (haptics == null) haptics = FindAnyObjectByType<HapticFeedback>();
    }

    void OnEnable()
    {
        ComponentDeliverySystem.OnComponentSent      += OnComponentArrived;
        ComponentDeliverySystem.OnComponentInstalled += OnInstalled;
    }

    void OnDisable()
    {
        ComponentDeliverySystem.OnComponentSent      -= OnComponentArrived;
        ComponentDeliverySystem.OnComponentInstalled -= OnInstalled;
    }

    void Update()
    {
        AnimateBorder();
    }

    // ─────────────────────────────────────────────
    //  Geometría del borde
    // ─────────────────────────────────────────────

    void BuildBorderRect()
    {
        float hw = width  * 0.5f;
        float hd = depth  * 0.5f;
        float y  = height;

        _lr.loop           = true;
        _lr.positionCount  = 4;
        _lr.useWorldSpace  = false;
        _lr.widthMultiplier = 0.004f;
        _lr.numCapVertices  = 4;

        _lr.SetPosition(0, new Vector3(-hw, y, -hd));
        _lr.SetPosition(1, new Vector3( hw, y, -hd));
        _lr.SetPosition(2, new Vector3( hw, y,  hd));
        _lr.SetPosition(3, new Vector3(-hw, y,  hd));

        // Material simple additive
        var mat = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit"));
        if (mat != null)
        {
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.EnableKeyword("_EMISSION");
        }
        _lr.material = mat;
        _currentColor = _cyan;
        SetLineColor(_cyan, 0.35f);
    }

    void EnsureLabel()
    {
        if (labelCanvas != null && txtLabel != null) return;

        var labelGO = new GameObject("Label_Bandeja");
        labelGO.transform.SetParent(transform, false);
        labelGO.transform.localPosition = new Vector3(0f, 0.05f, -depth * 0.5f - 0.03f);
        labelGO.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        labelGO.transform.localScale    = Vector3.one * 0.001f;

        labelCanvas = labelGO.AddComponent<Canvas>();
        labelCanvas.renderMode = RenderMode.WorldSpace;

        var rt = labelGO.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200f, 30f);

        var bgGO  = new GameObject("Bg");
        bgGO.transform.SetParent(labelGO.transform, false);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.03f, 0.07f, 0.12f, 0.85f);
        var bgRT  = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

        var txtGO = new GameObject("Txt");
        txtGO.transform.SetParent(labelGO.transform, false);
        txtLabel = txtGO.AddComponent<TextMeshProUGUI>();
        txtLabel.text      = "MATERIALES RECIBIDOS";
        txtLabel.fontSize  = 10f;
        txtLabel.color     = _cyan;
        txtLabel.alignment = TextAlignmentOptions.Center;
        txtLabel.fontStyle = FontStyles.Bold;

        var txtRT = txtGO.GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = new Vector2(4, 2); txtRT.offsetMax = new Vector2(-4, -2);

        // Neon border on label
        var borderGO = new GameObject("Border");
        borderGO.transform.SetParent(labelGO.transform, false);
        var borderImg = borderGO.AddComponent<Image>();
        borderImg.color = new Color(_cyan.r, _cyan.g, _cyan.b, 0.5f);
        var borderRT = borderGO.GetComponent<RectTransform>();
        borderRT.anchorMin = Vector2.zero; borderRT.anchorMax = Vector2.one;
        borderRT.offsetMin = new Vector2(-1, -1); borderRT.offsetMax = new Vector2(1, 1);
        borderImg.transform.SetSiblingIndex(0);
    }

    // ─────────────────────────────────────────────
    //  Animación de pulso
    // ─────────────────────────────────────────────

    void AnimateBorder()
    {
        float speed = _pulseFast ? 6f : 1.2f;
        _pulseT += Time.deltaTime * speed;

        float alpha = _pulseFast
            ? Mathf.Abs(Mathf.Sin(_pulseT)) * 0.8f + 0.2f
            : Mathf.Lerp(0.2f, 0.5f, (Mathf.Sin(_pulseT) + 1f) * 0.5f);

        SetLineColor(_currentColor, alpha);

        if (trayLight != null)
        {
            trayLight.color     = _currentColor;
            trayLight.intensity = _lightBaseIntensity * alpha;
        }
    }

    void SetLineColor(Color col, float alpha)
    {
        var c = new Color(col.r, col.g, col.b, alpha);
        _lr.startColor = c;
        _lr.endColor   = c;
    }

    // ─────────────────────────────────────────────
    //  Eventos de red
    // ─────────────────────────────────────────────

    void OnComponentArrived(ComponentType _, float __)
    {
        if (_flashRoutine != null) StopCoroutine(_flashRoutine);
        _flashRoutine = StartCoroutine(FlashBorder(_cyan, 2.5f, fast: true));

        if (txtLabel != null)
        {
            txtLabel.text  = "¡COMPONENTE RECIBIDO!";
            txtLabel.color = _cyan;
            StartCoroutine(ResetLabelAfter(3f));
        }

        haptics?.PlayMedium();
    }

    void OnInstalled(bool correcto)
    {
        if (_flashRoutine != null) StopCoroutine(_flashRoutine);
        _flashRoutine = StartCoroutine(FlashBorder(correcto ? _green : _red, 0.5f, fast: false));
    }

    IEnumerator FlashBorder(Color color, float duration, bool fast)
    {
        _currentColor = color;
        _pulseFast    = fast;
        _lightBaseIntensity = 0.4f;

        yield return new WaitForSeconds(duration);

        _currentColor = _cyan;
        _pulseFast    = false;
        _lightBaseIntensity = 0.15f;
    }

    IEnumerator ResetLabelAfter(float t)
    {
        yield return new WaitForSeconds(t);
        if (txtLabel != null)
        {
            txtLabel.text  = "MATERIALES RECIBIDOS";
            txtLabel.color = _cyan;
        }
    }
}
