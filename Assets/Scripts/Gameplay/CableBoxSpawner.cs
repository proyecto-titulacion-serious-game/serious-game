using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// Dispensador industrial de cables jumper para la mesa VR del Explorador.
/// Máquina de estados visual: Idle → Active → Warning → Full.
[RequireComponent(typeof(Collider))]
public class CableBoxSpawner : MonoBehaviour
{
    // ─────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────
    [Header("Cable")]
    public GameObject cablePrefab;
    public Vector3    spawnOffset      = new Vector3(0f, 0.07f, 0f);
    [Range(5, 50)]
    public int        maxActiveCables  = 20;

    [Header("Referencias visuales (asignadas por el tool)")]
    public Renderer  ledRenderer;
    public Renderer  buttonRenderer;
    public Transform buttonCap;
    public Transform gatePlate;
    public TMP_Text  counterText;

    [Header("Animación")]
    public float buttonPressDepth = 0.006f;
    public float buttonAnimTime   = 0.10f;
    public float gateAnimTime     = 0.35f;

    // ─────────────────────────────────────────
    //  Paleta de colores (cuerpo, puntaA, puntaB)
    // ─────────────────────────────────────────
    static readonly (Color body, Color probeA, Color probeB)[] Palettes =
    {
        (Hex("E63946"), Hex("FFD60A"), Hex("4361EE")),
        (Hex("2EC4B6"), Hex("FF9F1C"), Hex("CBFF8C")),
        (Hex("8338EC"), Hex("FB5607"), Hex("FFBE0B")),
        (Hex("06D6A0"), Hex("EF233C"), Hex("4895EF")),
        (Hex("F72585"), Hex("4CC9F0"), Hex("7209B7")),
        (Hex("FFB703"), Hex("023047"), Hex("FB8500")),
        (Hex("118AB2"), Hex("EF476F"), Hex("FFD166")),
        (Hex("43AA8B"), Hex("F94144"), Hex("F3722C")),
    };

    enum BoxState { Idle, Active, Warning, Full }

    static readonly Color LedGreen = new Color(0f,    0.78f, 0.39f);
    static readonly Color LedAmber = new Color(0.91f, 0.51f, 0f);
    static readonly Color LedRed   = new Color(0.80f, 0.13f, 0.13f);

    BoxState _state;
    int      _activeCables;
    int      _paletteIdx;
    bool     _gateOpen = true;

    MaterialPropertyBlock _mpbLed;
    MaterialPropertyBlock _mpbButton;

    Coroutine _buttonCo;
    Coroutine _gateCo;
    Vector3   _buttonRestPos;
    Vector3   _gateOpenPos;
    Vector3   _gateClosedPos;

    void Awake()
    {
        _mpbLed    = new MaterialPropertyBlock();
        _mpbButton = new MaterialPropertyBlock();

        GetComponent<Collider>().isTrigger = true;

        var xr = GetComponent<XRBaseInteractable>();
        if (xr != null)
        {
            xr.selectEntered.AddListener(_ => SpawnCable());
            xr.hoverEntered.AddListener(_ => OnHoverEnter());
            xr.hoverExited.AddListener(_  => OnHoverExit());
        }

        if (buttonCap != null) _buttonRestPos = buttonCap.localPosition;
        if (gatePlate  != null)
        {
            _gateOpenPos   = gatePlate.localPosition;
            _gateClosedPos = _gateOpenPos + new Vector3(0f, -0.015f, 0f);
        }
    }

    void Start()
    {
        UpdateState();
        StartCoroutine(PulseLED());
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("RightHand") || other.CompareTag("LeftHand"))
            SpawnCable();
    }

    void OnHoverEnter() => SetButtonEmission(LedGreen * 3.5f);
    void OnHoverExit()  => UpdateVisuals();

    public void SpawnCable()
    {
        if (cablePrefab == null) { Debug.LogWarning("[CableBoxSpawner] cablePrefab no asignado."); return; }
        if (_state == BoxState.Full) return;

        if (_buttonCo != null) StopCoroutine(_buttonCo);
        _buttonCo = StartCoroutine(AnimateButtonPress());

        Vector3 pos   = transform.position + transform.TransformDirection(spawnOffset);
        GameObject go = Instantiate(cablePrefab, pos, Random.rotation);

        ApplyCableColors(go, _paletteIdx);
        _paletteIdx = (_paletteIdx + 1) % Palettes.Length;

        go.transform.localScale = Vector3.zero;
        StartCoroutine(ScaleIn(go.transform, 0.12f));

        _activeCables++;
        var tracker = go.AddComponent<CableTracker>();
        tracker.spawner = this;

        UpdateState();
    }

    internal void OnCableDestroyed()
    {
        _activeCables = Mathf.Max(0, _activeCables - 1);
        UpdateState();
        if (_state != BoxState.Full && !_gateOpen) StartGateAnim(true);
    }

    void UpdateState()
    {
        float ratio = maxActiveCables > 0 ? (float)_activeCables / maxActiveCables : 0f;
        BoxState next = ratio >= 1f    ? BoxState.Full
                      : ratio >= 0.75f ? BoxState.Warning
                      : ratio > 0f    ? BoxState.Active
                      :                 BoxState.Idle;

        bool shouldOpen = next != BoxState.Full;
        if (next != _state || shouldOpen != _gateOpen)
        {
            _state = next;
            if (shouldOpen != _gateOpen) StartGateAnim(shouldOpen);
        }

        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        if (counterText != null)
            counterText.text = $"{_activeCables}/{maxActiveCables}";

        Color led = _state switch
        {
            BoxState.Warning => LedAmber,
            BoxState.Full    => LedRed,
            _                => LedGreen,
        };
        SetLedEmission(led * 1.5f);

        Color btn = _state == BoxState.Full ? LedRed * 1.5f : LedGreen * 1.2f;
        SetButtonEmission(btn);
    }

    IEnumerator PulseLED()
    {
        float t = 0f;
        while (true)
        {
            if (_state == BoxState.Idle)
            {
                t += Time.deltaTime * 0.5f;
                float i = Mathf.Lerp(0.5f, 1.8f, (Mathf.Sin(t * Mathf.PI * 2f) + 1f) * 0.5f);
                SetLedEmission(LedGreen * i);
            }
            else if (_state == BoxState.Warning)
            {
                SetLedEmission(LedAmber * 2.2f);
                yield return new WaitForSeconds(0.28f);
                SetLedEmission(LedAmber * 0.4f);
                yield return new WaitForSeconds(0.28f);
                continue;
            }
            yield return null;
        }
    }

    IEnumerator AnimateButtonPress()
    {
        if (buttonCap == null) yield break;
        var pressed = _buttonRestPos + new Vector3(0f, 0f, buttonPressDepth);
        yield return MoveTo(buttonCap, _buttonRestPos, pressed, buttonAnimTime * 0.35f);
        yield return MoveTo(buttonCap, pressed, _buttonRestPos, buttonAnimTime * 0.65f);
    }

    void StartGateAnim(bool open)
    {
        _gateOpen = open;
        if (_gateCo != null) StopCoroutine(_gateCo);
        if (gatePlate != null)
            _gateCo = StartCoroutine(MoveTo(gatePlate, gatePlate.localPosition,
                open ? _gateOpenPos : _gateClosedPos, gateAnimTime));
    }

    static IEnumerator MoveTo(Transform t, Vector3 from, Vector3 to, float dur)
    {
        for (float e = 0f; e < dur; e += Time.deltaTime)
        {
            if (t == null) yield break; // CORRECCIÓN: Previene MissingReferenceException
            float n = Mathf.Clamp01(e / dur);
            float s = n < 0.5f ? 4f * n * n * n : 1f - Mathf.Pow(-2f * n + 2f, 3f) / 2f;
            t.localPosition = Vector3.Lerp(from, to, s);
            yield return null;
        }
        if (t != null) t.localPosition = to;
    }

    static IEnumerator ScaleIn(Transform t, float dur)
    {
        for (float e = 0f; e < dur; e += Time.deltaTime)
        {
            if (t == null) yield break; // CORRECCIÓN: Salva el sistema si el cable muere antes de terminar de crecer
            float n = Mathf.Clamp01(e / dur);
            t.localScale = Vector3.one * (1f - Mathf.Pow(1f - n, 3f));
            yield return null;
        }
        if (t != null) t.localScale = Vector3.one;
    }

    void SetLedEmission(Color c)
    {
        if (ledRenderer == null) return;
        _mpbLed.SetColor("_BaseColor",     c * 0.6f);
        _mpbLed.SetColor("_EmissionColor", c);
        ledRenderer.SetPropertyBlock(_mpbLed);
    }

    void SetButtonEmission(Color c)
    {
        if (buttonRenderer == null) return;
        _mpbButton.SetColor("_BaseColor",     c * 0.5f);
        _mpbButton.SetColor("_EmissionColor", c);
        buttonRenderer.SetPropertyBlock(_mpbButton);
    }

    static void ApplyCableColors(GameObject go, int idx)
    {
        var p = Palettes[idx % Palettes.Length];

        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var mpb = new MaterialPropertyBlock();   // bloque fresco por renderer — evita herencia de propiedades
            if (r.name.Contains("Cable_Body"))
            {
                mpb.SetColor("_BaseColor",     p.body);
                mpb.SetColor("_EmissionColor", p.body * 0.1f);
                r.SetPropertyBlock(mpb);
            }
            else if (r.name.Contains("Probe_A"))
            {
                mpb.SetColor("_BaseColor", p.probeA);
                r.SetPropertyBlock(mpb);
            }
            else if (r.name.Contains("Probe_B"))
            {
                mpb.SetColor("_BaseColor", p.probeB);
                r.SetPropertyBlock(mpb);
            }
        }
    }

    static Color Hex(string h) { ColorUtility.TryParseHtmlString("#" + h, out var c); return c; }
}

public class CableTracker : MonoBehaviour
{
    [HideInInspector] public CableBoxSpawner spawner;
    void OnDestroy() => spawner?.OnCableDestroyed();
}