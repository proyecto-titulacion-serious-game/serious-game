using System.Text;
using UnityEngine;
using TMPro;

/// <summary>
/// Controlador maestro de la estación de trabajo del Técnico.
/// Coordina: mesa física, manual, componentes, bandeja, mini HUD y panel de diagnóstico.
///
/// JERARQUÍA DE LA ESCENA:
///
/// [EM] TechnicianWorkstation   ← este script
///   ├─ [3D Cube]  Desk_Surface
///   ├─ [3D Cube]  Manual_Book
///   │   └─ [Canvas WS] Manual_Canvas
///   ├─ [EM] ComponentsArea
///   │   ├─ [3D Cyl] Comp_R100  ← DeskComponent + XRSimpleInteractable (VR opcional)
///   │   ├─ [3D Cyl] Comp_R220
///   │   ├─ [3D Cyl] Comp_R330
///   │   ├─ [3D Sph] Comp_LED
///   │   └─ [3D Cyl] Comp_Cap
///   ├─ [3D Cube]  SendingTray
///   │   └─ [Canvas WS] Tray_Canvas
///   ├─ [Canvas SS] MiniHUD              ← hudVoltaje / hudCorriente / hudReto
///   └─ [Canvas WS] DiagnosticPanel      ← txtDiagnostico / txtAccionSiguiente
///       (World Space frente al Técnico, visible en pantalla o en visor VR)
/// </summary>
public class TechnicianWorkstation : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector — Referencias
    // ─────────────────────────────────────────────

    [Header("Sistemas")]
    public GameManager       gameManager;
    public CircuitManager    circuit;
    public TechnicianManual  manual;
    public TechnicianActions technicianActions;

    [Header("Objetos de la mesa")]
    public Transform deskSurface;
    public Transform manualBook;
    public Transform sendingTray;
    public Transform componentsArea;

    [Header("Mini HUD — esquina de pantalla (Screen Space Overlay)")]
    public TMP_Text hudVoltaje;
    public TMP_Text hudCorriente;
    public TMP_Text hudReto;

    [Header("Panel de diagnóstico — World Space frente al Técnico")]
    [Tooltip("Muestra fallas activas y estado de cada componente.")]
    public TMP_Text txtDiagnostico;
    [Tooltip("Siguiente acción priorizada que el Técnico debe comunicar al Explorador.")]
    public TMP_Text txtAccionSiguiente;

    // ─────────────────────────────────────────────
    //  Internos
    // ─────────────────────────────────────────────
    private float            _hudTimer;
    private const float      HUD_INTERVAL = 0.2f;
    private DiagnosticSystem _diagnostic  = new DiagnosticSystem();

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────

    void Start()
    {
        if (gameManager       == null) gameManager       = FindFirstObjectByType<GameManager>();
        if (circuit           == null) circuit           = FindFirstObjectByType<CircuitManager>();
        if (manual            == null) manual            = FindFirstObjectByType<TechnicianManual>();
        if (technicianActions == null) technicianActions = FindFirstObjectByType<TechnicianActions>();

        CircuitManager.OnCircuitChanged += RefreshDiagnosticPanel;
        GameManager.OnLevelLoaded       += OnLevelLoaded;
    }

    void OnDestroy()
    {
        CircuitManager.OnCircuitChanged -= RefreshDiagnosticPanel;
        GameManager.OnLevelLoaded       -= OnLevelLoaded;
    }

    void Update()
    {
        _hudTimer += Time.deltaTime;
        if (_hudTimer < HUD_INTERVAL) return;
        _hudTimer = 0f;
        RefreshMiniHUD();
    }

    // ─────────────────────────────────────────────
    //  Panel de diagnóstico (event-driven)
    // ─────────────────────────────────────────────

    void RefreshDiagnosticPanel()
    {
        if (circuit == null) return;

        Set(txtDiagnostico,     _diagnostic.GetDiagnosis(circuit.components, circuit.totalCurrent));
        Set(txtAccionSiguiente, _diagnostic.GetNextAction(circuit.components, circuit.totalCurrent));
    }

    // ─────────────────────────────────────────────
    //  Mini HUD (polling cada 0.2 s)
    // ─────────────────────────────────────────────

    void RefreshMiniHUD()
    {
        if (circuit == null || gameManager == null) return;

        float  vSource = 0f;
        float  mA      = circuit.totalCurrent * 1000f;
        string estado  = circuit.isShortCircuited ? "CORTOCIRCUITO"
                       : mA > 20f                 ? "SOBRECARGA"
                       : mA < 5f                  ? "MUY BAJA"
                       :                            "OK";

        // Acumular estado de todos los componentes (Reto 2: 2 LEDs, Reto 3: R+LED+Cap)
        var sb = new StringBuilder();
        foreach (var c in circuit.components)
        {
            if (c is VoltageSource vs)
            {
                vSource = vs.voltage;
            }
            else if (c is Resistor r)
            {
                sb.AppendLine(r.hasFault
                    ? $"R {r.resistance:F0}Ω  FALLA"
                    : $"R {r.resistance:F0}Ω  OK");
            }
            else if (c is LED led)
            {
                sb.AppendLine(led.isOn
                    ? $"LED  ON ({led.current*1000f:F0}mA)"
                    : $"LED  OFF ({led.current*1000f:F0}mA)");
            }
            else if (c is Capacitor cap)
            {
                sb.AppendLine(cap.polarityInverted ? "CAP  INVERTIDO" : "CAP  OK");
            }
            else if (c is ArduinoPin pin)
            {
                sb.AppendLine(pin.hasFault
                    ? $"D{pin.pinNumber}  FALLA (correcto: D{pin.correctPinNumber})"
                    : $"D{pin.pinNumber}  OK");
            }
        }

        Set(hudVoltaje,   $"V: {vSource:F1}V  |  I: {mA:F1}mA  {estado}");
        Set(hudCorriente, sb.Length > 0 ? sb.ToString().TrimEnd() : "—");
        Set(hudReto,      $"RETO {(int)gameManager.currentLevel + 1}  —  " +
                          $"Tiempo: {gameManager.remainingTime:F0}s");
    }

    // ─────────────────────────────────────────────
    //  Eventos
    // ─────────────────────────────────────────────

    void OnLevelLoaded(LevelType level)
    {
        // GameManager cambia la referencia circuit al activar cada zona
        if (gameManager != null)
            circuit = gameManager.circuit;

        RefreshDiagnosticPanel();
        Debug.Log($"[TechnicianWorkstation] Reto {(int)level + 1} cargado.");
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────
    void Set(TMP_Text t, string s) { if (t != null) t.text = s; }
}