using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Gatea todos los HUD del Explorador para que aparezcan SOLO al llegar a la zona de trabajo
/// (modo Posición + Reto), salvo la presentación inicial (ExplorerOnboarding), que NO se toca.
///
///   Tools → TITA → Explorador → Gatear HUDs por zona (Posición+Reto)
///
/// Qué hace:
///   1. Desactiva por completo el HUD flotante que persigue la cabeza (ExplorerHUDFollower),
///      por redundante (su info ya está en el multímetro diegético y el Clipboard).
///   2. Reúne los HUD a gatear: ExplorerTelemetryHUD, DeliveryTrayIndicator y Clipboard_VR.
///   3. En cada ZoneHUDTrigger de la escena: modo=PosicionYReto, cualquierReto=true,
///      startHidden=true, y añade esos HUD a 'targets' (sin duplicar).
///   4. La presentación (ExplorerOnboarding) NO se añade como target → sigue saliendo al inicio;
///      además ZoneHUDTrigger mantiene los HUD ocultos hasta que el onboarding termina.
/// </summary>
public static class HUDZoneGateSetup
{
    [MenuItem("Tools/TITA/Explorador/Gatear HUDs por zona (Posición+Reto)")]
    static void GateHUDs()
    {
        // ── 1. Desactivar el HUD flotante (sigue la cabeza) ──
        var follower = Object.FindAnyObjectByType<ExplorerHUDFollower>(FindObjectsInactive.Include);
        bool flotanteOff = false;
        if (follower != null && follower.gameObject.activeSelf)
        {
            Undo.RecordObject(follower.gameObject, "Desactivar HUD flotante");
            follower.gameObject.SetActive(false);
            EditorUtility.SetDirty(follower.gameObject);
            flotanteOff = true;
        }

        // ── 2. Reunir los HUD a gatear (NUNCA el onboarding) ──
        var targets = new List<GameObject>();
        AddIfPresent<ExplorerTelemetryHUD>(targets);
        AddIfPresent<DeliveryTrayIndicator>(targets);
        AddIfPresent<ExplorerTaskClipboard>(targets);   // Clipboard_VR

        if (targets.Count == 0)
        {
            EditorUtility.DisplayDialog("TITA — Gatear HUDs",
                "No se encontró ningún HUD a gatear (ExplorerTelemetryHUD / DeliveryTrayIndicator / Clipboard_VR).\n\n" +
                "Abre la escena Explorador.unity antes de ejecutar.", "OK");
            return;
        }

        // ── 3. Configurar los ZoneHUDTrigger ──
        var triggers = Object.FindObjectsByType<ZoneHUDTrigger>(FindObjectsInactive.Include);
        if (triggers.Length == 0)
        {
            EditorUtility.DisplayDialog("TITA — Gatear HUDs",
                "No hay ningún ZoneHUDTrigger en la escena.\n\n" +
                "Ejecuta primero  Tools → TITA → Explorador → ZoneHUD: uno por reto (1-4)  " +
                "para crear las zonas, y vuelve a ejecutar esto.", "OK");
            return;
        }

        foreach (var z in triggers)
        {
            Undo.RecordObject(z, "Gatear HUDs por zona");

            z.modo = ZoneHUDTrigger.ActivationMode.PosicionYReto;
            z.cualquierReto = true;
            z.startHidden = true;

            // Unión de targets actuales + nuevos, sin duplicar ni meter nulos.
            var union = new List<GameObject>();
            if (z.targets != null)
                union.AddRange(z.targets.Where(t => t != null));
            foreach (var t in targets)
                if (!union.Contains(t)) union.Add(t);

            z.targets = union.ToArray();
            EditorUtility.SetDirty(z);
        }

        EditorSceneManager.MarkSceneDirty(triggers[0].gameObject.scene);

        string lista = string.Join("\n  • ", targets.Select(t => t.name));
        EditorUtility.DisplayDialog("TITA — Gatear HUDs",
            $"Listo.\n\n" +
            $"HUD flotante: {(flotanteOff ? "DESACTIVADO" : "no encontrado / ya estaba off")}\n\n" +
            $"Gateados en {triggers.Length} ZoneHUDTrigger (modo Posición+Reto, cualquierReto):\n  • {lista}\n\n" +
            "La presentación (onboarding) NO se gatea: sale al inicio y los HUD esperan a que termine.\n\n" +
            "Recuerda GUARDAR la escena (Ctrl+S).", "OK");

        Debug.Log($"[HUDZoneGateSetup] {triggers.Length} ZoneHUDTrigger configurados (PosicionYReto, cualquierReto). " +
                  $"Targets: {string.Join(", ", targets.Select(t => t.name))}. HUD flotante off: {flotanteOff}.");
    }

    static void AddIfPresent<T>(List<GameObject> list) where T : Component
    {
        var comp = Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
        if (comp != null && !list.Contains(comp.gameObject))
            list.Add(comp.gameObject);
    }
}
