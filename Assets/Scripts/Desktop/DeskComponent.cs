using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Renderer), typeof(Collider))]
public class DeskComponent : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Configuración")]
    public ComponentType componentType = ComponentType.Resistor;
    public float         componentValue  = 100f;
    public string        componentDescription = "100 Ω";

    [Header("Referencias")]
    public ComponentSendingTray tray;
    public GameObject deliveredPrefab;

    [Header("Visuales")]
    public Color colorNormal   = Color.white;
    public Color colorHover    = new Color(1f, 0.85f, 0.2f);
    public Color colorSelected = Color.yellow;
    public float selectedScaleMultiplier = 1.1f;
    public float emissionIntensity = 1.5f;

    private Renderer _renderer;
    private MaterialPropertyBlock _mpb;
    private bool _isSelected;
    private Vector3 _originalScale;
    private static readonly int _colorID = Shader.PropertyToID("_EmissionColor");
    private static readonly int _baseColorID = Shader.PropertyToID("_BaseColor");

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _mpb = new MaterialPropertyBlock();
        _originalScale = transform.localScale;
        
        var xr = GetComponent<XRSimpleInteractable>();
        if (xr) xr.selectEntered.AddListener(_ => SelectThisComponent());
    }

    public void OnPointerClick(PointerEventData eventData) => SelectThisComponent();
    public void OnPointerEnter(PointerEventData eventData) { if (!_isSelected) SetColor(colorNormal * 1.2f); }
    public void OnPointerExit(PointerEventData eventData)  { if (!_isSelected) SetColor(colorNormal); }

    public void SelectThisComponent()
    {
        // El mediador (tray) se encarga de deseleccionar a los demás
        if (tray != null) tray.SetSelectedComponent(this);
    }

    // Método llamado por la Bandeja
    public void SetSelectionState(bool isSelected)
    {
        _isSelected = isSelected;
        // Aquí debe estar la lógica que antes tenías en Deselect o en el método de selección
        SetColor(isSelected ? colorSelected : colorNormal);
        transform.localScale = isSelected ? _originalScale * selectedScaleMultiplier : _originalScale;
    }

    void SetColor(Color c)
    {
        if (_renderer == null) return;
        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(_baseColorID, c);
        _mpb.SetColor(_colorID, _isSelected ? c * emissionIntensity : Color.black);
        _renderer.SetPropertyBlock(_mpb);
    }
}