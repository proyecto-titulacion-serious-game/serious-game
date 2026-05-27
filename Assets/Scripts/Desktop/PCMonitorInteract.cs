using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Añade al Monitor del PC de escritorio la misma mecánica que el Clipboard/ManualScroll:
/// click → abre el overlay del manual técnico.
/// click / Escape → cierra el overlay.
/// Hover → brillo en el material del monitor.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PCMonitorInteract : MonoBehaviour,
    IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Manual Overlay")]
    [Tooltip("El mismo manualOverlay que usa ManualScroll. Se auto-busca si queda vacío.")]
    public GameObject manualOverlay;

    [Header("Cámara de referencia")]
    public Camera pcCamera;

    // ── Estado ────────────────────────────────────────────────────────────────
    private bool _isOpen;

    // ── Hover glow ────────────────────────────────────────────────────────────
    private Renderer              _renderer;
    private MaterialPropertyBlock _mpb;
    private Color                 _baseColorNormal;
    private static readonly int   ColorPropID = Shader.PropertyToID("_BaseColor");

    // ─────────────────────────────────────────────
    //  Lifecycle
    // ─────────────────────────────────────────────

    void Start()
    {
        if (pcCamera == null) pcCamera = Camera.main;

        // Auto-localizar manualOverlay a través de TechnicianManualDisplay
        if (manualOverlay == null)
        {
            var display = FindAnyObjectByType<TechnicianManualDisplay>();
            if (display != null) manualOverlay = display.gameObject;
        }

        if (manualOverlay != null) manualOverlay.SetActive(false);

        // Hover: usar MaterialPropertyBlock sobre el Renderer del Monitor
        _renderer = GetComponent<Renderer>();
        _mpb      = new MaterialPropertyBlock();

        if (_renderer != null)
        {
            _renderer.GetPropertyBlock(_mpb);
            _baseColorNormal = _mpb.HasProperty(ColorPropID)
                ? _mpb.GetColor(ColorPropID)
                : Color.white;
        }
    }

    void Update()
    {
        if (!_isOpen) return;

        bool esc = false;
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null) esc = kb.escapeKey.wasPressedThisFrame;
#endif
        if (esc) Close();
    }

    // ─────────────────────────────────────────────
    //  Pointer events (EventSystem + PhysicsRaycaster)
    // ─────────────────────────────────────────────

    void IPointerClickHandler.OnPointerClick(PointerEventData e) => Toggle();
    void IPointerEnterHandler.OnPointerEnter(PointerEventData e) => SetHover(true);
    void IPointerExitHandler.OnPointerExit(PointerEventData e)   => SetHover(false);

    // Fallback sin EventSystem
    void OnMouseDown()  => Toggle();
    void OnMouseEnter() => SetHover(true);
    void OnMouseExit()  => SetHover(false);

    // ─────────────────────────────────────────────
    //  Lógica de apertura / cierre
    // ─────────────────────────────────────────────

    public void Toggle()
    {
        if (_isOpen) Close();
        else         Open();
    }

    void Open()
    {
        _isOpen = true;
        SetHover(false);

        if (manualOverlay == null)
        {
            Debug.LogWarning("[PCMonitorInteract] manualOverlay no asignado.", this);
            return;
        }

        manualOverlay.SetActive(true);

        // Refrescar contenido igual que ManualScroll
        var display = manualOverlay.GetComponentInChildren<TechnicianManualDisplay>(true)
                      ?? FindAnyObjectByType<TechnicianManualDisplay>();
        if (display != null)
        {
            display.gameObject.SetActive(true);
            display.RefreshContent();
        }
    }

    public void Close()
    {
        _isOpen = false;
        if (manualOverlay != null) manualOverlay.SetActive(false);
    }

    // ─────────────────────────────────────────────
    //  Hover
    // ─────────────────────────────────────────────

    void SetHover(bool on)
    {
        if (_renderer == null || _isOpen) return;
        _renderer.GetPropertyBlock(_mpb);
        Color c = _baseColorNormal * (on ? 1.35f : 1f);
        c.a = _baseColorNormal.a;
        _mpb.SetColor(ColorPropID, c);
        _renderer.SetPropertyBlock(_mpb);
    }
}
