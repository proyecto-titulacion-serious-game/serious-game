using UnityEngine;

/// <summary>
/// Ranura física de la protoboard. Slots con el mismo railId comparten un nodo eléctrico
/// (equivale a la misma fila de cobre en una protoboard real).
/// CircuitSimulator asigna el nodo representativo al campo assignedNode.
/// </summary>
[DisallowMultipleComponent]
public class ProtoboardSlot : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Identificación eléctrica")]
    [Tooltip("Slots con el mismo railId están cortocircuitados internamente (misma tira de cobre).")]
    public string railId = "A";

    [Header("Posición en la cuadrícula")]
    public int row;
    public int col;

    [Header("Visual")]
    [SerializeField] private Color _colorEmpty    = new Color(0.35f, 0.35f, 0.35f);
    [SerializeField] private Color _colorOccupied = new Color(0.2f,  0.8f,  0.2f);

    // ─────────────────────────────────────────────
    //  Runtime (asignado por CircuitSimulator)
    // ─────────────────────────────────────────────
    [HideInInspector] public ElectricalNode assignedNode;

    // ─────────────────────────────────────────────
    //  Privado
    // ─────────────────────────────────────────────
    private Renderer _rend;
    private bool _occupied;

    void Awake()
    {
        _rend = GetComponentInChildren<Renderer>();
        SetOccupied(false);
    }

    public void SetOccupied(bool occupied)
    {
        if (_occupied == occupied) return;
        _occupied = occupied;
        if (_rend) _rend.material.color = occupied ? _colorOccupied : _colorEmpty;
    }

    public bool IsOccupied => _occupied;
}
