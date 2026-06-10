#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Configura el ComponentReceiver como bandeja CANÓNICA y agarrable del Explorador:
///   1. Instancia ComponentReceiver.prefab en Explorador.unity si no está.
///   2. Hace agarrable el ROOT (Rigidbody kinematic + Collider + XRGrabInteractable). Se usa el root
///      —no el Tray_Visual achatado— porque el modo híbrido EMPARIENTA los componentes a la bandeja
///      y una escala no uniforme los deformaría. Tray_Visual sigue siendo el visual (hijo del root).
///   3. Cablea ExplorerComponentReceiver.puntoDeEntrega y ComponentDeliverySystem.puntoDeEntrega al
///      root, y receiver.delivery al ComponentDeliverySystem de la escena.
///
/// NO borra el path antiguo (Bandeja_Recepcion del jugador): es reversible. Tras confirmar en Play
/// que el flujo funciona, se puede retirar ese path en un segundo paso.
///
/// Menú:  Tools → TITA → Explorador → Configurar ComponentReceiver (caja agarrable + híbrido)
/// Batch: -executeMethod ComponentReceiverSetupTool.SetupBatch
/// </summary>
public static class ComponentReceiverSetupTool
{
    const string MENU   = "Tools/TITA/Explorador/Configurar ComponentReceiver (caja agarrable + híbrido)";
    const string ESCENA = "Assets/Scenes/Explorador.unity";
    const string PREFAB = "Assets/Prefabs/ComponentReceiver.prefab";

    [MenuItem(MENU)]
    static void Menu()
    {
        bool ok = DoSetup(out string msg);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("TITA — ComponentReceiver", msg +
            "\n\nGUARDA la escena (Ctrl+S) y PRUEBA en Play/VR (esto no lo puedo verificar yo).", "OK");
        Debug.Log($"[ComponentReceiverSetupTool] {(ok ? "OK" : "AVISO")}: {msg}");
    }

    /// <summary>Entrada batch: abre Explorador.unity, configura, guarda y sale.</summary>
    public static void SetupBatch()
    {
        var scene = EditorSceneManager.OpenScene(ESCENA, OpenSceneMode.Single);
        bool ok = DoSetup(out string msg);
        if (ok)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        Debug.Log($"[ComponentReceiverSetupTool] RESULTADO: {msg}");
        EditorApplication.Exit(ok ? 0 : 1);
    }

    static bool DoSetup(out string msg)
    {
        // ── 1. Instanciar el prefab si no hay receiver en escena ──────────────
        var receiver = Object.FindAnyObjectByType<ExplorerComponentReceiver>(FindObjectsInactive.Include);
        bool creado = false;
        if (receiver == null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB);
            if (prefab == null) { msg = $"No se encontró {PREFAB}."; return false; }

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(inst, "Instanciar ComponentReceiver");
            inst.transform.position = PosicionSugerida();
            receiver = inst.GetComponent<ExplorerComponentReceiver>();
            creado = true;
        }
        var root = receiver.gameObject;

        // ── 2. ROOT agarrable (escala uniforme → no deforma a los hijos del híbrido) ──
        var rb = root.GetComponent<Rigidbody>();
        if (rb == null) rb = Undo.AddComponent<Rigidbody>(root);
        rb.isKinematic = true;    // descansa donde lo dejes; XRI lo mueve al agarrar
        rb.useGravity  = false;

        // Reutiliza el BoxCollider del prefab (trigger) para el agarre; XRI acepta triggers.
        if (root.GetComponent<Collider>() == null)
        {
            var bc = Undo.AddComponent<BoxCollider>(root);
            bc.size   = new Vector3(0.28f, 0.16f, 0.24f);
            bc.center = new Vector3(0f, 0.07f, 0f);
        }
        if (root.GetComponent<XRGrabInteractable>() == null)
            Undo.AddComponent<XRGrabInteractable>(root);

        // ── 3. Cablear receiver + delivery → ROOT ─────────────────────────────
        var delivery = Object.FindAnyObjectByType<ComponentDeliverySystem>(FindObjectsInactive.Include);

        Undo.RecordObject(receiver, "Cablear ComponentReceiver");
        receiver.puntoDeEntrega     = root.transform;   // híbrido: los componentes se emparentan aquí
        receiver.modoBandejaHibrida = true;
        if (delivery != null) receiver.delivery = delivery;
        EditorUtility.SetDirty(receiver);

        if (delivery != null)
        {
            // El "fantasma" del delivery también en la caja (el receiver lo cancela, pero por limpieza).
            Undo.RecordObject(delivery, "puntoDeEntrega → caja");
            delivery.puntoDeEntrega = root.transform;
            EditorUtility.SetDirty(delivery);
        }

        Selection.activeGameObject = root;

        msg = (creado ? "ComponentReceiver INSTANCIADO" : "ComponentReceiver encontrado") +
              $" en {root.transform.position}.\n" +
              "• Root agarrable: Rigidbody (kinematic) + Collider + XRGrabInteractable\n" +
              "• puntoDeEntrega (receiver + delivery) → root con escala uniforme (modo híbrido)\n" +
              $"• delivery: {(delivery != null ? "cableado ✓" : "NO se encontró ComponentDeliverySystem")}\n" +
              "• Path Bandeja_Recepcion del jugador: INTACTO (reversible)\n\n" +
              "PENDIENTE de prueba en Play/VR: agarrar la caja y moverla, recibir un componente " +
              "(debe aparecer pegado a la caja), agarrarlo con la mano (debe soltarse) e instalarlo.";
        return delivery != null || creado;
    }

    // Posición razonable y alcanzable: junto a la protoboard.
    static Vector3 PosicionSugerida()
    {
        var proto = Object.FindAnyObjectByType<ProtoboardSimulator>();
        if (proto != null) return proto.transform.position + Vector3.up * 0.15f + proto.transform.right * 0.40f;

        var cam = Camera.main;
        if (cam != null) return cam.transform.position + cam.transform.forward * 0.6f - Vector3.up * 0.2f;

        return new Vector3(0f, 1f, 0.5f);
    }
}
#endif
