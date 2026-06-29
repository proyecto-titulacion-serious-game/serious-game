# Proyecto TITA — Métricas y Dashboard Docente

**Serious Game de Circuitos Eléctricos en VR (roles asimétricos Técnico/Explorador)**
Documento de referencia del sistema de métricas, su recolección, el panel docente y su uso para el análisis (aprendizaje + KPIs del Capstone).

_Última actualización: 2026-06-29_

---

## 1. Propósito

El sistema de métricas mide **el desempeño del equipo** durante una partida colaborativa y permite:

1. **Evaluación de aprendizaje** (para el profesor): tiempos, errores y aciertos por reto, en vivo y como historial.
2. **KPIs del Capstone** (para el documento UDLA): efectividad, usabilidad y eficiencia del serious game.

Las métricas son **por sesión/equipo**, no por individuo: el juego es colaborativo sobre un mismo circuito (Técnico = PC/código; Explorador = VR/cableado), así que lo que se mide es el resultado conjunto.

---

## 2. Arquitectura del sistema

```
                 ┌─────────────────────── PC TÉCNICO (Host) ───────────────────────┐
                 │                                                                  │
  Explorador     │   GameManager ──► PerformanceTracker ──► ObjectiveSystem         │
  (Quest, VR) ───┼──►  (eventos de red Photon Fusion)         │                     │
   cablea /      │        │                                    ▼                     │
   pide validar  │        │                            SessionDataExporter          │
                 │        ▼                              │  (JSON en disco)          │
                 │   AddError / OnLevelCompleted         ▼                           │
                 │                               DashboardServer  ◄── navegador      │
                 │                               http://localhost:8080/   (profe)    │
                 └──────────────────────────────────────────────────────────────────┘
```

### Componentes (todos en `Assets/Scripts/`)
| Script | Rol |
|---|---|
| `Gameplay/GameManager.cs` | Orquesta los retos. Dispara `OnLevelLoaded`, `OnLevelCompleted`, clasifica errores (`RegisterWrongAttempt`). |
| `Gameplay/PerformanceTracker.cs` | Registra tiempo, errores (por tipo) y resultado **por reto**. |
| `Gameplay/ObjectiveSystem.cs` | Cierra la sesión y emite `OnSessionEnded` con el resultado global. |
| `Networking/SessionDataExporter.cs` | Reúne los datos, los expone (thread-safe) y guarda JSON en disco. |
| `Networking/DashboardServer.cs` | Servidor HTTP embebido: sirve el panel HTML + API JSON/CSV. |
| `Networking/DashboardBootstrap.cs` | **Arranca el panel automáticamente** en el Técnico al dar Play. |

### Multijugador (clave)
- El juego es **host-autoritativo** (Photon Fusion 2). El **Técnico es el Host**.
- Las acciones del **Explorador** (cablear, pedir validación) viajan por red (`GameSession.OnValidacionSolicitada`, `OnCableFixed`, `OnRetoChanged`) y las **procesa el Host**: evalúa, registra errores y completa retos.
- Por eso el panel del Técnico **captura la sesión completa**, incluidos los errores originados por el Explorador.
- El dashboard se sirve **solo en el PC del Técnico**: `http://localhost:8080/` funciona en esa misma máquina. Para verlo desde otra laptop, ver §7.

---

## 3. Catálogo de métricas

### A. Aprendizaje (del juego, automáticas)
| Métrica | Cálculo | Fuente |
|---|---|---|
| Tiempo por reto | `t_fin − t_inicio` | `PerformanceTracker.GetTime()` |
| Errores por reto | conteo de intentos incorrectos | `AddError()` |
| Errores por tipo | desglose por categoría | `GetErrorBreakdown()` |
| Éxito del reto | completado / fallado | `OnLevelCompleted(level, success)` |
| Evaluación por reto | EXCELENTE / BUENO / MEJORAR | `GetEvaluation()` |
| Tiempo total de sesión | suma de los retos | `SessionResult.totalTimeSeconds` |
| Retos completados | 0–4 | historial |
| Score | puntaje del `ObjectiveSystem` | `SessionResult.totalScore` |

**Umbrales de evaluación** (`PerformanceTracker`, ajustables en el Inspector):
- Tiempo "Excelente" por reto: **240 / 300 / 360 / 450 s** (Retos 1–4).
- Máx. errores para "Bueno": **3**.
- `EXCELENTE` = 0 errores y dentro del tiempo · `BUENO` = ≤3 errores · `MEJORAR` = >3 errores.

### B. KPIs del Capstone (requieren encuestas, ver §8)
| KPI | Fórmula | Fuente |
|---|---|---|
| **Ganancia de aprendizaje normalizada** (Hake) | `g = (post% − pre%) / (100 − pre%)` | Pre/Post-test |
| **Usabilidad (SUS)** | escala estándar 10 ítems → 0–100 (≥68 aceptable) | Encuesta SUS |
| **Satisfacción / engagement** | promedio Likert 1–5 | Encuesta |
| **Tasa de finalización** | % de equipos que completan los 4 retos | Eventos del juego |
| **Eficiencia** | tiempo medio total por equipo | Eventos del juego |

---

## 4. Categorías de error (desglose automático)

`GameManager.ClasificarError()` inspecciona el estado del circuito y la falla vigente:

| Categoría | Cuándo |
|---|---|
| **Cortocircuito** | circuito en corto / mensaje de validación "corto" |
| **Polaridad** | LED o capacitor con polaridad invertida |
| **Valor de resistencia** | R incorrecta / fuera de rango |
| **Voltaje de fuente** | fuente con falla |
| **Conexión abierta** / **Conexión/pin** | cable/LED desconectado, pin mal (Reto 4) |
| **Sobrecarga** | corriente excesiva |
| **Selección incorrecta / Procedimiento** | acción del Técnico inválida |

Estas categorías alimentan el desglose "Tipos de error" del panel y la columna `Tipos_error` del CSV (formato `Cortocircuito:2;Polaridad:1`).

---

## 5. Dónde se ven y guardan las métricas

### 5.1 Panel en vivo (navegador)
1. **Play** en el Técnico. En la consola de Unity aparece:
   `[DashboardServer] Panel docente en: http://localhost:8080/`
2. Abrir esa URL en Chrome/Edge (en el mismo PC). Secciones:
   - **Código de acceso**: genera el código de sala para que el grupo se una.
   - **Estado de sesión** y **Sesión en vivo** (ambos roles, reto actual, tiempo, errores por tipo, progreso 0/4) — refresco cada 2 s.
   - **Resultados de sesión** (tabla por reto).
   - **Historial de sesiones** + botones de exportación CSV.

### 5.2 Exportación CSV (para Looker / Sheets / Power BI)
Desde el panel (sección "Historial") o por URL directa:

| Archivo | URL | Contenido |
|---|---|---|
| `retos_tita.csv` | `http://localhost:8080/api/records.csv` | **1 fila por (sesión × reto)** con tipos de error — recomendado para análisis |
| `sesiones_tita.csv` | `http://localhost:8080/api/sessions.csv` | 1 fila por sesión (resumen) |

### 5.3 Archivos JSON en disco (respaldo automático)
Ruta: `Application.persistentDataPath` → en Windows:
`C:\Users\<usuario>\AppData\LocalLow\<Compañía>\<Producto>\`
(la consola imprime la ruta exacta: `[SessionDataExporter] Guardado en: ...`)

| Archivo | Contenido |
|---|---|
| `session_results.json` | última sesión (resumen + registros por reto) |
| `sessions_history.json` | historial completo de sesiones |

### 5.4 API del servidor (referencia)
| Ruta | Método | Devuelve |
|---|---|---|
| `/` | GET | Panel HTML |
| `/api/live` | GET | Estado en vivo (JSON) |
| `/api/results` | GET | Última sesión (JSON) |
| `/api/sessions` | GET | Historial (JSON) |
| `/api/records.csv` | GET | CSV por reto |
| `/api/sessions.csv` | GET | CSV por sesión |
| `/api/status` | GET | Estado actual (JSON) |
| `/api/code` | POST | Genera código de acceso de 4 dígitos |

---

## 6. Esquemas de datos (CSV)

**`retos_tita.csv`** (granular — el principal):
```
Sesion, Fecha, Codigo, Reto, Tiempo_s, Errores, Exito, Evaluacion, Tipos_error
1, 2026-06-29 10:14:02, 4821, Reto 1 — Ley de Ohm, 138, 1, 1, [BUENO] 1 errores 138s, Valor de resistencia:1
1, 2026-06-29 10:14:02, 4821, Reto 4 — Arduino, 402, 3, 1, [BUENO] 3 errores 402s, Cortocircuito:2;Conexión/pin:1
```

**`sesiones_tita.csv`** (resumen):
```
#, Fecha, Codigo, Score, ScoreMax, Porcentaje, Tiempo_s, Errores, Evaluacion
1, 2026-06-29 10:14:02, 4821, 320, 400, 80, 1140, 6, [BUENO]
```

> Nota: `Exito` = 1/0. `Tipos_error` agrupa categorías en una celda (`tipo:conteo;...`); en Sheets puedes separarla con `SPLIT`.

---

## 7. Uso en el aula

### Un grupo
1. Play en el Técnico → abrir `http://localhost:8080/` en ese PC.
2. Generar código → el Explorador se une a la sala con ese código.
3. Jugar → ver métricas en vivo; al **terminar la sesión** se guarda el historial y el CSV por reto.

### Ver el panel desde otra laptop (la del profesor)
En el GameObject `TITA_Dashboard` (creado en runtime) → `DashboardServer`:
- Poner **`localhostOnly = false`**.
- Abrir `http://<IP-del-PC-Técnico>:8080/` desde la otra máquina (misma red Wi-Fi/LAN).
- Si Windows bloquea el puerto: ejecutar Unity como administrador **o**
  `netsh http add urlacl url=http://+:8080/ user=Everyone`

### Varios grupos a la vez
Cada grupo = un Host = **su propio panel en su propio `localhost`** (los códigos de sala evitan que se crucen). Para un **dashboard agregado del aula**:
1. Cada PC-Técnico exporta su `retos_tita.csv`.
2. Se combinan en una hoja de Google Sheets añadiendo una columna **`Grupo`**.
3. Se conecta esa hoja a **Looker Studio**.

---

## 8. Instrumentos de encuesta (para los KPIs del Capstone)

### 8.1 Pre/Post-test de conocimiento (idéntico antes y después)
Sugerido: 8–10 ítems de opción múltiple sobre:
- Ley de Ohm (V = I·R), resistencia limitadora de un LED.
- Polaridad del LED / diodo.
- Lectura de multímetro (V, I, continuidad).
- Circuitos serie vs paralelo.
- PWM / `analogWrite` (Reto 4 Arduino).

Cálculo por estudiante: `pre%`, `post%`, y **ganancia de Hake** `g = (post − pre) / (100 − pre)`.
Interpretación: `g ≥ 0.7` alta · `0.3 ≤ g < 0.7` media · `g < 0.3` baja.

### 8.2 SUS (System Usability Scale) — 10 ítems, Likert 1–5
1. Me gustaría usar este sistema con frecuencia.
2. El sistema es innecesariamente complejo. *(inversa)*
3. El sistema es fácil de usar.
4. Necesitaría apoyo técnico para usarlo. *(inversa)*
5. Las funciones están bien integradas.
6. Hay demasiada inconsistencia. *(inversa)*
7. La mayoría aprendería a usarlo rápido.
8. Es muy engorroso de usar. *(inversa)*
9. Me sentí seguro al usarlo.
10. Tuve que aprender mucho antes de poder usarlo. *(inversa)*

**Puntaje**: ítems impares → `(valor − 1)`; ítems pares → `(5 − valor)`; sumar los 10 y multiplicar por **2.5** → 0–100. **≥68 = usabilidad aceptable.**

### 8.3 Engagement (corto, Likert 1–5)
Inmersión en VR · claridad de los roles · motivación · trabajo en equipo · intención de repetir.

### 8.4 Hoja `encuestas` (para cruzar con el juego)
```
playerId, grupo, rol, preTest, postTest, SUS1..SUS10, eng1..eng5
```
Se une con los CSV del juego por la columna **`grupo`** (o el código de sala).

---

## 9. Construir el dashboard en Looker Studio (recomendado, gratis)
1. Jugar 1+ sesiones completas.
2. Descargar `retos_tita.csv` (y `sesiones_tita.csv`).
3. Subir a **Google Sheets** (una pestaña por archivo + la pestaña `encuestas`).
4. Looker Studio → *Crear → Fuente de datos → Google Sheets*.
5. Visualizaciones sugeridas:
   - **Scorecards**: `g` promedio, SUS promedio, tasa de finalización, tiempo medio/equipo.
   - **Barras**: errores por tipo; errores y tiempo por reto.
   - **Líneas**: curva de mejora (intento vs tiempo/errores).
   - **Tabla**: ranking por equipo (retos, errores, duración, evaluación).
   - **Dispersión**: tiempo de juego vs ganancia de aprendizaje.

---

## 10. Solución de problemas
| Síntoma | Causa / arreglo |
|---|---|
| El panel no abre | ¿Diste Play en el **Técnico**? Revisa la consola: debe imprimir la URL. |
| No hay datos en vivo | El pipeline (`GameManager`/`PerformanceTracker`) corre en la escena del Técnico; entra a un reto para que empiece a registrar. |
| El historial/CSV por reto está vacío | Se llena al **terminar la sesión** (`ObjectiveSystem.OnSessionEnded`); el CSV por reto requiere sesiones finalizadas. |
| No abre desde otra PC | `localhostOnly = false` + permiso de puerto (ver §7). |
| Errores aparecen como "General" | Significa que se llamó `AddError()` sin categoría; los flujos principales ya clasifican (§4). |

---

## 11. Referencia técnica rápida
- **Auto-arranque**: `DashboardBootstrap` (`RuntimeInitializeOnLoadMethod`) crea `TITA_Dashboard` con `SessionDataExporter` + `DashboardServer` solo en el Técnico (no Android, escena "Tecnico"). No requiere tocar la escena.
- **Puerto**: `DashboardServer.port = 8080` (configurable).
- **Persistencia**: JSON en `Application.persistentDataPath`; el historial se acumula entre ejecuciones.
- **Privacidad**: los datos son locales (no se suben a ningún servidor externo); el CSV/JSON los exporta el docente manualmente.
