using UnityEngine;

/// Añadir a cualquier GO en la escena para probar el multímetro con teclado.
/// Solo activo en el Editor.
/// Teclas:
///   1 → asigna punta ROJA al nodo + de la fuente de voltaje
///   2 → asigna punta NEGRA al nodo - de la fuente de voltaje
///   0 → reinicia ambas puntas
///   S → fuerza re-simulación del circuito (útil si el voltaje sale en 0)
///   I → imprime diagnóstico completo en Console
public class MultimeterDebugHelper : MonoBehaviour
{
#if UNITY_EDITOR
    Multimeter _multimeter;
    CircuitManager _circuit;
    ElectricalNode _nodePos;  // nodo + de la fuente
    ElectricalNode _nodeNeg;  // nodo – de la fuente

    void Start()
    {
        _multimeter = FindAnyObjectByType<Multimeter>();

        // Buscar el CircuitManager que tenga una VoltageSource con nodos asignados
        foreach (var cm in FindObjectsByType<CircuitManager>(FindObjectsSortMode.None))
        {
            var vs = cm.FindCircuitComponent<VoltageSource>();
            if (vs != null && vs.nodeA != null && vs.nodeB != null)
            {
                _circuit = cm;
                _nodePos = vs.nodeA;
                _nodeNeg = vs.nodeB;
                Debug.Log($"[MultimeterDebug] Fuente encontrada en '{cm.gameObject.name}': {vs.voltage}V");
                break;
            }
        }

        if (_multimeter == null) Debug.LogWarning("[MultimeterDebug] No se encontró Multimeter en escena.");
        if (_nodePos    == null) Debug.LogWarning("[MultimeterDebug] No se encontró VoltageSource con nodos. Presiona I para más info.");
        else                     Debug.Log("[MultimeterDebug] Listo — 1=Rojo(+)  2=Negro(-)  0=Reset  S=Simular  I=Info");
    }

    void Update()
    {
        if (_multimeter == null) return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            _multimeter.SetRedNode(_nodePos);
            Debug.Log("[MultimeterDebug] Punta ROJA asignada. Voltaje nodo+: " + _nodePos?.voltage + "V");
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            _multimeter.SetBlackNode(_nodeNeg);
            Debug.Log("[MultimeterDebug] Punta NEGRA asignada. Voltaje nodo-: " + _nodeNeg?.voltage + "V");
        }

        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            _multimeter.ResetProbes();
            Debug.Log("[MultimeterDebug] Puntas reiniciadas.");
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            _circuit?.ForceSimulate();
            Debug.Log($"[MultimeterDebug] Re-simulado → nodo+={_nodePos?.voltage:F2}V, nodo-={_nodeNeg?.voltage:F2}V");
        }

        if (Input.GetKeyDown(KeyCode.I))
            PrintDiagnostics();
    }

    void PrintDiagnostics()
    {
        string nodeInfo = _nodePos != null
            ? $"{_nodePos.gameObject.name} ({_nodePos.voltage:F2}V)"
            : "NULL";
        string negInfo = _nodeNeg != null
            ? $"{_nodeNeg.gameObject.name} ({_nodeNeg.voltage:F2}V)"
            : "NULL";

        Debug.Log(
            $"[MultimeterDebug] === DIAGNÓSTICO ===\n" +
            $"  Multimeter:      {(_multimeter != null ? _multimeter.gameObject.name : "NULL")}\n" +
            $"  Nodo+:           {nodeInfo}\n" +
            $"  Nodo-:           {negInfo}\n" +
            $"  isReading:       {_multimeter?.isReading}\n" +
            $"  measuredVoltage: {_multimeter?.measuredVoltage:F2} V\n" +
            $"  CircuitManager:  {(_circuit != null ? _circuit.gameObject.name : "NULL")}\n" +
            $"  Topología:       {_circuit?.topology}\n" +
            $"  ShortCircuit:    {_circuit?.isShortCircuited}\n" +
            $"  Componentes:     {_circuit?.components?.Count ?? 0}"
        );
    }

    void OnGUI()
    {
        if (_multimeter == null) return;
        GUILayout.BeginArea(new Rect(10, 10, 330, 150));
        GUILayout.Box(
            $"MULTÍMETRO DEBUG\n" +
            $"[1] Rojo(+)  [2] Negro(-)  [0] Reset\n" +
            $"[S] Forzar simulación  [I] Info\n" +
            $"Voltaje:   {_multimeter.measuredVoltage:F2} V\n" +
            $"Corriente: {_multimeter.measuredCurrent * 1000f:F1} mA\n" +
            $"Midiendo:  {_multimeter.isReading}  |  Nodo+: {_nodePos?.voltage:F2}V"
        );
        GUILayout.EndArea();
    }
#endif
}
