using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;


/// <summary>
/// Botón físico VR de validación manual para el Explorador.
/// Al presionarlo activa GameManager.EvaluacionManualBotonFisico() y notifica
/// al Técnico vía GameSession.RPC_SolicitarValidacion (si hay red activa).
///
/// Feedback integrado:
///   - Animación: el capuchón del botón baja y sube (4 mm)
///   - LED: Azul=esperando | Amarillo=evaluando | Verde=aprobado | Rojo=fallido
///   - Háptica: PlayMedium al presionar, PlayStrong/PlayError según resultado
///   - Cooldown: no puede spamearse (configurable)
///
/// JERARQUÍA RECOMENDADA:
///   ValidationButton          ← este script, XRSimpleInteractable, CapsuleCollider
///   ├── Button_Cap             ← capuchón animado (MeshRenderer)
///   │   └── LED_Indicator      ← esfera pequeña (MeshRenderer)
///   └── Button_Base            ← cuerpo fijo (MeshRenderer)
///
/// SETUP: Arrastrar Button_Cap al campo buttonCap, LED_Indicator al ledRenderer,
///        HapticFeedback del Explorador a haptics. GameManager y GameSession se
///        auto-detectan en Start().
/// </summary>
[RequireComponent(typeof(Collider))]
public class VRValidationButton : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Animación del botón")]
    [Tooltip("Transform del capuchón que se desplaza al presionar.")]
    public Transform  buttonCap;
    [Tooltip("Distancia (m) que baja el capuchón al presionar.")]
    public float      pressDepth    = 0.004f;
    [Tooltip("Segundos de la animación de bajada y subida.")]
    public float      animDuration  = 0.08f;

    [Header("LED Indicador")]
    public Renderer   ledRenderer;
    public Color      colorIdle      = new Color(0.2f, 0.4f, 1f);
    public Color      colorEvaluando = new Color(1f,   0.8f, 0f);
    public Color      colorPass      = new Color(0.1f, 0.9f, 0.1f);
    public Color      colorFail      = new Color(0.9f, 0.1f, 0.1f);

    [Header("Retroalimentación")]
    public HapticFeedback haptics;
    [Tooltip("Segundos de cooldown entre pulsaciones.")]
    public float          cooldown = 2f;

    [Header("Audio")]
    [Tooltip("Sonido al presionar. Opcional.")]
    public AudioClip sfxPress;
    [Tooltip("Sonido de resultado positivo. Opcional.")]
    public AudioClip sfxPass;
    [Tooltip("Sonido de resultado negativo. Opcional.")]
    public AudioClip sfxFail;

    // ─────────────────────────────────────────────
    //  Estado interno
    // ─────────────────────────────────────────────
    private GameManager _gm;
    private AudioSource _audio;
    private bool        _animating;
    private float       _cooldownEnd;
    private Vector3     _capRestPos;

    // ─────────────────────────────────────────────
    //  Unity
    // ─────────────────────────────────────────────
    void Awake()
    {
        // Asegurar trigger en el collider raíz
        GetComponent<Collider>().isTrigger = true;

        // Suscribir XRSimpleInteractable si existe
        var xr = GetComponent<XRBaseInteractable>();
        if (xr != null)
            xr.selectEntered.AddListener(_ => OnPress());

        // AudioSource
        _audio = GetComponent<AudioSource>();
        if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake  = false;
        _audio.spatialBlend = 1f;

        if (buttonCap != null)
            _capRestPos = buttonCap.localPosition;
    }

    void Start()
    {
        _gm = FindAnyObjectByType<GameManager>();
        if (haptics == null)
            haptics = FindAnyObjectByType<HapticFeedback>();

        SetLED(colorIdle);
    }

    void OnDestroy() { }

    // ─────────────────────────────────────────────
    //  Detección por contacto físico (fallback)
    // ─────────────────────────────────────────────
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("RightHand") || other.CompareTag("LeftHand"))
            OnPress();
    }

    // ─────────────────────────────────────────────
    //  Lógica principal
    // ─────────────────────────────────────────────
    public void OnPress()
    {
        if (_animating) return;
        if (Time.time < _cooldownEnd) return;
        _cooldownEnd = Time.time + cooldown;

        StartCoroutine(PressSequence());
    }

    IEnumerator PressSequence()
    {
        _animating = true;

        // ── 1. Animación de bajada ────────────────
        PlaySfx(sfxPress);
        haptics?.PlayMedium();
        yield return StartCoroutine(AnimateCap(pressDepth));

        // ── 2. Estado "evaluando" ─────────────────
        SetLED(colorEvaluando);

        // ── 3. Notificar al Técnico por red ───────
        if (GameSession.Instance != null)
            GameSession.Instance.SolicitarValidacion();

        // ── 4. Evaluar localmente ─────────────────
        bool paso = false;
        string motivo = "Sin GameManager";

        if (_gm != null)
        {
            var resultado = _gm.EvaluacionManualBotonFisicoConResultado();
            paso   = resultado.pass;
            motivo = resultado.motivo;
        }

        // ── 5. Mostrar resultado visual ───────────
        yield return new WaitForSeconds(0.25f);
        MostrarResultado(paso, motivo);

        // ── 6. Animación de subida ────────────────
        yield return StartCoroutine(AnimateCap(-pressDepth));

        // ── 7. Volver a idle tras 3s ──────────────
        yield return new WaitForSeconds(3f);
        SetLED(colorIdle);

        _animating = false;
    }

    void MostrarResultado(bool paso, string motivo)
    {
        if (paso)
        {
            SetLED(colorPass);
            haptics?.PlayStrong();
            PlaySfx(sfxPass);
            Debug.Log("[VRValidationButton] ✅ APROBADO");
        }
        else
        {
            SetLED(colorFail);
            haptics?.PlayError();
            PlaySfx(sfxFail);
            Debug.Log($"[VRValidationButton] ❌ FALLIDO — {motivo}");
        }
    }

    // ─────────────────────────────────────────────
    //  Animación
    // ─────────────────────────────────────────────
    IEnumerator AnimateCap(float deltaY)
    {
        if (buttonCap == null) yield break;

        Vector3 from = buttonCap.localPosition;
        Vector3 to   = from + new Vector3(0, -deltaY, 0);
        float   t    = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / animDuration;
            buttonCap.localPosition = Vector3.Lerp(from, to, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }
        buttonCap.localPosition = to;
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────
    void SetLED(Color color)
    {
        if (ledRenderer == null) return;
        var mpb = new MaterialPropertyBlock();
        mpb.SetColor("_BaseColor",      color);
        mpb.SetColor("_EmissionColor",  color * 2f);
        ledRenderer.SetPropertyBlock(mpb);
    }

    void PlaySfx(AudioClip clip)
    {
        if (_audio != null && clip != null)
            _audio.PlayOneShot(clip);
    }
}

// ─────────────────────────────────────────────
//  Códigos de motivo de validación
// ─────────────────────────────────────────────
public static class ValidationMotivo
{
    public const int Pass          = 0;
    public const int Cortocircuito = 1;
    public const int CircuitoAbierto = 2;
    public const int CorrienteFuera  = 3;
    public const int PinArduino    = 4;
    public const int SinCircuito   = 5;

    public static string Texto(int codigo) => codigo switch
    {
        Pass            => "Aprobado",
        Cortocircuito   => "Cortocircuito en la protoboard",
        CircuitoAbierto => "Circuito abierto — falta una conexión",
        CorrienteFuera  => "Corriente fuera del rango 5–20 mA",
        PinArduino      => "Pin del Arduino incorrecto o cable suelto",
        SinCircuito     => "CircuitSimulator no encontrado",
        _               => "Error desconocido"
    };
}
