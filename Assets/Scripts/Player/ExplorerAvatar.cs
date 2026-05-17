using UnityEngine;

/// <summary>
/// Conecta el modelo 3D del robot (RobotKyle) al XR rig del Explorador.
///
/// SETUP en la escena Explorador.unity:
///   1. Selecciona el GameObject raíz del Explorador (el que tiene PlayerController).
///   2. Arrastra RobotKyle.prefab como hijo de ese GameObject.
///   3. Añade este script al mismo raíz del Explorador.
///   4. Asigna:
///        xrCamera    → la cámara principal del XR Origin (Main Camera)
///        avatarRoot  → el transform raíz de RobotKyle (el hijo recién añadido)
///   5. headBoneName → "head" (nombre del hueso de la cabeza en RobotKyle)
///
/// QUÉ HACE:
///   - El cuerpo sigue la posición XZ de la cámara (no el Y, para que no flote).
///   - El cuerpo rota hacia donde mira la cámara en el plano horizontal.
///   - Oculta el mesh de la cabeza del robot para que no tape el visor VR.
///   - Reproduce la animación de caminar/idle según si el CharacterController se mueve.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class ExplorerAvatar : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Main Camera del XR Origin (el visor VR del Explorador).")]
    public Transform xrCamera;

    [Tooltip("Raíz del modelo RobotKyle (hijo de este GameObject).")]
    public Transform avatarRoot;

    [Header("Animación")]
    [Tooltip("Animator del RobotKyle. Se detecta automáticamente si queda vacío.")]
    public Animator avatarAnimator;

    [Header("Cabeza")]
    [Tooltip("Nombre del hueso de la cabeza para ocultarlo (evita que tape el visor).")]
    public string headBoneName = "head";
    [Tooltip("Activar para ocultar la cabeza del robot en VR. Desactivar en modo PC para debug.")]
    public bool hideHeadInVR = true;

    [Header("Suavizado")]
    [Range(1f, 30f)]
    [Tooltip("Velocidad con la que el cuerpo rota para seguir la cámara.")]
    public float rotationSmoothing = 10f;

    // Parámetros del Animator (deben coincidir con StarterAssetsThirdPerson.controller)
    private static readonly int _animSpeed  = Animator.StringToHash("Speed");
    private static readonly int _animMotion = Animator.StringToHash("MotionSpeed");

    private CharacterController _cc;
    private Transform           _headBone;

    // ─────────────────────────────────────────────────────────
    //  Lifecycle
    // ─────────────────────────────────────────────────────────

    void Awake()
    {
        _cc = GetComponent<CharacterController>();

        if (xrCamera == null)
        {
            Camera cam = Camera.main ?? FindAnyObjectByType<Camera>();
            if (cam != null)
            {
                xrCamera = cam.transform;
                Debug.Log($"[ExplorerAvatar] xrCamera auto-encontrada: {xrCamera.name}");
            }
        }

        if (avatarRoot == null)
        {
            GameObject robot = GameObject.Find("RobotKyle_Explorer")
                            ?? GameObject.Find("RobotKyle");
            if (robot != null)
            {
                avatarRoot = robot.transform;
                Debug.Log($"[ExplorerAvatar] avatarRoot auto-encontrado: {avatarRoot.name}");
            }
        }

        if (avatarAnimator == null && avatarRoot != null)
            avatarAnimator = avatarRoot.GetComponentInChildren<Animator>();
    }

    void Start()
    {
        if (hideHeadInVR)
            HideHead();
    }

    void LateUpdate()
    {
        if (avatarRoot == null || xrCamera == null) return;

        MoveAvatarToCamera();
        RotateAvatarToCamera();
        UpdateAnimation();
    }

    // ─────────────────────────────────────────────────────────
    //  Posición y rotación
    // ─────────────────────────────────────────────────────────

    void MoveAvatarToCamera()
    {
        // El avatar sigue el XZ de la cámara; Y viene del CharacterController (gravedad).
        Vector3 target = xrCamera.position;
        target.y = transform.position.y;
        avatarRoot.position = target;
    }

    void RotateAvatarToCamera()
    {
        Vector3 forward = Vector3.ProjectOnPlane(xrCamera.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.01f) return;

        Quaternion targetRot = Quaternion.LookRotation(forward);
        avatarRoot.rotation  = Quaternion.Slerp(
            avatarRoot.rotation, targetRot, rotationSmoothing * Time.deltaTime);
    }

    // ─────────────────────────────────────────────────────────
    //  Animación
    // ─────────────────────────────────────────────────────────

    void UpdateAnimation()
    {
        if (avatarAnimator == null) return;

        // Velocidad normalizada: 0 = idle, 1 = caminar/correr
        float speed = new Vector3(_cc.velocity.x, 0f, _cc.velocity.z).magnitude;
        float normalized = Mathf.Clamp01(speed / 2f); // 2 m/s = velocidad máxima de caminar

        avatarAnimator.SetFloat(_animSpeed,  normalized, 0.1f, Time.deltaTime);
        avatarAnimator.SetFloat(_animMotion, 1f);
    }

    // ─────────────────────────────────────────────────────────
    //  Cabeza
    // ─────────────────────────────────────────────────────────

    void HideHead()
    {
        if (avatarRoot == null) return;

        // Buscar el hueso por nombre (búsqueda recursiva)
        _headBone = FindBone(avatarRoot, headBoneName);
        if (_headBone == null)
        {
            Debug.LogWarning($"[ExplorerAvatar] Hueso '{headBoneName}' no encontrado en {avatarRoot.name}. " +
                             "El mesh de la cabeza no se ocultará.");
            return;
        }

        // Escalar a cero es la forma más simple de ocultar un hueso sin tocar los renderers
        _headBone.localScale = Vector3.zero;
        Debug.Log($"[ExplorerAvatar] Cabeza ocultada: {_headBone.name}");
    }

    /// <summary>Activa o desactiva la cabeza en runtime (útil para modo espectador).</summary>
    public void SetHeadVisible(bool visible)
    {
        if (_headBone == null) return;
        _headBone.localScale = visible ? Vector3.one : Vector3.zero;
    }

    // ─────────────────────────────────────────────────────────
    //  Helper
    // ─────────────────────────────────────────────────────────

    static Transform FindBone(Transform root, string boneName)
    {
        string lower = boneName.ToLowerInvariant();
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name.ToLowerInvariant().Contains(lower))
                return t;
        }
        return null;
    }
}
