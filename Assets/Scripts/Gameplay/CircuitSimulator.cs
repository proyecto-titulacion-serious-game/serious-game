using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Motor central de simulación analítica para los retos del juego.
/// Calcula de forma dinámica voltajes, corrientes y estados de falla.
/// VERSIÓN INTEGRAL: Incluye puentes de compatibilidad para el Técnico y sistemas heredados.
/// </summary>
public class CircuitSimulator : MonoBehaviour
{
    [Header("Matriz de Trabajo")]
    public List<ComponentSlot> todosLosSlots = new List<ComponentSlot>();

    private bool        _isDirty = true;
    private GameManager _cachedGM;

    // ─────────────────────────────────────────────
    //  PUENTES DE COMPATIBILIDAD TYPECAST (Fix CS0029 / CS1503)
    // ─────────────────────────────────────────────
    // Este operador maestro engaña a Unity convirtiendo automáticamente un 'CircuitSimulator' 
    // en un 'CircuitManager' o viceversa si un script viejo lo solicita.
    public static implicit operator CircuitManager(CircuitSimulator instance)
    {
        if (instance == null) return null;
        
        // Intentamos retornar el componente CircuitManager si coexiste en el mismo objeto
        CircuitManager cm = instance.GetComponent<CircuitManager>();
        if (cm == null) cm = instance.gameObject.AddComponent<CircuitManager>();
        return cm;
    }

    // ─────────────────────────────────────────────
    //  PUENTES DE VARIABLES ELÉCTRICAS (Fix CS1061)
    // ─────────────────────────────────────────────
    [Header("Variables Globales de Red (Telemetría para Técnico)")]
    public float sourceVoltage = 9f;    // Voltaje de la fuente (V) — Usado por ObjectiveSystem
    public float totalCurrent = 0.02f;  // Amperaje total (I) — Usado por ComponentSendingTray y ObjectiveSystem
    public float totalPower = 0.18f;    // Potencia de la malla (W) — Usado por ComponentSendingTray
    public bool isShortCircuited = false; // Estado de cortocircuito — Usado por CircuitAudioManager y Técnico

    private List<ElectricalComponent> _legacyComponents = new List<ElectricalComponent>();
    
    /// <summary> Propiedad puente para simular la lista antigua de componentes. </summary>
    public List<ElectricalComponent> components
    {
        get
        {
            _legacyComponents.Clear();
            foreach (var slot in todosLosSlots)
            {
                if (slot != null && slot.InstalledObject != null)
                {
                    if (slot.InstalledObject.TryGetComponent<ElectricalComponent>(out var comp))
                    {
                        _legacyComponents.Add(comp);
                    }
                }
            }
            return _legacyComponents;
        }
    }

    public void AutoDetectComponents()
    {
        // En el nuevo sistema Sandbox, el escaneo es automático por la matriz de slots
        MarkDirty();
    }

    // ─────────────────────────────────────────────
    //  Unity Lifecycle y Advertencias (Fix CS0618)
    // ─────────────────────────────────────────────
    void Awake()
    {
        // FIX: Se removió FindObjectsSortMode que estaba obsoleto en las nuevas versiones de Unity
        if (todosLosSlots.Count == 0)
        {
            todosLosSlots.AddRange(FindObjectsByType<ComponentSlot>(FindObjectsInactive.Include));
        }
    }

    /// <summary>
    /// Marca el circuito para indicar que requiere un recálculo matemático.
    /// </summary>
    public void MarkDirty() => _isDirty = true;

    /// <summary>
    /// Ejecuta el análisis de parámetros eléctricos basándose en las leyes de la electrónica.
    /// </summary>
    public void ForceSimulate()
    {
        if (!_isDirty) return;
        _isDirty = false;

        Debug.Log("[CircuitSimulator] Actualizando análisis nodal y cálculo de mallas...");

        // Paso 1: Inicializar telemetría base de la simulación
        isShortCircuited = false;
        totalCurrent = 0f;
        totalPower = 0f;

        // Resetear estados eléctricos básicos
        foreach (var slot in todosLosSlots)
        {
            if (slot == null || slot.InstalledObject == null) continue;

            if (slot.InstalledObject.TryGetComponent<LED>(out var led))
            {
                led.isOn = false;
                if (led.nodeA != null && led.nodeB != null)
                {
                    led.nodeA.voltage = 0f;
                    led.nodeB.voltage = 0f;
                    led.Calculate();
                }
            }
        }

        // Paso 2: Resolver la matriz según el tipo de reto activo
        if (_cachedGM == null) _cachedGM = FindAnyObjectByType<GameManager>();
        GameManager gm = _cachedGM;
        if (gm == null) return;

        switch (gm.currentLevel)
        {
            case LevelType.OhmLaw:
                SimularReto1_Ohm(gm);
                break;
            case LevelType.Parallel:
                SimularReto2_Parallel();
                break;
            case LevelType.Mixed:
                SimularReto3_Mixed();
                break;
            case LevelType.Arduino:
                SimularReto4_Arduino();
                break;
        }
    }

    private void SimularReto1_Ohm(GameManager gm)
    {
        sourceVoltage = 9f;
        ComponentSlot slotR   = todosLosSlots.Find(s => s.targetElectricalValue == 850f);

        // Buscar LED pre-instalado en cualquier slot del circuito
        LED           led     = null;
        foreach (var slot in todosLosSlots)
        {
            if (slot?.InstalledObject == null) continue;
            if (slot.InstalledObject.TryGetComponent<LED>(out var l)) { led = l; break; }
        }

        // Sin resistencia instalada → LED ya fue apagado en el paso de reset de ForceSimulate
        if (slotR?.InstalledObject == null) return;
        if (!slotR.InstalledObject.TryGetComponent<Resistor>(out var res)) return;
        if (res.nodeA == null || res.nodeB == null) return;

        // ── Circuito serie correcto: V_src → Resistor → LED → GND ──────────
        float rRes   = Mathf.Max(res.resistance, 0.001f);
        float rLed   = (led != null) ? led.GetResistance() : 0f;
        float rTotal = rRes + rLed;

        if (rTotal < 0.1f) { isShortCircuited = true; return; }

        float I = sourceVoltage / rTotal;

        // Nodo de unión R-LED: voltaje después del resistor
        float vJuncion = sourceVoltage - I * rRes;

        // Voltajes y cálculo del Resistor
        res.nodeA.voltage = sourceVoltage;
        res.nodeB.voltage = vJuncion;
        res.Calculate();

        // Telemetría
        totalCurrent     = I;
        totalPower       = res.dissipatedPower;
        isShortCircuited = res.isOverloaded;

        // Voltajes y cálculo del LED (FIX principal: antes nunca se actualizaba)
        if (led != null && led.nodeA != null && led.nodeB != null)
        {
            led.nodeA.voltage = vJuncion;
            led.nodeB.voltage = 0f;
            led.Calculate();
        }
    }

    private void SimularReto2_Parallel()
    {
        sourceVoltage = 5f;
        foreach (var slot in todosLosSlots)
        {
            if (slot != null && slot.InstalledObject != null)
            {
                if (slot.InstalledObject.TryGetComponent<LED>(out var led))
                {
                    if (led.nodeA != null && led.nodeB != null)
                    {
                        led.nodeA.voltage = led.polarityInverted ? 0f : sourceVoltage;
                        led.nodeB.voltage = led.polarityInverted ? sourceVoltage : 0f;
                        led.Calculate();

                        totalCurrent += led.current;
                    }
                }
            }
        }
        totalPower = sourceVoltage * totalCurrent;
    }

    private void SimularReto3_Mixed()
    {
        sourceVoltage = 6f;
        foreach (var slot in todosLosSlots)
        {
            if (slot != null && slot.InstalledObject != null)
            {
                if (slot.InstalledObject.TryGetComponent<Resistor>(out var res))
                {
                    if (res.nodeA != null && res.nodeB != null)
                    {
                        res.nodeA.voltage = sourceVoltage;
                        res.nodeB.voltage = 2f;
                        res.Calculate();
                        totalCurrent += res.current;
                    }
                }
            }
        }
        totalPower = sourceVoltage * totalCurrent;
    }

    private void SimularReto4_Arduino()
    {
        ArduinoCore arduino = FindAnyObjectByType<ArduinoCore>();
        if (arduino == null) return;

        sourceVoltage = arduino.outputVoltageTTL;

        Resistor resistenciaProteccion = null;
        LED ledSalida = null;

        foreach (var slot in todosLosSlots)
        {
            if (slot == null || slot.InstalledObject == null) continue;

            if (slot.InstalledObject.TryGetComponent<Resistor>(out var res)) resistenciaProteccion = res;
            if (slot.InstalledObject.TryGetComponent<LED>(out var led)) ledSalida = led;
        }

        if (resistenciaProteccion != null && ledSalida != null)
        {
            resistenciaProteccion.nodeA = arduino.pin13Node; 
            
            if (ledSalida.polarityInverted)
            {
                ledSalida.nodeB = resistenciaProteccion.nodeB;
                ledSalida.nodeA = arduino.gndNode; 
            }
            else
            {
                ledSalida.nodeA = resistenciaProteccion.nodeB;
                ledSalida.nodeB = arduino.gndNode; 
            }

            resistenciaProteccion.Calculate();
            ledSalida.Calculate();

            totalCurrent = resistenciaProteccion.current;
            totalPower = resistenciaProteccion.dissipatedPower;
        }
    }

    public bool AreAllLEDsOn()
    {
        int ledsEncendidos = 0;
        int ledsTotales = 0;

        foreach (var slot in todosLosSlots)
        {
            if (slot != null && slot.InstalledObject != null && slot.InstalledObject.TryGetComponent<LED>(out var led))
            {
                ledsTotales++;
                if (led.isOn && led.state == LEDState.Correct) ledsEncendidos++;
            }
        }
        return ledsTotales > 0 && ledsEncendidos == ledsTotales;
    }
}