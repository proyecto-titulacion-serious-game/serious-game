using UnityEngine;
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
public class DeskComponent : MonoBehaviour
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

    [Header("Colores de feedback")]
    public Color colorNormal   = new Color(0.3f, 0.3f, 0.4f);
    public Color colorHover    = new Color(0.9f, 0.8f, 0.2f);
    public Color colorSelected = new Color(0.2f, 0.8f, 0.4f);

    // ─────────────────────────────────────────────
    //  Estado interno
    // ─────────────────────────────────────────────
    private Renderer              _renderer;
    private MaterialPropertyBlock _mpb;
    private static readonly int   _colorID = Shader.PropertyToID("_Color");
    private bool                  _isSelected;
    private XRBaseInteractable    _xrInteractable;   // null si no hay XRI en la escena

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _mpb      = new MaterialPropertyBlock();
        SetColor(colorNormal);

        if (tray == null)
            tray = FindFirstObjectByType<ComponentSendingTray>();

        // XRI es opcional: solo se activa si el GameObject tiene XRSimpleInteractable
        _xrInteractable = GetComponent<XRBaseInteractable>();
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
    //  PC — Mouse Interaction
    // ─────────────────────────────────────────────

    void OnMouseEnter() { if (!_isSelected) SetColor(colorHover);   }
    void OnMouseExit()  { if (!_isSelected) SetColor(colorNormal);  }
    void OnMouseDown()  { SelectThisComponent(); }

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
        foreach (var comp in FindObjectsByType<DeskComponent>(FindObjectsSortMode.None))
            comp.Deselect();

        _isSelected = true;
        SetColor(colorSelected);
        tray?.PlaceComponent(this);

        Debug.Log($"[DeskComponent] Seleccionado: {componentType} {componentValue}");
    }

    public void Deselect()
    {
        _isSelected = false;
        SetColor(colorNormal);
    }

    // ─────────────────────────────────────────────
    //  Helper
    // ─────────────────────────────────────────────

    void SetColor(Color c)
    {
        if (_renderer == null || _mpb == null) return;
        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(_colorID, c);
        _renderer.SetPropertyBlock(_mpb);
    }
}