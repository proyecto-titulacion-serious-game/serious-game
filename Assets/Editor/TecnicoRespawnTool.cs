using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Reubica el rig del Técnico (TechnicianRobot/TechnicianMover) al PISO DESPEJADO más cercano,
/// para que no spawnee atascado entre los muebles del Japan Office (lo que lo hacía caminar lento).
///
///   Tools → TITA → Técnico → Reubicar a piso despejado
///
/// REQUISITO: NoonA debe estar cargada para que existan sus colliders. En el editor:
///   File → Open Scene Additive → NoonA.unity   (junto con Tecnico.unity)
/// Luego ejecuta el menú. Busca en anillos crecientes alrededor del rig una posición donde la
/// cápsula del personaje quepa sin tocar nada (salvo el propio rig) sobre piso sólido.
/// </summary>
public static class TecnicoRespawnTool
{
    [MenuItem("Tools/TITA/Técnico/Reubicar a piso despejado")]
    static void Relocate()
    {
        var mover = Object.FindAnyObjectByType<TechnicianMover>(FindObjectsInactive.Include);
        if (mover == null)
        {
            EditorUtility.DisplayDialog("TITA — Reubicar Técnico",
                "No se encontró el rig (TechnicianMover). Abre Tecnico.unity.", "OK");
            return;
        }
        Transform rig = mover.transform;

        // Colliders del PROPIO rig (para excluirlos del chequeo de despeje).
        var propios = new HashSet<Collider>(rig.GetComponentsInChildren<Collider>(true));

        var cc = rig.GetComponent<CharacterController>();
        float radius = cc != null ? cc.radius : 0.3f;
        float height = cc != null ? cc.height : 1.8f;

        Vector3 start = rig.position;
        Vector3 found = Vector3.zero;
        bool ok = false;

        // Anillos crecientes (cada 0.5 m hasta 12 m) alrededor de la posición actual.
        for (float r = 0f; r <= 12f && !ok; r += 0.5f)
        {
            int steps = Mathf.Max(1, Mathf.RoundToInt(r * 6f));
            for (int i = 0; i < steps && !ok; i++)
            {
                float ang = i / (float)steps * Mathf.PI * 2f;
                float x = start.x + Mathf.Cos(ang) * r;
                float z = start.z + Mathf.Sin(ang) * r;

                // Buscar el piso con un raycast hacia abajo.
                if (!Physics.Raycast(new Vector3(x, start.y + 3f, z), Vector3.down, out var hit, 10f))
                    continue;
                Vector3 floor = hit.point;

                // ¿Cabe la cápsula del personaje sin tocar nada que no sea el rig?
                Vector3 p1 = floor + Vector3.up * (radius + 0.02f);
                Vector3 p2 = floor + Vector3.up * (height - radius);
                var overlaps = Physics.OverlapCapsule(p1, p2, radius * 0.9f, ~0, QueryTriggerInteraction.Ignore);
                bool clear = true;
                foreach (var o in overlaps)
                    if (!propios.Contains(o)) { clear = false; break; }

                if (clear) { found = floor + Vector3.up * 0.05f; ok = true; }
            }
        }

        if (!ok)
        {
            EditorUtility.DisplayDialog("TITA — Reubicar Técnico",
                "No encontré piso despejado cerca.\n\n¿Está NoonA cargada? Haz " +
                "File → Open Scene Additive → NoonA.unity y vuelve a ejecutar.", "OK");
            return;
        }

        Undo.RecordObject(rig, "Reubicar Técnico a piso despejado");
        Vector3 antes = rig.position;
        rig.position = found;
        EditorUtility.SetDirty(rig);
        EditorSceneManager.MarkSceneDirty(rig.gameObject.scene);
        Selection.activeGameObject = rig.gameObject;

        EditorUtility.DisplayDialog("TITA — Reubicar Técnico",
            $"Técnico reubicado a piso despejado.\n\nAntes: {antes}\nAhora: {found}\n\n" +
            "GUARDA la escena del rig (Ctrl+S).", "OK");
        Debug.Log($"[TecnicoRespawn] Rig movido de {antes} a {found}.");
    }
}
