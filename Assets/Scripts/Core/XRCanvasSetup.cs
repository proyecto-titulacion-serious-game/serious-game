using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// Reemplaza GraphicRaycaster por TrackedDeviceGraphicRaycaster en todos los
/// Canvas WorldSpace cuando hay un HMD activo.
///
/// La causa del error "Screen position out of view frustum (-nan, -nan)":
///   InputSystemUIInputModule envía la posición del controlador XR (world-space)
///   a GraphicRaycaster → intenta proyectarla a coordenadas de pantalla → NaN.
///   Solución: usar TrackedDeviceGraphicRaycaster, que acepta posiciones 3D.
///
/// SETUP: Agregar este componente a cualquier GameObject de la escena (p.ej. el
/// mismo que tiene CircuitManager o TechnicianController).
[DefaultExecutionOrder(-50)]
public class XRCanvasSetup : MonoBehaviour
{
    void Awake()
    {
        if (IsXRActive())
            FixWorldSpaceCanvases();
    }

    static bool IsXRActive()
    {
        if (UnityEngine.XR.XRSettings.isDeviceActive) return true;
        var mgr = XRGeneralSettings.Instance?.Manager;
        return mgr != null && mgr.activeLoader != null;
    }

    static void FixWorldSpaceCanvases()
    {
        int fixed_ = 0;
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include))
        {
            if (canvas.renderMode != RenderMode.WorldSpace) continue;

            var gr = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (gr != null)
            {
                Destroy(gr);
                fixed_++;
            }

            if (canvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
        }

        if (fixed_ > 0)
            Debug.Log($"[XRCanvasSetup] {fixed_} GraphicRaycaster(s) reemplazados por TrackedDeviceGraphicRaycaster.");
    }
}
