using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// La escena del Explorador tenía DOS multímetros instanciados (Multimeter_VR + Multimeter_VR_Art),
/// es decir 2 componentes <see cref="Multimeter"/>. Tener más de uno confunde al jugador y a todo
/// código que hace <c>FindAnyObjectByType&lt;Multimeter&gt;()</c> (puede elegir el equivocado).
///
/// Esta herramienta deja UNO activo (el ART) y DESACTIVA los demás. Es reversible: solo hace
/// SetActive(false) sobre el GameObject raíz del multímetro sobrante (no lo borra). Si prefieres
/// borrarlos, marca la casilla en el diálogo.
///
/// Menú: Tools → TITA → Multímetro → Quitar duplicado (dejar solo ART)
/// </summary>
public static class MultimeterDeduplicateTool
{
    [MenuItem("Tools/TITA/Multímetro/Quitar duplicado (dejar solo ART)")]
    public static void Deduplicate()
    {
        var all = Object.FindObjectsByType<Multimeter>(FindObjectsInactive.Include);
        if (all.Length == 0)
        {
            EditorUtility.DisplayDialog("Multímetro duplicado",
                "No hay ningún Multimeter en la escena activa.", "OK");
            return;
        }
        if (all.Length == 1)
        {
            EditorUtility.DisplayDialog("Multímetro duplicado",
                $"Solo hay 1 Multimeter ('{all[0].name}'). Nada que deduplicar.", "OK");
            return;
        }

        // Elegir el que se conserva: preferir el ART (mismo criterio que MultimeterArtWiringTool).
        Multimeter keep = ResolveArt(all);

        // Listar los sobrantes (los que NO son 'keep').
        var sobrantes = new List<Multimeter>();
        foreach (var m in all)
            if (m != keep) sobrantes.Add(m);

        var sb = new StringBuilder();
        sb.AppendLine($"Se CONSERVA: '{keep.name}'  (raíz: '{RootOf(keep).name}')");
        sb.AppendLine();
        sb.AppendLine($"Se DESACTIVARÁN ({sobrantes.Count}):");
        foreach (var m in sobrantes)
            sb.AppendLine($"  • '{m.name}'  (raíz: '{RootOf(m).name}')");

        bool borrar = EditorUtility.DisplayDialogComplex(
            "Quitar multímetro duplicado",
            sb.ToString() + "\n¿Desactivar (reversible) o BORRAR los sobrantes?",
            "Desactivar",   // 0
            "Cancelar",     // 1
            "Borrar") switch
        {
            0 => false,
            2 => true,
            _ => Cancelled()
        };

        if (_cancelled) { _cancelled = false; return; }

        int afectados = 0;
        foreach (var m in sobrantes)
        {
            var root = RootOf(m).gameObject;
            if (borrar)
            {
                Undo.DestroyObjectImmediate(root);
            }
            else
            {
                Undo.RecordObject(root, "Desactivar multímetro duplicado");
                root.SetActive(false);
                EditorUtility.SetDirty(root);
            }
            afectados++;
        }

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Selection.activeGameObject = keep.gameObject;
        EditorGUIUtility.PingObject(keep.gameObject);

        string verbo = borrar ? "borrados" : "desactivados";
        EditorUtility.DisplayDialog("Multímetro duplicado",
            $"Conservado: '{keep.name}'.\nSobrantes {verbo}: {afectados}.\n\n" +
            "Recuerda re-cablear referencias si algún script apuntaba al multímetro borrado " +
            "(Tools → TITA → Multímetro → Cablear Multímetro ART en escena).", "OK");

        Debug.Log($"[MultimeterDeduplicate] Conservado '{keep.name}', {verbo} {afectados}.");
    }

    static bool _cancelled;
    static bool Cancelled() { _cancelled = true; return false; }

    /// <summary>Elige el multímetro ART (nombre contiene 'Art'); si ninguno, el primero.</summary>
    static Multimeter ResolveArt(Multimeter[] all)
    {
        foreach (var m in all)
            if (m.name.Contains("Art") || m.name.Contains("VR_Art") || RootOf(m).name.Contains("Art"))
                return m;
        return all[0];
    }

    /// <summary>
    /// Raíz lógica del multímetro a desactivar. Sube a la raíz del prefab SOLO si esa raíz
    /// es realmente del multímetro (su nombre contiene "Multimeter"/"Multímetro"), para no
    /// desactivar por error una workstation/mesa que lo contenga como hijo. En caso contrario
    /// usa el GameObject del propio componente.
    /// </summary>
    static Transform RootOf(Multimeter m)
    {
        var prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(m.gameObject);
        if (prefabRoot != null)
        {
            string n = prefabRoot.name.ToLowerInvariant();
            if (n.Contains("multimeter") || n.Contains("multimetro") || n.Contains("multímetro"))
                return prefabRoot.transform;
        }
        return m.transform;
    }
}
