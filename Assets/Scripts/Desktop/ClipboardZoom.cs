using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Adjunta al clipboard del Técnico.
/// Click → el clipboard se acerca a la cámara con animación ease-out.
/// Click de nuevo (o Escape) → regresa a su posición en la mesa.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ClipboardZoom : MonoBehaviour,
    IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Referencias")]
    public Camera pcCamera;

    [Header("Zoom")]
    [Tooltip("Metros frente a la cámara cuando está abierto.")]
    public float zoomedDistance  = 0.45f;
    [Tooltip("Multiplicador de escala al acercarse (1 = sin cambio de tamaño).")]
    public float zoomedScaleMult = 2f;
    [Tooltip("Duración de la animación en segundos.")]
    public float animDuration    = 0.35f;

    // ── Estado en reposo (guardado al inicio) ─────────────────────────────────
    private Vector3    _restPosition;
    private Quaternion _restRotation;
    private Vector3    _restScale;
    private bool       _isZoomed;
    private Coroutine  _currentAnim;

    // ── Hover ─────────────────────────────────────────────────────────────────
    private Renderer _boardRenderer;
    private Material _boardMat;
    private Color    _colorNormal;
    private static readonly int _colorID = Shader.PropertyToID("_BaseColor");

    // ─────────────────────────────────────────────
    //  Lifecycle
    // ─────────────────────────────────────────────

    void Start()
    {
        _restPosition = transform.position;
        _restRotation = transform.rotation;
        _restScale    = transform.localScale;

        // Tomar renderer del tablero (primer hijo con Renderer)
        _boardRenderer = GetComponentInChildren<Renderer>();
        if (_boardRenderer != null)
        {
            _boardMat    = _boardRenderer.material;
            _colorNormal = _boardMat.HasProperty(_colorID)
                ? _boardMat.GetColor(_colorID)
                : Color.white;
        }

        if (pcCamera == null)
            pcCamera = Camera.main;
    }

    void Update()
    {
        if (!_isZoomed) return;

        bool escape = false;
#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb != null) escape = kb.escapeKey.wasPressedThisFrame;
#endif
        if (escape) Toggle();
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
    //  API pública
    // ─────────────────────────────────────────────

    public bool IsZoomed => _isZoomed;

    public void Close()
    {
        if (!_isZoomed) return;
        Toggle();
    }

    // ─────────────────────────────────────────────
    //  Animación
    // ─────────────────────────────────────────────

    void Toggle()
    {
        if (_currentAnim != null) StopCoroutine(_currentAnim);
        _isZoomed    = !_isZoomed;
        _currentAnim = StartCoroutine(AnimateTo(_isZoomed));
        SetHover(false);
    }

    IEnumerator AnimateTo(bool zoomIn)
    {
        Vector3    fromPos   = transform.position;
        Quaternion fromRot   = transform.rotation;
        Vector3    fromScale = transform.localScale;

        Vector3    toPos;
        Quaternion toRot;
        Vector3    toScale;

        if (zoomIn && pcCamera != null)
        {
            var camT = pcCamera.transform;
            toPos    = camT.position + camT.forward * zoomedDistance;
            toRot    = Quaternion.LookRotation(-camT.forward, camT.up);
            toScale  = _restScale * zoomedScaleMult;
        }
        else
        {
            toPos   = _restPosition;
            toRot   = _restRotation;
            toScale = _restScale;
        }

        float elapsed = 0f;
        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t    = Mathf.Clamp01(elapsed / animDuration);
            float ease = 1f - Mathf.Pow(1f - t, 3f);   // ease-out cúbica

            transform.position   = Vector3.Lerp(fromPos, toPos, ease);
            transform.rotation   = Quaternion.Slerp(fromRot, toRot, ease);
            transform.localScale = Vector3.Lerp(fromScale, toScale, ease);

            yield return null;
        }

        transform.position   = toPos;
        transform.rotation   = toRot;
        transform.localScale = toScale;
        _currentAnim = null;
    }

    void SetHover(bool on)
    {
        if (_boardMat == null || _isZoomed) return;
        if (!_boardMat.HasProperty(_colorID)) return;

        Color c = _colorNormal * (on ? 1.3f : 1f);
        c.a = 1f;
        _boardMat.SetColor(_colorID, c);
    }
}
