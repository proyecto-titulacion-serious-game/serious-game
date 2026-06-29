# -*- coding: utf-8 -*-
"""
Genera la Figura 7.6 — Arquitectura del Subsistema de Métricas y Panel Docente (TITA).
Mismo estilo C4/matplotlib que generate_c4_diagrams.py. Produce un PNG en Docs/figuras-uml/.

Uso:  python generate_metrics_diagram.py
"""

import matplotlib
matplotlib.use("Agg")
import matplotlib.pyplot as plt
import matplotlib.patches as mpatches
from matplotlib.patches import FancyBboxPatch
import os

OUT_DIR = r"C:\Users\holaq\Proyecto-TITA\Serious-Game\Docs\figuras-uml"
os.makedirs(OUT_DIR, exist_ok=True)

# ── Paleta (consistente con las otras figuras) ────────────────────────────────
C_BOUND_BG   = "#ddeeff"
C_GAME       = "#2e7d32"   # verde  — captura (Gameplay)
C_NET        = "#6a1b9a"   # morado — Networking / exposición
C_STORE      = "#795548"   # marrón — persistencia
C_PRESENT    = "#00838f"   # teal   — consumo / presentación
C_EXT_BG     = "#cccccc"   # gris   — actores / sistemas externos
C_ARROW      = "#555555"


class Box:
    def __init__(self, ax, x, y, w, h, title, subtitle="", desc="",
                 color="#1168bd", text_color="white", lw=1.5):
        self.x, self.y, self.w, self.h = x, y, w, h
        self.cx, self.cy = x + w / 2, y + h / 2
        ax.add_patch(FancyBboxPatch((x, y), w, h, boxstyle="round,pad=0.02",
                     facecolor=color, edgecolor="white", linewidth=lw, zorder=3))
        ty = y + h - 0.16
        ax.text(self.cx, ty, title, ha="center", va="top", fontsize=8.5,
                fontweight="bold", color=text_color, zorder=4, multialignment="center")
        if subtitle:
            ax.text(self.cx, ty - 0.20, f"[{subtitle}]", ha="center", va="top",
                    fontsize=6.5, fontstyle="italic", color=text_color, zorder=4, alpha=0.9)
        if desc:
            dy = ty - (0.40 if subtitle else 0.20)
            ax.text(self.cx, dy, desc, ha="center", va="top", fontsize=6,
                    color=text_color, zorder=4, multialignment="center", linespacing=1.3)

    def top(self):    return (self.cx, self.y + self.h)
    def bottom(self): return (self.cx, self.y)
    def left(self):   return (self.x, self.cy)
    def right(self):  return (self.x + self.w, self.cy)


def arrow(ax, start, end, label="", color=C_ARROW, rad=0.0):
    ax.annotate("", xy=end, xytext=start,
                arrowprops=dict(arrowstyle="->", color=color, lw=1.3,
                                connectionstyle=f"arc3,rad={rad}"), zorder=5)
    if label:
        mx, my = (start[0] + end[0]) / 2, (start[1] + end[1]) / 2
        ax.text(mx, my, label, ha="center", va="center", fontsize=5.6, color=color,
                bbox=dict(boxstyle="round,pad=0.15", fc="white", ec="none", alpha=0.9), zorder=6)


def boundary(ax, x, y, w, h, title, color):
    ax.add_patch(FancyBboxPatch((x, y), w, h, boxstyle="round,pad=0.03",
                 facecolor=C_BOUND_BG, edgecolor=color, linewidth=2,
                 linestyle="--", alpha=0.35, zorder=1))
    ax.text(x + 0.08, y + h - 0.06, title, ha="left", va="top",
            fontsize=7.5, fontweight="bold", color=color, zorder=2)


def patch(color, label):
    return mpatches.Patch(facecolor=color, edgecolor="white", label=label)


# ═════════════════════════════════════════════════════════════════════════════
fig, ax = plt.subplots(figsize=(17, 10))
ax.set_xlim(0, 17)
ax.set_ylim(0, 10)
ax.axis("off")
fig.patch.set_facecolor("white")

ax.text(8.5, 9.7, "Figura 7.6 — Arquitectura del Subsistema de Métricas y Panel Docente",
        ha="center", va="top", fontsize=13, fontweight="bold", color="#222222")
ax.text(8.5, 9.4, "Pipeline por capas: captura → agregación → persistencia → exposición → consumo  ·  Corre en el Host (Técnico)",
        ha="center", va="top", fontsize=9, fontstyle="italic", color="#555555")

# ── Actores externos ─────────────────────────────────────────────────────────
bExpl = Box(ax, 0.3, 4.15, 2.0, 1.1, "Explorador", subtitle="Actor · VR (Quest)",
            desc="Cablea / pide\nvalidación", color=C_EXT_BG, text_color="#222222", lw=1)
bDoc = Box(ax, 14.2, 8.2, 2.0, 0.62, "Docente", subtitle="Actor",
           desc="", color=C_EXT_BG, text_color="#222222", lw=1)

# ── Capa 1: Captura (Gameplay, Host) ─────────────────────────────────────────
boundary(ax, 2.7, 0.7, 3.5, 7.7, "1 · Captura  «Cliente Unity — Host»", C_GAME)
bSession = Box(ax, 2.9, 6.45, 3.1, 1.5, "GameSession", subtitle="NetworkBehaviour · Fusion",
               desc="Recibe acciones del\nExplorador por red.\nEventos OnValidacion...", color=C_NET)
bGM = Box(ax, 2.9, 4.35, 3.1, 1.7, "GameManager", subtitle="Component",
          desc="Orquesta retos.\nClasificaError() →\nCortocircuito/Polaridad...\nOnLevelCompleted", color=C_GAME)
bPerf = Box(ax, 2.9, 2.3, 3.1, 1.7, "PerformanceTracker", subtitle="Component",
            desc="Tiempo + errores\npor tipo y por reto.\nGetAllRecords()", color=C_GAME)

# ── Capa 2: Agregación ───────────────────────────────────────────────────────
boundary(ax, 6.5, 0.7, 3.3, 7.7, "2 · Agregación", C_GAME)
bObj = Box(ax, 6.7, 5.7, 2.9, 1.5, "ObjectiveSystem", subtitle="Component",
           desc="Score + bono.\nOnSessionEnded →\nSessionResult", color=C_GAME)
bExp = Box(ax, 6.7, 2.7, 2.9, 1.9, "SessionDataExporter", subtitle="Singleton",
           desc="Snapshot thread-safe\n(lock + DTOs).\nLive / Results / History", color=C_NET)

# ── Capa 3: Persistencia ─────────────────────────────────────────────────────
boundary(ax, 10.1, 0.7, 2.7, 7.7, "3 · Persistencia", C_STORE)
bJson = Box(ax, 10.3, 4.0, 2.3, 2.2, "Archivos JSON",
            subtitle="persistentDataPath",
            desc="session_results.json\nsessions_history.json\n(respaldo en disco)", color=C_STORE)

# ── Capa 4: Exposición ───────────────────────────────────────────────────────
boundary(ax, 13.0, 0.7, 3.7, 5.5, "4 · Exposición", C_NET)
bServer = Box(ax, 13.8, 2.4, 2.8, 2.3, "DashboardServer", subtitle="HttpListener · :8080",
              desc="Panel HTML + API\n/api/live · /results\n/records.csv\n/sessions.csv", color=C_NET)
bBoot = Box(ax, 13.15, 4.85, 1.7, 1.1, "Dashboard\nBootstrap", subtitle="RuntimeInit",
            desc="Auto-arranca\nsolo Técnico", color=C_NET)

# ── Capa 5: Consumo ──────────────────────────────────────────────────────────
boundary(ax, 13.0, 6.3, 3.7, 2.7, "5 · Consumo", C_PRESENT)
bBrowser = Box(ax, 13.7, 6.55, 3.0, 1.45, "Panel docente", subtitle="Navegador (HTML/JS)",
               desc="En vivo + resultados +\nhistorial + export CSV", color=C_PRESENT)

# Externo: Looker (abajo)
bLooker = Box(ax, 6.7, 0.85, 5.9, 1.05, "Google Sheets  →  Looker Studio / Power BI",
              subtitle="Análisis externo (CSV)",
              desc="Dashboard agregado del aula (cruza con encuestas pre/post + SUS)",
              color=C_EXT_BG, text_color="#222222", lw=1)

# ── Flechas (flujo de datos) ─────────────────────────────────────────────────
arrow(ax, bExpl.right(), bSession.left(), "acciones VR\n(RPC Photon)")
arrow(ax, bSession.bottom(), bGM.top(), "OnValidacionSolicitada\nOnCableFixed")
arrow(ax, bGM.bottom(), bPerf.top(), "AddError(categoría)\nOnLevelCompleted")
arrow(ax, bGM.right(), bObj.left(), "OnLevelCompleted", rad=0.05)
arrow(ax, bPerf.right(), bExp.left(), "GetAllRecords()", rad=-0.1)
arrow(ax, bObj.bottom(), bExp.top(), "OnSessionEnded")
arrow(ax, bExp.right(), bJson.left(), "escribe JSON")
arrow(ax, bJson.right(), bServer.left(), "lee snapshot", rad=0.05)
arrow(ax, bBoot.bottom(), bServer.top(), "crea y arranca")
arrow(ax, bServer.top(), bBrowser.bottom(), "HTTP :8080\nHTML / JSON / CSV")
arrow(ax, bDoc.bottom(), bBrowser.top(), "abre / lee")
arrow(ax, bBrowser.left(), bLooker.right(), "descarga CSV", color=C_PRESENT, rad=-0.25)

# ── Leyenda ──────────────────────────────────────────────────────────────────
ax.legend(handles=[
    patch(C_GAME,    "Captura / Agregación (Gameplay)"),
    patch(C_NET,     "Networking / Exposición"),
    patch(C_STORE,   "Persistencia (disco)"),
    patch(C_PRESENT, "Consumo / Presentación"),
    patch(C_EXT_BG,  "Actor / Sistema externo"),
], loc="lower right", fontsize=7.5, framealpha=0.9, bbox_to_anchor=(1.0, 0.0))

plt.tight_layout(rect=[0, 0, 1, 0.97])
out = os.path.join(OUT_DIR, "fig76_arquitectura_metricas.png")
plt.savefig(out, dpi=180, bbox_inches="tight", facecolor="white")
plt.close()
print(f"Figura 7.6 guardada: {out}")
