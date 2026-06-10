using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Herramientas de editor para conectar (o quitar) las manos de RobotKyle al IK de los
/// mandos VR de forma PERMANENTE en la escena Explorador, para poder ajustar los offsets
/// de rotación de palma en el Inspector sin tener que estar en modo Play.
///
///   Tools → TITA → Explorador → Conectar manos RobotKyle (IK permanente)
///   Tools → TITA → Explorador → Quitar IK de manos RobotKyle
///
/// El runtime (ExplorerVRAutoFix) sigue funcionando igual: si el RobotHandIK ya está
/// puesto, solo re-cablea referencias y oculta las manos procedurales en Play. Los
/// offsets que ajustes aquí se conservan (el runtime no los toca).
/// </summary>
public static class RobotKyleHandIKSetup
{
    const string MENU_ADD    = "Tools/TITA/Explorador/Conectar manos RobotKyle (IK permanente)";
    const string MENU_REMOVE = "Tools/TITA/Explorador/Quitar IK de manos RobotKyle";

    [MenuItem(MENU_ADD)]
    static void ConnectHandIK()
    {
        var ea = Object.FindAnyObjectByType<ExplorerAvatar>(FindObjectsInactive.Include);
        if (ea == null)
        {
            EditorUtility.DisplayDialog("TITA — IK de manos",
                "No se encontró ExplorerAvatar en la escena.\n\n" +
                "Abre la escena Explorador.unity antes de ejecutar esta herramienta.", "OK");
            return;
        }

        // 1) Resolver el avatar (RobotKyle_Explorer) y validar que sea Humanoid.
        Transform root = ea.avatarRoot;
        if (root == null)
        {
            var go = GameObject.Find("RobotKyle_Explorer") ?? GameObject.Find("RobotKyle");
            if (go != null) root = go.transform;
        }
        if (root == null)
        {
            EditorUtility.DisplayDialog("TITA — IK de manos",
                "No se encontró el avatar RobotKyle (avatarRoot de ExplorerAvatar está vacío " +
                "y no existe un GameObject 'RobotKyle_Explorer').", "OK");
            return;
        }

        var anim = root.GetComponentInChildren<Animator>();
        if (anim == null || !anim.isHuman)
        {
            EditorUtility.DisplayDialog("TITA — IK de manos",
                $"El avatar '{root.name}' no es Humanoid o no tiene Animator.\n\n" +
                "El IK de dos huesos necesita un avatar Humanoid.", "OK");
            return;
        }

        // 2) Localizar los mandos por sus HandModelController (izquierda / derecha).
        Transform leftCtrl = null, rightCtrl = null;
        var hands = Object.FindObjectsByType<HandModelController>(FindObjectsInactive.Include);
        foreach (var h in hands)
        {
            if (h.hand == HandModelController.HandSide.Left) leftCtrl = h.transform;
            else                                             rightCtrl = h.transform;
        }
        if (leftCtrl == null && rightCtrl == null)
        {
            EditorUtility.DisplayDialog("TITA — IK de manos",
                "No se encontraron las manos (HandModelController) en la escena.", "OK");
            return;
        }

        // 3) Añadir/obtener RobotHandIK en el mismo GO que ExplorerAvatar y cablearlo.
        var ik = ea.GetComponent<RobotHandIK>();
        bool created = false;
        if (ik == null)
        {
            ik = Undo.AddComponent<RobotHandIK>(ea.gameObject);
            created = true;
        }
        else
        {
            Undo.RecordObject(ik, "Configurar RobotHandIK");
        }

        ik.avatarRoot      = root;
        ik.leftController  = leftCtrl;
        ik.rightController = rightCtrl;
        ik.enabled         = true;

        EditorUtility.SetDirty(ik);
        EditorSceneManager.MarkSceneDirty(ea.gameObject.scene);

        // Selecciona el componente para que puedas ajustar los offsets de inmediato.
        Selection.activeGameObject = ea.gameObject;

        EditorUtility.DisplayDialog("TITA — IK de manos",
            (created ? "RobotHandIK añadido y conectado." : "RobotHandIK ya existía; referencias actualizadas.") +
            $"\n\nAvatar: {root.name}" +
            $"\nMano izquierda: {(leftCtrl ? leftCtrl.name : "—")}" +
            $"\nMano derecha: {(rightCtrl ? rightCtrl.name : "—")}" +
            "\n\nAjusta 'leftHandRotOffset' / 'rightHandRotOffset' en el Inspector " +
            "(seleccionado: Explorer_Player). Recuerda GUARDAR la escena (Ctrl+S).", "OK");

        Debug.Log("[RobotKyleHandIKSetup] RobotHandIK conectado de forma permanente en " +
                  $"'{ea.name}'. Avatar={root.name}, L={leftCtrl?.name}, R={rightCtrl?.name}.");
    }

    [MenuItem(MENU_REMOVE)]
    static void RemoveHandIK()
    {
        var ea = Object.FindAnyObjectByType<ExplorerAvatar>(FindObjectsInactive.Include);
        var ik = ea != null ? ea.GetComponent<RobotHandIK>() : Object.FindAnyObjectByType<RobotHandIK>(FindObjectsInactive.Include);

        if (ik == null)
        {
            EditorUtility.DisplayDialog("TITA — IK de manos",
                "No hay ningún RobotHandIK en la escena que quitar.", "OK");
            return;
        }

        var scene = ik.gameObject.scene;
        Undo.DestroyObjectImmediate(ik);
        EditorSceneManager.MarkSceneDirty(scene);

        EditorUtility.DisplayDialog("TITA — IK de manos",
            "RobotHandIK eliminado. Las manos procedurales vuelven a ser las visibles.\n\n" +
            "Recuerda GUARDAR la escena (Ctrl+S).", "OK");
    }
}
