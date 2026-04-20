using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;   // Requiere: com.unity.xr.interaction.toolkit

/// <summary>
/// Controlador del Explorador VR (Meta Quest 3 + KAT VR).
/// 
/// SETUP REQUERIDO (Package Manager):
///   1. com.unity.xr.interaction.toolkit  ≥ 3.0
///   2. com.unity.xr.hands (opcional, para hand-tracking)
///   3. KAT VR SDK  → importar desde KAT SDK Unity Plugin
///
/// INSTRUCCIONES EN INSPECTOR:
///   - useKatVR = true  → usa caminadora KAT VR (treadmill)
///   - useKatVR = false → fallback con joystick del Meta Quest (locomoción artificial)
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Modo de locomoción")]
    [Tooltip("True = caminadora KAT VR.  False = joystick Meta Quest (fallback).")]
    public bool useKatVR = false;   // Cambiar a true al integrar SDK de KAT

    [Header("Velocidad (joystick fallback)")]
    public float walkSpeed  = 2.0f;
    public float turnSpeed  = 60.0f;

    [Header("Referencia VR")]
    [Tooltip("XR Origin o CameraOffset de la escena VR.")]
    public Transform xrRig;
    [Tooltip("Cámara principal del visor (hijo de XR Origin).")]
    public Camera    headCamera;

    [Header("Interacción")]
    public PlayerInteraction interaction;

    // ─────────────────────────────────────────────
    //  Estado interno
    // ─────────────────────────────────────────────
    private CharacterController _cc;
    private Vector3 _velocity;
    private bool    _isGrounded;

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

        if (useKatVR)
            HandleKatVRLocomotion();
        else
            HandleJoystickLocomotion();

        ApplyGravity();
    }

    // ─────────────────────────────────────────────
    //  Locomoción — KAT VR
    // ─────────────────────────────────────────────

    void InitKatVR()
    {
        // ──────────────────────────────────────────────────────────────────────
        // INTEGRACIÓN KAT VR SDK
        // ──────────────────────────────────────────────────────────────────────
        // 1. Importa el KAT VR Unity Plugin desde el portal de desarrolladores.
        // 2. Agrega el KATCharacterControllerSDK al XR Origin.
        // 3. Reemplaza el código de abajo con las llamadas al SDK:
        //
        //    KATCharacterControllerSDK.Instance.Initialize();
        //
        // Referencia: https://github.com/extravi/kat-walk-c2-unity-template
        // ──────────────────────────────────────────────────────────────────────
        Debug.LogWarning("[PlayerController] KAT VR SDK no integrado aún. " +
                         "Usando joystick como fallback.");
        useKatVR = false;   // Fallback automático hasta que el SDK esté instalado
    }

    void HandleKatVRLocomotion()
    {
        // ──────────────────────────────────────────────────────────────────────
        // Cuando el SDK esté instalado, reemplazar con:
        //
        //   Vector3 katDirection = KATCharacterControllerSDK.Instance.GetMovementDirection();
        //   float   katSpeed     = KATCharacterControllerSDK.Instance.GetMovementSpeed();
        //   Vector3 move = katDirection * katSpeed * Time.deltaTime;
        //   _cc.Move(move);
        //
        // ──────────────────────────────────────────────────────────────────────
    }

    // ─────────────────────────────────────────────
    //  Locomoción — Joystick (fallback Meta Quest)
    // ─────────────────────────────────────────────

    void HandleJoystickLocomotion()
    {
        // Lee los ejes del joystick izquierdo del Meta Quest a través de XRI Input System
        // Los nombres de acción provienen de InputSystem_Actions.inputactions
        float horizontal = Input.GetAxis("Horizontal");   // Joystick X
        float vertical   = Input.GetAxis("Vertical");     // Joystick Y

        // Orientación basada en la cámara del visor (cabeza del jugador)
        Transform camTransform = headCamera != null ? headCamera.transform : Camera.main?.transform;
        if (camTransform == null) return;

        Vector3 forward = Vector3.ProjectOnPlane(camTransform.forward, Vector3.up).normalized;
        Vector3 right   = Vector3.ProjectOnPlane(camTransform.right,   Vector3.up).normalized;

        Vector3 moveDir = (forward * vertical + right * horizontal).normalized;
        _cc.Move(moveDir * walkSpeed * Time.deltaTime);

        // Rotación snap con joystick derecho (práctica estándar VR para reducir mareo)
        // Se implementa desde ActionBasedContinuousTurnProvider del XRI
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
    //  Seguridad (Reto 1 requisito del documento)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Detiene el movimiento del jugador (p.ej. mientras usa el multímetro).
    /// Llamar desde PlayerInteraction al iniciar interacción con componente.
    /// </summary>
    public void FreezeMovement(bool freeze)
    {
        walkSpeed = freeze ? 0f : 2.0f;
    }
}