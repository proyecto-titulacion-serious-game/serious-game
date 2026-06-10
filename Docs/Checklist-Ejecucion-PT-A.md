# Checklist de ejecución — Pruebas de Aceptación (PT-A / O5)

> Operativo, 2026-06-02. Complementa (no reemplaza) el `Protocolo-Pruebas-Usuarios.md`,
> que ya contiene instrumentos y Anexos A–G. Aquí está la secuencia para **ejecutar** las
> sesiones, ligada al estado técnico actual del proyecto.

---

## FASE 0 — Bloqueantes técnicos (cerrar ANTES de cualquier sesión)

- [ ] **Reto 4 E2E en hardware real** (PC Host + Quest Client). Con el fix del bridge ya aplicado,
      verificar que: enviar componente sube contador en ambos, subir código sube el contador en
      el Quest y enciende el LED. → recién aquí TC-S05 pasa a "Realizado".
- [ ] **Rebuild del APK del Quest** con todos los fixes de esta sesión (bridge, región, HUD,
      zonas). Confirmar que el `NullReferenceException` de OpenXR ya no aparece.
- [ ] **Red:** `ConnectionManager.modoOffline = false` (ambas escenas) · `FixedRegion = us` ·
      AppID Photon válido · sala `LaboratorioUbicua`.
- [ ] **Optimizar el tamaño del APK** (hoy ~2,1 GB): desactivar *Development Build* / *Debug
      Symbols*, revisar compresión de texturas (ASTC). Objetivo: que la instalación y el arranque
      en el Quest sean ágiles para sesiones consecutivas.
- [ ] **Audio mínimo:** asignar al menos `ButtonClick`/`Button Pop` (ya en el proyecto) como
      placeholder en `CircuitAudioManager`; descargar los 3 SFX CC0 si da tiempo.
- [ ] **Onboarding activo:** `ExplorerOnboarding` en escena (5 slides) — es lo que sustituye a la
      explicación verbal y permite medir M1 (comprensibilidad).
- [ ] **HUD/Clipboard por zona:** asignar el `Reto` correcto a cada `ZoneHUDTrigger` y guardar.

## FASE 1 — Preparación logística (días previos)

- [ ] Aprobación del tutor para el estudio con usuarios; reservar laboratorio y franja horaria.
- [ ] Reclutar **≥ 5 parejas** (ideal 8–10) de Computación Ubicua; agendar turnos de ~60 min.
- [ ] Imprimir 1 set por participante: consentimiento (A), demográfico (B), pre/post-test (C/C'),
      observación (D), SUS (E), Likert (E'), SSQ (F), entrevista (G).
- [ ] Opcional: digitalizar B, C, E, E', F, G en **Google Forms** para tabular automático.
- [ ] Tener la **clave del pre/post-test** solo en poder del facilitador (Anexo C: 1a,2c,3a,4b,5a,
      6b,7c,8c,9b,10b).

## FASE 2 — Pilotaje (obligatorio, 1 pareja)

- [ ] Ejecutar el protocolo completo con **1 pareja piloto** (no cuenta para resultados).
- [ ] Ajustar tiempos, redacción de ítems y montaje según lo observado.
- [ ] Confirmar que la hoja de observación (D) se llena con fluidez durante el juego.

## FASE 3 — Montaje el día de la sesión (checklist del facilitador)

- [ ] PC del Técnico con build Windows probada · Quest cargado (>80 %) con APK instalada.
- [ ] KAT VR montada y calibrada · chaleco háptico emparejado · espacio despejado + apoyo.
- [ ] **Smoke test E2E de los 4 retos el mismo día** (PC+Quest en LAN).
- [ ] Cronómetro, sets impresos, agua y silla de recuperación listos.

## FASE 4 — Por cada pareja (≈45–60 min) — secuencia del protocolo

- [ ] 1. Recepción + **consentimiento** (A) — 5 min.
- [ ] 2. Demográfico (B) + **Pre-test** (C) sin ayuda — 8 min.
- [ ] 3. **SSQ inicial** (F) — 1 min.
- [ ] 4. Equipamiento (visor/IPD/KAT/chaleco) — **sin explicar la mecánica** (mide M1) — 5 min.
- [ ] 5. **Juego de los 4 retos** — observar y registrar (D) sin intervenir salvo seguridad — 20–30 min.
- [ ] 6. **Post-test** (C') — 8 min.
- [ ] 7. **SUS** (E) + **Likert** (E') + **SSQ final** (F) — 6 min.
- [ ] 8. **Entrevista** breve (G) — 5 min.
- [ ] Verificar que todos los formularios quedaron completos antes de despedir a la pareja.

## FASE 5 — Tras cada sesión

- [ ] Asignar/confirmar **código de participante** y retirar cualquier dato identificable.
- [ ] Guardar la observación (D) y los formularios en la planilla anonimizada.
- [ ] Anotar incidencias técnicas (caídas de red, bugs) para el registro de defectos.

## FASE 6 — Análisis (con todas las parejas)

- [ ] **M1 Comprensibilidad** = (usuarios que inician su rol sin ayuda / total) × 100 → **>80 %**
- [ ] **M2 Consolidación** = % de mejora de aciertos Reto 1 → Reto 4 → **≥20 %**
- [ ] **M3 Operabilidad** = (acciones sin error de UI / acciones totales) × 100 → **>90 %**
- [ ] **M4 Satisfacción** = promedio Likert (E') → **≥3,5**
- [ ] **M5 SUS** = fórmula del Anexo E (media y DE) → **≥68**
- [ ] **M6 Aprendizaje** = pre vs. post (t pareada o Wilcoxon si n pequeño) + tamaño del efecto
- [ ] **M7 SSQ** = diferencia después−antes; % con síntoma ≥2
- [ ] Tabular todo (media, mediana, DE) y comparar contra los umbrales (aprobado / no aprobado).

## FASE 7 — Cierre documental

- [ ] Rellenar el marcador **[RELLENAR]** de **§7.4.6** con los resultados de PT-A.
- [ ] Completar **§7.4.7 (Discusión)** con la interpretación (qué se logró, hallazgos, fortalezas/
      debilidades).
- [ ] Adjuntar la **planilla anonimizada** y los formularios como **Anexo** del Capstone.
- [ ] Con O5 cerrado, ajustar **§8 Conclusiones** (O5 de "parcialmente alcanzado" a "alcanzado").

---

## Criterio de aprobación de PT-A

El sistema "aprueba" PT-A si se cumplen los umbrales de M1–M5 (los obligatorios del §7.3.5).
M6 y M7 son evidencia complementaria de aprendizaje y comodidad. Reportar también los
umbrales **no** alcanzados como oportunidades de mejora (insumo para §9 Trabajo futuro).
