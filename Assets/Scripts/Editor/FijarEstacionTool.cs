using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Herramienta de editor: añade <see cref="EstacionFijaAlMundo"/> a la raíz del prefab
/// Explorer_Player y lo cablea con los objetos de la estación, para que queden fijos en el
/// mundo (no sigan al jugador). Usa la API de prefabs (seguro, no edita el YAML a mano).
///
/// Menú: Tools → TITA → Explorador → Fijar estación al mundo
/// </summary>
public static class FijarEstacionTool
{
    const string PrefabPath = "Assets/Prefabs/Explorer_Player.prefab";

    // Objetos de la estación que deben quedarse quietos en el mundo.
    // (Clipboard_VR y Explorer_StatusPanel se dejan FUERA a propósito: siguen al jugador como HUD.)
    static readonly string[] NombresEstacion =
    {
        "Bandeja_Recepcion",
        "ValidationStation_VR",
    };

    [MenuItem("Tools/TITA/Explorador/Fijar estación al mundo")]
    public static void FijarEstacion()
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (root == null)
        {
            EditorUtility.DisplayDialog("Fijar estación",
                $"No se pudo cargar el prefab:\n{PrefabPath}", "OK");
            return;
        }

        try
        {
            var comp = root.GetComponent<EstacionFijaAlMundo>();
            if (comp == null) comp = root.AddComponent<EstacionFijaAlMundo>();

            var encontrados = new List<Transform>();
            var faltantes   = new List<string>();

            foreach (var nombre in NombresEstacion)
            {
                var t = BuscarHijoPorNombre(root.transform, nombre);
                if (t != null) encontrados.Add(t);
                else            faltantes.Add(nombre);
            }

            comp.objetosFijos = encontrados.ToArray();

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);

            string msg = $"Estación fijada al mundo.\n\nObjetos cableados ({encontrados.Count}):\n  " +
                         string.Join("\n  ", encontrados.ConvertAll(t => t.name));
            if (faltantes.Count > 0)
                msg += "\n\nNo encontrados (revisa los nombres):\n  " + string.Join("\n  ", faltantes);

            Debug.Log("[FijarEstacion] " + msg);
            EditorUtility.DisplayDialog("Fijar estación", msg, "OK");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static Transform BuscarHijoPorNombre(Transform root, string nombre)
    {
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == nombre) return t;
        return null;
    }
}
