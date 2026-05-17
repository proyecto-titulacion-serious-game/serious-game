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

    [Header("Debug / Override")]
    [Tooltip("Fuerza modo PC aunque haya dispositivos XR en escena (útil al probar sin headset)")]
    public bool forcePCMode = false;

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

    void LateUpdate()
    {
        // En modo PC, el sistema XR o el Input System pueden re-bloquear el cursor
        // en frames posteriores al Start. LateUpdate lo corrige cada frame.
        if (_activeMode == TechnicianMode.PC)
        {
            if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
            if (!Cursor.visible)                          Cursor.visible   = true;
        }
    }

    // ─────────────────────────────────────────────
    //  Detección automática de modo
    // ─────────────────────────────────────────────

    TechnicianMode DetectMode()
    {
        if (forcePCMode)          return TechnicianMode.PC;
        if (mode != TechnicianMode.Auto) return mode;

        // Filtrar solo dispositivos HMD reales (ignorar Mock HMD del paquete XR)
        var xrDevices = new System.Collections.Generic.List<UnityEngine.XR.InputDevice>();
        UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(
            UnityEngine.XR.InputDeviceCharacteristics.HeadMounted, xrDevices);

        // El Mock HMD aparece como HeadMounted pero no está realmente conectado:
        // verificar que al menos uno reporte isValid = true
        bool realHMD = false;
        foreach (var d in xrDevices)
        {
            if (d.isValid && !d.name.ToLowerInvariant().Contains("mock"))
            {
                realHMD = true;
                break;
            }
        }

        Debug.Log($"[TechnicianController] Dispositivos HMD: {xrDevices.Count}, HMD real: {realHMD}");
        return realHMD ? TechnicianMode.VR : TechnicianMode.PC;
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
        if (pcCamera           != null) pcCamera.gameObject.SetActive(true);
        if (xrOriginTechnician != null) xrOriginTechnician.SetActive(false);

        Camera cam = pcCamera != null ? pcCamera : Camera.main;

        if (cam != null)
        {
            // Para cada Canvas WorldSpace: asignar cámara y GraphicRaycaster si faltan.
            // Sin GraphicRaycaster el EventSystem no puede rutear clicks al canvas.
            // Sin worldCamera el GraphicRaycaster no puede convertir coordenadas de pantalla.
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include))
            {
                if (canvas.renderMode != RenderMode.WorldSpace) continue;

                if (canvas.worldCamera == null)
                    canvas.worldCamera = cam;

                if (canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                    canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }

            // El canvas principal del técnico se convierte a ScreenSpaceCamera para
            // que sea siempre visible y legible desde la cámara fija PC.
            if (technicianCanvas != null)
            {
                technicianCanvas.renderMode    = RenderMode.ScreenSpaceCamera;
                technicianCanvas.worldCamera   = cam;
                technicianCanvas.planeDistance = 1f;

                // Desactivar raycastTarget en imágenes decorativas para que los clicks
                // pasen al PhysicsRaycaster y lleguen a los DeskComponents 3D.
                foreach (var img in technicianCanvas.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                {
                    if (img.GetComponentInParent<UnityEngine.UI.Selectable>() == null)
                        img.raycastTarget = false;
                }
            }

            // PhysicsRaycaster enruta eventos de puntero del EventSystem a objetos 3D.
            if (cam.GetComponent<PhysicsRaycaster>() == null)
                cam.gameObject.AddComponent<PhysicsRaycaster>();
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
        var es = FindAnyObjectByType<EventSystem>();

#if ENABLE_INPUT_SYSTEM
        if (es == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            Debug.Log("[TechnicianController] EventSystem creado con InputSystemUIInputModule.");
            return;
        }

        // Si ya existe un EventSystem pero tiene StandaloneInputModule, reemplazarlo.
        // StandaloneInputModule no procesa teclado con New Input System activo,
        // por eso TMP_InputField no responde al teclado.
        var standalone = es.GetComponent<StandaloneInputModule>();
        if (standalone != null)
        {
            Destroy(standalone);
            if (es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
                es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            Debug.Log("[TechnicianController] StandaloneInputModule reemplazado por InputSystemUIInputModule.");
        }
#else
        if (es != null) return;
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
        Debug.Log("[TechnicianController] EventSystem creado con StandaloneInputModule.");
#endif
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