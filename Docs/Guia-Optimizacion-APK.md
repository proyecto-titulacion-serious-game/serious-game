# Guía de optimización del APK del Quest (de 2,1 GB → objetivo < 500 MB)

> 2026-06-02. Diagnóstico: el APK del Explorador pesa ~2,1 GB. Causas principales detectadas:
> (1) recorte de código desactivado (`stripEngineCode: 0`, `managedStrippingLevel` vacío);
> (2) texturas PBR sin comprimir/de alta resolución; (3) posibles símbolos de depuración.
> Todas se corrigen en **Player Settings / Build Settings** (Unity abierto). Ordenadas por ROI.

---

## PASO 1 — Recorte de código (mayor impacto, ~cientos de MB)

`Edit → Project Settings → Player → (pestaña Android) → Other Settings`:

- [ ] **Strip Engine Code:** ✅ ON  *(está en OFF — es lo primero)*
- [ ] **Managed Stripping Level:** **High**  *(está vacío/Disabled)*
- [ ] **C++ Compiler Configuration:** **Master**  *(no Debug)*
- [ ] **IL2CPP Code Generation:** **Faster (smaller) builds**
- [ ] **Target Architectures:** solo **ARM64** (verificar que ARMv7 esté **desmarcado** — duplicar arquitecturas duplica el binario)

> El recorte gestionado en High puede romper código que use reflexión (Photon, JSON). Si algo
> falla en runtime tras esto, añade un `link.xml` para preservar los ensamblados afectados
> (Fusion, Newtonsoft) — no bajes el nivel de stripping global.

## PASO 2 — Símbolos de depuración y development build

- [ ] `Project Settings → Player → Publishing Settings → **Debug Symbols: Disabled**`
      *(el build avisó "Diagnostics Data requires Debug Symbols Full" — si no necesitas crash
      reports detallados para la demo, Disabled reduce el tamaño).*
- [ ] `File → Build Settings → **Development Build: OFF**` (verificar el checkbox).
- [ ] `Build Settings → **Compression Method: LZ4HC**` (APK más pequeño que LZ4).
- [ ] `Build Settings → **Build App Bundle (Google Play): OFF**` (para sideload por APK).

## PASO 3 — Compresión de texturas (gran impacto, las texturas dominan el APK)

- [ ] `Project Settings → Player → Android → **Texture compression format: ASTC**`.
- [ ] En el inspector de las texturas más pesadas (el modelo **Meshy del Arduino**, el
      **skybox espacial**, materiales del laboratorio), pestaña Android → **Override for Android**:
  - **Max Size:** bajar de 8192/4096 a **2048** (o **1024** en props pequeños).
  - **Compression:** Normal Quality, formato **ASTC 6x6** (buen balance).
- [ ] Atajo masivo: selecciona varias texturas a la vez y aplica el override en bloque.

> El modelo Meshy AI del Arduino suele traer una textura PBR de 4K–8K. Bajarla a 2K es
> imperceptible en VR y puede ahorrar decenas de MB.

## PASO 4 — Quitar paquetes no usados en el build del Explorador

- [ ] **ReadyPlayerMe:** durante el build hacía llamadas a su API de analytics. Si el Explorador
      **no** usa avatares RPM, considera removerlo del proyecto o excluirlo — añade peso y red.
- [ ] **Skyboxes espaciales (204 MB):** conservar **solo** el que usa el Explorador
      (`SBS Space 3`); el resto puede moverse fuera de `Assets/` (no entran al APK si no se
      referencian, pero limpia el proyecto).
- [ ] **OBJ de Arduino duplicados (51,4 MB × 2, GUID 28cc… con 0 referencias):** sin uso. Es
      *housekeeping* (no reduce el APK porque no se incluye, pero aligera el repo). Verifica que
      no esté referenciado en IntegratedDemo antes de borrar.

## PASO 5 — Medir el resultado real

- [ ] Tras los cambios, **rebuild** y abre el **Build Report**:
      `Window → Analysis → Build Report` (o revisa el `Editor.log`, sección *"Build Report"* /
      *"Uncompressed usage by category"*). Te dirá exactamente qué categoría pesa más
      (Textures, Meshes, Animations, Shaders, Scripts) para iterar con datos.

---

## Expectativa

Con Pasos 1–3 es razonable bajar de **~2,1 GB a < 500 MB** (incluso ~200–300 MB si las
texturas estaban en 4K/8K). El Paso 1 (stripping) suele ser el de mayor retorno en proyectos
con IL2CPP y muchos SDK. **Haz un cambio por vez y mide**, para aislar el impacto y detectar
si el stripping rompe algo por reflexión.

> Recordatorio: el APK solo empaqueta `Explorador.unity` + lo que referencia. La oficina
> japonesa (4,3 GB) **no** entra al APK del Quest (la escena no la referencia) — no pierdas
> tiempo ahí; el peso viene del código sin recortar y las texturas.
