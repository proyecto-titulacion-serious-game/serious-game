# Plan de Desarrollo — Serious Game VR "TITA"
### Serious Game multijugador en realidad virtual para el aprendizaje de electrónica básica (Computación Ubicua, UDLA)

> Documento de planificación derivado del *Formato Documento Capstone — versión final 15-ene-2026* (Ricardo Quintana Maldonado · Mauro Vera Salguero · Tutor: Ing. Santiago Solórzano, Msc.) y del estado real del proyecto Unity al **31-may-2026**.
>
> **Metodología (según el Capstone, §5.1):** Cascada / Waterfall — 5 fases secuenciales, 12 semanas. Este plan respeta ese marco y detalla el trabajo restante de las **Fases 4 (Pruebas e Integración)** y **5 (Despliegue y Evaluación)**, que es donde está el proyecto hoy.

---

## 1. Resumen ejecutivo y diagnóstico

El Capstone propone (y selecciona, vía matriz ISO/IEC 25010) la **Alternativa A: VR inmersivo con locomoción física y colaboración asimétrica**. Dos estudiantes cooperan con roles e información diferenciados para diagnosticar y reparar circuitos averiados en una **nave espacial**:

- **Explorador** — VR (Meta Quest 3 + caminadora KAT VR + chaleco háptico). Inspecciona, mide con multímetro, reemplaza componentes. *No* tiene manuales.
- **Técnico** — PC. Consulta manuales/diagramas, calcula valores, diagnostica y guía. *No* ve el entorno 3D.

La dependencia mutua obliga a comunicación técnica precisa ("voltaje en nodo A: 9 V"), que es el corazón pedagógico del juego.

**Estado real (muy avanzado).** El documento (§7) y el código describen un proyecto prácticamente *code-complete*: **Unity 6 (6000.4.3f1)**, URP 17.4, **Photon Fusion**, **XRI 3.4.1 / OpenXR 1.16.1**, ~50 scripts en 8 módulos, 3 escenas, CI/CD en GitHub Actions con Doxygen. Pruebas internas reportadas: **PT-C 8/8, PT-I 6/6, PT-S 5/5 superadas**.

**Las dos brechas reales que cierran la tesis:**
1. **Evaluación con usuarios (PT-A) = "Pendiente"** en el propio documento. Es el objetivo específico O5 y la mayor pieza faltante.
2. **Secciones del documento en blanco:** Resumen, Abstract, 7.4 Resultados y Discusión, 7.5 Implicaciones éticas, 8 Conclusiones, 9 Trabajo futuro, 10 Referencias (APA 7), 11 Anexos, y tablas de figuras.

A esto se suman tareas finas de **integración en escena** (wiring) y el **cierre de Retos 3 y 4**, ya identificadas en las notas técnicas del proyecto.

---

## 2. Objetivos del Capstone → estado actual

| # | Objetivo específico (Capstone §3.2) | Estado | Trabajo restante |
|---|--------------------------------------|--------|------------------|
| O1 | Analizar y documentar requerimientos (entrevistas, sílabo, hardware) | 🟢 Hecho | Reflejado en encuesta a 20 estudiantes |
| O2 | Diseñar arquitectura (UML/C4, interacción, locomoción, red, gamificación) | 🟢 Hecho | Diagramas C4 niveles 1-3 + clases |
| O3 | Desarrollar en Unity (entorno, multijugador, manipulación, actividades) | 🟢 ~90% | Cerrar Reto 3, wiring Reto 4 |
| O4 | Integrar mediante casos de prueba (usabilidad, locomoción, comprensión) | 🟡 ~70% | PT-A pendiente; smoke test E2E en hardware |
| O5 | Implementar en escenario real (rendimiento + interacción + optimización) | 🔴 ~15% | Sesiones reales en laboratorio UDLA |

---

## 3. Los 4 retos (alcance confirmado, Capstone §4.1)

Un **único nivel continuo** con 4 retos secuenciales de dificultad creciente. Solo **corriente continua y componentes pasivos** (primer progreso del sílabo). Modo **cooperativo 2 jugadores**; sin modo individual ni versión móvil.

| Reto | Concepto | Falla simulada | Éxito (límite) |
|------|----------|----------------|----------------|
| **1 — Serie / Ley de Ohm** | V = I·R, circuito serie | Resistencia de valor incorrecto → sobrevoltaje en LEDs | Corregido < **8 min**, ≤ 3 intentos |
| **2 — Paralelo** | Divisor de corriente, R equivalente | Una rama abierta + otra en cortocircuito | Todas las ramas OK < **10 min** |
| **3 — Mixto + Polaridad** | Serie-paralelo, polaridad, código de colores | 3 fallas: LED invertido, capacitor invertido, resistencia errónea | 3 fallas en orden < **12 min** |
| **4 — Arduino (sensor-actuador)** | Pines digital/analógico, protoboard | Pin incorrecto + falta resistencia limitadora + cable suelto | Alarma funcional < **15 min** |

Retroalimentación inmediata por reto: **visual** (LEDs rojo→verde, humo, chispas, partículas, monitor serial), **háptica** (vibración del chaleco proporcional a la corriente; pulso de confirmación) y **auditiva**.

---

## 4. Arquitectura (referencia, Capstone §7.1)

Tres contenedores sobre Photon Fusion Cloud:
- **Cliente VR (Explorador)** — Meta Quest 3, KAT VR con *fallback* a joystick, XRI 3.4.1, URP.
- **Cliente PC (Técnico)** — Host/StateAuthority de la sesión; manual, selección y envío de componentes, diagnóstico.
- **Photon Fusion Cloud** — sincroniza `GameSession` (NetworkObject + RPCs tipados), sala fija `LaboratorioUbicua`.

8 módulos (SRP/SOLID): `Electrical` (motor de simulación: serie/paralelo/mixto + `ArduinoCore`), `Gameplay` (GameManager + retos + entrega + puntuación), `Interaction` (grab, multímetro, slots), `Networking`, `Player` (locomoción + háptica), `UI`, `Desktop`, `Core`. Patrones: **Observer/event-driven, Dirty Flag (20 Hz), Strategy, Template Method**. Cortocircuito si `R_total ≤ 0.1 Ω`.

> **Restricciones de diseño asumidas:** sin persistencia entre sesiones; **dashboard docente = trabajo futuro** (no entra en este alcance).

---

## 5. Plan de trabajo — Fases 4 y 5 (lo que falta)

Organizado en bloques de trabajo dentro del marco Cascada del documento. Cada bloque genera un entregable verificable.

### Bloque A — Estabilización e integración en escena (Fase 4)
*Bloquea las pruebas con usuarios; debe quedar limpio primero.*

- [ ] **A1** Verificar `ConnectionManager.modoOffline` (no dejar `true` de testing → rompe la entrega de componentes en multijugador real).
- [ ] **A2** Completar wiring de escena pendiente (usar `Tools → TITA → *`):
  - Multímetro en escena Explorador · `puntoDeEntrega` en `ExplorerComponentReceiver`
  - `CircuitAudioManager` + AudioClips · `cablePrefab` en `CableBoxSpawner`
  - Panel de validación en `TechnicianHUDController` · AudioClips en `VRValidationButton`
  - Posicionar `Walker_PC` y `SeatTrigger_Chair` en la sala
- [ ] **A3** Warning GUIStyle (Fusion + Unity 6): **no parchear** (ya documentado como cosmético); actualizar Fusion 2 o aceptar.
- [ ] **A4** Smoke test E2E de los 4 retos en **hardware real** (Quest 3 + PC), no solo Editor.

### Bloque B — Cierre de contenido de retos (Fase 4)
- [ ] **B1** Reto 3 (Mixto): validar resolución de las 3 fallas + condición de victoria.
- [ ] **B2** Reto 4 (Arduino): completar wiring en escena (Auto-Setup ya implementado): protoboard, nodos, IDE↔bridge, telemetría, CableBox, botón de validación.
- [ ] **B3** Tras piloto, ajustar tiempos/dificultad (8/10/12/15 min) y feedback.

### Bloque C — Evaluación con usuarios / O5 (Fase 5) — *prioridad de tesis*
- [ ] **C1** Protocolo PT-A: muestra (parejas Explorador-Técnico de Computación Ubicua), tareas y métricas; consentimiento informado.
- [ ] **C2** Instrumentos: **SUS** (usabilidad), cuestionario de presencia/inmersión, **pre/post-test** de conocimiento (Ley de Ohm, polaridad, lectura de multímetro), registro de **cybersickness** (SSQ o autoinforme).
- [ ] **C3** Sesiones piloto (1-2 parejas) → ajustes → sesiones formales en laboratorio UDLA.
- [ ] **C4** Análisis de datos (aprendizaje pre→post, SUS, tiempos/intentos por reto, comodidad).

### Bloque D — Redacción del documento (Fase 5)
Completar las secciones en blanco con datos reales:
- [ ] **D1** §7.4 Resultados y Discusión (con datos del Bloque C) · §7.3.2/7.3.3 (tablas de casos de prueba y de los 6 defectos corregidos).
- [ ] **D2** §7.5 Implicaciones éticas (privacidad de datos, accesibilidad, seguridad/caídas en caminadora).
- [ ] **D3** §8 Conclusiones · §9 Trabajo futuro (más circuitos, dashboard docente, modos, AC).
- [ ] **D4** Resumen (≤350 palabras) + Abstract + §10 Referencias APA 7 + §11 Anexos + tablas de figuras/tablas.

### Bloque E — Entrega y defensa (Fase 5)
- [ ] **E1** Builds finales: **APK Meta Quest** (Explorador) + **build Windows** (Técnico).
- [ ] **E2** Demo grabada (los 4 retos) + documentación técnica (Doxygen en CI ya activo).
- [ ] **E3** Presentación de defensa.

---

## 6. Cronograma sugerido (base 31-may-2026; ajustar a tu fecha de defensa)

> El documento define 12 semanas totales; estas ya consumidas en su mayoría. Lo siguiente son ~**8 semanas** para cerrar Fases 4-5.

| Semana | Bloque | Entregable verificable |
|--------|--------|------------------------|
| 1 (02-06 jun) | A1, A2, A4 | Build E2E jugable de los 4 retos en Quest + PC |
| 2 (09-13 jun) | B1, B2, A3 | Retos 3 y 4 completables de inicio a fin |
| 3 (16-20 jun) | C1, C2, B3 | Protocolo + instrumentos PT-A listos; build congelada para pruebas |
| 4 (23-27 jun) | C3 (piloto) | Sesiones piloto + ajustes aplicados |
| 5 (30 jun-04 jul) | C3 (formal) | Sesiones formales ejecutadas; datos crudos |
| 6 (07-11 jul) | C4, D1 | Análisis + §7.4 Resultados redactada |
| 7 (14-18 jul) | D2, D3, D4 | Documento completo (sin placeholders) |
| 8 (21-25 jul) | E1, E2, E3 | Builds finales + demo + defensa lista |

---

## 7. Definición de "Terminado" (DoD)

1. Compila sin errores nuevos en Unity 6 y pasa el build de CI.
2. Funciona en **hardware real** (Quest 3 + PC), no solo Editor.
3. Tiene retroalimentación al usuario (visual/sonora/háptica) cuando aplica.
4. No rompe retos ya completados (smoke test de los 4).
5. Queda reflejado en la documentación técnica y, si aplica, en el documento Capstone.

---

## 8. Riesgos y mitigaciones

| Riesgo | Impacto | Mitigación |
|--------|---------|------------|
| KAT VR / chaleco no disponibles en pruebas | Bloquea PT-A | Fallback joystick + `modoOffline`; validar el núcleo educativo sin periféricos si hace falta |
| Cybersickness / caídas en caminadora | Sesgo, abandono, seguridad | Sesiones cortas, supervisión, arnés/espacio libre, medir comodidad explícitamente |
| Fusion 2 ↔ Unity 6 (warning GUIStyle) | Ruido en defensa | Ya diagnosticado cosmético; actualizar Fusion o documentar |
| Pérdida de referencias en Inspector | Re-trabajo de wiring | Usar Editor tools `Tools → TITA → *` que auto-cablean |
| Muestra reducida de usuarios | Resultados poco concluyentes | Combinar métricas cuantitativas (SUS, pre/post) con evidencia cualitativa |
| Alcance creep (AC, dashboard, más retos) | Retraso de la defensa | Respetar §4.2; todo eso → §9 Trabajo futuro |

---

## 9. Métricas de éxito (para §7.4 Resultados / O5)

- **Usabilidad:** SUS ≥ 68.
- **Aprendizaje:** mejora pre→post-test (Ley de Ohm, polaridad, multímetro).
- **Completitud:** % de parejas que completan cada reto dentro del tiempo límite (8/10/12/15 min).
- **Eficiencia:** tiempo e intentos por reto, tipo de errores.
- **Comodidad VR:** cybersickness bajo.
- **Compromiso/colaboración:** calidad de la comunicación técnica entre roles (observación) + intención de uso.

---

## 10. Próximos 3 pasos inmediatos

1. **Hoy/mañana:** smoke test E2E (A4) para fijar el punto de partida real y verificar `modoOffline` (A1).
2. **Esta semana:** completar el wiring de escena pendiente (A2) con los Editor tools `Tools → TITA`.
3. **En paralelo:** empezar a redactar el **protocolo PT-A e instrumentos** (C1, C2) — es el camino crítico de la tesis.
