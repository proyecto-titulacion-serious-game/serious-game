using UnityEngine;

/// <summary>
/// Reproduce chispas cuando el CircuitManager detecta cortocircuito.
/// Colocar en el mismo GameObject que el componente causante del corto
/// (p.ej. el Capacitor con polaridad invertida, o el Resistor con R=0).
///
/// SETUP en Inspector:
///   1. Asignar un ParticleSystem de chispas en el campo 'sparkEffect'.
///   2. (Opcional) Asignar el CircuitManager; si se deja null lo busca en el padre.
/// </summary>
[DisallowMultipleComponent]
public class ShortCircuitSparkEffect : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Efecto visual")]
    [Tooltip("ParticleSystem de chispas. Debe estar en la misma posición que el componente.")]
    public ParticleSystem sparkEffect;

    [Tooltip("Intensidad visual: multiplica la tasa de emisión base del sistema de partículas.")]
    [Range(0.1f, 5f)]
    public float intensityMultiplier = 1f;

    [Header("Referencias")]
    [Tooltip("CircuitManager de esta zona. Si es null, busca en los padres automáticamente.")]
    public CircuitManager circuitManager;

    // ─────────────────────────────────────────────
    //  Estado interno
    // ─────────────────────────────────────────────
    private bool  _wasShorted = false;
    private float _baseEmissionRate = 30f;
    // Motor del Reto 4 (sandbox). Los retos 1-3 usan CircuitManager; el Reto 4 usa
    // ProtoboardSimulator, que dispara su PROPIO OnCircuitChanged. Sin esto no había chispas en VR.
    private ProtoboardSimulator _protoSim;

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────
    void Awake()
    {
        if (circuitManager == null)
            circuitManager = GetComponentInParent<CircuitManager>(true);
        if (_protoSim == null)
            _protoSim = GetComponentInParent<ProtoboardSimulator>(true) ?? FindAnyObjectByType<ProtoboardSimulator>();

        if (sparkEffect != null)
        {
            var emission = sparkEffect.emission;
            _baseEmissionRate = emission.rateOverTime.constant;
            sparkEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    void OnEnable()
    {
        CircuitManager.OnCircuitChanged      += OnCircuitChanged;
        ProtoboardSimulator.OnCircuitChanged += OnCircuitChanged;
    }

    void OnDisable()
    {
        CircuitManager.OnCircuitChanged      -= OnCircuitChanged;
        ProtoboardSimulator.OnCircuitChanged -= OnCircuitChanged;
        StopSparks();
    }

    // ─────────────────────────────────────────────
    //  Lógica principal
    // ─────────────────────────────────────────────
    void OnCircuitChanged()
    {
        if (sparkEffect == null) return;

        // Cortocircuito según el motor presente: CircuitManager (retos 1-3) o ProtoboardSimulator (Reto 4).
        bool shortedNow = (circuitManager != null && circuitManager.isShortCircuited)
                       || (_protoSim      != null && _protoSim.isShortCircuited);

        if (shortedNow && !_wasShorted)
            PlaySparks();
        else if (!shortedNow && _wasShorted)
            StopSparks();

        _wasShorted = shortedNow;
    }

    void PlaySparks()
    {
        if (sparkEffect == null) return;

        var emission = sparkEffect.emission;
        emission.rateOverTime = _baseEmissionRate * intensityMultiplier;

        if (!sparkEffect.isPlaying)
            sparkEffect.Play();
    }

    void StopSparks()
    {
        if (sparkEffect == null || !sparkEffect.isPlaying) return;
        sparkEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    // ─────────────────────────────────────────────
    //  API pública
    // ─────────────────────────────────────────────
    /// <summary>Fuerza las chispas desde código (sin esperar la detección de cortocircuito).</summary>
    public void ForcePlay()  => PlaySparks();

    /// <summary>Detiene las chispas manualmente.</summary>
    public void ForceStop()  => StopSparks();
}
