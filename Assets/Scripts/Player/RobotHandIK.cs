using UnityEngine;

/// Conecta los brazos del RobotKyle a los controladores VR usando Two-Bone IK.
/// Cadena: LeftUpperArm → LeftArm (antebrazo) → LeftHand  (igual derecha).
///
/// SETUP en Explorador.unity:
///   1. Añadir este componente al mismo GO donde está ExplorerAvatar.
///   2. Los controladores y el avatar se auto-detectan.
///   3. Ajustar leftHandRotOffset / rightHandRotOffset hasta que la palma quede bien.
///   4. (Opcional) Crear dos GOs vacíos como "pistas de codo" para controlar la
///      dirección de doblado: colocar uno detrás-izquierda y otro detrás-derecha.
[DefaultExecutionOrder(200)]
public class RobotHandIK : MonoBehaviour
{
    [Header("Controladores VR (auto-detectados si están vacíos)")]
    public Transform leftController;
    public Transform rightController;

    [Header("Avatar (auto-detectado por nombre 'RobotKyle_Explorer')")]
    public Transform avatarRoot;

    [Header("Offset de rotación de la mano")]
    [Tooltip("Gira la mano izquierda para alinear la palma con el grip del controlador Meta Quest.")]
    public Vector3 leftHandRotOffset  = new Vector3(0f, 90f, -90f);
    [Tooltip("Gira la mano derecha.")]
    public Vector3 rightHandRotOffset = new Vector3(0f, -90f, 90f);

    [Header("Pistas de codo (opcional — GOs vacíos para guiar el doblez del codo)")]
    public Transform leftElbowHint;
    public Transform rightElbowHint;

    [Header("Peso del IK (0 = solo animación, 1 = solo IK)")]
    [Range(0f, 1f)]
    public float ikWeight = 1f;

    // Huesos izquierda
    private Transform _lUpper, _lFore, _lHand;
    private float _lUpperLen, _lLowerLen;

    // Huesos derecha
    private Transform _rUpper, _rFore, _rHand;
    private float _rUpperLen, _rLowerLen;

    // ─────────────────────────────────
    //  Lifecycle
    // ─────────────────────────────────

    void Awake()
    {
        if (avatarRoot == null)
        {
            var robot = GameObject.Find("RobotKyle_Explorer") ?? GameObject.Find("RobotKyle");
            if (robot != null) avatarRoot = robot.transform;
        }

        if (avatarRoot == null)
        {
            Debug.LogError("[RobotHandIK] No se encontró el avatar del robot. " +
                           "Asigna 'avatarRoot' en el Inspector.", this);
            enabled = false;
            return;
        }

        _lUpper = FindBone("LeftUpperArm");
        _lFore  = FindBone("LeftArm");       // En KyleRobot "LeftArm" = antebrazo
        _lHand  = FindBone("LeftHand");

        _rUpper = FindBone("RightUpperArm");
        _rFore  = FindBone("RightArm");
        _rHand  = FindBone("RightHand");

        LogMissingBone("LeftUpperArm",  _lUpper);
        LogMissingBone("LeftArm",       _lFore);
        LogMissingBone("LeftHand",      _lHand);
        LogMissingBone("RightUpperArm", _rUpper);
        LogMissingBone("RightArm",      _rFore);
        LogMissingBone("RightHand",     _rHand);

        if (leftController == null)
            leftController = FindGO("LeftHand Controller");
        if (rightController == null)
            rightController = FindGO("RightHand Controller");
    }

    void Start()
    {
        // Longitudes capturadas en T-pose — se usan durante toda la sesión
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
    }

    void LateUpdate()
    {
        if (ikWeight <= 0f) return;

        if (leftController && _lUpper && _lFore && _lHand && _lUpperLen > 0.001f && _lLowerLen > 0.001f)
            SolveArm(_lUpper, _lFore, _lHand,
                     leftController.position, leftController.rotation,
                     _lUpperLen, _lLowerLen,
                     leftElbowHint, Quaternion.Euler(leftHandRotOffset));

        if (rightController && _rUpper && _rFore && _rHand && _rUpperLen > 0.001f && _rLowerLen > 0.001f)
            SolveArm(_rUpper, _rFore, _rHand,
                     rightController.position, rightController.rotation,
                     _rUpperLen, _rLowerLen,
                     rightElbowHint, Quaternion.Euler(rightHandRotOffset));
    }

    // ─────────────────────────────────
    //  Two-Bone IK
    // ─────────────────────────────────

    void SolveArm(Transform upper, Transform fore, Transform hand,
                  Vector3 target, Quaternion targetRot,
                  float upperLen, float lowerLen,
                  Transform elbowHint, Quaternion handOffset)
    {
        Vector3 rootPos = upper.position;
        float dist = Mathf.Clamp(Vector3.Distance(rootPos, target),
                                 0.01f, (upperLen + lowerLen) * 0.999f);

        Vector3 toTarget = (target - rootPos).normalized;

        // Dirección del codo: hint si existe, si no usa "abajo" como fallback natural
        Vector3 elbowDir = elbowHint != null
            ? (elbowHint.position - rootPos).normalized
            : Vector3.down;

        // Componente perpendicular de elbowDir respecto a toTarget
        Vector3 perp = (elbowDir - Vector3.Dot(elbowDir, toTarget) * toTarget);
        if (perp.sqrMagnitude < 0.001f)
            perp = Vector3.Cross(toTarget, upper.right);
        perp = perp.normalized;

        // Ley de cosenos: ángulo en el hombro
        float a = upperLen, b = lowerLen, c = dist;
        float cosA = Mathf.Clamp((a * a + c * c - b * b) / (2f * a * c), -1f, 1f);
        float sinA = Mathf.Sqrt(Mathf.Max(0f, 1f - cosA * cosA));

        // Posición del codo en espacio mundo
        Vector3 elbowPos = rootPos + (toTarget * cosA + perp * sinA) * upperLen;

        // --- Rotar brazo superior para apuntar al codo ---
        Vector3 curUpperDir = (fore.position - upper.position).normalized;
        Vector3 newUpperDir = (elbowPos    - upper.position).normalized;
        ApplyDeltaRotation(upper, curUpperDir, newUpperDir);

        // --- Rotar antebrazo para apuntar al target ---
        // fore.position ya se actualizó al rotar upper (Unity propaga inmediatamente)
        Vector3 curForeDir = (hand.position - fore.position).normalized;
        Vector3 newForeDir = (target        - fore.position).normalized;
        ApplyDeltaRotation(fore, curForeDir, newForeDir);

        // --- Rotación de la mano: iguala al controlador + offset ---
        if (ikWeight >= 1f)
        {
            hand.rotation = targetRot * handOffset;
        }
        else
        {
            hand.rotation = Quaternion.Slerp(hand.rotation, targetRot * handOffset, ikWeight);
        }
    }

    static void ApplyDeltaRotation(Transform bone, Vector3 from, Vector3 to)
    {
        if (Vector3.Angle(from, to) < 0.1f) return;
        bone.rotation = Quaternion.FromToRotation(from, to) * bone.rotation;
    }

    // ─────────────────────────────────
    //  Helpers
    // ─────────────────────────────────

    Transform FindBone(string boneName)
    {
        string lower = boneName.ToLowerInvariant();
        foreach (Transform t in avatarRoot.GetComponentsInChildren<Transform>(true))
            if (t.name.ToLowerInvariant() == lower) return t;
        return null;
    }

    static Transform FindGO(string name) => GameObject.Find(name)?.transform;

    static void LogMissingBone(string name, Transform t)
    {
        if (t == null)
            Debug.LogWarning($"[RobotHandIK] Hueso '{name}' no encontrado en el avatar.");
    }
}
