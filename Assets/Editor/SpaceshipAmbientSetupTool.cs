using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Configura SpaceshipAmbientSystem en la escena activa.
///
/// Menu: Tools → TITA → Setup Ambiente Nave Espacial
///
/// Qué hace:
///   1. Crea o localiza SpaceshipAmbientSystem en la escena
///   2. Auto-detecta GameManager y sus 4 zonas de reto
///   3. Auto-detecta CircuitManager por zona
///   4. Auto-detecta luces (Light) dentro de cada zona GO
///   5. Busca ParticleSystems bajo cada zona y los asigna como atmosphereParticles
///   6. Busca Volumes URP por zona para post-processing
/// </summary>
public static class SpaceshipAmbientSetupTool
{
    [MenuItem("Tools/TITA/Setup Ambiente Nave Espacial")]
    static void Run()
    {
        var log     = new StringBuilder();
        bool changed = false;

        // ── 1. GameManager ───────────────────────────────────────────────────
        var gm = Object.FindAnyObjectByType<GameManager>(FindObjectsInactive.Include);
        if (gm == null)
        {
            EditorUtility.DisplayDialog("Setup Ambiente Nave",
                "No se encontró GameManager en la escena.", "OK");
            return;
        }
        log.AppendLine($"[GameManager: '{gm.name}']");

        // ── 2. SpaceshipAmbientSystem ────────────────────────────────────────
        var ambient = Object.FindAnyObjectByType<SpaceshipAmbientSystem>(FindObjectsInactive.Include);
        if (ambient == null)
        {
            var go = new GameObject("SpaceshipAmbientSystem");
            Undo.RegisterCreatedObjectUndo(go, "Setup Ambiente Nave");
            ambient = Undo.AddComponent<SpaceshipAmbientSystem>(go);
            log.AppendLine("  ✓ SpaceshipAmbientSystem creado");
            changed = true;
        }
        else log.AppendLine($"  — SpaceshipAmbientSystem: '{ambient.name}'");

        Undo.RecordObject(ambient, "Setup Ambiente Nave");

        // ── 3. Asignar GameManager ───────────────────────────────────────────
        if (ambient.gameManager == null)
        {
            ambient.gameManager = gm;
            log.AppendLine("  ✓ GameManager asignado");
            changed = true;
        }

        // ── 4. Configurar zonas ──────────────────────────────────────────────
        GameObject[] zoneGOs =
        {
            gm.reto1Zone, gm.reto2Zone, gm.reto3Zone, gm.reto4Zone
        };
        LevelType[] levels =
        {
            LevelType.OhmLaw, LevelType.Parallel, LevelType.Mixed, LevelType.Arduino
        };

        // Asegurar que el array tenga 4 elementos
        if (ambient.zones == null || ambient.zones.Length != 4)
        {
            ambient.zones = new SpaceshipAmbientSystem.ZoneAmbientConfig[4];
            for (int i = 0; i < 4; i++)
                ambient.zones[i] = new SpaceshipAmbientSystem.ZoneAmbientConfig();
            changed = true;
        }

        for (int i = 0; i < 4; i++)
        {
            var z   = ambient.zones[i];
            var zGO = zoneGOs[i];
            z.reto  = levels[i];

            if (zGO == null)
            {
                log.AppendLine($"  ⚠ Zona {i + 1} ({levels[i]}): reto{i+1}Zone no asignado en GameManager.");
                continue;
            }

            log.AppendLine($"  [Zona {i + 1} — {levels[i]}: '{zGO.name}']");

            // CircuitManager
            if (z.circuitManager == null)
            {
                z.circuitManager = zGO.GetComponentInChildren<CircuitManager>(true);
                if (z.circuitManager != null)
                { log.AppendLine($"    ✓ CircuitManager: '{z.circuitManager.name}'"); changed = true; }
                else
                    log.AppendLine($"    ⚠ No se encontró CircuitManager en '{zGO.name}'.");
            }
            else log.AppendLine($"    — CircuitManager: '{z.circuitManager.name}'");

            // Luces
            var lights = zGO.GetComponentsInChildren<Light>(true);
            if (lights.Length > 0 && (z.zoneLights == null || z.zoneLights.Length == 0))
            {
                z.zoneLights = lights;
                log.AppendLine($"    ✓ {lights.Length} luz(es) asignadas");
                changed = true;
            }
            else if (lights.Length == 0)
                log.AppendLine($"    ⚠ No hay Light components en '{zGO.name}' — agrégalos manualmente.");
            else
                log.AppendLine($"    — Luces ya configuradas ({z.zoneLights.Length})");

            // Partículas de atmósfera
            var particles = zGO.GetComponentsInChildren<ParticleSystem>(true);
            if (particles.Length > 0 && (z.atmosphereParticles == null || z.atmosphereParticles.Length == 0))
            {
                z.atmosphereParticles = particles;
                log.AppendLine($"    ✓ {particles.Length} ParticleSystem(s) asignados como atmósfera");
                changed = true;
            }
            else if (particles.Length > 0)
                log.AppendLine($"    — Partículas ya configuradas ({z.atmosphereParticles.Length})");

            // Volume URP (post-processing)
            var volume = zGO.GetComponentInChildren<Volume>(true);
            if (volume != null && z.postProcessVolume == null)
            {
                z.postProcessVolume = volume;
                log.AppendLine($"    ✓ Volume URP asignado: '{volume.name}'");
                changed = true;
            }
            else if (volume == null)
                log.AppendLine($"    — Sin Volume URP en la zona (opcional).");
        }

        // ── 5. Finalizar ─────────────────────────────────────────────────────
        if (changed)
        {
            EditorUtility.SetDirty(ambient);
            EditorSceneManager.MarkSceneDirty(ambient.gameObject.scene);
        }

        log.AppendLine();
        log.AppendLine(changed ? "✓ Setup completado." : "— Sin cambios.");
        log.AppendLine();
        log.AppendLine("PRÓXIMOS PASOS:");
        log.AppendLine("• Agrega Light components dentro de cada zona GO para feedback visual.");
        log.AppendLine("• Agrega ParticleSystems de vapor/chispas para atmósfera de falla.");
        log.AppendLine("• (Opcional) Agrega URP Volume con Vignette/Color Grading por zona.");
        log.AppendLine("• Ajusta colores y velocidad de parpadeo en SpaceshipAmbientSystem > zones[].");

        EditorUtility.DisplayDialog("Setup Ambiente Nave", log.ToString(), "OK");
    }
}
