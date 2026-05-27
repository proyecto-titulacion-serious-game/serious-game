using System.Collections;
using UnityEngine;

/// <summary>
/// Humo + chispas para componentes dañados/sobrecargados.
///
/// DOS NIVELES de efecto:
///   Dañado  → humo denso (hasFault, polarityInverted, NearOverload…)
///   Sobrecarga → humo + chispas amarillas (LEDState.Overload, Resistor.isOverloaded, etc.)
///
/// SETUP MÍNIMO: añadir este script al GO del componente eléctrico.
/// Los ParticleSystem se generan automáticamente si no se asignan.
/// </summary>
[DisallowMultipleComponent]
public class ComponentSmokeEffect : MonoBehaviour
{
    [Header("Humo")]
    [Tooltip("ParticleSystem de humo. Se genera automáticamente si queda vacío.")]
    public ParticleSystem smokeEffect;
    [Tooltip("Material para el humo (URP Particles/Unlit recomendado). Opcional.")]
    public Material smokeMaterial;

    [Header("Dirección del humo (espacio LOCAL del componente)")]
    [Tooltip("Hacia dónde sube el humo en el espacio local del componente.\n" +
             "Vector3.up    → sale hacia el eje Y local (componente horizontal).\n" +
             "Vector3.forward → sale hacia el eje Z local (componente vertical/de frente).\n" +
             "Vector3.right  → sale hacia el eje X local.")]
    public Vector3 smokeDirection = Vector3.up;

    [Header("Chispas (sobrecarga)")]
    [Tooltip("ParticleSystem de chispas. Se genera automáticamente si queda vacío.")]
    public ParticleSystem sparkEffect;
    [Tooltip("Material para las chispas (Additive recomendado). Opcional.")]
    public Material sparkMaterial;

    [Header("Tiempos")]
    [Range(0.5f, 5f)]
    public float fadeInDuration = 1.5f;

    [Header("Referencias")]
    public CircuitManager circuitManager;

    // ─────────────────────────────────────────────
    private Resistor            _resistor;
    private LED                 _led;
    private Capacitor           _capacitor;
    private VoltageSource       _source;
    private ArduinoPin          _pin;
    private ElectricalComponent _ec;

    private float    _smokeBaseRate;
    private bool     _smokingActive;
    private bool     _sparksActive;
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

        if (smokeEffect == null) smokeEffect = CreateSmokeParticles();
        if (sparkEffect == null) sparkEffect = CreateSparkParticles();

        // Rotar el GO del humo para que su eje Y apunte en smokeDirection (local space)
        if (smokeEffect != null)
        {
            var dir = smokeDirection == Vector3.zero ? Vector3.up : smokeDirection.normalized;
            smokeEffect.transform.localRotation = Quaternion.FromToRotation(Vector3.up, dir);
            _smokeBaseRate = smokeEffect.emission.rateOverTime.constant;
            smokeEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        if (sparkEffect != null)
            sparkEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    void OnEnable()  => CircuitManager.OnCircuitChanged += Refresh;
    void OnDisable()
    {
        CircuitManager.OnCircuitChanged -= Refresh;
        StopAll();
    }

    // ─────────────────────────────────────────────
    //  Lógica principal
    // ─────────────────────────────────────────────
    void Refresh()
    {
        if (circuitManager == null) return;

        // Sin corriente real (switch apagado) → nada
        bool circuitLive = Mathf.Abs(circuitManager.totalCurrent) > 0.0005f;

        bool damaged    = circuitLive && IsDamaged();
        bool overloaded = circuitLive && IsOverloaded();

        // ── Humo ──────────────────────────────────
        if (smokeEffect != null)
        {
            if (damaged && !_smokingActive)
            {
                _smokingActive = true;
                if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = StartCoroutine(FadeInSmoke());
            }
            else if (!damaged && _smokingActive)
            {
                StopSmoke();
            }
        }

        // ── Chispas (solo en sobrecarga) ──────────
        if (sparkEffect != null)
        {
            if (overloaded && !_sparksActive)
            {
                _sparksActive = true;
                sparkEffect.Play(true);   // withChildren para prefabs CFXR multi-PS
            }
            else if (!overloaded && _sparksActive)
            {
                _sparksActive = false;
                sparkEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }

    // Cualquier falla → humo
    bool IsDamaged()
    {
        if (_ec        != null && _ec.isOpenCircuit)                              return true;
        if (_resistor  != null && (_resistor.hasFault || _resistor.isOverloaded)) return true;
        if (_led       != null && (_led.polarityInverted ||
                                   _led.state == LEDState.Overload ||
                                   _led.state == LEDState.NearOverload))          return true;
        if (_capacitor != null && _capacitor.polarityInverted)                    return true;
        if (_source    != null && _source.hasFault)                               return true;
        if (_pin       != null && (_pin.hasFault || _pin.hasLooseCable))          return true;
        return false;
    }

    // Solo sobrecarga severa → chispas
    bool IsOverloaded()
    {
        if (_resistor  != null && _resistor.isOverloaded)           return true;
        if (_led       != null && _led.state == LEDState.Overload)  return true;
        if (_capacitor != null && _capacitor.polarityInverted &&
            Mathf.Abs(circuitManager.totalCurrent) > 0.05f)         return true;
        if (_pin       != null && _pin.hasFault &&
            Mathf.Abs(circuitManager.totalCurrent) > 0.05f)         return true;
        return false;
    }

    // ─────────────────────────────────────────────
    //  Fade-in humo
    // ─────────────────────────────────────────────
    IEnumerator FadeInSmoke()
    {
        // withChildren:true activa los sub-efectos de prefabs CFXR (múltiples PS hijos)
        smokeEffect.Play(true);

        // Fade-in en el PS raíz (afecta la densidad visible del efecto)
        var emission = smokeEffect.emission;
        emission.rateOverTime = 0f;

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            if (this == null || smokeEffect == null) yield break;
            elapsed += Time.deltaTime;
            emission.rateOverTime = Mathf.Lerp(0f, _smokeBaseRate, elapsed / fadeInDuration);
            yield return null;
        }
        emission.rateOverTime = _smokeBaseRate;
        _fadeCoroutine = null;
    }

    void StopSmoke()
    {
        _smokingActive = false;
        if (_fadeCoroutine != null) { StopCoroutine(_fadeCoroutine); _fadeCoroutine = null; }
        // Usar if en lugar de ?. — Unity destruye el PS al desactivar la zona
        // y ?. bypasea el operador == de Unity para objetos destruidos.
        if (smokeEffect != null)
            smokeEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    void StopAll()
    {
        StopSmoke();
        _sparksActive = false;
        if (sparkEffect != null)
            sparkEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        // withChildren:true detiene también los sub-efectos CFXR
        smokeEffect?.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    void StopAll()
    {
        StopSmoke();
        _sparksActive = false;
        sparkEffect?.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    // ─────────────────────────────────────────────
    //  Autocreación: HUMO
    // ─────────────────────────────────────────────
    ParticleSystem CreateSmokeParticles()
    {
        var go = new GameObject("Smoke_VFX");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.up * 0.06f;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale    = Vector3.one;

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake     = false;
        main.loop            = true;
        main.duration        = 2f;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(1.5f, 3.0f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(0.06f, 0.18f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);  // más grande
        main.startColor      = new ParticleSystem.MinMaxGradient(
                                   new Color(0.55f, 0.55f, 0.55f, 0.80f),
                                   new Color(0.20f, 0.20f, 0.20f, 0.45f));
        main.gravityModifier = 0f;       // sin gravedad mundial: la dirección la da el Cone
        main.maxParticles    = 60;
        main.simulationSpace = ParticleSystemSimulationSpace.Local; // respeta la rotación del GO

        var emission = ps.emission;
        emission.rateOverTime = 18f;                                           // más emisión

        // Cone: emite a lo largo del eje Y local del GO → smokeDirection lo rota en Awake
        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle     = 18f;   // apertura del cono (más estrecho = más columna)
        shape.radius    = 0.008f;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(new Keyframe(0f, 0.2f), new Keyframe(1f, 1f)));

        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.65f, 0.60f, 0.55f), 0f),
                new GradientColorKey(new Color(0.15f, 0.15f, 0.15f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.90f, 0f),
                new GradientAlphaKey(0.00f, 1f)
            });
        col.color = new ParticleSystem.MinMaxGradient(g);

        ApplyMaterial(go, smokeMaterial,
            "Universal Render Pipeline/Particles/Unlit",
            "Particles/Standard Unlit",
            "Legacy Shaders/Particles/Alpha Blended",
            additive: false);

        return ps;
    }

    // ─────────────────────────────────────────────
    //  Autocreación: CHISPAS
    // ─────────────────────────────────────────────
    ParticleSystem CreateSparkParticles()
    {
        var go = new GameObject("Sparks_VFX");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.up * 0.02f;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale    = Vector3.one;

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake     = false;
        main.loop            = true;
        main.duration        = 1f;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(0.8f, 2.5f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.003f, 0.009f);
        main.startColor      = new ParticleSystem.MinMaxGradient(
                                   new Color(1.0f, 0.9f, 0.3f, 1f),   // amarillo brillante
                                   new Color(1.0f, 0.5f, 0.1f, 1f));  // naranja
        main.gravityModifier = 1.8f;    // caen como chispas reales
        main.maxParticles    = 80;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 35f;

        // Ráfagas ocasionales para efecto errático
        emission.SetBursts(new ParticleSystem.Burst[]
        {
            new ParticleSystem.Burst(0f,   8, 15, 3, 0.4f),
            new ParticleSystem.Burst(0.5f, 5, 12, 2, 0.3f),
        });

        var shape = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius    = 0.006f;

        // Las chispas no crecen, se mantienen pequeñas
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f,
            new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f)));

        // Color: blanco brillante → naranja → apagado
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 1f, 0.8f), 0.0f),
                new GradientColorKey(new Color(1f, 0.4f, 0f), 0.5f),
                new GradientColorKey(new Color(0.3f, 0.1f, 0f), 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1f, 0.0f),
                new GradientAlphaKey(0f, 1.0f)
            });
        col.color = new ParticleSystem.MinMaxGradient(g);

        // Stretch para que las chispas parezcan líneas de luz al moverse
        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.renderMode       = ParticleSystemRenderMode.Stretch;
        rend.velocityScale    = 0.15f;
        rend.lengthScale      = 1.5f;

        ApplyMaterial(go, sparkMaterial,
            "Universal Render Pipeline/Particles/Unlit",
            "Particles/Additive",
            "Legacy Shaders/Particles/Additive",
            additive: true);

        return ps;
    }

    // ─────────────────────────────────────────────
    //  Helper material
    // ─────────────────────────────────────────────
    void ApplyMaterial(GameObject go, Material custom,
                       string shader1, string shader2, string shader3,
                       bool additive)
    {
        var rend = go.GetComponent<ParticleSystemRenderer>();
        Material mat = custom;
        if (mat == null)
        {
            Shader sh = Shader.Find(shader1) ?? Shader.Find(shader2) ?? Shader.Find(shader3);
            if (sh != null)
            {
                mat = new Material(sh);
                if (additive)
                {
                    if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
                    if (mat.HasProperty("_Blend"))   mat.SetFloat("_Blend",   3f); // Additive en URP
                }
                else
                {
                    if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
                    if (mat.HasProperty("_Blend"))   mat.SetFloat("_Blend",   0f);
                }
                mat.renderQueue = 3000;
            }
        }
        if (mat != null) rend.material = mat;
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

    public void ForceStop() => StopAll();
}
