using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Componente físico sobre la mesa del Técnico.
/// PC:  hover (mouse) + click → selecciona y coloca en la bandeja.
/// VR:  rayo del controlador → hover + gatillo → mismo resultado.
///
/// SETUP VR (opcional — sin esto funciona solo en PC):
///   Añadir XRSimpleInteractable al GameObject.
///   No hace falta Rigidbody ni cambiar nada más; este script detecta
///   el interactable automáticamente en Awake y conecta los eventos.
/// </summary>
[RequireComponent(typeof(Renderer))]
[RequireComponent(typeof(Collider))]
public class DeskComponent : MonoBehaviour,
    IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────

    [Header("Configuración")]
    public ComponentType componentType    = ComponentType.Resistor;
    public float         componentValue  = 100f;
    public string        componentDescription = "100 Ω";

    [Header("Referencias")]
    public ComponentSendingTray tray;

    [Header("Prefab entregado")]
    [Tooltip("Variante específica que se envía al Explorador (LED verde, capacitor azul, etc.).\n" +
             "Deja vacío para usar el prefab default del ComponentDeliverySystem.")]
    public GameObject deliveredPrefab;

    [Header("Colores de feedback")]
    public Color colorNormal   = new Color(0.3f, 0.3f, 0.4f);
    public Color colorHover    = new Color(0.9f, 0.8f, 0.2f);
    public Color colorSelected = new Color(0.2f, 0.8f, 0.4f);

    [Header("Glow al seleccionar")]
    [Tooltip("Intensidad del brillo de emisión cuando el componente está seleccionado.")]
    public float emissionIntensity = 1.8f;
    [Tooltip("Escala adicional al seleccionar (1.08 = 8% más grande).")]
    public float selectedScaleMultiplier = 1.08f;

    // ─────────────────────────────────────────────
    //  Estado interno
    // ─────────────────────────────────────────────
    private Renderer              _renderer;
    private MaterialPropertyBlock _mpb;
    private static readonly int   _colorID         = Shader.PropertyToID("_Color");
    private static readonly int   _baseColorID     = Shader.PropertyToID("_BaseColor");
    private static readonly int   _emissionColorID = Shader.PropertyToID("_EmissionColor");
    private bool                  _isSelected;
    private Vector3               _originalScale;
    private XRBaseInteractable    _xrInteractable;

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────

    void Awake()
    {
        _renderer      = GetComponent<Renderer>() ?? GetComponentInChildren<Renderer>();
        _mpb           = new MaterialPropertyBlock();
        _originalScale = transform.localScale;

        // Ajustar BoxCollider a los bounds reales del mesh para que sea clicable.
        // El generador pone el BoxCollider con size (1,1,1) pero el FBX puede tener
        // bounds muy distintos; sin este ajuste el área de clic es casi invisible.
        FitColliderToMesh();

        // Copia por objeto del material para habilitar el keyword _EMISSION
        // sin afectar el material compartido de otros objetos.
        if (_renderer != null && _renderer.sharedMaterial != null)
        {
            var copy = new Material(_renderer.sharedMaterial);
            copy.EnableKeyword("_EMISSION");
            copy.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            _renderer.sharedMaterial = copy;
        }

        SetColor(colorNormal);

        if (tray == null)
            tray = FindAnyObjectByType<ComponentSendingTray>();

        _xrInteractable = GetComponent<XRBaseInteractable>();
    }

    void FitColliderToMesh()
    {
        var bc = GetComponent<BoxCollider>() ?? GetComponentInChildren<BoxCollider>();
        var mf = GetComponent<MeshFilter>() ?? GetComponentInChildren<MeshFilter>();
        if (bc == null || mf == null || mf.sharedMesh == null) return;

        Bounds b  = mf.sharedMesh.bounds;
        bc.center = b.center;
        bc.size   = b.size * 1.4f;   // 40% de margen para facilitar el clic
    }

    void OnEnable()
    {
        if (_xrInteractable == null) return;
        _xrInteractable.hoverEntered.AddListener(OnXRHoverEnter);
        _xrInteractable.hoverExited.AddListener(OnXRHoverExit);
        _xrInteractable.selectEntered.AddListener(OnXRSelect);
    }

    void OnDisable()
    {
        if (_xrInteractable == null) return;
        _xrInteractable.hoverEntered.RemoveListener(OnXRHoverEnter);
        _xrInteractable.hoverExited.RemoveListener(OnXRHoverExit);
        _xrInteractable.selectEntered.RemoveListener(OnXRSelect);
    }

    // ─────────────────────────────────────────────
    //  PC — Mouse Interaction (legacy, funciona en modo "Both")
    // ─────────────────────────────────────────────

    void OnMouseEnter() { if (!_isSelected) SetColor(colorHover);  }
    void OnMouseExit()  { if (!_isSelected) SetColor(colorNormal); }
    void OnMouseDown()  { SelectThisComponent(); }

    // ─────────────────────────────────────────────
    //  EventSystem pointer events (New Input System + PhysicsRaycaster)
    // ─────────────────────────────────────────────

    void IPointerClickHandler.OnPointerClick(PointerEventData e)  => SelectThisComponent();
    void IPointerEnterHandler.OnPointerEnter(PointerEventData e)  { if (!_isSelected) SetColor(colorHover);  }
    void IPointerExitHandler.OnPointerExit(PointerEventData e)    { if (!_isSelected) SetColor(colorNormal); }

    // ─────────────────────────────────────────────
    //  VR — XRI Interaction
    // ─────────────────────────────────────────────

    void OnXRHoverEnter(HoverEnterEventArgs args) { if (!_isSelected) SetColor(colorHover);  }
    void OnXRHoverExit (HoverExitEventArgs  args) { if (!_isSelected) SetColor(colorNormal); }
    void OnXRSelect    (SelectEnterEventArgs args) { SelectThisComponent(); }

    // ─────────────────────────────────────────────
    //  Lógica de selección
    // ─────────────────────────────────────────────

    public void SelectThisComponent()
    {
        foreach (var comp in FindObjectsByType<DeskComponent>(FindObjectsInactive.Exclude))
            comp.Deselect();

        _isSelected = true;
        SetColor(colorSelected);
        transform.localScale = _originalScale * selectedScaleMultiplier;
        tray?.PlaceComponent(this);

        Debug.Log($"[DeskComponent] Seleccionado: {componentType} {componentValue}");
    }

    public void Deselect()
    {
        _isSelected = false;
        SetColor(colorNormal);
        transform.localScale = _originalScale;
    }

    // ─────────────────────────────────────────────
    //  Helper
    // ─────────────────────────────────────────────

    void SetColor(Color c)
    {
        if (_renderer == null || _mpb == null) return;
        _renderer.GetPropertyBlock(_mpb);

        _mpb.SetColor(_colorID,     c);
        _mpb.SetColor(_baseColorID, c);     // URP Lit usa _BaseColor

        // Emisión: solo cuando está seleccionado
        Color emission = _isSelected ? c * emissionIntensity : Color.black;
        _mpb.SetColor(_emissionColorID, emission);

        _renderer.SetPropertyBlock(_mpb);
    }
}
