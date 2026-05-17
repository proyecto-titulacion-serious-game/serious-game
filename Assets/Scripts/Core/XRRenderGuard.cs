using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.Rendering.Universal;

/// Prevents IndexOutOfRangeException in XRDisplaySubsystem.GetRenderPass when
/// Quest Link is initialized but reports 0 render passes. Also applies a fallback
/// eye height when no headset is active so the camera doesn't stay at floor level.
/// The TrackedPoseDriver (UpdateType=BeforeRender) overrides the fallback height
/// automatically once a headset connects, so this does not interfere with real VR.
[RequireComponent(typeof(Camera))]
public class XRRenderGuard : MonoBehaviour
{
    [Tooltip("Eye height (m) used when no XR headset is detected")]
    public float fallbackEyeHeight = 1.36f;

    private UniversalAdditionalCameraData _camData;
    private readonly List<XRDisplaySubsystem> _displays = new();
    private bool _lastXRReady = false;

    void Awake()
    {
        _camData = GetComponent<UniversalAdditionalCameraData>();
        if (_camData == null)
            Debug.LogError("[XRRenderGuard] No UniversalAdditionalCameraData found.", this);
    }

    void Start()
    {
        // Apply fallback immediately so the first frame isn't at floor level.
        // If XR is ready, the TrackedPoseDriver (BeforeRender) corrects it before rendering.
        ApplyFallbackHeight();
    }

    void Update()
    {
        if (_camData == null) return;

        SubsystemManager.GetSubsystems(_displays);

        bool xrReady = false;
        foreach (var display in _displays)
        {
            if (display.running && display.GetRenderPassCount() > 0)
            {
                xrReady = true;
                break;
            }
        }

        if (!xrReady)
            ApplyFallbackHeight();

        if (xrReady != _lastXRReady)
        {
            _camData.allowXRRendering = xrReady;
            _lastXRReady = xrReady;
            Debug.Log($"[XRRenderGuard] XR rendering {(xrReady ? "ENABLED" : "DISABLED")} on {gameObject.name}");
        }
    }

    void ApplyFallbackHeight()
    {
        var pos = transform.localPosition;
        if (pos.y != fallbackEyeHeight)
        {
            pos.y = fallbackEyeHeight;
            transform.localPosition = pos;
        }
    }
}
