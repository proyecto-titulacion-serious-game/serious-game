using UnityEngine;

public class PerformanceTracker : MonoBehaviour
{
    public float startTime;
    public int errors = 0;

    void Start()
    {
        startTime = Time.time;
    }

    public void AddError()
    {
        errors++;
    }

    public float GetTime()
    {
        return Time.time - startTime;
    }

    public string GetEvaluation()
    {
        float time = GetTime();

        if (errors == 0 && time < 60)
            return "⭐ Excelente desempeño";

        if (errors < 3)
            return "👍 Buen desempeño";

        return "⚠️ Necesita mejorar";
    }
}