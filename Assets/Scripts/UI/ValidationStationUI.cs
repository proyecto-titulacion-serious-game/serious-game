using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ESTACIÓN DE VALIDACIÓN — Panel holográfico y efectos de luz alrededor
/// del VRValidationButton del Explorador.
///
/// GEOMETRÍA:
///   • Panel inclinado −20° en X, 8×12 cm en world space (escala 1/1000).
///   • Leyenda de 4 estados con círculo de color + texto.
///   • PointLight que cambia color y parpadea según el estado de la red.
///   • Halo de partículas (opcional) en Aprobado.
///
/// ESTADOS DE RED (eventos GameSession):
///   Idle (azul)      → sin validación activa
///   Evaluando (ámbar) → GameSession.OnValidacionSolicitada
///   Aprobado (verde)  → OnResultadoValidacion(true, _)
///   Fallo (rojo)      → OnResultadoValidacion(false, _)
///
/// SUSCRIPCIONES:
///   GameSession.OnValidacionSolicitada  → SetState(Evaluando)
///   GameSession.OnResultadoValidacion   → SetState(Aprobado | Fallo)
///   GameManager.OnLevelLoaded           → Reset a Idle
/// </summary>
public class ValidationStationUI : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Estados
    // ─────────────────────────────────────────────
    public enum ValidationState { Idle, Evaluando, Aprobado, Fallo }

    // ─────────────────────────────────────────────
    //  Inspector — Panel leyenda
    // ─────────────────────────────────────────────
    [Header("Panel leyenda (Canvas WorldSpace)")]
    public Canvas legendCanvas;

    [Tooltip("Cuatro imágenes circulares para los indicadores de color.")]
    public Image  dotIdle, dotEval, dotPass, dotFail;
    [Tooltip("Cuatro labels de texto para la leyenda.")]
    public TMP_Text lblIdle, lblEval, lblPass, lblFail;

    [Header("Texto de estado grande (encima del botón)")]
    public TMP_Text txtEstado;

    // ─────────────────────────────────────────────
    //  Inspector — Luz ambiental
    // ─────────────────────────────────────────────
    [Header("Luz de ambiente del botón")]
    public Light stationLight;
    [Range(0f, 2f)] public float lightMaxIntensity = 0.6f;

    [Header("Partículas (opcional — Aprobado)")]
    public ParticleSystem particlesApproved;

    [Header("Háptica")]
    public HapticFeedback haptics;

    // ─────────────────────────────────────────────
    //  Colores
    // ─────────────────────────────────────────────
    static Color C(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

    static readonly Color _cyan     = C("#00E5FF");   // cyan holográfico
    static readonly Color _colIdle  = C("#2979FF");   // azul  — Listo
    static readonly Color _colEval  = C("#FFB300");   // ámbar — Evaluando
    static readonly Color _colPass  = C("#00E676");   // verde — Aprobado
    static readonly Color _colFail  = C("#FF3D3D");   // rojo  — Fallo
    static readonly Color _muted    = C("#6A8FA8");
    static readonly Color _textMain = C("#D8EEFF");
    static readonly Color _bgPanel  = C("#08111E");

    // ─────────────────────────────────────────────
    //  Estado interno
    // ─────────────────────────────────────────────
    private ValidationState _state = ValidationState.Idle;
    private Coroutine       _lightAnim;

    // ─────────────────────────────────────────────
    //  Unity
    // ─────────────────────────────────────────────
    void Awake()
    {
        if (haptics == null) haptics = FindAnyObjectByType<HapticFeedback>();
        EnsureLegendPanel();
    }

    void OnEnable()
    {
        GameSession.OnValidacionSolicitada += OnValidacionSolicitada;
        GameSession.OnResultadoValidacion  += OnResultadoValidacion;
        GameManager.OnLevelLoaded          += OnLevelLoaded;
    }

    void OnDisable()
    {
        GameSession.OnValidacionSolicitada -= OnValidacionSolicitada;
        GameSession.OnResultadoValidacion  -= OnResultadoValidacion;
        GameManager.OnLevelLoaded          -= OnLevelLoaded;
    }

    void Start()
    {
        var gm = FindAnyObjectByType<GameManager>();
        bool esReto4 = gm != null && gm.currentLevel == LevelType.Arduino;
        MostrarPanel(esReto4);
        if (esReto4) SetState(ValidationState.Idle);
    }

    void OnLevelLoaded(LevelType level)
    {
        bool esReto4 = level == LevelType.Arduino;
        MostrarPanel(esReto4);
        if (esReto4) SetState(ValidationState.Idle);
    }

    void MostrarPanel(bool visible)
    {
        if (legendCanvas != null) legendCanvas.gameObject.SetActive(visible);
        if (txtEstado    != null) txtEstado.gameObject.SetActive(visible);
        if (stationLight != null) stationLight.enabled = visible;
        if (particlesApproved != null) particlesApproved.gameObject.SetActive(visible);
    }

    // ─────────────────────────────────────────────
    //  Callbacks de red
    // ─────────────────────────────────────────────

    void OnValidacionSolicitada()
    {
        SetState(ValidationState.Evaluando);
        haptics?.PlayLight();
    }

    void OnResultadoValidacion(bool paso, int _)
    {
        SetState(paso ? ValidationState.Aprobado : ValidationState.Fallo);

        if (paso)
        {
            haptics?.PlayStrong();
            if (particlesApproved != null) particlesApproved.Play();
            StartCoroutine(AutoResetAfter(6f));
        }
        else
        {
            haptics?.PlayError();
            StartCoroutine(AutoResetAfter(5f));
        }
    }

    IEnumerator AutoResetAfter(float t)
    {
        yield return new WaitForSeconds(t);
        SetState(ValidationState.Idle);
    }

    // ─────────────────────────────────────────────
    //  SetState
    // ─────────────────────────────────────────────

    public void SetState(ValidationState state)
    {
        _state = state;

        Color activeCol = state switch
        {
            ValidationState.Idle      => _colIdle,
            ValidationState.Evaluando => _colEval,
            ValidationState.Aprobado  => _colPass,
            ValidationState.Fallo     => _colFail,
            _                          => _colIdle
        };

        string statusText = state switch
        {
            ValidationState.Idle      => "LISTO PARA\nVALIDAR",
            ValidationState.Evaluando => "EVALUANDO\nCIRCUITO...",
            ValidationState.Aprobado  => "¡CIRCUITO\nAPROBADO!",
            ValidationState.Fallo     => "ERROR EN\nCIRCUITO",
            _                          => "LISTO"
        };

        // Texto de estado
        if (txtEstado != null)
        {
            txtEstado.text  = statusText;
            txtEstado.color = activeCol;
        }

        // Highlight del punto activo en la leyenda
        HighlightDot(dotIdle, lblIdle, state == ValidationState.Idle,      _colIdle);
        HighlightDot(dotEval, lblEval, state == ValidationState.Evaluando, _colEval);
        HighlightDot(dotPass, lblPass, state == ValidationState.Aprobado,  _colPass);
        HighlightDot(dotFail, lblFail, state == ValidationState.Fallo,     _colFail);

        // Luz
        if (_lightAnim != null) StopCoroutine(_lightAnim);
        _lightAnim = StartCoroutine(AnimLight(activeCol, state == ValidationState.Evaluando));
    }

    void HighlightDot(Image dot, TMP_Text lbl, bool active, Color col)
    {
        if (dot != null)
            dot.color = active ? col : new Color(col.r, col.g, col.b, 0.25f);
        if (lbl != null)
            lbl.color = active ? _textMain : _muted;
    }

    IEnumerator AnimLight(Color targetColor, bool pulsing)
    {
        if (stationLight == null) yield break;

        if (pulsing)
        {
            float t = 0f;
            while (_state == ValidationState.Evaluando)
            {
                t += Time.deltaTime * 2.5f;
                stationLight.color     = targetColor;
                stationLight.intensity = Mathf.Abs(Mathf.Sin(t)) * lightMaxIntensity;
                yield return null;
            }
        }
        else
        {
            // Lerp suave hasta color objetivo
            float elapsed = 0f;
            Color startCol = stationLight.color;
            float startInt = stationLight.intensity;
            while (elapsed < 0.4f)
            {
                elapsed += Time.deltaTime;
                float s = elapsed / 0.4f;
                stationLight.color     = Color.Lerp(startCol, targetColor, s);
                stationLight.intensity = Mathf.Lerp(startInt, lightMaxIntensity, s);
                yield return null;
            }
            stationLight.color     = targetColor;
            stationLight.intensity = lightMaxIntensity;
        }
    }

    // ─────────────────────────────────────────────
    //  Auto-construcción del panel leyenda
    // ─────────────────────────────────────────────

    void EnsureLegendPanel()
    {
        if (legendCanvas != null) return;

        var go = new GameObject("LegendPanel");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = new Vector3(0f, 0.07f, -0.01f);
        go.transform.localRotation = Quaternion.Euler(-20f, 0f, 0f);
        go.transform.localScale    = Vector3.one * 0.001f;

        legendCanvas = go.AddComponent<Canvas>();
        legendCanvas.renderMode = RenderMode.WorldSpace;

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(80f, 120f);

        // Fondo del panel
        var bgGO  = new GameObject("Bg");
        bgGO.transform.SetParent(go.transform, false);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = _bgPanel;
        var bgRT  = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

        // Borde del panel
        AddPanelBorder(go);

        // Header
        AddTMPLabel(go, "ESTADO",  new Vector2(0f, 52f), new Vector2(76f, 14f), 8f, _cyan, FontStyles.Bold);

        // Las 4 filas de leyenda
        float[] yPositions = { 36f, 18f, 0f, -18f };
        Color[] colors     = { _colIdle, _colEval, _colPass, _colFail };
        string[] labels    = { "Listo", "Evaluando", "Aprobado", "Fallo" };

        Image[]    dots  = new Image[4];
        TMP_Text[] lbls  = new TMP_Text[4];

        for (int i = 0; i < 4; i++)
        {
            // Círculo de color
            var dotGO = new GameObject($"Dot_{i}");
            dotGO.transform.SetParent(go.transform, false);
            dots[i]       = dotGO.AddComponent<Image>();
            dots[i].color = new Color(colors[i].r, colors[i].g, colors[i].b, 0.25f);
            var dotRT     = dotGO.GetComponent<RectTransform>();
            dotRT.anchorMin = dotRT.anchorMax = dotRT.pivot = new Vector2(0f, 0.5f);
            dotRT.anchoredPosition = new Vector2(6f, yPositions[i]);
            dotRT.sizeDelta = new Vector2(9f, 9f);

            // Texto
            var lGO = new GameObject($"Lbl_{i}");
            lGO.transform.SetParent(go.transform, false);
            lbls[i]            = lGO.AddComponent<TextMeshProUGUI>();
            lbls[i].text       = labels[i];
            lbls[i].fontSize   = 9f;
            lbls[i].color      = _muted;
            lbls[i].fontStyle  = FontStyles.Bold;
            lbls[i].alignment  = TextAlignmentOptions.MidlineLeft;
            var lRT = lGO.GetComponent<RectTransform>();
            lRT.anchorMin = lRT.anchorMax = lRT.pivot = new Vector2(0f, 0.5f);
            lRT.anchoredPosition = new Vector2(18f, yPositions[i]);
            lRT.sizeDelta = new Vector2(58f, 14f);
        }

        dotIdle = dots[0]; dotEval = dots[1]; dotPass = dots[2]; dotFail = dots[3];
        lblIdle = lbls[0]; lblEval = lbls[1]; lblPass = lbls[2]; lblFail = lbls[3];

        // Texto de estado grande (separado, encima del panel)
        if (txtEstado == null)
        {
            var stGO = new GameObject("Txt_Estado");
            stGO.transform.SetParent(transform, false);
            stGO.transform.localPosition = new Vector3(0f, 0.14f, -0.005f);
            stGO.transform.localRotation = Quaternion.Euler(-20f, 0f, 0f);
            stGO.transform.localScale    = Vector3.one * 0.001f;

            var stCanvas = stGO.AddComponent<Canvas>();
            stCanvas.renderMode = RenderMode.WorldSpace;
            stGO.GetComponent<RectTransform>().sizeDelta = new Vector2(80f, 30f);

            txtEstado            = stGO.AddComponent<TextMeshProUGUI>();
            txtEstado.text       = "LISTO PARA\nVALIDAR";
            txtEstado.fontSize   = 10f;
            txtEstado.color      = _colIdle;
            txtEstado.fontStyle  = FontStyles.Bold;
            txtEstado.alignment  = TextAlignmentOptions.Center;
        }
    }

    // ─────────────────────────────────────────────
    //  Helpers de construcción
    // ─────────────────────────────────────────────

    static TMP_Text AddTMPLabel(GameObject parent, string text,
        Vector2 pos, Vector2 size, float fontSize, Color color, FontStyles style = FontStyles.Normal)
    {
        var go = new GameObject("Lbl_" + text);
        go.transform.SetParent(parent.transform, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text      = text;
        t.fontSize  = fontSize;
        t.color     = color;
        t.fontStyle = style;
        t.alignment = TextAlignmentOptions.Center;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;
        return t;
    }

    static void AddPanelBorder(GameObject panel)
    {
        var go = new GameObject("Border");
        go.transform.SetParent(panel.transform, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0f, 0.9f, 1f, 0.3f);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(-1f, -1f);
        rt.offsetMax = new Vector2( 1f,  1f);
        img.transform.SetSiblingIndex(0);
    }
}
