using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class WalkerPC : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed        = 3f;
    public float mouseSensitivity = 2f;

    private Rigidbody _rb;
    private bool      _posLocked;
    private float     _yaw;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.isKinematic            = false;
        _rb.useGravity             = false;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        _rb.constraints            = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        _yaw = transform.eulerAngles.y;

        // Los Rigidbodies de los hijos (avatar, ragdoll) deben ser kinematic para
        // no pelear con este Rigidbody principal ni desplazar la cámara.
        foreach (var rb in GetComponentsInChildren<Rigidbody>())
            if (rb != _rb) rb.isKinematic = true;
    }

    void Update()
    {
        if (_posLocked) return;

        var mouse = Mouse.current;
        if (mouse != null)
            _yaw += mouse.delta.x.ReadValue() * mouseSensitivity * 0.1f;
        // La rotación visual se aplica en FixedUpdate via MoveRotation.
        // Para suavizado entre pasos de física activa Rigidbody.interpolation = Interpolate en el Inspector.
    }

    void FixedUpdate()
    {
        if (_posLocked) return;

        // Sincronizar el Rigidbody con la rotación visual para que las colisiones
        // usen la orientación correcta.
        _rb.MoveRotation(Quaternion.Euler(0f, _yaw, 0f));
        HandleMove();
    }

    void HandleMove()
    {
        float h = 0f, v = 0f;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  h -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  v -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    v += 1f;
        }

        // Dirección relativa a la cámara: W=adelante, S=atrás, A=izquierda, D=derecha
        // desde el punto de vista de quien mira la pantalla.
        Vector3 fwd, right;
        var cam = Camera.main;
        if (cam != null && cam.enabled)
        {
            fwd = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up);
            if (fwd.sqrMagnitude < 0.001f) fwd = transform.forward;
            else fwd.Normalize();
            right = Vector3.ProjectOnPlane(cam.transform.right, Vector3.up).normalized;
            _yaw  = Quaternion.LookRotation(fwd).eulerAngles.y; // mantener _yaw en sinc
        }
        else
        {
            var rot = Quaternion.Euler(0f, _yaw, 0f);
            fwd   = rot * Vector3.forward;
            right = rot * Vector3.right;
        }
        Vector3 dir = right * h + fwd * v;

        float vy = _rb.linearVelocity.y;
        if (dir.sqrMagnitude > 0.001f)
        {
            dir = dir.normalized;
            _rb.linearVelocity = new Vector3(dir.x * moveSpeed, vy, dir.z * moveSpeed);
        }
        else
        {
            _rb.linearVelocity = new Vector3(0f, vy, 0f);
        }
    }

    public void LockPosition(bool locked)
    {
        _posLocked = locked;

        if (locked)
        {
            // FreezeAll: la física NO puede mover ni rotar este Rigidbody.
            // Zerear velocidad evita que el impulso previo se aplique antes de
            // que los constraints surtan efecto en el siguiente paso de física.
            _rb.constraints    = RigidbodyConstraints.FreezeAll;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
        else
        {
            _rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        }
    }

    public void LockRotation(bool locked) { }
    public bool IsPositionLocked => _posLocked;
    public bool IsRotationLocked => false;
}
