using System.Collections.Generic;
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
///
/// COMPORTAMIENTO:
///   - El cuerpo sigue la posición XZ de la cámara.
///   - El cuerpo SOLO rota cuando el jugador se mueve (no al girar la cámara).
///   - La Y usa raycast al suelo + suavizado para evitar caídas visuales.
///   - Oculta el mesh de la cabeza del robot para que no tape el visor VR.
///   - Las manos las proveen LeftHandQuestVisual / RightHandQuestVisual del XR rig.
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
    [Tooltip("Oculta la cabeza del robot en VR. Desactivar en modo PC para debug.")]
    public bool hideHeadInVR = true;

    [Header("Rotación del cuerpo")]
    [Range(1f, 30f)]
    [Tooltip("Velocidad de giro del cuerpo cuando el jugador se mueve.")]
    public float rotationSmoothing = 10f;
    [Range(0f, 1f)]
    [Tooltip("Velocidad mínima (m/s) para que el cuerpo gire hacia el movimiento.\n" +
             "Mientras el jugador está quieto o solo gira la cabeza, el cuerpo no rota.")]
    public float movementThreshold = 0.25f;

    [Header("Estabilidad de suelo")]
    [Range(0f, 30f)]
    [Tooltip("Suavizado de la posición Y del cuerpo (Lerp). Evita que caídas bruscas del CC salten visualmente.")]
    public float ySmoothing = 15f;

    [Header("Cuerpo parcial (solo piernas visibles)")]
    [Tooltip("Oculta todo el mesh del robot y muestra solo piernas primitivas animadas.\n" +
             "Las manos las proveen LeftHandQuestVisual/RightHandQuestVisual del XR rig.")]
    public bool hideBodyShowLegs = false;
    [Tooltip("Color de los segmentos de pierna generados.")]
    public Color legColor = new Color(0.15f, 0.15f, 0.20f);

    // Parámetros del Animator (StarterAssetsThirdPerson.controller)
    private static readonly int _animSpeed    = Animator.StringToHash("Speed");
    private static readonly int _animMotion   = Animator.StringToHash("MotionSpeed");
    private static readonly int _animGrounded = Animator.StringToHash("Grounded");

    private CharacterController _cc;
    private Transform           _headBone;
    private float               _smoothedY;   // Y suavizada — evita caídas visuales bruscas

    // Segmentos de pierna: (hueso inicio, hueso fin, radio, GameObject primitivo)
    private readonly List<(Transform a, Transform b, float r, GameObject go)> _limbSegments = new();

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
        _smoothedY = transform.position.y;

        if (hideBodyShowLegs)
            SetupPartialBody();
        else if (hideHeadInVR)
            HideHead();
    }

    void LateUpdate()
    {
        if (avatarRoot == null || xrCamera == null) return;

        MoveAvatarToCamera();
        RotateAvatarWithMovement();
        UpdateAnimation();

        if (hideBodyShowLegs)
            SyncLimbMeshes();
    }

    // ─────────────────────────────────────────────────────────
    //  Posición
    // ─────────────────────────────────────────────────────────

    void MoveAvatarToCamera()
    {
        // XZ: el cuerpo sigue la posición horizontal del visor VR.
        Vector3 camPos = xrCamera.position;

        // Y: usar la base del CharacterController con suavizado.
        // El Lerp amortigua cualquier caída brusca de un frame sin usar raycast
        // (el raycast con mask ~0 golpea contra PCBs y objetos del circuito).
        float targetY = transform.position.y;
        _smoothedY = ySmoothing > 0f
            ? Mathf.Lerp(_smoothedY, targetY, ySmoothing * Time.deltaTime)
            : targetY;

        avatarRoot.position = new Vector3(camPos.x, _smoothedY, camPos.z);
    }

    // ─────────────────────────────────────────────────────────
    //  Rotación del cuerpo
    // ─────────────────────────────────────────────────────────

    void RotateAvatarWithMovement()
    {
        // El cuerpo SOLO gira cuando el jugador se mueve físicamente.
        // Girar la cabeza/cámara sin desplazarse no afecta la orientación del torso.
        Vector3 hVel = new Vector3(_cc.velocity.x, 0f, _cc.velocity.z);
        if (hVel.magnitude < movementThreshold) return;

        Quaternion targetRot = Quaternion.LookRotation(hVel.normalized);
        avatarRoot.rotation = Quaternion.Slerp(
            avatarRoot.rotation, targetRot, rotationSmoothing * Time.deltaTime);
    }

    // ─────────────────────────────────────────────────────────
    //  Animación
    // ─────────────────────────────────────────────────────────

    void UpdateAnimation()
    {
        if (avatarAnimator == null) return;

        float speed = new Vector3(_cc.velocity.x, 0f, _cc.velocity.z).magnitude;

        avatarAnimator.SetFloat(_animSpeed,    speed, 0.1f, Time.deltaTime);
        avatarAnimator.SetFloat(_animMotion,   1f);
        avatarAnimator.SetBool(_animGrounded,  _cc.isGrounded);
    }

    // ─────────────────────────────────────────────────────────
    //  Cuerpo parcial
    // ─────────────────────────────────────────────────────────

    void SetupPartialBody()
    {
        if (avatarRoot == null) return;

        foreach (var smr in avatarRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            smr.enabled = false;
        foreach (var mr in avatarRoot.GetComponentsInChildren<MeshRenderer>(true))
            mr.enabled = false;

        var anim = avatarRoot.GetComponentInChildren<Animator>();
        if (anim == null || !anim.isHuman)
        {
            Debug.LogError("[ExplorerAvatar] hideBodyShowLegs requiere un avatar Humanoid.", this);
            return;
        }

        Transform lUpper  = anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        Transform lLower  = anim.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        Transform lFoot   = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
        Transform lToe    = anim.GetBoneTransform(HumanBodyBones.LeftToes);

        Transform rUpper  = anim.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        Transform rLower  = anim.GetBoneTransform(HumanBodyBones.RightLowerLeg);
        Transform rFoot   = anim.GetBoneTransform(HumanBodyBones.RightFoot);
        Transform rToe    = anim.GetBoneTransform(HumanBodyBones.RightToes);

        if (lUpper && lLower) AddSegment(lUpper, lLower, 0.065f, "Leg_L_Upper");
        if (lLower && lFoot)  AddSegment(lLower, lFoot,  0.050f, "Leg_L_Lower");
        if (lFoot)            AddFootBlock(lFoot, lToe, "Foot_L");

        if (rUpper && rLower) AddSegment(rUpper, rLower, 0.065f, "Leg_R_Upper");
        if (rLower && rFoot)  AddSegment(rLower, rFoot,  0.050f, "Leg_R_Lower");
        if (rFoot)            AddFootBlock(rFoot, rToe, "Foot_R");
    }

    void AddSegment(Transform boneA, Transform boneB, float radius, string goName)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = goName;
        Object.Destroy(go.GetComponent<Collider>());
        ApplyLegMaterial(go);
        _limbSegments.Add((boneA, boneB, radius, go));
    }

    void AddFootBlock(Transform footBone, Transform toeBone, string goName)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = goName;
        Object.Destroy(go.GetComponent<Collider>());
        ApplyLegMaterial(go);
        Transform b = toeBone != null ? toeBone : footBone;
        _limbSegments.Add((footBone, b, -1f, go));
    }

    void ApplyLegMaterial(GameObject go)
    {
        var mr = go.GetComponent<Renderer>();
        if (mr == null) return;
        var mat = new Material(mr.sharedMaterial);
        mat.color = legColor;
        mr.material = mat;
    }

    void SyncLimbMeshes()
    {
        foreach (var (a, b, radius, go) in _limbSegments)
        {
            if (go == null || a == null || b == null) continue;

            Vector3 posA = a.position;
            Vector3 posB = b.position;
            Vector3 dir  = posB - posA;
            float   len  = dir.magnitude;
            if (len < 0.001f) continue;

            go.transform.position = (posA + posB) * 0.5f;
            go.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir.normalized);

            if (radius < 0f)
                go.transform.localScale = new Vector3(0.09f, 0.04f, len);
            else
                go.transform.localScale = new Vector3(radius * 2f, len * 0.5f, radius * 2f);
        }
    }

    // ─────────────────────────────────────────────────────────
    //  Cabeza
    // ─────────────────────────────────────────────────────────

    void HideHead()
    {
        if (avatarRoot == null) return;

        _headBone = FindBone(avatarRoot, headBoneName);
        if (_headBone == null)
        {
            Debug.LogWarning($"[ExplorerAvatar] Hueso '{headBoneName}' no encontrado en {avatarRoot.name}.");
            return;
        }

        _headBone.localScale = Vector3.zero;
        Debug.Log($"[ExplorerAvatar] Cabeza ocultada: {_headBone.name}");
    }

    /// <summary>Activa o desactiva la cabeza en runtime (útil para modo espectador).</summary>
    public void SetHeadVisible(bool visible)
    {
        if (_headBone == null) return;
        _headBone.localScale = visible ? Vector3.one : Vector3.zero;
    }

    void OnDestroy()
    {
        foreach (var (_, _, _, go) in _limbSegments)
            if (go != null) Destroy(go);
        _limbSegments.Clear();
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
