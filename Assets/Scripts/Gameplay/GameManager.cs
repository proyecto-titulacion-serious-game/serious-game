using UnityEngine;

public class GameManager : MonoBehaviour
{
    public CircuitManager circuit;
    public PerformanceTracker performance;

    public int currentLevel = 1;

    [Header("Objetivos")]
    public float targetCurrent = 0.05f;
    public float tolerance = 0.01f;

    [Header("Estado")]
    public bool levelCompleted = false;

    void Start()
    {
        SetupLevel();
    }

    void Update()
    {
        CheckWinCondition();
    }

    void SetupLevel()
    {
        levelCompleted = false;

        switch (currentLevel)
        {
            case 1:
                Debug.Log("Reto 1: Ley de Ohm");

                targetCurrent = 0.05f;
                break;

            case 2:
                Debug.Log("Reto 2: Ajuste de resistencia");

                targetCurrent = 0.02f;
                break;
        }
    }

    void CheckWinCondition()
    {
        if (levelCompleted) return;

        float current = circuit.totalCurrent;

        if (Mathf.Abs(current - targetCurrent) <= tolerance)
        {
            levelCompleted = true;

            Debug.Log("✅ Nivel completado");

            if (performance != null)
            {
                Debug.Log(performance.GetEvaluation());
            }

            Invoke("NextLevel", 3f);
        }
    }

    void NextLevel()
    {
        currentLevel++;
        SetupLevel();
    }
}