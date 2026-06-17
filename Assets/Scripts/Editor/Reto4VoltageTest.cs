using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Verificación HEADLESS del Reto 4: ¿el Arduino genera voltaje y el circuito
/// Pin → R(≥100Ω) → LED → GND conduce de forma segura?
///
/// No necesita VR ni Play Mode: arma el circuito en memoria (GameObjects inactivos,
/// para no disparar Awake/OnEnable), hace que ArduinoCore genere 5 V con DigitalWrite,
/// resuelve el MNA real (CircuitGraphAnalyzer.SolveMNA) y comprueba V, I y polaridad.
///
/// Ejecutar:
///   Editor:     Tools → TITA → Reto 4 → Test de voltaje (headless)
///   Batch mode: Unity.exe -batchmode -quit -projectPath . -executeMethod Reto4VoltageTest.Run -logFile -
/// </summary>
public static class Reto4VoltageTest
{
    [MenuItem("Tools/TITA/Reto 4/Test de voltaje (headless)")]
    public static void Run()
    {
        int fails = 0;
        Debug.Log("===== RETO 4 — TEST DE VOLTAJE DEL ARDUINO =====");

        // ── Nodos eléctricos ──────────────────────────────────────────────
        var pin = NewNode("PinD13");
        var mid = NewNode("Mid");
        var gnd = NewNode("GND");

        // ── Arduino (GO inactivo: evita OnEnable + StartCoroutine en edit mode) ──
        var goA = new GameObject("ArduinoTest"); goA.SetActive(false);
        var core = goA.AddComponent<ArduinoCore>();
        core.nodoP13         = pin;
        core.nodoGND         = gnd;
        core.activePinNumber = 13;
        core.outputVoltageTTL = 5f;
        core.activePinMode   = PinMode.OUTPUT;
        core.blinkEnabled    = true;

        // ── [1] El Arduino genera voltaje ─────────────────────────────────
        core.DigitalWrite(13, true);
        Debug.Log($"[1] HIGH → OutputVoltage = {core.OutputVoltage:F2} V | nodo pin = {pin.voltage:F2} V");
        if (!Aprox(core.OutputVoltage, 5f)) { fails++; Debug.LogError("FALLO: el Arduino NO genera 5 V en HIGH."); }
        if (!Aprox(pin.voltage, 5f))        { fails++; Debug.LogError("FALLO: el nodo del pin no quedó a 5 V."); }

        core.DigitalWrite(13, false);
        Debug.Log($"[1b] LOW → OutputVoltage = {core.OutputVoltage:F2} V");
        if (!Aprox(core.OutputVoltage, 0f)) { fails++; Debug.LogError("FALLO: el Arduino no baja a 0 V en LOW."); }

        // ── Circuito: Pin --R(330)-- mid --LED(Vf=2, 50Ω)-- GND ───────────
        var r = NewComp<Resistor>("R330");
        r.resistance = 330f; r.nodeA = pin; r.nodeB = mid;

        var led = NewComp<LED>("LED");
        led.forwardVoltage = 2f; led.resistance = 50f; led.maxSafeCurrent = 0.02f;
        led.polarityInverted = false; led.nodeA = mid; led.nodeB = gnd;

        var comps = new List<ElectricalComponent> { r, led };

        // ── [2] MNA con 5 V en el pin (LED en polaridad correcta) ─────────
        bool solved = CircuitGraphAnalyzer.SolveMNA(comps, pin, 5f, gnd);
        led.ApplyResolvedCurrent();
        float vR  = Mathf.Abs(pin.voltage - mid.voltage);
        float vL  = Mathf.Abs(mid.voltage - gnd.voltage);
        float mA  = Mathf.Abs(r.current) * 1000f;
        Debug.Log($"[2] MNA={solved} | I={mA:F2} mA | V_R={vR:F2} V | V_LED={vL:F2} V | " +
                  $"LED.isOn={led.isOn} | estado={led.state}");
        if (!solved)                                   { fails++; Debug.LogError("FALLO: el MNA no resolvió el circuito."); }
        if (mA < 1f)                                   { fails++; Debug.LogError("FALLO: no circula corriente (LED apagado)."); }
        if (mA > led.maxSafeCurrent * 1000f + 0.5f)    { fails++; Debug.LogError("FALLO: corriente sobre el límite seguro."); }
        if (!led.isOn)                                 { fails++; Debug.LogError("FALLO: el LED no encendió."); }

        // ── [3] LED invertido → el diodo NO debe conducir ─────────────────
        led.polarityInverted = true;
        bool solved2 = CircuitGraphAnalyzer.SolveMNA(comps, pin, 5f, gnd);
        led.ApplyResolvedCurrent();
        float mInv = Mathf.Abs(r.current) * 1000f;
        Debug.Log($"[3] LED invertido → MNA={solved2} | I={mInv:F3} mA | LED.isOn={led.isOn}  (esperado ≈0 mA, apagado)");
        if (mInv > 0.5f) { fails++; Debug.LogError("FALLO: el LED invertido sigue conduciendo."); }

        // ── Resultado ─────────────────────────────────────────────────────
        Debug.Log(fails == 0
            ? "===== RESULTADO: ✓ OK — el Arduino genera voltaje y el circuito del Reto 4 conduce y es seguro ====="
            : $"===== RESULTADO: ✗ {fails} FALLO(S) — revisa los errores de arriba =====");

        // limpieza
        Object.DestroyImmediate(goA);
        Kill(pin); Kill(mid); Kill(gnd);
        Kill(r); Kill(led);

        if (Application.isBatchMode) EditorApplication.Exit(fails == 0 ? 0 : 1);
    }

    static ElectricalNode NewNode(string n)
    {
        var go = new GameObject(n); go.SetActive(false);
        return go.AddComponent<ElectricalNode>();
    }

    static T NewComp<T>(string n) where T : Component
    {
        var go = new GameObject(n); go.SetActive(false);
        return go.AddComponent<T>();
    }

    static void Kill(Component c) { if (c != null) Object.DestroyImmediate(c.gameObject); }
    static bool Aprox(float a, float b) => Mathf.Abs(a - b) < 0.01f;
}
