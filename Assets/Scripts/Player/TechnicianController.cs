using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

/// <summary>
/// Controlador del Técnico — soporta DOS modos de entrada:
///
///   MODO PC (TechnicianMode.PC):
///     • Mouse para hacer clic en botones del Canvas
///     • Teclado para escribir valores en el InputField
///     • Canvas en Screen Space - Camera
///     • Cámara fija apuntando al panel
///
///   MODO VR ESTÁTICO (TechnicianMode.VR):
///     • Sin locomoción (el Técnico no se mueve)
///     • Ray Interactor de la mano derecha apunta al Canvas World Space
///     • Trigger derecho = clic en botones
///     • Canvas en World Space frente al Técnico
///
/// El modo se detecta AUTOMÁTICAMENTE al inicio:
///   si hay un XRController conectado → VR, sino → PC.
/// También se puede forzar desde el inspector.
/// </summary>
public class TechnicianController : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Modo de entrada")]
    public TechnicianMode mode = TechnicianMode.Auto;

    [Header("Referencias — PC")]
    [Tooltip("Cámara fija del Técnico en modo PC")]
    public Camera  pcCamera;
    [Tooltip("Canvas del Técnico (Screen Space en PC, World Space en VR)")]
    public Canvas  technicianCanvas;

    [Header("Referencias — VR estático")]
    [Tooltip("XR Origin del Técnico (sin PlayerController, sin locomoción)")]
    public GameObject xrOriginTechnician;
    [Tooltip("Mano derecha del Técnico VR para apuntar al canvas")]
    public GameObject rightHandVR;

    [Header("Posición del canvas VR (World Space)")]
    [Tooltip("Distancia del canvas al frente del técnico")]
    public float canvasDistanceVR = 1.2f;

    // ─────────────────────────────────────────────
    //  Internos
    // ─────────────────────────────────────────────
    private TechnicianMode _activeMode;

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────
    void Start()
    {
        _activeMode = DetectMode();
        ApplyMode();
        Debug.Log($"[TechnicianController] Modo activo: {_activeMode}");
    }

    // ─────────────────────────────────────────────
    //  Detección automática de modo
    // ─────────────────────────────────────────────

    TechnicianMode DetectMode()
    {
        if (mode != TechnicianMode.Auto) return mode;

        // Si hay algún XRController conectado → VR
        var xrDevices = new System.Collections.Generic.List<UnityEngine.XR.InputDevice>();
        UnityEngine.XR.InputDevices.GetDevices(xrDevices);

        bool vrConnected = xrDevices.Count > 0;
        Debug.Log($"[TechnicianController] Dispositivos XR detectados: {xrDevices.Count}");
        return vrConnected ? TechnicianMode.VR : TechnicianMode.PC;
    }

    // ─────────────────────────────────────────────
    //  Configuración según modo
    // ─────────────────────────────────────────────

    void ApplyMode()
    {
        switch (_activeMode)
        {
            case TechnicianMode.PC:
                SetupPC();
                break;
            case TechnicianMode.VR:
                SetupVR();
                break;
        }
    }

    void SetupPC()
    {
        // Activar cámara PC, desactivar XR Origin del Técnico
        if (pcCamera          != null) pcCamera.gameObject.SetActive(true);
        if (xrOriginTechnician!= null) xrOriginTechnician.SetActive(false);

        // Canvas en Screen Space - Camera
        if (technicianCanvas != null)
        {
            technicianCanvas.renderMode       = RenderMode.ScreenSpaceCamera;
            technicianCanvas.worldCamera      = pcCamera;
            technicianCanvas.planeDistance    = 1f;
        }

        // Activar EventSystem para mouse
        EnsureEventSystem();

        // Cursor visible
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        Debug.Log("[TechnicianController] Modo PC configurado.");
    }

    void SetupVR()
    {
        // Desactivar cámara PC, activar XR Origin del Técnico
        if (pcCamera           != null) pcCamera.gameObject.SetActive(false);
        if (xrOriginTechnician != null) xrOriginTechnician.SetActive(true);

        // Canvas en World Space frente al técnico
        if (technicianCanvas != null)
        {
            technicianCanvas.renderMode = RenderMode.WorldSpace;

            // Posicionar el canvas frente al XR Origin
            if (xrOriginTechnician != null)
            {
                Transform t = xrOriginTechnician.transform;
                technicianCanvas.transform.position = t.position + t.forward * canvasDistanceVR + Vector3.up * 0.3f;
                technicianCanvas.transform.rotation = Quaternion.LookRotation(
                    technicianCanvas.transform.position - t.position);
            }

            // Escala del canvas World Space (0.001 = 1mm por unidad UI)
            technicianCanvas.transform.localScale = Vector3.one * 0.001f;
        }

        // Asegurarse de que la mano derecha tenga el XR Ray Interactor
        // apuntando al canvas (configurado en el prefab)
        Debug.Log("[TechnicianController] Modo VR Estático configurado.");
    }

    void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
            Debug.Log("[TechnicianController] EventSystem creado automáticamente.");
        }
    }

    // ─────────────────────────────────────────────
    //  API pública
    // ─────────────────────────────────────────────

    public TechnicianMode GetActiveMode() => _activeMode;
    public bool IsPC() => _activeMode == TechnicianMode.PC;
    public bool IsVR() => _activeMode == TechnicianMode.VR;

    /// <summary>
    /// Forzar cambio de modo en runtime (p.ej. botón de opciones).
    /// </summary>
    public void SwitchMode(TechnicianMode newMode)
    {
        _activeMode = newMode;
        ApplyMode();
    }
}

public enum TechnicianMode { Auto, PC, VR }