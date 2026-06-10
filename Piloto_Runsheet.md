# Runsheet — Sesión Piloto PT-A TITA
**Participantes:** Ricardo (facilitador) + Mauro (o 1 pareja voluntaria)
**Duración estimada:** 60–75 min | **Objetivo:** verificar flujo E2E y calibrar tiempos

---

## T-1 día: Preparación

- [ ] Instalar APK en Quest 3:
  ```
  adb install "C:\Users\holaq\Proyecto-TITA\Serious-Game\Explorador\Explorador.apk"
  ```
- [ ] Probar `Serious-Game\Build-Tecnico\Tecnico.exe` abre sin errores
- [ ] Abrir Meta Horizon Link → Quest conectado via cable/Air Link
- [ ] Meta Horizon Link → Settings → General → **Set Meta Quest Link as Active OpenXR Runtime**
- [ ] Verificar red LAN: PC Técnico y Quest en la misma red (o misma máquina)
- [ ] Imprimir o tener en móvil los forms:
  - [B Demográfico](https://docs.google.com/forms/d/15bmpoNOV5LuvxVhDL8AGJx30HESSgGwVFUketuKcMbc/edit) (vista previa)
  - [C Pre-test](https://docs.google.com/forms/d/1QUZDk85Q2-3kC89RDpWWgclApO92Jq3i0xyHBCQWktk/edit)
  - [C' Post-test](https://docs.google.com/forms/d/1xn-dEN3GYSAZbwKakN93Vz4-JYwn68Q6QLIDNkZsHtM/edit)
  - [E SUS](https://docs.google.com/forms/d/1I-xseRVyaWrj87ZLfvq67kGIlgLNGgQKkSdFK12890s/edit)
  - [E' Likert](https://docs.google.com/forms/d/1AX3MfgUWiGoRqS30zTiB2yINvzxZQtvKg4AR6EYe23o/edit)
  - [F SSQ Antes](https://docs.google.com/forms/d/143-hMG6un3RCNzklll-V1Wz7ZKny3I2F4W5h4oOHer4/edit)
  - [F SSQ Después](https://docs.google.com/forms/d/1K3wNP-08AX4E7o4zT8LvdzGD_wtBNJFuZ6aGJ0zPyBQ/edit)

---

## Smoke test E2E (sin participante, ~15 min)

Ejecutar ANTES de que llegue la pareja piloto.

| # | Acción | Resultado esperado |
|---|--------|--------------------|
| 1 | Lanzar `Tecnico.exe` | Carga escena, no crash |
| 2 | Ponerse Quest, lanzar APK | XR Origin activo, manos visibles |
| 3 | Técnico conecta a sala "LaboratorioUbicua" | Ambos en sala, sin timeout |
| 4 | Técnico envía un Resistor al Explorador | Componente aparece en bandeja VR |
| 5 | Explorador agarra componente y lo instala en slot | Slot confirma instalación |
| 6 | Técnico avanza al Reto 2 (F2 en debug o botón) | Zona 2 activa |
| 7 | Completar Reto 1 completo | LED verde, pantalla victoria |
| 8 | Probar Reto 4: Técnico sube código Arduino | Telemetría actualiza en tiempo real |

Si falla el paso 3 → verificar `ConnectionManager.modoOffline = false` en la escena.
Si falla el paso 4 → revisar `ComponentSendingTray` (Workstream A2 pendiente de ejecutar en Unity).

---

## Sesión piloto — guion por fases

### Fase 0 — Setup (5 min)
- Asignar códigos: P01 = rol Explorador, P02 = rol Técnico
- Sentar al Técnico frente al PC con `Tecnico.exe` ya abierto en menú
- Ayudar al Explorador a ponerse el Quest; confirmar imagen en visor

### Fase 1 — Instrumentos previos (10 min)
- **[Form B]** Demográfico — cada uno en su móvil o PC separado
- **[Form C]** Pre-test — responder sin ayuda; anotar hora de inicio y fin
- **[Form F inicio]** SSQ antes de sesión

### Fase 2 — Juego (25–35 min)
- Arrancar partida: Técnico hace Host, Explorador se une
- **No dar instrucciones de mecánica** — observar si comprenden el rol solos (M1)
- Registrar en papel por reto: tiempo, intentos incorrectos, errores de UI, pedidos de ayuda
- Si un reto supera el tiempo límite + 50% → apuntar y continuar con el siguiente (F3 debug)

| Reto | Límite oficial | Límite piloto (con margen) |
|------|---------------|---------------------------|
| 1 Serie | 8 min | 12 min |
| 2 Paralelo | 10 min | 15 min |
| 3 Mixto | 12 min | 18 min |
| 4 Arduino | 15 min | 22 min |

### Fase 3 — Instrumentos posteriores (10 min)
- **[Form C']** Post-test
- **[Form E]** SUS
- **[Form E']** Likert satisfacción
- **[Form F final]** SSQ después de sesión

### Fase 4 — Entrevista breve (5 min)
Preguntas del Anexo G (de memoria o en papel):
1. ¿Qué fue lo más fácil y lo más difícil de tu rol?
2. ¿Hubo algún momento en que no supiste qué hacer o el sistema no respondió?
3. ¿Qué cambiarías?
4. ¿Sientes que aprendiste algo de electrónica?

---

## Qué registrar/ajustar tras el piloto

| Observación | Acción |
|-------------|--------|
| Tiempo total real por reto | Ajustar límites en protocolo si necesario |
| Preguntas confusas en el test | Reescribir antes de sesiones formales |
| Errores de UI recurrentes | Abrir bug en Unity y parchear build |
| Forms no cargaron bien en móvil | Verificar URLs públicas y QR |
| Reto 4 no completable | Activar `modoOffline + DebugLevelSkipper F4` y documentar |

---

## Post-piloto: rebuild si se parchean bugs

```
# En Unity: File → Build Settings → Build
# Técnico: plataforma PC, escena Tecnico.unity
# Explorador: plataforma Android, escena Explorador.unity → Build APK
# Luego: adb install -r Explorador.apk
```
