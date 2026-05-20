using UnityEngine;

/// Conecta los brazos del RobotKyle a los controladores VR usando Two-Bone IK.
/// Usa HumanBodyBones en lugar de búsqueda por nombre para ser compatible con
/// cualquier prefijo del FBX (ej. "mixamorig:LeftArm", etc.).
///
/// SETUP:
///   1. Añadir este componente al mismo GO donde está ExplorerAvatar.
///   2. Los controladores y el avatar se auto-detectan.
///   3. Ajustar leftHandRotOffset / rightHandRotOffset en Inspector hasta que la
///      palma quede bien (ver comentarios de cada campo).
///   4. Asegurarse de que el Animator del RobotKyle tiene avatar Humanoid.
[DefaultExecutionOrder(200)]
public class RobotHandIK : MonoBehaviour
{
    [Header("Controladores VR (auto-detectados si están vacíos)")]
    public Transform leftController;
    public Transform rightController;

    [Header("Avatar (auto-detectado por nombre 'RobotKyle_Explorer' / 'RobotKyle')")]
    public Transform avatarRoot;

    [Header("Offset de rotación de la mano")]
    [Tooltip("Gira la mano izquierda para alinear la palma con el grip del controlador.\n" +
             "Si la mano está al revés: añade 180 en Y.\n" +
             "Si está girada 90°: ajusta Z.\n" +
             "Valor inicial recomendado para Meta Quest 3: (0, 90, 0)")]
    public Vector3 leftHandRotOffset  = new Vector3(0f,  90f, 0f);

    [Tooltip("Gira la mano derecha.\n" +
             "Valor inicial recomendado para Meta Quest 3: (0, -90, 0)")]
    public Vector3 rightHandRotOffset = new Vector3(0f, -90f, 0f);

    [Header("Pistas de codo (opcional — GOs vacíos para guiar el doblez del codo)")]
    [Tooltip("Si está vacío se usa una dirección lateral automática (izquierda/derecha según el brazo).")]
    public Transform leftElbowHint;
    public Transform rightElbowHint;

    [Header("Peso del IK (0 = solo animación, 1 = solo IK)")]
    [Range(0f, 1f)]
    public float ikWeight = 1f;

    // Huesos izquierda
    private Transform _lUpper, _lFore, _lHand;
    private float     _lUpperLen, _lLowerLen;

    // Huesos derecha
    private Transform _rUpper, _rFore, _rHand;
    private float     _rUpperLen, _rLowerLen;

    // ─────────────────────────────────
    void Awake()
    {
        if (avatarRoot == null)
        {
            var robot = GameObject.Find("RobotKyle_Explorer") ?? GameObject.Find("RobotKyle");
            if (robot != null) avatarRoot = robot.transform;
        }

        if (leftController  == null)
            leftController  = FindGO("LeftHandQuestVisual")  ?? FindGO("LeftHand Controller");
        if (rightController == null)
            rightController = FindGO("RightHandQuestVisual") ?? FindGO("RightHand Controller");
    }

    void Start()
    {
        if (avatarRoot == null)
        {
            Debug.LogError("[RobotHandIK] No se encontró avatarRoot. Asígnalo en el Inspector.", this);
            enabled = false;
            return;
        }

        // Usar el Avatar Humanoid para obtener huesos correctamente sin importar el nombre del FBX.
        var anim = avatarRoot.GetComponentInChildren<Animator>();
        if (anim == null || !anim.isHuman)
        {
            Debug.LogError("[RobotHandIK] El Animator no existe o el avatar no es Humanoid.", this);
            enabled = false;
            return;
        }

        _lUpper = anim.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        _lFore  = anim.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        _lHand  = anim.GetBoneTransform(HumanBodyBones.LeftHand);

        _rUpper = anim.GetBoneTransform(HumanBodyBones.RightUpperArm);
        _rFore  = anim.GetBoneTransform(HumanBodyBones.RightLowerArm);
        _rHand  = anim.GetBoneTransform(HumanBodyBones.RightHand);

        if (_lUpper && _lFore && _lHand)
        {
            _lUpperLen = Vector3.Distance(_lUpper.position, _lFore.position);
            _lLowerLen = Vector3.Distance(_lFore.position,  _lHand.position);
        }
        if (_rUpper && _rFore && _rHand)
        {
            _rUpperLen = Vector3.Distance(_rUpper.position, _rFore.position);
            _rLowerLen = Vector3.Distance(_rFore.position,  _rHand.position);
        }

        LogMissing("LeftUpperArm",  _lUpper);
        LogMissing("LeftLowerArm",  _lFore);
        LogMissing("LeftHand",      _lHand);
        LogMissing("RightUpperArm", _rUpper);
        LogMissing("RightLowerArm", _rFore);
        LogMissing("RightHand",     _rHand);
    }

    void LateUpdate()
    {
        if (ikWeight <= 0f) return;

        if (leftController  && _lUpper && _lFore && _lHand && _lUpperLen > 0.001f)
            SolveArm(_lUpper, _lFore, _lHand,
                     leftController.position,  leftController.rotation,
                     _lUpperLen, _lLowerLen,
                     leftElbowHint, Quaternion.Euler(leftHandRotOffset), isLeft: true);

        if (rightController && _rUpper && _rFore && _rHand && _rUpperLen > 0.001f)
            SolveArm(_rUpper, _rFore, _rHand,
                     rightController.position, rightController.rotation,
                     _rUpperLen, _rLowerLen,
                     rightElbowHint, Quaternion.Euler(rightHandRotOffset), isLeft: false);
    }

    // ─────────────────────────────────
    //  Two-Bone IK
    // ─────────────────────────────────
    void SolveArm(Transform upper, Transform fore, Transform hand,
                  Vector3 target, Quaternion targetRot,
                  float upperLen, float lowerLen,
                  Transform elbowHint, Quaternion handOffset, bool isLeft)
    {
        Vector3 rootPos = upper.position;
        float   dist    = Mathf.Clamp(Vector3.Distance(rootPos, target),
                                      0.01f, (upperLen + lowerLen) * 0.999f);
        Vector3 toTarget = (target - rootPos).normalized;

        // Dirección del codo:
        //   Con hint: apunta hacia el hint.
        //   Sin hint: el codo va hacia el lado correspondiente (izquierda/derecha),
        //             ligeramente hacia abajo y hacia atrás — postura natural del brazo.
        Vector3 elbowDir;
        if (elbowHint != null)
        {
            elbowDir = (elbowHint.position - rootPos).normalized;
        }
        else
        {
            Vector3 sideDir = isLeft ? Vector3.left : Vector3.right;
            // Componente lateral + 30 % hacia abajo para doblez natural
            elbowDir = (sideDir + Vector3.down * 0.3f).normalized;
        }

        // Quitar componente paralela al segmento hombro→target
        Vector3 perp = elbowDir - Vector3.Dot(elbowDir, toTarget) * toTarget;
        if (perp.sqrMagnitude < 0.001f)
            perp = Vector3.Cross(toTarget, upper.right);
        perp = perp.normalized;

        // Ley de cosenos → ángulo en el hombro
        float a = upperLen, b = lowerLen, c = dist;
        float cosA = Mathf.Clamp((a * a + c * c - b * b) / (2f * a * c), -1f, 1f);
        float sinA = Mathf.Sqrt(Mathf.Max(0f, 1f - cosA * cosA));

        Vector3 elbowPos = rootPos + (toTarget * cosA + perp * sinA) * upperLen;

        // Rotar brazo superior → codo
        ApplyDelta(upper, (fore.position - upper.position).normalized,
                          (elbowPos - upper.position).normalized);

        // Rotar antebrazo → target (fore.position se actualizó al rotar upper)
        ApplyDelta(fore,  (hand.position - fore.position).normalized,
                          (target - fore.position).normalized);

        // Rotación de la mano = controlador × offset
        hand.rotation = ikWeight >= 1f
            ? targetRot * handOffset
            : Quaternion.Slerp(hand.rotation, targetRot * handOffset, ikWeight);
    }

    static void ApplyDelta(Transform bone, Vector3 from, Vector3 to)
    {
        if (Vector3.Angle(from, to) < 0.1f) return;
        bone.rotation = Quaternion.FromToRotation(from, to) * bone.rotation;
    }

    static Transform FindGO(string n) => GameObject.Find(n)?.transform;

    static void LogMissing(string name, Transform t)
    {
        if (t == null) Debug.LogWarning($"[RobotHandIK] Hueso '{name}' no encontrado en el avatar.");
    }
}
