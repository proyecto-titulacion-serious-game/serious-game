using UnityEngine;

/// <summary>
/// Componente físico sobre la mesa del Técnico.
/// PC: hover brillo + click para seleccionar y colocar en la bandeja.
/// VR: XRGrabInteractable para agarrar y soltar en la bandeja.
/// </summary>
[RequireComponent(typeof(Renderer))]
[RequireComponent(typeof(Collider))]
public class DeskComponent : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────

    /// <summary>Tipo de componente que representa este objeto 3D.</summary>
    [Header("Configuración")]
    public ComponentType componentType = ComponentType.Resistor;

    /// <summary>Valor del componente (ohms para resistencias, µF para capacitores).</summary>
    public float componentValue = 100f;

    /// <summary>Descripción para la bandeja. Ej: "100Ω | Marrón-Negro-Marrón-Oro"</summary>
    public string componentDescription = "100 Ω";

    [Header("Referencias")]
    public ComponentSendingTray tray;

    [Header("Colores de feedback")]
    public Color colorNormal   = new Color(0.3f, 0.3f, 0.4f);
    public Color colorHover    = new Color(0.9f, 0.8f, 0.2f);
    public Color colorSelected = new Color(0.2f, 0.8f, 0.4f);

    // ─────────────────────────────────────────────
    //  Estado interno
    // ─────────────────────────────────────────────
    private Renderer             _renderer;
    private MaterialPropertyBlock _mpb;
    private static readonly int  _colorID = Shader.PropertyToID("_Color");
    private bool _isSelected = false;

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _mpb      = new MaterialPropertyBlock();
        SetColor(colorNormal);

        // Buscar bandeja automáticamente si no está asignada
        if (tray == null)
            tray = FindObjectOfType<ComponentSendingTray>();
    }

    // ─────────────────────────────────────────────
    //  PC — Mouse Interaction
    // ─────────────────────────────────────────────

    /// <summary>Brillo al pasar el mouse (PC).</summary>
    void OnMouseEnter()
    {
        if (!_isSelected) SetColor(colorHover);
    }

    /// <summary>Restaura color al salir el mouse (PC).</summary>
    void OnMouseExit()
    {
        if (!_isSelected) SetColor(colorNormal);
    }

    /// <summary>
    /// Click del mouse — selecciona el componente y lo coloca en la bandeja (PC).
    /// </summary>
    void OnMouseDown()
    {
        SelectThisComponent();
    }

    // ─────────────────────────────────────────────
    //  Lógica de selección
    // ─────────────────────────────────────────────

    /// <summary>
    /// Selecciona este componente: lo coloca en la bandeja de envío.
    /// Desselecciona cualquier componente previamente seleccionado.
    /// </summary>
    public void SelectThisComponent()
    {
        // Deseleccionar todos los demás
        foreach (var comp in FindObjectsOfType<DeskComponent>())
            comp.Deselect();

        _isSelected = true;
        SetColor(colorSelected);

        // Colocar en la bandeja
        tray?.PlaceComponent(this);

        Debug.Log($"[DeskComponent] Seleccionado: {componentType} {componentValue}");
    }

    /// <summary>Deselecciona este componente y restaura su color.</summary>
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