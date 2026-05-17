using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Adds a simple sphere visual to the hand controller so it's visible in VR.
/// Attach to LeftHand Controller or RightHand Controller GameObjects.
/// The sphere changes color when the interactor is selecting (grabbing).
/// </summary>
[RequireComponent(typeof(XRDirectInteractor))]
public class HandControllerVisual : MonoBehaviour
{
    [Header("Visual Settings")]
    [Tooltip("Size of the controller sphere in meters")]
    public float sphereRadius = 0.04f;

    [Tooltip("Normal hand color")]
    public Color colorIdle = new Color(0.8f, 0.8f, 0.9f, 1f);

    [Tooltip("Color when gripping/selecting an object")]
    public Color colorGrab = new Color(0.3f, 0.9f, 0.3f, 1f);

    private XRDirectInteractor _interactor;
    private MeshRenderer _renderer;
    private MaterialPropertyBlock _mpb;

    void Awake()
    {
        _interactor = GetComponent<XRDirectInteractor>();
        _mpb = new MaterialPropertyBlock();

        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "HandVisual";
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale = Vector3.one * (sphereRadius * 2f);

        Destroy(go.GetComponent<SphereCollider>());

        _renderer = go.GetComponent<MeshRenderer>();
        // Use URP lit shader if available, fallback to default
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        mat.color = colorIdle;
        _renderer.material = mat;
    }

    void Update()
    {
        bool grabbing = _interactor.interactablesSelected.Count > 0;
        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetColor("_BaseColor", grabbing ? colorGrab : colorIdle);
        _renderer.SetPropertyBlock(_mpb);
    }
}
