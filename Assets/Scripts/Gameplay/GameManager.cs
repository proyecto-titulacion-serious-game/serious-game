using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Referencias")]
    public CircuitManager circuit;
    public Multimeter multimeter;
    public PerformanceTracker performance;
    public InstructionSystem instructionSystem;

    [Header("Nivel actual")]
    public LevelType currentLevel = LevelType.OhmLaw;

    [Header("Objetivo")]
    public float targetVoltage = 9f;
    public float tolerance = 0.5f;

    [Header("Estado")]
    public bool levelCompleted = false;

    [Header("Progresión")]
    public LevelType[] levels;
    private int currentLevelIndex = 0;

    [Header("Seguimiento del jugador")]
    public bool repairPerformed = false;

    void Start()
    {
        levels = new LevelType[]
        {
            LevelType.OhmLaw,
            LevelType.Parallel,
            LevelType.Mixed,
            LevelType.Arduino
        };

        LoadLevel(currentLevelIndex);
    }

    void Update()
    {
        if (levelCompleted) return;

        switch (currentLevel)
        {
            case LevelType.OhmLaw:
                CheckOhmLaw();
                break;

            case LevelType.Parallel:
                CheckParallel();
                break;

            case LevelType.Mixed:
                // Futuro
                break;

            case LevelType.Arduino:
                // Futuro
                break;
        }
    }

    public bool HasPerformedRepair()
    {
        return repairPerformed;
    }

    public void RegisterRepairAction()
    {
        repairPerformed = true;
        Debug.Log("🛠 Reparación registrada");
    }

    void LoadLevel(int index)
    {
        currentLevel = levels[index];
        levelCompleted = false;
        repairPerformed = false;

        if (performance != null)
            performance.ResetTracker();

        if (multimeter != null)
            multimeter.ResetProbes();

        if (instructionSystem != null)
        {
            instructionSystem.ResetInstructions();
            instructionSystem.BuildInstructions();
        }

        Debug.Log("🎮 Cargando nivel: " + currentLevel);
        SetupLevel();
    }

    void SetupLevel()
    {
        switch (currentLevel)
        {
            case LevelType.OhmLaw:
                SetupOhmLaw();
                break;

            case LevelType.Parallel:
                SetupParallel();
                break;

            case LevelType.Mixed:
                Debug.Log("⚠ Nivel 3 aún no implementado");
                break;

            case LevelType.Arduino:
                Debug.Log("⚠ Nivel 4 aún no implementado");
                break;
        }
    }

    // -------------------------
    // RETO 1
    // -------------------------
    void SetupOhmLaw()
    {
        foreach (var comp in circuit.components)
        {
            if (comp is Resistor r)
            {
                r.resistance = 10f; // falla inicial
                Debug.Log("⚠ Reto 1: resistencia incorrecta aplicada");
            }

            if (comp is LED led)
            {
                led.resistance = 50f; // normal
            }
        }

        targetVoltage = 9f;
        tolerance = 0.5f;
    }

    void CheckOhmLaw()
    {
        if (!repairPerformed) return;
        if (multimeter == null) return;
        if (multimeter.probeA == null || multimeter.probeB == null) return;

        float measured = multimeter.measuredVoltage;

        if (Mathf.Abs(measured - targetVoltage) <= tolerance)
        {
            levelCompleted = true;
            Debug.Log("✅ RETO 1 COMPLETADO");

            if (performance != null)
                Debug.Log(performance.GetEvaluation());

            Invoke(nameof(NextLevel), 2f);
        }
    }

    // -------------------------
    // RETO 2
    // -------------------------
    void SetupParallel()
    {
        foreach (var comp in circuit.components)
        {
            if (comp is Resistor r)
            {
                r.resistance = 100f; // restaurar valor normal
            }

            if (comp is LED led)
            {
                led.resistance = 9999f; // simula rama abierta / fallo
                Debug.Log("⚠ Reto 2: rama en paralelo fallando");
            }
        }

        targetVoltage = 9f;
        tolerance = 0.5f;
    }

    void CheckParallel()
    {
        if (!repairPerformed) return;

        bool allWorking = true;

        foreach (var comp in circuit.components)
        {
            if (comp is LED led)
            {
                if (!led.isOn)
                {
                    allWorking = false;
                }
            }
        }

        if (allWorking)
        {
            levelCompleted = true;
            Debug.Log("✅ RETO 2 COMPLETADO");

            if (performance != null)
                Debug.Log(performance.GetEvaluation());

            Invoke(nameof(NextLevel), 2f);
        }
    }

    void NextLevel()
    {
        currentLevelIndex++;

        if (currentLevelIndex >= levels.Length)
        {
            Debug.Log("🏆 JUEGO COMPLETADO");
            return;
        }

        LoadLevel(currentLevelIndex);
    }

    public bool IsVoltageCorrect()
    {
        if (multimeter == null) return false;
        return Mathf.Abs(multimeter.measuredVoltage - targetVoltage) <= tolerance;
    }
}