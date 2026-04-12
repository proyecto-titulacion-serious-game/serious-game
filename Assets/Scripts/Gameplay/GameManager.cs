using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Referencias")]
    public CircuitManager circuit;
    public Multimeter multimeter;
    public PerformanceTracker performance;

    [Header("Nivel actual")]
    public LevelType currentLevel = LevelType.OhmLaw;

    [Header("Objetivo")]
    public float targetVoltage = 9f;
    public float tolerance = 0.5f;

    public bool levelCompleted = false;

    void Update()
    {
        if (levelCompleted) return;

        switch (currentLevel)
        {
            case LevelType.OhmLaw:
                CheckOhmLaw();
                break;
        }
    }

    void CheckOhmLaw()
    {
        if (multimeter.probeA == null || multimeter.probeB == null)
            return;

        float measured = multimeter.measuredVoltage;

        if (Mathf.Abs(measured - targetVoltage) <= tolerance)
        {
            levelCompleted = true;
            Debug.Log("✅ RETO 1 COMPLETADO");

            if (performance != null)
                Debug.Log(performance.GetEvaluation());
        }
    }
}