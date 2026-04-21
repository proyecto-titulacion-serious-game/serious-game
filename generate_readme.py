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
    # Regex mejorada para el formato de Lynx/Doxygen
    classes = re.findall(r"class\s+(\w+)\s*\n\s+(.+)", content)
    unique_classes = {}
    for name, desc in classes:
        if name not in unique_classes and len(desc.strip()) > 10:
            clean_desc = desc.replace("More...", "").strip()
            unique_classes[name] = clean_desc
        if len(unique_classes) >= 10: break
    return unique_classes

# 1. Cargar Progreso (con manejo de errores de encoding)
retos_rows = ""
if os.path.exists(PROGRESS_FILE):
    with open(PROGRESS_FILE, "r", encoding="utf-8", errors="replace") as f:
        for line in f:
            if ":" in line and "|" in line:
                partes = line.split(":")
                nombre = partes[0].strip()
                desc_estado = partes[1].split("|")
                desc = desc_estado[1].strip()
                estado = get_status_ui(desc_estado[0].strip())
                retos_rows += f"| **{nombre}** | {desc} | {estado} |\n"

# 2. Cargar Arquitectura (Cambiamos el encoding aquí para evitar el UnicodeDecodeError)
arch_md = ""
if os.path.exists(DOCS_TXT):
    # Probamos con latin-1 que es más permisivo con caracteres latinos
    try:
        with open(DOCS_TXT, "r", encoding="latin-1", errors="replace") as f:
            doc_raw = f.read()
    except Exception:
        with open(DOCS_TXT, "r", encoding="utf-8", errors="replace") as f:
            doc_raw = f.read()
            
    class_map = extract_architecture(doc_raw)
    for name, desc in class_map.items():
        arch_md += f"* **`{name}`**: {desc[:150]}...\n"
else:
    arch_md = "*Pendiente de generación de documentación técnica.*"

# 3. Template Final
template = f"""# 🛠️ Serious Game - Electrónica VR (Tesis UDLA)

![Build Status](https://img.shields.io/github/actions/workflow/status/Proyecto-titulacion-Serious-Game/Serious-Game/main.yml?branch=main)
![Docs](https://img.shields.io/badge/Docs-Doxygen-blue)

## 🚀 Estado de los Retos de Tesis
| Reto | Descripción | Estado |
| :--- | :--- | :--- |
{retos_rows}

## 🏗️ Arquitectura de Software Detectada
{arch_md}

---
> **Última actualización:** {datetime.now().strftime("%d/%m/%Y %H:%M:%S")} (Quito, EC)
"""

with open(README_FILE, "w", encoding="utf-8") as f:
    f.write(template)

print("🚀 README.md generado con éxito.")
