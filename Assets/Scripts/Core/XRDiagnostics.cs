using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

// Aliases to avoid ambiguity between UnityEngine.XR and UnityEngine.InputSystem
using XRInputDevice   = UnityEngine.XR.InputDevice;
using XRCommonUsages  = UnityEngine.XR.CommonUsages;

/// <summary>
/// Attach to any GameObject in the scene. Logs XR initialization state,
/// registered devices, and input device data on Start (and every N seconds).
/// Check the Console window while in Play mode to diagnose Quest Link issues.
/// </summary>
public class XRDiagnostics : MonoBehaviour
{
    [Tooltip("Repeat diagnostics every N seconds (0 = only on Start)")]
    public float repeatInterval = 5f;

    void Start()
    {
        StartCoroutine(DiagnoseLoop());
    }

    IEnumerator DiagnoseLoop()
    {
        yield return null;
        yield return null;

        LogAll();

        if (repeatInterval > 0f)
        {
            while (true)
            {
                yield return new WaitForSeconds(repeatInterval);
                LogAll();
            }
        }
    }

    void LogAll()
    {
        LogXRManagement();
        LogXRDevices();
        LogInputSystemDevices();
    }

    void LogXRManagement()
    {
        var gen = XRGeneralSettings.Instance;
        if (gen == null)
        {
            Debug.LogError("[XRDiag] XRGeneralSettings.Instance is NULL — XR Plugin Management not initialized.");
            return;
        }

        var mgr = gen.Manager;
        if (mgr == null)
        {
            Debug.LogError("[XRDiag] XRGeneralSettings.Manager is NULL — no XR loader running.");
            return;
        }

        Debug.Log($"[XRDiag] XR active loader: {(mgr.activeLoader != null ? mgr.activeLoader.name : "NONE")}");

        var displays = new List<XRDisplaySubsystem>();
        SubsystemManager.GetSubsystems(displays);
        foreach (var d in displays)
            Debug.Log($"[XRDiag] Display subsystem '{d.subsystemDescriptor.id}' running={d.running}");

        var inputs = new List<XRInputSubsystem>();
        SubsystemManager.GetSubsystems(inputs);
        foreach (var i in inputs)
            Debug.Log($"[XRDiag] Input subsystem '{i.subsystemDescriptor.id}' running={i.running}");

        if (displays.Count == 0)
            Debug.LogWarning("[XRDiag] No XR Display subsystems — OpenXR may not have initialized. Set Meta Quest Link as Active OpenXR Runtime in Meta Horizon Link → Settings → General.");
        if (inputs.Count == 0)
            Debug.LogWarning("[XRDiag] No XR Input subsystems — head/hand tracking will not work.");
    }

    void LogXRDevices()
    {
        var devices = new List<XRInputDevice>();
        InputDevices.GetDevices(devices);

        if (devices.Count == 0)
        {
            Debug.LogWarning("[XRDiag] No XR InputDevices registered — Meta Quest Link may not be the active OpenXR runtime.");
            return;
        }

        foreach (var dev in devices)
        {
            dev.TryGetFeatureValue(XRCommonUsages.isTracked, out bool tracked);
            dev.TryGetFeatureValue(XRCommonUsages.devicePosition, out Vector3 pos);
            dev.TryGetFeatureValue(XRCommonUsages.deviceRotation, out Quaternion rot);
            Debug.Log($"[XRDiag] XR device: '{dev.name}' tracked={tracked} pos={pos} rot={rot.eulerAngles}");
        }
    }

    void LogInputSystemDevices()
    {
        bool foundHMD   = false;
        bool foundLeft  = false;
        bool foundRight = false;

        foreach (var dev in InputSystem.devices)
        {
            if (dev is XRHMD hmd)
            {
                foundHMD = true;
                Debug.Log($"[XRDiag] InputSystem HMD: {hmd.name} ({hmd.GetType().Name})");
            }

            if (dev is XRController xrc)
            {
                string usageStr = string.Join(",", xrc.usages.Select(u => u.ToString()));
                Debug.Log($"[XRDiag] InputSystem Controller: {xrc.name} usages=[{usageStr}]");

                if (xrc.usages.Any(u => u == "LeftHand"))  foundLeft  = true;
                if (xrc.usages.Any(u => u == "RightHand")) foundRight = true;
            }
        }

        if (!foundHMD)
            Debug.LogWarning("[XRDiag] No XRHMD in InputSystem — <XRHMD>/centerEyeRotation binding will not receive data.");
        if (!foundLeft)
            Debug.LogWarning("[XRDiag] No LeftHand XRController — <XRController>{LeftHand}/devicePosition binding will not receive data.");
        if (!foundRight)
            Debug.LogWarning("[XRDiag] No RightHand XRController — <XRController>{RightHand}/devicePosition binding will not receive data.");
    }
}
