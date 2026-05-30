using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Botón físico en el multímetro que cicla entre modos: DCVoltage → DCCurrent → Resistance.
/// Requiere XRSimpleInteractable en el mismo GO.
/// Cambia de color para indicar el modo activo.
/// VERSIÓN BLINDADA: Previene errores de inicialización nula en el renderizado de Unity.
/// </summary>
[RequireComponent(typeof(XRSimpleInteractable))]
public class MultimeterModeButton : MonoBehaviour
{
    [Header("Referencias")]
    public Multimeter multimeter;

    [Header("Colores por modo")]
    public Color colorVoltage    = new Color(1f,   0.85f, 0f);    // amarillo
    public Color colorCurrent    = new Color(0f,   0.75f, 1f);    // cyan
    public Color colorResistance = new Color(0.2f, 0.9f,  0.3f);  // verde

    private XRSimpleInteractable _interactable;
    private Renderer              _renderer;
    private MaterialPropertyBlock _mpb;
    private static readonly int   _baseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int   _emissionID  = Shader.PropertyToID("_EmissionColor");

    void Awake()
    {
        _interactable = GetComponent<XRSimpleInteractable>();
        _renderer     = GetComponent<Renderer>();
        
        // Inicialización temprana
        _mpb          = new MaterialPropertyBlock();

        if (multimeter == null)
            multimeter = GetComponentInParent<Multimeter>(true);
    }

    void OnEnable()
    {
        if (_interactable != null)
            _interactable.selectEntered.AddListener(OnPressed);
            
        UpdateColor();
    }

    void OnDisable()
    {
        if (_interactable != null)
            _interactable.selectEntered.RemoveListener(OnPressed);
    }

    void OnPressed(SelectEnterEventArgs _)
    {
        if (multimeter == null) return;
        int next = ((int)multimeter.mode + 1) % 3;
        multimeter.SetMode((MultimeterMode)next);
        UpdateColor();
        Debug.Log($"[ModeButton] Modo → {multimeter.mode}");
    }

    void UpdateColor()
    {
        if (_renderer == null || multimeter == null) return;

        // ── FIX: Protección absoluta contra el error "Value cannot be null" ──
        // Garantiza que la tubería gráfica siempre tenga el bloque de material listo
        if (_mpb == null)
        {
            _mpb = new MaterialPropertyBlock();
        }

        Color c = multimeter.mode switch
        {
            MultimeterMode.DCVoltage  => colorVoltage,
            MultimeterMode.DCCurrent  => colorCurrent,
            MultimeterMode.Resistance => colorResistance,
            _                         => Color.white
        };
        
        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(_baseColorID, c);
        _mpb.SetColor(_emissionID,  c * 0.6f);
        _renderer.SetPropertyBlock(_mpb);
    }
}