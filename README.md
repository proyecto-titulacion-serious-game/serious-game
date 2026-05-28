# 🛠️ Serious Game - Electrónica VR (Tesis UDLA)

![Build](https://img.shields.io/github/actions/workflow/status/Proyecto-titulacion-Serious-Game/Serious-Game/main.yml?branch=main&label=Build%20Status)
![Docs](https://img.shields.io/badge/Docs-Doxygen-blue?logo=read-the-docs)
![Unity](https://img.shields.io/badge/Unity-6000.4.3f1-black?logo=unity)
![Linux](https://img.shields.io/badge/Runner-CachyOS-orange?logo=arch-linux)

## 📖 Descripción General
Simulador asimétrico en Realidad Virtual para la enseñanza de circuitos eléctricos. Desarrollado con **Unity 6**, Meta Quest 3, chalecos hápticos y locomoción KAT VR para una experiencia inmersiva completa.

## 🚀 Estado de los Retos de Tesis
| Reto | Descripción | Estado |
| :--- | :--- | :--- |
| **Reto 1** | Ley de Ohm y Circuitos Simples en VR. | ✅ Completado |
| **Reto 2** | Configuraciones Serie/Paralelo con multímetro. | ✅ Completado |
| **Reto 3** | Carga y descarga de capacitores con feedback háptico. | 🛠️ En Pruebas |
| **Reto 4** | Integración de lógica de control con Arduino virtual. | 📅 Pendiente |


## 🏗️ Arquitectura de Software Detectada
*Resumen de clases core extraído automáticamente desde la documentación técnica:*
* **`MouseGrabSimulator`**: Simula la mano VR del Explorador usando el mouse. Permite agarrar,...
* **`SceneLoader`**: Punto central para cambiar de escena. Usar desde botones UI con...
* **`TecnicoBootstrapper`**: Carga NoonA.unity de forma aditiva cuando el build del Técnico...
* **`XRDiagnostics`**: Attach to any GameObject in the scene. Logs XR initialization state,...
* **`ClipboardZoom`**: Adjunta al clipboard del Técnico. Click -> el clipboard se acerca a...
* **`ComponentSendingTray`**: Bandeja de envío sobre la mesa del Técnico....
* **`DeskComponent`**: Componente físico sobre la mesa del Técnico. PC: hover (mouse) +...
* **`FlexWire`**: Cable flexible editable en Scene View. Genera un tubo Catmull-Rom a...
* **`ManualBookOpener`**: Click en el libro físico sobre la mesa para abrir el manual a...
* **`ManualScroll`**: Manual del Técnico en forma de pergamino enrollado....
* **`PCMonitorInteract`**: Añade al Monitor del PC de escritorio la misma mecánica que el...
* **`TechnicianManualDisplay`**: Manual técnico físico sobre la mesa del Técnico. Muestra las páginas...


## 🔗 Recursos y Despliegue
* 🌐 **[Documentación Técnica Online](https://proyecto-titulacion-serious-game.github.io/Serious-Game/)** (Generada por Doxygen)
* 🎮 **[Descargar Ejecutables](https://github.com/Proyecto-titulacion-Serious-Game/Serious-Game/actions)** (Sección de Artifacts)
* 📑 **[Jerarquía de Clases](https://proyecto-titulacion-serious-game.github.io/Serious-Game//inherits.html)** (Reporte visual)

---
> [!IMPORTANT]
> Este archivo se auto-genera en cada Push. Sincronizado con el código fuente y el estado del proyecto.
> **Última actualización:** 28/05/2026 04:21:00 (Quito, EC)
