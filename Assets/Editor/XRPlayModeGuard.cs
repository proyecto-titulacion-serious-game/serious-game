#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Management;

/// Previene "Call to StopSubsystems without an initialized manager" en el Editor.
///
/// Cuando se sale del Play Mode, Unity llama OnDisable() en XRManagerSettings que a
/// su vez llama StopSubsystems() — incluso si XR nunca se inicializó (sin headset).
/// Este script intercepta la transición ExitingPlayMode y llama DeinitializeLoader()
/// ANTES de que Unity dispare los OnDisable, dejando el manager en estado limpio.
[InitializeOnLoad]
static class XRPlayModeGuard
{
    static XRPlayModeGuard()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingPlayMode) return;

        var settings = XRGeneralSettings.Instance;
        if (settings?.Manager == null) return;

        try
        {
            if (settings.Manager.activeLoader != null)
            {
                settings.Manager.StopSubsystems();
                settings.Manager.DeinitializeLoader();
                Debug.Log("[XRPlayModeGuard] XR desinicializado limpiamente al salir del Play Mode.");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[XRPlayModeGuard] Error al detener XR: {e.Message}");
        }
    }
}
#endif
