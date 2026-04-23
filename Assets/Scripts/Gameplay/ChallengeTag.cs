using UnityEngine;

/// <summary>
/// Etiqueta que indica a qué reto(s) pertenece un componente eléctrico.
/// Agregar este script a cada GameObject de componente en el circuito maestro.
///
/// SETUP EN UNITY:
///   1. Selecciona el GameObject del componente (ej: Resistor_Reto1)
///   2. Add Component → ChallengeTag
///   3. En el Inspector, marca los checkboxes de los retos en los que participa
///
/// Si un componente se usa en más de un reto (ej: la fuente de voltaje),
/// marca varios checkboxes.
/// Si no tiene ChallengeTag, se considera COMPARTIDO (siempre activo).
/// </summary>
public class ChallengeTag : MonoBehaviour
{
    [Header("¿En qué retos participa este componente?")]
    [Tooltip("Marcar los retos donde este componente debe estar activo.")]
    public bool reto1_OhmLaw   = false;
    public bool reto2_Parallel = false;
    public bool reto3_Mixed    = false;
    public bool reto4_Arduino  = false;

    /// <summary>
    /// Devuelve true si este componente debe estar activo en el nivel dado.
    /// </summary>
    public bool BelongsToLevel(LevelType level)
    {
        return level switch
        {
            LevelType.OhmLaw   => reto1_OhmLaw,
            LevelType.Parallel => reto2_Parallel,
            LevelType.Mixed    => reto3_Mixed,
            LevelType.Arduino  => reto4_Arduino,
            _ => false
        };
    }

    /// <summary>
    /// True si no tiene ningún reto marcado (componente compartido/global).
    /// </summary>
    public bool IsShared => !reto1_OhmLaw && !reto2_Parallel && !reto3_Mixed && !reto4_Arduino;
}
