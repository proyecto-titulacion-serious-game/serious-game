using System.IO;
using UnityEngine;
using ReadyPlayerMe.Core;

/// <summary>
/// Carga el avatar Ready Player Me en el Explorador con tres rutas de fallback:
///
///   RUTA 1 (PRIORIDAD): archivo .glb local en Assets/Resources/Avatars/avatar_explorer.glb
///   RUTA 2: descarga desde internet (URL configurable en Inspector)
///   RUTA 3: avatarRoot ya asignado en ExplorerAvatar (RobotKyle u otro)
///
/// Para usar la ruta local (recomendada para demos sin internet):
///   1. Descarga un avatar .glb desde readyplayer.me (ver instrucciones abajo)
///   2. Colócalo en Assets/Resources/Avatars/avatar_explorer.glb
///   3. Este script lo cargará automáticamente sin internet.
///
/// CÓMO DESCARGAR EL AVATAR .glb:
///   Opción A — Desde el editor RPM en Unity:
///     Window → Ready Player Me → Avatar Creator → descarga y guarda en Resources/Avatars/
///
///   Opción B — Desde el navegador:
///     1. Ve a demo.readyplayer.me
///     2. Crea y personaliza tu avatar
///     3. Copia la URL .glb que aparece al finalizar
///     4. Ábrela en el navegador → descarga el archivo
///     5. Renómbralo avatar_explorer.glb y ponlo en Assets/Resources/Avatars/
///
///   Opción C — Desde los Samples del package:
///     Package Manager → Ready Player Me Core → Samples → AvatarLoadingSamples → Import
///     Toma un .glb de la carpeta de samples.
/// </summary>
[RequireComponent(typeof(ExplorerAvatar))]
public class RPMExplorerAvatar : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Archivo local (sin internet — recomendado para demos)")]
    [Tooltip("Nombre del archivo .glb en Assets/Resources/Avatars/ (sin extensión).")]
    public string localAvatarName = "avatar_explorer";

    [Header("Descarga online (requiere internet)")]
    [Tooltip("URL del avatar .glb. Vacío = usa URL demo de RPM.")]
    public string avatarUrl = "";
    [Tooltip("Si no hay archivo local, intentar descargar desde internet.")]
    public bool allowOnlineDownload = true;

    [Header("Feedback de carga")]
    [Tooltip("GO visible mientras carga (spinner, texto, etc.). Opcional.")]
    public GameObject loadingIndicator;

    [Header("Auto-detectado")]
    public ExplorerAvatar explorerAvatar;

    // ─────────────────────────────────────────────
    //  Privado
    // ─────────────────────────────────────────────
    private GameObject      _currentAvatar;
    private AvatarObjectLoader _loader;

    const string RESOURCES_PATH = "Avatars/";
    const string DEMO_URL = "https://api.readyplayer.me/v1/avatars/638df693d72bffc6fa17943c.glb";

    // ─────────────────────────────────────────────
    //  Lifecycle
    // ─────────────────────────────────────────────
    void Awake()
    {
        explorerAvatar ??= GetComponent<ExplorerAvatar>();
    }

    void Start()
    {
        SetLoading(true);
        TryLoadLocal();
    }

    // ─────────────────────────────────────────────
    //  Ruta 1 — Archivo local en Resources
    // ─────────────────────────────────────────────
    void TryLoadLocal()
    {
        // Resources.Load no soporta .glb directamente — usamos AvatarObjectLoader con path
        // absoluto si el archivo existe en StreamingAssets o en persistentDataPath.

        // Buscar en Application.persistentDataPath (caché del SDK)
        string cachePath = Path.Combine(Application.persistentDataPath,
                                        localAvatarName + ".glb");

        // Buscar en StreamingAssets (archivo copiado manualmente)
        string streamingPath = Path.Combine(Application.streamingAssetsPath,
                                            "Avatars", localAvatarName + ".glb");

        string foundPath = null;
        if (File.Exists(cachePath))      foundPath = cachePath;
        else if (File.Exists(streamingPath)) foundPath = streamingPath;

        if (foundPath != null)
        {
            Debug.Log($"[RPMExplorerAvatar] Cargando avatar local: {foundPath}");
            LoadFromPath(foundPath);
        }
        else
        {
            Debug.Log("[RPMExplorerAvatar] No hay archivo local. " +
                      (allowOnlineDownload ? "Intentando descarga online..." :
                       "Descarga online desactivada. Usando avatar base."));

            if (allowOnlineDownload)
                TryLoadOnline();
            else
                UseBaseAvatar();
        }
    }

    // ─────────────────────────────────────────────
    //  Ruta 2 — Descarga online
    // ─────────────────────────────────────────────
    void TryLoadOnline()
    {
        string url = string.IsNullOrWhiteSpace(avatarUrl) ? DEMO_URL : avatarUrl;
        Debug.Log($"[RPMExplorerAvatar] Descargando avatar: {url}");
        LoadFromUrl(url);
    }

    // ─────────────────────────────────────────────
    //  Loader compartido
    // ─────────────────────────────────────────────
    void LoadFromPath(string path)
    {
        _loader = new AvatarObjectLoader();
        _loader.OnCompleted += OnLoaded;
        _loader.OnFailed    += OnFailedLocal;
        _loader.LoadAvatar(path);
    }

    void LoadFromUrl(string url)
    {
        _loader = new AvatarObjectLoader();
        _loader.OnCompleted += OnLoaded;
        _loader.OnFailed    += OnFailedOnline;
        _loader.LoadAvatar(url);
    }

    // ─────────────────────────────────────────────
    //  Callbacks
    // ─────────────────────────────────────────────
    void OnLoaded(object sender, CompletionEventArgs args)
    {
        SetLoading(false);

        if (_currentAvatar != null) Destroy(_currentAvatar);
        _currentAvatar = args.Avatar;

        _currentAvatar.transform.SetParent(transform, false);
        _currentAvatar.transform.localPosition = Vector3.zero;
        _currentAvatar.transform.localRotation = Quaternion.identity;
        _currentAvatar.transform.localScale    = Vector3.one;

        EnsureURPMaterials(_currentAvatar);
        ApplyToExplorerAvatar(_currentAvatar);

        Debug.Log($"[RPMExplorerAvatar] Avatar listo: {_currentAvatar.name}");
    }

    void OnFailedLocal(object sender, FailureEventArgs args)
    {
        Debug.LogWarning($"[RPMExplorerAvatar] Fallo en carga local: {args.Message}");
        if (allowOnlineDownload)
            TryLoadOnline();
        else
            UseBaseAvatar();
    }

    void OnFailedOnline(object sender, FailureEventArgs args)
    {
        SetLoading(false);
        Debug.LogWarning($"[RPMExplorerAvatar] Sin internet o API caída: {args.Message}\n" +
                         "Usando avatar base (RobotKyle o el que esté en ExplorerAvatar).");
        UseBaseAvatar();
    }

    // ─────────────────────────────────────────────
    //  Ruta 3 — Fallback al avatar base
    // ─────────────────────────────────────────────
    void UseBaseAvatar()
    {
        SetLoading(false);

        if (explorerAvatar == null) return;

        // Si ExplorerAvatar ya tiene un avatarRoot asignado (RobotKyle), lo usa tal cual
        if (explorerAvatar.avatarRoot != null)
        {
            Debug.Log("[RPMExplorerAvatar] Usando avatar base: " +
                      explorerAvatar.avatarRoot.name);
            explorerAvatar.HideHead();
        }
        else
        {
            Debug.LogWarning("[RPMExplorerAvatar] Sin avatar local, sin internet y sin " +
                             "avatar base. Arrastra un modelo al campo avatarRoot " +
                             "de ExplorerAvatar en el Inspector.");
        }
    }

    // ─────────────────────────────────────────────
    //  Aplicar avatar al sistema de tracking
    // ─────────────────────────────────────────────
    void ApplyToExplorerAvatar(GameObject avatar)
    {
        if (explorerAvatar == null) return;

        explorerAvatar.avatarRoot     = avatar.transform;
        explorerAvatar.avatarAnimator = avatar.GetComponentInChildren<Animator>();
        explorerAvatar.hideHeadInVR   = true;
        explorerAvatar.HideHead();
    }

    // ─────────────────────────────────────────────
    //  API pública
    // ─────────────────────────────────────────────

    /// <summary>Carga un avatar específico por URL (llamar desde lobby/menú).</summary>
    public void SetAvatarUrl(string url)
    {
        avatarUrl = url;
        SetLoading(true);
        LoadFromUrl(url);
    }

    /// <summary>Guarda el avatar actual en el caché local para futuros usos sin internet.</summary>
    public void SaveToLocalCache()
    {
        if (_currentAvatar == null) return;
        Debug.Log("[RPMExplorerAvatar] El SDK guarda el avatar automáticamente en persistentDataPath.");
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────
    void SetLoading(bool active)
    {
        if (loadingIndicator != null)
            loadingIndicator.SetActive(active);
    }

    static void EnsureURPMaterials(GameObject avatar)
    {
        var urpShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpShader == null) return;

        foreach (var r in avatar.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] != null && mats[i].shader.name.StartsWith("Standard"))
                    mats[i].shader = urpShader;
            }
            r.sharedMaterials = mats;
        }
    }

    void OnDestroy()
    {
        if (_loader != null)
        {
            _loader.OnCompleted -= OnLoaded;
            _loader.OnFailed    -= OnFailedLocal;
            _loader.OnFailed    -= OnFailedOnline;
        }
        if (_currentAvatar != null) Destroy(_currentAvatar);
    }
}
