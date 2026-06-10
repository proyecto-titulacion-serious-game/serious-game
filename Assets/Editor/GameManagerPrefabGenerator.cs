using UnityEditor;
using UnityEngine;

/// <summary>
/// Genera el prefab GameManager_System con todos los subsistemas
/// del juego en un solo GameObject, con las referencias internas ya cableadas.
///
/// Menu: Tools → TITA → Generar Prefab GameManager
/// Resultado: Assets/Prefabs/GameManager_System.prefab
///
/// Motor dual de simulacion:
///   Retos 1-3 → CircuitSimulator  (campo gm.circuit,  se resuelve en runtime)
///   Reto  4   → ProtoboardSimulator (campo gm.protoSim, se resuelve en runtime)
///
/// Scripts incluidos y cableado interno:
///   GameManager             ← nucleo del juego
///   PerformanceTracker      ← gm.performance
///   InstructionSystem       ← gm.instructionSystem / instr.gameManager / instr.technicianActions
///   ObjectiveSystem         ← obj.gameManager / obj.performance
///   ComponentDeliverySystem ← delivery.gameManager
///   TechnicianActions       ← techAct.gameManager / techAct.performance / techAct.instructionSystem
///   ConnectionManager       ← connMgr.gameManager (red Fusion Host<->Client)
///
/// REFERENCIAS EXTERNAS — asignar en Inspector tras colocar en escena:
///   GameManager
///     • multimeter         → Multimeter_VR
///     • reto1Zone–reto4Zone → zonas generadas por Vol2RetosBuilder
///     • circuit            → CircuitSimulator del Reto1_Zone (Retos 1-3; auto-detecta al cambiar zona)
///     • protoSim           → ProtoboardSimulator del Reto4 (auto-detecta en runtime si se deja vacio)
///   InstructionSystem
///     • multimeter         → Multimeter_VR
///   TechnicianActions
///     • circuit            → CircuitManager del Reto1_Zone (solo Retos 1-3)
///     • multimeter         → Multimeter_VR
///   ComponentDeliverySystem
///     • puntoDeEntrega     → Bandeja_Recepcion (Explorer)
///     • resistorPrefab / ledPrefab / capacitorPrefab / arduinoPinPrefab
/// </summary>
public static class GameManagerPrefabGenerator
{
    private const string PREFAB_PATH = "Assets/Prefabs/GameManager_System.prefab";

    [MenuItem("Tools/TITA/Generar Prefab GameManager")]
    public static void Generate()
    {
        // ── Confirmar sobreescritura ───────────────────────────────────────
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH) != null)
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "Prefab ya existe",
                $"Ya existe:\n{PREFAB_PATH}\n\n¿Sobreescribir?",
                "Si, sobreescribir", "Cancelar");
            if (!overwrite) return;
        }

        // ── Crear carpeta si no existe ─────────────────────────────────────
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        // ── Raiz ──────────────────────────────────────────────────────────
        var root = new GameObject("GameManager_System");

        // ── Subsistemas ────────────────────────────────────────────────────
        var gm       = root.AddComponent<GameManager>();
        var perf     = root.AddComponent<PerformanceTracker>();
        var instr    = root.AddComponent<InstructionSystem>();
        var obj      = root.AddComponent<ObjectiveSystem>();
        var delivery = root.AddComponent<ComponentDeliverySystem>();
        var techAct  = root.AddComponent<TechnicianActions>();
        var connMgr  = root.AddComponent<ConnectionManager>();

        // ── Cableado interno ──────────────────────────────────────────────

        // GameManager
        gm.performance       = perf;
        gm.instructionSystem = instr;
        // gm.circuit    → CircuitSimulator  de Reto1_Zone (Retos 1-3); se auto-actualiza en ActivateComponentsForLevel
        // gm.protoSim   → ProtoboardSimulator de Reto4;                auto-detectado en runtime si queda vacio
        // gm.multimeter / gm.reto*Zone → asignar en Inspector

        // InstructionSystem
        instr.gameManager       = gm;
        instr.technicianActions = techAct;
        // instr.multimeter → manual

        // ObjectiveSystem
        obj.gameManager = gm;
        obj.performance = perf;

        // ComponentDeliverySystem
        delivery.gameManager = gm;
        // delivery.puntoDeEntrega / *Prefab → manual

        // TechnicianActions (circuit = CircuitManager para Retos 1-3; Reto 4 usa ProtoboardSimulator)
        techAct.gameManager       = gm;
        techAct.performance       = perf;
        techAct.instructionSystem = instr;
        // techAct.circuit   → CircuitManager del Reto1_Zone (solo Retos 1-3)
        // techAct.multimeter → manual

        // ConnectionManager (red Fusion Host<->Client)
        connMgr.gameManager            = gm;
        connMgr.rolAutomatico          = ConnectionManager.AutoConnectRole.Ninguno;
        connMgr.modoOffline            = false;
        connMgr.connectionTimeoutSeconds = 12f;
        // connMgr.playerPrefab / entornoExplorador → asignar en Inspector

        // ── Valores por defecto ───────────────────────────────────────────
        gm.timeLimits          = new float[] { 480f, 600f, 720f, 900f };
        gm.zoneTransitionDelay = 3f;

        perf.excellentTimeLimits = new float[] { 240f, 300f, 360f, 450f };
        perf.maxErrorsForGood    = 3;

        techAct.demoMode            = true;
        techAct.correctResistance   = 100f;
        techAct.normalLedResistance = 50f;

        // ── Guardar prefab ────────────────────────────────────────────────
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PREFAB_PATH, out bool saved);
        Object.DestroyImmediate(root);

        if (saved)
        {
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(prefab);
            EditorUtility.DisplayDialog(
                "GameManager_System creado",
                $"Guardado en:\n{PREFAB_PATH}\n\n" +
                "Scripts incluidos (referencias internas cableadas):\n" +
                "  GameManager\n" +
                "  PerformanceTracker\n" +
                "  InstructionSystem\n" +
                "  ObjectiveSystem\n" +
                "  ComponentDeliverySystem\n" +
                "  TechnicianActions\n\n" +
                "Motor dual de simulacion:\n" +
                "  Retos 1-3 → circuit   (CircuitSimulator,   auto-update al cambiar zona)\n" +
                "  Reto  4   → protoSim  (ProtoboardSimulator, auto-detectado en runtime)\n\n" +
                "ASIGNAR MANUALMENTE en el Inspector:\n" +
                "  GameManager\n" +
                "    multimeter      → Multimeter_VR\n" +
                "    reto1Zone-reto4Zone → zonas generadas\n" +
                "    circuit         → CircuitSimulator de Reto1_Zone\n" +
                "    protoSim        → ProtoboardSimulator (opcional, auto-detecta)\n" +
                "  InstructionSystem\n" +
                "    multimeter      → Multimeter_VR\n" +
                "  TechnicianActions\n" +
                "    circuit         → CircuitManager del Reto1_Zone\n" +
                "    multimeter      → Multimeter_VR\n" +
                "  ConnectionManager\n" +
                "    playerPrefab    → NetworkObject del avatar de red\n" +
                "    entornoExplorador → GO raiz del entorno VR\n" +
                "    rolAutomatico   → Explorador / Tecnico / Ninguno\n" +
                "  ComponentDeliverySystem\n" +
                "    puntoDeEntrega  → Bandeja_Recepcion (Explorer)\n" +
                "    resistorPrefab / ledPrefab / capacitorPrefab / arduinoPinPrefab",
                "OK");
            Debug.Log($"[GameManagerPrefabGenerator] Prefab guardado en {PREFAB_PATH}");
        }
        else
        {
            EditorUtility.DisplayDialog("Error",
                "No se pudo guardar el prefab.\n" +
                "Verifica que exista la carpeta Assets/Prefabs/.", "OK");
        }
    }
}
