using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Movimiento del Técnico en 1ª persona. SOLO mueve el rig con W/A/S/D; el GIRO con el mouse lo
/// maneja <see cref="ThirdPersonCamera"/> (que rota este mismo rig en yaw). Por eso el movimiento es
/// relativo al CUERPO: transform.forward apunta EXACTAMENTE a donde mira la cámara.
///
/// El rig es una instancia de RobotKyle (StarterAssets), que trae SU PROPIO controlador de movimiento
/// + inputs + WalkerPC + Rigidbody. En Start se neutraliza todo eso para que este sea el ÚNICO mover
/// (si no, hay DOBLE movimiento → "va más rápido y raro, peor al girar la cámara").
/// </summary>
public class TechnicianMover : MonoBehaviour
{
    [Header("Velocidad")]
    [Tooltip("Velocidad al caminar. Igual en las 4 direcciones (W/A/S/D).")]
    public float walkSpeed   = 5f;
    [Tooltip("Velocidad con Shift (sprint).")]
    public float sprintSpeed = 8f;

    [Header("Suavizado")]
    [Tooltip("Tiempo de aceleración/frenado en segundos. 0 = respuesta instantánea.")]
    public float accelerationTime = 0.08f;

    // Caída — constantes (definidas aquí, NO en el Inspector).
    const float GRAVITY        = -20f;   // m/s² mientras está en el aire
    const float GROUNDED_STICK = -2f;    // pequeña fuerza hacia abajo para pegarse al piso
    const float MAX_FALL       = -50f;   // tope de velocidad de caída

    CharacterController _cc;
    Vector2 _input;        // WASD suavizado
    Vector2 _inputVel;     // referencia interna del SmoothDamp
    float   _verticalVel;  // velocidad vertical (gravedad)
    bool    _locked;

    public bool IsPositionLocked => _locked;
    public bool IsRotationLocked => false;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        if (_cc == null)
        {
            _cc = gameObject.AddComponent<CharacterController>();
            _cc.height = 1.8f;
            _cc.radius = 0.3f;
            _cc.center = new Vector3(0f, 0.9f, 0f);
        }
        _cc.stepOffset = 0.5f;   // trepa zócalos/bordes bajos sin atascarse
    }

    void Start() => NeutralizarMoversEnConflicto();

    void Update()
    {
        if (_locked || PauseMenu.IsPaused) return;

        // 1. Leer W/A/S/D (normalizado: la diagonal NO va más rápido).
        Vector2 raw = Vector2.zero;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    raw.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  raw.y -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) raw.x += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  raw.x -= 1f;
        }
        if (raw.sqrMagnitude > 1f) raw.Normalize();
        _input = Vector2.SmoothDamp(_input, raw, ref _inputVel, accelerationTime);

        // 2. Dirección relativa al CUERPO (ThirdPersonCamera lo gira con la vista).
        //    transform.forward/right son ejes horizontales unitarios → velocidad constante.
        bool    sprint     = kb != null && kb.leftShiftKey.isPressed;
        float   speed      = sprint ? sprintSpeed : walkSpeed;
        Vector3 horizontal = (transform.right * _input.x + transform.forward * _input.y) * speed;

        // 3. Gravedad.
        if (_cc.isGrounded && _verticalVel < 0f)
            _verticalVel = GROUNDED_STICK;
        else
            _verticalVel = Mathf.Max(_verticalVel + GRAVITY * Time.deltaTime, MAX_FALL);

        // 4. Mover (una sola vez por frame).
        _cc.Move((horizontal + Vector3.up * _verticalVel) * Time.deltaTime);
    }

    /// <summary>Bloquea/desbloquea el movimiento. Lo usa WorkstationSeat al sentarse/levantarse.</summary>
    public void LockPosition(bool locked)
    {
        _locked = locked;
        if (locked) { _input = Vector2.zero; _inputVel = Vector2.zero; _verticalVel = 0f; }
    }

    /// <summary>
    /// Apaga TODO lo que pueda mover/girar el rig aparte de este script. El RobotKyle de StarterAssets
    /// trae su propio ThirdPersonController + StarterAssetsInputs + PlayerInput + BasicRigidBodyPush, y
    /// el rig arrastra el viejo WalkerPC + Rigidbody + CapsuleCollider. Sin esto hay DOBLE mover.
    /// </summary>
    void NeutralizarMoversEnConflicto()
    {
        // Scripts de movimiento/input de StarterAssets y el viejo WalkerPC — por NOMBRE de tipo
        // (no referenciamos sus clases para no depender de su assembly).
        foreach (var b in GetComponents<Behaviour>())
        {
            if (b == null || b == this) continue;
            switch (b.GetType().Name)
            {
                case "ThirdPersonController":
                case "StarterAssetsInputs":
                case "BasicRigidBodyPush":
                case "PlayerInput":
                case "WalkerPC":
                    b.enabled = false;
                    break;
            }
        }

        // Rigidbody: que la física no empuje ni frene al CharacterController.
        if (TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = true;

        // CapsuleCollider hermano: estorba (la cápsula del CharacterController ya basta).
        if (TryGetComponent<CapsuleCollider>(out var cap)) cap.enabled = false;

        // El CharacterController NO debe chocar con los colliders del PROPIO cuerpo (lo frenan).
        foreach (var col in GetComponentsInChildren<Collider>(true))
        {
            if (col == _cc || col is CharacterController) continue;
            Physics.IgnoreCollision(_cc, col, true);
        }
    }
}
