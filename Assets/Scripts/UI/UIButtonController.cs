using UnityEngine;
using UnityEngine.UI;

public class UIButtonController : MonoBehaviour
{
    [Header("Referencias")]
    public InstructionSystem instructionSystem;
    public GameManager gameManager;
    public TechnicianActions technicianActions;

    [Header("Botones")]
    public Button fixResistorButton;
    public Button fixParallelButton;
    public Button nextStepButton;

    [Header("Textos opcionales")]
    public Text fixResistorLabel;
    public Text fixParallelLabel;
    public Text nextStepLabel;

    void Update()
    {
        UpdateButtons();
    }

    void UpdateButtons()
    {
        if (instructionSystem == null || gameManager == null)
            return;

        UpdateFixResistorButton();
        UpdateFixParallelButton();
        UpdateNextStepButton();
    }

    void UpdateFixResistorButton()
    {
        if (fixResistorButton == null) return;

        bool enabled =
            gameManager.currentLevel == LevelType.OhmLaw &&
            instructionSystem.CanRepairResistor();

        fixResistorButton.interactable = enabled;

        if (fixResistorLabel != null)
        {
            if (enabled)
                fixResistorLabel.text = "Reemplazar Resistencia";
            else
                fixResistorLabel.text = "Mide y selecciona la resistencia";
        }
    }

    void UpdateFixParallelButton()
    {
        if (fixParallelButton == null) return;

        bool enabled =
            gameManager.currentLevel == LevelType.Parallel &&
            instructionSystem.CanRepairParallel();

        fixParallelButton.interactable = enabled;

        if (fixParallelLabel != null)
        {
            if (enabled)
                fixParallelLabel.text = "Reparar Circuito Paralelo";
            else
                fixParallelLabel.text = "Primero mide el circuito";
        }
    }

    void UpdateNextStepButton()
    {
        if (nextStepButton == null) return;

        // El paso se valida automáticamente, así que el botón queda desactivado
        nextStepButton.interactable = false;

        if (nextStepLabel != null)
        {
            nextStepLabel.text = "Paso automático";
        }
    }
}