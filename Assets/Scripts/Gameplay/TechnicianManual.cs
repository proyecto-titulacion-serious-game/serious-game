using UnityEngine;

public class TechnicianManual : MonoBehaviour
{
    public GameManager gameManager;

    public string GetManualText()
    {
        if (gameManager == null)
            return "Manual no disponible.";

        switch (gameManager.currentLevel)
        {
            case LevelType.OhmLaw:
                return
                    "MANUAL TECNICO - RETO 1\n\n" +
                    "Concepto: Ley de Ohm\n" +
                    "Formula: V = I x R\n\n" +
                    "Objetivo:\n" +
                    "- Verificar el voltaje del circuito.\n" +
                    "- Detectar si la resistencia es incorrecta.\n" +
                    "- Reemplazarla por 100 ohmios.\n\n" +
                    "Indicacion:\n" +
                    "Si el LED entra en sobrecarga, la resistencia probablemente es demasiado baja.";

            case LevelType.Parallel:
                return
                    "MANUAL TECNICO - RETO 2\n\n" +
                    "Concepto: Circuito en paralelo\n\n" +
                    "Objetivo:\n" +
                    "- Identificar una rama fallando.\n" +
                    "- Diagnosticar el componente alterado.\n" +
                    "- Aplicar la reparacion.\n\n" +
                    "Indicacion:\n" +
                    "Si una rama no recibe suficiente energia, uno de los componentes puede estar simulando circuito abierto.";

            case LevelType.Mixed:
                return
                    "MANUAL TECNICO - RETO 3\n\n" +
                    "Concepto: Circuito mixto y polaridad.\n\n" +
                    "Objetivo:\n" +
                    "- Detectar polaridad incorrecta.\n" +
                    "- Verificar valores de resistencias.\n" +
                    "- Corregir los componentes defectuosos.";

            case LevelType.Arduino:
                return
                    "MANUAL TECNICO - RETO 4\n\n" +
                    "Concepto: Sensor-actuador con Arduino.\n\n" +
                    "Objetivo:\n" +
                    "- Revisar conexiones en protoboard.\n" +
                    "- Verificar pinout.\n" +
                    "- Corregir el cableado del sensor y actuador.";

            default:
                return "Nivel en desarrollo.";
        }
    }
}