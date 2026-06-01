using UnityEngine;

/// <summary>
/// Dibuja un cable flexible entre dos puntos.
/// ACTUALIZADO: Usa curva de Bézier Cúbica para un realismo físico coherente
/// con el resto de las herramientas de la mesa.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class VRCableRenderer : MonoBehaviour
{
    public Transform origin;
    public Transform target;
    
    [Header("Físicas visuales del cable")]
    [Range(6, 32)] public int segments = 16;
    [Range(0f, 0.3f)] public float sagAmount = 0.08f;
    [Tooltip("Distancia máxima del cable antes de tensarse por completo.")]
    public float maxCableLength = 0.6f;

    private LineRenderer _lineRenderer;

    void Start()
    {
        _lineRenderer = GetComponent<LineRenderer>();
    }

    void Update()
    {
        if (origin == null || target == null) return;
        
        Vector3 start = origin.position;
        Vector3 end = target.position;

        float dist  = Vector3.Distance(start, end);
        
        // CORRECCIÓN: Cálculo de tensión. Más holgura = más caída. Tenso = línea recta.
        float slack = Mathf.Max(0f, maxCableLength - dist);
        float currentSag = Mathf.Clamp(slack * 0.5f, 0.01f, sagAmount);

        Vector3 mid = Vector3.Lerp(start, end, 0.5f) + Vector3.down * currentSag;
        Vector3 c1  = Vector3.Lerp(start, mid, 0.5f);
        Vector3 c2  = Vector3.Lerp(end,   mid, 0.5f);

        _lineRenderer.positionCount = segments;
        for (int i = 0; i < segments; i++)
        {
            float t = (float)i / (segments - 1);
            _lineRenderer.SetPosition(i, Bezier(start, c1, c2, end, t));
        }
    }

    // Curva matemática de Bézier de 4 puntos de control
    static Vector3 Bezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1f - t;
        return u*u*u*p0 + 3f*u*u*t*p1 + 3f*u*t*t*p2 + t*t*t*p3;
    }
}