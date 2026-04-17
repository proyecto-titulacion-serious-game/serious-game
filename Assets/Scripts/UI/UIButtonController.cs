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

    void Update()
    {
        UpdateButtons();
    }

    void UpdateButtons()
    {
        if (instructionSystem == null || gameManager == null) return;

        // Reto 1
        if (fixResistorButton != null)
        {
            bool canFixResistor =
                gameManager.currentLevel == LevelType.OhmLaw &&
                instructionSystem.CanRepairResistor();

            fixResistorButton.interactable = canFixResistor;
        }

        // Reto 2
        if (fixParallelButton != null)
        {
            bool canFixParallel =
                gameManager.currentLevel == LevelType.Parallel &&
                instructionSystem.CanRepairParallel();

            fixParallelButton.interactable = canFixParallel;
        }

        // Botón siguiente solo como apoyo
        if (nextStepButton != null)
        {
            nextStepButton.interactable = false;
        }
    }
}