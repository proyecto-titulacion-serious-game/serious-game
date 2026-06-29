using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Arranca el PANEL DOCENTE (SessionDataExporter + DashboardServer) automáticamente al entrar a Play,
/// SOLO en el Técnico (PC host). Así el dashboard de métricas funciona sin tener que añadir los
/// componentes a la escena a mano (no estaban puestos en ninguna escena → el panel nunca corría).
///
/// El pipeline de datos (GameManager → PerformanceTracker → ObjectiveSystem) ya vive en la escena del
/// Técnico, así que en cuanto esto arranca el servidor, el panel ya muestra tiempos/errores/resultados.
///
/// ▶ DÓNDE VER LAS MÉTRICAS: al dar Play, la consola imprime
///       [DashboardServer] Panel docente en: http://localhost:8080/
///   Abre esa URL en cualquier navegador (Chrome/Edge). Botones "Exportar a CSV" para Looker/Sheets.
/// </summary>
public static class DashboardBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        // El Explorador corre en la Quest (Android) → ahí NO se levanta el panel.
        if (Application.platform == RuntimePlatform.Android) return;

        // Solo en el rol Técnico: su escena se llama "Tecnico". (Evita levantarlo en el Explorador PCVR.)
        bool esTecnico = false;
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).name.Contains("Tecnico")) { esTecnico = true; break; }
        if (!esTecnico) return;

        // Evitar duplicados si ya estuviera puesto en la escena.
        if (Object.FindAnyObjectByType<DashboardServer>(FindObjectsInactive.Include) != null) return;

        var go = new GameObject("TITA_Dashboard");
        Object.DontDestroyOnLoad(go);

        var exporter = go.AddComponent<SessionDataExporter>();
        var server   = go.AddComponent<DashboardServer>();
        server.dataExporter  = exporter;
        server.localhostOnly = true;   // solo este PC (sin permisos extra). Cambiar a false para toda la LAN.

        Debug.Log("[DashboardBootstrap] Panel docente iniciado. Mira la URL que imprime [DashboardServer] " +
                  "abajo y ábrela en el navegador.");
    }
}
