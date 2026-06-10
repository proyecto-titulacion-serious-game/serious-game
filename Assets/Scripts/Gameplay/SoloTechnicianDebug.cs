using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// AYUDA DE PRUEBA SOLO (offline, sin Técnico).
///
/// Tecla <b>F8</b> = "resolver el reto actual": aplica la reparación correcta a las piezas con
/// falla, como si el Técnico hubiera entregado el valor perfecto. Permite verificar la condición
/// de victoria de cada reto sin un segundo jugador (el token físico siempre vale 100Ω, así que
/// solo nunca se podía alimentar el valor correcto — ej. 850Ω del Reto 1).
///
/// Tecla <b>F9</b> = "simular entrega correcta por la RUTA REAL" (Reto 1 y 3): busca el resistor
/// con falla y lo repara pasando por <c>ComponentDeliverySystem.ValidateValueForRepair</c> →
/// <c>BuscarResistorDelReto</c> → <c>ApplyRepairToCircuit</c>. A diferencia de F8 (que llama
/// <c>Repair()</c> directo), F9 SÍ ejercita esa validación, por lo que sirve para probar sin VR el
/// fix de la entrega. Log "REPARADO ✅" = fix OK; "RECHAZADO ❌" = la validación está rota.
///
/// Auto-bootstrap: no requiere ponerlo en ninguna escena. Solo compila/corre en Editor o
/// Development Build, así que NO se va en un build de release.
///
///   Reto 1 (Ohm)    → repara el resistor con falla a su correctResistance (850Ω).
///   Reto 2 (Paral.) → restaura la rama del LED rota (open / >1000Ω) a 50Ω.
///   Reto 3 (Mixto)  → repara el resistor (220Ω) + endereza LED y capacitor invertidos.
///   Reto 4 (Arduino)→ es sandbox: lo armas tú (Pin→R≥100Ω→LED→GND + BLINK).
/// </summary>
public class SoloTechnicianDebug : MonoBehaviour
{
    static SoloTechnicianDebug _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (_instance != null) return;
        var go = new GameObject("[SoloTechnicianDebug]");
        _instance = go.AddComponent<SoloTechnicianDebug>();
        DontDestroyOnLoad(go);
#endif
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.f8Key.wasPressedThisFrame) ResolverRetoActual();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // F9 = simular la ENTREGA correcta del Técnico por la RUTA REAL (valida el fix de
        // BuscarResistorDelReto/ValidateValueForRepair, que F8 se salta al llamar Repair() directo).
        if (kb.f9Key.wasPressedThisFrame) SimularEntregaCorrecta();
#endif
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // ─────────────────────────────────────────────
    //  F9 — entrega correcta por la ruta REAL (prueba del fix sin VR)
    // ─────────────────────────────────────────────
    void SimularEntregaCorrecta()
    {
        var gm = FindAnyObjectByType<GameManager>();
        if (gm == null)
        {
            if (!_avisoSinGM) { Debug.LogWarning("[SoloDebug][F9] No hay GameManager en escena."); _avisoSinGM = true; }
            return;
        }
        _avisoSinGM = false;

        var delivery = FindAnyObjectByType<ComponentDeliverySystem>();
        if (delivery == null)
        {
            Debug.LogWarning("[SoloDebug][F9] No hay ComponentDeliverySystem en escena (¿estás en Explorador?).");
            return;
        }

        switch (gm.currentLevel)
        {
            case LevelType.OhmLaw:
            case LevelType.Mixed:
            {
                // Resistor con falla, cableado y activo (el del reto actual, gracias a la zona activa).
                Resistor faulty = null;
                foreach (var r in FindObjectsByType<Resistor>(FindObjectsInactive.Exclude))
                    if (r != null && r.nodeA != null && r.nodeB != null && r.hasFault) { faulty = r; break; }

                if (faulty == null)
                {
                    Debug.LogWarning("[SoloDebug][F9] No hay resistor con falla activo (¿ya reparado o sin zona activa?).");
                    return;
                }

                bool ok = delivery.DebugSimularEntregaEInstalacion(ComponentType.Resistor, faulty.correctResistance);
                Debug.Log($"[SoloDebug][F9] Entrega resistor {faulty.correctResistance}Ω por ruta real → " +
                          $"{(ok ? "REPARADO ✅ (fix OK)" : "RECHAZADO ❌ (revisar BuscarResistorDelReto)")}");
                break;
            }
            default:
                Debug.Log("[SoloDebug][F9] F9 prueba la entrega de resistor (Reto 1/3). Reto 2 → usa F8; Reto 4 es sandbox.");
                break;
        }
    }
#endif

    bool _avisoSinGM;

    void ResolverRetoActual()
    {
        var gm = FindAnyObjectByType<GameManager>();
        if (gm == null)
        {
            // Escena sin GameManager (p. ej. Explorador offline): avisar una sola vez, no en cada F8.
            if (!_avisoSinGM) { Debug.LogWarning("[SoloDebug] No hay GameManager en escena (F8 no aplica aquí)."); _avisoSinGM = true; }
            return;
        }
        _avisoSinGM = false;

        switch (gm.currentLevel)
        {
            case LevelType.OhmLaw:
                FixResistorFault();
                break;
            case LevelType.Parallel:
                FixBrokenLedBranch();
                break;
            case LevelType.Mixed:
                FixResistorFault();
                FixInvertedLed();
                FixInvertedCap();
                break;
            case LevelType.Arduino:
                Debug.Log("[SoloDebug] Reto 4 es sandbox: ármalo a mano (Pin → R≥100Ω → LED → GND + código BLINK).");
                return;
        }

        Resimular(gm);
        gm.RegisterRepairAction();   // por si la condición de victoria lo requiere
        Debug.Log($"[SoloDebug] ✅ Reto {(int)gm.currentLevel + 1} resuelto (modo solo). Si todo está bien, el LED se pone verde y el reto se completa.");
    }

    // ─────────────────────────────────────────────
    //  Reparaciones (mismo efecto que la entrega correcta del Técnico)
    // ─────────────────────────────────────────────
    void FixResistorFault()
    {
        foreach (var r in FindObjectsByType<Resistor>(FindObjectsInactive.Exclude))
            if (r != null && r.hasFault)
            {
                r.Repair(); // resistance = correctResistance, hasFault = false
                r.powerRatingWatts = Mathf.Max(r.powerRatingWatts, 1f);
                Debug.Log($"[SoloDebug] Resistor '{r.name}' reparado → {r.resistance}Ω");
            }
    }

    void FixBrokenLedBranch()
    {
        foreach (var led in FindObjectsByType<LED>(FindObjectsInactive.Exclude))
        {
            if (led == null || led.nodeA == null || led.nodeB == null) continue;
            if (led.isOpenCircuit || led.resistance > 1000f)
            {
                led.isOpenCircuit = false;
                led.resistance    = 50f; // resistencia normal de rama
                Debug.Log($"[SoloDebug] Rama rota restaurada en LED '{led.name}' (50Ω).");
            }
        }
    }

    void FixInvertedLed()
    {
        foreach (var led in FindObjectsByType<LED>(FindObjectsInactive.Exclude))
            if (led != null && led.nodeA != null && led.polarityInverted)
            {
                led.polarityInverted = false;
                Debug.Log($"[SoloDebug] LED '{led.name}' polaridad corregida.");
            }
    }

    void FixInvertedCap()
    {
        foreach (var cap in FindObjectsByType<Capacitor>(FindObjectsInactive.Exclude))
            if (cap != null && cap.polarityInverted)
            {
                cap.polarityInverted = false;
                Debug.Log($"[SoloDebug] Capacitor '{cap.name}' polaridad corregido.");
            }
    }

    void Resimular(GameManager gm)
    {
        if (gm.circuit != null) gm.circuit.MarkDirty();
        foreach (var cm in FindObjectsByType<CircuitManager>(FindObjectsInactive.Exclude))
        {
            if (cm == null) continue;
            cm.MarkDirty();
            cm.ForceSimulate(); // recalcula ya y dispara OnCircuitChanged (LED, multímetro, auto-check)
        }
        if (gm.protoSim != null) gm.protoSim.MarkDirty();
    }
}
