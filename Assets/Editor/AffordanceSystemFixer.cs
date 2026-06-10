#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Repara NullReferenceException del Affordance System de XRI 3.x.
///
/// Usa reflexión para no depender de los tipos exactos del package —
/// así compila aunque los nombres cambien entre versiones de XRI.
///
/// Menú: Tools → TITA → Fix → Reparar Affordance System
/// </summary>
public static class AffordanceSystemFixer
{
    // Fragmentos del nombre de tipo que identifican componentes del Affordance System
    static readonly string[] AffordanceTypeHints =
    {
        "AffordanceStateProvider",
        "AffordanceStateReceiver",
        "AffordanceReceiver",
        "AffordanceTheme",
        "AsyncAffordanceStateReceiver",
    };

    [MenuItem("Tools/TITA/Fix/Reparar Affordance System (NullReference)")]
    static void Run()
    {
        var log     = new System.Text.StringBuilder("=== AffordanceSystemFixer ===\n\n");
        int disabled = 0;

        var allComponents = new List<MonoBehaviour>();

        // Buscar todos los MonoBehaviours en escena (activos e inactivos)
        foreach (var mb in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                     FindObjectsInactive.Include))
        {
            if (mb == null) continue;
            string typeName = mb.GetType().Name;

            foreach (var hint in AffordanceTypeHints)
            {
                if (typeName.Contains(hint, StringComparison.OrdinalIgnoreCase))
                {
                    allComponents.Add(mb);
                    break;
                }
            }
        }

        log.AppendLine($"Componentes Affordance encontrados: {allComponents.Count}\n");

        if (allComponents.Count == 0)
        {
            log.AppendLine("No se encontraron componentes Affordance en la escena.");
            log.AppendLine("El error puede venir de un prefab instanciado en runtime.");
            log.AppendLine("\nSolución alternativa: usar el botón nuclear de abajo.");
        }

        foreach (var mb in allComponents)
        {
            if (mb == null) continue;

            log.AppendLine($"[FOUND] {mb.GetType().Name}  →  {GetPath(mb.transform)}");

            // Deshabilitar el componente
            if (mb.enabled)
            {
                Undo.RecordObject(mb, "Deshabilitar Affordance");
                mb.enabled = false;
                EditorUtility.SetDirty(mb.gameObject);
                disabled++;
            }
        }

        EditorSceneManager.MarkAllScenesDirty();

        log.AppendLine($"\n{"─",40}");
        log.AppendLine($"Componentes deshabilitados: {disabled}");
        log.AppendLine(disabled > 0
            ? "Guarda con Ctrl+S. El error desaparecerá."
            : "No se deshabilitó nada — prueba la opción nuclear.");

        Debug.Log(log.ToString());
        EditorUtility.DisplayDialog("Affordance Fixer", log.ToString(), "OK");
    }

    [MenuItem("Tools/TITA/Fix/Eliminar TODOS los Affordance de la escena")]
    static void NukeAll()
    {
        if (!EditorUtility.DisplayDialog(
            "Eliminar Affordances",
            "Elimina TODOS los componentes del Affordance System de la escena activa.\n\n" +
            "Los interactables XR siguen funcionando — solo pierden el feedback " +
            "visual de color que da el Affordance System.\n\n" +
            "En TITA ese feedback ya está implementado en DeskComponent " +
            "y GrabbableComponent con MaterialPropertyBlock.\n\n" +
            "¿Continuar?", "Sí, eliminar", "Cancelar"))
            return;

        int count = 0;

        foreach (var mb in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                     FindObjectsInactive.Include))
        {
            if (mb == null) continue;
            string typeName = mb.GetType().Name;

            foreach (var hint in AffordanceTypeHints)
            {
                if (typeName.Contains(hint, StringComparison.OrdinalIgnoreCase))
                {
                    Undo.DestroyObjectImmediate(mb);
                    count++;
                    break;
                }
            }
        }

        EditorSceneManager.MarkAllScenesDirty();
        Debug.Log($"[AffordanceSystemFixer] {count} componentes eliminados.");
        EditorUtility.DisplayDialog("Listo",
            $"{count} componentes Affordance eliminados.\nGuarda con Ctrl+S.", "OK");
    }

    static string GetPath(Transform t) =>
        t.parent == null ? t.name : GetPath(t.parent) + "/" + t.name;
}
#endif
