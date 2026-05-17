using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Manual técnico físico sobre la mesa del Técnico.
/// Muestra las páginas del manual en un Canvas World Space sobre el objeto "libro".
///
/// SETUP en Unity:
///   1. Crear un Cube "Manual_Book" (Scale X=0.4, Y=0.01, Z=0.3) sobre la mesa
///   2. Crear hijo: Canvas (World Space, Scale=0.001, Width=400, Height=300)
///   3. Agregar este script al Canvas hijo
///   4. Agregar Botones Anterior/Siguiente al Canvas
///   5. Agregar BoxCollider al Manual_Book para detección de click/hover
/// </summary>
public class TechnicianManualDisplay : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────

    [Header("Referencias")]
    public GameManager     gameManager;
    public TechnicianManual manual;

    [Header("Textos del manual (TMPs en el Canvas del libro)")]
    public TMP_Text txtTitulo;
    public TMP_Text txtPaginaIzquierda;   // concepto + fórmulas
    public TMP_Text txtPaginaDerecha;     // tabla de valores + objetivo

    [Header("Navegación de páginas")]
    public Button btnPaginaAnterior;
    public Button btnPaginaSiguiente;
    public TMP_Text txtNumeroPagina;      // "Página 1 de 3"

    [Header("Imagen del diagrama (opcional)")]
    public Image   imgDiagrama;
    public Sprite[] diagramas;            // sprites por reto

    // ─────────────────────────────────────────────
    //  Páginas del manual
    // ─────────────────────────────────────────────

    /// <summary>Cada página tiene contenido izquierdo y derecho.</summary>
    private struct Pagina
    {
        public string izquierda;
        public string derecha;
    }

    private Pagina[] _paginas;
    private int      _paginaActual = 0;

    // ─────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────

    void Start()
    {
        // Auto-buscar referencias no asignadas en el Inspector
        if (gameManager == null) gameManager = FindAnyObjectByType<GameManager>();
        if (manual      == null) manual      = FindAnyObjectByType<TechnicianManual>();

        if (gameManager == null)
            Debug.LogWarning("[TechnicianManualDisplay] GameManager no encontrado. " +
                             "Asígnalo en el Inspector de TechnicianManualDisplay.");
        if (manual == null)
            Debug.LogWarning("[TechnicianManualDisplay] TechnicianManual no encontrado. " +
                             "Asígnalo en el Inspector de TechnicianManualDisplay.");

        if (btnPaginaAnterior  != null) btnPaginaAnterior.onClick.AddListener(PaginaAnterior);
        if (btnPaginaSiguiente != null) btnPaginaSiguiente.onClick.AddListener(PaginaSiguiente);

        GameManager.OnLevelLoaded += OnLevelLoaded;
        BuildPages();
    }

    void OnDestroy()
    {
        GameManager.OnLevelLoaded -= OnLevelLoaded;
    }

    // ─────────────────────────────────────────────
    //  Construcción de páginas
    // ─────────────────────────────────────────────

    void OnLevelLoaded(LevelType level)
    {
        _paginaActual = 0;
        BuildPages();
    }

    /// <summary>Construye las páginas del manual para el reto activo.</summary>
    void BuildPages()
    {
        if (manual == null || gameManager == null) return;

        var data = manual.GetManualData(gameManager.currentLevel);

        // Dividir el contenido en 3 páginas
        _paginas = new Pagina[]
        {
            // Página 1: Concepto
            new Pagina
            {
                izquierda = data.titulo + "\n\n" + data.concepto,
                derecha   = "FORMULAS:\n\n" + data.formula
            },
            // Página 2: Objetivo y pasos
            new Pagina
            {
                izquierda = "OBJETIVO:\n\n" + data.objetivo,
                derecha   = "TABLA DE REFERENCIA:\n\n" + data.tablaValores
            },
            // Página 3: Valores de componentes correctos
            new Pagina
            {
                izquierda = BuildComponentValues(),
                derecha   = BuildColorCodes()
            }
        };

        MostrarPagina(_paginaActual);
    }

    /// <summary>Muestra la página actual en los TMPs del libro.</summary>
    void MostrarPagina(int index)
    {
        if (_paginas == null || index < 0 || index >= _paginas.Length) return;

        var p = _paginas[index];
        Set(txtPaginaIzquierda, p.izquierda);
        Set(txtPaginaDerecha,   p.derecha);
        Set(txtNumeroPagina,    $"Pag {index + 1} / {_paginas.Length}");

        // Diagrama en página 2
        if (imgDiagrama != null && diagramas != null)
        {
            int idx = (int)(gameManager?.currentLevel ?? 0);
            if (index == 1 && idx < diagramas.Length && diagramas[idx] != null)
                imgDiagrama.sprite = diagramas[idx];
        }

        // Botones de navegación
        if (btnPaginaAnterior  != null) btnPaginaAnterior.interactable  = index > 0;
        if (btnPaginaSiguiente != null) btnPaginaSiguiente.interactable = index < _paginas.Length - 1;
    }

    // ─────────────────────────────────────────────
    //  Navegación
    // ─────────────────────────────────────────────

    public void PaginaSiguiente()
    {
        if (_paginas == null) return;
        if (_paginaActual < _paginas.Length - 1)
        {
            _paginaActual++;
            MostrarPagina(_paginaActual);
        }
    }

    public void PaginaAnterior()
    {
        if (_paginaActual > 0)
        {
            _paginaActual--;
            MostrarPagina(_paginaActual);
        }
    }

    /// <summary>Ir directamente a una página específica.</summary>
    public void IrAPagina(int index) => MostrarPagina(_paginaActual = index);

    // ─────────────────────────────────────────────
    //  Contenido adicional
    // ─────────────────────────────────────────────

    string BuildComponentValues()
    {
        if (gameManager == null) return "—";

        return gameManager.currentLevel switch
        {
            LevelType.OhmLaw   => "VALORES DEL RETO 1:\n\nFuente: 9V\nR correcta: 100 Ohm\nLED R interna: 50 Ohm\nI objetivo: 10-20 mA",
            LevelType.Parallel => "VALORES DEL RETO 2:\n\nFuente: 9V\nR normal por rama: 50 Ohm\nRama rota: 9999 Ohm\nI por rama: 180 mA",
            LevelType.Mixed    => "VALORES DEL RETO 3:\n\nR serie incorrecta: 470 Ohm\nR correcta: 220 Ohm\nLED: polaridad invertida\nCap: polaridad invertida",
            LevelType.Arduino  => "VALORES DEL RETO 4:\n\nFuente: 5V\nPin sensor: D2 (correcto)\nPin actual: D4 (incorrecto)\nR buzzer: 330 Ohm",
            _ => "—"
        };
    }

    string BuildColorCodes() =>
        "CODIGO DE COLORES:\n\n" +
        "Negro=0   Marron=1  Rojo=2\n" +
        "Naranja=3 Amarillo=4 Verde=5\n" +
        "Azul=6    Violeta=7  Gris=8\n" +
        "Blanco=9\n\n" +
        "Tolerancia: Oro=5% Plata=10%\n\n" +
        "100 Ohm = Marron-Negro-Marron-Oro\n" +
        "220 Ohm = Rojo-Rojo-Marron-Oro\n" +
        "330 Ohm = Naranja-Naranja-Marron-Oro\n" +
        "470 Ohm = Amarillo-Violeta-Marron-Oro";

    void Set(TMP_Text t, string s) { if (t != null) t.text = s; }
}