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

    // KAT VR
    private float   _yawCorrection;   // diferencia yaw cuerpo<->visor al calibrar
    private Vector3 _lastKatPosition; // para sincronizar XR Origin en LateUpdate
    private bool    _usedSimpleMove;  // SimpleMove aplica gravedad; evita doble aplicacion

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
        _usedSimpleMove = false;

        if (!_frozen)
        {
            if (useKatVR) HandleKatVRLocomotion();
            else          HandleJoystickLocomotion();
        }

        // SimpleMove (KAT) ya aplica gravedad internamente; solo aplicar en modo joystick o frozen
        if (!_usedSimpleMove)
            ApplyGravity();
    }

    void LateUpdate()
    {
        // Mueve el XR Origin para que el visor siga al CharacterController en modo KAT
        if (!useKatVR || xrRig == null) return;
        Vector3 offset = transform.position - _lastKatPosition;
        offset.y = 0f;
        xrRig.position += offset;
        _lastKatPosition = transform.position;
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

        // Calibracion inicial: sincroniza orientacion cuerpo<->visor
        CalibrateOrientation(data);
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

        // Calibracion: el boton del sensor sincroniza cuerpo<->visor (estandar del SDK)
        if (data.deviceDatas[0].btnPressed)
        {
            CalibrateOrientation(data);
            return;
        }

        // Rotacion corporal con correccion de yaw para alinear cuerpo y visor
        Quaternion bodyRot = data.bodyRotationRaw
                           * Quaternion.Inverse(Quaternion.Euler(0f, _yawCorrection, 0f));

        // SimpleMove espera velocidad en m/s (sin Time.deltaTime) y aplica gravedad automaticamente
        _cc.SimpleMove(bodyRot * data.moveSpeed * katSpeedMultiplier);
        _usedSimpleMove = true;
    }

    void CalibrateOrientation(KATNativeSDK.TreadMillData data)
    {
        if (headCamera == null) return;

        _yawCorrection = data.bodyRotationRaw.eulerAngles.y - headCamera.transform.eulerAngles.y;

        // Alinea la posicion del CharacterController con el visor al calibrar
        Vector3 pos  = transform.position;
        pos.x        = headCamera.transform.position.x;
        pos.z        = headCamera.transform.position.z;
        transform.position = pos;
        _lastKatPosition   = transform.position;
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
