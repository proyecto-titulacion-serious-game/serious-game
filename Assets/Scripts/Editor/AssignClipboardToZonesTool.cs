using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Asigna el Clipboard_VR (GameObject con ExplorerTaskClipboard) al array 'targets' de las
/// ZoneHUDTrigger, para que aparezca/desaparezca con la zona. Te ahorra cazar el clipboard
/// dentro del prefab anidado (ExplorerWorkstation / Explorer_Player).
///
/// Uso: selecciona en la Jerarquía las ZoneHUDTrigger donde quieras el clipboard y ejecuta el
/// menú. Si no seleccionas ninguna, se asigna a TODAS las zonas de la escena.
///
/// Menú: Tools → TITA → Reto 4 → Asignar Clipboard_VR a Zona(s) HUD
/// </summary>
public static class AssignClipboardToZonesTool
{
    [MenuItem("Tools/TITA/Reto 4/Asignar Clipboard_VR a Zona(s) HUD")]
    public static void Assign()
    {
        var clip = Object.FindAnyObjectByType<ExplorerTaskClipboard>(FindObjectsInactive.Include);
        if (clip == null)
        {
            EditorUtility.DisplayDialog("Clipboard_VR",
                "No se encontró ningún ExplorerTaskClipboard en la escena.\n" +
                "Asegúrate de que ExplorerWorkstation o Explorer_Player estén instanciados en Explorador.unity.",
                "Cerrar");
            return;
        }
        GameObject clipGO = clip.gameObject;

        // Zonas destino: las seleccionadas, o todas si no hay selección.
        var selected = Selection.gameObjects
            .Select(g => g.GetComponentInParent<ZoneHUDTrigger>())
            .Where(z => z != null)
            .Distinct()
            .ToList();

        var zones = selected.Count > 0
            ? selected
            : Object.FindObjectsByType<ZoneHUDTrigger>(FindObjectsInactive.Include).ToList();

        if (zones.Count == 0)
        {
            EditorUtility.DisplayDialog("Clipboard_VR",
                "No hay ninguna ZoneHUDTrigger en la escena.\n" +
                "Crea una con  Tools → TITA → Reto 4 → Crear Zona HUD (trigger).",
                "Cerrar");
            return;
        }

        int changed = 0;
        foreach (var z in zones)
        {
            var list = z.targets != null ? z.targets.ToList() : new List<GameObject>();
            if (list.Contains(clipGO)) continue;     // ya asignado
            list.Add(clipGO);
            z.targets = list.ToArray();
            EditorUtility.SetDirty(z);
            changed++;
        }

        if (changed > 0)
            EditorSceneManager.MarkSceneDirty(zones[0].gameObject.scene);

        Debug.Log($"[Clipboard_VR] '{clipGO.name}' asignado a {changed} zona(s) " +
                  $"(de {zones.Count} {(selected.Count > 0 ? "seleccionada(s)" : "en escena")}). " +
                  "Guarda la escena (Ctrl+S). Si prefieres ocultar otro objeto (p.ej. el modelo " +
                  "físico padre), ajusta el target a mano en el Inspector de la zona.");

        Selection.activeObject = clipGO;
        EditorGUIUtility.PingObject(clipGO);
    }
}
