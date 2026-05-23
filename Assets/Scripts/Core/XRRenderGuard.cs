using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.Rendering.Universal;

/// Prevents IndexOutOfRangeException in XRDisplaySubsystem.GetRenderPass when
/// Quest Link is initialized but reports 0 render passes temporarily.
/// Uses a debounce so a single-frame dip in render pass count doesn't flicker the view.
[RequireComponent(typeof(Camera))]
public class XRRenderGuard : MonoBehaviour
{
    [Tooltip("Eye height (m) used when no XR headset is detected")]
    public float fallbackEyeHeight = 1.36f;

    [Tooltip("Consecutive 'not ready' frames required before disabling XR rendering (prevents flicker)")]
    public int disableDebounceFrames = 10;

    private UniversalAdditionalCameraData _camData;
    private readonly List<XRDisplaySubsystem> _displays = new();
    private bool _xrRenderingEnabled = true;
    private int _notReadyFrameCount = 0;

    void Awake()
    {
        _camData = GetComponent<UniversalAdditionalCameraData>();
        if (_camData == null)
            Debug.LogError("[XRRenderGuard] No UniversalAdditionalCameraData found.", this);
    }

    void Start()
    {
        ApplyFallbackHeight();
        if (_camData != null)
            _camData.allowXRRendering = true;
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

        if (xrReady)
        {
            _notReadyFrameCount = 0;
            if (!_xrRenderingEnabled)
            {
                _xrRenderingEnabled = true;
                _camData.allowXRRendering = true;
                Debug.Log("[XRRenderGuard] XR rendering ENABLED on " + gameObject.name);
            }
        }
        else
        {
            ApplyFallbackHeight();
            _notReadyFrameCount++;
            if (_xrRenderingEnabled && _notReadyFrameCount >= disableDebounceFrames)
            {
                _xrRenderingEnabled = false;
                _camData.allowXRRendering = false;
                Debug.Log("[XRRenderGuard] XR rendering DISABLED on " + gameObject.name);
            }
        }
    }

    void ApplyFallbackHeight()
    {
        var pos = transform.localPosition;
        if (Mathf.Abs(pos.y - fallbackEyeHeight) > 0.001f)
        {
            pos.y = fallbackEyeHeight;
            transform.localPosition = pos;
        }
    }
}
