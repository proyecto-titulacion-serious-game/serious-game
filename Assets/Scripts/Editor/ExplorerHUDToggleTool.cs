using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Activa/desactiva el HUD flotante del Explorador (el Canvas WorldSpace que sigue la cabeza,
/// con <see cref="ExplorerHUDFollower"/> + <see cref="PlayerFeedbackUI"/> + <see cref="MultimeterUI"/>).
///
/// Es REDUNDANTE: el voltaje ya lo muestra la pantalla del multímetro ART (Screen_Canvas) y las
/// instrucciones ya están en el Clipboard_VR. Por eso suele molestar tenerlo pegado a la cara en VR.
///
/// El toggle es reversible (SetActive sobre el GO raíz del HUD, con Undo). No borra nada.
///
/// Menú: Tools → TITA → Explorador → HUD flotante (activar/desactivar)
/// </summary>
public static class ExplorerHUDToggleTool
{
    [MenuItem("Tools/TITA/Explorador/HUD flotante (activar-desactivar)")]
    public static void Toggle()
    {
        // El GO raíz del HUD es el que lleva el follower (Canvas que persigue la cabeza).
        var follower = Object.FindAnyObjectByType<ExplorerHUDFollower>(FindObjectsInactive.Include);
        GameObject hud = follower != null ? follower.gameObject : null;

        // Fallback: si no hay follower, usar la raíz de prefab del PlayerFeedbackUI.
        if (hud == null)
        {
            var feedback = Object.FindAnyObjectByType<PlayerFeedbackUI>(FindObjectsInactive.Include);
            if (feedback != null)
            {
                var root = PrefabUtility.GetOutermostPrefabInstanceRoot(feedback.gameObject);
                hud = root != null ? root : feedback.gameObject;
            }
        }

        if (hud == null)
        {
            EditorUtility.DisplayDialog("HUD flotante del Explorador",
                "No se encontró el ExplorerHUD en la escena activa " +
                "(ni ExplorerHUDFollower ni PlayerFeedbackUI).", "OK");
            return;
        }

        bool nuevoEstado = !hud.activeSelf;
        Undo.RecordObject(hud, "Toggle ExplorerHUD");
        hud.SetActive(nuevoEstado);
        EditorUtility.SetDirty(hud);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Selection.activeGameObject = hud;
        EditorGUIUtility.PingObject(hud);

        string estado = nuevoEstado ? "ACTIVADO" : "DESACTIVADO";
        Debug.Log($"[ExplorerHUDToggle] '{hud.name}' {estado}.");
        EditorUtility.DisplayDialog("HUD flotante del Explorador",
            $"'{hud.name}' ahora está {estado}.\n\n" +
            (nuevoEstado
                ? "El panel volverá a flotar frente al jugador."
                : "El jugador leerá del multímetro físico (pantalla) y del Clipboard_VR.\n" +
                  "Ctrl+Z lo revierte."),
            "OK");
    }
}
