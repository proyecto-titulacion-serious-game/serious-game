using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel WorldSpace que dibuja el diagrama del circuito del reto activo.
/// Se auto-detecta del CircuitManager padre al hacer Start().
/// Actualiza colores de pistas y componentes vía CircuitManager.OnCircuitChanged.
///
/// SETUP (generado automáticamente por GameSceneGenerator):
///   Canvas WorldSpace, hijo del root de la zona de reto.
///   Añadir este script al mismo GameObject del Canvas.
///
/// TOPOLOGÍAS soportadas:
///   Series   → Reto 1: bucle rectangular, componentes en rail superior
///   Parallel → Reto 2: raíles top/bot con ramas verticales por componente
///   Mixed    → Reto 3: sección serie + bifurcación paralela (LED ‖ Capacitor)
/// </summary>
[RequireComponent(typeof(Canvas))]
public class CircuitDiagramPanel : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────

    [Header("Colores PCB")]
    public Color colFondo      = new Color(0.05f, 0.17f, 0.07f);  // verde PCB
    public Color colPista      = new Color(0.80f, 0.88f, 0.78f);  // pista plata
    public Color colActivo     = new Color(0.20f, 1.00f, 0.35f);  // corriente activa
    public Color colFalla      = new Color(1.00f, 0.18f, 0.18f);  // falla / cortocircuito
    public Color colNodo       = new Color(1.00f, 0.88f, 0.15f);  // punto de unión (junction)
    public float grosor        = 4f;

    [Header("CircuitManager (auto-detectado del padre si queda vacío)")]
    public CircuitManager circuit;

    // ─────────────────────────────────────────────
    //  Internos
    // ─────────────────────────────────────────────

    RectTransform              _root;           // área interna del diagrama
    readonly List<Image>       _pistas   = new();
    readonly List<(Image box, ElectricalComponent comp)> _indicadores = new();

    // ─────────────────────────────────────────────
    //  Lifecycle
    // ─────────────────────────────────────────────

    void Start()
    {
        if (circuit == null && transform.parent != null)
            circuit = transform.parent.GetComponentInChildren<CircuitManager>();

        Build();
        CircuitManager.OnCircuitChanged += RefreshStatus;
    }

    void OnDestroy() => CircuitManager.OnCircuitChanged -= RefreshStatus;

    // ─────────────────────────────────────────────
    //  Build
    // ─────────────────────────────────────────────

    void Build()
    {
        // Destruir hijos anteriores
        var hijos = new List<GameObject>();
        for (int i = 0; i < transform.childCount; i++)
            hijos.Add(transform.GetChild(i).gameObject);
        foreach (var h in hijos) Destroy(h);

        _pistas.Clear();
        _indicadores.Clear();

        // ── Fondo PCB ────────────────────────────────────────────────────────
        var fondo   = Nodo("PCB_BG", transform);
        Stretch(fondo);
        fondo.AddComponent<Image>().color = colFondo;

        // ── Puntos de grid decorativos ────────────────────────────────────────
        var grid    = Nodo("Grid", transform);
        Stretch(grid);
        var gridTxt             = grid.AddComponent<TextMeshProUGUI>();
        var sb                  = new StringBuilder();
        for (int r = 0; r < 16; r++) { for (int c = 0; c < 42; c++) sb.Append("·  "); sb.AppendLine(); }
        gridTxt.text            = sb.ToString();
        gridTxt.fontSize        = 4f;
        gridTxt.color           = new Color(0.14f, 0.32f, 0.14f, 0.5f);
        gridTxt.enableWordWrapping = true;
        gridTxt.alignment       = TextAlignmentOptions.TopLeft;

        // ── Root del diagrama con padding ─────────────────────────────────────
        var rootGO  = Nodo("Root", transform);
        _root       = rootGO.AddComponent<RectTransform>();
        _root.anchorMin = Vector2.zero; _root.anchorMax = Vector2.one;
        _root.offsetMin = new Vector2(22, 28); _root.offsetMax = new Vector2(-22, -30);

        // ── Barra de título ───────────────────────────────────────────────────
        BuildTitleBar();

        if (circuit == null) return;

        // ── Diagrama según topología ──────────────────────────────────────────
        switch (circuit.topology)
        {
            case CircuitTopology.Series:   LayoutSerie();    break;
            case CircuitTopology.Parallel: LayoutParalelo(); break;
            default:                       LayoutMixto();    break;
        }

        RefreshStatus();
    }

    // ─────────────────────────────────────────────
    //  TOPOLOGÍA — Serie (Reto 1)
    // ─────────────────────────────────────────────

    void LayoutSerie()
    {
        //  Bucle rectangular, sentido horario:
        //
        //   ┌─────────[R]──────────[LED]─────────┐
        //   │                                     │
        //  [V]                                   GND
        //   │                                     │
        //   └─────────────────────────────────────┘
        //
        //  Colores de pista cambian según estado del circuito.

        float W = 155f, H = 82f;

        // Raíles y bordes
        Pista((-W, H),  (W, H));   // rail superior
        Pista((W, H),   (W, -H));  // borde derecho
        Pista((W, -H),  (-W, -H)); // rail inferior
        Pista((-W, -H), (-W, H));  // borde izquierdo (encima irá la fuente)

        // Recopilar componentes
        var partes = ComponentesFiltrados();

        // Fuente en borde izquierdo
        if (partes.vs != null)
            CajaFuente(partes.vs, new Vector2(-W, 0));

        // Componentes a lo largo del rail superior
        var comps = partes.otros;
        float paso = comps.Count > 0 ? (2 * W) / (comps.Count + 1) : 0;
        for (int i = 0; i < comps.Count; i++)
        {
            float x = -W + paso * (i + 1);
            CajaComp(comps[i], new Vector2(x, H), new Vector2(72f, 30f));
        }

        // Símbolo GND
        LabelGND(new Vector2(W + 12f, -H + 6f));
    }

    // ─────────────────────────────────────────────
    //  TOPOLOGÍA — Paralelo (Reto 2)
    // ─────────────────────────────────────────────

    void LayoutParalelo()
    {
        //  Rail superior e inferior, ramas verticales:
        //
        //  ┌──────────────────────────────────────────┐
        //  │      |           |          |             │
        // [V]   [R 50Ω]   [LED1 ⚠]   [LED2]          │
        //  │      |           |          |             │
        //  └──────────────────────────────────────────┘

        float W = 155f, H = 78f;

        // Rails
        Pista((-W, H),  (W, H));
        Pista((-W, -H), (W, -H));
        // Bordes
        Pista((-W, H), (-W, -H));
        Pista((W, H),  (W, -H));

        var partes = ComponentesFiltrados();

        if (partes.vs != null)
            CajaFuente(partes.vs, new Vector2(-W, 0));

        var comps = partes.otros;
        float paso = comps.Count > 0 ? (2 * W) / (comps.Count + 1) : 0;

        for (int i = 0; i < comps.Count; i++)
        {
            float x = -W + paso * (i + 1);
            Pista((x, H), (x, -H));          // rama vertical
            PuntoNodo(new Vector2(x, H));
            PuntoNodo(new Vector2(x, -H));
            CajaComp(comps[i], new Vector2(x, 0), new Vector2(70f, 30f));
        }

        LabelRail("V+",  new Vector2(-W + 18f, H - 5f));
        LabelRail("GND", new Vector2(-W + 18f, -H + 5f));
    }

    // ─────────────────────────────────────────────
    //  TOPOLOGÍA — Mixto (Reto 3)
    // ─────────────────────────────────────────────

    void LayoutMixto()
    {
        //  Serie hasta el nodo X, luego bifurcación LED ‖ Capacitor:
        //
        //  ┌──[R]──────┬────[LED]────────────┐
        //  │           │                      │
        // [V]          └────[CAP]─────────────┘
        //  │                                  │
        //  └──────────────────────────────────┘

        float W = 150f, H = 90f;
        float splitX = -20f;        // punto de bifurcación en X
        float ledY   = H;           // rama LED (superior)
        float capY   = 10f;         // rama CAP (inferior intermedia)

        // Contorno exterior
        Pista((-W, ledY), (W, ledY));
        Pista((W, ledY),  (W, -H));
        Pista((W, -H),    (-W, -H));
        Pista((-W, -H),   (-W, ledY));

        // Segmento serie (izquierda → nodo bifurcación)
        // ya cubierto por el rail superior, solo añadir rama baja capacitor
        Pista((splitX, ledY), (splitX, capY));      // vertical descenso bifurcación
        Pista((splitX, capY), (W, capY));           // rama CAP horizontal
        Pista((W, capY),      (W, ledY));           // cierre derecho (encima)

        PuntoNodo(new Vector2(splitX, ledY));
        PuntoNodo(new Vector2(W,  capY));
        PuntoNodo(new Vector2(W,  ledY));

        var partes = ComponentesFiltrados();

        if (partes.vs != null)
            CajaFuente(partes.vs, new Vector2(-W, 0));

        // Resistencia en serie (rail superior izquierda)
        foreach (var c in partes.otros)
        {
            if (c is Resistor)
            {
                float midSerie = (-W + splitX) * 0.5f;
                CajaComp(c, new Vector2(midSerie, ledY), new Vector2(70f, 28f));
            }
        }

        // LED en rail superior (después del nodo)
        foreach (var c in partes.otros)
        {
            if (c is LED)
            {
                float midLED = (splitX + W) * 0.5f;
                CajaComp(c, new Vector2(midLED, ledY), new Vector2(70f, 28f));
            }
        }

        // Capacitor en rama intermedia
        foreach (var c in partes.otros)
        {
            if (c is Capacitor)
            {
                float midCap = (splitX + W) * 0.5f;
                CajaComp(c, new Vector2(midCap, capY), new Vector2(70f, 28f));
            }
        }

        LabelGND(new Vector2(W + 12f, -H + 6f));
    }

    // ─────────────────────────────────────────────
    //  Actualización de estado
    // ─────────────────────────────────────────────

    void RefreshStatus()
    {
        if (circuit == null) return;

        bool hasCorriente  = circuit.totalCurrent > 0.001f;
        bool cortocircuito = circuit.isShortCircuited;

        Color colPistaActual = cortocircuito ? colFalla
                             : hasCorriente  ? colActivo
                             : colPista;

        foreach (var p in _pistas)
            if (p != null) p.color = colPistaActual;

        foreach (var (box, comp) in _indicadores)
            if (box != null && comp != null)
                box.color = ColorComp(comp);
    }

    // ─────────────────────────────────────────────
    //  Drawing helpers
    // ─────────────────────────────────────────────

    void Pista((float x, float y) desde, (float x, float y) hasta)
        => Pista(new Vector2(desde.x, desde.y), new Vector2(hasta.x, hasta.y));

    void Pista(Vector2 desde, Vector2 hasta)
    {
        var go  = Nodo("Pista", _root);
        var rt  = go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = colPista;

        Vector2 diff = hasta - desde;
        float len    = diff.magnitude;
        float angle  = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

        rt.anchoredPosition = (desde + hasta) * 0.5f;
        rt.sizeDelta        = new Vector2(len, grosor);
        rt.localRotation    = Quaternion.Euler(0, 0, angle);

        _pistas.Add(img);
    }

    void PuntoNodo(Vector2 pos)
    {
        var go  = Nodo("Junction", _root);
        var rt  = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta        = new Vector2(11f, 11f);
        var img = go.AddComponent<Image>();
        img.color = colNodo;
        _pistas.Add(img);
    }

    void CajaFuente(VoltageSource vs, Vector2 pos)
    {
        var go  = Nodo("Fuente", _root);
        var rt  = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta        = new Vector2(34f, 70f);

        // Borde
        var borde = go.AddComponent<Image>();
        borde.color = new Color(0.55f, 0.6f, 0.5f);

        // Interior
        var inner   = Nodo("Inner", go);
        var innerRT = inner.AddComponent<RectTransform>();
        innerRT.anchorMin = Vector2.zero; innerRT.anchorMax = Vector2.one;
        innerRT.offsetMin = new Vector2(2, 2); innerRT.offsetMax = new Vector2(-2, -2);
        var innerImg = inner.AddComponent<Image>();
        innerImg.color = new Color(0.7f, 0.58f, 0.05f);

        // Texto
        TxtEn(go, $"{vs.voltage}V", new Vector2(0, 10f), 8f, Color.white, FontStyles.Bold);
        TxtEn(go, "FUENTE",          new Vector2(0, -10f), 6.5f, new Color(0.9f, 0.85f, 0.5f));
        TxtEn(go, "+",               new Vector2(0, 30f), 10f, new Color(1f, 0.45f, 0.45f), FontStyles.Bold);
        TxtEn(go, "−",               new Vector2(0, -30f), 10f, new Color(0.45f, 0.7f, 1f), FontStyles.Bold);

        _indicadores.Add((innerImg, vs));
    }

    void CajaComp(ElectricalComponent comp, Vector2 pos, Vector2 size)
    {
        string lbl = LabelComp(comp);
        string val = ValorComp(comp);

        var go  = Nodo($"Comp_{lbl}", _root);
        var rt  = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta        = size;

        // Borde exterior gris
        go.AddComponent<Image>().color = new Color(0.5f, 0.58f, 0.5f, 0.95f);

        // Caja interior de color (indicador de estado)
        var inner   = Nodo("Inner", go);
        var innerRT = inner.AddComponent<RectTransform>();
        innerRT.anchorMin = Vector2.zero; innerRT.anchorMax = Vector2.one;
        innerRT.offsetMin = new Vector2(2, 2); innerRT.offsetMax = new Vector2(-2, -2);
        var innerImg = inner.AddComponent<Image>();
        innerImg.color = ColorComp(comp);

        // Texto label (arriba)
        var lblGO = Nodo("Lbl", go);
        var lblRT = lblGO.AddComponent<RectTransform>();
        lblRT.anchorMin = new Vector2(0, 0.5f); lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = new Vector2(2, 0);    lblRT.offsetMax = new Vector2(-2, -1);
        var lblTxt = lblGO.AddComponent<TextMeshProUGUI>();
        lblTxt.text      = lbl;
        lblTxt.fontSize  = 7.5f;
        lblTxt.color     = Color.white;
        lblTxt.alignment = TextAlignmentOptions.Center;
        lblTxt.fontStyle = FontStyles.Bold;

        // Texto valor (abajo)
        var valGO = Nodo("Val", go);
        var valRT = valGO.AddComponent<RectTransform>();
        valRT.anchorMin = Vector2.zero;         valRT.anchorMax = new Vector2(1, 0.5f);
        valRT.offsetMin = new Vector2(2, 1);    valRT.offsetMax = new Vector2(-2, 0);
        var valTxt = valGO.AddComponent<TextMeshProUGUI>();
        valTxt.text      = val;
        valTxt.fontSize  = 6.5f;
        valTxt.color     = new Color(0.95f, 0.95f, 0.75f);
        valTxt.alignment = TextAlignmentOptions.Center;

        _indicadores.Add((innerImg, comp));
    }

    void BuildTitleBar()
    {
        string titulo = circuit?.topology switch
        {
            CircuitTopology.Series   => "◈  RETO 1 — CIRCUITO SERIE",
            CircuitTopology.Parallel => "◈  RETO 2 — CIRCUITO PARALELO",
            CircuitTopology.Mixed    => "◈  RETO 3 — CIRCUITO MIXTO",
            _                        => "◈  CIRCUITO"
        };

        var bar = Nodo("TitleBar", transform);
        var rt  = bar.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1f); rt.anchorMax = new Vector2(1, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = new Vector2(0, 26f);
        bar.AddComponent<Image>().color = new Color(0.03f, 0.10f, 0.04f, 0.96f);

        var txtGO = Nodo("TitleTxt", bar);
        var txtRT = txtGO.AddComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = txtRT.offsetMax = Vector2.zero;
        var txt         = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text        = titulo;
        txt.fontSize    = 9.5f;
        txt.color       = new Color(0.35f, 0.95f, 0.42f);
        txt.alignment   = TextAlignmentOptions.Center;
        txt.fontStyle   = FontStyles.Bold;
    }

    void LabelGND(Vector2 pos)
    {
        var go = Nodo("GND", _root);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta        = new Vector2(36f, 16f);
        var txt             = go.AddComponent<TextMeshProUGUI>();
        txt.text      = "GND ⏚";
        txt.fontSize  = 7.5f;
        txt.color     = new Color(0.45f, 0.85f, 0.48f);
        txt.alignment = TextAlignmentOptions.Left;
    }

    void LabelRail(string texto, Vector2 pos)
    {
        var go = Nodo($"Rail_{texto}", _root);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta        = new Vector2(36f, 14f);
        var txt             = go.AddComponent<TextMeshProUGUI>();
        txt.text      = texto;
        txt.fontSize  = 7f;
        txt.color     = new Color(0.45f, 0.85f, 0.48f);
        txt.alignment = TextAlignmentOptions.Left;
    }

    void TxtEn(GameObject parent, string texto, Vector2 offset,
               float size, Color color, FontStyles style = FontStyles.Normal)
    {
        var go  = Nodo("Txt", parent);
        var rt  = go.AddComponent<RectTransform>();
        rt.anchoredPosition = offset;
        rt.sizeDelta        = new Vector2(30f, 14f);
        var t               = go.AddComponent<TextMeshProUGUI>();
        t.text      = texto;
        t.fontSize  = size;
        t.color     = color;
        t.alignment = TextAlignmentOptions.Center;
        t.fontStyle = style;
    }

    // ─────────────────────────────────────────────
    //  Component info
    // ─────────────────────────────────────────────

    static string LabelComp(ElectricalComponent c) => c switch
    {
        Resistor   => "RESISTOR",
        LED        => "LED",
        Capacitor  => "CAP",
        ArduinoPin => "ARDUINO",
        VoltageSource => "FUENTE",
        _ => c.GetType().Name.ToUpper()
    };

    static string ValorComp(ElectricalComponent c) => c switch
    {
        Resistor r    => r.hasFault       ? $"⚠ {r.resistance:F0} Ω" : $"{r.resistance:F0} Ω",
        LED led       => led.isOn         ? "ON  ✓"
                       : led.polarityInverted ? "⊘ Inv."  : "OFF",
        Capacitor cap => cap.polarityInverted ? "⊘ Inv."  : "OK",
        VoltageSource vs => $"{vs.voltage} V",
        ArduinoPin pin => pin.hasFault    ? $"⚠ D{pin.pinNumber}" : $"D{pin.pinNumber}",
        _ => "—"
    };

    Color ColorComp(ElectricalComponent c) => c switch
    {
        Resistor r    => r.hasFault
                         ? new Color(0.65f, 0.18f, 0.12f)
                         : new Color(0.72f, 0.48f, 0.12f),
        LED led       => led.isOn
                         ? new Color(0.12f, 0.60f, 0.18f)
                         : led.polarityInverted
                           ? new Color(0.60f, 0.18f, 0.12f)
                           : new Color(0.18f, 0.28f, 0.18f),
        Capacitor cap => cap.polarityInverted
                         ? new Color(0.60f, 0.18f, 0.12f)
                         : new Color(0.12f, 0.28f, 0.65f),
        VoltageSource => new Color(0.65f, 0.52f, 0.05f),
        ArduinoPin p  => p.hasFault
                         ? new Color(0.60f, 0.18f, 0.12f)
                         : new Color(0.12f, 0.45f, 0.22f),
        _ => new Color(0.25f, 0.25f, 0.25f)
    };

    // ─────────────────────────────────────────────
    //  Recopilar componentes del CircuitManager
    // ─────────────────────────────────────────────

    (VoltageSource vs, List<ElectricalComponent> otros) ComponentesFiltrados()
    {
        VoltageSource vs = null;
        var otros = new List<ElectricalComponent>();

        if (circuit == null) return (null, otros);

        foreach (var c in circuit.components)
        {
            if (c is VoltageSource v) vs = v;
            else otros.Add(c);
        }
        return (vs, otros);
    }

    // ─────────────────────────────────────────────
    //  Utility
    // ─────────────────────────────────────────────

    static GameObject Nodo(string name, GameObject parent) => Nodo(name, parent.transform);
    static GameObject Nodo(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    static void Stretch(GameObject go)
    {
        var rt        = go.AddComponent<RectTransform>();
        rt.anchorMin  = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin  = rt.offsetMax = Vector2.zero;
    }
}
