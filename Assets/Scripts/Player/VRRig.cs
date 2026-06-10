using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Puente entre el hardware de rastreo (Meta XR) y los huesos de un avatar humanoide.
/// IK de 3 puntos: cabeza ← Main Camera, manos ← anclajes de los mandos.
///
/// Arquitectura:
///   - Corre en LateUpdate con execution order alto → sobrescribe la pose del Animator
///     (el Animator escribe la animación antes de LateUpdate; aquí la pisamos con tracking).
///   - NO estira el cuello: la cabeza se ROTA hacia la cámara y luego se traslada TODA la
///     raíz del modelo para que el pivote de la cabeza caiga en el objetivo (cuerpo rígido).
///   - Brazos por IK analítico de dos huesos (ley de cosenos).
///   - Decapitación local (Fase B) + calibración de altura (Fase C).
///
/// Sustituye a ExplorerAvatar (seguimiento de cuerpo/cabeza) y a RobotHandIK (brazos).
/// Si esos componentes están presentes, actívalos en 'desactivarControladoresPrevios'.
/// </summary>
[DefaultExecutionOrder(10000)]
public class VRRig : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────────────────
    [Header("Targets de tracking (hardware Meta XR)")]
    [Tooltip("Main Camera del XR Origin (centro óptico del visor).")]
    public Transform headTarget;
    [Tooltip("Anclaje del mando izquierdo.")]
    public Transform leftHandTarget;
    [Tooltip("Anclaje del mando derecho.")]
    public Transform rightHandTarget;

    [Header("Avatar humanoide")]
    public Animator animator;
    [Tooltip("Raíz del modelo que se mueve y se escala. Normalmente el GO con el Animator.")]
    public Transform avatarRoot;

    [Header("Offset de la cabeza (en el espacio LOCAL de la cámara)")]
    [Tooltip("Desfase posición pivote-de-cabeza → centro óptico. Z- = el hueso queda detrás del ojo.")]
    public Vector3 headPositionOffset = new Vector3(0f, -0.07f, -0.10f);
    [Tooltip("Corrección de rotación si el eje del hueso de la cabeza no mira al frente del FBX.")]
    public Vector3 headRotationOffset = Vector3.zero;

    [Header("Offset de las manos (en el espacio LOCAL del mando)")]
    public Vector3 leftHandPositionOffset = Vector3.zero;
    public Vector3 leftHandRotationOffset = new Vector3(0f, 90f, 0f);
    public Vector3 rightHandPositionOffset = Vector3.zero;
    public Vector3 rightHandRotationOffset = new Vector3(0f, -90f, 0f);

    [Header("Peso global del IK (0 = solo animación, 1 = solo tracking)")]
    [Range(0f, 1f)] public float weight = 1f;

    [Tooltip("Mueve los BRAZOS del robot hacia los mandos por IK. Si las manos visibles son las " +
             "procedurales (siguen el mando exacto), puedes desactivarlo para evitar brazos raros.")]
    public bool resolverBrazos = true;

    [Tooltip("ESTIRA el brazo del robot para que la mano llegue EXACTO al mando aunque el brazo " +
             "sea más corto que tu alcance (arregla 'el mando queda por encima de la mano'). " +
             "Escala el hueso del brazo solo cuando hace falta.")]
    public bool estirarBrazo = true;

    [Header("Dedos del avatar (grip/gatillo)")]
    [Tooltip("Abre/cierra los DEDOS del propio avatar con el grip/gatillo del mando, pisando al " +
             "Animator (que los dejaba en puño). Requiere avatar Humanoid con dedos mapeados.")]
    public bool controlarDedos = true;
    [Tooltip("Cuánto cierran los dedos (grados por falange). Si cierran AL REVÉS, ponlo negativo.")]
    [Range(-120f, 120f)] public float curlDedosMax = 60f;
    [Tooltip("Eje LOCAL de flexión de las falanges. Ajústalo en vivo si los dedos doblan raro " +
             "(prueba (0,0,1), (0,1,0), (1,0,0) y signos).")]
    public Vector3 ejeCurlDedos = new Vector3(0f, 0f, 1f);
    [Tooltip("Suavizado del cierre de dedos.")]
    public float velocidadDedos = 14f;

    [Tooltip("Oculta las manos PROCEDURALES (HandModelController) al iniciar para usar SOLO las " +
             "manos del propio avatar RobotKyle (movidas por el IK de brazos). Activa esto + " +
             "'resolverBrazos' para tener una sola pareja de manos. El GO de la mano conserva su " +
             "XRDirectInteractor, así que se sigue pudiendo agarrar.")]
    public bool usarManosDelAvatar = false;

    [Tooltip("Oculta las manos del PROPIO avatar RobotKyle (encoge sus huesos a 0, como la " +
             "decapitación). Úsalo cuando muestras las manos PROCEDURALES, para que las manos " +
             "del robot no cuelguen cerradas y se dupliquen. NO lo actives si usas las manos del avatar.")]
    public bool ocultarManosRobot = false;

    [Header("Seguir cuerpo con la cámara (yaw)")]
    [Tooltip("Rota el CUERPO en el eje vertical para que mire hacia donde mira la cámara. " +
             "Al girar la cabeza/visor, el torso gira con ella.")]
    public bool seguirCuerpoConCamara = true;
    [Tooltip("Suavizado del giro del cuerpo (mayor = más rápido). 0 o 'giroInstantaneo' = sin suavizar.")]
    public float velocidadGiroCuerpo = 12f;
    [Tooltip("Si está activo, el cuerpo gira instantáneamente con la cámara (sin suavizado).")]
    public bool giroInstantaneo = false;
    [Tooltip("Corrección de yaw si el modelo no mira al frente (+Z) en su pose neutra. " +
             "Si el robot queda de espaldas, pon 180.")]
    public float cuerpoYawOffset = 0f;

    [Header("Fase B — Decapitación local")]
    public bool decapitar = true;
    [Tooltip("Capa a la que mandar el renderer de la cabeza/casco (debe existir en Tags & Layers).")]
    public string capaOculta = "LocalHidden";
    [Tooltip("Cámara local del visor a la que se le quita esa capa del culling mask.")]
    public Camera camaraLocal;

    [Header("Fase C — Calibración de altura")]
    public bool calibrarAltura = true;
    [Tooltip("Transform del suelo del rig (XR Origin). Vacío = plano y=0 del mundo.")]
    public Transform pisoReferencia;
    [Tooltip("Escalado uniforme (recomendado). Si lo desactivas, escala solo Y (deforma proporciones).")]
    public bool escalaUniforme = true;
    [Range(0.3f, 1f)] public float escalaMin = 0.6f;
    [Range(1f, 2.5f)] public float escalaMax = 1.6f;
    [Tooltip("Segundos de espera para que el tracking del visor se asiente antes de medir.")]
    public float retrasoCalibracion = 0.5f;
    [Tooltip("Altura de ojos del jugador (m) usada si el visor aún NO reporta (Editor sin casco).\n" +
             "Evita que el avatar quede a tamaño completo y atraviese el piso.")]
    public float alturaJugador = 1.6f;

    [Tooltip("Vigila que los PIES sigan tocando el suelo y se recalibra solo si se separan " +
             "(p. ej. si la calibración inicial corrió mientras el rig aún caía). Evita el avatar " +
             "flotando o con las piernas bajo el piso.")]
    public bool mantenerPiesEnSuelo = true;
    [Tooltip("Cuánto pueden separarse los pies del suelo (m) antes de recalibrar. Evita reescalados constantes.")]
    public float toleranciaPiso = 0.06f;
    [Tooltip("Cambio mínimo de escala (fracción) para aplicar una recalibración. Rompe el lazo de " +
             "realimentación de VigilarPies: sin esto, cada reescalado mueve los pies y vuelve a " +
             "disparar la recalibración cada frame (stutter + temblor de altura).")]
    [Range(0.005f, 0.1f)] public float histeresisEscala = 0.02f;

    [Header("Convivencia")]
    [Tooltip("Desactiva ExplorerAvatar y RobotHandIK al iniciar para evitar doble control.")]
    public bool desactivarControladoresPrevios = true;

    // ─────────────────────────────────────────────────────────
    //  Huesos cacheados
    // ─────────────────────────────────────────────────────────
    Transform _head, _neck;
    Transform _lUpper, _lFore, _lHand;
    Transform _rUpper, _rFore, _rHand;
    Transform _lFoot, _rFoot;
    float _lUpperLen, _lForeLen, _rUpperLen, _rForeLen;
    Vector3 _lUpperScale0, _rUpperScale0;   // escala bind del brazo (para restaurar el estiramiento)
    bool _ready;

    // Dedos (huesos Humanoid + su rotación local de bind) y entrada del mando
    readonly List<Transform> _lFingerBones = new();
    readonly List<Quaternion> _lFingerBind = new();
    readonly List<Transform> _rFingerBones = new();
    readonly List<Quaternion> _rFingerBind = new();
    readonly List<InputDevice> _lDevices = new();
    readonly List<InputDevice> _rDevices = new();
    float _lCurl, _rCurl;

    // ─────────────────────────────────────────────────────────
    //  Lifecycle
    // ─────────────────────────────────────────────────────────
    void Start()
    {
        if (avatarRoot == null && animator != null) avatarRoot = animator.transform;
        if (animator == null && avatarRoot != null) animator = avatarRoot.GetComponentInChildren<Animator>();
        if (camaraLocal == null && headTarget != null) camaraLocal = headTarget.GetComponent<Camera>();

        if (!ResolveBones()) { enabled = false; return; }

        if (desactivarControladoresPrevios) DesactivarPrevios();
        if (usarManosDelAvatar)             OcultarManosProcedurales();
        if (ocultarManosRobot)              OcultarManosDelAvatar();
        if (decapitar)       Decapitar();
        if (calibrarAltura)  Invoke(nameof(CalibrarAltura), retrasoCalibracion);

        _ready = true;
    }

   // 1. Añade esta variable debajo de "public Transform avatarRoot;"
    [Tooltip("Cápsula física de colisión. Se moverá para seguir la cabeza local del jugador.")]
    public CharacterController characterController;


    // 2. Reemplaza el método LateUpdate() con esta versión:
    void LateUpdate()
    {
        if (!_ready || weight <= 0f) return;

        if (seguirCuerpoConCamara) SolveBodyYaw();
        SolveHead();

        if (resolverBrazos)
        {
            SolveArm(_lUpper, _lFore, _lHand, _lUpperLen, _lForeLen, _lUpperScale0,
                     leftHandTarget,  leftHandPositionOffset,  leftHandRotationOffset,  isLeft: true);
            SolveArm(_rUpper, _rFore, _rHand, _rUpperLen, _rForeLen, _rUpperScale0,
                     rightHandTarget, rightHandPositionOffset, rightHandRotationOffset, isLeft: false);
        }

        if (controlarDedos) SolveDedos();

        if (mantenerPiesEnSuelo) VigilarPies();

        // CORRECCIÓN: Sincronizar colisionador físico con la cámara VR
        SincronizarColisiones();
    }

    // 3. Añade este método nuevo al final del script:
    void SincronizarColisiones()
    {
        if (characterController != null && headTarget != null)
        {
            // Alinear el centro (X, Z) con la cabeza, manteniendo la altura física de la cápsula
            Vector3 center = headTarget.localPosition;
            center.y = (characterController.height / 2f) + characterController.skinWidth;
            characterController.center = center;
        }
    }
    
    // ─────────────────────────────────────────────────────────
    //  Mantener los pies en el suelo (auto-corrección)
    // ─────────────────────────────────────────────────────────
    void VigilarPies()
    {
        if (avatarRoot == null) return;

        float piso = pisoReferencia != null ? pisoReferencia.position.y : 0f;
        float pies = PiesY();
        if (pies == float.MaxValue) return; // sin huesos de pie → no se puede vigilar

        // Si los pies se separaron del suelo (flotan o se hunden) más que la tolerancia,
        // recalibrar la altura → reescala el avatar para que los pies vuelvan al piso.
        if (Mathf.Abs(pies - piso) > toleranciaPiso)
            CalibrarAltura();
    }

    /// <summary>Y del pie más bajo en mundo, o float.MaxValue si no hay huesos de pie.</summary>
    float PiesY()
    {
        float y = float.MaxValue;
        if (_lFoot != null) y = Mathf.Min(y, _lFoot.position.y);
        if (_rFoot != null) y = Mathf.Min(y, _rFoot.position.y);
        return y;
    }

    // ─────────────────────────────────────────────────────────
    //  FASE A.0 — Cuerpo (yaw) sigue a la cámara
    // ─────────────────────────────────────────────────────────
    void SolveBodyYaw()
    {
        if (avatarRoot == null || headTarget == null) return;

        // Dirección de mirada de la cámara, aplanada al suelo (solo yaw, cuerpo siempre erguido).
        Vector3 fwd = headTarget.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 1e-4f) return; // mirando recto arriba/abajo → no hay yaw definido

        Quaternion objetivo = Quaternion.LookRotation(fwd.normalized, Vector3.up)
                              * Quaternion.Euler(0f, cuerpoYawOffset, 0f);

        if (giroInstantaneo || velocidadGiroCuerpo <= 0f)
        {
            avatarRoot.rotation = objetivo;
        }
        else
        {
            // Suavizado exponencial estable independiente del framerate.
            float t = 1f - Mathf.Exp(-velocidadGiroCuerpo * Time.deltaTime);
            avatarRoot.rotation = Quaternion.Slerp(avatarRoot.rotation, objetivo, t);
        }
        // Nota: SolveHead (abajo) reubica la raíz para que el pivote de la cabeza vuelva a caer
        // bajo la cámara tras este giro → el cuerpo rota alrededor de tu cabeza, no se desplaza.
    }

    // ─────────────────────────────────────────────────────────
    //  FASE A.1 — Cabeza (sin romper el cuello)
    // ─────────────────────────────────────────────────────────
    void SolveHead()
    {
        if (_head == null || headTarget == null || avatarRoot == null) return;

        // Pose objetivo = target del hardware + offset expresado en el espacio LOCAL del target.
        //   targetPos = camPos + camRot * offsetLocal   → el offset gira con tu cabeza.
        Quaternion targetRot = headTarget.rotation * Quaternion.Euler(headRotationOffset);
        Vector3    targetPos = headTarget.position + headTarget.rotation * headPositionOffset;

        // 1) Rotar la cabeza (gira sobre su pivote; NO mueve su propia posición).
        _head.rotation = weight >= 1f
            ? targetRot
            : Quaternion.Slerp(_head.rotation, targetRot, weight);

        // 2) Trasladar TODA la raíz para que el pivote de la cabeza caiga en targetPos.
        //    Como movemos el cuerpo entero de forma rígida, el cuello conserva su longitud.
        Vector3 delta = targetPos - _head.position;
        avatarRoot.position += delta * weight;
    }

    // ─────────────────────────────────────────────────────────
    //  FASE A.2 — Brazos (IK analítico de 2 huesos)
    // ─────────────────────────────────────────────────────────
    void SolveArm(Transform upper, Transform fore, Transform hand,
                  float upperLen, float foreLen, Vector3 upperScale0,
                  Transform target, Vector3 posOffset, Vector3 rotOffset, bool isLeft)
    {
        if (upper == null || fore == null || hand == null || target == null || upperLen <= 0.001f)
            return;

        Vector3    tgtPos = target.position + target.rotation * posOffset;
        Quaternion tgtRot = target.rotation * Quaternion.Euler(rotOffset);

        Vector3 root     = upper.position;
        float   realDist = Vector3.Distance(root, tgtPos);
        float   reach0   = upperLen + foreLen;

        // ESTIRAMIENTO: si el mando está más lejos que el alcance del brazo, escala el hueso del
        // brazo (uniforme; el antebrazo y la mano lo heredan) para que la mano LLEGUE al mando.
        // Si no hace falta, restaura la escala bind. Así no queda "el mando por encima de la mano".
        float stretch = (estirarBrazo && reach0 > 0.001f && realDist > reach0) ? realDist / reach0 : 1f;
        upper.localScale = upperScale0 * stretch;
        upperLen *= stretch;
        foreLen  *= stretch;

        float   reach  = upperLen + foreLen;
        float   dist   = Mathf.Clamp(realDist, 0.01f, reach * 0.999f);
        Vector3 toTgt  = (tgtPos - root).normalized;

        // Pista de codo: hacia el lado del brazo y un poco abajo (postura natural).
        Vector3 pole = ((isLeft ? Vector3.left : Vector3.right) + Vector3.down * 0.3f).normalized;
        Vector3 perp = Vector3.ProjectOnPlane(pole, toTgt);
        if (perp.sqrMagnitude < 1e-4f) perp = Vector3.Cross(toTgt, upper.right);
        perp.Normalize();

        // Ley de cosenos → ángulo en el hombro y posición del codo.
        float cosA   = Mathf.Clamp((upperLen*upperLen + dist*dist - foreLen*foreLen) / (2f*upperLen*dist), -1f, 1f);
        float sinA   = Mathf.Sqrt(Mathf.Max(0f, 1f - cosA*cosA));
        Vector3 elbow = root + (toTgt * cosA + perp * sinA) * upperLen;

        // Rotar hombro → codo, antebrazo → muñeca, y fijar la rotación de la mano.
        AlinearHueso(upper, fore.position - upper.position, elbow - upper.position);
        AlinearHueso(fore,  hand.position - fore.position,  tgtPos - fore.position);
        hand.rotation = weight >= 1f ? tgtRot : Quaternion.Slerp(hand.rotation, tgtRot, weight);
    }

    static void AlinearHueso(Transform bone, Vector3 from, Vector3 to)
    {
        if (from.sqrMagnitude < 1e-8f || to.sqrMagnitude < 1e-8f) return;
        if (Vector3.Angle(from, to) < 0.05f) return;
        bone.rotation = Quaternion.FromToRotation(from, to) * bone.rotation;
    }

    // ─────────────────────────────────────────────────────────
    //  FASE A.3 — Dedos del avatar (grip/gatillo)
    // ─────────────────────────────────────────────────────────
    void SolveDedos()
    {
        float dt = Time.deltaTime;
        _lCurl = Mathf.Lerp(_lCurl, LeerCierre(_lDevices, true),  velocidadDedos * dt);
        _rCurl = Mathf.Lerp(_rCurl, LeerCierre(_rDevices, false), velocidadDedos * dt);
        AplicarCurl(_lFingerBones, _lFingerBind, _lCurl);
        AplicarCurl(_rFingerBones, _rFingerBind, _rCurl);
    }

    /// <summary>0..1 = cuánto cerrar la mano (máximo de grip y gatillo del mando de ese lado).</summary>
    float LeerCierre(List<InputDevice> devices, bool izq)
    {
        if (devices.Count == 0)
        {
            var car = (izq ? InputDeviceCharacteristics.Left : InputDeviceCharacteristics.Right)
                      | InputDeviceCharacteristics.Controller;
            InputDevices.GetDevicesWithCharacteristics(car, devices);
        }
        float grip = 0f, trig = 0f;
        foreach (var d in devices)
        {
            if (!d.isValid) continue;
            if (d.TryGetFeatureValue(CommonUsages.grip,    out float g)) grip = Mathf.Max(grip, g);
            if (d.TryGetFeatureValue(CommonUsages.trigger, out float t)) trig = Mathf.Max(trig, t);
        }
        return Mathf.Max(grip, trig);
    }

    /// <summary>Pisa al Animator: cada falange = su bind + un giro de cierre proporcional a 'curl'.</summary>
    void AplicarCurl(List<Transform> bones, List<Quaternion> binds, float curl)
    {
        Quaternion delta = Quaternion.Euler(ejeCurlDedos * (curl * curlDedosMax));
        for (int i = 0; i < bones.Count; i++)
            if (bones[i] != null) bones[i].localRotation = binds[i] * delta;
    }

    // ─────────────────────────────────────────────────────────
    //  FASE B — Decapitación local
    // ─────────────────────────────────────────────────────────
    public void Decapitar()
    {
        // Estrategia ideal: si existe un renderer de cabeza/casco SEPARADO, mandarlo a una capa
        // que la cámara local NO renderiza → invisible para ti, visible para otras cámaras/red.
        Renderer headRend = BuscarRendererCabeza();
        if (headRend != null)
        {
            int capa = LayerMask.NameToLayer(capaOculta);
            if (capa >= 0)
            {
                headRend.gameObject.layer = capa;
                if (camaraLocal != null) camaraLocal.cullingMask &= ~(1 << capa);
                Debug.Log($"[VRRig] Cabeza '{headRend.name}' enviada a la capa '{capaOculta}' (oculta solo localmente).");
                return;
            }
            Debug.LogWarning($"[VRRig] La capa '{capaOculta}' no existe en Tags & Layers. Usando fallback.");
        }

        // Fallback (malla skinned ÚNICA, como RobotKyle): encoger el hueso de la cabeza.
        // En red, cada cliente renderiza SU instancia del avatar → encogerla aquí solo te afecta
        // a ti; el cliente del Técnico tiene su copia con la cabeza intacta.
        if (_head != null)
        {
            _head.localScale = Vector3.zero;
            Debug.Log("[VRRig] Cabeza ocultada por escala de hueso (malla única → decapitación por-cliente).");
        }
    }

    /// <summary>Devuelve un renderer dedicado a la cabeza/casco si existe (malla separada), o null.</summary>
    Renderer BuscarRendererCabeza()
    {
        if (avatarRoot == null) return null;

        // 1) Por nombre.
        foreach (var r in avatarRoot.GetComponentsInChildren<Renderer>(true))
        {
            string n = r.name.ToLowerInvariant();
            if (n.Contains("head") || n.Contains("helmet") || n.Contains("casco") || n.Contains("face"))
                return r;
        }

        // 2) SkinnedMeshRenderer cuya raíz/huesos dominantes sean la cabeza (heurística).
        if (_head != null)
            foreach (var smr in avatarRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (smr.rootBone != null && smr.rootBone.IsChildOf(_neck != null ? _neck : _head))
                    return smr;

        return null; // malla única → el caller usa el fallback de escala de hueso
    }

    // ─────────────────────────────────────────────────────────
    //  FASE C — Calibración de altura (procedural)
    // ─────────────────────────────────────────────────────────
    public void CalibrarAltura()
    {
        if (_head == null || headTarget == null || avatarRoot == null) return;

        float piso = pisoReferencia != null ? pisoReferencia.position.y : 0f;

        // Altura de OJOS real del jugador, medida por el visor respecto al suelo del rig.
        float alturaReal = headTarget.position.y - piso;
        if (alturaReal < 0.5f) alturaReal = alturaJugador; // sin casco/editor → usa el valor manual

        // Altura de ojos del MODELO en su escala actual: distancia ojos→PIES (no ojos→raíz),
        // para que al escalar los PIES queden exactamente en el suelo. Si no hay huesos de pie,
        // se usa la raíz como referencia (modelos cuyo origen está entre los pies).
        float pies = PiesY();
        float baseY = (pies != float.MaxValue) ? pies : avatarRoot.position.y;
        float alturaModelo = _head.position.y - baseY;
        if (alturaModelo < 0.01f) return;

        // Escala que iguala la altura de ojos del modelo a la del jugador.
        //   escala = alturaReal / (alturaModelo / escalaActual)
        float escalaActual = avatarRoot.localScale.y;
        float escala = Mathf.Clamp(alturaReal / alturaModelo * escalaActual, escalaMin, escalaMax);

        // Histéresis: si el cambio es minúsculo, no reescalar. Reescalar mueve los pies, lo que
        // vuelve a disparar VigilarPies → CalibrarAltura cada frame (stutter por reescalado +
        // forzado de jerarquía/IK, y el temblor de altura visible en escena). Una vez dentro del
        // umbral, CalibrarAltura es un no-op barato y el rig se queda quieto.
        if (Mathf.Abs(escala - escalaActual) < histeresisEscala * escalaActual) return;

        avatarRoot.localScale = escalaUniforme
            ? Vector3.one * escala
            : new Vector3(avatarRoot.localScale.x, escala, avatarRoot.localScale.z);

        Debug.Log($"[VRRig] Altura calibrada: ojos reales {alturaReal:0.00} m → escala {escala:0.000} " +
                  $"({(escalaUniforme ? "uniforme" : "solo Y")}).");
    }

    // ─────────────────────────────────────────────────────────
    //  Setup
    // ─────────────────────────────────────────────────────────
    bool ResolveBones()
    {
        if (animator == null || !animator.isHuman)
        {
            Debug.LogError("[VRRig] Se requiere un Animator Humanoid en el avatar.", this);
            return false;
        }

        _head = animator.GetBoneTransform(HumanBodyBones.Head);
        _neck = animator.GetBoneTransform(HumanBodyBones.Neck);

        _lUpper = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        _lFore  = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        _lHand  = animator.GetBoneTransform(HumanBodyBones.LeftHand);

        _rUpper = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        _rFore  = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        _rHand  = animator.GetBoneTransform(HumanBodyBones.RightHand);

        // Pies (para mantener el avatar pegado al suelo). Preferir los dedos si existen.
        _lFoot = animator.GetBoneTransform(HumanBodyBones.LeftToes)  ?? animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        _rFoot = animator.GetBoneTransform(HumanBodyBones.RightToes) ?? animator.GetBoneTransform(HumanBodyBones.RightFoot);

        if (_lUpper && _lFore && _lHand)
        {
            _lUpperLen = Vector3.Distance(_lUpper.position, _lFore.position);
            _lForeLen  = Vector3.Distance(_lFore.position,  _lHand.position);
            _lUpperScale0 = _lUpper.localScale;
        }
        if (_rUpper && _rFore && _rHand)
        {
            _rUpperLen = Vector3.Distance(_rUpper.position, _rFore.position);
            _rForeLen  = Vector3.Distance(_rFore.position,  _rHand.position);
            _rUpperScale0 = _rUpper.localScale;
        }

        CachearDedos(true,  _lFingerBones, _lFingerBind);
        CachearDedos(false, _rFingerBones, _rFingerBind);

        if (_head == null) { Debug.LogError("[VRRig] No se encontró el hueso Head.", this); return false; }
        return true;
    }

    /// <summary>Cachea las falanges Humanoid (proximal+intermedia) de una mano y su rotación de bind.</summary>
    void CachearDedos(bool izq, List<Transform> bones, List<Quaternion> binds)
    {
        bones.Clear(); binds.Clear();
        HumanBodyBones[] huesos = izq
            ? new[] { HumanBodyBones.LeftThumbProximal,  HumanBodyBones.LeftThumbIntermediate,
                      HumanBodyBones.LeftIndexProximal,  HumanBodyBones.LeftIndexIntermediate,
                      HumanBodyBones.LeftMiddleProximal, HumanBodyBones.LeftMiddleIntermediate,
                      HumanBodyBones.LeftRingProximal,   HumanBodyBones.LeftRingIntermediate,
                      HumanBodyBones.LeftLittleProximal, HumanBodyBones.LeftLittleIntermediate }
            : new[] { HumanBodyBones.RightThumbProximal,  HumanBodyBones.RightThumbIntermediate,
                      HumanBodyBones.RightIndexProximal,  HumanBodyBones.RightIndexIntermediate,
                      HumanBodyBones.RightMiddleProximal, HumanBodyBones.RightMiddleIntermediate,
                      HumanBodyBones.RightRingProximal,   HumanBodyBones.RightRingIntermediate,
                      HumanBodyBones.RightLittleProximal, HumanBodyBones.RightLittleIntermediate };

        foreach (var hb in huesos)
        {
            var t = animator.GetBoneTransform(hb);
            if (t != null) { bones.Add(t); binds.Add(t.localRotation); }
        }
    }

    void DesactivarPrevios()
    {
        var ea = GetComponent<ExplorerAvatar>();
        if (ea != null && ea.enabled) { ea.enabled = false; Debug.Log("[VRRig] ExplorerAvatar desactivado (lo sustituye VRRig)."); }

        var ik = GetComponent<RobotHandIK>();
        if (ik != null && ik.enabled) { ik.enabled = false; Debug.Log("[VRRig] RobotHandIK desactivado (lo sustituye VRRig)."); }
    }

    /// <summary>
    /// Oculta las manos procedurales (HandModelController) para dejar SOLO las manos del avatar
    /// (movidas por el IK de brazos). Apaga el visual 'Hand_Pivot' pero conserva el GO de la mano
    /// (y su XRDirectInteractor) para seguir pudiendo agarrar.
    /// </summary>
    void OcultarManosProcedurales()
    {
        int n = 0;
        foreach (var h in FindObjectsByType<HandModelController>(FindObjectsInactive.Include))
        {
            var pivot = h.transform.Find("Hand_Pivot");
            if (pivot != null) { pivot.gameObject.SetActive(false); n++; }
        }
        Debug.Log($"[VRRig] Manos procedurales ocultadas ({n}). Se usan las manos del avatar (IK de brazos). " +
                  (resolverBrazos ? "" : "ATENCIÓN: 'resolverBrazos' está desactivado → las manos del robot no seguirán a los mandos."));
    }

    /// <summary>
    /// Oculta las manos del propio avatar encogiendo sus huesos (LeftHand/RightHand) a escala 0,
    /// igual que la decapitación. Solo afecta a la instancia local. Úsalo con manos procedurales.
    /// </summary>
    void OcultarManosDelAvatar()
    {
        if (_lHand != null) _lHand.localScale = Vector3.zero;
        if (_rHand != null) _rHand.localScale = Vector3.zero;
        Debug.Log("[VRRig] Manos del avatar ocultadas (huesos a 0) → solo se ven las procedurales.");
    }
}
