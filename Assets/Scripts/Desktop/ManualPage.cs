using UnityEngine;

/// <summary>
/// Contenido de una pagina del manual del Tecnico.
/// Data-Driven: edita los textos en el Inspector sin recompilar.
///
/// Crear assets: clic derecho → Create → TITA → Manual Page
/// Ubicacion recomendada: Assets/Data/ManualPages/
/// </summary>
[CreateAssetMenu(fileName = "ManualPage_Reto", menuName = "TITA/Manual Page")]
public class ManualPage : ScriptableObject
{
    [Header("Identificacion")]
    public LevelType levelType;

    [Header("Pagina 1 — Concepto")]
    [TextArea(3, 8)] public string titulo;
    [TextArea(4, 10)] public string concepto;
    [TextArea(4, 10)] public string formula;

    [Header("Pagina 2 — Objetivo")]
    [TextArea(4, 10)] public string objetivo;
    [TextArea(4, 10)] public string tablaValores;

    [Header("Pagina 3 — Referencia rapida")]
    [TextArea(4, 10)] public string componentesClave;
    [TextArea(4, 10)] public string codigoColores;
}
