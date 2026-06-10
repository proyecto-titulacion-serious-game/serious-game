# Protocolo de Pruebas con Usuarios (PT-A) e Instrumentos
### Serious Game VR "TITA" — Evaluación de Usabilidad y Aprendizaje (ISO/IEC 25010)

> Documento operativo para ejecutar las **Pruebas de Aceptación (PT-A)** definidas en el Capstone §7.3.5 y cerrar el objetivo específico **O5**.
> Versión 1.0 — 31-may-2026. Aplicar en el laboratorio VR de la UDLA.

---

## 1. Objetivos de la evaluación

**Objetivo general:** evaluar la usabilidad, la efectividad de aprendizaje y la comodidad de uso del Serious Game con estudiantes del segmento objetivo, para establecer su rendimiento en un escenario de uso real.

**Objetivos específicos (alineados a las métricas del documento §7.3.5):**

| ID | Subcaracterística (ISO/IEC 25010) | Métrica | Umbral aceptable |
|----|-----------------------------------|---------|------------------|
| M1 | Comprensibilidad | % usuarios que comprenden su rol sin explicación previa | **> 80 %** |
| M2 | Consolidación de conocimiento | Mejora en tasa de aciertos Reto 1 → Reto 4 (misma sesión) | **≥ 20 %** |
| M3 | Operabilidad | % de acciones completadas sin error de interfaz | **> 90 %** |
| M4 | Satisfacción | Escala Likert 1–5 (promedio) | **≥ 3,5** |
| M5 | Presencia / Usabilidad global | System Usability Scale (SUS) | **≥ 68 puntos** |
| M6 | Aprendizaje (añadida) | Mejora pre-test → post-test de conocimiento | Mejora significativa |
| M7 | Comodidad VR (añadida) | Cybersickness (SSQ corto) | Síntomas bajos |

---

## 2. Participantes

- **Población:** estudiantes de Ingeniería de Software matriculados o que cursaron **Computación Ubicua** (UDLA).
- **Tamaño de muestra sugerido:** mínimo **5 parejas (10 participantes)**; ideal 8–10 parejas. (Nielsen: ~5 usuarios detectan el 80 % de problemas de usabilidad; para significancia de aprendizaje, cuantos más mejor.)
- **Unidad de prueba:** **pareja** Explorador + Técnico (la mecánica es cooperativa 2 jugadores).
- **Criterios de inclusión:** mayor de edad, sin contraindicaciones para VR (epilepsia fotosensible, vértigo severo, embarazo avanzado, problemas de equilibrio).
- **Criterios de exclusión:** haber participado en el desarrollo del juego.
- **Asignación de roles:** alternar para equilibrar (la mitad de las parejas con el de mayor conocimiento previo como Explorador, la otra mitad como Técnico), o asignar al azar y registrarlo.

---

## 3. Diseño experimental

- **Tipo:** estudio cuasiexperimental **intra-sujeto** (pre-test / post-test sin grupo de control), con observación estructurada y cuestionarios post-sesión.
- **Variable independiente:** uso del Serious Game (intervención).
- **Variables dependientes:** conocimiento (pre/post), usabilidad (SUS, Likert), operabilidad, comprensión de rol, comodidad (SSQ), tiempos e intentos por reto.
- **Control de sesgos:** mismo facilitador y guion para todas las parejas; pre-test y post-test con ítems equivalentes; no dar pistas durante el juego más allá del onboarding del propio sistema.

---

## 4. Materiales y montaje (checklist del facilitador)

- [ ] PC del Técnico con build Windows instalada y probada.
- [ ] Meta Quest 3 cargado (>80 %), build APK del Explorador instalada.
- [ ] Caminadora KAT VR montada y calibrada; chaleco háptico emparejado.
- [ ] Photon Fusion operativo en red LAN (sala `LaboratorioUbicua`); **verificar `modoOffline = false`** para sesión real.
- [ ] Espacio despejado, arnés/punto de apoyo de seguridad para la caminadora.
- [ ] Smoke test E2E de los 4 retos realizado el mismo día.
- [ ] Impresos: consentimiento, pre-test, post-test, SUS, Likert, SSQ, hoja de observación (1 set por participante/pareja).
- [ ] Cronómetro / registro de tiempos; cámara opcional (con consentimiento).
- [ ] Agua y silla para recuperación post-VR.

---

## 5. Procedimiento (≈ 45–60 min por pareja)

| Fase | Tiempo | Actividad |
|------|--------|-----------|
| 1. Recepción y consentimiento | 5 min | Explicar propósito, voluntariedad, derecho a abandonar. Firmar consentimiento (Anexo A). |
| 2. Datos demográficos + Pre-test | 8 min | Cuestionario demográfico (Anexo B) + Pre-test de conocimiento (Anexo C). **Sin ayuda.** |
| 3. Línea base de comodidad | 1 min | SSQ inicial (Anexo F) para medir síntomas previos. |
| 4. Equipamiento | 5 min | Colocar visor, ajustar IPD, calibrar KAT VR, chaleco. **No explicar la mecánica del juego** (para medir M1). |
| 5. Sesión de juego (4 retos) | 20–30 min | La pareja juega los 4 retos. El facilitador **observa y registra** (Anexo D) sin intervenir, salvo seguridad o bloqueo técnico. |
| 6. Post-test | 8 min | Post-test de conocimiento (Anexo C') equivalente al pre-test. |
| 7. Cuestionarios de experiencia | 6 min | SUS (Anexo E) + Likert de satisfacción (Anexo E') + SSQ final (Anexo F). |
| 8. Entrevista breve | 5 min | 3–4 preguntas abiertas (Anexo G). |

**Regla de oro:** durante la fase 5 el facilitador NO enseña a jugar. Si el sistema no logra que el usuario entienda su rol, eso es justamente lo que mide M1.

---

## 6. Plan de análisis de datos

- **M1 Comprensibilidad:** (nº usuarios que inician su rol correctamente sin pedir ayuda / total) × 100. Registrar en Anexo D.
- **M2 Consolidación:** comparar aciertos/intentos del Reto 1 vs. Reto 4 dentro de la sesión (de la hoja de observación). Calcular % de mejora.
- **M3 Operabilidad:** (acciones sin error de interfaz / acciones totales) × 100. Contar errores de UI en Anexo D.
- **M4 Satisfacción:** promedio de los ítems Likert (Anexo E').
- **M5 SUS:** calcular puntaje SUS (fórmula en Anexo E). Reportar media y desviación.
- **M6 Aprendizaje:** comparar puntaje pre vs. post (prueba t pareada o Wilcoxon si n pequeño). Reportar tamaño del efecto.
- **M7 SSQ:** comparar síntomas inicial vs. final; reportar % con malestar relevante.
- Reportar todo en tablas (media, mediana, DE) en **§7.4 Resultados y Discusión** del Capstone.

---
---

# ANEXO A — Consentimiento informado

**Proyecto:** Serious Game multijugador en realidad virtual para el aprendizaje de electrónica básica (UDLA).
**Investigadores:** Ricardo Quintana Maldonado, Mauro Vera Salguero. **Tutor:** Ing. Santiago Solórzano, Msc.

Se me ha invitado a participar en una prueba de usuario de un videojuego educativo en realidad virtual. Declaro que:

1. Comprendo que mi participación es **voluntaria** y que puedo **retirarme en cualquier momento** sin consecuencia alguna.
2. Entiendo que usaré un visor VR y una caminadora; se me ha informado de posibles molestias (mareo, fatiga visual) y de las medidas de seguridad.
3. Los datos recogidos (cuestionarios, tiempos, observaciones) se usarán **solo con fines académicos**, de forma **anónima y agregada**.
4. He podido hacer preguntas y han sido respondidas.

☐ Acepto que se grabe video/audio de la sesión (opcional).

Nombre: _______________________  Firma: ____________  Fecha: __/__/____

Contraindicaciones (marcar si aplica): ☐ Epilepsia fotosensible ☐ Vértigo severo ☐ Problemas de equilibrio ☐ Embarazo ☐ Ninguna

---

# ANEXO B — Datos demográficos

1. Código de participante (asignado): ______   Rol: ☐ Explorador ☐ Técnico
2. Edad: ____   3. Semestre/nivel: ____
4. ¿Cursaste/cursas Computación Ubicua? ☐ Sí ☐ No
5. Experiencia previa con VR: ☐ Ninguna ☐ Poca (1–3 veces) ☐ Frecuente
6. Experiencia previa con Unity/desarrollo de juegos: ☐ Ninguna ☐ Básica ☐ Intermedia/avanzada
7. Autoevaluación de conocimiento en electrónica básica (1=nulo, 5=experto): 1 2 3 4 5

---

# ANEXO C — Pre-test de conocimiento (electrónica básica)

> 10 ítems de opción múltiple. 1 punto cada uno. **Sin calculadora externa salvo indicación.** El post-test (Anexo C') usa ítems equivalentes con valores distintos.

**1. Ley de Ohm.** Si una resistencia de 100 Ω tiene una caída de 5 V, ¿qué corriente circula?
a) 0,05 A  b) 0,5 A  c) 20 A  d) 500 A

**2. Circuito serie.** En un circuito serie con dos resistencias (100 Ω y 200 Ω), la resistencia total es:
a) 66,7 Ω  b) 150 Ω  c) 300 Ω  d) 20 000 Ω

**3. Circuito paralelo.** Dos resistencias iguales de 100 Ω en paralelo dan una resistencia equivalente de:
a) 50 Ω  b) 100 Ω  c) 200 Ω  d) 0 Ω

**4. Corriente en serie.** En un circuito serie, la corriente que pasa por cada componente es:
a) Distinta en cada uno  b) La misma en todos  c) Cero  d) Depende del color

**5. Voltaje en paralelo.** En ramas en paralelo, el voltaje en cada rama es:
a) El mismo en todas  b) Se divide entre las ramas  c) Siempre 0  d) Siempre 9 V

**6. Polaridad del LED.** Un LED conectado con la polaridad invertida:
a) Se enciende más fuerte  b) No enciende  c) Explota siempre  d) Cambia de color

**7. Código de colores.** Una resistencia con bandas marrón-negro-marrón vale aproximadamente:
a) 10 Ω  b) 100 Ω  c) 1 000 Ω  d) 10 000 Ω

**8. Capacitor electrolítico.** Estos capacitores tienen polaridad; conectarlos al revés puede causar:
a) Nada  b) Mayor capacitancia  c) Falla/daño (calor, humo)  d) Más brillo

**9. Cortocircuito.** Un cortocircuito se caracteriza por:
a) Resistencia muy alta  b) Resistencia casi nula y corriente muy alta  c) Voltaje infinito  d) No circula corriente

**10. Arduino.** Para leer un sensor analógico en Arduino se usa típicamente:
a) Un pin digital de salida  b) Un pin analógico de entrada (A0…)  c) El pin de tierra (GND)  d) El pin de 5 V

*(Clave de respuestas para el facilitador: 1a, 2c, 3a, 4b, 5a, 6b, 7c, 8c, 9b, 10b.)*

**ANEXO C' — Post-test** (mismas competencias, valores distintos; p. ej. ítem 1 con 220 Ω y 11 V → 0,05 A; ítem 7 con rojo-rojo-rojo → 2 200 Ω, etc.).

---

# ANEXO D — Hoja de observación estructurada (por pareja)

Código de pareja: ______  Fecha: __/__/____  Facilitador: ____________

**Comprensión de rol (M1)** — marcar al inicio, sin ayuda del facilitador:
- Explorador inicia su rol correctamente sin pedir ayuda: ☐ Sí ☐ No
- Técnico inicia su rol correctamente sin pedir ayuda: ☐ Sí ☐ No

**Por reto** — registrar:

| Reto | ¿Completado? | Tiempo (mm:ss) | Intentos incorrectos | Errores de interfaz* | Pidió ayuda (nº) |
|------|--------------|----------------|----------------------|----------------------|------------------|
| 1 Serie / Ley de Ohm |  |  |  |  |  |
| 2 Paralelo |  |  |  |  |  |
| 3 Mixto / polaridad |  |  |  |  |  |
| 4 Arduino |  |  |  |  |  |

\* *Error de interfaz (M3): la acción del usuario fue correcta en intención pero la UI no respondió o respondió mal (botón que no reacciona, no encuentra el componente, no puede medir, etc.). Distinto de un error conceptual del usuario.*

Cálculo M3 al final: total acciones sin error / total acciones = ____ %
Notas cualitativas / incidencias: ________________________________________

---

# ANEXO E — System Usability Scale (SUS)

> 10 ítems, escala 1 (Totalmente en desacuerdo) a 5 (Totalmente de acuerdo). Aplicar a **cada participante**.

1. Creo que me gustaría usar este sistema con frecuencia.
2. Encontré el sistema innecesariamente complejo.
3. Pensé que el sistema era fácil de usar.
4. Creo que necesitaría apoyo de un técnico para poder usar este sistema.
5. Encontré que las distintas funciones del sistema estaban bien integradas.
6. Pensé que había demasiada inconsistencia en el sistema.
7. Imagino que la mayoría de la gente aprendería a usar este sistema muy rápido.
8. Encontré el sistema muy incómodo/engorroso de usar.
9. Me sentí muy seguro/a usando el sistema.
10. Necesité aprender muchas cosas antes de poder empezar a usar el sistema.

**Cálculo del puntaje SUS:**
- Ítems impares (1,3,5,7,9): puntaje = (respuesta − 1).
- Ítems pares (2,4,6,8,10): puntaje = (5 − respuesta).
- Sumar los 10 valores y multiplicar por 2,5 → puntaje de 0 a 100.
- **Umbral del proyecto: SUS ≥ 68** (promedio de la industria).

---

# ANEXO E' — Satisfacción (Likert 1–5)

> 1 = Totalmente en desacuerdo, 5 = Totalmente de acuerdo. **Umbral del proyecto: promedio ≥ 3,5.**

1. El juego me ayudó a entender mejor los conceptos de electrónica básica.
2. La retroalimentación (luces, sonido, vibración) fue clara y útil.
3. La colaboración con mi compañero/a fue necesaria y bien lograda.
4. La narrativa (nave espacial) hizo la experiencia más motivadora.
5. Recomendaría este juego como apoyo a la asignatura Computación Ubicua.
6. Me sentí inmerso/a en el entorno virtual.

---

# ANEXO F — Cybersickness (SSQ corto)

> Marcar la intensidad de cada síntoma: 0 = Ninguno, 1 = Leve, 2 = Moderado, 3 = Severo. Aplicar **antes** y **después** de la sesión.

| Síntoma | Antes (0–3) | Después (0–3) |
|---------|-------------|----------------|
| Malestar general |  |  |
| Fatiga |  |  |
| Dolor de cabeza |  |  |
| Fatiga visual |  |  |
| Dificultad para enfocar |  |  |
| Náusea |  |  |
| Mareo (con ojos abiertos) |  |  |
| Sensación de vértigo |  |  |

Reportar diferencia (después − antes) y % de participantes con algún síntoma ≥ 2 al final.

---

# ANEXO G — Entrevista breve (preguntas abiertas)

1. ¿Qué fue lo más fácil y lo más difícil de tu rol?
2. ¿Hubo algún momento en que no supiste qué hacer o el sistema no respondió como esperabas?
3. ¿Qué cambiarías o agregarías al juego?
4. ¿Sientes que aprendiste o reforzaste algo de electrónica? ¿Qué?

---

## Notas de implementación

- Puedes digitalizar los Anexos B, C, E, E', F, G en **Google Forms** (ya tienes uno de la encuesta diagnóstica en el documento) para tabular automáticamente.
- Pilotea el protocolo con **1 pareja** antes de las sesiones formales; ajusta tiempos y redacción de ítems.
- Guarda los datos crudos anonimizados (planilla) como **Anexo del Capstone**; los agregados van a §7.4 Resultados.
- Relaciona los hallazgos con los umbrales de la tabla de §1 para concluir si el sistema "aprueba" PT-A.
