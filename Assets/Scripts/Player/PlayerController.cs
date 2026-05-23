using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;

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
    public bool useKatVR = true;

    [Header("Velocidad (joystick VR)")]
    public float walkSpeed = 2.0f;

    [Header("Snap Turn (joystick derecho)")]
    [Tooltip("False = el joystick derecho NO rota la cámara.")]
    public bool enableSnapTurn = false;
    [Tooltip("Angulo de giro por snap (grados). Solo aplica si enableSnapTurn = true.")]
    [Range(0f, 90f)] public float snapTurnAngle = 45f;
    [Tooltip("Umbral del thumbstick derecho para activar snap turn.")]
    [Range(0.2f, 0.9f)] public float snapTurnThreshold = 0.5f;

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

    [Header("Input Actions (New Input System)")]
    [Tooltip("Accion de movimiento del joystick izquierdo (Vector2). Asignar desde InputSystem_Actions.")]
    public InputActionReference moveAction;

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
    private float   _yawCorrection;
    private Vector3 _lastKatPosition;
    private bool    _usedSimpleMove;
    // true solo cuando KAT está proveyendo datos reales en este frame
    // (evita que LateUpdate aplique el offset cuando se usó el joystick como fallback)
    private bool    _katActive;

    // Snap turn
    private InputAction _snapTurnAct;
    private bool        _snapTurnHeld;

    // Fallback when moveAction reference is unassigned in Inspector
    private InputAction _moveFallback;

    InputAction MoveAction => moveAction?.action ?? _moveFallback;

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────
    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _lastKatPosition = transform.position;

        if (headCamera == null)
            headCamera = Camera.main;

        if (moveAction?.action == null)
            TryAutoFindMoveAction();
    }

    void TryAutoFindMoveAction()
    {
        foreach (var asset in Resources.FindObjectsOfTypeAll<InputActionAsset>())
        {
            var act = asset.FindAction("XRI Left Locomotion/Move")
                   ?? asset.FindAction("Move");
            if (act != null)
            {
                _moveFallback = act;
                Debug.Log($"[PlayerController] moveAction auto-asignado desde '{asset.name}'.");
                return;
            }
        }
        Debug.LogWarning("[PlayerController] moveAction no encontrado automáticamente. " +
                         "Asigna 'XRI Left Locomotion/Move' en el Inspector.");
    }

    void OnEnable()
    {
        var act = MoveAction;
        if (act != null)
        {
            act.actionMap?.Enable();
            act.Enable();
        }
        _snapTurnAct?.Enable();
    }

    void OnDisable()
    {
        MoveAction?.Disable();
        _snapTurnAct?.Disable();
    }

    void Start()
    {
        // Asegurar CharacterController antes de cualquier operación
        EnsureCharacterController();
        
#if !UNITY_EDITOR
        if (!XRSettings.isDeviceActive)
        {
            Debug.LogError("[PlayerController] No hay dispositivo XR activo. " +
                           "El Explorador requiere Meta Quest 3.");
            enabled = false;
            return;
        }
#endif
        // Inicializar KAT VR PRIMERO para que useKatVR refleje si hay hardware real.
        // Si KAT falla, useKatVR quedará false antes de evaluar qué providers deshabilitar.
        if (useKatVR) InitKatVR();

        DisableConflictingXRILocomotion();

        // Asegurar que la acción de movimiento esté habilitada
        var moveAct = MoveAction;
        if (moveAct != null)
        {
            moveAct.actionMap?.Enable();
            moveAct.Enable();
        }
        else
        {
            Debug.LogError("[PlayerController] moveAction NO está asignado. " +
                           "El joystick no moverá al jugador.\n" +
                           "Fix: corre Tools → TITA → Setup Completo VR Explorador, " +
                           "o asigna manualmente la acción 'XRI Left Locomotion/Move' " +
                           "del asset 'XRI Default Input Actions' en el Inspector.");
        }

        DiagnosticarMovimiento();
        InitSnapTurn();
    }

    /// <summary>
    /// Desactiva los providers de locomoción de XRI solo cuando KAT VR está activo.
    /// En modo joystick normal el ContinuousMoveProvider de XRI maneja el movimiento.
    /// </summary>
    void DisableConflictingXRILocomotion()
    {
        // Solo tomar el control cuando KAT VR está activo. En modo joystick estándar
        // el ContinuousMoveProvider de XRI (ya configurado en el XR Rig) maneja el
        // thumbstick izquierdo nativamente sin conflictos.
        if (!useKatVR) return;

        // Buscar en el XR Rig (hijo) y en padres — GetComponentsInParent solo sube,
        // así que buscamos también hacia abajo desde el rig para cubrir la jerarquía real.
        Transform searchRoot = xrRig != null ? xrRig : transform;

        var moveProviders = searchRoot.GetComponentsInChildren<ContinuousMoveProvider>(true);
        foreach (var p in moveProviders)
        {
            if (p.enabled)
            {
                p.enabled = false;
                Debug.Log($"[PlayerController] ContinuousMoveProvider '{p.gameObject.name}' desactivado " +
                          "(PlayerController toma control del movimiento).");
            }
        }

        var continuousTurn = searchRoot.GetComponentsInChildren<ContinuousTurnProvider>(true);
        foreach (var p in continuousTurn)
        {
            if (p.enabled)
            {
                p.enabled = false;
                Debug.Log($"[PlayerController] ContinuousTurnProvider '{p.gameObject.name}' desactivado.");
            }
        }

        var snapTurn = searchRoot.GetComponentsInChildren<SnapTurnProvider>(true);
        foreach (var p in snapTurn)
        {
            if (p.enabled)
            {
                p.enabled = false;
                Debug.Log($"[PlayerController] SnapTurnProvider '{p.gameObject.name}' desactivado.");
            }
        }
    }

    [ContextMenu("Diagnosticar movimiento")]
    public void DiagnosticarMovimiento()
    {
        Debug.Log("════ [PlayerController] DIAGNÓSTICO DE MOVIMIENTO ════");
        
        // Verificar componente crítico primero
        EnsureCharacterController();
        
        Debug.Log($"  GameObject      = {gameObject.name}");
        Debug.Log($"  GameObject activo = {gameObject.activeInHierarchy}");
        Debug.Log($"  Component activo = {enabled}");
        Debug.Log($"  useKatVR        = {useKatVR}");
        Debug.Log($"  moveAction      = {(MoveAction != null ? MoveAction.name : "⚠ NULL — asignar en Inspector")}");

        if (moveAction != null)
        {
            try
            {
                bool enabled_ = moveAction.action.enabled;
                Vector2 val   = moveAction.action.ReadValue<Vector2>();
                Debug.Log($"  action enabled  = {enabled_}");
                Debug.Log($"  action value    = {val}  (mueve el joystick izquierdo ahora para ver si cambia)");
                Debug.Log($"  action map      = {moveAction.action.actionMap?.name ?? "sin action map"}");
                Debug.Log($"  bindings        = {moveAction.action.bindings.Count}");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"  ⚠ Error leyendo moveAction: {e.Message}");
            }
        }

        Debug.Log($"  headCamera      = {(headCamera != null ? headCamera.name : "⚠ NULL")}");
        Debug.Log($"  xrRig           = {(xrRig != null ? xrRig.name : "⚠ NULL")}");
        
        if (_cc != null)
        {
            Debug.Log($"  CharacterController.enabled = {_cc.enabled}");
            Debug.Log($"  CharacterController.isGrounded = {_cc.isGrounded}");
            Debug.Log($"  CharacterController.height = {_cc.height}");
            Debug.Log($"  CharacterController.radius = {_cc.radius}");
        }
        else
        {
            Debug.LogError("  ❌ CharacterController is NULL - ejecutar Tools → TITA → Fix PlayerController Components");
        }
        
        Debug.Log($"  _frozen         = {_frozen}");
        Debug.Log("════════════════════════════════════════════════════════");
    }
    
    /// <summary>
    /// Asegura que CharacterController esté asignado correctamente
    /// </summary>
    void EnsureCharacterController()
    {
        if (_cc == null)
        {
            _cc = GetComponent<CharacterController>();
            if (_cc == null)
            {
                Debug.LogError($"[PlayerController] {gameObject.name} no tiene CharacterController. " +
                               "Ejecutar Tools → TITA → Fix PlayerController Components para corregir.");
            }
        }
    }

    void Update()
    {
        // Asegurar que CharacterController esté disponible
        EnsureCharacterController();
        if (_cc == null) return; // Skip update si no hay CharacterController
        
        _isGrounded = _cc.isGrounded;
        _usedSimpleMove = false;
        _katActive = false;

        if (!_frozen)
        {
            // En modo KAT VR el CharacterController mueve al jugador.
            // En modo joystick estándar el ContinuousMoveProvider de XRI lo hace:
            // no aplicamos movimiento aquí para evitar doble desplazamiento.
            if (useKatVR) HandleKatVRLocomotion();

            HandleSnapTurn();
        }

        // La gravedad solo aplica en modo KAT VR donde usamos CharacterController.
        // XRI gestiona la altura del jugador a través del tracking del visor.
        if (useKatVR && !_usedSimpleMove)
            ApplyGravity();
    }

    void LateUpdate()
    {
        // Solo desplaza el XR Origin cuando KAT proveyó movimiento real en este frame.
        // Si KAT cayó en fallback de joystick, _katActive=false → NO doble movimiento.
        if (!_katActive || xrRig == null) return;
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
        try
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
            CalibrateOrientation(data);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[PlayerController] KAT VR no disponible ({e.GetType().Name}). Usando joystick como fallback.");
            useKatVR = false;
        }
    }

    void HandleKatVRLocomotion()
    {
        EnsureCharacterController();
        if (_cc == null) return;
        
        KATNativeSDK.TreadMillData data = KATNativeSDK.GetWalkStatus(katSerialNumber);

        if (!data.connected || data.deviceDatas == null || data.deviceDatas.Length == 0)
        {
            // KAT no disponible en este frame — joystick sin doble offset
            HandleJoystickLocomotion();
            return;
        }

        if (data.deviceDatas[0].btnPressed)
        {
            CalibrateOrientation(data);
            return;
        }

        Quaternion bodyRot = data.bodyRotationRaw
                           * Quaternion.Inverse(Quaternion.Euler(0f, _yawCorrection, 0f));

        _cc.SimpleMove(bodyRot * data.moveSpeed * katSpeedMultiplier);
        _usedSimpleMove = true;
        _katActive = true;   // KAT proveyó movimiento real → LateUpdate puede correr
    }

    void CalibrateOrientation(KATNativeSDK.TreadMillData data)
    {
        if (headCamera == null) return;

        _yawCorrection = data.bodyRotationRaw.eulerAngles.y - headCamera.transform.eulerAngles.y;

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
        EnsureCharacterController();
        if (_cc == null) return;
        
        if (headCamera == null)
        {
            headCamera = Camera.main;
            if (headCamera == null) return;
        }

        Vector2 stick = MoveAction != null
            ? MoveAction.ReadValue<Vector2>()
            : Vector2.zero;

        if (stick.sqrMagnitude < 0.001f) return;

        Vector3 forward = Vector3.ProjectOnPlane(headCamera.transform.forward, Vector3.up).normalized;
        Vector3 right   = Vector3.ProjectOnPlane(headCamera.transform.right,   Vector3.up).normalized;

        // Sin .normalized: la magnitud del stick controla la velocidad (control analógico)
        Vector3 moveDir = forward * stick.y + right * stick.x;
        _cc.Move(moveDir * walkSpeed * Time.deltaTime);
    }

    // ─────────────────────────────────────────────
    //  Snap Turn
    // ─────────────────────────────────────────────

    void InitSnapTurn()
    {
        if (!enableSnapTurn || snapTurnAngle <= 0f || xrRig == null) return;

        // Busca la acción de snap turn en el mismo asset que moveAction
        // (XRI Default Input Actions tiene "XRI Right Locomotion/Snap Turn" → thumbstick derecho)
        if (moveAction?.asset != null)
        {
            _snapTurnAct = moveAction.asset.FindAction("XRI Right Locomotion/Snap Turn")
                        ?? moveAction.asset.FindAction("XRI Right Locomotion/Turn")
                        ?? moveAction.asset.FindAction("Explorer/SnapTurn");
        }

        if (_snapTurnAct == null)
        {
            Debug.LogWarning("[PlayerController] No se encontró acción de snap turn. " +
                             "El thumbstick derecho no rotará al jugador.");
            return;
        }

        _snapTurnAct.Enable();
        Debug.Log($"[PlayerController] Snap turn activado: {_snapTurnAct.actionMap?.name}/{_snapTurnAct.name}");
    }

    void HandleSnapTurn()
    {
        if (!enableSnapTurn || snapTurnAngle <= 0f || _snapTurnAct == null || xrRig == null) return;

        Vector2 val = _snapTurnAct.ReadValue<Vector2>();

        if (Mathf.Abs(val.x) > snapTurnThreshold)
        {
            if (!_snapTurnHeld)
            {
                // Rota el XR Origin alrededor de la posición horizontal de la cabeza
                // → la cámara mantiene su posición XZ pero el visor gira visualmente
                Vector3 pivot = headCamera
                    ? new Vector3(headCamera.transform.position.x, xrRig.position.y, headCamera.transform.position.z)
                    : xrRig.position;

                xrRig.RotateAround(pivot, Vector3.up, Mathf.Sign(val.x) * snapTurnAngle);
                _snapTurnHeld = true;
            }
        }
        else
        {
            _snapTurnHeld = false;
        }
    }

    // ─────────────────────────────────────────────
    //  Gravedad
    // ─────────────────────────────────────────────

    void ApplyGravity()
    {
        EnsureCharacterController();
        if (_cc == null) return;
        
        if (_isGrounded && _velocity.y < 0f)
            _velocity.y = -2f;

        _velocity.y += Physics.gravity.y * Time.deltaTime;
        _cc.Move(_velocity * Time.deltaTime);
    }

    // ─────────────────────────────────────────────
    //  API publica
    // ─────────────────────────────────────────────

    public void FreezeMovement(bool freeze)
    {
        _frozen = freeze;
    }
}
