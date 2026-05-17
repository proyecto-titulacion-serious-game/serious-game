using UnityEngine;

/// Añadir a Probe_Red_Tip y Probe_Black_Tip en el prefab Multimeter_VR.
/// Cuando la punta toca físicamente el collider de un NodeInteractable,
/// asigna ese nodo al multímetro sin necesitar ningún botón ni raycast.
[RequireComponent(typeof(Collider))]
public class MultimeterProbe : MonoBehaviour
{
    public Multimeter multimeter;
    public ProbeType  probeType = ProbeType.Red;

    void Awake()
    {
        if (multimeter == null)
            multimeter = GetComponentInParent<Multimeter>(true);
    }

    void OnTriggerEnter(Collider other)
    {
        if (multimeter == null) return;

        var node = other.GetComponent<NodeInteractable>()
                ?? other.GetComponentInParent<NodeInteractable>();

        if (node == null || node.nodeTarget == null) return;

        if (probeType == ProbeType.Red)
            multimeter.SetRedNode(node.nodeTarget);
        else
            multimeter.SetBlackNode(node.nodeTarget);
    }
}
