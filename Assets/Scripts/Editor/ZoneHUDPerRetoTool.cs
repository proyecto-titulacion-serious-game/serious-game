using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Configura UN <see cref="ZoneHUDTrigger"/> por reto (Reto 1..4) en la escena del Explorador,
/// todos mostrando el MISMO conjunto de objetivos (ExplorerTelemetryHUD + Clipboard_VR), cada uno
/// gateado a su propio reto. Efecto: la telemetría y el clipboard de instrucciones se ven en
/// TODOS los retos (no solo en el 4), porque en cada momento la zona del reto activo los pide.
///
/// Reutiliza los triggers existentes y crea los que falten hasta tener 4 (uno por LevelType).
/// Cada trigger queda: modo=PorReto, reto = el suyo, targets = los objetivos compartidos,
/// startHidden=true, Collider isTrigger. Si hay más de 4, los sobrantes se desactivan.
///
/// Menú: Tools → TITA → Explorador → ZoneHUD: uno por reto (1-4)
/// </summary>
public static class ZoneHUDPerRetoTool
{
    static readonly LevelType[] Retos =
        { LevelType.OhmLaw, LevelType.Parallel, LevelType.Mixed, LevelType.Arduino };

    [MenuItem("Tools/TITA/Explorador/ZoneHUD: uno por reto (1-4)")]
    public static void Configurar()
    {
        var existentes = new List<ZoneHUDTrigger>(
            Object.FindObjectsByType<ZoneHUDTrigger>(FindObjectsInactive.Include));

        // ── Objetivos compartidos: tomar la unión de targets de los triggers actuales.
        var objetivos = RecolectarObjetivos(existentes);
        if (objetivos.Count == 0)
        {
            // Fallback: al menos el ExplorerTelemetryHUD si existe.
            var tele = Object.FindAnyObjectByType<ExplorerTelemetryHUD>(FindObjectsInactive.Include);
            if (tele != null) objetivos.Add(tele.gameObject);
        }
        if (objetivos.Count == 0)
        {
            EditorUtility.DisplayDialog("ZoneHUD por reto",
                "No encontré objetivos para mostrar (ni targets en triggers existentes ni un " +
                "ExplorerTelemetryHUD en la escena). Aborto para no crear triggers vacíos.", "OK");
            return;
        }

        var objetivosArr = objetivos.ToArray();
        var sb = new StringBuilder();

        // ── Asegurar 4 triggers, uno por reto.
        for (int i = 0; i < Retos.Length; i++)
        {
            ZoneHUDTrigger zt = (i < existentes.Count) ? existentes[i] : CrearTrigger(i);

            Undo.RecordObject(zt, "Config ZoneHUD por reto");
            zt.modo        = ZoneHUDTrigger.ActivationMode.PorReto;
            zt.reto        = Retos[i];
            zt.targets     = objetivosArr;
            zt.startHidden = true;

            // Collider trigger (lo exige RequireComponent; garantizar isTrigger).
            if (zt.TryGetComponent<Collider>(out var col))
            {
                Undo.RecordObject(col, "ZoneHUD collider trigger");
                col.isTrigger = true;
            }

            // Nombre claro y emparentar bajo su RetoX_Zone si existe (solo organización).
            zt.gameObject.name = $"ZoneHUD_Reto{i + 1}";
            var zona = BuscarPorNombre($"Reto{i + 1}_Zone");
            if (zona != null && zt.transform.parent != zona)
                Undo.SetTransformParent(zt.transform, zona, "Reparent ZoneHUD");

            EditorUtility.SetDirty(zt);
            sb.AppendLine($"  • ZoneHUD_Reto{i + 1} → reto={Retos[i]}");
        }

        // ── Desactivar triggers sobrantes (más de 4).
        int desactivados = 0;
        for (int i = Retos.Length; i < existentes.Count; i++)
        {
            var go = existentes[i].gameObject;
            Undo.RecordObject(go, "Desactivar ZoneHUD sobrante");
            go.SetActive(false);
            EditorUtility.SetDirty(go);
            desactivados++;
        }

        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        var nombresObj = new List<string>();
        foreach (var o in objetivos) nombresObj.Add(o.name);

        string msg = $"4 ZoneHUDTrigger configurados (uno por reto):\n{sb}\n" +
                     $"Objetivos compartidos en cada zona:\n  {string.Join("\n  ", nombresObj)}\n\n" +
                     "Se ven en TODOS los retos (cada zona los pide en su reto).";
        if (desactivados > 0)
            msg += $"\n\nTriggers sobrantes desactivados: {desactivados}.";

        EditorUtility.DisplayDialog("ZoneHUD por reto", msg, "OK");
        Debug.Log($"[ZoneHUDPerReto] 4 triggers configurados. Objetivos: {string.Join(", ", nombresObj)}. " +
                  $"Sobrantes desactivados: {desactivados}.");
    }

    // ─────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────

    static List<GameObject> RecolectarObjetivos(List<ZoneHUDTrigger> triggers)
    {
        var set = new List<GameObject>();
        foreach (var zt in triggers)
        {
            if (zt.targets == null) continue;
            foreach (var t in zt.targets)
                if (t != null && !set.Contains(t)) set.Add(t);
        }
        return set;
    }

    static ZoneHUDTrigger CrearTrigger(int index)
    {
        var go = new GameObject($"ZoneHUD_Reto{index + 1}");
        Undo.RegisterCreatedObjectUndo(go, "Crear ZoneHUD");
        // RequireComponent(Collider) no puede auto-añadir un tipo abstracto → añadir BoxCollider.
        var box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(2f, 2f, 2f);
        return go.AddComponent<ZoneHUDTrigger>();
    }

    static Transform BuscarPorNombre(string nombre)
    {
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            if (t.name == nombre) return t;
        return null;
    }
}
