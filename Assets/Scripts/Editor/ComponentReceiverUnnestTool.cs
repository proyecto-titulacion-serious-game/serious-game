#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// "Des-anida" la bandeja del Explorador: el ComponentReceiver venía NESTED dentro del prefab
/// Explorer_Player (la caja seguía al jugador → no se podía dejar en cualquier sitio). En vez de
/// hacer cirugía sobre el prefab anidado (riesgoso), esta herramienta:
///   1. Desactiva TODOS los ExplorerComponentReceiver existentes (el anidado en el player) para que
///      no dupliquen el spawn.
///   2. Instancia un ComponentReceiver.prefab NUEVO como objeto SUELTO de escena (root), junto a la
///      protoboard, agarrable (Rigidbody kinematic + Collider + XRGrabInteractable).
///   3. Cablea su puntoDeEntrega → su propio root (escala uniforme → híbrido), delivery → el CDS de
///      la escena, y reapunta ComponentDeliverySystem.puntoDeEntrega a la nueva caja.
///
/// Resultado: una sola caja independiente que el Explorador agarra, lleva y DEJA donde quiera.
/// Reversible: el receiver anidado solo queda desactivado (no borrado).
///
/// Menú:  Tools → TITA → Explorador → Des-anidar caja (ComponentReceiver suelto)
/// Batch: -executeMethod ComponentReceiverUnnestTool.UnnestBatch
/// </summary>
public static class ComponentReceiverUnnestTool
{
    const string MENU   = "Tools/TITA/Explorador/Des-anidar caja (ComponentReceiver suelto)";
    const string ESCENA = "Assets/Scenes/Explorador.unity";
    const string PREFAB = "Assets/Prefabs/ComponentReceiver.prefab";

    [MenuItem(MENU)]
    static void Menu()
    {
        bool ok = DoUnnest(out string msg);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorUtility.DisplayDialog("TITA — Des-anidar caja", msg +
            "\n\nGUARDA la escena (Ctrl+S) y PRUEBA en Play/VR.", "OK");
        Debug.Log($"[ComponentReceiverUnnestTool] {(ok ? "OK" : "AVISO")}: {msg}");
    }

    public static void UnnestBatch()
    {
        var scene = EditorSceneManager.OpenScene(ESCENA, OpenSceneMode.Single);
        bool ok = DoUnnest(out string msg);
        if (ok)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        Debug.Log($"[ComponentReceiverUnnestTool] RESULTADO: {msg}");
        EditorApplication.Exit(ok ? 0 : 1);
    }

    static bool DoUnnest(out string msg)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB);
        if (prefab == null) { msg = $"No se encontró {PREFAB}."; return false; }

        // ── 1. Desactivar receivers existentes (el anidado en el player) ──────
        int desactivados = 0;
        foreach (var r in Object.FindObjectsByType<ExplorerComponentReceiver>(FindObjectsInactive.Include))
        {
            if (r == null) continue;
            Undo.RecordObject(r.gameObject, "Desactivar receiver anidado");
            r.gameObject.SetActive(false);
            EditorUtility.SetDirty(r.gameObject);
            desactivados++;
        }

        // ── 2. Instanciar uno NUEVO, suelto en la escena ──────────────────────
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(inst, "Instanciar caja independiente");
        inst.name = "ComponentReceiver_Caja";
        inst.transform.SetParent(null, worldPositionStays: false);   // root de escena (independiente)
        inst.transform.position = PosicionSugerida();
        var receiver = inst.GetComponent<ExplorerComponentReceiver>();

        // ── 3. Root agarrable (escala uniforme → híbrido sin deformar) ────────
        var rb = inst.GetComponent<Rigidbody>();
        if (rb == null) rb = Undo.AddComponent<Rigidbody>(inst);
        rb.isKinematic = true;     // descansa donde lo dejes; XRI lo mueve al agarrar
        rb.useGravity  = false;

        if (inst.GetComponent<Collider>() == null)
        {
            var bc = Undo.AddComponent<BoxCollider>(inst);
            bc.size   = new Vector3(0.28f, 0.16f, 0.24f);
            bc.center = new Vector3(0f, 0.07f, 0f);
        }
        if (inst.GetComponent<XRGrabInteractable>() == null)
            Undo.AddComponent<XRGrabInteractable>(inst);

        // ── 4. Cablear receiver + delivery → la nueva caja ────────────────────
        var delivery = Object.FindAnyObjectByType<ComponentDeliverySystem>(FindObjectsInactive.Include);

        Undo.RecordObject(receiver, "Cablear caja independiente");
        receiver.puntoDeEntrega     = inst.transform;
        receiver.modoBandejaHibrida = true;
        if (delivery != null) receiver.delivery = delivery;
        EditorUtility.SetDirty(receiver);

        if (delivery != null)
        {
            Undo.RecordObject(delivery, "puntoDeEntrega → caja independiente");
            delivery.puntoDeEntrega = inst.transform;
            EditorUtility.SetDirty(delivery);
        }

        Selection.activeGameObject = inst;

        msg = $"Caja independiente '{inst.name}' creada en {inst.transform.position}.\n" +
              $"• Receivers anidados desactivados: {desactivados} (reversible)\n" +
              "• Root suelto y agarrable: Rigidbody (kinematic) + Collider + XRGrabInteractable\n" +
              "• puntoDeEntrega (receiver + delivery) → la caja; modo híbrido ON\n" +
              $"• delivery: {(delivery != null ? "cableado ✓" : "NO se encontró ComponentDeliverySystem")}\n\n" +
              "PENDIENTE prueba Play/VR: agarrar la caja, moverla, DEJARLA en otro sitio (debe quedarse), " +
              "recibir componente (pegado a la caja), agarrarlo (se suelta) e instalar.";
        return true;
    }

    static Vector3 PosicionSugerida()
    {
        var proto = Object.FindAnyObjectByType<ProtoboardSimulator>();
        if (proto != null) return proto.transform.position + Vector3.up * 0.05f + proto.transform.right * 0.50f;

        var cam = Camera.main;
        if (cam != null) return cam.transform.position + cam.transform.forward * 0.6f - Vector3.up * 0.2f;

        return new Vector3(0f, 1f, 0.5f);
    }
}
#endif
