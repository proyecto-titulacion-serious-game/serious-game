using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controla la cámara del puesto de trabajo del Técnico (Pc_Camera).
///   • Click derecho + drag → pan horizontal y vertical
///   • Rueda del ratón      → zoom (FOV)
///   • Doble click izq.     → reset suave a posición/FOV por defecto
///
/// Añadir este componente al mismo GO que tiene la Camera del puesto.
/// WorkstationSeat llama RecordDefault() al sentarse para capturar
/// la posición exacta desde la que parte el jugador.
///
/// IsDraggingComponent se pone a true por DeskComponent mientras el
/// jugador arrastra una pieza, bloqueando el pan para evitar conflictos.
/// </summary>
[RequireComponent(typeof(Camera))]
public class TechnicianCameraController : MonoBehaviour
{
    // ── Pan ──────────────────────────────────────────────────────────────
    [Header("Pan (click derecho + drag)")]
    public float panSpeed = 0.004f;
    [Tooltip("Límite de desplazamiento horizontal respecto a la posición por defecto.")]
    public Vector2 panLimitX = new Vector2(-0.6f, 0.6f);
    [Tooltip("Límite de desplazamiento vertical respecto a la posición por defecto.")]
    public Vector2 panLimitY = new Vector2(-0.35f, 0.35f);

    // ── Zoom ─────────────────────────────────────────────────────────────
    [Header("Zoom (rueda del ratón)")]
    public float zoomSpeed = 8f;
    public float minFOV    = 20f;
    public float maxFOV    = 75f;

    // ── Reset ─────────────────────────────────────────────────────────────
    [Header("Reset (doble click izquierdo)")]
    public float resetSpeed          = 8f;
    public float doubleClickInterval = 0.30f;

    // ── Static flag para DeskComponent ───────────────────────────────────
    /// <summary>
    /// DeskComponent lo pone a true mientras arrastra una pieza.
    /// Impide que el pan interfiera con el drag de componentes.
    /// </summary>
    public static bool IsDraggingComponent;

    // ── Privados ──────────────────────────────────────────────────────────
    private Camera  _cam;
    private Vector3 _defaultLocalPos;
    private float   _defaultFOV;

    private Vector3 _targetLocalPos;
    private float   _targetFOV;
    private bool    _resetting;

    private Vector2 _panMouseOrigin;
    private bool    _panning;

    private float _lastLeftClickTime = -1f;

    // ─────────────────────────────────────────────────────────────────────
    void Awake()
    {
        _cam = GetComponent<Camera>();
    }

    void OnEnable()
    {
        RecordDefault();
    }

    // ─────────────────────────────────────────────────────────────────────
    void Update()
    {
        if (!_cam.isActiveAndEnabled) return;

        HandlePan();
        HandleZoom();
        HandleDoubleClickReset();

        if (_resetting)
            ApplySmoothReset();
    }

    // ── Pan ───────────────────────────────────────────────────────────────
    void HandlePan()
    {
        if (IsDraggingComponent) { _panning = false; return; }

        var mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.rightButton.wasPressedThisFrame)
        {
            _panMouseOrigin = mouse.position.ReadValue();
            _panning        = true;
            _resetting      = false;
        }
        if (mouse.rightButton.wasReleasedThisFrame)
            _panning = false;

        if (!_panning || !mouse.rightButton.isPressed) return;

        Vector2 curPos  = mouse.position.ReadValue();
        Vector2 delta2  = curPos - _panMouseOrigin;
        _panMouseOrigin = curPos;
        Vector3 delta   = new Vector3(delta2.x, delta2.y, 0f);

        Vector3 move   = new Vector3(-delta.x * panSpeed, -delta.y * panSpeed, 0f);
        Vector3 newPos = transform.localPosition + move;

        newPos.x = Mathf.Clamp(newPos.x,
            _defaultLocalPos.x + panLimitX.x,
            _defaultLocalPos.x + panLimitX.y);
        newPos.y = Mathf.Clamp(newPos.y,
            _defaultLocalPos.y + panLimitY.x,
            _defaultLocalPos.y + panLimitY.y);
        newPos.z = transform.localPosition.z;

        transform.localPosition = newPos;
        _targetLocalPos         = newPos;
    }

    // ── Zoom ──────────────────────────────────────────────────────────────
    void HandleZoom()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        // scroll.y en el nuevo Input System viene en unidades de pantalla (~120 por notch)
        float scroll = mouse.scroll.ReadValue().y / 1200f;
        if (Mathf.Abs(scroll) < 0.0001f) return;

        _resetting       = false;
        _targetFOV       = Mathf.Clamp(_targetFOV - scroll * zoomSpeed, minFOV, maxFOV);
        _cam.fieldOfView = _targetFOV;
    }

    // ── Double-click reset ────────────────────────────────────────────────
    void HandleDoubleClickReset()
    {
        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

        float now = Time.unscaledTime;
        if (now - _lastLeftClickTime < doubleClickInterval)
        {
            _targetLocalPos = _defaultLocalPos;
            _targetFOV      = _defaultFOV;
            _resetting      = true;
        }
        _lastLeftClickTime = now;
    }

    void ApplySmoothReset()
    {
        float t = resetSpeed * Time.deltaTime;
        transform.localPosition = Vector3.Lerp(transform.localPosition, _targetLocalPos, t);
        _cam.fieldOfView        = Mathf.Lerp(_cam.fieldOfView, _targetFOV, t);

        bool posOk = Vector3.Distance(transform.localPosition, _targetLocalPos) < 0.0005f;
        bool fovOk = Mathf.Abs(_cam.fieldOfView - _targetFOV) < 0.05f;
        if (posOk && fovOk)
        {
            transform.localPosition = _targetLocalPos;
            _cam.fieldOfView        = _targetFOV;
            _resetting              = false;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    //  API pública
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Guarda la posición/FOV actuales como "por defecto".
    /// Llamado por WorkstationSeat al sentarse y por OnEnable.
    /// </summary>
    public void RecordDefault()
    {
        _defaultLocalPos = transform.localPosition;
        _defaultFOV      = _cam != null ? _cam.fieldOfView : 60f;
        _targetLocalPos  = _defaultLocalPos;
        _targetFOV       = _defaultFOV;
        _resetting       = false;
    }

    /// <summary>Resetea la cámara instantáneamente (sin suavizado).</summary>
    public void ResetImmediate()
    {
        transform.localPosition = _defaultLocalPos;
        if (_cam) _cam.fieldOfView = _defaultFOV;
        _targetLocalPos = _defaultLocalPos;
        _targetFOV      = _defaultFOV;
        _resetting      = false;
    }
}
