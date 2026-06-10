# Guía de demostración para la defensa — Serious Game TITA

> 2026-06-02. Guion para demostrar el prototipo ante el tribunal, mapeado a los objetivos
> (O1–O5) y a los niveles de prueba (PT-C/I/S/A). Pensado para PC (Técnico) + Quest 3
> (Explorador) en red local, con plan de contingencia.

---

## 0. Antes de la defensa (preparación, el día previo)

- [ ] **Build congelada y probada**: APK del Quest optimizado (ver `Guia-Optimizacion-APK.md`)
      instalado y arrancado; build Windows del Técnico probada.
- [ ] **Red**: `modoOffline = false`, `FixedRegion = us`, AppID Photon válido, sala
      `LaboratorioUbicua`. Probar el emparejamiento PC↔Quest **en la sala real de la defensa**
      (otra red Wi-Fi puede cambiar la latencia).
- [ ] **Prueba decisiva pasada** (la víspera): enviar componente sube contador en ambos; subir
      código sube el contador en el Quest y enciende el LED (Reto 4 con el fix del bridge).
- [ ] **Plan B grabado**: video de 2–3 min de una partida completa (PC + casting del Quest lado
      a lado) por si falla la red o el hardware en vivo. **Imprescindible.**
- [ ] **Casting del Quest** configurado (a la pantalla/proyector) y probado.
- [ ] Visor cargado (>80 %), espacio despejado, segundo operador para ponerse el visor.

## 1. Estructura sugerida (≈8–10 min de demo dentro de la defensa)

| Min | Bloque | Qué mostrar | Objetivo que evidencia |
|-----|--------|-------------|------------------------|
| 0–1 | Contexto | Problema (electrónica abstracta, sin feedback) → solución asimétrica | O1 |
| 1–2 | Arquitectura | Diagrama: Explorador (VR) + Técnico (PC) + Photon Fusion | O2 |
| 2–4 | Conexión en vivo | Arrancar Host (PC) y Client (Quest); mostrar el **overlay de red** (rol, sala, peers=2, ping) | O3 |
| 4–7 | Reto en vivo | Jugar **Reto 1** (Ley de Ohm) o **Reto 4** (Arduino) de principio a fin | O3, O4 |
| 7–8 | Evidencia de red | Enviar componente / subir código → contador sube en **ambos** equipos | O4 (integración real) |
| 8–9 | Resultados | Pantalla de resultados (score, tiempos, errores) | O3 |
| 9–10 | Cierre | Estado de pruebas (PT-C/I/S ✓, PT-A en curso) y trabajo futuro | O4, O5 |

## 2. El momento clave: demostrar que el multijugador es REAL

Lo más convincente para un tribunal es probar **causalidad y dirección** de la red. Con el
**NetworkDemoOverlay** visible en ambas pantallas:

1. Señala que el Quest marca **PEERS: 2** y un **PING** real → están en la misma sesión Photon.
2. El Técnico **envía un resistor** → el contador de RPC sube **a la vez** en PC y Quest, con el
   mismo timestamp → el dato cruzó la red (no es local).
3. El Técnico **escribe `digitalWrite(7, HIGH)` y sube el código** → el contador del Quest sube y
   el **LED del pin 7 enciende** en el mundo VR → cierra el flujo hardware/software del Reto 4.

> Este es el resultado del **fix del bridge (BUG-07)**: el código ahora viaja por `GameSession`.
> Es tu mejor evidencia de O4 y conviene narrarlo explícitamente.

## 3. Guion de narración (frases ancla)

- *"El diseño es asimétrico a propósito: el Técnico ve el código y la telemetría 2D, el Explorador
  ve y manipula el hardware 3D. Ninguno ve la pantalla del otro, así que la comunicación verbal
  se vuelve una mecánica del juego."* (rompe la simetría de información → O2/O3)
- *"Lo que ven en el panel de evidencia de red no es una animación: cada fila es un RPC real de
  Photon Fusion que cruzó entre los dos dispositivos."* (O4)
- *"El motor eléctrico está validado analíticamente: nueve pruebas unitarias al 100 %, por
  ejemplo la rama en paralelo dio 6,21 V frente a 6,2 V teóricos."* (PT-C)
- *"Las pruebas de usabilidad con estudiantes están en ejecución con su protocolo e
  instrumentos ya elaborados; es la fase de cierre del objetivo 5."* (honesto sobre PT-A)

## 4. Contingencias (qué hacer si algo falla en vivo)

| Falla | Acción inmediata |
|-------|------------------|
| El Quest no se conecta al PC | Verificar misma Wi-Fi/región; si no, **pasar al video Plan B** sin dramatismo |
| Cybersickness del operador | Tener una segunda persona; o narrar sobre el casting grabado |
| Bug en un reto en vivo | Usar el **DebugLevelSkipper (F1–F4)** para saltar al reto que sí funciona |
| Red caída total | El sistema cae a **modo offline**; demostrar el rol Explorador en solitario + video |
| Tiempo corto | Mostrar **solo Reto 4** (es el más completo y el que cierra el aporte técnico) |

## 5. Preguntas probables del tribunal y respuestas ancla

- **"¿Cómo garantizan que aprende y no solo se entretiene?"** → pre/post-test de conocimiento
  (Anexo C) + métrica M2 (mejora de aciertos Reto 1→Reto 4) + M6 (pre vs. post).
- **"¿Es seguro para los usuarios?"** → SSQ antes/después, criterios de exclusión, seguridad de
  la caminadora (§7.5).
- **"¿Por qué multijugador asimétrico y no individual?"** → matriz ISO/IEC 25010 (Alternativa A,
  4,35 vs 4,05); fuerza comunicación y colaboración (O2, §2.1).
- **"¿Qué quedó pendiente?"** → PT-A con muestra mayor, optimización del cliente, integración
  física de la KAT VR (§9 Trabajo futuro) — responder con transparencia.
- **"¿Cómo manejan los datos de los estudiantes?"** → anónimos y agregados, consentimiento,
  minimización (§7.5).

## 6. Checklist de los 5 minutos previos a entrar

- [ ] PC en Play como Host · Quest con APK abierta · ambos en `PEERS: 2`.
- [ ] Overlay de red visible en las dos pantallas · casting en el proyector.
- [ ] Video Plan B abierto en una pestaña, listo.
- [ ] Componente y línea de código de Arduino ya decididos (`digitalWrite(7, HIGH)`).
- [ ] Respirar: si la red falla, el video Plan B salva la demostración.
