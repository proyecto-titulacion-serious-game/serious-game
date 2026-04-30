using UnityEngine;

/// <summary>
/// Nodo eléctrico del circuito. Almacena voltaje y corriente calculados
/// por CircuitManager. El multímetro lee estos valores al hacer contacto.
///
/// SETUP EN UNITY:
///   Cada componente eléctrico (Resistor, LED, Capacitor, ArduinoPin, VoltageSource)
///   necesita 2 o 3 nodos hijo:
///
///   Resistor                       ← Resistor.cs
///   ├─ Node_Positive               ← CircuitNode.cs + SphereCollider (isTrigger=false)
///   │                                 nodeLabel = "A+", color dorado
///   └─ Node_Negative               ← CircuitNode.cs + SphereCollider (isTrigger=false)
///                                     nodeLabel = "B-", color plata
///
///   LED                            ← LED.cs
///   ├─ Node_Anode                  ← CircuitNode.cs (polo positivo, ánodo)
///   └─ Node_Cathode                ← CircuitNode.cs (polo negativo, cátodo)
///
///   VoltageSource
///   ├─ Node_Positive               ← el "9V" del circuito
///   └─ Node_Ground                 ← referencia 0V
///
/// IMPORTANTE: El collider de cada CircuitNode debe ser NON-trigger
/// para que el trigger de la punta del multímetro lo detecte.
///
/// VISUALIZACIÓN:
///   Los nodos se muestran como pequeñas esferas metálicas en el circuito.
///   El Explorador aprende qué nodo medir según el manual del Técnico.
/// </summary>
public class CircuitNode : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────

    [Header("Identificación")]
    [Tooltip("Nombre visible para debug y para el display del multímetro.")]
    public string nodeLabel = "Node";

    [Tooltip("Tipo de nodo — determina qué mide el multímetro aquí.")]
    public NodeType nodeType = NodeType.Intermediate;

    [Header("Valores eléctricos (actualizados por CircuitManager)")]
    public float voltage;   // Voltios respecto a tierra
    public float current;   // Amperios que fluyen por este punto

    [Header("Visual")]
    public Renderer nodeRenderer;
    public Color    colorNormal   = new Color(0.9f,  0.75f, 0.2f);  // dorado
    public Color    colorProbed   = new Color(0.2f,  0.85f, 0.4f);  // verde al medir
    public Color    colorGround   = new Color(0.7f,  0.7f,  0.7f);  // plata para tierra

    // ─────────────────────────────────────────────
    //  Estado
    // ─────────────────────────────────────────────
    private bool _isBeingProbed = false;

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────

    void Awake()
    {
        if (nodeRenderer == null)
            nodeRenderer = GetComponent<Renderer>();

        SetVisualIdle();
    }

    // ─────────────────────────────────────────────
    //  API pública
    // ─────────────────────────────────────────────

    /// <summary>
    /// Actualiza los valores eléctricos del nodo.
    /// Llamado por CircuitManager después de cada simulación.
    /// </summary>
    public void SetValues(float v, float i)
    {
        voltage = v;
        current = i;
    }

    /// <summary>Feedback visual cuando el multímetro toca este nodo.</summary>
    public void SetProbed(bool probed)
    {
        _isBeingProbed = probed;
        if (probed)
            SetColor(colorProbed);
        else
            SetVisualIdle();
    }

    // ─────────────────────────────────────────────
    //  Visual
    // ─────────────────────────────────────────────

    void SetVisualIdle()
    {
        SetColor(nodeType == NodeType.Ground ? colorGround : colorNormal);
    }

    void SetColor(Color c)
    {
        if (nodeRenderer == null) return;
        var mpb = new MaterialPropertyBlock();
        mpb.SetColor("_BaseColor", c);
        nodeRenderer.SetPropertyBlock(mpb);
    }

    // Debug visual en editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = nodeType == NodeType.Ground
            ? Color.gray
            : new Color(1f, 0.8f, 0f);
        Gizmos.DrawWireSphere(transform.position, 0.015f);
    }
}

// ──────────────────────────────────────────────────────────────────────────────

public enum NodeType
{
    Positive,       // Nodo positivo del componente
    Negative,       // Nodo negativo
    Ground,         // Tierra (referencia 0V)
    Intermediate    // Punto intermedio del circuito
}