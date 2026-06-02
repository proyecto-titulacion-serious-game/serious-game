using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Engancha las dos patas de un <see cref="ElectricalComponent"/> (LED, resistencia, jumper)
/// al <see cref="ElectricalNode"/> más cercano — sea un <see cref="ProtoboardSlot"/> de la
/// protoboard o un header de pin del Arduino. Sin esto, un componente colocado nunca obtiene
/// nodeA/nodeB y el grafo del Reto 4 queda vacío.
///
/// Es OPT-IN: solo los componentes con este script se re-enganchan; los pre-cableados a mano
/// no se tocan. <see cref="ProtoboardSimulator"/> llama <see cref="Bind"/> en cada simulación
/// (tras BuildNodeMap), así que mover el componente lo re-conecta automáticamente.
///
/// Las patas (leadA/leadB) se auto-crean en los extremos del bounding box si no se asignan.
/// </summary>
[RequireComponent(typeof(ElectricalComponent))]
public class ProtoboardConnector : MonoBehaviour
{
    [Tooltip("Pata A (ánodo/positivo). Si null, se auto-crea en un extremo del componente.")]
    public Transform leadA;
    [Tooltip("Pata B (cátodo/negativo). Si null, se auto-crea en el extremo opuesto.")]
    public Transform leadB;
    [Tooltip("Radio máximo (m) para enganchar una pata a un nodo. ~1.2 cm por defecto.")]
    public float snapRadius = 0.012f;

    // Registro estático: ProtoboardSimulator itera esto sin FindObjectsByType cada tick.
    public static readonly List<ProtoboardConnector> Active = new List<ProtoboardConnector>();

    private ElectricalComponent _comp;
    private ProtoboardSimulator _sim;
    private Vector3 _lastPos;

    void Awake()
    {
        _comp = GetComponent<ElectricalComponent>();
        EnsureLeads();
    }

    void OnEnable()
    {
        if (!Active.Contains(this)) Active.Add(this);
        _lastPos = transform.position;
        MarkSimDirty();   // re-simula al aparecer/colocarse
    }

    void OnDisable() { Active.Remove(this); }

    void Update()
    {
        // Al mover el componente (agarrar/soltar) re-engancha y re-simula.
        if ((transform.position - _lastPos).sqrMagnitude > 1e-6f)  // > 1 mm
        {
            _lastPos = transform.position;
            MarkSimDirty();
        }
    }

    void MarkSimDirty()
    {
        if (_sim == null) _sim = FindAnyObjectByType<ProtoboardSimulator>();
        if (_sim != null) _sim.MarkDirty();
    }

    // ─────────────────────────────────────────────
    //  Enganche (llamado por ProtoboardSimulator)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Asigna nodeA/nodeB del componente al nodo más cercano a cada pata dentro de snapRadius.
    /// Una pata sin nodo cercano queda en null → ese extremo flota (circuito abierto), que es
    /// el comportamiento físico correcto.
    /// </summary>
    public void Bind(List<ConnectionPoint> points)
    {
        if (_comp == null || leadA == null || leadB == null) return;
        _comp.nodeA = Nearest(leadA.position, points);
        _comp.nodeB = Nearest(leadB.position, points);
    }

    /// <summary>
    /// Asigna patas externas (p.ej. Probe_A / Probe_B de un cable jumper) y descarta las
    /// auto-creadas por <see cref="EnsureLeads"/> si las hubiera. Llamar tras AddComponent.
    /// </summary>
    public void SetLeads(Transform a, Transform b)
    {
        if (a != null) ReplaceLead(ref leadA, a);
        if (b != null) ReplaceLead(ref leadB, b);
    }

    void ReplaceLead(ref Transform slot, Transform newLead)
    {
        if (slot != null && slot != newLead && slot.parent == transform &&
            (slot.name == "LeadA" || slot.name == "LeadB"))
            Destroy(slot.gameObject);   // limpia la auto-creada
        slot = newLead;
    }

    ElectricalNode Nearest(Vector3 p, List<ConnectionPoint> points)
    {
        ElectricalNode best = null;
        float bestSqr = snapRadius * snapRadius;
        foreach (var cp in points)
        {
            if (cp.node == null) continue;
            float d = (cp.position - p).sqrMagnitude;
            if (d <= bestSqr) { bestSqr = d; best = cp.node; }
        }
        return best;
    }

    // ─────────────────────────────────────────────
    //  Auto-creación de patas
    // ─────────────────────────────────────────────

    void EnsureLeads()
    {
        if (leadA != null && leadB != null) return;

        var rend = GetComponentInChildren<Renderer>();
        Vector3 center = rend != null ? rend.bounds.center  : transform.position;
        Vector3 ext    = rend != null ? rend.bounds.extents : Vector3.one * 0.01f;

        // Eje mundial más largo del bounding box = orientación de las patas
        Vector3 dir = Vector3.right; float m = ext.x;
        if (ext.y > m) { dir = Vector3.up;      m = ext.y; }
        if (ext.z > m) { dir = Vector3.forward; m = ext.z; }

        if (leadA == null) leadA = MakeLead("LeadA", center + dir * m);
        if (leadB == null) leadB = MakeLead("LeadB", center - dir * m);
    }

    Transform MakeLead(string n, Vector3 worldPos)
    {
        var go = new GameObject(n);
        go.transform.SetParent(transform, true);  // worldPositionStays → sigue al componente
        go.transform.position = worldPos;
        return go.transform;
    }

    void OnDrawGizmosSelected()
    {
        if (leadA != null) { Gizmos.color = Color.red;   Gizmos.DrawWireSphere(leadA.position, snapRadius); }
        if (leadB != null) { Gizmos.color = Color.black; Gizmos.DrawWireSphere(leadB.position, snapRadius); }
    }
}

/// <summary>Punto de conexión (posición + nodo eléctrico) que el conector puede enganchar.</summary>
public struct ConnectionPoint
{
    public Vector3        position;
    public ElectricalNode node;
    public ConnectionPoint(Vector3 position, ElectricalNode node) { this.position = position; this.node = node; }
}
