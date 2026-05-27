using UnityEngine;
using UnityEditor;

/// <summary>
/// Herramientas para configurar y diagnosticar el TechnicianRobot (RobotKyle) en la escena.
/// Menú: Tools → TITA → TechnicianRobot
/// </summary>
public static class TechnicianRobotSetup
{
    const string ROBOT_NAME = "TechnicianRobot";
    const string HEAD_BONE  = "Head";

    // ─── Menú principal ───────────────────────────────────────────────────────

    [MenuItem("Tools/TITA/TechnicianRobot/Autoconfigurar referencias")]
    static void AutoSetup()
    {
        var robot = FindRobot();
        if (robot == null) return;

        int changes = 0;
        changes += AssignHeadBone(robot);
        changes += AssignToWorkstationSeats(robot);

        Debug.Log($"[TechnicianRobotSetup] Autoconfiguración completada — {changes} referencia(s) asignada(s).");
    }

    [MenuItem("Tools/TITA/TechnicianRobot/Validar setup")]
    static void ValidateSetup()
    {
        bool ok = true;

        // ── Robot en escena ──
        var robot = GameObject.Find(ROBOT_NAME);
        if (robot == null)
        {
            Debug.LogError($"[TechnicianRobotSetup] '{ROBOT_NAME}' no encontrado en la escena activa.");
            ok = false;
        }
        else
        {
            Debug.Log($"[TechnicianRobotSetup] ✓ {ROBOT_NAME} encontrado: {GetPath(robot.transform)}");

            // ── Animator ──
            var anim = robot.GetComponent<Animator>();
            if (anim == null || anim.avatar == null)
                Debug.LogWarning($"[TechnicianRobotSetup] {ROBOT_NAME} no tiene Animator o Avatar asignado.");
            else
                Debug.Log($"[TechnicianRobotSetup] ✓ Animator OK — avatar: {anim.avatar.name}");

            // ── Hueso Head ──
            var head = FindChildByName(robot.transform, HEAD_BONE);
            if (head == null)
                Debug.LogError($"[TechnicianRobotSetup] Hueso '{HEAD_BONE}' no encontrado dentro de {ROBOT_NAME}.");
            else
                Debug.Log($"[TechnicianRobotSetup] ✓ Hueso '{HEAD_BONE}' encontrado: {GetPath(head)}");
        }

        // ── ThirdPersonCamera ──
        var tpc = Object.FindAnyObjectByType<ThirdPersonCamera>(FindObjectsInactive.Include);
        if (tpc == null)
        {
            Debug.LogError("[TechnicianRobotSetup] ThirdPersonCamera no encontrada en la escena.");
            ok = false;
        }
        else
        {
            if (tpc.headBone == null)
                Debug.LogWarning($"[TechnicianRobotSetup] ThirdPersonCamera.headBone no asignado — ejecuta 'Autoconfigurar referencias'.");
            else
                Debug.Log($"[TechnicianRobotSetup] ✓ ThirdPersonCamera.headBone → '{tpc.headBone.name}'");
        }

        // ── WorkstationSeats ──
        var seats = Object.FindObjectsByType<WorkstationSeat>(FindObjectsInactive.Include);
        if (seats.Length == 0)
        {
            Debug.LogWarning("[TechnicianRobotSetup] No hay WorkstationSeat en la escena.");
        }
        else
        {
            foreach (var seat in seats)
            {
                if (seat.technicianRobot == null)
                    Debug.LogWarning($"[TechnicianRobotSetup] WorkstationSeat '{seat.gameObject.name}' no tiene technicianRobot asignado.");
                else
                    Debug.Log($"[TechnicianRobotSetup] ✓ {seat.gameObject.name}.technicianRobot → '{seat.technicianRobot.name}'");
            }
        }

        if (ok) Debug.Log("[TechnicianRobotSetup] Setup validado sin errores críticos.");
    }

    [MenuItem("Tools/TITA/TechnicianRobot/Imprimir jerarquía de huesos")]
    static void PrintBoneHierarchy()
    {
        var robot = FindRobot();
        if (robot == null) return;

        // Buscar raíz del esqueleto (hijo llamado "Skeleton" o "Hips")
        Transform skeletonRoot = FindChildByName(robot.transform, "Skeleton")
                              ?? FindChildByName(robot.transform, "Hips");

        if (skeletonRoot == null)
        {
            Debug.LogWarning($"[TechnicianRobotSetup] No se encontró 'Skeleton' ni 'Hips' en {ROBOT_NAME}. Imprimiendo desde la raíz.");
            skeletonRoot = robot.transform;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"── Jerarquía de huesos: {ROBOT_NAME} ──");
        AppendHierarchy(skeletonRoot, sb, 0);
        Debug.Log(sb.ToString());
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    static GameObject FindRobot()
    {
        var go = GameObject.Find(ROBOT_NAME);
        if (go == null)
            Debug.LogError($"[TechnicianRobotSetup] '{ROBOT_NAME}' no encontrado. " +
                           "Asegúrate de que la escena NoonA esté cargada y el objeto esté activo.");
        return go;
    }

    static int AssignHeadBone(GameObject robot)
    {
        var tpc = Object.FindAnyObjectByType<ThirdPersonCamera>(FindObjectsInactive.Include);
        if (tpc == null)
        {
            Debug.LogWarning("[TechnicianRobotSetup] ThirdPersonCamera no encontrada — no se asignó headBone.");
            return 0;
        }

        var head = FindChildByName(robot.transform, HEAD_BONE);
        if (head == null)
        {
            Debug.LogError($"[TechnicianRobotSetup] Hueso '{HEAD_BONE}' no encontrado en {ROBOT_NAME}.");
            return 0;
        }

        if (tpc.headBone == head)
        {
            Debug.Log("[TechnicianRobotSetup] ThirdPersonCamera.headBone ya estaba asignado correctamente.");
            return 0;
        }

        Undo.RecordObject(tpc, "Asignar headBone a ThirdPersonCamera");
        tpc.headBone = head;
        EditorUtility.SetDirty(tpc);
        Debug.Log($"[TechnicianRobotSetup] ThirdPersonCamera.headBone → '{head.name}' ({GetPath(head)})");
        return 1;
    }

    static int AssignToWorkstationSeats(GameObject robot)
    {
        int count = 0;
        foreach (var seat in Object.FindObjectsByType<WorkstationSeat>(FindObjectsInactive.Include))
        {
            if (seat.technicianRobot != null) continue;

            Undo.RecordObject(seat, "Asignar technicianRobot a WorkstationSeat");
            seat.technicianRobot = robot;
            EditorUtility.SetDirty(seat);
            Debug.Log($"[TechnicianRobotSetup] '{seat.gameObject.name}'.technicianRobot → '{robot.name}'");
            count++;
        }
        return count;
    }

    static Transform FindChildByName(Transform root, string name)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (string.Equals(t.name, name, System.StringComparison.OrdinalIgnoreCase))
                return t;
        return null;
    }

    static void AppendHierarchy(Transform t, System.Text.StringBuilder sb, int depth)
    {
        string indent = new string(' ', depth * 2);
        string marker = depth == 0 ? "" : (t.childCount > 0 ? "├─ " : "└─ ");
        sb.AppendLine($"{indent}{marker}{t.name}");
        foreach (Transform child in t)
            AppendHierarchy(child, sb, depth + 1);
    }

    static string GetPath(Transform t)
    {
        var parts = new System.Collections.Generic.List<string>();
        while (t != null) { parts.Insert(0, t.name); t = t.parent; }
        return string.Join("/", parts);
    }
}
