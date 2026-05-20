using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Manual del Técnico en forma de pergamino enrollado.
///
/// Estado cerrado  → cilindro visible (Scroll_Roll), papel oculto.
/// Click           → animación de desenrollado (~0.5 s), luego abre el overlay.
/// Click / Escape  → cierra overlay y re-enrolla el papel.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ManualScroll : MonoBehaviour,
    IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Partes del scroll")]
    [Tooltip("Cilindro que representa el pergamino enrollado.")]
    public GameObject scrollRoll;
    [Tooltip("Cubo plano que se despliega hacia arriba al abrir.")]
    public GameObject scrollPaper;

    [Header("Manual (pantalla completa)")]
    public GameObject manualOverlay;

    [Header("Animación")]
    public float paperHeight  = 0.36f;
    public float animDuration = 0.50f;

    // ─────────────────────────────────────────────
    //  Estado interno
    // ─────────────────────────────────────────────
    private bool      _isOpen;
    private Coroutine _anim;
    private Vector3   _rollOriginalScale;

    // Hover
    private Renderer              _rollRenderer;
    private MaterialPropertyBlock _rollMpb;
    private Color                 _colorNormal;
    private static readonly int   _colorID = Shader.PropertyToID("_BaseColor");

    // ─────────────────────────────────────────────
    //  Lifecycle
    // ─────────────────────────────────────────────

    void Start()
    {
        // Guardar escala original del rollo
        if (scrollRoll != null)
            _rollOriginalScale = scrollRoll.transform.localScale;

        // Estado inicial: papel aplanado e invisible
        if (scrollPaper != null)
        {
            var s = scrollPaper.transform.localScale;
            scrollPaper.transform.localScale    = new Vector3(s.x, 0.001f, s.z);
            scrollPaper.transform.localPosition = new Vector3(
                scrollPaper.transform.localPosition.x, 0f,
                scrollPaper.transform.localPosition.z);
            scrollPaper.SetActive(false);
        }

        // Auto-buscar overlay usando TechnicianManualDisplay si no fue asignado
        if (manualOverlay == null)
        {
            var display = FindAnyObjectByType<TechnicianManualDisplay>();
            if (display != null) manualOverlay = display.gameObject;
        }

        if (manualOverlay != null) manualOverlay.SetActive(false);

        // Auto-conectar botón Cerrar dentro del overlay
        if (manualOverlay != null)
        {
            foreach (var btn in manualOverlay.GetComponentsInChildren<Button>(true))
            {
                string n = btn.name.ToLowerInvariant();
                if (n.Contains("cerrar") || n.Contains("close"))
                {
                    btn.onClick.RemoveListener(CloseManual);
                    btn.onClick.AddListener(CloseManual);
                }
            }
        }

        // Configurar hover del rollo con MaterialPropertyBlock (evita instanciar material)
        _rollRenderer = scrollRoll?.GetComponent<Renderer>();
        _rollMpb      = new MaterialPropertyBlock();
        if (_rollRenderer != null)
        {
            _rollRenderer.GetPropertyBlock(_rollMpb);
            _colorNormal = _rollMpb.HasProperty(_colorID)
                ? _rollMpb.GetColor(_colorID) : Color.white;
        }
    }

    void Update()
    {
        if (!_isOpen) return;

        bool esc = false;
#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null) esc = kb.escapeKey.wasPressedThisFrame;
#endif
        if (!esc) esc = Input.GetKeyDown(KeyCode.Escape);
        if (esc) CloseManual();
    }

    // ─────────────────────────────────────────────
    //  Pointer events (EventSystem + PhysicsRaycaster)
    // ─────────────────────────────────────────────

    void IPointerClickHandler.OnPointerClick(PointerEventData e) => Toggle();
    void IPointerEnterHandler.OnPointerEnter(PointerEventData e) => SetHover(true);
    void IPointerExitHandler.OnPointerExit(PointerEventData e)   => SetHover(false);

    void OnMouseDown()  => Toggle();
    void OnMouseEnter() => SetHover(true);
    void OnMouseExit()  => SetHover(false);

    // ─────────────────────────────────────────────
    //  API pública
    // ─────────────────────────────────────────────

    public void Toggle()
    {
        if (_isOpen) CloseManual();
        else         OpenManual();
    }

    /// <summary>Cierra el overlay y re-enrolla el scroll. Llamado también por el botón ✕.</summary>
    public void CloseManual()
    {
        if (!_isOpen) return;
        _isOpen = false;
        if (manualOverlay != null) manualOverlay.SetActive(false);
        if (_anim != null) StopCoroutine(_anim);
        _anim = StartCoroutine(AnimateClose());
    }

    // ─────────────────────────────────────────────
    //  Animaciones
    // ─────────────────────────────────────────────

    void OpenManual()
    {
        _isOpen = true;
        if (_anim != null) StopCoroutine(_anim);
        _anim = StartCoroutine(AnimateOpen());
    }

    IEnumerator AnimateOpen()
    {
        var rollT  = scrollRoll?.transform;
        var paperT = scrollPaper?.transform;

        if (scrollRoll  != null) scrollRoll.SetActive(true);
        if (scrollPaper != null)
        {
            scrollPaper.SetActive(true);
            paperT.localScale = new Vector3(paperT.localScale.x, 0.001f, paperT.localScale.z);
        }

        float elapsed = 0f;
        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t    = Mathf.Clamp01(elapsed / animDuration);
            float ease = 1f - Mathf.Pow(1f - t, 3f);   // ease-out cúbica

            // Rollo se achata (diámetro mengua)
            if (rollT != null)
            {
                float d = Mathf.Lerp(1f, 0.05f, ease);
                rollT.localScale = new Vector3(
                    _rollOriginalScale.x * d,
                    _rollOriginalScale.y,
                    _rollOriginalScale.z * d);
            }

            // Papel crece hacia arriba; pivote en la base
            if (paperT != null)
            {
                float h = Mathf.Lerp(0.001f, paperHeight, ease);
                paperT.localScale    = new Vector3(paperT.localScale.x, h, paperT.localScale.z);
                paperT.localPosition = new Vector3(
                    paperT.localPosition.x, h * 0.5f, paperT.localPosition.z);
            }

            yield return null;
        }

        // Al terminar: ocultar rollo y mostrar el overlay
        if (scrollRoll != null) scrollRoll.SetActive(false);
        if (manualOverlay != null)
        {
            manualOverlay.SetActive(true);

            // Refrescar contenido: buscar primero dentro del overlay, luego globalmente
            var display = manualOverlay.GetComponentInChildren<TechnicianManualDisplay>(true)
                          ?? FindAnyObjectByType<TechnicianManualDisplay>();
            if (display != null)
            {
                display.gameObject.SetActive(true);
                display.RefreshContent();
            }
            else
            {
                Debug.LogWarning("[ManualScroll] TechnicianManualDisplay no encontrado en la escena. " +
                                 "Añade el componente al Canvas del manual_Overlay y asigna los TMP_Text.");
            }
        }
        _anim = null;
    }

    IEnumerator AnimateClose()
    {
        var rollT  = scrollRoll?.transform;
        var paperT = scrollPaper?.transform;

        // Restaurar rollo aplanado para que crezca desde pequeño
        if (rollT != null)
        {
            scrollRoll.SetActive(true);
            rollT.localScale = new Vector3(
                _rollOriginalScale.x * 0.05f,
                _rollOriginalScale.y,
                _rollOriginalScale.z * 0.05f);
        }

        float startH = paperT != null ? paperT.localScale.y : paperHeight;

        float elapsed = 0f;
        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t    = Mathf.Clamp01(elapsed / animDuration);
            float ease = 1f - Mathf.Pow(1f - t, 3f);

            // Rollo recupera su tamaño
            if (rollT != null)
            {
                float d = Mathf.Lerp(0.05f, 1f, ease);
                rollT.localScale = new Vector3(
                    _rollOriginalScale.x * d,
                    _rollOriginalScale.y,
                    _rollOriginalScale.z * d);
            }

            // Papel se enrolla hacia abajo
            if (paperT != null)
            {
                float h = Mathf.Lerp(startH, 0.001f, ease);
                paperT.localScale    = new Vector3(paperT.localScale.x, h, paperT.localScale.z);
                paperT.localPosition = new Vector3(
                    paperT.localPosition.x, h * 0.5f, paperT.localPosition.z);
            }

            yield return null;
        }

        if (scrollPaper != null) scrollPaper.SetActive(false);
        _anim = null;
    }

    // ─────────────────────────────────────────────
    //  Hover
    // ─────────────────────────────────────────────

    void SetHover(bool on)
    {
        if (_rollRenderer == null || _isOpen) return;
        Color c = _colorNormal * (on ? 1.3f : 1f);
        c.a = 1f;
        _rollRenderer.GetPropertyBlock(_rollMpb);
        _rollMpb.SetColor(_colorID, c);
        _rollRenderer.SetPropertyBlock(_rollMpb);
    }
}
