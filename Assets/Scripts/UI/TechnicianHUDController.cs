using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Actualiza el HUD de pantalla del Técnico suscribiéndose a eventos de GameManager.
/// Se añade al root del prefab TechnicianHUD.
/// Referencias externas a asignar en el Inspector: gameManager.
/// </summary>
public class TechnicianHUDController : MonoBehaviour
{
    [Header("Referencias")]
    public GameManager gameManager;

    [Header("Info — panel superior")]
    public TMP_Text txtReto;
    public TMP_Text txtTimer;
    public TMP_Text txtErrores;

    [Header("Panel de transición de zona")]
    public GameObject panelTransicion;
    public TMP_Text   txtTransicionTitulo;
    public TMP_Text   txtTransicionSub;

    [Header("Overlay de validación")]
    public GameObject panelValidacion;
    public Image      imgValidacionBg;
    public TMP_Text   txtValidacionEstado;
    public Image      progressFill;
    public GameObject panelChecklist;
    public TMP_Text   txtCheck1;
    public TMP_Text   txtCheck2;
    public TMP_Text   txtCheck3;
    public Button     btnCerrarValidacion;

    // ─────────────────────────────────────────────
    void OnEnable()
    {
        GameManager.OnLevelLoaded         += OnLevelLoaded;
        GameManager.OnTimerTick           += OnTimerTick;
        GameManager.OnTimerExpired        += OnTimerExpired;
        GameManager.OnZoneTransitionStart += OnZoneTransitionStart;
        GameManager.OnZoneActivated       += OnZoneActivated;
    }

    void OnDisable()
    {
        GameManager.OnLevelLoaded         -= OnLevelLoaded;
        GameManager.OnTimerTick           -= OnTimerTick;
        GameManager.OnTimerExpired        -= OnTimerExpired;
        GameManager.OnZoneTransitionStart -= OnZoneTransitionStart;
        GameManager.OnZoneActivated       -= OnZoneActivated;
    }

    void Start()
    {
        if (panelTransicion != null) panelTransicion.SetActive(false);
    }

    void Update()
    {
        if (gameManager == null || txtErrores == null) return;
        txtErrores.text = $"Errores: {gameManager.GetWrongAttempts()}";
    }

    // ─────────────────────────────────────────────
    void OnLevelLoaded(LevelType level)
    {
        if (txtReto != null)
            txtReto.text = level switch
            {
                LevelType.OhmLaw   => "RETO 1 — Ley de Ohm",
                LevelType.Parallel => "RETO 2 — Paralelo",
                LevelType.Mixed    => "RETO 3 — Mixto",
                LevelType.Arduino  => "RETO 4 — Arduino",
                _                  => "RETO —"
            };

        if (panelTransicion != null) panelTransicion.SetActive(false);
    }

    void OnTimerTick(float remaining)
    {
        if (txtTimer == null) return;
        int min = Mathf.FloorToInt(remaining / 60f);
        int sec = Mathf.FloorToInt(remaining % 60f);
        txtTimer.text  = $"{min}:{sec:00}";
        txtTimer.color = remaining < 60f ? new Color(1f, 0.3f, 0.3f) : Color.white;
    }

    void OnTimerExpired(LevelType _)
    {
        if (txtTimer != null) { txtTimer.text = "0:00"; txtTimer.color = Color.red; }
    }

    void OnZoneTransitionStart(LevelType level, bool success)
    {
        if (panelTransicion == null) return;
        panelTransicion.SetActive(true);

        int num = (int)level + 1;
        if (txtTransicionTitulo != null)
            txtTransicionTitulo.text = success
                ? $"RETO {num} COMPLETADO"
                : $"RETO {num} — Tiempo agotado";

        if (txtTransicionSub != null)
            txtTransicionSub.text = "Cargando siguiente zona...";
    }

    void OnZoneActivated(int index)
    {
        // El panel de transición se oculta cuando el nuevo nivel ya cargó (OnLevelLoaded)
    }
}
