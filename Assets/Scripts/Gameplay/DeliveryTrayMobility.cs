using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Hace que la CAJA de la bandeja de entrega sea AGARRABLE/movible y evita que las piezas se caigan.
/// Añádelo (por Inspector) al GameObject raíz de la bandeja — `ComponentReceiver` / `Bandeja_Recepcion`,
/// el que tiene escala uniforme (1,1,1), NO el `Tray_Visual` achatado.
///
/// Resuelve dos síntomas:
///   • "La caja no se mueve" → le asegura un Rigidbody KINEMÁTICO + XRGrabInteractable, así el
///     Explorador la agarra y la arrastra; las piezas emparentadas (bandeja híbrida) viajan con ella.
///   • "Las piezas se caen" → construye un PISO (y paredes bajas) de colliders SÓLIDOS bajo la bandeja,
///     para que una pieza soltada repose dentro en vez de atravesar el collider-trigger y caer al suelo.
/// </summary>
[DisallowMultipleComponent]
public class DeliveryTrayMobility : MonoBehaviour
{
    [Header("Movilidad de la caja")]
    [Tooltip("Permite agarrar y mover la caja (Rigidbody kinematic + XRGrabInteractable).")]
    public bool hacerAgarrable = true;

    [Header("Contención (que las piezas no se caigan)")]
    [Tooltip("Construye un piso + paredes bajas sólidas bajo la bandeja.")]
    public bool construirContencion = true;
    [Tooltip("Tamaño interior de la bandeja: X = ancho, Y = alto de pared, Z = profundidad (metros).")]
    public Vector3 tamanoBandeja = new Vector3(0.30f, 0.05f, 0.20f);
    [Tooltip("Centro del piso relativo al objeto (ajústalo si la bandeja no está en el origen del root).")]
    public Vector3 centroLocal = Vector3.zero;
    [Tooltip("Grosor de piso/paredes (metros).")]
    public float grosor = 0.01f;
    [Tooltip("Crear paredes laterales (además del piso) para que las piezas no rueden fuera.")]
    public bool conParedes = true;

    void Awake()
    {
        if (hacerAgarrable)       EnsureGrabbable();
        if (construirContencion)  BuildContainment();
    }

    void EnsureGrabbable()
    {
        var rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;   // no cae; XRI la mueve al agarrarla. Las piezas hijas la siguen.
        rb.useGravity  = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (GetComponent<XRGrabInteractable>() == null)
        {
            var grab = gameObject.AddComponent<XRGrabInteractable>();
            // Default (Instantaneous) + Rigidbody kinematic = la caja sigue la mano directamente.
            grab.retainTransformParent = true;  // conserva su lugar en la jerarquía al soltarla
            grab.throwOnDetach         = false; // no salir volando al soltar
        }
    }

    void BuildContainment()
    {
        Vector3 c = centroLocal;
        float hw = tamanoBandeja.x * 0.5f;
        float hd = tamanoBandeja.z * 0.5f;
        float wy = Mathf.Max(tamanoBandeja.y, grosor);

        // Piso sólido bajo la bandeja.
        AddBox("Tray_Floor", c + new Vector3(0f, -wy * 0.5f, 0f),
               new Vector3(tamanoBandeja.x, grosor, tamanoBandeja.z));

        if (!conParedes) return;

        // Paredes bajas para contener las piezas.
        AddBox("Tray_Wall_N", c + new Vector3(0f, 0f,  hd), new Vector3(tamanoBandeja.x, wy, grosor));
        AddBox("Tray_Wall_S", c + new Vector3(0f, 0f, -hd), new Vector3(tamanoBandeja.x, wy, grosor));
        AddBox("Tray_Wall_E", c + new Vector3( hw, 0f, 0f), new Vector3(grosor, wy, tamanoBandeja.z));
        AddBox("Tray_Wall_W", c + new Vector3(-hw, 0f, 0f), new Vector3(grosor, wy, tamanoBandeja.z));
    }

    void AddBox(string nombre, Vector3 localPos, Vector3 size)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;
        var bc = go.AddComponent<BoxCollider>();
        bc.size      = size;
        bc.isTrigger = false;   // SÓLIDO: las piezas reposan sobre él, no lo atraviesan.
    }
}
