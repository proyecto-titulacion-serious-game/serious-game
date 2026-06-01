using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform target;

    [Header("Cabeza del robot")]
    [Tooltip("Arrastra aquí el Transform del hueso 'Head' del TechnicianRobot.")]
    public Transform headBone;
    public string headBoneName = "Head";
    public Vector3 headOffset = new Vector3(0f, 0.08f, 0.05f);

    [Header("Fallback (sin hueso)")]
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
        transform.SetParent(null);
        CacheBodyRenderers();
    }

    void Start()
    {
        LockCursor(true);
        if (headBone == null && target != null)
            headBone = FindChildByName(target, headBoneName);

        SetBodyVisible(false);
        _started = true;
    }

    // CORRECCIÓN: Cambiado de Update a LateUpdate para evitar Jitter (temblor) 
    // al seguir los huesos de la animación del Técnico.
    void LateUpdate() 
    {
        if (!_started) return;

        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame)
        {
            _cursorFree = !_cursorFree;
            LockCursor(!_cursorFree);
        }

        if (!_cursorFree)
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 delta = mouse.delta.ReadValue();
                _pitch -= delta.y * sensitivity * 0.1f;
                _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

                if (target != null)
                {
                    float yaw = target.eulerAngles.y + delta.x * sensitivity * 0.1f;
                    target.rotation = Quaternion.Euler(0f, yaw, 0f);
                }
            }
        }

        transform.rotation = Quaternion.Euler(_pitch, Yaw, 0f);

        if (headBone != null)
        {
            transform.position = headBone.position
                               + headBone.up      * headOffset.y
                               + headBone.forward * headOffset.z
                               + headBone.right   * headOffset.x;
        }
        else if (target != null)
        {
            transform.position = target.position + new Vector3(0f, eyeHeight, 0f);
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