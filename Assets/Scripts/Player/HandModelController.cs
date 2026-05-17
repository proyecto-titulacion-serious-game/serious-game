using UnityEngine;

/// <summary>
/// Crea visualmente una mano (palma + dedos) sobre el controlador VR.
/// Añadir a LeftHand_Controller y RightHand_Controller.
/// El TrackedPoseDriver ya mueve el GameObject — este script solo construye la geometría.
/// </summary>
public class HandModelController : MonoBehaviour
{
    public enum HandSide { Left, Right }

    [Header("Configuracion")]
    public HandSide hand = HandSide.Left;

    [Tooltip("Ajuste de rotacion para alinear la mano con la pose del controller Meta Quest 3. " +
             "Modifica X si la mano aparece volcada hacia adelante/atras.")]
    public Vector3 rotationOffset = new Vector3(-60f, 0f, 0f);

    [Header("Material")]
    [Tooltip("Material de la mano. Se crea automaticamente con color piel si queda vacio.")]
    public Material handMaterial;

    void Start()
    {
        RemovePlaceholders();
        BuildHand();
    }

    // Permite reconstruir desde el Inspector (click derecho → Reconstruir Mano)
    [ContextMenu("Reconstruir Mano")]
    public void RebuildInEditor()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child.name.StartsWith("Hand_") || child.name == "HandPlaceholder_Replace")
                DestroyImmediate(child.gameObject);
        }
        BuildHand();
    }

    void RemovePlaceholders()
    {
        foreach (Transform child in transform)
        {
            if (child.name == "HandPlaceholder_Replace")
                Destroy(child.gameObject);
        }
    }

    void BuildHand()
    {
        if (handMaterial == null)
            handMaterial = CreateHandMaterial();

        // Pivot con offset de rotacion para alinear con el grip pose del controller
        var pivot = new GameObject("Hand_Pivot");
        pivot.transform.SetParent(transform, false);
        pivot.transform.localPosition = Vector3.zero;
        pivot.transform.localRotation = Quaternion.Euler(rotationOffset);

        // mirror: izquierda = 1, derecha = -1 (refleja el pulgar)
        float mirror = hand == HandSide.Left ? 1f : -1f;

        // Palma
        AddPart(pivot, "Hand_Palm",
            new Vector3(0f, 0f, 0.04f),
            Quaternion.identity,
            new Vector3(0.075f, 0.022f, 0.09f),
            PrimitiveType.Cube);

        // 4 dedos: indice, medio, anular, menique
        float[] xOff   = { -0.028f, -0.009f,  0.010f,  0.029f };
        float[] length = {  0.070f,  0.075f,  0.065f,  0.050f };
        for (int i = 0; i < 4; i++)
        {
            AddPart(pivot, $"Hand_Finger{i}",
                new Vector3(xOff[i], 0f, 0.095f + length[i] * 0.5f),
                Quaternion.Euler(90f, 0f, 0f),
                new Vector3(0.014f, length[i] * 0.5f, 0.014f),
                PrimitiveType.Capsule);
        }

        // Pulgar (angulo hacia afuera segun lado)
        AddPart(pivot, "Hand_Thumb",
            new Vector3(mirror * -0.043f, 0.005f, 0.025f),
            Quaternion.Euler(90f, 0f, mirror * 40f),
            new Vector3(0.017f, 0.022f, 0.017f),
            PrimitiveType.Capsule);
    }

    void AddPart(GameObject parent, string partName,
                 Vector3 localPos, Quaternion localRot, Vector3 scale,
                 PrimitiveType type)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = partName;
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = localRot;
        go.transform.localScale    = scale;

        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null) mr.sharedMaterial = handMaterial;

        // Quitar colisores para no interferir con XRGrabInteractable
        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);
    }

    Material CreateHandMaterial()
    {
        // Prueba los nombres de shader URP en orden; "Hidden/InternalErrorShader" nunca falla
        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                     ?? Shader.Find("URP/Lit")
                     ?? Shader.Find("Lit")
                     ?? Shader.Find("Standard");

        if (shader == null)
        {
            Debug.LogWarning("[HandModelController] No se encontró shader para las manos. " +
                             "Asigna un material manualmente en el Inspector.");
            return new Material(Shader.Find("Hidden/InternalErrorShader"));
        }

        var mat  = new Material(shader);
        var skin = new Color(0.90f, 0.73f, 0.55f);
        mat.SetColor("_BaseColor", skin);
        mat.SetColor("_Color",     skin);
        return mat;
    }
}
