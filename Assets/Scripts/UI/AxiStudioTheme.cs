using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Paleta y constructores de UI para la estética "AXISTUDIO" del Reto 4 (boceto Técnico):
/// paneles oscuros con borde/cabecera cian-neón y texto monoespaciado tipo terminal.
///
/// Centraliza el "look" para que SerialMonitorFeed, NetworkDataIntercept y el reskin
/// del IDE compartan exactamente los mismos colores y construcción de panel.
/// Es puro uGUI estático → funciona igual en runtime (auto-build) y en Editor (tool).
/// </summary>
public static class AxiStudioTheme
{
    // ─── Paleta ──────────────────────────────────────────────────────────
    public static readonly Color Cyan      = new(0.00f, 0.92f, 0.94f, 1f);   // neón principal
    public static readonly Color CyanDim   = new(0.00f, 0.55f, 0.62f, 1f);
    public static readonly Color BgPanel   = new(0.012f, 0.055f, 0.075f, 0.94f);
    public static readonly Color BgHeader  = new(0.00f, 0.16f, 0.20f, 0.98f);
    public static readonly Color BgRowAlt  = new(0.02f, 0.10f, 0.13f, 0.55f);

    // Colores por etiqueta de log (estilo boceto)
    public static readonly Color TxtSystem = new(0.55f, 1.00f, 0.78f, 1f);   // [SYSTEM]
    public static readonly Color TxtNet    = new(0.38f, 0.86f, 1.00f, 1f);   // [NET]
    public static readonly Color TxtLcd    = new(1.00f, 0.84f, 0.32f, 1f);   // [LCD]
    public static readonly Color TxtWarn   = new(1.00f, 0.42f, 0.32f, 1f);   // errores

    public static string Hex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);

    // ─── Construcción de panel ───────────────────────────────────────────
    /// <summary>
    /// Crea un panel estilizado (fondo + barra de cabecera con título + cuerpo de texto
    /// monoespaciado) bajo <paramref name="parent"/> ocupando el rect [anchorMin..anchorMax].
    /// Devuelve el TMP del cuerpo donde se escribe el contenido.
    /// </summary>
    public static TMP_Text BuildPanel(Transform parent, string title,
        Vector2 anchorMin, Vector2 anchorMax, int bodyFontSize = 13)
    {
        var panel = NewUI("AxiPanel", parent);
        var prt = (RectTransform)panel.transform;
        prt.anchorMin = anchorMin; prt.anchorMax = anchorMax;
        prt.offsetMin = Vector2.zero; prt.offsetMax = Vector2.zero;

        var bg = panel.AddComponent<Image>();
        bg.color = BgPanel;
        AddNeonBorder(panel.transform);

        // Cabecera
        const float headerH = 26f;
        var header = NewUI("Header", panel.transform);
        var hrt = (RectTransform)header.transform;
        hrt.anchorMin = new Vector2(0, 1); hrt.anchorMax = new Vector2(1, 1);
        hrt.pivot = new Vector2(0.5f, 1f);
        hrt.offsetMin = new Vector2(0, -headerH); hrt.offsetMax = Vector2.zero;
        hrt.sizeDelta = new Vector2(0, headerH);
        header.AddComponent<Image>().color = BgHeader;

        var hTxt = NewText("HeaderTxt", header.transform, title, 15, Cyan, TextAlignmentOptions.Left);
        var htrt = hTxt.rectTransform;
        htrt.anchorMin = Vector2.zero; htrt.anchorMax = Vector2.one;
        htrt.offsetMin = new Vector2(10, 0); htrt.offsetMax = new Vector2(-10, 0);
        hTxt.fontStyle = FontStyles.Bold;

        // Cuerpo
        var body = NewText("Body", panel.transform, "", bodyFontSize, Color.white, TextAlignmentOptions.TopLeft);
        var brt = body.rectTransform;
        brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
        brt.offsetMin = new Vector2(10, 8); brt.offsetMax = new Vector2(-10, -headerH - 4);
        body.textWrappingMode = TextWrappingModes.NoWrap;
        body.richText = true;
        body.overflowMode = TextOverflowModes.Truncate;
        return body;
    }

    /// <summary>Línea/borde neón fino en los 4 lados (simulado con una Image + Outline).</summary>
    public static void AddNeonBorder(Transform panel)
    {
        var outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(Cyan.r, Cyan.g, Cyan.b, 0.5f);
        outline.effectDistance = new Vector2(2f, -2f);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────
    public static GameObject NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    public static TextMeshProUGUI NewText(string name, Transform parent, string text,
        int size, Color color, TextAlignmentOptions align)
    {
        var go = NewUI(name, parent);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.richText = true;
        return t;
    }
}
