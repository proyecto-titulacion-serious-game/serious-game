using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;   // Requiere: com.unity.xr.interaction.toolkit

/// <summary>
/// Controlador del Explorador VR (Meta Quest 3 + KAT VR).
///
/// SETUP REQUERIDO (Package Manager):
///   1. com.unity.xr.interaction.toolkit  >= 3.0
///   2. com.unity.xr.hands (opcional, para hand-tracking)
///   3. KAT VR SDK  -> descargar desde kat-vr.com/pages/sdk
///      - Copiar KATNativeSDK.dll  -> Assets/Plugins/
///      - Copiar KATNativeSDK.cs   -> Assets/Scripts/
///
/// INSTRUCCIONES EN INSPECTOR:
///   - useKatVR = true  -> usa caminadora KAT VR (requiere SDK instalado)
///   - useKatVR = false -> fallback con joystick del Meta Quest
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Modo de locomocion")]
    [Tooltip("True = caminadora KAT VR.  False = joystick Meta Quest (fallback).")]
    public bool useKatVR = false;

    [Header("Velocidad (joystick fallback)")]
    public float walkSpeed = 2.0f;
    public float turnSpeed = 60.0f;

    [Header("KAT VR")]
    [Tooltip("Numero de serie del dispositivo. Dejar vacio para detectar automaticamente.")]
    public string katSerialNumber = "";
    [Tooltip("Multiplicador sobre la velocidad reportada por la caminadora.")]
    [Range(0.1f, 3f)] public float katSpeedMultiplier = 1.0f;

    [Header("Referencia VR")]
    [Tooltip("XR Origin o CameraOffset de la escena VR.")]
    public Transform xrRig;
    [Tooltip("Camara principal del visor (hijo de XR Origin).")]
    public Camera headCamera;

    [Header("Interaccion")]
    public PlayerInteraction interaction;

    // ─────────────────────────────────────────────
    //  Estado interno
    // ─────────────────────────────────────────────
    private CharacterController _cc;
    private Vector3 _velocity;
    private bool    _isGrounded;
    private bool    _frozen;

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────
    void Awake()
    {
        _cc = GetComponent<CharacterController>();
    }

    void Start()
    {
        if (useKatVR) InitKatVR();
    }

    void Update()
    {
        _isGrounded = _cc.isGrounded;

        if (!_frozen)
        {
            if (useKatVR) HandleKatVRLocomotion();
            else          HandleJoystickLocomotion();
        }

        ApplyGravity();
    }

    // ─────────────────────────────────────────────
    //  Locomocion — KAT VR
    // ─────────────────────────────────────────────

    void InitKatVR()
    {
        int deviceCount = KATNativeSDK.DeviceCount();
        if (deviceCount == 0)
        {
            Debug.LogWarning("[PlayerController] KAT VR: sin dispositivos detectados. Usando joystick como fallback.");
            useKatVR = false;
            return;
        }

        KATNativeSDK.TreadMillData data = KATNativeSDK.GetWalkStatus(katSerialNumber);
        if (!data.connected)
        {
            Debug.LogWarning("[PlayerController] KAT VR: dispositivo no conectado. Usando joystick como fallback.");
            useKatVR = false;
            return;
        }

        Debug.Log($"[PlayerController] KAT VR conectado: {data.deviceName}");
    }

    void HandleKatVRLocomotion()
    {
        KATNativeSDK.TreadMillData data = KATNativeSDK.GetWalkStatus(katSerialNumber);

        // Fallback en caliente si la caminadora se desconecta durante el juego
        if (!data.connected)
        {
            HandleJoystickLocomotion();
            return;
        }

        // moveSpeed contiene direccion + magnitud en espacio mundo.
        // Se proyecta sobre el plano horizontal para evitar deriva vertical.
        Vector3 move = Vector3.ProjectOnPlane(data.moveSpeed, Vector3.up)
                       * katSpeedMultiplier
                       * Time.deltaTime;
        _cc.Move(move);
    }

    // ─────────────────────────────────────────────
    //  Locomocion — Joystick (fallback Meta Quest)
    // ─────────────────────────────────────────────

    void HandleJoystickLocomotion()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical   = Input.GetAxis("Vertical");

        Transform cam = headCamera != null ? headCamera.transform : Camera.main?.transform;
        if (cam == null) return;

        Vector3 forward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
        Vector3 right   = Vector3.ProjectOnPlane(cam.right,   Vector3.up).normalized;

        Vector3 moveDir = (forward * vertical + right * horizontal).normalized;
        _cc.Move(moveDir * walkSpeed * Time.deltaTime);
    }

    // ─────────────────────────────────────────────
    //  Gravedad
    // ─────────────────────────────────────────────

    void ApplyGravity()
    {
        if (_isGrounded && _velocity.y < 0f)
            _velocity.y = -2f;

        _velocity.y += Physics.gravity.y * Time.deltaTime;
        _cc.Move(_velocity * Time.deltaTime);
    }

    // ─────────────────────────────────────────────
    //  API publica
    // ─────────────────────────────────────────────

    /// <summary>
    /// Detiene o reanuda el movimiento del jugador.
    /// Llamar desde PlayerInteraction al iniciar/terminar una interaccion.
    /// </summary>
    public void FreezeMovement(bool freeze)
    {
        _frozen = freeze;
    }
}
