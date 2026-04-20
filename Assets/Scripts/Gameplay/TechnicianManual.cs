using UnityEngine;

/// <summary>
/// Contiene el manual técnico completo para cada reto.
/// El Técnico lo consulta en su panel para diagnosticar y guiar al Explorador.
/// Incluye: concepto, fórmula, objetivo, tabla de valores y pistas.
/// </summary>
public class TechnicianManual : MonoBehaviour
{
    public GameManager gameManager;

    public ManualData GetManualData(LevelType level)
    {
        return level switch
        {
            LevelType.OhmLaw  => ManualReto1(),
            LevelType.Parallel=> ManualReto2(),
            LevelType.Mixed   => ManualReto3(),
            LevelType.Arduino => ManualReto4(),
            _                 => new ManualData { titulo = "Sin manual disponible" }
        };
    }

    // ─────────────────────────────────────────────
    //  RETO 1 — Ley de Ohm
    // ─────────────────────────────────────────────
    ManualData ManualReto1() => new ManualData
    {
        titulo    = "RETO 1 — Circuito Serie & Ley de Ohm",

        concepto  = "Un circuito serie conecta los componentes en cadena.\n" +
                    "La misma corriente I fluye por todos los componentes.\n" +
                    "El voltaje total se divide en caídas proporcionales a cada R.",

        formula   = "Ley de Ohm:     V = I × R\n" +
                    "Corriente:       I = V / R_total\n" +
                    "R total serie:   R_t = R1 + R2 + ...\n" +
                    "━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                    "Ejemplo con tus datos:\n" +
                    "R_necesaria = (V_fuente - V_LED) / I_LED\n" +
                    "I_LED correcto: 0.005A a 0.020A",

        objetivo  = "El LED está en sobrecarga (rojo) porque la\n" +
                    "resistencia tiene el valor incorrecto.\n\n" +
                    "1. Pide al Explorador que mida V en nodo_A y nodo_B\n" +
                    "2. Calcula: R = (9V - V_LED) / I_objetivo\n" +
                    "3. El valor correcto es 100 Ω\n" +
                    "4. Escribe 100 en el campo y pulsa ENVIAR",

        tablaValores =
                    "CÓDIGO DE COLORES — Resistencia 100Ω 5%\n" +
                    "━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                    "Banda 1: Marrón  (1)\n" +
                    "Banda 2: Negro   (0)\n" +
                    "Banda 3: Marrón  (×10)\n" +
                    "Banda 4: Oro     (±5%)\n" +
                    "━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                    "Resistencia INCORRECTA en nave: 10Ω\n" +
                    "→ Bandas: Marrón-Negro-Negro-Oro"
    };

    // ─────────────────────────────────────────────
    //  RETO 2 — Circuito Paralelo
    // ─────────────────────────────────────────────
    ManualData ManualReto2() => new ManualData
    {
        titulo    = "RETO 2 — Circuito Paralelo & Divisor de Corriente",

        concepto  = "En paralelo, cada rama recibe el MISMO voltaje.\n" +
                    "La corriente total se divide entre las ramas.\n" +
                    "Una rama abierta (∞Ω) no recibe corriente → sensor apagado.",

        formula   = "Voltaje en cada rama: V_rama = V_fuente\n" +
                    "Corriente por rama:   I_n = V / R_n\n" +
                    "Corriente total:      I_t = I_1 + I_2 + I_3\n" +
                    "R equivalente:        1/R_eq = 1/R1 + 1/R2 + 1/R3",

        objetivo  = "Uno de los 3 sensores (LEDs) no enciende.\n" +
                    "La rama tiene resistencia 9999Ω → circuito abierto.\n\n" +
                    "1. Pide al Explorador medir voltaje en cada sensor\n" +
                    "2. El sensor con 0V en sus nodos es la rama rota\n" +
                    "3. Indica al Explorador qué cable reconectar\n" +
                    "4. Pulsa REPARAR PARALELO para autorizar",

        tablaValores =
                    "VALORES ESPERADOS (rama normal)\n" +
                    "━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                    "V en nodo+ de cada sensor: 9.0 V\n" +
                    "V en nodo- de cada sensor: 0.0 V\n" +
                    "I por sensor normal: 9/50 = 0.18 A\n" +
                    "━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                    "Sensor roto:  V_nodo+ ≈ 0V  (sin corriente)\n" +
                    "Sensor OK:    LED verde encendido"
    };

    // ─────────────────────────────────────────────
    //  RETO 3 — Circuito Mixto & Polaridad
    // ─────────────────────────────────────────────
    ManualData ManualReto3() => new ManualData
    {
        titulo    = "RETO 3 — Circuito Mixto & Polaridad de Componentes",

        concepto  = "3 fallas simultáneas en el módulo de control:\n" +
                    "• LED con polaridad invertida → no enciende\n" +
                    "• Capacitor electrolítico invertido → cortocircuito\n" +
                    "• Resistencia con código de colores erróneo",

        formula   = "POLARIDAD LED:\n" +
                    "  Ánodo (+) → al voltaje positivo\n" +
                    "  Cátodo (−) → a tierra (banda plana o pata corta)\n\n" +
                    "POLARIDAD CAPACITOR electrolítico:\n" +
                    "  (+) banda blanca / pata larga → positivo\n" +
                    "  (−) banda negra / pata corta → tierra\n\n" +
                    "CÓDIGO COLORES — R=220Ω:\n" +
                    "  Rojo-Rojo-Marrón-Oro",

        objetivo  = "Corregir las 3 fallas EN ORDEN DE PRIORIDAD:\n\n" +
                    "PRIORIDAD 1 → Capacitor (humo = riesgo crítico)\n" +
                    "  Indica al Explorador: girar el capacitor 180°\n\n" +
                    "PRIORIDAD 2 → LED invertido\n" +
                    "  Indica: girar el LED para invertir polaridad\n\n" +
                    "PRIORIDAD 3 → Resistencia incorrecta (470Ω)\n" +
                    "  Valor correcto: 220Ω → Enviar componente",

        tablaValores =
                    "RESISTENCIA INCORRECTA vs CORRECTA\n" +
                    "━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                    "INCORRECTA (nave): 470Ω\n" +
                    "  Amarillo-Violeta-Marrón-Oro\n\n" +
                    "CORRECTA:         220Ω\n" +
                    "  Rojo-Rojo-Marrón-Oro\n" +
                    "━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                    "Indicador de humo en capacitor:\n" +
                    "  → corregir ANTES que el LED"
    };

    // ─────────────────────────────────────────────
    //  RETO 4 — Sensor-Actuador Arduino
    // ─────────────────────────────────────────────
    ManualData ManualReto4() => new ManualData
    {
        titulo    = "RETO 4 — Sensor-Actuador con Arduino",

        concepto  = "El sensor de temperatura debe activar una alarma (buzzer).\n" +
                    "3 fallas impiden el funcionamiento:\n" +
                    "• Sensor en pin incorrecto del Arduino\n" +
                    "• Buzzer sin resistencia limitadora\n" +
                    "• Cable suelto en la protoboard",

        formula   = "PINOUT ARDUINO (sensor temperatura):\n" +
                    "  VCC → 5V\n" +
                    "  GND → GND\n" +
                    "  OUT → Pin Digital 2  ← CORRECTO\n" +
                    "  (nave: conectado en Pin 4 → INCORRECTO)\n\n" +
                    "RESISTENCIA BUZZER:\n" +
                    "  R = (V_fuente - V_buzzer) / I_max\n" +
                    "  R = (5V - 1.5V) / 0.01A = 330Ω\n" +
                    "  Bandas: Naranja-Naranja-Marrón-Oro",

        objetivo  = "1. Localiza el cable del sensor\n" +
                    "   → Indica al Explorador: mover de Pin 4 a Pin 2\n\n" +
                    "2. Calcula la resistencia del buzzer: 330Ω\n" +
                    "   → Escribe 330 y pulsa ENVIAR al Explorador\n\n" +
                    "3. Cable suelto en protoboard\n" +
                    "   → Indica al Explorador: reconectar fila G-14\n\n" +
                    "Verificación: monitor serial muestra lecturas\n" +
                    "y el buzzer suena cuando T > umbral",

        tablaValores =
                    "PINOUT ARDUINO UNO (referencia)\n" +
                    "━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                    "Pines digitales: 0-13\n" +
                    "Pines analógicos: A0-A5\n" +
                    "Pin sensor correcto: D2\n" +
                    "Pin sensor incorrecto (nave): D4\n" +
                    "━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                    "RESISTENCIA 330Ω:\n" +
                    "  Naranja-Naranja-Marrón-Oro\n" +
                    "Voltaje buzzer activo: aparece en monitor serial"
    };
}

[System.Serializable]
public struct ManualData
{
    public string titulo;
    public string concepto;
    public string formula;
    public string objetivo;
    public string tablaValores;
}