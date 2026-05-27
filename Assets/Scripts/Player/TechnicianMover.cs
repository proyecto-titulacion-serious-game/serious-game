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
    public float walkSpeed   = 3f;
    public float sprintSpeed = 6f;

    [Header("Suavizado")]
    [Tooltip("Tiempo de aceleración y frenado en segundos. Valor bajo = respuesta inmediata.")]
    public float accelerationTime = 0.08f;

    [Header("Gravedad")]
    public float gravity = -15f;

    [Header("Mouse")]
    public float mouseSensitivity = 2f;

    // ─── Estado interno ───────────────────────────────────────────────────────

    private CharacterController _cc;
    private float   _yaw;
    private Vector3 _horizontal;  // velocidad horizontal actual (suavizada)
    private Vector3 _smoothRef;   // referencia interna para SmoothDamp
    private float   _verticalVel; // velocidad vertical acumulada por la gravedad
    private bool    _locked;

    // ─── API pública ──────────────────────────────────────────────────────────

    public bool IsPositionLocked => _locked;
    public bool IsRotationLocked => false;

    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

    void Awake()
    {
        _cc  = GetComponent<CharacterController>();
        _yaw = transform.eulerAngles.y;
    }

    void Update()
    {
        if (_locked) return;
        ApplyMouseYaw();
        ApplyMovement();
    }

    // ─── Movimiento ───────────────────────────────────────────────────────────

    void ApplyMouseYaw()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;
        _yaw += mouse.delta.x.ReadValue() * mouseSensitivity * 0.1f;
        transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
    }

    void ApplyMovement()
    {
        // ── Leer input de teclado ──────────────────────────────────────────────
        float h = 0f, v = 0f;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  h -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  v -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    v += 1f;
        }

        // ── Velocidad objetivo ─────────────────────────────────────────────────
        bool sprint = kb != null && kb.leftShiftKey.isPressed;
        float speed = sprint ? sprintSpeed : walkSpeed;

        Vector3 wish = transform.right * h + transform.forward * v;
        if (wish.sqrMagnitude > 1f) wish.Normalize();

        // SmoothDamp: aceleración y frenado suave sin deslizamiento brusco
        _horizontal = Vector3.SmoothDamp(
            _horizontal, wish * speed, ref _smoothRef, accelerationTime);

        // ── Gravedad ───────────────────────────────────────────────────────────
        if (_cc.isGrounded && _verticalVel < 0f)
            _verticalVel = -2f;   // pequeño valor negativo para mantener grounded
        else
            _verticalVel += gravity * Time.deltaTime;

        // ── Mover ──────────────────────────────────────────────────────────────
        _cc.Move((_horizontal + Vector3.up * _verticalVel) * Time.deltaTime);
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
            _horizontal  = Vector3.zero;
            _verticalVel = 0f;
        }
    }

    public void LockRotation(bool locked) { }
}
