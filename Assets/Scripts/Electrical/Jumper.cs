using UnityEngine;

/// <summary>
/// Cable puente (jumper) del sandbox: un <see cref="ElectricalComponent"/> de resistencia
/// casi nula que conecta dos nodos eléctricos. Es lo que el Explorador usa para llevar
/// corriente del header de un pin del Arduino a la protoboard, o entre filas.
///
/// Combinar con <see cref="ProtoboardConnector"/> para que sus dos extremos se enganchen
/// automáticamente al nodo (slot o pin) más cercano. Si está cortado (<see cref="isOpenCircuit"/>)
/// se comporta como circuito abierto (1 MΩ).
/// </summary>
public class Jumper : ElectricalComponent
{
    [Tooltip("Resistencia del cable en ohmios (casi 0). Sube para simular un cable de mala calidad.")]
    public float resistance = 0.01f;

    public override float GetResistance() => isOpenCircuit ? 1_000_000f : Mathf.Max(0.001f, resistance);

    public override void Calculate()
    {
        if (nodeA == null || nodeB == null) { current = 0f; voltageDrop = 0f; return; }
        voltageDrop = nodeA.voltage - nodeB.voltage;
        current     = voltageDrop / GetResistance();
    }
}
