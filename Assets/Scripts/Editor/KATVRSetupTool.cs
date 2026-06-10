#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Activa/desactiva la locomoción por caminadora KAT VR en el Explorador.
///
/// PROBLEMA QUE RESUELVE: el PlayerController de la escena/prefab tenía 'useKatVR = false'
/// serializado (override de prefab en Explorador.unity). Con ese valor, InitKatVR() nunca
/// corre y el código de la caminadora jamás se ejecuta → al caminar sobre la KAT el personaje
/// no se mueve (espera joystick). Este tool pone useKatVR = true en TODOS los PlayerController
/// de la escena abierta y, opcionalmente, en el prefab Explorer_Player.
///
/// Es seguro tener useKatVR = true aunque NO haya caminadora: InitKatVR detecta que no hay
/// dispositivo y cae automáticamente a joystick.
///
/// Menús:
///   Tools → TITA → Explorador → Activar KAT VR (caminadora)
///   Tools → TITA → Explorador → Desactivar KAT VR (solo joystick)
/// </summary>
public static class KATVRSetupTool
{
    const string PREFAB_PATH = "Assets/Prefabs/Explorer_Player.prefab";

    [MenuItem("Tools/TITA/Explorador/Activar KAT VR (caminadora)")]
    public static void EnableKat() => SetKat(true);

    [MenuItem("Tools/TITA/Explorador/Desactivar KAT VR (solo joystick)")]
    public static void DisableKat() => SetKat(false);

    static void SetKat(bool value)
    {
        // ── 1. Componentes en la escena abierta ──────────────────────────────
        var controllers = Object.FindObjectsByType<PlayerController>(
            FindObjectsInactive.Include);

        int enEscena = 0;
        foreach (var pc in controllers)
        {
            Undo.RecordObject(pc, "Cambiar useKatVR");
            pc.useKatVR = value;
            EditorUtility.SetDirty(pc);
            EditorSceneManager.MarkSceneDirty(pc.gameObject.scene);
            enEscena++;
        }

        // ── 2. El prefab base (para que persista en nuevas instancias/builds) ─
        bool prefabHecho = false;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
        if (prefab != null)
        {
            var root = PrefabUtility.LoadPrefabContents(PREFAB_PATH);
            var pc = root.GetComponentInChildren<PlayerController>(true);
            if (pc != null)
            {
                pc.useKatVR = value;
                PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH);
                prefabHecho = true;
            }
            PrefabUtility.UnloadPrefabContents(root);
        }
        AssetDatabase.Refresh();

        string estado = value ? "ACTIVADA" : "desactivada (solo joystick)";
        EditorUtility.DisplayDialog("TITA — KAT VR",
            $"Locomoción KAT VR {estado}.\n\n" +
            $"PlayerController en escena: {enEscena} actualizado(s).\n" +
            $"Prefab Explorer_Player: {(prefabHecho ? "actualizado" : "no encontrado/omitido")}.\n\n" +
            (value
              ? "Al pulsar Play: si hay caminadora conectada (KAT Gateway abierto) se usa; " +
                "si no, cae a joystick automáticamente.\nRevisa la consola: logs '[PlayerController/KAT]'.\n\n"
              : "") +
            "Guarda la escena (Ctrl+S).\n\n" +
            "NOTA: no ejecutes después 'Tools → TITA → ... Configurar Explorador VR' " +
            "ni el ExplorerPrefabUpdater, porque vuelven a poner useKatVR = false.", "OK");

        Debug.Log($"[KATVRSetupTool] useKatVR = {value} → {enEscena} en escena, prefab={(prefabHecho ? "sí" : "no")}.");
    }
}
#endif
