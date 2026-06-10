using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Auto-cablea el controlador VRRig (IK de 3 puntos) sobre el Explorador y crea la capa
/// 'LocalHidden' usada por la decapitación local (Fase B).
///
///   Tools → TITA → Explorador → Conectar VRRig (3 puntos IK)
///   Tools → TITA → Explorador → Quitar VRRig
///
/// Resuelve automáticamente: cámara (headTarget + camaraLocal), manos (por HandModelController),
/// avatar Humanoid (RobotKyle_Explorer) y el suelo del rig (XR Origin).
/// </summary>
public static class RobotKyleVRRigSetup
{
    const string MENU_ADD    = "Tools/TITA/Explorador/Conectar VRRig (3 puntos IK)";
    const string MENU_REMOVE = "Tools/TITA/Explorador/Quitar VRRig";
    const string CAPA        = "LocalHidden";

    [MenuItem(MENU_ADD)]
    static void ConnectVRRig()
    {
        var ea = Object.FindAnyObjectByType<ExplorerAvatar>(FindObjectsInactive.Include);
        if (ea == null)
        {
            EditorUtility.DisplayDialog("TITA — VRRig",
                "No se encontró ExplorerAvatar en la escena. Abre Explorador.unity primero.", "OK");
            return;
        }

        // ── Avatar (RobotKyle_Explorer) + Animator Humanoid ──
        Transform avatarRoot = ea.avatarRoot;
        if (avatarRoot == null)
        {
            var go = GameObject.Find("RobotKyle_Explorer") ?? GameObject.Find("RobotKyle");
            if (go != null) avatarRoot = go.transform;
        }
        if (avatarRoot == null)
        {
            EditorUtility.DisplayDialog("TITA — VRRig", "No se encontró el avatar RobotKyle_Explorer.", "OK");
            return;
        }
        var animator = avatarRoot.GetComponentInChildren<Animator>();
        if (animator == null || !animator.isHuman)
        {
            EditorUtility.DisplayDialog("TITA — VRRig",
                $"El avatar '{avatarRoot.name}' no es Humanoid o no tiene Animator.", "OK");
            return;
        }

        // ── Cámara (head target + cámara local) ──
        Camera cam = Camera.main;
        if (cam == null)
            foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
                if (c.name.ToLower().Contains("camera")) { cam = c; break; }

        // ── Manos (por HandModelController izquierda/derecha) ──
        Transform leftHand = null, rightHand = null;
        foreach (var h in Object.FindObjectsByType<HandModelController>(FindObjectsInactive.Include))
        {
            if (h.hand == HandModelController.HandSide.Left) leftHand = h.transform;
            else                                             rightHand = h.transform;
        }

        // ── Suelo del rig (XR Origin) ──
        Transform piso = null;
        var xrGo = GameObject.Find("XR Origin (XR Rig)") ?? GameObject.Find("XR Origin");
        if (xrGo != null) piso = xrGo.transform;

        // ── Crear capa LocalHidden ──
        int capa = EnsureLayer(CAPA);

        // ── Añadir/obtener VRRig en el MISMO GO que ExplorerAvatar ──
        var rig = ea.GetComponent<VRRig>();
        bool created = false;
        if (rig == null) { rig = Undo.AddComponent<VRRig>(ea.gameObject); created = true; }
        else             { Undo.RecordObject(rig, "Configurar VRRig"); }

        rig.animator       = animator;
        rig.avatarRoot     = avatarRoot;
        rig.headTarget     = cam != null ? cam.transform : null;
        rig.camaraLocal    = cam;
        rig.leftHandTarget  = leftHand;
        rig.rightHandTarget = rightHand;
        rig.pisoReferencia = piso;
        rig.capaOculta     = CAPA;
        rig.seguirCuerpoConCamara = true;    // al girar la cámara, el cuerpo gira con ella
        rig.resolverBrazos        = true;    // el BRAZO del robot sigue al mando (IK)
        rig.usarManosDelAvatar    = true;    // usar las MANOS DEL PROPIO AVATAR (no las procedurales)
        rig.ocultarManosRobot     = false;   // (debe ser false: queremos VER las manos del robot)
        rig.estirarBrazo          = true;    // estira el brazo para que la mano llegue al mando
        rig.controlarDedos        = true;    // abrir/cerrar dedos del avatar con grip/gatillo
        rig.enabled        = true;

        EditorUtility.SetDirty(rig);
        EditorSceneManager.MarkSceneDirty(ea.gameObject.scene);
        Selection.activeGameObject = ea.gameObject;

        EditorUtility.DisplayDialog("TITA — VRRig",
            (created ? "VRRig añadido y conectado." : "VRRig ya existía; referencias actualizadas.") +
            $"\n\nAvatar: {avatarRoot.name}" +
            $"\nCámara: {(cam ? cam.name : "— (revisa)")}" +
            $"\nMano izq: {(leftHand ? leftHand.name : "—")}   Mano der: {(rightHand ? rightHand.name : "—")}" +
            $"\nSuelo (XR Origin): {(piso ? piso.name : "— → usa y=0")}" +
            $"\nCapa '{CAPA}': {(capa >= 0 ? "creada/ok (índice " + capa + ")" : "NO se pudo crear")}" +
            "\n\nVRRig desactivará ExplorerAvatar y RobotHandIK en Play (un solo controlador)." +
            "\nMANOS: procedurales (van pegadas al mando + dedos). El cuerpo gira con la cámara." +
            "\nAjusta headPositionOffset en el Inspector si la cabeza queda alta/baja. GUARDA (Ctrl+S).", "OK");

        Debug.Log($"[RobotKyleVRRigSetup] VRRig conectado. Avatar={avatarRoot.name}, Cam={(cam ? cam.name : "null")}, " +
                  $"L={(leftHand ? leftHand.name : "null")}, R={(rightHand ? rightHand.name : "null")}, Piso={(piso ? piso.name : "null")}, capa={capa}.");
    }

    [MenuItem(MENU_REMOVE)]
    static void RemoveVRRig()
    {
        var rig = Object.FindAnyObjectByType<VRRig>(FindObjectsInactive.Include);
        if (rig == null)
        {
            EditorUtility.DisplayDialog("TITA — VRRig", "No hay ningún VRRig en la escena.", "OK");
            return;
        }
        var scene = rig.gameObject.scene;
        Undo.DestroyObjectImmediate(rig);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorUtility.DisplayDialog("TITA — VRRig",
            "VRRig eliminado. Reactiva ExplorerAvatar / RobotHandIK si los quieres de vuelta. Guarda (Ctrl+S).", "OK");
    }

    // Crea la capa por nombre en el primer slot libre (8..31). Devuelve su índice o -1.
    static int EnsureLayer(string layerName)
    {
        int existing = LayerMask.NameToLayer(layerName);
        if (existing >= 0) return existing;

        var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0) return -1;

        var so = new SerializedObject(assets[0]);
        var layers = so.FindProperty("layers");
        if (layers == null) return -1;

        for (int i = 8; i < layers.arraySize; i++)
        {
            var el = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(el.stringValue))
            {
                el.stringValue = layerName;
                so.ApplyModifiedProperties();
                return i;
            }
        }
        return -1;
    }
}
