using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;

/// <summary>
/// Auto-reparador del Explorador VR (RobotKyle). Se ejecuta solo, sin necesidad de
/// colocar nada en la escena, tanto en el Editor (Play) como en el build del Quest.
///
/// Arregla 3 problemas reportados con RobotKyle_Explorer en VR:
///
///   1. MANOS QUE SALTAN — El TrackedPoseDriver de cada mano tenía un binding
///      corrupto: además de "&lt;XRController&gt;{Hand}/devicePosition" (correcto)
///      también "&lt;XRHMD&gt;/...EyePosition" (la posición del OJO del visor). La mano
///      oscilaba entre el mando y la cámara. Aquí se borran los bindings de ojo/HMD,
///      dejando solo el del controlador.
///
///   2. CÁMARA / CUERPO TAPA LA VISTA + tracking — Se fuerza que el TrackedPoseDriver
///      de la cámara esté habilitado, y se desactiva la descarga online de
///      RPMExplorerAvatar para que el cuerpo sea SIEMPRE RobotKyle (sin swap en
///      runtime que rompía el ocultado de la cabeza).
///
///   3. CAMINAR CON JITTER — Doble locomoción: PlayerController (joystick) y el
///      ContinuousMoveProvider de XRI movían al jugador a la vez. Se desactiva el
///      de XRI cuando PlayerController está presente, dejando una sola fuente.
///
/// Es idempotente y seguro: si algo no existe simplemente lo omite.
/// </summary>
public static class ExplorerVRAutoFix
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Run()
    {
        // Solo en la escena del Explorador: si no hay PlayerController/ExplorerAvatar,
        // no es esta escena y no tocamos nada.
        var player = Object.FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
        var avatar = Object.FindAnyObjectByType<ExplorerAvatar>(FindObjectsInactive.Include);
        if (player == null && avatar == null) return;

        FixHandTrackedPoseDrivers();
        FixCameraTracking();
        ForceRobotKyleAvatar();
        FixDoubleLocomotion(player);

        // Conexión de los brazos de RobotKyle a los mandos (RobotHandIK). Se hace en
        // diferido porque las manos procedurales y el avatar se construyen en sus Start().
        var runner = new GameObject("[ExplorerVRAutoFixRunner]");
        runner.AddComponent<ExplorerVRAutoFixRunner>();

        Debug.Log("[ExplorerVRAutoFix] Reparación VR aplicada (manos / cámara / caminar).");
    }

    // ─────────────────────────────────────────────────────────
    //  1. Manos — quitar bindings de ojo/HMD del TrackedPoseDriver
    // ─────────────────────────────────────────────────────────
    static void FixHandTrackedPoseDrivers()
    {
        // Las manos procedurales del Explorador llevan HandModelController.
        var hands = Object.FindObjectsByType<HandModelController>(FindObjectsInactive.Include);

        foreach (var h in hands)
        {
            var tpd = h.GetComponent<TrackedPoseDriver>();
            if (tpd == null) continue;

            StripHmdBindings(tpd.positionInput, h.name + ".position");
            StripHmdBindings(tpd.rotationInput, h.name + ".rotation");

            // El controlador es lo único que debe mover la mano.
            tpd.enabled = true;
        }
    }

    static void StripHmdBindings(InputActionProperty prop, string label)
    {
        // Si usa una InputActionReference compartida no la tocamos (evita afectar otras cosas).
        if (prop.reference != null) return;

        InputAction act = prop.action;
        if (act == null) return;

        bool wasEnabled = act.enabled;
        try
        {
            act.Disable();
            int removed = 0;
            for (int i = act.bindings.Count - 1; i >= 0; i--)
            {
                string path = act.bindings[i].path;
                if (string.IsNullOrEmpty(path)) continue;

                bool isHmd = path.Contains("XRHMD")
                          || path.IndexOf("Eye", System.StringComparison.OrdinalIgnoreCase) >= 0
                          || path.IndexOf("centerEye", System.StringComparison.OrdinalIgnoreCase) >= 0;

                if (isHmd)
                {
                    act.ChangeBinding(i).Erase();
                    removed++;
                }
            }
            if (removed > 0)
                Debug.Log($"[ExplorerVRAutoFix] {label}: {removed} binding(s) de HMD/ojo eliminado(s).");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[ExplorerVRAutoFix] No se pudo limpiar bindings de {label}: {e.Message}");
        }
        finally
        {
            if (wasEnabled) act.Enable();
        }
    }

    // ─────────────────────────────────────────────────────────
    //  2a. Cámara — asegurar que su TrackedPoseDriver esté activo
    // ─────────────────────────────────────────────────────────
    static void FixCameraTracking()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
            {
                if (c.name.Contains("Camera") || c.stereoEnabled) { cam = c; break; }
            }
        }
        if (cam == null) return;

        var tpd = cam.GetComponent<TrackedPoseDriver>();
        if (tpd != null && !tpd.enabled)
        {
            tpd.enabled = true;
            Debug.Log("[ExplorerVRAutoFix] TrackedPoseDriver de la cámara re-habilitado.");
        }
    }

    // ─────────────────────────────────────────────────────────
    //  2b. Cuerpo — forzar RobotKyle fijo (sin descarga online de RPM)
    // ─────────────────────────────────────────────────────────
    static void ForceRobotKyleAvatar()
    {
        // RPMExplorerAvatar descarga un avatar Ready Player Me y lo swapea en runtime,
        // lo que rompe el ocultado de la cabeza de RobotKyle. El usuario eligió
        // "RobotKyle fijo": desactivamos la descarga ANTES de que corra su Start().
        var rpm = Object.FindAnyObjectByType<RPMExplorerAvatar>(FindObjectsInactive.Include);
        if (rpm != null && rpm.allowOnlineDownload)
        {
            rpm.allowOnlineDownload = false;
            Debug.Log("[ExplorerVRAutoFix] RPMExplorerAvatar: descarga online desactivada → RobotKyle fijo.");
        }

        // Refuerzo: que el avatar oculte la cabeza en VR (evita que tape el visor).
        var avatar = Object.FindAnyObjectByType<ExplorerAvatar>(FindObjectsInactive.Include);
        if (avatar != null)
        {
            avatar.hideHeadInVR = true;
            avatar.HideHead();
        }
    }

    // ─────────────────────────────────────────────────────────
    //  3. Caminar — eliminar la doble locomoción
    // ─────────────────────────────────────────────────────────
    static void FixDoubleLocomotion(PlayerController player)
    {
        // Si PlayerController maneja el joystick, el ContinuousMoveProvider de XRI
        // sobra y provoca movimiento doble / jitter. Lo desactivamos.
        if (player == null || !player.enabled) return;

        var movers = Object.FindObjectsByType<ContinuousMoveProvider>(FindObjectsInactive.Include);

        int disabled = 0;
        foreach (var m in movers)
        {
            if (m.enabled) { m.enabled = false; disabled++; }
        }
        if (disabled > 0)
            Debug.Log($"[ExplorerVRAutoFix] {disabled} ContinuousMoveProvider de XRI desactivado(s) " +
                      "(PlayerController es la única locomoción). El giro de XRI se mantiene.");
    }
}
