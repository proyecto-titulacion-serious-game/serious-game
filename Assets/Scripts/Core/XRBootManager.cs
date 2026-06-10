using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// Se auto-crea en el primer frame cuando hay hardware XR real activo.
/// Resuelve dos problemas que impiden que el juego funcione con Meta Quest via Link:
///
///   1. XR Device Simulator (o XR Interaction Simulator) se instancia automáticamente
///      desde XRDeviceSimulatorSettings aunque haya hardware real conectado.
///      Esto corrompe el tracking y hace que el juego se cierre.
///
///   2. Canvases WorldSpace con GraphicRaycaster estándar producen
///      "Screen position out of view frustum (-nan)" cada frame porque
///      InputSystemUIInputModule envía posiciones 3D del controlador XR al
///      raycaster 2D. Necesitan TrackedDeviceGraphicRaycaster.
///
/// NO necesita colocarse manualmente en ninguna escena.
[DefaultExecutionOrder(200)]
public class XRBootManager : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void TrimLogStackTraces()
    {
        // En el Editor cada Debug.Log normal captura un stack trace completo (visible en la consola).
        // Hacerlo varias veces por frame (calibración de altura, multímetro, avisos de OVR canvas)
        // genera GC y micro-stutters → "a veces baja el FPS". Quitamos la traza solo de los Log
        // normales; Warning y Error la conservan para no perder diagnóstico.
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        // Solo corre si hay XR real activo (Quest via Link, Quest standalone, etc.)
        if (!IsXRActive()) return;

        // Instancia única persistente entre escenas
        var go = new GameObject("[XRBootManager]");
        go.AddComponent<XRBootManager>();
        DontDestroyOnLoad(go);
    }

    void Start()
    {
        // Start() corre DESPUÉS de RuntimeInitializeOnLoadMethod(AfterSceneLoad),
        // así que el simulador ya está instanciado para este momento.
        DestroySimulators();
        FixWorldSpaceCanvases();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) => FixWorldSpaceCanvases();

    // ─────────────────────────────────────────────
    //  Destruir simuladores
    // ─────────────────────────────────────────────

    static void DestroySimulators()
    {
        // XR Device Simulator (paquete XRI)
        var deviceSim = FindAnyObjectByType<XRDeviceSimulator>(FindObjectsInactive.Include);
        if (deviceSim != null)
        {
            Destroy(deviceSim.gameObject);
            Debug.Log("[XRBootManager] XRDeviceSimulator destruido — hardware real detectado.");
        }

        // XR Interaction Simulator (sample de XRI — puede o no estar presente)
        foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
        {
            if (mb == null) continue;
            string typeName = mb.GetType().Name;
            if (typeName == "XRInteractionSimulator")
            {
                Destroy(mb.gameObject);
                Debug.Log("[XRBootManager] XRInteractionSimulator destruido.");
                break;
            }
        }
    }

    // ─────────────────────────────────────────────
    //  Reparar canvases WorldSpace
    // ─────────────────────────────────────────────

    static void FixWorldSpaceCanvases()
    {
        int replaced = 0;
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include))
        {
            if (canvas.renderMode != RenderMode.WorldSpace) continue;

            var gr = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (gr != null) { Destroy(gr); replaced++; }

            if (canvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
        }
        if (replaced > 0)
            Debug.Log($"[XRBootManager] {replaced} GraphicRaycaster → TrackedDeviceGraphicRaycaster (canvases WorldSpace).");
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────

    static bool IsXRActive()
    {
        if (UnityEngine.XR.XRSettings.isDeviceActive) return true;
        var mgr = XRGeneralSettings.Instance?.Manager;
        return mgr != null && mgr.activeLoader != null;
    }
}
