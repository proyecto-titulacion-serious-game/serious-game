using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

#if UNITY_XR_HANDS
using UnityEngine.XR.Hands;
#endif

/// <summary>
/// Complemento para XRDirectInteractor que activa el grab cuando detecta
/// gesto de pinch de la mano (pulgar + índice).
/// </summary>
[RequireComponent(typeof(XRBaseInteractor))]
public class HandPinchGrab : MonoBehaviour
{
    [Header("Configuración del pinch")]
    public Handedness handedness = Handedness.Right;

    [Tooltip("Umbral de distancia pulgar-índice (metros) para considerar pinch activo.")]
    [Range(0.005f, 0.05f)] public float pinchThreshold = 0.025f;

    private XRBaseInteractor _interactor;
    private bool             _pinchActive;

#if UNITY_XR_HANDS
    private XRHandSubsystem _handSubsystem;
    private bool            _subsystemReady;

    void OnEnable()
    {
        var subsystems = new System.Collections.Generic.List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(subsystems);
        if (subsystems.Count > 0)
        {
            _handSubsystem  = subsystems[0];
            _subsystemReady = true;
        }
    }
#endif

    void Awake()
    {
        _interactor = GetComponent<XRBaseInteractor>();
    }

    void Update()
    {
        bool pinchThisFrame = IsPinching();

        if (pinchThisFrame != _pinchActive)
        {
            _pinchActive = pinchThisFrame;

            // CORRECCIÓN: Permitir explorar (Hover) siempre, pero solo agarrar (Select) al hacer pinza.
            _interactor.allowSelect = _pinchActive;
        }
    }

    bool IsPinching()
    {
#if UNITY_XR_HANDS
        if (_subsystemReady && _handSubsystem != null)
        {
            XRHand hand = handedness == Handedness.Right
                ? _handSubsystem.rightHand
                : _handSubsystem.leftHand;

            if (hand.isTracked)
            {
                bool thumbOk  = hand.GetJoint(XRHandJointID.ThumbTip).TryGetPose(out Pose thumbTip);
                bool indexOk  = hand.GetJoint(XRHandJointID.IndexTip).TryGetPose(out Pose indexTip);

                if (thumbOk && indexOk)
                {
                    float dist = Vector3.Distance(thumbTip.position, indexTip.position);
                    return dist < pinchThreshold;
                }
            }
        }
#endif
        return ReadControllerTrigger();
    }

    bool ReadControllerTrigger()
    {
        try
        {
            var triggerAction = handedness == Handedness.Right
                ? UnityEngine.InputSystem.InputSystem.actions?.FindAction("XRI RightHand/Select")
                : UnityEngine.InputSystem.InputSystem.actions?.FindAction("XRI LeftHand/Select");

            return triggerAction != null && triggerAction.ReadValue<float>() > 0.7f;
        }
        catch { return false; }
    }
}

public enum Handedness { Left, Right }