import os
import re
from datetime import datetime

# Configuración de rutas
DOCS_TXT = "documentacion_plana_tesis.txt"
PROGRESS_FILE = "progreso.txt"
README_FILE = "README.md"
REPO_URL = "https://github.com/Proyecto-titulacion-Serious-Game/Serious-Game"
PAGES_URL = "https://proyecto-titulacion-serious-game.github.io/Serious-Game/"

def get_status_ui(status_text):
    s = status_text.lower()
    if "completado" in s: return "✅ Completado"
    if "en pruebas" in s: return "🛠️ En Pruebas"
    if "pendiente" in s: return "📅 Pendiente"
    return "⚪ Desconocido"

def extract_architecture(content):
    # Busca el patrón de clases y sus descripciones cortas en el dump de lynx
    # Ajustado para capturar la estructura típica de Doxygen
    classes = re.findall(r"class\s+(\w+)\s*\n\s+(.*)", content)
    unique_classes = {}
    for name, desc in classes:
        if name not in unique_classes and len(desc.strip()) > 15:
            unique_classes[name] = desc.strip()
        if len(unique_classes) >= 10: break
    return unique_classes

# 1. Cargar Progreso
retos_rows = ""
if os.path.exists(PROGRESS_FILE):
    with open(PROGRESS_FILE, "r", encoding="utf-8") as f:
        for line in f:
            if ":" in line and "|" in line:
                partes = line.split(":")
                nombre = partes[0].strip()
                desc_estado = partes[1].split("|")
                desc = desc_estado[1].strip()
                estado = get_status_ui(desc_estado[0].strip())
                retos_rows += f"| **{nombre}** | {desc} | {estado} |\n"

# 2. Cargar Arquitectura desde la Doc
arch_md = ""
if os.path.exists(DOCS_TXT):
    with open(DOCS_TXT, "r", encoding="utf-8") as f:
        doc_raw = f.read()
    class_map = extract_architecture(doc_raw)
    for name, desc in class_map.items():
        arch_md += f"* **`{name}`**: {desc[:150]}...\n"
else:
    arch_md = "*Pendiente de generación de documentación técnica.*"

# 3. Template Final
template = f"""# 🛠️ Serious Game - Electrónica VR (Tesis UDLA)

![Build](https://img.shields.io/github/actions/workflow/status/Proyecto-titulacion-Serious-Game/Serious-Game/main.yml?branch=main&label=Build%20Status)
![Docs](https://img.shields.io/badge/Docs-Doxygen-blue?logo=read-the-docs)
![Unity](https://img.shields.io/badge/Unity-6000.4.3f1-black?logo=unity)
![Linux](https://img.shields.io/badge/Runner-CachyOS-orange?logo=arch-linux)

## 📖 Descripción General
Simulador asimétrico en Realidad Virtual para la enseñanza de circuitos eléctricos. Desarrollado con **Unity 6**, Meta Quest 3, chalecos hápticos y locomoción KAT VR.

## 🚀 Estado de los Retos de Tesis
| Reto | Descripción | Estado |
| :--- | :--- | :--- |
{retos_rows}

## 🏗️ Arquitectura de Software Detectada
*Resumen de clases core extraído automáticamente:*
{arch_md}

## 🔗 Recursos y Despliegue
* 🌐 **[Documentación Técnica Online]({PAGES_URL})** (Generada por Doxygen)
* 🎮 **[Descargar Ejecutables]({REPO_URL}/actions)** (Artefactos de GitHub Actions)
* 📑 **[Reporte de Cobertura]({PAGES_URL}/inherits.html)** (Jerarquía de clases)

---
> [!IMPORTANT]
> Este archivo se auto-genera en cada Push. Sincronizado con el código fuente y el estado del proyecto.
> **Última actualización:** {datetime.now().strftime("%d/%m/%Y %H:%M:%S")} (Quito, EC)
"""

with open(README_FILE, "w", encoding="utf-8") as f:
    f.write(template)

print("🚀 README.md generado con éxito.")
