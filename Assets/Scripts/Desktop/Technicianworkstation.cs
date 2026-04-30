using UnityEngine;
using TMPro;

/// <summary>
/// Controlador maestro de la estación de trabajo del Técnico.
/// Coordina: mesa física, manual, componentes, bandeja y mini HUD.
/// 
/// JERARQUÍA DE LA ESCENA (crear en este orden):
/// 
/// [EM] TechnicianWorkstation   ← este script va aquí
///   ├─ [3D Cube]  Desk_Surface        (mesa)
///   ├─ [3D Cube]  Manual_Book         (libro del manual)
///   │   └─ [Canvas WS] Manual_Canvas  (texto del manual)
///   ├─ [EM] ComponentsArea            (zona de componentes)
///   │   ├─ [3D Cyl] Comp_R100        resistor 100Ω
///   │   ├─ [3D Cyl] Comp_R220        resistor 220Ω
///   │   ├─ [3D Cyl] Comp_R330        resistor 330Ω
///   │   ├─ [3D Sph] Comp_LED         LED
///   │   └─ [3D Cyl] Comp_Cap         capacitor
///   ├─ [3D Cube]  SendingTray         (bandeja de envío)
///   │   └─ [Canvas WS] Tray_Canvas    (UI de la bandeja)
///   └─ [Canvas SS] MiniHUD            (datos en tiempo real, esquina)
/// </summary>
public class TechnicianWorkstation : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector — Referencias
    // ─────────────────────────────────────────────

    [Header("Sistemas")]
    public GameManager          gameManager;
    public CircuitManager       circuit;
    public TechnicianManual     manual;
    public TechnicianActions    technicianActions;

    [Header("Objetos de la mesa")]
    public Transform deskSurface;
    public Transform manualBook;
    public Transform sendingTray;
    public Transform componentsArea;

    [Header("Mini HUD — datos en tiempo real")]
    public TMP_Text hudVoltaje;
    public TMP_Text hudCorriente;
    public TMP_Text hudEstado;
    public TMP_Text hudReto;

    // ─────────────────────────────────────────────
    //  Internos
    // ─────────────────────────────────────────────
    private float _timer;
    private const float HUD_INTERVAL = 0.2f;

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────

    void Start()
    {
        // Buscar referencias automáticamente si no están asignadas
        if (gameManager       == null) gameManager       = FindFirstObjectByType<GameManager>();
        if (circuit           == null) circuit           = FindFirstObjectByType<CircuitManager>();
        if (manual            == null) manual            = FindFirstObjectByType<TechnicianManual>();
        if (technicianActions == null) technicianActions = FindFirstObjectByType<TechnicianActions>();

        GameManager.OnLevelLoaded += OnLevelLoaded;
    }

    void OnDestroy()
    {
        GameManager.OnLevelLoaded -= OnLevelLoaded;
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer < HUD_INTERVAL) return;
        _timer = 0f;
        RefreshMiniHUD();
    }

    // ─────────────────────────────────────────────
    //  Mini HUD — datos en tiempo real
    // ─────────────────────────────────────────────

    /// <summary>
    /// Actualiza el mini HUD con los datos críticos del circuito.
    /// Se muestra en la esquina de la pantalla (Canvas Screen Space Overlay).
    /// </summary>
    void RefreshMiniHUD()
    {
        if (circuit == null || gameManager == null) return;

        // Voltaje fuente
        float vSource = 0f;
        string estadoLED = "—";
        string estadoR   = "—";

        foreach (var c in circuit.components)
        {
            if (c is VoltageSource vs) vSource = vs.voltage;
            if (c is LED led)
                estadoLED = led.isOn
                    ? $"ENCENDIDO ({led.current*1000f:F0}mA)"
                    : $"APAGADO ({led.current*1000f:F0}mA)";
            if (c is Resistor r)
                estadoR = r.hasFault
                    ? $"{r.resistance:F0} Ohm  FALLA"
                    : $"{r.resistance:F0} Ohm  OK";
        }

        float   mA     = circuit.totalCurrent * 1000f;
        bool    sobre  = mA > 20f;
        string  estado = sobre ? "SOBRECARGA" : mA < 5f ? "MUY BAJA" : "OK";

        Set(hudVoltaje,   $"V: {vSource:F1}V");
        Set(hudCorriente, $"I: {mA:F1}mA  {estado}");
        Set(hudEstado,    $"R: {estadoR}\nLED: {estadoLED}");
        Set(hudReto,      $"RETO {(int)gameManager.currentLevel + 1}");
    }

    // ─────────────────────────────────────────────
    //  Eventos
    // ─────────────────────────────────────────────

    void OnLevelLoaded(LevelType level)
    {
        Debug.Log($"[TechnicianWorkstation] Reto {(int)level + 1} cargado.");
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────
    void Set(TMP_Text t, string s) { if (t != null) t.text = s; }
}