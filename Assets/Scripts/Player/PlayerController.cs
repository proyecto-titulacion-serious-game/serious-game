using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;

/// <summary>
/// Controlador del Explorador VR (Meta Quest 3 + KAT VR).
/// VERSIÓN CORREGIDA: Incluye validaciones matemáticas estrictas contra valores NaN/Infinity
/// para prevenir el error 'Screen position out of view frustum'.
/// </summary>
public class PlayerController : MonoBehaviour
{
    //      
    //  Inspector     
    //      
    [Header("Modo de locomocion")]
    [Tooltip("True = caminadora KAT VR.  False = joystick Meta Quest (fallback).")]
    public bool useKatVR = true;

    [Header("Velocidad (joystick VR)")]
    public float walkSpeed = 2.0f;

    [Header("Snap Turn (joystick derecho)")]
    [Tooltip("False = el joystick derecho NO rota la c mara.")]
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
    [Tooltip("CharacterController a mover. Vacío = se busca en este GO o en el xrRig.\n" +
             "En el setup consolidado apunta al CC del XR Origin (XR Rig).")]
    public CharacterController characterController;

    [Header("Input Actions (New Input System)")]
    [Tooltip("Accion de movimiento del joystick izquierdo (Vector2). Asignar desde InputSystem_Actions.")]
    public InputActionReference moveAction;

    [Header("Interaccion")]
    public PlayerInteraction interaction;

    //      
    //  Estado interno     
    //      
    private CharacterController _cc;
    private Vector3 _velocity;
    private bool    _isGrounded;
    private bool    _frozen;

    // KAT VR
    private float   _yawCorrection;
    private Vector3 _lastKatPosition;
    private bool    _usedSimpleMove;
    private bool    _katActive;
    private bool    _katBtnWasPressed;
    private float   _lastKatDiag;
    private double  _lastKatUpdateTime;
    private string  _resolvedSerial = "";

    // Snap turn
    private InputAction _snapTurnAct;
    private bool        _snapTurnHeld;

    // Fallback when moveAction reference is unassigned in Inspector
    private InputAction _moveFallback;
    InputAction MoveAction => moveAction?.action ?? _moveFallback;

    //      
    //  Unity Lifecycle     
    //      
    void Awake()
    {
        EnsureCharacterController();
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
        Debug.LogWarning("[PlayerController] moveAction no encontrado autom ticamente. " +
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
        EnsureCharacterController(); 

#if !UNITY_EDITOR
        if (!XRSettings.isDeviceActive)
        {
            Debug.LogError("[PlayerController] No hay dispositivo XR activo. El Explorador requiere Meta Quest 3.");
            enabled = false;
            return;
        }
#endif

        if (useKatVR) InitKatVR();

        DisableConflictingXRILocomotion();

        var moveAct = MoveAction;
        if (moveAct != null)
        {
            moveAct.actionMap?.Enable();
            moveAct.Enable();
        }
        else
        {
            Debug.LogError("[PlayerController] moveAction NO est  asignado.");
        }

        // PARCHE DE SEGURIDAD START: Forzamos calibración posicional inicial en coordenadas limpias
        _lastKatPosition = transform.position;

        DiagnosticarMovimiento();
        InitSnapTurn();
    }

    void DisableConflictingXRILocomotion()
    {
        if (!useKatVR) return;

        Transform searchRoot = xrRig != null ? xrRig : transform;
        var moveProviders = searchRoot.GetComponentsInChildren<ContinuousMoveProvider>(true);
        foreach (var p in moveProviders)
        {
            if (p.enabled) p.enabled = false;
        }

        var continuousTurn = searchRoot.GetComponentsInChildren<ContinuousTurnProvider>(true);
        foreach (var p in continuousTurn)
        {
            if (p.enabled) p.enabled = false;
        }

        var snapTurn = searchRoot.GetComponentsInChildren<SnapTurnProvider>(true);
        foreach (var p in snapTurn)
        {
            if (p.enabled) p.enabled = false;
        }
    }

    [ContextMenu("Diagnosticar movimiento")]
    public void DiagnosticarMovimiento()
    {
        Debug.Log("== [PlayerController] DIAGN STICO DE MOVIMIENTO ==");
        EnsureCharacterController(); 
        Debug.Log($"  headCamera      = {(headCamera != null ? headCamera.name : "== NULL ==")}");
        Debug.Log($"  xrRig           = {(xrRig != null ? xrRig.name : "== NULL ==")}");
        Debug.Log("=================================================");
    }

    void EnsureCharacterController()
    {
        if (_cc != null) return;
        if (characterController != null) { _cc = characterController; return; }
        _cc = GetComponent<CharacterController>();
        if (_cc == null && xrRig != null)
            _cc = xrRig.GetComponent<CharacterController>() ?? xrRig.GetComponentInChildren<CharacterController>();
    }

    void Update()
    {
        EnsureCharacterController();
        if (_cc == null) return; 

        _isGrounded = _cc.isGrounded;
        _usedSimpleMove = false;
        _katActive = false;

        if (!_frozen)
        {
            if (useKatVR)
                HandleKatVRLocomotion();
            else
                HandleJoystickLocomotion();

            // Gravedad SIEMPRE que no se haya usado SimpleMove (KAT), que ya la aplica
            // internamente. Sin esto, en modo joystick el rig nunca cae al suelo → flota.
            if (!_usedSimpleMove)
                ApplyGravity();

            HandleSnapTurn();
        }
    }

    void LateUpdate()
    {
        if (!_katActive || xrRig == null) return;

        Vector3 offset = transform.position - _lastKatPosition;
        offset.y = 0f;

        // ─── PARCHE CRÍTICO: VALIDACIÓN DE LIMBO DE FLOTANTES (ANTI-NAN) ───
        if (float.IsNaN(offset.x) || float.IsNaN(offset.z) || float.IsInfinity(offset.x) || float.IsInfinity(offset.z))
        {
            _lastKatPosition = transform.position;
            return;
        }

        // Si el cálculo genera un salto anormal (un pico infinito de teletransporte en el frame 1)
        if (offset.sqrMagnitude > 50f)
        {
            _lastKatPosition = transform.position;
            return;
        }

        xrRig.position += offset;
        _lastKatPosition = transform.position;
    }

    void InitKatVR()
    {
        try
        {
            int deviceCount = KATNativeSDK.DeviceCount();
            Debug.Log($"[PlayerController/KAT] DeviceCount = {deviceCount}");
            if (deviceCount == 0)
            {
                Debug.LogWarning("[PlayerController/KAT] No se detectó ninguna caminadora KAT. " +
                                 "Verifica KAT Gateway abierto y la caminadora conectada/encendida. " +
                                 "→ Fallback a joystick.");
                useKatVR = false;
                return;
            }

            // Resolver el SERIAL real del dispositivo. Llamar GetWalkStatus("") suele devolver una
            // estructura congelada (lastUpdateTimePoint no avanza) → hay que pasar el serial real.
            ResolveKatSerial(deviceCount);

            // Enganchar el streaming de datos en vivo del dispositivo.
            try { KATNativeSDK.ForceConnect(_resolvedSerial); Debug.Log($"[PlayerController/KAT] ForceConnect('{_resolvedSerial}')"); }
            catch (System.Exception e) { Debug.LogWarning("[PlayerController/KAT] ForceConnect falló: " + e.Message); }

            KATNativeSDK.TreadMillData data = KATNativeSDK.GetWalkStatus(KatSerial());
            Debug.Log($"[PlayerController/KAT] GetWalkStatus(sn='{KatSerial()}') → connected={data.connected}, device='{data.deviceName}', updT={data.lastUpdateTimePoint:0.000}");
            if (!data.connected)
            {
                Debug.LogWarning("[PlayerController/KAT] La caminadora aparece pero NO está 'connected'. " +
                                 "→ Fallback a joystick.");
                useKatVR = false;
                return;
            }

            CalibrateOrientation(data);
            Debug.Log("[PlayerController/KAT] ✓ Caminadora KAT inicializada y calibrada. Camina sobre la plataforma.");
        }
        catch (System.DllNotFoundException e)
        {
            Debug.LogError("[PlayerController/KAT] No se cargó KATSDKWarpper.dll: " + e.Message +
                           "\n→ Fallback a joystick.");
            useKatVR = false;
        }
        catch (System.Exception e)
        {
            Debug.LogError("[PlayerController/KAT] Error inicializando KAT: " + e.Message + "\n→ Fallback a joystick.");
            useKatVR = false;
        }
    }

    /// <summary>Serial a usar en GetWalkStatus: el del Inspector si se puso, si no el auto-resuelto.</summary>
    string KatSerial() => string.IsNullOrEmpty(katSerialNumber) ? _resolvedSerial : katSerialNumber;

    /// <summary>
    /// Resuelve el número de serie real de la caminadora (deviceType==1) recorriendo los
    /// dispositivos del SDK. GetWalkStatus necesita el serial real para entregar datos en vivo;
    /// con "" suele devolver una estructura congelada.
    /// </summary>
    void ResolveKatSerial(int deviceCount)
    {
        if (!string.IsNullOrEmpty(katSerialNumber)) { _resolvedSerial = katSerialNumber; return; }

        for (uint i = 0; i < deviceCount; i++)
        {
            try
            {
                var desc = KATNativeSDK.GetDevicesDesc(i);
                Debug.Log($"[PlayerController/KAT] device[{i}]: name='{desc.device}' sn='{desc.serialNumber}' " +
                          $"type={desc.deviceType} (1=caminadora, 2=tracker)");
                if (desc.deviceType == 1 && !string.IsNullOrEmpty(desc.serialNumber))
                {
                    _resolvedSerial = desc.serialNumber;
                    Debug.Log($"[PlayerController/KAT] Serial de caminadora resuelto: '{_resolvedSerial}'");
                    return;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[PlayerController/KAT] GetDevicesDesc({i}) falló: {e.Message}");
            }
        }

        // Fallback: serial del primer dispositivo.
        try { _resolvedSerial = KATNativeSDK.GetDevicesDesc(0).serialNumber; } catch { }
        Debug.Log($"[PlayerController/KAT] Serial fallback: '{_resolvedSerial}'");
    }

    void HandleKatVRLocomotion()
    {
        EnsureCharacterController();
        if (_cc == null) return;

        KATNativeSDK.TreadMillData data = KATNativeSDK.GetWalkStatus(KatSerial());

        // SOLO se exige 'connected'. NO exigir deviceDatas: ese array de structs se marshala por
        // P/Invoke (ByValArray) y con frecuencia vuelve NULL aunque la caminadora esté conectada;
        // si lo exigíamos, la KAT siempre caía a joystick y nunca movía.
        if (!data.connected)
        {
            DiagKat("NO conectada (connected=false) → joystick. ¿KAT Gateway abierto y caminadora encendida?");
            HandleJoystickLocomotion();
            return;
        }

        // Botón de calibración: solo si el SDK reportó deviceDatas. Calibrar en el FLANCO de
        // pulsación (no mientras se mantiene), para que un botón "siempre presionado" no bloquee.
        bool btn = false;
        if (data.deviceDatas != null && data.deviceDatas.Length > 0)
        {
            btn = data.deviceDatas[0].btnPressed;
            if (btn && !_katBtnWasPressed)
            {
                _katBtnWasPressed = true;
                CalibrateOrientation(data);
                Debug.Log("[PlayerController/KAT] Recentrado (botón de calibración).");
                return;
            }
            if (!btn) _katBtnWasPressed = false;
        }

        Quaternion bodyRot = data.bodyRotationRaw
                           * Quaternion.Inverse(Quaternion.Euler(0f, _yawCorrection, 0f));

        Vector3 moveVelocity = bodyRot * data.moveSpeed * katSpeedMultiplier;

        // PARCHE DE SEGURIDAD: Evitar velocidades de caminadora corruptas al inicio
        if (float.IsNaN(moveVelocity.x) || float.IsNaN(moveVelocity.z)) moveVelocity = Vector3.zero;

        // ¿El SDK está entregando datos VIVOS? Si lastUpdateTimePoint avanza pero moveSpeed=0,
        // el dispositivo reporta bien pero NO detecta pasos → calibración/sensores de la KAT.
        // Si lastUpdateTimePoint NO avanza, los datos están congelados/mal marshalados.
        bool datosFrescos = data.lastUpdateTimePoint != _lastKatUpdateTime;
        _lastKatUpdateTime = data.lastUpdateTimePoint;

        DiagKat($"moveSpeed raw=({data.moveSpeed.x:0.000}, {data.moveSpeed.y:0.000}, {data.moveSpeed.z:0.000}) " +
                $"|{data.moveSpeed.magnitude:0.00}|  body={data.bodyRotationRaw.eulerAngles}  " +
                $"updT={data.lastUpdateTimePoint:0.000} fresco={datosFrescos}  btn={btn}  cc={(_cc != null ? _cc.name : "NULL")}");

        _cc.SimpleMove(moveVelocity);
        _usedSimpleMove = true;
        _katActive = true;
    }

    // Diagnóstico KAT throttleado (~1 vez por segundo) para no inundar la consola.
    void DiagKat(string msg)
    {
        if (Time.unscaledTime - _lastKatDiag < 1f) return;
        _lastKatDiag = Time.unscaledTime;
        Debug.Log("[PlayerController/KAT] " + msg);
    }

    void CalibrateOrientation(KATNativeSDK.TreadMillData data)
    {
        if (headCamera == null) return;
        
        // Evitar que la calibración reciba transformadas nulas de la cámara en el frame 0
        if (float.IsNaN(headCamera.transform.position.x) || headCamera.transform.position.sqrMagnitude < 0.001f) return;

        _yawCorrection = data.bodyRotationRaw.eulerAngles.y - headCamera.transform.eulerAngles.y;

        Vector3 pos  = transform.position;
        pos.x        = headCamera.transform.position.x;
        pos.z        = headCamera.transform.position.z;
        
        if (!float.IsNaN(pos.x) && !float.IsNaN(pos.z))
        {
            transform.position = pos;
        }
        _lastKatPosition = transform.position;
    }

    void HandleJoystickLocomotion()
    {
        EnsureCharacterController();
        if (_cc == null) return;

        if (headCamera == null)
        {
            headCamera = Camera.main;
            if (headCamera == null) return;
        }

        Vector2 stick = MoveAction != null ? MoveAction.ReadValue<Vector2>() : Vector2.zero;
        if (stick.sqrMagnitude < 0.001f) return;

        Vector3 forward = Vector3.ProjectOnPlane(headCamera.transform.forward, Vector3.up).normalized;
        Vector3 right   = Vector3.ProjectOnPlane(headCamera.transform.right,   Vector3.up).normalized;

        Vector3 moveDir = forward * stick.y + right * stick.x;

        if (!float.IsNaN(moveDir.x) && !float.IsNaN(moveDir.z))
        {
            _cc.Move(moveDir * walkSpeed * Time.deltaTime);
        }
    }

    void InitSnapTurn()
    {
        if (!enableSnapTurn || snapTurnAngle <= 0f || xrRig == null) return;

        if (moveAction?.asset != null)
        {
            _snapTurnAct = moveAction.asset.FindAction("XRI Right Locomotion/Snap Turn")
                        ?? moveAction.asset.FindAction("XRI Right Locomotion/Turn")
                        ?? moveAction.asset.FindAction("Explorer/SnapTurn");
        }

        if (_snapTurnAct != null) _snapTurnAct.Enable();
    }

    void HandleSnapTurn()
    {
        if (!enableSnapTurn || snapTurnAngle <= 0f || _snapTurnAct == null || xrRig == null) return;

        Vector2 val = _snapTurnAct.ReadValue<Vector2>();
        if (Mathf.Abs(val.x) > snapTurnThreshold)
        {
            if (!_snapTurnHeld)
            {
                Vector3 pivot = headCamera
                    ? new Vector3(headCamera.transform.position.x, xrRig.position.y, headCamera.transform.position.z)
                    : xrRig.position;

                if (!float.IsNaN(pivot.x) && !float.IsNaN(pivot.z))
                {
                    xrRig.RotateAround(pivot, Vector3.up, Mathf.Sign(val.x) * snapTurnAngle);
                }
                _snapTurnHeld = true;
            }
        }
        else
        {
            _snapTurnHeld = false;
        }
    }

    void ApplyGravity()
    {
        EnsureCharacterController();
        if (_cc == null) return;

        if (_isGrounded && _velocity.y < 0f)
            _velocity.y = -2f;   
        else
            _velocity.y += Physics.gravity.y * Time.deltaTime;

        if (!float.IsNaN(_velocity.y))
        {
            _cc.Move(_velocity * Time.deltaTime);
        }
    }

    public void FreezeMovement(bool freeze)
    {
        _frozen = freeze;
    }
}