using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controlador de botones del panel del Técnico.
/// Actualiza la interactividad de cada botón según el estado del juego
/// y conecta los eventos de clic con TechnicianActions.
/// </summary>
/// <remarks>
/// Agregar este script al Panel_Botones del TechnicianCanvas.
/// Los botones se activan/desactivan automáticamente según el paso actual
/// del InstructionSystem — no es necesario gestionarlos manualmente.
/// </remarks>
public class UIButtonController : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Referencias
    // ─────────────────────────────────────────────

    /// <summary>Sistema de pasos — determina cuándo se habilita cada botón.</summary>
    [Header("Referencias")]
    public InstructionSystem instructionSystem;

    /// <summary>Gestor del juego — provee el nivel activo.</summary>
    public GameManager gameManager;

    /// <summary>Acciones del Técnico — ejecuta las reparaciones.</summary>
    public TechnicianActions technicianActions;

    // ─────────────────────────────────────────────
    //  Botones
    // ─────────────────────────────────────────────

    /// <summary>
    /// Botón "ENVIAR RESISTENCIA" — Reto 1.
    /// Se habilita cuando el Técnico completó los pasos de medición y selección.
    /// </summary>
    [Header("Botones")]
    public Button fixResistorButton;

    /// <summary>
    /// Botón "REPARAR PARALELO" — Reto 2.
    /// Se habilita cuando el Técnico identificó la rama rota.
    /// </summary>
    public Button fixParallelButton;

    /// <summary>
    /// Botón de paso siguiente (desactivado — avance automático).
    /// Incluido para compatibilidad futura.
    /// </summary>
    public Button nextStepButton;

    // ─────────────────────────────────────────────
    //  Labels opcionales (TMP en vez de Text)
    // ─────────────────────────────────────────────

    /// <summary>Label del botón de resistencia — cambia texto según el estado.</summary>
    [Header("Labels opcionales")]
    public TMP_Text fixResistorLabel;

    /// <summary>Label del botón de paralelo — cambia texto según el estado.</summary>
    public TMP_Text fixParallelLabel;

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────

    /// <summary>Actualiza el estado de los botones cada frame.</summary>
    private void Update()
    {
        if (instructionSystem == null || gameManager == null) return;

        UpdateFixResistorButton();
        UpdateFixParallelButton();
        UpdateNextStepButton();
    }

    // ─────────────────────────────────────────────
    //  Actualización de botones
    // ─────────────────────────────────────────────

    /// <summary>
    /// Activa el botón de resistencia solo en Reto 1 cuando el Técnico
    /// completó los pasos de medición y selección de componente.
    /// </summary>
    private void UpdateFixResistorButton()
    {
        if (fixResistorButton == null) return;

        bool habilitado = gameManager.currentLevel == LevelType.OhmLaw
                       && instructionSystem.CanRepairResistor();

        fixResistorButton.interactable = habilitado;

        if (fixResistorLabel != null)
            fixResistorLabel.text = habilitado
                ? "ENVIAR COMPONENTE"
                : "Mide y selecciona primero";
    }

    /// <summary>
    /// Activa el botón de paralelo solo en Reto 2 cuando el Técnico
    /// completó las mediciones de la rama rota.
    /// </summary>
    private void UpdateFixParallelButton()
    {
        if (fixParallelButton == null) return;

        bool habilitado = gameManager.currentLevel == LevelType.Parallel
                       && instructionSystem.CanRepairParallel();

        fixParallelButton.interactable = habilitado;

        if (fixParallelLabel != null)
            fixParallelLabel.text = habilitado
                ? "REPARAR PARALELO"
                : "Mide el circuito primero";
    }

    /// <summary>
    /// El paso siguiente es automático — este botón siempre está desactivado.
    /// El InstructionSystem avanza solo cuando se cumplen las condiciones.
    /// </summary>
    private void UpdateNextStepButton()
    {
        if (nextStepButton == null) return;
        nextStepButton.interactable = false;
    }

    // ─────────────────────────────────────────────
    //  NUEVA FUNCIÓN DE LIMPIEZA AUTOMÁTICA
    // ─────────────────────────────────────────────

    /// <summary>
    /// Busca el componente eléctrico que está clonado en la mesa de trabajo
    /// del Técnico y lo destruye para evitar acumulaciones tras el envío.
    /// </summary>
    public void LimpiarMesaDelTecnico()
    {
        // Buscamos dinámicamente cualquier script del componente eléctrico que esté vivo en la escena
        ElectricalComponent componenteEnMesa = FindAnyObjectByType<ElectricalComponent>();

        if (componenteEnMesa != null)
        {
            Debug.Log($"[Mesa Técnico] Destruyendo clon enviado: {componenteEnMesa.gameObject.name}");
            Destroy(componenteEnMesa.gameObject);
        }
    }
}