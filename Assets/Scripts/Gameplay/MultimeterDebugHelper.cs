using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Panel de prueba del multímetro — funciona en Play Mode sin VR.
///
/// SETUP: Añadir este script a cualquier GameObject activo en la escena
///        (p.ej. el GameManager_System o un GO vacío "DebugTools").
///
/// TECLAS (Game View activo):
///   1 → Asigna punta ROJA  al nodo + (VoltageSource.nodeA)
///   2 → Asigna punta NEGRA al nodo - (VoltageSource.nodeB)
///   0 → Reinicia ambas puntas
///   S → Fuerza re-simulación del circuito activo
///   R → Reintenta buscar el CircuitManager (útil si la zona se activó tarde)
///   I → Imprime diagnóstico detallado en la Console
/// </summary>
public class MultimeterDebugHelper : MonoBehaviour
{
    [Header("Debug UI")]
    [Tooltip("Mostrar panel en pantalla. Desactivar para builds de producción.")]
    public bool showGui = true;

    // ─────────────────────────────────────────────
    //  Estado interno
    // ─────────────────────────────────────────────
    Multimeter      _multimeter;
    CircuitManager  _circuit;
    ElectricalNode  _nodePos;   // VoltageSource.nodeA  (+)
    ElectricalNode  _nodeNeg;   // VoltageSource.nodeB  (–)
    float           _retryTimer;
    string          _lastStatus = "Iniciando…";

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────
    void Start()
    {
        _multimeter = FindAnyObjectByType<Multimeter>();
        if (_multimeter == null)
            _lastStatus = "ERROR: No hay Multimeter en escena.";
        TryFindCircuit();
    }

    void Update()
    {
        if (_multimeter == null) return;

        // Re-intentar búsqueda cada segundo mientras no haya nodos
        if (_nodePos == null)
        {
            _retryTimer += Time.deltaTime;
            if (_retryTimer >= 1f) { _retryTimer = 0f; TryFindCircuit(); }
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) AssignProbe(red: true);
        if (Input.GetKeyDown(KeyCode.Alpha2)) AssignProbe(red: false);
        if (Input.GetKeyDown(KeyCode.Alpha0)) ResetAll();
        if (Input.GetKeyDown(KeyCode.S))      ForceSimulate();
        if (Input.GetKeyDown(KeyCode.R))      TryFindCircuit();
        if (Input.GetKeyDown(KeyCode.I))      PrintDiagnostic();
    }

    // ─────────────────────────────────────────────
    //  Búsqueda de circuito
    // ─────────────────────────────────────────────

    /// <summary>
    /// Busca el CircuitManager que tenga una VoltageSource con nodos asignados.
    /// Incluye objetos inactivos para cubrir el caso en que la zona empieza desactivada.
    /// Prioriza el CM que GameManager.circuit referencia actualmente.
    /// </summary>
    void TryFindCircuit()
    {
        _circuit = null; _nodePos = null; _nodeNeg = null;

        // Construir lista: primero el CM que usa GameManager, luego el resto
        var ordered = new List<CircuitManager>();
        var gm = FindAnyObjectByType<GameManager>();
        if (gm?.circuit != null) ordered.Add(gm.circuit);

        foreach (var cm in FindObjectsByType<CircuitManager>(FindObjectsInactive.Include))
            if (!ordered.Contains(cm)) ordered.Add(cm);

        foreach (var cm in ordered)
        {
            // Asegurar que el CM tiene componentes (puede estar recién activado)
            if (cm.components.Count == 0) cm.AutoDetectComponents();

            var vs = cm.FindCircuitComponent<VoltageSource>();
            if (vs == null || vs.nodeA == null || vs.nodeB == null) continue;

            _circuit = cm;
            _nodePos  = vs.nodeA;
            _nodeNeg  = vs.nodeB;

            _lastStatus = $"CM: {cm.name}  |  Fuente: {vs.voltage:F1}V\n" +
                          $"Nodo+: {_nodePos.name}  Nodo–: {_nodeNeg.name}\n" +
                          "Listo — [1] Rojo  [2] Negro  [S] Simular";

            Debug.Log($"[DebugHelper] Circuito OK → '{cm.name}', " +
                      $"VoltageSource={vs.voltage}V, " +
                      $"nodeA='{vs.nodeA.name}', nodeB='{vs.nodeB.name}'");
            return;
        }

        _lastStatus = "Sin CM con VoltageSource+nodos. Reintentando…";
        Debug.LogWarning("[DebugHelper] No se encontró CircuitManager con VoltageSource y nodos asignados.");
    }

    // ─────────────────────────────────────────────
    //  Acciones
    // ─────────────────────────────────────────────

    void ForceSimulate()
    {
        if (_circuit == null) { TryFindCircuit(); return; }
        if (_circuit.components.Count == 0) _circuit.AutoDetectComponents();
        _circuit.ForceSimulate();
        string v = _nodePos != null ? $"{_nodePos.voltage:F2}V" : "—";
        Debug.Log($"[DebugHelper] Re-simulado → nodo+={v}");
    }

    void AssignProbe(bool red)
    {
        // Si los nodos aún no tienen voltaje, forzar simulación primero
        if (_nodePos != null && _nodePos.voltage == 0f) ForceSimulate();

        ElectricalNode node = red ? _nodePos : _nodeNeg;
        if (node == null)
        {
            Debug.LogWarning($"[DebugHelper] Nodo {(red ? "+" : "–")} es null. Presiona R para buscar el CM.");
            return;
        }

        if (red) _multimeter.SetRedNode(node);
        else     _multimeter.SetBlackNode(node);

        Debug.Log($"[DebugHelper] Punta {(red ? "ROJA(+)" : "NEGRA(–)")} asignada → " +
                  $"'{node.name}' = {node.voltage:F2}V");
    }

    void ResetAll()
    {
        _multimeter.ResetProbes();
        Debug.Log("[DebugHelper] Puntas reiniciadas.");
    }

    void PrintDiagnostic()
    {
        if (_multimeter == null) { Debug.LogWarning("[DebugHelper] Sin Multimeter."); return; }
        Debug.Log(
            $"[DebugHelper] === DIAGNÓSTICO ===\n" +
            $"  Multimeter:      {_multimeter.gameObject.name}\n" +
            $"  isReading:       {_multimeter.isReading}\n" +
            $"  measuredVoltage: {_multimeter.measuredVoltage:F3} V\n" +
            $"  measuredCurrent: {_multimeter.measuredCurrent * 1000f:F2} mA\n" +
            $"  CircuitManager:  {(_circuit != null ? _circuit.name : "NULL")}\n" +
            $"  Componentes:     {_circuit?.components?.Count ?? 0}\n" +
            $"  sourceVoltage:   {_circuit?.sourceVoltage:F2} V\n" +
            $"  shortCircuit:    {_circuit?.isShortCircuited}\n" +
            $"  Nodo+:           {(_nodePos != null ? $"{_nodePos.name} = {_nodePos.voltage:F3}V" : "NULL")}\n" +
            $"  Nodo–:           {(_nodeNeg != null ? $"{_nodeNeg.name} = {_nodeNeg.voltage:F3}V" : "NULL")}"
        );
    }

    // ─────────────────────────────────────────────
    //  Panel en pantalla
    // ─────────────────────────────────────────────
    void OnGUI()
    {
        if (!showGui || _multimeter == null) return;

        const float W = 340f, H = 220f, PAD = 8f;
        var area = new Rect(PAD, PAD, W, H);

        // Fondo semitransparente
        GUI.color = new Color(0f, 0f, 0f, 0.72f);
        GUI.DrawTexture(area, Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUILayout.BeginArea(new Rect(PAD + 6, PAD + 6, W - 12, H - 12));

        GUIStyle title = new GUIStyle(GUI.skin.label)
            { fontStyle = FontStyle.Bold, fontSize = 13, normal = { textColor = Color.cyan } };
        GUIStyle val = new GUIStyle(GUI.skin.label)
            { fontSize = 12, normal = { textColor = Color.white } };
        GUIStyle warn = new GUIStyle(GUI.skin.label)
            { fontSize = 11, normal = { textColor = Color.yellow } };

        GUILayout.Label("MULTÍMETRO DEBUG", title);
        GUILayout.Space(2);

        // Estado medición
        bool reading = _multimeter.isReading;
        float voltage = _multimeter.measuredVoltage;
        float current = _multimeter.measuredCurrent * 1000f;

        GUI.color = reading ? Color.green : Color.gray;
        GUILayout.Label(reading
            ? $"MIDIENDO  →  {FormatV(voltage)}  /  {current:F1} mA"
            : "SIN CONTACTO (ambas puntas sin asignar)", val);
        GUI.color = Color.white;

        GUILayout.Space(4);

        // Nodos actuales
        float vPos = _nodePos != null ? _nodePos.voltage : float.NaN;
        float vNeg = _nodeNeg != null ? _nodeNeg.voltage : float.NaN;

        bool nodesOk = _nodePos != null && _nodeNeg != null;
        GUI.color = nodesOk ? (vPos > 0.01f ? Color.white : Color.yellow) : Color.red;
        GUILayout.Label(nodesOk
            ? $"Nodo+: {_nodePos.name} = {vPos:F2}V  |  Nodo–: {_nodeNeg.name} = {vNeg:F2}V"
            : "Nodo+ / Nodo– sin asignar — presiona [R]", warn);
        GUI.color = Color.white;

        GUILayout.Space(4);

        // Prueba rápida de las sondas
        float redV  = _multimeter.probeA != null ? _multimeter.probeA.voltage : float.NaN;
        float blackV = _multimeter.probeB != null ? _multimeter.probeB.voltage : float.NaN;
        GUILayout.Label(
            $"Sonda roja:  {(_multimeter.probeA != null ? $"{_multimeter.probeA.name} = {redV:F2}V" : "—")}",
            val);
        GUILayout.Label(
            $"Sonda negra: {(_multimeter.probeB != null ? $"{_multimeter.probeB.name} = {blackV:F2}V" : "—")}",
            val);

        GUILayout.Space(6);
        GUILayout.Label("[1] Rojo(+)  [2] Negro(–)  [0] Reset  [S] Simular  [R] Rebuscar  [I] Info",
            new GUIStyle(GUI.skin.label) { fontSize = 10, normal = { textColor = new Color(0.7f,0.7f,0.7f) } });

        GUILayout.EndArea();
    }

    static string FormatV(float v) =>
        Mathf.Abs(v) >= 1f ? $"{v:F2} V" : $"{v * 1000f:F1} mV";
}
