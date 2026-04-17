using UnityEngine;
using UnityEngine.UI;

public class PlayerFeedbackUI : MonoBehaviour
{
    [Header("Referencias")]
    public InstructionSystem instructionSystem;
    public TechnicianActions technicianActions;
    public Multimeter multimeter;
    public GameManager gameManager;

    [Header("UI")]
    public Text feedbackText;

    void Update()
    {
        UpdateFeedback();
    }

    void UpdateFeedback()
    {
        if (feedbackText == null || instructionSystem == null || gameManager == null)
            return;

        switch (gameManager.currentLevel)
        {
            case LevelType.OhmLaw:
                UpdateOhmLawFeedback();
                break;

            case LevelType.Parallel:
                UpdateParallelFeedback();
                break;

            default:
                feedbackText.text = "Nivel en desarrollo.";
                break;
        }
    }

    void UpdateOhmLawFeedback()
    {
        switch (instructionSystem.currentStep)
        {
            case 0:
                feedbackText.text = "Conecta las dos puntas del multimetro a los nodos del circuito.";
                break;

            case 1:
                feedbackText.text = "Observa el valor medido en el multimetro.";
                break;

            case 2:
                feedbackText.text = "Selecciona la resistencia defectuosa.";
                break;

            case 3:
                feedbackText.text = "Ya puedes reemplazar la resistencia.";
                break;

            default:
                if (gameManager.levelCompleted)
                    feedbackText.text = "Reto completado correctamente.";
                else
                    feedbackText.text = "Continua con el procedimiento.";
                break;
        }
    }

    void UpdateParallelFeedback()
    {
        switch (instructionSystem.currentStep)
        {
            case 0:
                feedbackText.text = "Mide el circuito para detectar la rama fallando.";
                break;

            case 1:
                feedbackText.text = "Analiza cual rama o componente presenta la falla.";
                break;

            case 2:
                feedbackText.text = "Ya puedes reparar el circuito paralelo.";
                break;

            default:
                if (gameManager.levelCompleted)
                    feedbackText.text = "Circuito paralelo reparado correctamente.";
                else
                    feedbackText.text = "Continua con el diagnostico.";
                break;
        }
    }
}