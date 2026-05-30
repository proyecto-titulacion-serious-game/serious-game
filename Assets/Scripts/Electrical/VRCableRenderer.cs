using UnityEngine;

/// <summary>
/// Dibuja un cable flexible entre el multímetro y la punta de prueba.
/// </summary>
public class VRCableRenderer : MonoBehaviour
{
    public Transform origin; // Punto fijo en el cuerpo del multímetro
    public Transform target; // Punta de prueba que agarra el jugador
    private LineRenderer _lineRenderer;

    void Start()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.positionCount = 2;
    }

    void Update()
    {
        if (origin != null && target != null)
        {
            _lineRenderer.SetPosition(0, origin.position);
            _lineRenderer.SetPosition(1, target.position);
        }
    }
}