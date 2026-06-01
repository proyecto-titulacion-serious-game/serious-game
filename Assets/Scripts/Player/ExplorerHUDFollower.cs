using UnityEngine;

/// <summary>
/// Hace que el ExplorerHUD (Canvas WorldSpace) siga la cabeza del jugador VR
/// con lerp suave, zona muerta angular y verificación de obstrucción.
///
/// SETUP:
///   1. Añadir este script al GO raíz del ExplorerHUD (Canvas WorldSpace).
///   2. Asignar 'headCamera' al ExplorerCamera (Main Camera del XR Rig).
///   3. El Canvas no debe ser hijo de la cámara — este script lo mueve solo.
///
/// COMPORTAMIENTO:
///   • El panel flota a [followDistance] metros delante de los ojos.
///   • Se mueve suavemente (lerp) cuando la cabeza gira más de [deadZoneDegrees].
///   • Si hay geometría entre el panel y los ojos, el panel se retrae para no
///     atravesar la mesa/paredes (raycast de proximidad).
/// </summary>
public class ExplorerHUDFollower : MonoBehaviour
{
    [Header("Referencia")]
    [Tooltip("ExplorerCamera (Main Camera del XR Rig). Se auto-busca si queda vacío.")]
    public Camera headCamera;

    [Header("Posición")]
    [Tooltip("Distancia en metros delante de los ojos.")]
    [Range(0.3f, 2f)] public float followDistance  = 0.8f;
    [Tooltip("Desplazamiento vertical respecto a la línea de visión (valores negativos = más abajo).")]
    [Range(-0.5f, 0.3f)] public float verticalOffset = -0.15f;

    [Header("Suavizado")]
    [Tooltip("Velocidad del lerp de posición (1=lento, 20=instantáneo).")]
    [Range(1f, 20f)] public float positionSpeed = 5f;
    [Tooltip("Velocidad del lerp de rotación.")]
    [Range(1f, 20f)] public float rotationSpeed  = 7f;
    [Tooltip("El panel solo se mueve cuando la cabeza gira más de estos grados (evita temblor).")]
    [Range(5f, 45f)]  public float deadZoneDegrees = 20f;

    [Header("Anti-clipping")]
    [Tooltip("Capas de geometría que pueden bloquear el panel (mesa, paredes, etc.).")]
    public LayerMask obstructionMask = ~0;
    [Tooltip("Distancia mínima al objeto obstructor antes de retraer el panel.")]
    [Range(0.05f, 0.3f)] public float minClearance = 0.1f;

    // ─────────────────────────────────────────────
    private Vector3    _targetPosition;
    private Quaternion _targetRotation;
    private bool       _initialized;

    void Start()
    {
        if (headCamera == null)
        {
            headCamera = Camera.main;
            if (headCamera == null)
            {
                Debug.LogWarning("[HUDFollower] headCamera no encontrada. " +
                                 "Asígnala en Inspector o asegúrate de tener una Camera con tag MainCamera.", this);
                enabled = false;
                return;
            }
        }

        // Posición inicial: directamente delante del jugador
        _targetPosition = ComputeTargetPosition();
        _targetRotation = ComputeTargetRotation();
        transform.SetPositionAndRotation(_targetPosition, _targetRotation);
        _initialized = true;
    }

    void LateUpdate()
    {
        if (!_initialized || headCamera == null) return;

        // Comprobar si la cabeza giró más de la zona muerta
        Vector3 toPanel    = (transform.position - headCamera.transform.position).normalized;
        float   angleDelta = Vector3.Angle(toPanel, headCamera.transform.forward);

        if (angleDelta > deadZoneDegrees)
        {
            _targetPosition = ComputeTargetPosition();
            _targetRotation = ComputeTargetRotation();
        }

        // Aplicar lerp suave
        transform.position = Vector3.Lerp(transform.position, _targetPosition,
                                          Time.deltaTime * positionSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation,
                                               Time.deltaTime * rotationSpeed);

        // Anti-clipping: retrae el panel si hay geometría entre él y los ojos
        ResolveObstruction();
    }

    Vector3 ComputeTargetPosition()
    {
        Transform cam = headCamera.transform;
        // Proyectamos el forward sobre el plano horizontal para evitar que el panel
        // se vaya para arriba cuando el jugador mira al suelo
        Vector3 flatForward = new Vector3(cam.forward.x, 0f, cam.forward.z);
        if (flatForward.sqrMagnitude < 0.01f) flatForward = Vector3.forward;
        flatForward.Normalize();

        return cam.position
               + flatForward * followDistance
               + Vector3.up  * verticalOffset;
    }

    Quaternion ComputeTargetRotation()
    {
        // El panel siempre mira hacia los ojos del jugador (billboard)
        Vector3 dir = headCamera.transform.position - transform.position;
        if (dir.sqrMagnitude < 0.0001f) return transform.rotation;
        return Quaternion.LookRotation(-dir.normalized, Vector3.up);
    }

    void ResolveObstruction()
    {
        Vector3 eyePos    = headCamera.transform.position;
        Vector3 toPanelDir = (transform.position - eyePos);
        float   dist      = toPanelDir.magnitude;

        if (Physics.Raycast(eyePos, toPanelDir.normalized, out RaycastHit hit,
                            dist, obstructionMask, QueryTriggerInteraction.Ignore))
        {
            // Hay geometría entre los ojos y el panel — retraer
            float safeDist = Mathf.Max(hit.distance - minClearance, 0.2f);
            transform.position = eyePos + toPanelDir.normalized * safeDist;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (headCamera == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(headCamera.transform.position, transform.position);
        Gizmos.DrawWireSphere(transform.position, 0.05f);
    }
#endif
}
