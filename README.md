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
* **`ArduinoPin`**: Simula un pin del Arduino para el Reto 4. Permite fallas: pin...
* **`Capacitor`**: Capacitor electrolítico para el Reto 3. Simula fallo por polaridad...
* **`CircuitManager`**: Simula el circuito eléctrico para los 4 retos del Serious Game....
* **`LED`**: LED con simulación educativa: muestra verde (correcto), rojo...
* **`PerformanceTracker`**: Registra el desempeño del jugador por reto: tiempo empleado, errores...
* **`Resistor`**: Resistencia con soporte de código de colores (Reto 3). Permite...
* **`ResistorColorCode`**: Utilidad para calcular el código de colores de una resistencia....
* **`ComponentDeliverySystem`**: Sistema de entrega asimétrica de componentes....
* **`DiagnosticSystem`**: Motor de diagnóstico del circuito para el panel del Técnico. Clase...
* **`GameManager`**: Controlador principal del juego. Gestiona los 4 retos del Serious...
* **`InstructionSystem`**: Sistema de pasos e instrucciones del reto activo. Valida...
* **`ObjectiveSystem`**: Gestiona los objetivos específicos de cada reto y el puntaje final....


## 🔗 Recursos y Despliegue
* 🌐 **[Documentación Técnica Online](https://proyecto-titulacion-serious-game.github.io/Serious-Game/)** (Generada por Doxygen)
* 🎮 **[Descargar Ejecutables](https://github.com/Proyecto-titulacion-Serious-Game/Serious-Game/actions)** (Sección de Artifacts)
* 📑 **[Jerarquía de Clases](https://proyecto-titulacion-serious-game.github.io/Serious-Game//inherits.html)** (Reporte visual)

---
> [!IMPORTANT]
> Este archivo se auto-genera en cada Push. Sincronizado con el código fuente y el estado del proyecto.
> **Última actualización:** 21/04/2026 01:02:26 (Quito, EC)
