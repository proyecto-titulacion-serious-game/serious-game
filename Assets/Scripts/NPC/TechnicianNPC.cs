using UnityEngine;

/// <summary>
/// Controla el comportamiento del robot técnico como NPC en la escena.
/// — Mantiene animación idle (Speed = 0).
/// — Gira la cabeza suavemente hacia el jugador cuando está dentro del rango.
/// Requiere un Animator con rig Humanoid en el mismo GameObject.
/// </summary>
[RequireComponent(typeof(Animator))]
public class TechnicianNPC : MonoBehaviour
{
    [Header("Look-At")]
    [Tooltip("Radio en metros dentro del cual el robot mira al jugador")]
    public float lookRange = 5f;
    [Tooltip("Velocidad de giro de la cabeza")]
    public float lookSpeed = 4f;
    [Tooltip("Altura adicional al objetivo de mirada (para apuntar al centro de la cabeza del jugador)")]
    public float targetHeightOffset = 1.6f;

    private Animator _animator;
    private Transform _headBone;
    private Transform _player;

    static readonly int SpeedHash = Animator.StringToHash("Speed");

    void Start()
    {
        _animator = GetComponent<Animator>();

        // Fuerza idle: el parámetro Speed a 0 mantiene la animación en reposo
        if (_animator != null)
            _animator.SetFloat(SpeedHash, 0f);

        // Obtiene el hueso Head del rig humanoid automáticamente
        if (_animator != null && _animator.isHuman)
            _headBone = _animator.GetBoneTransform(HumanBodyBones.Head);

        // Busca al jugador por tag
        var playerGO = GameObject.FindWithTag("Player");
        if (playerGO != null)
            _player = playerGO.transform;
    }

    void LateUpdate()
    {
        // Speed siempre en 0 (NPC no se mueve)
        if (_animator != null)
            _animator.SetFloat(SpeedHash, 0f);

        if (_headBone == null || _player == null) return;

        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist > lookRange) return;

        // Peso de atención: máximo dentro del rango, se desvanece cerca del límite
        float t = Mathf.Clamp01(1f - dist / lookRange);

        Vector3 targetPos = _player.position + Vector3.up * targetHeightOffset;
        Vector3 dir = (targetPos - _headBone.position).normalized;

        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        _headBone.rotation = Quaternion.Slerp(
            _headBone.rotation,
            targetRot,
            Time.deltaTime * lookSpeed * t);
    }
}
