using System.Collections;
using UnityEngine;

/// <summary>
/// Muestra humo en un componente dañado cuando el circuito está energizado.
/// Si no se asigna smokeEffect en el Inspector, crea uno por código automáticamente.
///
/// SETUP MÍNIMO:
///   Añadir este script al GO del componente eléctrico (Resistor, LED, Capacitor, ArduinoPin).
///   Nada más es necesario — el ParticleSystem se genera solo si queda vacío.
///
/// SETUP AVANZADO (aspecto personalizado):
///   Asignar un ParticleSystem ya configurado en el campo smokeEffect.
///   Asignar un Material de partículas URP en smokeMaterial para evitar el color magenta.
/// </summary>
[DisallowMultipleComponent]
public class ComponentSmokeEffect : MonoBehaviour
{
    [Header("Efecto visual")]
    [Tooltip("ParticleSystem de humo. Si queda vacío se genera uno automáticamente.")]
    public ParticleSystem smokeEffect;

    [Tooltip("Material para las partículas. Si queda vacío se usa el shader URP Particles/Unlit.")]
    public Material smokeMaterial;

    [Header("Fade-in")]
    [Tooltip("Segundos que tarda la emisión en llegar a su tasa máxima.")]
    [Range(0.5f, 5f)]
    public float fadeInDuration = 2f;

    [Header("Referencias")]
    [Tooltip("CircuitManager de esta zona. Si es null, busca en los padres automáticamente.")]
    public CircuitManager circuitManager;

    // ─────────────────────────────────────────────
    private Resistor            _resistor;
    private LED                 _led;
    private Capacitor           _capacitor;
    private VoltageSource       _source;
    private ArduinoPin          _pin;
    private ElectricalComponent _ec;

    private float    _baseEmissionRate;
    private bool     _smokingActive;
    private Coroutine _fadeCoroutine;

    // ─────────────────────────────────────────────
    void Awake()
    {
        _ec        = GetComponent<ElectricalComponent>();
        _resistor  = GetComponent<Resistor>();
        _led       = GetComponent<LED>();
        _capacitor = GetComponent<Capacitor>();
        _source    = GetComponent<VoltageSource>();
        _pin       = GetComponent<ArduinoPin>();

        if (circuitManager == null)
            circuitManager = GetComponentInParent<CircuitManager>(true);

        if (smokeEffect == null)
            smokeEffect = CreateSmokeParticles();

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

        // Usar corriente real, no sourceVoltage: cuando el switch está OFF,
        // CircuitSwitch presenta 1 MΩ → I ≈ 9 μA, bien por debajo del umbral.
        bool shouldSmoke = Mathf.Abs(circuitManager.totalCurrent) > 0.0005f && IsDamaged();

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
        if (_ec        != null && _ec.isOpenCircuit)                                     return true;
        if (_resistor  != null && (_resistor.hasFault || _resistor.isOverloaded))        return true;
        if (_led       != null && (_led.polarityInverted ||
                                   _led.state == LEDState.Overload ||
                                   _led.state == LEDState.NearOverload))                 return true;
        if (_capacitor != null && _capacitor.polarityInverted)                           return true;
        if (_source    != null && _source.hasFault)                                      return true;
        if (_pin       != null && (_pin.hasFault || _pin.hasLooseCable))                 return true;
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
    //  Autocreación del ParticleSystem
    // ─────────────────────────────────────────────
    ParticleSystem CreateSmokeParticles()
    {
        var go = new GameObject("Smoke_VFX");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.up * 0.05f;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale    = Vector3.one;

        var ps = go.AddComponent<ParticleSystem>();

        // Detener antes de configurar: AddComponent arranca el sistema de inmediato
        // y Unity no permite cambiar 'duration' mientras está reproduciendo.
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // ── Main ──────────────────────────────────
        var main = ps.main;
        main.playOnAwake      = false;
        main.loop             = true;
        main.duration         = 2f;
        main.startLifetime    = new ParticleSystem.MinMaxCurve(1.2f, 2.5f);
        main.startSpeed       = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
        main.startSize        = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
        main.startColor       = new ParticleSystem.MinMaxGradient(
                                    new Color(0.55f, 0.55f, 0.55f, 0.70f),
                                    new Color(0.25f, 0.25f, 0.25f, 0.40f));
        main.gravityModifier  = -0.04f;   // sube lentamente
        main.maxParticles     = 30;
        main.simulationSpace  = ParticleSystemSimulationSpace.World;

        // ── Emission ──────────────────────────────
        var emission = ps.emission;
        emission.rateOverTime = 8f;

        // ── Shape ─────────────────────────────────
        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = 0.008f;

        // ── Size over lifetime (crece al subir) ───
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        var sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0.25f),
            new Keyframe(1f, 1.00f));
        sol.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // ── Color over lifetime (se desvanece) ────
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(new Color(0.6f, 0.6f, 0.6f), 0.0f),
                new GradientColorKey(new Color(0.2f, 0.2f, 0.2f), 1.0f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.85f, 0.0f),
                new GradientAlphaKey(0.00f, 1.0f)
            });
        col.color = new ParticleSystem.MinMaxGradient(gradient);

        // ── Renderer / Material ───────────────────
        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;

        Material mat = smokeMaterial;
        if (mat == null)
        {
            // Intenta URP primero, luego fallbacks
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                         ?? Shader.Find("Particles/Standard Unlit")
                         ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended");

            if (shader != null)
            {
                mat = new Material(shader);
                // Surface Type = Transparent en URP
                if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
                if (mat.HasProperty("_Blend"))   mat.SetFloat("_Blend",   0f);
                mat.renderQueue = 3000;
            }
        }
        if (mat != null) rend.material = mat;

        return ps;
    }

    // ─────────────────────────────────────────────
    //  API pública
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
