# Borrador — BUG-07 para la tabla §7.3.6 (Defectos identificados y correcciones)

> Redactado el 2026-06-02. Fila adicional para la tabla de defectos del documento, con las
> mismas columnas (ID · Descripción · Módulo · Severidad · Corrección aplicada).

## Ajuste del texto introductorio de §7.3.6

El párrafo introductorio dice actualmente "se identificaron y corrigieron **seis** defectos".
Con la incorporación de BUG-07 debe actualizarse a "**siete** defectos" (consistente con
§7.4.5 del borrador de resultados).

## Fila a insertar en la tabla

| ID | Descripción | Módulo | Severidad | Corrección aplicada |
|----|-------------|--------|-----------|---------------------|
| BUG-07 | El sketch compilado por el Técnico en el editor de Arduino no se reflejaba en el Arduino del Explorador durante una sesión multijugador con escenas separadas; en consecuencia, el Reto 4 no podía completarse de extremo a extremo (escenario TC-S05, "en ajuste"). | Networking / UI | Alta | El enlace de red del Arduino (`ArduinoNetworkBridge`) era un objeto de escena presente únicamente del lado del Explorador y, por tanto, no replicado al Host, por lo que el RPC de programación no cruzaba entre escenas distintas. Se reencaminó la entrega del sketch a través del objeto de red compartido `GameSession` —instanciado por el Host y replicado a ambos clientes— mediante el método `ArduinoNetworkBridge.DeliverSketch`; el editor `ArduinoIDEUI` prioriza el envío por `GameSession.Instance`. |

## Versión en texto plano (por si la tabla del Word usa celdas multilínea)

**ID:** BUG-07

**Descripción:** El sketch compilado por el Técnico en el editor de Arduino no se reflejaba en
el Arduino del Explorador durante una sesión multijugador con escenas separadas; en
consecuencia, el Reto 4 no podía completarse de extremo a extremo (escenario TC-S05).

**Módulo:** Networking / UI

**Severidad:** Alta

**Corrección aplicada:** El enlace de red del Arduino (ArduinoNetworkBridge) era un objeto de
escena presente únicamente del lado del Explorador y no replicado al Host, por lo que el RPC
de programación no cruzaba entre escenas distintas. Se reencaminó la entrega del sketch a
través del objeto de red compartido GameSession —instanciado por el Host y replicado a
ambos clientes— mediante el método ArduinoNetworkBridge.DeliverSketch; el editor
ArduinoIDEUI prioriza el envío por GameSession.Instance.

## Impacto en otras secciones (recordatorio)

- **§7.3.4 (Pruebas de sistema):** el estado de **TC-S05** pasa de *"En ajuste"* a **Realizado**
  una vez verificado el Reto 4 de extremo a extremo en hardware real con esta corrección.
- **§7.4.5 (Resultados — gestión de defectos):** ya redactado mencionando los **siete**
  defectos (incluye BUG-07).
