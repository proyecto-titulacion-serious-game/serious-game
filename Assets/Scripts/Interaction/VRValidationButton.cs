using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Botón físico en VR para validar el circuito.
/// Integrado 100% con la arquitectura de GameSession.cs y compatible
/// con las herramientas de auto-setup del Editor de TITA.
/// </summary>
[RequireComponent(typeof(XRSimpleInteractable))]
public class VRValidationButton : MonoBehaviour
{
    [Header("Referencias (Asignadas por Editor Setup Tools)")]
    public Transform buttonCap;
    public Renderer ledRenderer;
    public HapticFeedback haptics;

    private XRSimpleInteractable _interactable;
    private MaterialPropertyBlock _mpb;

    // Colores del LED del botón (Diegético)
    private static readonly Color ColorIdle    = new Color(0.1f, 0.4f, 0.8f); // Azul (Listo)
    private static readonly Color ColorWait    = new Color(0.8f, 0.6f, 0.1f); // Naranja (Evaluando)
    private static readonly Color ColorSuccess = new Color(0.1f, 0.8f, 0.2f); // Verde (Aprobado)
    private static readonly Color ColorFail    = new Color(0.8f, 0.1f, 0.1f); // Rojo (Fallo)

    void Awake()
    {
        _interactable = GetComponent<XRSimpleInteractable>();
        
        // Fallback por si la herramienta de Editor no logra asignar el renderer
        if (ledRenderer == null) ledRenderer = GetComponent<Renderer>();
        
        _mpb = new MaterialPropertyBlock();
        SetLedColor(ColorIdle);
    }

    void OnEnable()
    {
        _interactable.selectEntered.AddListener(OnButtonPressed);
        GameSession.OnResultadoValidacion += HandleResultadoRed;
        GameManager.OnLevelLoaded         += OnLevelLoaded;
    }

    void OnDisable()
    {
        _interactable.selectEntered.RemoveListener(OnButtonPressed);
        GameSession.OnResultadoValidacion -= HandleResultadoRed;
        GameManager.OnLevelLoaded         -= OnLevelLoaded;
    }

    void Start()
    {
        // Estado inicial: oculto hasta que el GameManager cargue el reto
        var gm = FindAnyObjectByType<GameManager>();
        bool esReto4 = gm != null && gm.currentLevel == LevelType.Arduino;
        AplicarEstadoReto(esReto4);
    }

    void OnLevelLoaded(LevelType level)
    {
        AplicarEstadoReto(level == LevelType.Arduino);
    }

    void AplicarEstadoReto(bool esReto4)
    {
        // Deshabilitar interacción y apagar LED cuando no es Reto 4
        _interactable.enabled = esReto4;
        SetLedColor(esReto4 ? ColorIdle : Color.black);

        // Ocultar también el capuchón si no es Reto 4
        if (buttonCap  != null) buttonCap.gameObject.SetActive(esReto4);
        if (ledRenderer != null) ledRenderer.enabled = esReto4;
    }

    void OnButtonPressed(SelectEnterEventArgs args)
    {
        // 1. Animación física (usa el cap si está asignado, si no usa todo el objeto)
        Transform targetTransform = buttonCap != null ? buttonCap : transform;
        targetTransform.localPosition += new Vector3(0, -0.002f, 0);
        Invoke(nameof(ResetButtonPosition), 0.1f);

        // 2. Feedback Háptico
        if (haptics != null) haptics.PlayMedium();

        // 3. Feedback visual de "Procesando"
        SetLedColor(ColorWait);

        // 4a. En red: pedir validación al Host (responde por GameSession.OnResultadoValidacion).
        if (GameSession.Instance != null)
        {
            GameSession.Instance.SolicitarValidacion();
            Debug.Log("[VR Button] Petición de validación enviada a la PC.");
            return;
        }

        // 4b. Offline (modo de prueba sin Host): evaluar localmente para que el Explorador pueda
        //     comprobar su propio circuito y ver "¡Misión cumplida!" o seguir construyendo.
        var gm = FindAnyObjectByType<GameManager>();
        if (gm != null)
        {
            var (paso, _) = gm.EvaluacionManualBotonFisicoConResultado();
            HandleResultadoRed(paso, paso ? 0 : 1);
            Debug.Log($"[VR Button] Validación local (offline): {(paso ? "OK — misión cumplida" : "circuito incompleto")}.");
        }
        else
        {
            Debug.LogWarning("[VR Button] No hay GameSession ni GameManager: no se pudo validar.");
            ResetToIdle();
        }
    }

    void HandleResultadoRed(bool paso, int codigoMotivo)
    {
        // Feedback visual
        SetLedColor(paso ? ColorSuccess : ColorFail);
        
        // Feedback Háptico
        if (haptics != null)
        {
            if (paso) haptics.PlayStrong();
            else haptics.PlayError();
        }
        
        // Si falló, regresamos a Azul después de 3 segundos
        if (!paso) Invoke(nameof(ResetToIdle), 3f);
    }

    void ResetButtonPosition() 
    {
        Transform targetTransform = buttonCap != null ? buttonCap : transform;
        targetTransform.localPosition -= new Vector3(0, -0.002f, 0);
    }
    
    void ResetToIdle() => SetLedColor(ColorIdle);

    void SetLedColor(Color c)
    {
        if (ledRenderer == null) return;
        ledRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor("_BaseColor", c);
        _mpb.SetColor("_EmissionColor", c * 1.5f);
        ledRenderer.SetPropertyBlock(_mpb);
    }
}