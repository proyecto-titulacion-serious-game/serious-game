using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Dibuja cables visuales entre componentes del circuito usando LineRenderer.
/// Se actualiza automáticamente cuando el circuito cambia (OnCircuitChanged).
///
/// SETUP en Unity:
///   1. Crear Empty GameObject hijo de Circuit → renombrar "WireRenderer"
///   2. Agregar este script
///   3. Definir las conexiones en el inspector arrastrando los Transforms
///   4. El script crea un LineRenderer por cada cable automáticamente
///
/// JERARQUÍA RESULTANTE:
///   Circuit
///   ├─ VoltageSource
///   ├─ Resistor
///   ├─ LED
///   ├─ Node Positive   ← esferas de nodo
///   ├─ Node Ground
///   └─ WireRenderer    ← este script
///       ├─ Wire_0      ← generado automáticamente
///       ├─ Wire_1
///       └─ Wire_2
/// </summary>
public class CircuitWireRenderer : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────

    [Header("Conexiones (definir en orden del circuito)")]
    [Tooltip("Lista de puntos del circuito en orden. El script dibuja A→B→C→D...")]
    public List<Transform> circuitPoints = new List<Transform>();

    [Header("Apariencia del cable")]
    [Tooltip("Material del cable. Usar Unlit/Color o UI/Default para Built-in RP.")]
    public Material wireMaterial;
    public Color    colorNormal  = new Color(0.2f, 0.8f, 0.3f);   // verde activo
    public Color    colorFaulty  = new Color(0.8f, 0.2f, 0.2f);   // rojo con falla
    public Color    colorFlow    = new Color(0.4f, 1.0f, 0.6f);   // verde brillante fluyendo
    [Range(0.005f, 0.05f)]
    public float    wireWidth    = 0.015f;

    [Header("Animación de flujo (opcional)")]
    public bool  animateFlow    = true;
    public float flowSpeed      = 1.5f;

    [Header("Referencia al CircuitManager")]
    public CircuitManager circuitManager;

    // ─────────────────────────────────────────────
    //  Internos
    // ─────────────────────────────────────────────
    private List<LineRenderer> _wires = new List<LineRenderer>();
    private bool   _hasCurrent = false;
    private float  _flowOffset = 0f;

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────
    void Awake()
    {
        if (circuitManager == null)
            circuitManager = GetComponentInParent<CircuitManager>();

        // Crear material por defecto si no se asignó
        if (wireMaterial == null)
        {
            wireMaterial = new Material(Shader.Find("Unlit/Color"));
            wireMaterial.color = colorNormal;
        }

        BuildWires();
    }

    void OnEnable()
    {
        CircuitManager.OnCircuitChanged += OnCircuitChanged;
    }

    void OnDisable()
    {
        CircuitManager.OnCircuitChanged -= OnCircuitChanged;
    }

    void Update()
    {
        if (!animateFlow || !_hasCurrent) return;

        // Animar offset de textura para simular flujo de corriente
        _flowOffset += Time.deltaTime * flowSpeed;
        foreach (var wire in _wires)
            if (wire != null)
                wire.material.SetTextureOffset("_MainTex", new Vector2(_flowOffset, 0));
    }

    // ─────────────────────────────────────────────
    //  Construcción de cables
    // ─────────────────────────────────────────────

    void BuildWires()
    {
        // Limpiar cables anteriores
        foreach (var w in _wires)
            if (w != null) Destroy(w.gameObject);
        _wires.Clear();

        if (circuitPoints.Count < 2) return;

        // Crear un LineRenderer por cada segmento
        for (int i = 0; i < circuitPoints.Count - 1; i++)
        {
            if (circuitPoints[i] == null || circuitPoints[i + 1] == null) continue;

            GameObject wireGO = new GameObject($"Wire_{i}");
            wireGO.transform.SetParent(transform);

            LineRenderer lr = wireGO.AddComponent<LineRenderer>();
            lr.material         = new Material(wireMaterial);
            lr.startWidth       = wireWidth;
            lr.endWidth         = wireWidth;
            lr.positionCount    = 2;
            lr.useWorldSpace    = true;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows   = false;

            lr.SetPosition(0, circuitPoints[i].position);
            lr.SetPosition(1, circuitPoints[i + 1].position);

            SetWireColor(lr, colorNormal);
            _wires.Add(lr);
        }
    }

    // ─────────────────────────────────────────────
    //  Actualización al cambiar el circuito
    // ─────────────────────────────────────────────

    void OnCircuitChanged()
    {
        UpdateWirePositions();
        UpdateWireColors();
    }

    void UpdateWirePositions()
    {
        for (int i = 0; i < _wires.Count; i++)
        {
            if (_wires[i] == null) continue;
            if (i     < circuitPoints.Count && circuitPoints[i]     != null)
                _wires[i].SetPosition(0, circuitPoints[i].position);
            if (i + 1 < circuitPoints.Count && circuitPoints[i + 1] != null)
                _wires[i].SetPosition(1, circuitPoints[i + 1].position);
        }
    }

    void UpdateWireColors()
    {
        if (circuitManager == null) return;

        _hasCurrent = circuitManager.totalCurrent > 0.001f;

        Color targetColor = circuitManager.isShortCircuited ? colorFaulty
                          : _hasCurrent                     ? colorFlow
                          :                                   colorNormal;

        foreach (var wire in _wires)
            if (wire != null) SetWireColor(wire, targetColor);
    }

    // ─────────────────────────────────────────────
    //  API pública
    // ─────────────────────────────────────────────

    /// <summary>Reconstruye todos los cables (útil al mover componentes en runtime).</summary>
    public void Rebuild() => BuildWires();

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────

    void SetWireColor(LineRenderer lr, Color c)
    {
        lr.startColor = c;
        lr.endColor   = c;
        lr.material.color = c;
    }

    // Debug: dibuja los puntos del circuito en el editor
    void OnDrawGizmos()
    {
        if (circuitPoints == null || circuitPoints.Count < 2) return;

        Gizmos.color = Color.green;
        for (int i = 0; i < circuitPoints.Count - 1; i++)
        {
            if (circuitPoints[i] == null || circuitPoints[i + 1] == null) continue;
            Gizmos.DrawLine(circuitPoints[i].position, circuitPoints[i + 1].position);
            Gizmos.DrawWireSphere(circuitPoints[i].position, 0.02f);
        }
    }
}