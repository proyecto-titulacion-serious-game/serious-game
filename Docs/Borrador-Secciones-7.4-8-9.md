# Borrador — Secciones 7.4, 8 y 9 (Documento Capstone TITA)

> Redactado el 2026-06-02 a partir de los resultados reales de implementación y pruebas.
> Los valores de **usabilidad (PT-A)** se dejan como marcadores `[RELLENAR…]` porque
> dependen de las sesiones con usuarios aún no ejecutadas (§7.3.5 "Por relevar, Semana 12").
> No se incluyen cifras inventadas de SUS/Likert.

---

## 7.4. Resultados y Discusión

El presente apartado sintetiza los resultados obtenidos tras la ejecución de los cuatro
niveles de prueba definidos en la estrategia ISO/IEC/IEEE 29119 (§7.3.1) y los interpreta a
la luz de las características de calidad de la norma ISO/IEC 25010. Los resultados se
organizan en términos de completitud funcional, correctitud del motor de simulación,
integración entre módulos, comportamiento del sistema completo, gestión de defectos y
evaluación de usabilidad, cerrando con una discusión global de los hallazgos.

### 7.4.1. Completitud funcional

El prototipo implementa la totalidad del alcance comprometido (§4.1): un nivel continuo
compuesto por cuatro retos secuenciales de electrónica básica —Ley de Ohm (serie),
circuito en paralelo, circuito mixto con polaridad y sincronización hardware/software con
Arduino— resueltos bajo la dinámica de colaboración asimétrica entre los roles Explorador
(VR) y Técnico (PC). El flujo de juego de extremo a extremo se encuentra operativo: inicio
de sesión por rol, conexión multijugador, envío y manipulación de componentes,
diagnóstico mediante multímetro virtual, validación de circuitos y presentación automática
de la pantalla de resultados en ambos dispositivos. En términos de la subcaracterística de
**completitud funcional** (ISO/IEC 25010), el prototipo satisface los requerimientos
funcionales priorizados en el objetivo O3.

### 7.4.2. Correctitud del motor eléctrico (Pruebas de componente, PT-C)

Las pruebas unitarias verificaron la corrección matemática del motor de simulación de forma
aislada, contrastando los valores obtenidos con referencias calculadas analíticamente. Se
ejecutaron los **nueve casos TC-U01 a TC-U09 con un 100 % de aprobación**. Entre los
resultados representativos: la simulación en serie bajo la Ley de Ohm produjo una corriente
de 0,060 A coincidente con el cálculo teórico; la detección de cortocircuito activó
correctamente el indicador `isShortCircuited`; el cálculo de la rama en paralelo arrojó 6,21 V
frente a los 6,2 V esperados (desviación < 0,2 %); y la validación de tolerancia de resistencia
(`IsValueCorrect`) discriminó correctamente valores dentro y fuera de rango. Estos resultados
confirman la **exactitud (correctness)** del núcleo de cálculo, condición necesaria para que la
retroalimentación educativa que recibe el estudiante sea fiable.

Cabe destacar que, durante el desarrollo del cuarto reto, el motor lineal inicial fue sustituido
por un solver de **Análisis Nodal Modificado (MNA)**, que resuelve topologías arbitrarias
(serie, paralelo y mixtas) sobre la protoboard mediante la construcción de una matriz de
conductancias y su resolución por eliminación gaussiana con pivoteo. Esta decisión amplió la
robustez del simulador frente al enfoque previo, que asumía topologías fijas.

### 7.4.3. Integración y comunicación multijugador (Pruebas de integración, PT-I)

Las pruebas de integración verificaron la comunicación entre módulos a través del sistema de
eventos, validando la cadena `DeliverySystem → CircuitManager → GameManager` y la
sincronización de estado entre roles. De los **siete casos TC-I01 a TC-I07**, seis se
completaron satisfactoriamente y el caso TC-I02 (registro de intento incorrecto) quedó en
estado **parcial**, pendiente de un ajuste menor en el conteo de reintentos. El caso TC-I06,
que valida el envío de componentes por RPC desde el Técnico hacia el Explorador a través de
**Photon Fusion 2**, fue verificado en la escena IntegratedDemo con ambos roles conectados
en red local, confirmando la **interoperabilidad** del subsistema de red.

### 7.4.4. Comportamiento del sistema completo (Pruebas de sistema, PT-S)

Se ejecutaron los cuatro retos de principio a fin sobre la escena IntegratedDemo,
verificando el flujo completo de la experiencia. De los **siete escenarios TC-S01 a TC-S07**,
se aprobaron los relativos a los retos 1, 2 y 3, así como los flujos transversales de timer
agotado y finalización completa de la partida con pantalla de resultados (`OnGameCompleted`).
El escenario **TC-S05 (Reto 4: sincronización sensor-actuador con Arduino)** se encontraba
inicialmente en estado *"En ajuste"*: el código compilado por el Técnico no se reflejaba en el
Arduino del Explorador cuando ambos roles ejecutaban escenas distintas. El diagnóstico
determinó que el enlace de red del Arduino (`ArduinoNetworkBridge`) era un objeto de escena
presente únicamente del lado del Explorador y, por tanto, no replicado al Host, por lo que el
RPC no cruzaba entre escenas separadas. La corrección reencaminó la entrega del sketch a
través del objeto de red compartido `GameSession` —instanciado por el Host y replicado a
ambos clientes—, con lo que el reto 4 quedó operativo de extremo a extremo. Este hallazgo
se documenta como defecto BUG-07 en la sección 7.3.6.

### 7.4.5. Gestión de defectos

Conforme al proceso de gestión de incidencias de ISO 29119-3, se identificaron y corrigieron
**siete defectos** a lo largo de las fases de prueba (BUG-01 a BUG-07), abarcando la
respuesta de la interfaz del Técnico al teclado, el cierre del manual técnico, la captura de
clics sobre objetos 3D, la validación del cable suelto del reto 4, la inicialización de listas de
componentes y, finalmente, el encaminamiento del RPC de programación del Arduino por el
canal de red compartido. El carácter iterativo del proceso —corregir cada defecto antes de
avanzar al siguiente nivel— resultó coherente con la metodología en cascada adoptada y
mantuvo la **fiabilidad (madurez)** del prototipo a lo largo del desarrollo.

### 7.4.6. Evaluación de usabilidad (Pruebas de aceptación, PT-A)

La evaluación de usabilidad con usuarios del segmento objetivo (estudiantes de la asignatura
Computación Ubicua) se rige por el protocolo e instrumentos descritos en los Anexos
(consentimiento informado, cuestionario demográfico, pre/post-test de comprensión, hoja de
observación, System Usability Scale, escala Likert y cuestionario de cybersickness SSQ). Las
métricas y umbrales de aceptación definidos (§7.3.5) son: comprensibilidad del rol > 80 %,
mejora de aciertos entre el Reto 1 y el Reto 4 ≥ 20 %, operabilidad > 90 %, satisfacción Likert
≥ 3,5 y System Usability Scale ≥ 68.

> **[RELLENAR tras las sesiones de laboratorio]** — Resultados cuantitativos de PT-A:
> Comprensibilidad: ___ % · Mejora Reto1→Reto4: ___ % · Operabilidad: ___ % ·
> Likert (promedio): ___ · SUS: ___ puntos. Análisis de la hoja de observación y de las
> respuestas del SSQ (cybersickness). Tamaño de muestra: ___ parejas.

### 7.4.7. Discusión

Los resultados de los tres primeros niveles de prueba evidencian que el prototipo alcanza un
grado de madurez funcional y técnica adecuado para su despliegue en un escenario de uso
real. La verificación analítica del motor eléctrico (PT-C) garantiza que la retroalimentación
inmediata —identificada en el diagnóstico del problema como la principal carencia pedagógica
(§1.1)— se sustenta en cálculos correctos. La integración multijugador (PT-I) y el flujo
completo de los cuatro retos (PT-S) confirman que la dinámica de colaboración asimétrica,
seleccionada mediante la matriz ISO/IEC 25010 (Alternativa A, §2.1), es viable sobre el
hardware disponible (Meta Quest 3 + PC en red local).

Como **fortaleza** principal destaca la separación estricta de información entre roles, que
convierte la comunicación verbal en una mecánica central y refuerza el aprendizaje
colaborativo. Entre las **debilidades** identificadas en esta etapa se encuentran: (i) la
ausencia, hasta el momento, de datos empíricos de usabilidad, que constituye la principal
limitación para cerrar el objetivo O5; y (ii) el tamaño del paquete de instalación del cliente VR,
cuya optimización se recomienda antes de las sesiones masivas para no comprometer los
tiempos de despliegue en el laboratorio. La interpretación definitiva de la efectividad
educativa del prototipo queda condicionada a los resultados de PT-A.

---

## 8. Conclusiones y Recomendaciones

### 8.1. Conclusiones

El proyecto logró desarrollar un Serious Game multijugador en realidad virtual orientado al
aprendizaje de electrónica básica para la asignatura Computación Ubicua, cumpliendo el
objetivo general planteado. En relación con los objetivos específicos se concluye que:

- **O1 (requisitos).** Se levantaron y documentaron los requerimientos funcionales y no
  funcionales a partir de una encuesta exploratoria a 20 estudiantes, la revisión del sílabo y el
  análisis del hardware VR disponible, cuyo diagnóstico (dificultades cognitivas, barreras
  psicológicas y brecha de motivación) orientó el diseño de la solución.

- **O2 (arquitectura).** Se diseñó la arquitectura del sistema mediante diagramas UML y un
  modelo C4, definiendo los módulos de interacción, locomoción segura, conexión
  multijugador y estructura gamificada, materializados en los paquetes Electrical, Gameplay,
  Networking, Interaction, Player y UI.

- **O3 (desarrollo).** Se implementó el prototipo en Unity integrando el entorno virtual
  interactivo, el sistema multijugador asimétrico sobre Photon Fusion 2, la manipulación de
  componentes electrónicos virtuales y las actividades educativas alineadas a la asignatura, a
  través de los cuatro retos comprometidos.

- **O4 (integración y pruebas controladas).** Se verificó la estabilidad funcional del prototipo
  en un entorno controlado mediante 9 pruebas de componente (100 % aprobadas), 7 de
  integración y 7 de sistema, junto con la corrección de 7 defectos conforme a ISO 29119-3,
  alcanzando un comportamiento estable de los cuatro retos de extremo a extremo.

- **O5 (despliegue real).** La solución se encuentra técnicamente lista para su implementación
  en un escenario de uso real; la evaluación de usabilidad con usuarios (PT-A) y el
  consiguiente análisis de rendimiento e interacción están programados como fase de cierre,
  con el protocolo e instrumentos ya elaborados. Este objetivo se considera **parcialmente
  alcanzado**, a la espera de los datos de las sesiones de laboratorio.

En conjunto, se concluye que la realidad virtual y la colaboración asimétrica constituyen un
enfoque viable para transformar una práctica de laboratorio limitada por tiempo y por la
disponibilidad de componentes físicos en una experiencia segura, repetible y con
retroalimentación inmediata, en línea con la evidencia reportada en la literatura sobre VR en
educación STEM.

### 8.2. Recomendaciones

- Ejecutar las sesiones de PT-A con al menos una prueba piloto previa, para depurar la
  logística (calibración del visor, conexión LAN, guiado verbal entre roles) antes de las
  sesiones formales.
- Optimizar el paquete de instalación del cliente VR (compresión de texturas, desactivación de
  símbolos de depuración y *development build*) antes del despliegue, a fin de reducir los
  tiempos de instalación y el almacenamiento requerido en los dispositivos.
- Mantener una sala y región de red fijas durante las sesiones para garantizar el
  emparejamiento de los dispositivos, y disponer de un modo sin conexión como contingencia.
- Acompañar las primeras partidas con la fase de onboarding integrada, dado el bajo
  conocimiento previo de los estudiantes en entornos VR detectado en el diagnóstico.

---

## 9. Trabajo futuro

A partir de los resultados de este proyecto se identifican las siguientes líneas de continuidad:

- **Tablero docente de analítica de aprendizaje.** Ampliar el servidor de datos de sesión ya
  presente para ofrecer al profesorado un panel con métricas de desempeño (errores por reto,
  tiempos, progreso de aciertos), facilitando la evaluación formativa.
- **Ampliación del catálogo de retos.** Incorporar circuitos de corriente alterna, divisores de
  tensión, sensores adicionales y nuevas topologías que extiendan la cobertura curricular más
  allá de la electrónica de corriente continua.
- **Integración de la caminadora KAT VR.** Completar la locomoción natural mediante la
  caminadora omnidireccional para enriquecer la inmersión y la exploración física de la nave.
- **Modo de un solo jugador asistido.** Diseñar una variante individual en la que un agente
  asuma el rol complementario, de modo que el recurso pueda emplearse de forma autónoma
  fuera de las sesiones presenciales por parejas.
- **Distribución del cliente.** Publicar una versión empaquetada y optimizada para su
  instalación autónoma en los visores del laboratorio, así como evaluar una distribución interna
  controlada.
- **Estudio de efectividad longitudinal.** Realizar una evaluación con mayor tamaño de muestra
  y grupo de control que permita medir el impacto del recurso sobre el rendimiento académico a
  lo largo del semestre, más allá de la mejora intra-sesión.
