using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controlador de movimiento del Técnico en modo PC.
/// Usa CharacterController — maneja pendientes y escalones automáticamente.
///
/// SETUP (se aplica automáticamente con el WalkerPCSetupTool):
///   Walker_PC ─► TechnicianMover + CharacterController
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class TechnicianMover : MonoBehaviour
{
    // ─── Inspector ────────────────────────────────────────────────────────────

    [Header("Velocidad")]
    [Tooltip("Velocidad al caminar. Es la MISMA para adelante, atrás y los lados (W/A/S/D).")]
    public float walkSpeed   = 5f;
    [Tooltip("Velocidad con Shift (sprint). También igual en todas las direcciones.")]
    public float sprintSpeed = 8f;

    [Header("Suavizado")]
    [Tooltip("Tiempo de aceleración y frenado en segundos. Valor bajo = respuesta inmediata.")]
    public float accelerationTime = 0.08f;

    [Header("Gravedad")]
    public float gravity = -15f;
    [Tooltip("Límite máximo de velocidad de caída para no atravesar el suelo por aceleración infinita.")]
    public float terminalVelocity = -50f;

    [Header("Mouse")]
    public float mouseSensitivity = 2f;

    [Header("Cámara de referencia (adelante/atrás de WASD)")]
    [Tooltip("Cámara por la que mira el jugador; define a dónde es 'adelante' (W). Si lo dejas " +
             "vacío usa Camera.main. Si W va al revés, arrastra aquí la cámara correcta de la vista.")]
    public Transform cameraOverride;
    [Tooltip("Invierte adelante/atrás y los lados (giro de 180°). Úsalo si W manda hacia atrás y no " +
             "puedes/quieres asignar la cámara correcta. Ajustable en vivo en Play.")]
    public bool invertirDireccion = false;

    [Header("Calibración de dirección")]
    [Tooltip("Gira la base de WASD en GRADOS hasta que W vaya EXACTO hacia donde apunta la cámara. " +
             "Hay un desfase fijo entre el forward de la cámara y la vista real. Ajústalo EN PLAY: " +
             "prueba 45, -45, 90, -90... (suele ser múltiplo de 45). Cuando W vaya recto a la vista, " +
             "ese es tu valor; déjalo puesto.")]
    public float cameraYawOffset = 0f;

    // ─── Estado interno ───────────────────────────────────────────────────────

    private CharacterController _cc;
    private float     _yaw;
    
    // Suavizamos el input 2D (WASD) en lugar del vector de velocidad 3D global para evitar el "patinaje"
    private Vector2   _smoothedInput;  
    private Vector2   _inputSmoothVel; 
    
    private float     _verticalVel; // velocidad vertical acumulada por la gravedad
    private bool      _locked;
    private Transform _camT;        // cámara por la que mira el jugador (base del movimiento)

    // ─── API pública ──────────────────────────────────────────────────────────

    public bool IsPositionLocked => _locked;
    public bool IsRotationLocked => false;

    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

    void Awake()
    {
        _yaw = transform.eulerAngles.y;

        // El rig del Técnico es una instancia de RobotKyle, que NO trae CharacterController.
        // Sin él, este mover lanzaba NullReference cada frame y NUNCA movía (movía el WalkerPC,
        // lento). Si falta, lo creamos y configuramos aquí → TechnicianMover se vuelve funcional
        // y autosuficiente aunque el rig esté mal montado en la escena.
        _cc = GetComponent<CharacterController>();
        if (_cc == null)
        {
            _cc = gameObject.AddComponent<CharacterController>();
            _cc.height = 1.8f;
            _cc.radius = 0.3f;
            _cc.center = new Vector3(0f, 0.9f, 0f);
            _cc.slopeLimit = 45f;
        }
        // Siempre (CC nuevo o existente): trepa obstáculos bajos (zócalos, bases de muebles,
        // bordes de piso) sin quedarse raspando contra ellos.
        _cc.stepOffset = 0.5f;
    }

    void Start()
    {
        // Este es el ÚNICO mover. El rig arrastra componentes físicos DUPLICADOS del viejo WalkerPC
        // (Rigidbody NO-kinemático + CapsuleCollider) que PELEAN con el CharacterController y lo dejan
        // lento/pegajoso ("avanza lento en todas las direcciones; al girar la cámara va rápido").
        // Se neutraliza en Start —no en Awake— para correr DESPUÉS de WalkerPC.Awake (que vuelve el
        // Rigidbody no-kinemático); así el fix no se anula por el orden de ejecución de los Awake.
        var walker = GetComponent<WalkerPC>();
        if (walker != null) walker.enabled = false;

        // El Rigidbody no debe simularse: que la física no lo empuje ni lo frene.
        var rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        // El CapsuleCollider hermano BLOQUEA al CharacterController (no se ignora por estar en el
        // mismo GO). El CharacterController ya aporta su propia cápsula → este sobra y estorba.
        var cap = GetComponent<CapsuleCollider>();
        if (cap != null) cap.enabled = false;

        // El CharacterController NO debe chocar con los colliders del PROPIO rig (cuerpo de RobotKyle:
        // pecho, brazos, etc.). Esos colliders-hijo lo frenan al avanzar (W lento, S rápido). Se
        // ignoran las colisiones CC↔collider-hijo (el CC sigue chocando con el ENTORNO). CharacterController
        // hereda de Collider, así que Physics.IgnoreCollision lo acepta.
        // El CharacterController ya no choca con los colliders del PROPIO rig.
        foreach (var col in GetComponentsInChildren<Collider>(true))
        {
            if (col == _cc || col is CharacterController) continue;
            Physics.IgnoreCollision(_cc, col, true);
        }
    }

    void Update()
    {
        if (_locked || PauseMenu.IsPaused) return;
        ApplyMouseYaw();
        ApplyMovement();
    }

    // ─── Movimiento ───────────────────────────────────────────────────────────

    void ApplyMouseYaw()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;
        
        _yaw += mouse.delta.x.ReadValue() * mouseSensitivity * GameSettings.MouseSensitivity * 0.1f;
        transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
    }

    void ApplyMovement()
    {
        // ── 1. Leer input de teclado (Deseo de movimiento) ─────────────────────
        float h = 0f, v = 0f;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  h -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  v -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    v += 1f;
        }

        Vector2 targetInput = new Vector2(h, v);
        // Evitar correr más rápido moviéndose en diagonal
        if (targetInput.sqrMagnitude > 1f) targetInput.Normalize(); 

        bool sprint = kb != null && kb.leftShiftKey.isPressed;
        float speed = sprint ? sprintSpeed : walkSpeed;

        // ── 2. Suavizar el Input (Fix del efecto "Patinaje") ───────────────────
        _smoothedInput = Vector2.SmoothDamp(
            _smoothedInput, targetInput, ref _inputSmoothVel, accelerationTime);

        // ── 3. Direcciones relativas a la CÁMARA que ves (no al cuerpo) ────────
        // El cuerpo del rig y la cámara (PlayerCameraRoot) tienen un DESFASE de yaw, así que usar
        // transform.forward manda W a ~45° de donde miras. Usamos el forward de la CÁMARA → W/A/S/D
        // van EXACTAMENTE según la vista. Se proyecta al plano (mirar arriba/abajo no afecta) y se
        // NORMALIZA (velocidad capada a walkSpeed, igual en todas las direcciones). En 1ª persona el
        // cuerpo está oculto, así que no importa que el modelo apunte distinto a la cámara.
        Transform camRef = LookCamera();
        Vector3 fwd = Vector3.ProjectOnPlane(camRef.forward, Vector3.up);
        fwd = fwd.sqrMagnitude > 1e-4f ? fwd.normalized : transform.forward;

        // Calibración: corrige el desfase fijo entre el forward de la cámara y la vista real.
        if (Mathf.Abs(cameraYawOffset) > 0.01f)
            fwd = Quaternion.Euler(0f, cameraYawOffset, 0f) * fwd;

        // 'right' SIEMPRE perpendicular a 'fwd' en el plano → base ortonormal exacta (A/D correctos).
        Vector3 right = Vector3.Cross(Vector3.up, fwd);

        if (invertirDireccion) { fwd = -fwd; right = -right; }

        // ── 4. Generar Vector de Movimiento Final ──────────────────────────────
        Vector3 moveDirection = (right * _smoothedInput.x + fwd * _smoothedInput.y) * speed;

        // ── 5. Gravedad Segura ─────────────────────────────────────────────────
        if (_cc.isGrounded && _verticalVel < 0f)
        {
            _verticalVel = -2f;   // Pequeño valor negativo para mantenerlo firmemente pegado al suelo
        }
        else
        {
            _verticalVel += gravity * Time.deltaTime;
            _verticalVel = Mathf.Max(_verticalVel, terminalVelocity); // Tope de caída
        }

        // ── 6. Aplicar Movimiento ──────────────────────────────────────────────
        _cc.Move((moveDirection + Vector3.up * _verticalVel) * Time.deltaTime);
    }

    /// <summary>
    /// Cámara por la que el jugador mira al caminar (define el "adelante" de WASD).
    /// </summary>
    Transform LookCamera()
    {
        if (cameraOverride != null) return cameraOverride;
        if (_camT == null)
        {
            // ¡LA CORRECCIÓN CRÍTICA ESTÁ AQUÍ!
            // 1. Priorizamos buscar la cámara del Técnico (ThirdPersonCamera).
            // Si usamos Camera.main primero, Unity atrapa el visor VR del Explorador
            // y tus teclas WASD se torcerán siguiendo la cabeza del otro jugador.
            var tpc = FindAnyObjectByType<ThirdPersonCamera>();
            if (tpc != null) 
            {
                _camT = tpc.transform;
            }
            // 2. Fallback a MainCamera SOLO si no hay ThirdPersonCamera
            else if (Camera.main != null) 
            {
                _camT = Camera.main.transform;
            }
        }
        return _camT != null ? _camT : transform;
    }

    // ─── API para WorkstationSeat ─────────────────────────────────────────────

    /// <summary>
    /// Bloquea (true) o desbloquea (false) el movimiento.
    /// WorkstationSeat llama esto al sentarse y levantarse.
    /// </summary>
    public void LockPosition(bool locked)
    {
        _locked = locked;
        if (locked)
        {
            _smoothedInput  = Vector2.zero;
            _inputSmoothVel = Vector2.zero;
            _verticalVel    = 0f;
        }
    }

    public void LockRotation(bool locked) { }
}