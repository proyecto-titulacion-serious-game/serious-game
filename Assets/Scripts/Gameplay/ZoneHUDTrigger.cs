using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Muestra/oculta objetivos (HUD holográfico, Clipboard_VR, paneles…) según el RETO activo
/// y/o la POSICIÓN del Explorador. Tú asignas a cada zona su reto y su modo.
///
/// Modos:
///   • PorReto        → aparece cuando GameManager.currentLevel == reto (sin importar dónde estés).
///   • PorPosicion    → aparece cuando la cabeza VR está dentro del Collider (ignora el reto).
///   • PosicionYReto  → requiere ambas.
///
/// El Explorador tiene GameManager local (sincronizado por red), así que 'currentLevel' refleja
/// el reto que cargó el Técnico. Reacciona al instante vía GameManager.OnLevelLoaded.
///
/// VARIAS zonas pueden compartir el mismo objetivo sin pelearse: se usa un contador de DEMANDA
/// (el objetivo está visible si AL MENOS una zona lo pide; se oculta cuando ninguna lo pide).
/// </summary>
[RequireComponent(typeof(Collider))]
public class ZoneHUDTrigger : MonoBehaviour
{
    public enum ActivationMode { PorReto, PorPosicion, PosicionYReto }

    [Header("Cuándo mostrar")]
    [Tooltip("PorReto: según el reto activo. PorPosicion: por estar dentro del volumen. PosicionYReto: ambas.")]
    public ActivationMode modo = ActivationMode.PorReto;

    [Tooltip("Reto asociado a esta zona (para modos PorReto / PosicionYReto).")]
    public LevelType reto = LevelType.OhmLaw;

    [Header("Objetivos a mostrar")]
    [Tooltip("HUD holográfico, Clipboard_VR, paneles… Pueden compartirse entre varias zonas.")]
    public GameObject[] targets;

    [Header("Comportamiento")]
    public bool startHidden = true;

    [Header("Detección de posición (modos con posición)")]
    [Tooltip("Cabeza/cámara VR. Vacío = Camera.main o cámara con 'Explorer' en el nombre.")]
    public Transform playerHead;

    [Header("Eventos (opcional)")]
    public UnityEvent onShow;
    public UnityEvent onHide;

    // Demanda compartida: cuántas zonas piden visible cada objetivo.
    private static readonly Dictionary<GameObject, int> _demand = new();

    private Collider _col;
    private Transform _head;
    private GameManager _gm;
    private bool _shown;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => _demand.Clear();

    void Reset()
    {
        var c = GetComponent<Collider>();
        if (c != null) c.isTrigger = true;
    }

    void Awake()
    {
        _col = GetComponent<Collider>();
        if (_col != null) _col.isTrigger = true;
        if (startHidden) SetActiveDirect(false);   // apaga; la demanda los encenderá si corresponde
    }

    void OnEnable() => GameManager.OnLevelLoaded += HandleLevelLoaded;

    void OnDisable()
    {
        GameManager.OnLevelLoaded -= HandleLevelLoaded;
        if (_shown) { _shown = false; ApplyDemand(false); }   // libera su demanda al desactivarse
    }

    void HandleLevelLoaded(LevelType _) => Evaluate();
    void Start()  => Evaluate();
    void Update() => Evaluate();

    // ─── Lógica de visibilidad ───────────────────────────────────────────
    void Evaluate()
    {
        bool show = modo switch
        {
            ActivationMode.PorReto     => RetoOk(),
            ActivationMode.PorPosicion => InsideZone(),
            _                          => InsideZone() && RetoOk(),
        };

        if (show == _shown) return;
        _shown = show;
        ApplyDemand(show);
        if (show) onShow?.Invoke(); else onHide?.Invoke();
    }

    bool RetoOk()
    {
        if (_gm == null) _gm = FindAnyObjectByType<GameManager>();
        return _gm != null && _gm.currentLevel == reto;
    }

    bool InsideZone()
    {
        var head = ResolveHead();
        if (head == null || _col == null) return false;
        Vector3 p = head.position;
        return (_col.ClosestPoint(p) - p).sqrMagnitude < 1e-6f;
    }

    Transform ResolveHead()
    {
        if (playerHead != null) return playerHead;
        if (_head != null) return _head;
        if (Camera.main != null) { _head = Camera.main.transform; return _head; }
        foreach (var c in FindObjectsByType<Camera>(FindObjectsInactive.Exclude))
            if (c.name.ToLower().Contains("explorer")) { _head = c.transform; return _head; }
        return null;
    }

    // Ajusta el contador de demanda y aplica visibilidad (visible si demanda > 0).
    void ApplyDemand(bool want)
    {
        if (targets == null) return;
        foreach (var t in targets)
        {
            if (t == null) continue;
            int n = _demand.TryGetValue(t, out var v) ? v : 0;
            n = Mathf.Max(0, n + (want ? 1 : -1));
            _demand[t] = n;
            t.SetActive(n > 0);
        }
    }

    void SetActiveDirect(bool on)
    {
        if (targets == null) return;
        foreach (var t in targets)
            if (t != null) t.SetActive(on);
    }

    void OnDrawGizmosSelected()
    {
        if (!TryGetComponent<BoxCollider>(out var b)) return;
        Gizmos.color = new Color(0f, 0.92f, 0.94f, 0.22f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(b.center, b.size);
        Gizmos.color = new Color(0f, 0.92f, 0.94f, 0.9f);
        Gizmos.DrawWireCube(b.center, b.size);
    }
}
