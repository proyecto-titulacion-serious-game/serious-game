using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Verificación HEADLESS del fix de registro duplicado en <see cref="PerformanceTracker"/>.
///
/// Antes: GameManager.OnLevelCompleted podía dispararse dos veces por la misma compleción
/// (p.ej. con NoonA aditiva en el Host) y el tracker agregaba un registro por cada disparo,
/// duplicando cada reto en los resultados. Ahora el registro es idempotente por carga de nivel.
///
/// El test invoca por reflexión los handlers privados (HandleLevelLoaded / HandleLevelCompleted)
/// simulando: doble disparo en un mismo nivel → 1 registro; y un replay (nueva carga) → +1.
///
/// Ejecutar:
///   Editor:     Tools → TITA → Reto 4 → Test anti-duplicado (headless)
///   Batch mode: Unity.exe -batchmode -quit -projectPath . -executeMethod Reto4TrackerDedupTest.Run -logFile -
/// </summary>
public static class Reto4TrackerDedupTest
{
    [MenuItem("Tools/TITA/Reto 4/Test anti-duplicado (headless)")]
    public static void Run()
    {
        int fails = 0;
        Debug.Log("===== RETO — TEST ANTI-DUPLICADO DE REGISTROS =====");

        var go = new GameObject("TrackerTest"); go.SetActive(false);
        var tracker = go.AddComponent<PerformanceTracker>();

        var hLoaded    = Method("HandleLevelLoaded");
        var hCompleted = Method("HandleLevelCompleted");
        if (hLoaded == null || hCompleted == null)
        {
            Debug.LogError("FALLO: no se encontraron los handlers privados por reflexión.");
            Object.DestroyImmediate(go);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            return;
        }

        // ── [1] Doble disparo en el MISMO nivel → debe quedar 1 registro ──
        hLoaded.Invoke(tracker, new object[] { LevelType.OhmLaw });
        hCompleted.Invoke(tracker, new object[] { LevelType.OhmLaw, true });
        hCompleted.Invoke(tracker, new object[] { LevelType.OhmLaw, true }); // disparo duplicado
        int c1 = tracker.GetAllRecords().Count;
        Debug.Log($"[1] Reto 1 con doble OnLevelCompleted → registros = {c1} (esperado 1)");
        if (c1 != 1) { fails++; Debug.LogError("FALLO: el disparo duplicado no se ignoró."); }

        // ── [2] Nuevo nivel (replay/avance) → debe sumar 1 más ──
        hLoaded.Invoke(tracker, new object[] { LevelType.Parallel });
        hCompleted.Invoke(tracker, new object[] { LevelType.Parallel, true });
        hCompleted.Invoke(tracker, new object[] { LevelType.Parallel, true }); // duplicado otra vez
        int c2 = tracker.GetAllRecords().Count;
        Debug.Log($"[2] + Reto 2 con doble disparo → registros = {c2} (esperado 2)");
        if (c2 != 2) { fails++; Debug.LogError("FALLO: el segundo nivel no se registró correctamente."); }

        // ── [3] Replay del MISMO nivel (recarga) → cuenta como registro nuevo ──
        hLoaded.Invoke(tracker, new object[] { LevelType.Parallel }); // recarga del Reto 2
        hCompleted.Invoke(tracker, new object[] { LevelType.Parallel, true });
        int c3 = tracker.GetAllRecords().Count;
        Debug.Log($"[3] Replay del Reto 2 → registros = {c3} (esperado 3: un replay legítimo SÍ registra)");
        if (c3 != 3) { fails++; Debug.LogError("FALLO: un replay legítimo debería añadir un registro."); }

        Debug.Log(fails == 0
            ? "===== RESULTADO: ✓ OK — sin duplicados; un registro por carga de nivel ====="
            : $"===== RESULTADO: ✗ {fails} FALLO(S) =====");

        Object.DestroyImmediate(go);
        if (Application.isBatchMode) EditorApplication.Exit(fails == 0 ? 0 : 1);
    }

    static MethodInfo Method(string name) =>
        typeof(PerformanceTracker).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
}
