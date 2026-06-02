using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Panel de "OBJETIVO" del Reto 4 — muestra el boceto (Reto 4 boceto.png) como meta
/// visual del circuito a construir.
///
/// IMPORTANTE — rol asimétrico: este panel pertenece al EXPLORADOR (VR), que es quien
/// ve el mundo 3D. NO debe mostrarse nunca al Técnico (PC), cuya regla de diseño es
/// "no puede ver el modelo 3D del Arduino/protoboard". Ver memoria reto4-roles-asimetricos.
///
/// Se auto-construye un Canvas WorldSpace en Awake (no requiere cablear nada en escena);
/// basta asignar <see cref="bocetoSprite"/> — el Editor tool lo hace automáticamente.
///
/// Toggle: tecla B (Editor/teclado) o llamar <see cref="Toggle"/> desde un botón VR.
/// </summary>
public class Reto4GoalBlueprint : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Contenido")]
    [Tooltip("Sprite del boceto. Asignado por el Editor tool desde Assets/Art/Reto4_Boceto.png.")]
    public Sprite bocetoSprite;

    [Tooltip("Título del panel.")]
    public string titulo = "OBJETIVO — Reto 4";

    [TextArea(2, 4)]
    [Tooltip("Pie de ayuda mostrado bajo el boceto.")]
    public string subtitulo =
        "Arma este circuito: Arduino → resistencia (≥100 Ω) → LED → GND.\n" +
        "Mide con el multímetro y avisa al Técnico qué pin usaste.";

    [Header("Construcción del panel")]
    [Tooltip("Construye el Canvas WorldSpace automáticamente en Awake.")]
    public bool autoBuild = true;
    [Tooltip("Ancho del panel en metros (WorldSpace).")]
    public float anchoMetros = 0.9f;
    [Tooltip("Visible al iniciar.")]
    public bool visibleAlInicio = true;

    // ─────────────────────────────────────────────
    //  Estado
    // ─────────────────────────────────────────────
    private GameObject _panelRoot;
    private bool _visible;

    // ─────────────────────────────────────────────
    //  Unity
    // ─────────────────────────────────────────────
    void Awake()
    {
        if (autoBuild) BuildPanel();
        SetVisible(visibleAlInicio);
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame)
            Toggle();
#endif
    }

    // ─────────────────────────────────────────────
    //  API pública
    // ─────────────────────────────────────────────
    /// <summary>Alterna la visibilidad del panel. Conectar a un botón/trigger VR.</summary>
    public void Toggle() => SetVisible(!_visible);

    public void SetVisible(bool v)
    {
        _visible = v;
        if (_panelRoot != null) _panelRoot.SetActive(v);
    }

    // ─────────────────────────────────────────────
    //  Construcción procedural del Canvas WorldSpace
    // ─────────────────────────────────────────────
    void BuildPanel()
    {
        if (_panelRoot != null) return;

        float aspecto = (bocetoSprite != null && bocetoSprite.rect.height > 0f)
            ? bocetoSprite.rect.width / bocetoSprite.rect.height
            : 16f / 9f;

        float w = anchoMetros;
        float h = w / aspecto;

        // Raíz Canvas
        _panelRoot = new GameObject("BlueprintCanvas");
        _panelRoot.transform.SetParent(transform, false);

        var canvas = _panelRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rt = _panelRoot.GetComponent<RectTransform>();
        // 1 unidad de canvas = 1 px lógico; escalamos a metros con localScale
        rt.sizeDelta = new Vector2(1000f, 1000f * (h + 0.28f * w) / w);
        float pxToM = w / 1000f;
        rt.localScale = new Vector3(pxToM, pxToM, pxToM);

        _panelRoot.AddComponent<GraphicRaycaster>();

        // Fondo translúcido tipo blueprint
        var bg = NewChild(rt, "BG", new Vector2(1000f, rt.sizeDelta.y));
        var bgImg = bg.gameObject.AddComponent<Image>();
        bgImg.color = new Color(0.03f, 0.10f, 0.16f, 0.92f);

        // Título
        var titleRt = NewChild(rt, "Titulo", new Vector2(960f, 90f));
        titleRt.anchoredPosition = new Vector2(0f, rt.sizeDelta.y * 0.5f - 60f);
        var title = titleRt.gameObject.AddComponent<TextMeshProUGUI>();
        title.text = titulo;
        title.fontSize = 56;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(0.0f, 1.0f, 0.70f); // neón #00FFB2

        // Imagen del boceto
        float imgH = 1000f * (h / w);
        var imgRt = NewChild(rt, "Boceto", new Vector2(940f, imgH));
        imgRt.anchoredPosition = new Vector2(0f, 30f);
        var img = imgRt.gameObject.AddComponent<Image>();
        img.preserveAspect = true;
        if (bocetoSprite != null) img.sprite = bocetoSprite;
        else img.color = new Color(0.1f, 0.15f, 0.2f, 1f);

        // Subtítulo
        var subRt = NewChild(rt, "Subtitulo", new Vector2(940f, 150f));
        subRt.anchoredPosition = new Vector2(0f, -rt.sizeDelta.y * 0.5f + 90f);
        var sub = subRt.gameObject.AddComponent<TextMeshProUGUI>();
        sub.text = subtitulo;
        sub.fontSize = 34;
        sub.alignment = TextAlignmentOptions.Center;
        sub.color = new Color(0.85f, 0.92f, 1f);
    }

    static RectTransform NewChild(RectTransform parent, string name, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.sizeDelta = size;
        rt.anchoredPosition = Vector2.zero;
        return rt;
    }
}
