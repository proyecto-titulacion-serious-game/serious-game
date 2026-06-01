using UnityEngine;

/// <summary>
/// Detecta contacto físico de una punta del multímetro con los slots de la protoboard
/// y actualiza el <see cref="Multimeter"/> con el <see cref="ElectricalNode"/> tocado.
///
/// SETUP: Añadir este componente a los GOs Probe_Red_Tip y Probe_Black_Tip
///        del prefab Multimeter_VR_Art.
///
///   Probe_Red_Tip   ← MultimeterProbeContact (color = Red)  + SphereCollider (isTrigger)
///   Probe_Black_Tip ← MultimeterProbeContact (color = Black) + SphereCollider (isTrigger)
///
/// Los ProtoboardSlots deben tener colliders NON-trigger para ser detectados por el trigger.
/// </summary>
[RequireComponent(typeof(Collider))]
public class MultimeterProbeContact : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────

    [Tooltip("Indica si esta punta es la roja (+) o la negra (–).")]
    [SerializeField] private MultimeterProbeColor _color = MultimeterProbeColor.Red;

    [Tooltip("Referencia al Multimeter padre. Si null, se busca automáticamente en el padre.")]
    [SerializeField] private Multimeter _multimeter;

    // ─────────────────────────────────────────────
    //  Estado
    // ─────────────────────────────────────────────

    private ElectricalNode _contactNode; // nodo actualmente en contacto

    // ─────────────────────────────────────────────
    //  Unity
    // ─────────────────────────────────────────────

    void Awake()
    {
        // Auto-buscar Multimeter en el árbol de padres si no asignado en Inspector
        if (_multimeter == null)
            _multimeter = GetComponentInParent<Multimeter>();

        // Verificar que el collider sea trigger
        var col = GetComponent<Collider>();
        if (!col.isTrigger)
        {
            Debug.LogWarning($"[MultimeterProbeContact] El collider de '{name}' debe ser isTrigger. Corrigiéndolo automáticamente.", this);
            col.isTrigger = true;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (_multimeter == null) return;

        // ── Protoboard sandbox (Reto 4) ──────────────────────────────────────
        // Detecta ProtoboardSlot y obtiene su ElectricalNode asignado por ProtoboardSimulator
        var slot = other.GetComponent<ProtoboardSlot>();
        if (slot != null)
        {
            ElectricalNode node = slot.assignedNode;
            if (node == null) return; // BuildNodeMap() no ha corrido aún

            _contactNode = node;
            AssignNode(node);
            return;
        }

        // ── Nodos clásicos (Retos 1–3) ───────────────────────────────────────
        // Compatibilidad con el sistema de NodeInteractable ya existente
        var nodeInteractable = other.GetComponent<NodeInteractable>();
        if (nodeInteractable != null && nodeInteractable.nodeTarget != null)
        {
            _contactNode = nodeInteractable.nodeTarget;
            AssignNode(nodeInteractable.nodeTarget);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (_multimeter == null) return;

        // Verificar que es el mismo nodo (evitar limpiar si toca otro slot en el mismo frame)
        ElectricalNode exitNode = null;

        var slot = other.GetComponent<ProtoboardSlot>();
        if (slot != null) exitNode = slot.assignedNode;

        var ni = other.GetComponent<NodeInteractable>();
        if (ni != null) exitNode = ni.nodeTarget;

        if (exitNode != null && exitNode == _contactNode)
        {
            _contactNode = null;
            ClearNode();
        }
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────

    void AssignNode(ElectricalNode node)
    {
        if (_color == MultimeterProbeColor.Red) _multimeter.SetRedNode(node);
        else                                    _multimeter.SetBlackNode(node);
    }

    void ClearNode()
    {
        if (_color == MultimeterProbeColor.Red) _multimeter.SetRedNode(null);
        else                                    _multimeter.SetBlackNode(null);
    }
}

/// <summary>
/// Color de la punta del multímetro.
/// Enum separado del <see cref="MultimeterMode"/> para evitar colisión de nombres.
/// </summary>
public enum MultimeterProbeColor { Red, Black }
