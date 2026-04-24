using UnityEngine;

/// <summary>
/// Simula la mano VR del Explorador usando el mouse.
/// Permite agarrar, mover y soltar componentes sin Meta Quest.
///
/// CONTROLES:
///   Click izquierdo en un objeto  → lo agarra (sigue al mouse)
///   Click izquierdo en vacío      → suelta el objeto
///   Scroll                        → acercar/alejar el objeto agarrado
///
/// SETUP:
///   1. Agregar este script a la ExplorerCamera
///   2. Asegurarse que los objetos a agarrar tienen Collider + Rigidbody
///   3. Los prefabs de componentes entregados necesitan Rigidbody
///
/// NOTA: Este script es SOLO para testing sin VR.
///       Eliminarlo antes de la build final con Meta Quest.
/// </summary>
public class MouseGrabSimulator : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Distancia máxima del raycast para detectar objetos.")]
    public float maxGrabDistance = 10f;

    [Tooltip("Distancia inicial del objeto agarrado respecto a la cámara.")]
    public float holdDistance = 1.5f;

    [Tooltip("Velocidad con la que el objeto sigue al mouse.")]
    public float followSpeed = 15f;

    [Tooltip("Velocidad del scroll para acercar/alejar.")]
    public float scrollSpeed = 0.5f;

    [Header("Movimiento de cámara")]
    [Tooltip("Velocidad de rotación con click derecho.")]
    public float lookSpeed = 2f;

    [Tooltip("Velocidad de movimiento con WASD.")]
    public float moveSpeed = 3f;

    [Header("Estado (solo lectura)")]
    [SerializeField] private GameObject _heldObject;
    [SerializeField] private bool _isHolding = false;

    private Camera _cam;
    private Rigidbody _heldRb;
    private float _currentDistance;
    private float _rotX, _rotY;

    void Start()
    {
        _cam = GetComponent<Camera>();
        if (_cam == null) _cam = Camera.main;

        _rotX = transform.eulerAngles.y;
        _rotY = transform.eulerAngles.x;
    }

    void Update()
    {
        HandleCameraMovement();
        HandleGrab();
        HandleScroll();
    }

    void FixedUpdate()
    {
        MoveHeldObject();
    }

    // ─────────────────────────────────────────────
    //  Agarrar y soltar
    // ─────────────────────────────────────────────

    void HandleGrab()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        if (_isHolding)
        {
            // Soltar el objeto
            Release();
        }
        else
        {
            // Intentar agarrar
            TryGrab();
        }
    }

    void TryGrab()
    {
        Ray ray = _cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, maxGrabDistance))
        {
            GameObject target = hit.collider.gameObject;

            // Solo agarrar objetos con Rigidbody (componentes entregados)
            Rigidbody rb = target.GetComponent<Rigidbody>();
            if (rb == null)
            {
                // Intentar en el padre (por si el collider está en un hijo)
                rb = target.GetComponentInParent<Rigidbody>();
                if (rb != null) target = rb.gameObject;
            }

            if (rb != null)
            {
                _heldObject = target;
                _heldRb = rb;
                _isHolding = true;
                _currentDistance = holdDistance;

                // Desactivar gravedad mientras está agarrado
                _heldRb.useGravity = false;
                _heldRb.linearDamping = 10f;

                Debug.Log($"[MouseGrab] Agarrado: {target.name}");
            }
        }
    }

    void Release()
    {
        if (_heldRb != null)
        {
            _heldRb.useGravity = true;
            _heldRb.linearDamping = 0f;
            Debug.Log($"[MouseGrab] Soltado: {_heldObject.name}");
        }

        _heldObject = null;
        _heldRb = null;
        _isHolding = false;
    }

    // ─────────────────────────────────────────────
    //  Mover objeto agarrado
    // ─────────────────────────────────────────────

    void MoveHeldObject()
    {
        if (!_isHolding || _heldRb == null || _cam == null) return;

        // Posición objetivo: frente a la cámara en la dirección del mouse
        Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
        Vector3 targetPos = ray.origin + ray.direction * _currentDistance;

        // Mover con física (suave)
        Vector3 force = (targetPos - _heldRb.position) * followSpeed;
        _heldRb.linearVelocity = force;
    }

    void HandleScroll()
    {
        if (!_isHolding) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            _currentDistance += scroll * scrollSpeed;
            _currentDistance = Mathf.Clamp(_currentDistance, 0.3f, 5f);
        }
    }

    // ─────────────────────────────────────────────
    //  Movimiento de cámara (click derecho + WASD)
    // ─────────────────────────────────────────────

    void HandleCameraMovement()
    {
        // Rotar con click derecho
        if (Input.GetMouseButton(1))
        {
            _rotX += Input.GetAxis("Mouse X") * lookSpeed;
            _rotY -= Input.GetAxis("Mouse Y") * lookSpeed;
            _rotY = Mathf.Clamp(_rotY, -80f, 80f);
            transform.rotation = Quaternion.Euler(_rotY, _rotX, 0f);
        }

        // Mover con WASD
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        float up = 0f;

        if (Input.GetKey(KeyCode.E)) up = 1f;
        if (Input.GetKey(KeyCode.Q)) up = -1f;

        Vector3 move = transform.right * h + transform.forward * v + Vector3.up * up;
        transform.position += move * moveSpeed * Time.deltaTime;
    }
}