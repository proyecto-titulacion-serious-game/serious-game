using UnityEngine;

public class InstructionSystem : MonoBehaviour
{
    [Header("Estado")]
    public int currentStep = 0;

    [Header("Referencias")]
    public Multimeter multimeter;
    public GameManager gameManager;

    [Header("Instrucciones")]
    public string[] instructions;

    void Start()
    {
        instructions = new string[]
        {
            "🧠 Paso 1: Haz clic en DOS nodos para medir voltaje.",
            "📟 Paso 2: Observa el valor en el multímetro.",
            "⚡ Paso 3: El valor correcto debe ser cercano a 9V.",
            "🔧 Paso 4: Si no es correcto, reemplaza la resistencia."
        };
    }

    void Update()
    {
        ValidateStep();
    }

    void ValidateStep()
    {
        switch (currentStep)
        {
            // ✅ Paso 1 → seleccionar nodos
            case 0:
                if (multimeter.probeA != null && multimeter.probeB != null)
                {
                    NextStep();
                }
                break;

            // ✅ Paso 2 → ver medición
            case 1:
                if (multimeter.measuredVoltage > 0)
                {
                    NextStep();
                }
                break;

            // ✅ Paso 3 → comparar voltaje
            case 2:
                float v = multimeter.measuredVoltage;

                if (v > 0) // ya midió algo
                {
                    NextStep();
                }
                break;

            // Paso 4 se completa con GameManager
        }
    }

    public string GetCurrentInstruction()
    {
        if (currentStep < instructions.Length)
            return instructions[currentStep];

        return "✔ Procedimiento completado.";
    }

    public void NextStep()
    {
        currentStep++;
        Debug.Log("➡ Avanzando a paso: " + currentStep);
    }

    public void ResetInstructions()
    {
        currentStep = 0;
    }
}