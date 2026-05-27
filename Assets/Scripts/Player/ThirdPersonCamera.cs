using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform target;

    [Header("Cabeza del robot")]
    [Tooltip("Arrastra aquí el Transform del hueso 'Head' del TechnicianRobot. Se auto-busca si queda vacío.")]
    public Transform headBone;
    [Tooltip("Nombre del hueso para auto-búsqueda (case-insensitive).")]
    public string headBoneName = "Head";
    [Tooltip("Offset en espacio local del hueso: sube la cámara a los ojos (eje Y).")]
    public Vector3 headOffset = new Vector3(0f, 0.08f, 0.05f);

    [Header("Fallback (sin hueso)")]
    [Tooltip("Altura de los ojos si no se encuentra el hueso de cabeza.")]
    public float eyeHeight = 1.65f;

    [Header("Cámara")]
    public float sensitivity = 2f;
    public float minPitch    = -80f;
    public float maxPitch    =  80f;

    private float _pitch       = 0f;
    private bool  _cursorFree  = false;
    private bool  _started     = false;

    private Renderer[] _bodyRenderers;

    public float Yaw => target != null ? target.eulerAngles.y : transform.eulerAngles.y;

    void Awake()
    {
        // Si la cámara quedó guardada como hija de un objeto que no es Walker_PC
        // (p.ej. del hueso Head del robot tras guardar en Play Mode), nos desparentamos.
        if (transform.parent != null)
        {
            bool parentIsWalker = transform.parent.GetComponentInParent<TechnicianMover>() != null;
            if (!parentIsWalker)
                transform.SetParent(null, worldPositionStays: true);
        }
    }

    void OnEnable()
    {
        if (_started) LockCursor(true);
        GetComponent<Camera>().enabled = true;
        SetBodyVisible(false);
    }

    void OnDisable()
    {
        LockCursor(false);
        GetComponent<Camera>().enabled = false;
        SetBodyVisible(true);
    }

    void Start()
    {
        _started = true;

        if (target == null)
        {
            var mover = FindAnyObjectByType<TechnicianMover>();
            if (mover != null) target = mover.transform;
        }

        if (target != null)
        {
            transform.SetParent(target);
            transform.localRotation = Quaternion.identity;
        }

        // Auto-buscar el hueso de cabeza en el TechnicianRobot
        if (headBone == null && !string.IsNullOrEmpty(headBoneName))
        {
            var robot = GameObject.Find("TechnicianRobot");
            if (robot != null)
                headBone = FindChildByName(robot.transform, headBoneName);

            if (headBone == null && target != null)
                headBone = FindChildByName(target, headBoneName);
        }

        // Cachear renderers del robot para ocultarlos en primera persona
        CacheBodyRenderers();
        SetBodyVisible(false);

        // Posición inicial: hueso o fallback eyeHeight
        ApplyHeadPosition();

        // Cursor: bloquear ahora que ya tenemos contexto válido
        LockCursor(true);

        Debug.Log(headBone != null
            ? $"[ThirdPersonCamera] Anclada al hueso '{headBone.name}'."
            : $"[ThirdPersonCamera] Hueso '{headBoneName}' no encontrado — usando eyeHeight={eyeHeight}.");
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            _cursorFree = !_cursorFree;
            LockCursor(!_cursorFree);
        }
    }

    void LateUpdate()
    {
        // Actualizar posición: headBone tiene prioridad sobre target para no requerir Walker_PC
        bool hasTracking = (headBone != null && headBone.gameObject.activeInHierarchy)
                        || (target != null);
        if (!hasTracking) return;

        ApplyHeadPosition();

        if (_cursorFree) return;

        var mouse = Mouse.current;
        if (mouse != null)
        {
            _pitch -= mouse.delta.y.ReadValue() * sensitivity * 0.1f;
            _pitch  = Mathf.Clamp(_pitch, minPitch, maxPitch);
        }

        transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    void ApplyHeadPosition()
    {
        if (headBone != null && headBone.gameObject.activeInHierarchy)
        {
            // Usar posición mundial del hueso + offset (sin heredar la rotación del hueso
            // para que el mouse controle 100% la orientación de la cámara).
            transform.position = headBone.position
                               + headBone.up      * headOffset.y
                               + headBone.forward * headOffset.z
                               + headBone.right   * headOffset.x;
        }
        else if (target != null)
        {
            // Fallback: altura fija relativa al Walker_PC
            transform.localPosition = new Vector3(0f, eyeHeight, 0f);
        }
    }

    void CacheBodyRenderers()
    {
        var robot = GameObject.Find("TechnicianRobot");
        if (robot != null)
            _bodyRenderers = robot.GetComponentsInChildren<Renderer>(true);
    }

    void SetBodyVisible(bool visible)
    {
        if (_bodyRenderers == null) return;
        var mode = visible
            ? UnityEngine.Rendering.ShadowCastingMode.On
            : UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        foreach (var r in _bodyRenderers)
            if (r != null) r.shadowCastingMode = mode;
    }

    static Transform FindChildByName(Transform root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (string.Equals(t.name, name, System.StringComparison.OrdinalIgnoreCase))
                return t;
        return null;
    }

    static void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible   = !locked;
    }
}
