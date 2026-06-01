using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Herramienta de editor para añadir los assets recientes a la escena del Explorador
/// y reparar las referencias de ExplorerComponentReceiver.
///
/// USO (con Explorador.unity ABIERTA):
///   Tools → TITA → Explorador → Configurar Assets Recientes
///
/// Assets que añade si no existen:
///   • [VFX_DeliveryTray]   → DeliveryTrayIndicator en Bandeja_Recepcion
///   • [UI_ValidationStation] → ValidationStationUI junto al VRValidationButton
///   • [Onboarding_Explorer]  → ExplorerOnboarding (tutorial de bienvenida)
///
/// Además repara ExplorerComponentReceiver:
///   • Asigna todos los prefabs Delivered_* desde Assets/Prefabs/Delivered/
///   • Asigna delivery (ComponentDeliverySystem) si falta
///   • Asigna puntoDeEntrega (Bandeja_Recepcion) si falta
/// </summary>
public static class ExplorerAssetsSetup
{
    const string PREFABS_PATH = "Assets/Prefabs/Delivered";

    [MenuItem("Tools/TITA/Explorador/Configurar Assets Recientes")]
    static void Setup()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!scene.name.ToLower().Contains("explorador"))
        {
            bool proceed = EditorUtility.DisplayDialog(
                "Escena activa no es Explorador",
                $"La escena activa es '{scene.name}'.\n" +
                "Abre Explorador.unity y vuelve a ejecutar.\n\n" +
                "¿Continuar de todas formas?",
                "Continuar", "Cancelar");
            if (!proceed) return;
        }

        int total = 0;
        total += SetupDeliveryTrayIndicator();
        total += SetupValidationStation();
        total += SetupOnboarding();
        total += SetupComponentReceiver();

        EditorSceneManager.MarkSceneDirty(scene);

        EditorUtility.DisplayDialog("Explorer Assets Setup",
            total > 0
                ? $"{total} cambios aplicados.\nGuarda la escena (Ctrl+S).\nRevisa la Consola para el detalle."
                : "Todos los assets ya estaban configurados correctamente.",
            "OK");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  DeliveryTrayIndicator
    // ─────────────────────────────────────────────────────────────────────────

    static int SetupDeliveryTrayIndicator()
    {
        if (Object.FindAnyObjectByType<DeliveryTrayIndicator>(FindObjectsInactive.Include) != null)
        {
            Debug.Log("[Explorer Setup] DeliveryTrayIndicator ya existe — omitido.");
            return 0;
        }

        Transform parent = null;
        var bandeja = GameObject.Find("Bandeja_Recepcion");
        if (bandeja != null) parent = bandeja.transform;

        var go = new GameObject("[VFX_DeliveryTray]");
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.localPosition = Vector3.zero;

        // LineRenderer requerido por [RequireComponent]
        go.AddComponent<LineRenderer>();
        go.AddComponent<DeliveryTrayIndicator>();

        Undo.RegisterCreatedObjectUndo(go, "Crear DeliveryTrayIndicator");
        Debug.Log($"[Explorer Setup] DeliveryTrayIndicator creado" +
                  (parent != null ? $" en '{parent.name}'" : " en raíz de escena"));
        return 1;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  ValidationStationUI
    // ─────────────────────────────────────────────────────────────────────────

    static int SetupValidationStation()
    {
        if (Object.FindAnyObjectByType<ValidationStationUI>(FindObjectsInactive.Include) != null)
        {
            Debug.Log("[Explorer Setup] ValidationStationUI ya existe — omitido.");
            return 0;
        }

        var vrBtn = Object.FindAnyObjectByType<VRValidationButton>(FindObjectsInactive.Include);
        if (vrBtn != null)
        {
            // Añadir al mismo GO del botón o crear hijo
            if (vrBtn.GetComponent<ValidationStationUI>() == null)
            {
                Undo.RecordObject(vrBtn.gameObject, "Añadir ValidationStationUI");
                vrBtn.gameObject.AddComponent<ValidationStationUI>();
                EditorUtility.SetDirty(vrBtn.gameObject);
                Debug.Log($"[Explorer Setup] ValidationStationUI añadido a '{vrBtn.name}'");
                return 1;
            }
        }

        // Fallback: crear GO independiente
        var go = new GameObject("[UI_ValidationStation]");
        go.AddComponent<ValidationStationUI>();
        Undo.RegisterCreatedObjectUndo(go, "Crear ValidationStationUI");
        Debug.Log("[Explorer Setup] ValidationStationUI creado en raíz de escena. " +
                  "Muévelo cerca del VRValidationButton.");
        return 1;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  ExplorerOnboarding
    // ─────────────────────────────────────────────────────────────────────────

    static int SetupOnboarding()
    {
        if (Object.FindAnyObjectByType<ExplorerOnboarding>(FindObjectsInactive.Include) != null)
        {
            Debug.Log("[Explorer Setup] ExplorerOnboarding ya existe — omitido.");
            return 0;
        }

        var go = new GameObject("[Onboarding_Explorer]");
        go.AddComponent<ExplorerOnboarding>();
        Undo.RegisterCreatedObjectUndo(go, "Crear ExplorerOnboarding");
        Debug.Log("[Explorer Setup] ExplorerOnboarding creado. " +
                  "Se muestra al iniciar, el jugador lo cierra con Trigger.");
        return 1;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  ExplorerComponentReceiver — prefabs + referencias
    // ─────────────────────────────────────────────────────────────────────────

    static int SetupComponentReceiver()
    {
        var receiver = Object.FindAnyObjectByType<ExplorerComponentReceiver>(FindObjectsInactive.Include);
        if (receiver == null)
        {
            Debug.LogWarning("[Explorer Setup] ExplorerComponentReceiver no encontrado en la escena.");
            return 0;
        }

        Undo.RecordObject(receiver, "Setup ExplorerComponentReceiver");
        int fixes = 0;

        // Cargar prefabs desde Assets/Prefabs/Delivered/ ─────────────────────
        fixes += TryAssignPrefab(ref receiver.resistorPrefab,   "Delivered_Resistor",   "resistorPrefab");
        fixes += TryAssignPrefab(ref receiver.ledPrefab,        "Delivered_LED",         "ledPrefab");
        fixes += TryAssignPrefab(ref receiver.capacitorPrefab,  "Delivered_Capacitor",   "capacitorPrefab");
        fixes += TryAssignPrefab(ref receiver.arduinoPinPrefab, "Delivered_ArduinoPIn",  "arduinoPinPrefab");

        // Variantes LED
        fixes += TryAssignPrefab(ref receiver.ledGreenPrefab,   "Delivered_LED_Green",   "ledGreenPrefab",   required: false);
        fixes += TryAssignPrefab(ref receiver.ledRedPrefab,     "Delivered_LED_Red",     "ledRedPrefab",     required: false);
        fixes += TryAssignPrefab(ref receiver.ledYellowPrefab,  "Delivered_LED_Yellow",  "ledYellowPrefab",  required: false);

        // Variantes Capacitor
        fixes += TryAssignPrefab(ref receiver.capacitorBluePrefab,  "Delivered_Capacitor_Blue",  "capacitorBluePrefab",  required: false);
        fixes += TryAssignPrefab(ref receiver.capacitorBlackPrefab, "Delivered_Capacitor_Black", "capacitorBlackPrefab", required: false);
        fixes += TryAssignPrefab(ref receiver.capacitorOrangePrefab,"Delivered_Capacitor_Orange","capacitorOrangePrefab",required: false);

        // Variante Resistor vertical
        fixes += TryAssignPrefab(ref receiver.resistorVerticalPrefab, "Delivered_Resistor_Vertical", "resistorVerticalPrefab", required: false);

        // delivery ─────────────────────────────────────────────────────────────
        if (receiver.delivery == null)
        {
            var cds = Object.FindAnyObjectByType<ComponentDeliverySystem>(FindObjectsInactive.Include);
            if (cds != null)
            {
                receiver.delivery = cds;
                fixes++;
                Debug.Log($"[Explorer Setup] delivery → {cds.name}");
            }
            else
            {
                // Crear ComponentDeliverySystem en escena si no existe
                var cdsGO = new GameObject("[ComponentDeliverySystem]");
                var newCds = cdsGO.AddComponent<ComponentDeliverySystem>();
                CopiarPrefabsAl(newCds, receiver);
                receiver.delivery = newCds;
                Undo.RegisterCreatedObjectUndo(cdsGO, "Crear ComponentDeliverySystem");
                Debug.Log("[Explorer Setup] ComponentDeliverySystem creado en escena " +
                          "(necesario para validación de instalación).");
                fixes += 2;
            }
        }

        // puntoDeEntrega ───────────────────────────────────────────────────────
        if (receiver.puntoDeEntrega == null)
        {
            var bandeja = GameObject.Find("Bandeja_Recepcion");
            if (bandeja != null)
            {
                receiver.puntoDeEntrega = bandeja.transform;
                fixes++;
                Debug.Log("[Explorer Setup] puntoDeEntrega → Bandeja_Recepcion");
            }
            else
            {
                var toolbox = Object.FindAnyObjectByType<ToolboxController>(FindObjectsInactive.Include);
                if (toolbox != null)
                {
                    receiver.puntoDeEntrega = toolbox.GetComponentSlot();
                    fixes++;
                    Debug.Log("[Explorer Setup] puntoDeEntrega → ToolboxController.GetComponentSlot()");
                }
                else
                {
                    Debug.LogWarning("[Explorer Setup] No se encontró 'Bandeja_Recepcion' ni ToolboxController. " +
                                     "Asigna puntoDeEntrega manualmente en el Inspector.");
                }
            }
        }

        // Asegurar que ComponentDeliverySystem.puntoDeEntrega también esté ─────
        if (receiver.delivery != null && receiver.delivery.puntoDeEntrega == null
            && receiver.puntoDeEntrega != null)
        {
            Undo.RecordObject(receiver.delivery, "Asignar puntoDeEntrega a CDS");
            receiver.delivery.puntoDeEntrega = receiver.puntoDeEntrega;
            EditorUtility.SetDirty(receiver.delivery);
            fixes++;
            Debug.Log("[Explorer Setup] CDS.puntoDeEntrega sincronizado con receiver.");
        }

        EditorUtility.SetDirty(receiver);

        if (fixes > 0)
            Debug.Log($"[Explorer Setup] ExplorerComponentReceiver: {fixes} referencias asignadas.");
        else
            Debug.Log("[Explorer Setup] ExplorerComponentReceiver ya estaba completamente configurado.");

        return fixes;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────────

    static int TryAssignPrefab(ref GameObject field, string prefabName,
                               string fieldName, bool required = true)
    {
        if (field != null) return 0;

        // Buscar por nombre exacto, luego parcial
        var guids = AssetDatabase.FindAssets($"t:Prefab {prefabName}", new[] { PREFABS_PATH });
        if (guids.Length == 0)
        {
            if (required) Debug.LogWarning($"[Explorer Setup] Prefab '{prefabName}' no encontrado en {PREFABS_PATH}");
            return 0;
        }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        field = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (field != null)
        {
            Debug.Log($"[Explorer Setup] {fieldName} → {System.IO.Path.GetFileName(path)}");
            return 1;
        }
        return 0;
    }

    static void CopiarPrefabsAl(ComponentDeliverySystem target, ExplorerComponentReceiver src)
    {
        if (src.resistorPrefab   != null) target.resistorPrefab   = src.resistorPrefab;
        if (src.ledPrefab        != null) target.ledPrefab        = src.ledPrefab;
        if (src.capacitorPrefab  != null) target.capacitorPrefab  = src.capacitorPrefab;
        if (src.arduinoPinPrefab != null) target.arduinoPinPrefab = src.arduinoPinPrefab;
    }
}
