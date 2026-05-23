using System.Collections;
using UnityEngine;

/// <summary>
/// Muestra humo en un componente dañado cuando el circuito está energizado.
/// La emisión sube gradualmente (fadeInDuration) al encender el switch,
/// y se detiene de inmediato al apagarlo o reparar el fallo.
///
/// SETUP:
///   1. Crear GO hijo "Smoke_VFX" con un ParticleSystem configurado como humo.
///   2. Añadir este script al GO del componente (Resistor, LED, ArduinoPin, VoltageSource).
///   3. Asignar smokeEffect y, opcionalmente, circuitManager (si null busca en padres).
///
/// Nota: Capacitor maneja su propio humo internamente — no necesita este script.
/// </summary>
[DisallowMultipleComponent]
public class ComponentSmokeEffect : MonoBehaviour
{
    [Header("Efecto visual")]
    [Tooltip("ParticleSystem de humo. Debe estar posicionado sobre el componente.")]
    public ParticleSystem smokeEffect;

    [Header("Fade-in")]
    [Tooltip("Segundos que tarda la emisión en llegar a su tasa máxima al encender.")]
    [Range(0.5f, 5f)]
    public float fadeInDuration = 2f;

    [Header("Referencias")]
    [Tooltip("CircuitManager de la escena. Si es null, busca en los padres automáticamente.")]
    public CircuitManager circuitManager;

    // ─────────────────────────────────────────────
    private Resistor            _resistor;
    private LED                 _led;
    private VoltageSource       _source;
    private ArduinoPin          _pin;
    private ElectricalComponent _ec;

    private float    _baseEmissionRate;
    private bool     _smokingActive;
    private Coroutine _fadeCoroutine;

    // ─────────────────────────────────────────────
    void Awake()
    {
        _ec       = GetComponent<ElectricalComponent>();
        _resistor = GetComponent<Resistor>();
        _led      = GetComponent<LED>();
        _source   = GetComponent<VoltageSource>();
        _pin      = GetComponent<ArduinoPin>();

        if (circuitManager == null)
            circuitManager = GetComponentInParent<CircuitManager>(true);

        if (smokeEffect != null)
        {
            _baseEmissionRate = smokeEffect.emission.rateOverTime.constant;
            smokeEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    void OnEnable()  => CircuitManager.OnCircuitChanged += Refresh;

    void OnDisable()
    {
        CircuitManager.OnCircuitChanged -= Refresh;
        StopSmoke();
    }

    // ─────────────────────────────────────────────
    //  Lógica principal
    // ─────────────────────────────────────────────
    void Refresh()
    {
        if (smokeEffect == null || circuitManager == null) return;

        bool shouldSmoke = circuitManager.sourceVoltage > 0f && IsDamaged();

        if (shouldSmoke && !_smokingActive)
        {
            _smokingActive = true;
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeInSmoke());
        }
        else if (!shouldSmoke && _smokingActive)
        {
            StopSmoke();
        }
    }

    bool IsDamaged()
    {
        if (_ec       != null && _ec.isOpenCircuit)                     return true;
        if (_resistor != null && _resistor.hasFault)                    return true;
        if (_led      != null && _led.polarityInverted)                 return true;
        if (_source   != null && _source.hasFault)                      return true;
        if (_pin      != null && (_pin.hasFault || _pin.hasLooseCable)) return true;
        return false;
    }

    // ─────────────────────────────────────────────
    //  Corrutina de fade-in
    // ─────────────────────────────────────────────
    IEnumerator FadeInSmoke()
    {
        var emission = smokeEffect.emission;
        emission.rateOverTime = 0f;
        smokeEffect.Play();

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            if (this == null || smokeEffect == null) yield break;
            elapsed += Time.deltaTime;
            emission.rateOverTime = Mathf.Lerp(0f, _baseEmissionRate, elapsed / fadeInDuration);
            yield return null;
        }

        emission.rateOverTime = _baseEmissionRate;
        _fadeCoroutine = null;
    }

    void StopSmoke()
    {
        _smokingActive = false;
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = null;
        }
        smokeEffect?.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    // ─────────────────────────────────────────────
    //  API pública (para testing o cutscenes)
    // ─────────────────────────────────────────────
    public void ForcePlay()
    {
        if (_smokingActive) return;
        _smokingActive = true;
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeInSmoke());
    }

    public void ForceStop() => StopSmoke();
}
