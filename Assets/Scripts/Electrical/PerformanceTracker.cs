using UnityEngine;

public class PerformanceTracker : MonoBehaviour
{
    private float startTime;
    private int errors = 0;

    [Header("Configuración")]
    public float excellentTime = 60f;
    public int maxGoodErrors = 3;

    void Start()
    {
        ResetTracker();
    }

    public void ResetTracker()
    {
        startTime = Time.time;
        errors = 0;
    }

    public void AddError(string errorType = "general")
    {
        errors++;
        Debug.Log("Error registrado: " + errorType);
    }

    public float GetTime()
    {
        return Time.time - startTime;
    }

    public int GetErrors()
    {
        return errors;
    }

    public string GetEvaluation()
    {
        float time = GetTime();

        if (errors == 0 && time < excellentTime)
            return "⭐ Excelente desempeño";

        if (errors <= maxGoodErrors)
            return "👍 Buen desempeño";

        return "⚠️ Necesita mejorar";
    }
}