using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Salto rápido de retos con <b>F1-F4</b> en Play Mode (ayuda de prueba, dev-only).
///   F1 → Reto 1 · F2 → Reto 2 · F3 → Reto 3 · F4 → Reto 4
///
/// Auto-bootstrap: se crea solo en Editor / Development Build, en CUALQUIER escena (incluida
/// Tecnico.unity, que no tiene GameManager local — ahí el salto viaja por red al Host). Antes el
/// script solo existía en Explorador.unity y su Update abortaba si no había GameManager, así que
/// F1-F4 NO funcionaban desde el PC del Técnico. NO entra en builds de release.
/// </summary>
public class DebugLevelSkipper : MonoBehaviour
{
    static DebugLevelSkipper _instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (_instance != null) return;
        var go = new GameObject("[DebugLevelSkipper]");
        _instance = go.AddComponent<DebugLevelSkipper>();
        DontDestroyOnLoad(go);
#endif
    }

    private GameManager _gameManager;

    // Si el script también está colocado a mano en una escena, evita duplicar con el bootstrap.
    void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
    }

    /// <summary>Localiza el GameManager (si lo hay en esta escena) y le fuerza _debugMode = true.</summary>
    void EnsureGameManager()
    {
        if (_gameManager == null)
            _gameManager = FindAnyObjectByType<GameManager>();

        if (_gameManager != null)
        {
            var field = typeof(GameManager).GetField("_debugMode",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field != null && !(bool)field.GetValue(_gameManager))
                field.SetValue(_gameManager, true);
        }
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if      (kb.f1Key.wasPressedThisFrame) JumpTo(0);
        else if (kb.f2Key.wasPressedThisFrame) JumpTo(1);
        else if (kb.f3Key.wasPressedThisFrame) JumpTo(2);
        else if (kb.f4Key.wasPressedThisFrame) JumpTo(3);
    }

    private void JumpTo(int index)
    {
        EnsureGameManager();

        var gs = GameSession.Instance;
        bool enRed = gs != null && gs.Object != null && gs.Object.IsValid;

        // Caso 1 — en red y NO somos el Host (Explorador cliente, o Técnico sin GameManager local):
        // pedimos el cambio al Host. Él aplica AvanzarReto y lo propaga a todos por RPC_CambiarReto.
        // (Si lo hiciéramos local, el Host nos revertiría en el siguiente tick.)
        if (enRed && !gs.Object.HasStateAuthority)
        {
            Debug.Log($"[DEBUG] Solicitando al Host saltar al Reto {index + 1}...");
            gs.RPC_SolicitarCambioReto(index);
            return;
        }

        // Caso 2 — somos el Host en red: aplicar y propagar a los clientes.
        if (enRed && gs.Object.HasStateAuthority)
        {
            Debug.Log($"[DEBUG] Host: saltando al Reto {index + 1} (se propaga a los clientes)...");
            gs.AvanzarReto(index);
            return;
        }

        // Caso 3 — offline / sin red: directo al GameManager local.
        if (_gameManager != null)
        {
            Debug.Log($"[DEBUG] Offline: saltando al Reto {index + 1}...");
            _gameManager.GoToLevel(index);
        }
        else
        {
            Debug.LogWarning($"[DEBUG] No hay GameManager ni GameSession para saltar al Reto {index + 1}.");
        }
    }
}
