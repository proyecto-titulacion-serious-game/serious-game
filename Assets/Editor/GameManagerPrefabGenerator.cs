using UnityEditor;
using UnityEngine;

/// <summary>
/// Genera el prefab GameManager_System con todos los subsistemas
/// del juego en un solo GameObject, con las referencias internas
/// ya cableadas.
///
/// Menú: Tools → TITA → Generar Prefab GameManager
/// Resultado: Assets/Prefabs/GameManager_System.prefab
///
/// Scripts incluidos y su cableado interno:
///   GameManager          ← núcleo del juego
///   PerformanceTracker   ← gm.performance
///   InstructionSystem    ← gm.instructionSystem / instr.gameManager / instr.technicianActions
///   ObjectiveSystem      ← obj.gameManager / obj.performance
///   ComponentDeliverySystem ← delivery.gameManager
///   TechnicianActions    ← techAct.gameManager / techAct.performance / techAct.instructionSystem
///
/// REFERENCIAS EXTERNAS — asignar en Inspector tras colocar en escena:
///   GameManager          → multimeter, reto1Zone–reto4Zone
///   InstructionSystem    → multimeter
///   TechnicianActions    → circuit, multimeter
///   ComponentDeliverySystem → puntoDeEntrega, resistorPrefab, ledPrefab,
///                             capacitorPrefab, arduinoPinPrefab
///
/// NOTA: gameManager.circuit y technicianActions.circuit se resuelven
///   en runtime: ActivateComponentsForLevel() actualiza gameManager.circuit
///   automáticamente al cambiar de zona. Asigna también technicianActions.circuit
///   a la zona activa inicial (Reto1_Zone → su CircuitManager).
/// </summary>
public static class GameManagerPrefabGenerator
{
    private const string PREFAB_PATH = "Assets/Prefabs/GameManager_System.prefab";

    [MenuItem("Tools/TITA/Generar Prefab GameManager")]
    public static void Generate()
    {
        // ── Confirmar sobreescritura ────────────────────────────────────────
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH) != null)
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "Prefab ya existe",
                $"Ya existe:\n{PREFAB_PATH}\n\n¿Sobreescribir?",
                "Sí, sobreescribir", "Cancelar");
            if (!overwrite) return;
        }

        // ── Raíz ────────────────────────────────────────────────────────────
        var root = new GameObject("GameManager_System");

        // ── Añadir subsistemas ───────────────────────────────────────────────
        var gm       = root.AddComponent<GameManager>();
        var perf     = root.AddComponent<PerformanceTracker>();
        var instr    = root.AddComponent<InstructionSystem>();
        var obj      = root.AddComponent<ObjectiveSystem>();
        var delivery = root.AddComponent<ComponentDeliverySystem>();
        var techAct  = root.AddComponent<TechnicianActions>();

        // ── Cableado interno ─────────────────────────────────────────────────

        // GameManager → subsistemas del mismo GO
        gm.performance       = perf;
        gm.instructionSystem = instr;
        // gm.circuit, gm.multimeter, gm.reto*Zone → manual (dependen de la escena)

        // InstructionSystem → mismos GO
        instr.gameManager        = gm;
        instr.technicianActions  = techAct;
        // instr.multimeter → manual

        // ObjectiveSystem → mismos GO
        obj.gameManager  = gm;
        obj.performance  = perf;

        // ComponentDeliverySystem → mismo GO
        delivery.gameManager = gm;
        // delivery.puntoDeEntrega → manual (Transform de la bandeja del Explorador)
        // delivery.resistorPrefab / ledPrefab / capacitorPrefab / arduinoPinPrefab → manual

        // TechnicianActions → mismos GO
        techAct.gameManager       = gm;
        techAct.performance       = perf;
        techAct.instructionSystem = instr;
        // techAct.circuit   → manual (mismo CircuitManager que GameManager usa)
        // techAct.multimeter → manual

        // ── Valores por defecto ──────────────────────────────────────────────
        gm.timeLimits           = new float[] { 480f, 600f, 720f, 900f };
        gm.zoneTransitionDelay  = 3f;

        perf.excellentTimeLimits = new float[] { 240f, 300f, 360f, 450f };
        perf.maxErrorsForGood    = 3;

        techAct.demoMode              = true;   // permite reparar sin completar pasos
        techAct.correctResistance     = 100f;   // Reto 1
        techAct.normalLedResistance   = 50f;    // Reto 2

        // ── Guardar prefab ───────────────────────────────────────────────────
        bool saved;
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH, out saved);
        Object.DestroyImmediate(root);

        if (saved)
        {
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(prefab);
            EditorUtility.DisplayDialog(
                "GameManager_System creado",
                $"Guardado en:\n{PREFAB_PATH}\n\n" +
                "Scripts incluidos (referencias internas cableadas):\n" +
                "  ✓ GameManager\n" +
                "  ✓ PerformanceTracker\n" +
                "  ✓ InstructionSystem\n" +
                "  ✓ ObjectiveSystem\n" +
                "  ✓ ComponentDeliverySystem\n" +
                "  ✓ TechnicianActions\n\n" +
                "ASIGNAR MANUALMENTE en el Inspector:\n" +
                "  GameManager\n" +
                "    • multimeter      → Multimeter_VR\n" +
                "    • reto1Zone–reto4Zone → zonas generadas\n" +
                "  InstructionSystem\n" +
                "    • multimeter      → Multimeter_VR\n" +
                "  TechnicianActions\n" +
                "    • circuit         → CircuitManager del Reto1_Zone\n" +
                "    • multimeter      → Multimeter_VR\n" +
                "  ComponentDeliverySystem\n" +
                "    • puntoDeEntrega  → Bandeja_Recepcion (Explorer)\n" +
                "    • resistorPrefab / ledPrefab / capacitorPrefab / arduinoPinPrefab",
                "OK");
            Debug.Log($"[GameManagerPrefabGenerator] Prefab guardado en {PREFAB_PATH}");
        }
        else
        {
            EditorUtility.DisplayDialog("Error",
                "No se pudo guardar el prefab.\n" +
                "Verifica que exista la carpeta Assets/Prefabs/.",
                "OK");
        }
    }
}
