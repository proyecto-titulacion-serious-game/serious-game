using UnityEngine;

/// <summary>
/// Reproduce efectos de sonido para los eventos del circuito.
/// Asigna los AudioClips en el Inspector; cualquier campo vacío se omite silenciosamente.
///
/// Eventos escuchados:
///   CircuitManager.OnCircuitChanged  → sfxComponentInstalled
///   GameManager.OnFaultDetected      → sfxFault
///   GameManager.OnLevelLoaded        → sfxLevelStart
///   GameManager.OnLevelCompleted     → sfxSuccess / sfxFailure
///   GameManager.OnGameCompleted      → sfxVictory
///   CircuitManager.OnShortCircuit    → sfxShortCircuit  (si el evento existe)
///
/// ASSETS RECOMENDADOS (todos gratuitos):
///   • Kenney Interface Sounds   – kenney.nl/assets/interface-sounds     (clicks, beeps UI)
///   • Kenney Sci-Fi Sounds      – kenney.nl/assets/sci-fi-sounds        (futurista/laboratorio)
///   • Kenney Music Jingles      – kenney.nl/assets/music-jingles        (stings de victoria/error)
///   • freesound.org "multimeter beep"  – ID 263133 (CC0)
///   • freesound.org "electric spark"   – ID 253173 (CC0)
///   • freesound.org "capacitor charge" – ID 399095 (CC0)
///   • freesound.org "circuit click"    – ID 220206 (CC0)
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class CircuitAudioManager : MonoBehaviour
{
    [Header("Sonidos de circuito")]
    [Tooltip("Sonido al instalar o cambiar un componente en el circuito.")]
    public AudioClip sfxComponentInstalled;

    [Tooltip("Chispa / zumbido al detectar cortocircuito.")]
    public AudioClip sfxShortCircuit;

    [Tooltip("Alarma al detectar una falla en el circuito.")]
    public AudioClip sfxFault;

    [Header("Sonidos de progreso")]
    [Tooltip("Sting corto al iniciar un nuevo reto.")]
    public AudioClip sfxLevelStart;

    [Tooltip("Jingle de éxito al completar un reto.")]
    public AudioClip sfxSuccess;

    [Tooltip("Sonido de fallo al agotar el tiempo o completar con errores.")]
    public AudioClip sfxFailure;

    [Tooltip("Fanfarria al completar todos los retos.")]
    public AudioClip sfxVictory;

    [Header("Volúmenes")]
    [Range(0f, 1f)] public float volumeCircuit  = 0.7f;
    [Range(0f, 1f)] public float volumeProgress = 0.9f;

    private AudioSource  _src;
    private GameManager  _gm;
    private bool         _shortCircuitPlaying;

    void Awake()
    {
        _src = GetComponent<AudioSource>();
        _src.playOnAwake = false;
        _gm = FindAnyObjectByType<GameManager>();
    }

    void OnEnable()
    {
        CircuitManager.OnCircuitChanged  += OnCircuitChanged;
        GameManager.OnFaultDetected      += OnFaultDetected;
        GameManager.OnLevelLoaded        += OnLevelLoaded;
        GameManager.OnLevelCompleted     += OnLevelCompleted;
        GameManager.OnGameCompleted      += OnGameCompleted;
    }

    void OnDisable()
    {
        CircuitManager.OnCircuitChanged  -= OnCircuitChanged;
        GameManager.OnFaultDetected      -= OnFaultDetected;
        GameManager.OnLevelLoaded        -= OnLevelLoaded;
        GameManager.OnLevelCompleted     -= OnLevelCompleted;
        GameManager.OnGameCompleted      -= OnGameCompleted;
    }

    // ── Handlers ────────────────────────────────────────────────────────

    void OnCircuitChanged()
    {
        bool isShort = _gm?.circuit?.isShortCircuited ?? false;

        if (isShort)
        {
            if (!_shortCircuitPlaying)
            {
                Play(sfxShortCircuit, volumeCircuit);
                _shortCircuitPlaying = true;
            }
        }
        else
        {
            _shortCircuitPlaying = false;
            Play(sfxComponentInstalled, volumeCircuit);
        }
    }

    void OnFaultDetected(string _) => Play(sfxFault, volumeCircuit);

    void OnLevelLoaded(LevelType _) => Play(sfxLevelStart, volumeProgress);

    void OnLevelCompleted(LevelType _, bool success)
        => Play(success ? sfxSuccess : sfxFailure, volumeProgress);

    void OnGameCompleted() => Play(sfxVictory, volumeProgress);

    // ── Helpers ─────────────────────────────────────────────────────────

    void Play(AudioClip clip, float volume)
    {
        if (clip == null || _src == null) return;
        _src.PlayOneShot(clip, volume);
    }
}
