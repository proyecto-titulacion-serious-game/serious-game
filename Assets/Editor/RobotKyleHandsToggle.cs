using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Alterna entre las manos del PROPIO avatar RobotKyle (brazos por IK + dedos con grip/gatillo)
/// y las manos PROCEDURALES, sobre el VRRig de la escena del Explorador.
///
///   Tools → TITA → Explorador → Manos del Kyle: ACTIVAR (avatar + dedos)
///   Tools → TITA → Explorador → Manos del Kyle: DESACTIVAR (procedurales)
///
/// No re-cablea el VRRig: solo flipea los toggles. Si aún no hay VRRig en la escena, ACTIVAR
/// lo conecta primero (RobotKyleVRRigSetup.ConnectVRRig, que ya deja las manos del avatar listas).
/// </summary>
public static class RobotKyleHandsToggle
{
    const string MENU_ON  = "Tools/TITA/Explorador/Manos del Kyle: ACTIVAR (avatar + dedos)";
    const string MENU_OFF = "Tools/TITA/Explorador/Manos del Kyle: DESACTIVAR (procedurales)";

    [MenuItem(MENU_ON)]
    static void Activar()
    {
        var rig = Object.FindAnyObjectByType<VRRig>(FindObjectsInactive.Include);
        if (rig == null)
        {
            // No hay VRRig todavía → conectarlo (ya configura las manos del avatar).
            RobotKyleVRRigSetup.ConnectVRRig();
            rig = Object.FindAnyObjectByType<VRRig>(FindObjectsInactive.Include);
            if (rig == null) return; // ConnectVRRig ya avisó del error (avatar/escena faltante)
        }

        Undo.RecordObject(rig, "Activar manos del Kyle");
        rig.usarManosDelAvatar = true;   // usar las MANOS DEL PROPIO AVATAR
        rig.resolverBrazos     = true;   // el brazo del robot sigue al mando (IK)
        rig.estirarBrazo       = true;   // estira el brazo para que la mano llegue al mando
        rig.controlarDedos     = true;   // abrir/cerrar dedos con grip/gatillo (pisa al Animator)
        rig.ocultarManosRobot  = false;  // queremos VER las manos del robot
        rig.enabled            = true;
        EditorUtility.SetDirty(rig);
        EditorSceneManager.MarkSceneDirty(rig.gameObject.scene);
        Selection.activeGameObject = rig.gameObject;

        EditorUtility.DisplayDialog("TITA — Manos del Kyle",
            "Manos del avatar RobotKyle ACTIVADAS.\n\n" +
            "• usarManosDelAvatar = true\n" +
            "• resolverBrazos = true\n" +
            "• estirarBrazo = true\n" +
            "• controlarDedos = true\n" +
            "• ocultarManosRobot = false\n\n" +
            "Entra en Play (o build a la Quest) para probar.\n" +
            "Si los dedos doblan raro, ajusta 'ejeCurlDedos' / 'curlDedosMax' en el VRRig EN VIVO " +
            "(prueba ejes (0,0,1),(0,1,0),(1,0,0) y signos).\n\nGUARDA (Ctrl+S).", "OK");

        Debug.Log("[RobotKyleHandsToggle] Manos del avatar ACTIVADAS en el VRRig.");
    }

    [MenuItem(MENU_OFF)]
    static void Desactivar()
    {
        var rig = Object.FindAnyObjectByType<VRRig>(FindObjectsInactive.Include);
        if (rig == null)
        {
            EditorUtility.DisplayDialog("TITA — Manos del Kyle",
                "No hay ningún VRRig en la escena. Nada que desactivar.", "OK");
            return;
        }

        Undo.RecordObject(rig, "Desactivar manos del Kyle");
        rig.usarManosDelAvatar = false;  // mostrar las manos PROCEDURALES
        rig.ocultarManosRobot  = true;   // esconder las del robot para no verlas duplicadas
        EditorUtility.SetDirty(rig);
        EditorSceneManager.MarkSceneDirty(rig.gameObject.scene);

        EditorUtility.DisplayDialog("TITA — Manos del Kyle",
            "Vuelto a manos PROCEDURALES.\n\n" +
            "• usarManosDelAvatar = false\n" +
            "• ocultarManosRobot = true\n\n" +
            "GUARDA (Ctrl+S).", "OK");

        Debug.Log("[RobotKyleHandsToggle] Manos procedurales restauradas en el VRRig.");
    }
}
