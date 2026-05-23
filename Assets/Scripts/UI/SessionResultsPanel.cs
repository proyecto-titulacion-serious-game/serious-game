using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Panel de resultados al finalizar toda la sesión de juego.
/// Se activa al recibir ObjectiveSystem.OnSessionEnded.
/// Puede usarse en el HUD del Explorador (WorldSpace canvas) y en el del Técnico (ScreenSpace canvas).
///
/// JERARQUÍA SUGERIDA:
///   [Canvas]
///     └─ Panel_Resultados   ← asignar en panelResultados (inactivo al inicio)
///         ├─ TMP_Evaluacion     — "[EXCELENTE]" / "[BUENO]" / "[MEJORAR]"
///         ├─ TMP_Score          — "850 / 1000 pts (85%)"
///         ├─ TMP_Tiempo         — "Tiempo total: 12:34"
///         ├─ TMP_Errores        — "Errores totales: 3"
///         ├─ TMP_Reto1..4       — asignar en txtRetoRows (opcional)
///         └─ BTN_Cerrar         — llama ClosePanel()
/// </summary>
public class SessionResultsPanel : MonoBehaviour
{
    [Header("Panel raíz")]
    public GameObject panelResultados;

    [Header("Textos de resumen")]
    public TMP_Text txtEvaluacion;
    public TMP_Text txtScore;
    public TMP_Text txtTiempo;
    public TMP_Text txtErrores;

    [Header("Desglose por reto (4 textos, uno por reto — opcional)")]
    public TMP_Text[] txtRetoRows = new TMP_Text[4];

    [Header("Colores de evaluación")]
    public Color colorExcelente = new Color(0.20f, 0.90f, 0.30f);
    public Color colorBueno     = new Color(1.00f, 0.80f, 0.10f);
    public Color colorMejorar   = new Color(1.00f, 0.30f, 0.20f);

    static readonly string[] _nombres = { "Ley de Ohm", "Paralelo", "Mixto", "Arduino" };

    void OnEnable()  => ObjectiveSystem.OnSessionEnded += ShowResults;
    void OnDisable() => ObjectiveSystem.OnSessionEnded -= ShowResults;

    void Start()
    {
        if (panelResultados != null) panelResultados.SetActive(false);
    }

    public void ClosePanel()
    {
        if (panelResultados != null) panelResultados.SetActive(false);
    }

    // ─────────────────────────────────────────────
    void ShowResults(SessionResult result)
    {
        if (panelResultados != null) panelResultados.SetActive(true);

        if (txtEvaluacion != null)
        {
            txtEvaluacion.text  = result.evaluation;
            txtEvaluacion.color = EvaluationColor(result.evaluation);
        }

        if (txtScore != null)
            txtScore.text = $"{result.totalScore} / {result.maxScore} pts  ({result.scorePercent:P0})";

        if (txtTiempo != null)
        {
            int min = Mathf.FloorToInt(result.totalTimeSeconds / 60f);
            int sec = Mathf.FloorToInt(result.totalTimeSeconds % 60f);
            txtTiempo.text = $"Tiempo total: {min}:{sec:00}";
        }

        if (txtErrores != null)
            txtErrores.text = $"Errores totales: {result.totalErrors}";

        FillRetoRows();
    }

    void FillRetoRows()
    {
        if (txtRetoRows == null || txtRetoRows.Length == 0) return;

        var tracker = FindAnyObjectByType<PerformanceTracker>(FindObjectsInactive.Include);
        List<LevelRecord> records = tracker != null ? tracker.GetAllRecords() : new List<LevelRecord>();

        for (int i = 0; i < txtRetoRows.Length; i++)
        {
            if (txtRetoRows[i] == null) continue;

            if (i < records.Count)
            {
                var r   = records[i];
                int min = Mathf.FloorToInt(r.timeSeconds / 60f);
                int sec = Mathf.FloorToInt(r.timeSeconds % 60f);
                string check = r.success ? "OK" : "X ";
                txtRetoRows[i].text = $"Reto {i + 1} ({_nombres[i]}):  {check}  {min}:{sec:00}  {r.errors} err";
            }
            else
            {
                string nombre = i < _nombres.Length ? _nombres[i] : $"Reto {i + 1}";
                txtRetoRows[i].text = $"Reto {i + 1} ({nombre}):  —";
            }
        }
    }

    Color EvaluationColor(string evaluation)
    {
        if (evaluation.Contains("EXCELENTE")) return colorExcelente;
        if (evaluation.Contains("BUENO"))     return colorBueno;
        return colorMejorar;
    }
}
