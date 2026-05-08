using UnityEngine;

/// <summary>
/// Escala una zona de reto basándose en la proximidad del Explorador.
/// El circuito arranca pequeño (factorMinimo) y crece suavemente hasta su
/// tamaño real (factorMaximo) cuando el jugador se acerca.
///
/// SETUP:
///   Añadir este script al root de cada Reto*_Zone (Reto1_Zone, Reto2_Zone, etc.)
///   El componente usa Camera.main como referencia del jugador (auto-detectado).
///
/// DISTANCIAS:
///   distanciaActivacion → a partir de aquí empieza a crecer
///   distanciaCompleta   → a esta distancia alcanza el tamaño máximo
///
/// NO requiere sincronización de red: es un efecto visual puro calculado
/// independientemente en cada cliente.
/// </summary>
public class ZoneProximityScaler : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────

    [Header("Escalas")]
    [Tooltip("Tamaño relativo cuando el jugador está lejos (0.25 = 25% del tamaño real)")]
    [Range(0.05f, 1f)]
    public float factorMinimo   = 0.25f;

    [Tooltip("Tamaño relativo cuando el jugador está cerca (1.0 = tamaño real)")]
    [Range(0.1f, 2f)]
    public float factorMaximo   = 1.0f;

    [Header("Distancias de transición (metros)")]
    [Tooltip("El circuito empieza a crecer cuando el jugador entra en este radio")]
    public float distanciaActivacion = 4.0f;

    [Tooltip("El circuito alcanza su tamaño máximo a esta distancia")]
    public float distanciaCompleta   = 1.5f;

    [Header("Suavizado")]
    [Tooltip("Velocidad de la transición. Valores altos = más rápido")]
    public float velocidad = 5f;

    [Tooltip("Curva de easing (X = distancia normalizada 0–1, Y = factor de escala 0–1). " +
             "Dejar vacía para interpolación lineal.")]
    public AnimationCurve curvaSuavizado;

    [Header("Referencia del jugador")]
    [Tooltip("Se detecta automáticamente usando Camera.main si queda vacío")]
    public Transform playerHead;

    // ─────────────────────────────────────────────
    //  Internos
    // ─────────────────────────────────────────────

    private Vector3 _escalaOriginal;
    private bool    _inicializado;

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────

    void Start()
    {
        // Guardar la escala configurada en el Inspector como "tamaño real"
        _escalaOriginal = transform.localScale;

        // Arrancar en tamaño mínimo
        transform.localScale = _escalaOriginal * factorMinimo;
        _inicializado = true;

        BuscarJugador();

        // Validar distancias
        if (distanciaCompleta >= distanciaActivacion)
        {
            Debug.LogWarning($"[ZoneProximityScaler] {name}: distanciaCompleta debe ser " +
                             "menor que distanciaActivacion. Valores intercambiados.");
            (distanciaCompleta, distanciaActivacion) = (distanciaActivacion, distanciaCompleta);
        }
    }

    void Update()
    {
        if (playerHead == null)
        {
            BuscarJugador();
            return;
        }

        // Distancia horizontal (ignora diferencia de altura para mayor robustez en VR)
        float dist = DistanciaHorizontal(transform.position, playerHead.position);

        // t = 0 cuando está lejos, t = 1 cuando está cerca
        float t = Mathf.InverseLerp(distanciaActivacion, distanciaCompleta, dist);
        t = Mathf.Clamp01(t);

        // Aplicar curva de easing si existe
        if (curvaSuavizado != null && curvaSuavizado.length > 0)
            t = curvaSuavizado.Evaluate(t);

        float  factorObjetivo = Mathf.Lerp(factorMinimo, factorMaximo, t);
        Vector3 escalaObjetivo = _escalaOriginal * factorObjetivo;

        // Interpolar suavemente hacia la escala objetivo
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            escalaObjetivo,
            Time.deltaTime * velocidad);
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────

    void BuscarJugador()
    {
        if (Camera.main != null)
            playerHead = Camera.main.transform;
    }

    static float DistanciaHorizontal(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    // ─────────────────────────────────────────────
    //  Gizmos — visualizar radios en la escena
    // ─────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        Vector3 center = transform.position;

        // Radio de activación (amarillo)
        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.4f);
        DrawCircle(center, distanciaActivacion);

        // Radio de tamaño completo (verde)
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.4f);
        DrawCircle(center, distanciaCompleta);
    }

    static void DrawCircle(Vector3 center, float radius, int segments = 32)
    {
        float step = 360f / segments;
        for (int i = 0; i < segments; i++)
        {
            float a0 = i * step * Mathf.Deg2Rad;
            float a1 = (i + 1) * step * Mathf.Deg2Rad;
            Gizmos.DrawLine(
                center + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * radius,
                center + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * radius);
        }
    }
}
