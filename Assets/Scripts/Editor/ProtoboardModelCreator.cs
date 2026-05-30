#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Genera el modelo 3D primitivo de la protoboard con:
///   - Base (tabla blanca/beige)
///   - Franja divisoria central
///   - Rieles VCC (rojo) y GND (negro) reposicionados en bordes superior/inferior
///   - VoltageSource conectada entre los rieles
///   - Repositiona todos los slots existentes en [ProtoboardSlots]
///
/// Tools > TITA > Crear Modelo 3D Protoboard
/// </summary>
public static class ProtoboardModelCreator
{
    // Dimensiones base — deben coincidir con las del generador de slots
    const int   FILAS   = 10;
    const int   COLS    = 5;
    const float SPACING = 0.018f;

    // Padding alrededor de los slots
    const float PAD_X   = 0.015f;
    const float PAD_Z   = 0.020f;
    const float GROSOR  = 0.007f;  // espesor de la tabla
    const float SLOT_Y  = 0.002f;  // altura de los cilindros de slot

    [MenuItem("Tools/TITA/Crear Modelo 3D Protoboard")]
    static void CrearModelo()
    {
        // ── Buscar [ProtoboardSlots] ──────────────────────────────────────
        var slotsRoot = GameObject.Find("[ProtoboardSlots]");
        if (slotsRoot == null)
        {
            EditorUtility.DisplayDialog("Error",
                "No se encontró '[ProtoboardSlots]' en la escena.\n" +
                "Ejecuta primero 'Tools > TITA > Reto 4 — Auto-Setup Completo'.", "OK");
            return;
        }

        // ── Crear raíz Protoboard si no existe ────────────────────────────
        var protoRoot = slotsRoot.transform.parent?.gameObject;
        if (protoRoot == null || protoRoot == slotsRoot)
        {
            protoRoot = new GameObject("Protoboard_Model");
            Undo.RegisterCreatedObjectUndo(protoRoot, "Crear Protoboard_Model");
            protoRoot.transform.position = slotsRoot.transform.position;
            slotsRoot.transform.SetParent(protoRoot.transform, true);
        }

        // ── Dimensiones de la tabla ───────────────────────────────────────
        // La cuadrícula principal ocupa:
        //   X: COLS * SPACING   = 0..COLS*SPACING
        //   Z: FILAS * SPACING  = 0..-FILAS*SPACING
        // Los rieles de alimentación añaden 2 filas extra en bordes superior e inferior
        float boardW = COLS  * SPACING + PAD_X * 2f;            // ancho X
        float boardD = (FILAS + 4) * SPACING + PAD_Z * 2f;      // fondo Z (+4 = 2 rieles arriba + 2 abajo)
        float boardH = GROSOR;

        // Offset del centro de la tabla respecto al slot (0,0,0)
        // El slot [0,0] está en la esquina superior izquierda.
        // Centramos la tabla bajo la cuadrícula incluyendo los rieles.
        float centerX = (COLS  - 1) * SPACING * 0.5f;
        float centerZ = -(FILAS + 1) * SPACING * 0.5f;          // centro incluyendo rieles arriba/abajo

        // ── BASE de la tabla ──────────────────────────────────────────────
        var baseGO = CrearPrimitivo(protoRoot.transform, "Base_Protoboard",
            PrimitiveType.Cube,
            new Vector3(centerX, -GROSOR * 0.5f, centerZ),
            new Vector3(boardW, boardH, boardD),
            new Color(0.93f, 0.90f, 0.78f));   // beige/marfil

        // ── FRANJA divisoria central (separación A-E / F-J) ───────────────
        float midZ  = -(FILAS * 0.5f) * SPACING;
        CrearPrimitivo(protoRoot.transform, "Franja_Central",
            PrimitiveType.Cube,
            new Vector3(centerX, SLOT_Y * 0.5f, midZ),
            new Vector3(boardW - 0.004f, SLOT_Y * 0.3f, 0.006f),
            new Color(0.6f, 0.6f, 0.6f));

        // ── MARCAS de numeración (laterales decorativos) ──────────────────
        CrearPrimitivo(protoRoot.transform, "Marca_Izq",
            PrimitiveType.Cube,
            new Vector3(-PAD_X * 0.5f, SLOT_Y * 0.3f, centerZ),
            new Vector3(0.003f, 0.001f, boardD - 0.006f),
            new Color(0.3f, 0.3f, 0.3f));

        CrearPrimitivo(protoRoot.transform, "Marca_Der",
            PrimitiveType.Cube,
            new Vector3((COLS - 1) * SPACING + PAD_X * 0.5f, SLOT_Y * 0.3f, centerZ),
            new Vector3(0.003f, 0.001f, boardD - 0.006f),
            new Color(0.3f, 0.3f, 0.3f));

        // ── RIEL VCC superior (encima de ROW_A) ───────────────────────────
        float vccTopZ = SPACING * 1.5f;    // +1.5 filas sobre ROW_A
        CrearRiel(protoRoot.transform, "Riel_VCC_Top",
            new Vector3(centerX, SLOT_Y * 0.4f, vccTopZ),
            new Vector3(boardW - 0.006f, 0.001f, SPACING * 0.6f),
            new Color(0.85f, 0.1f, 0.1f));

        float gndTopZ = SPACING * 0.5f;    // +0.5 filas sobre ROW_A
        CrearRiel(protoRoot.transform, "Riel_GND_Top",
            new Vector3(centerX, SLOT_Y * 0.4f, gndTopZ),
            new Vector3(boardW - 0.006f, 0.001f, SPACING * 0.6f),
            new Color(0.1f, 0.15f, 0.7f));

        // ── RIEL VCC inferior (debajo de ROW_J) ───────────────────────────
        float vccBotZ = -(FILAS + 0.5f) * SPACING;
        CrearRiel(protoRoot.transform, "Riel_VCC_Bot",
            new Vector3(centerX, SLOT_Y * 0.4f, vccBotZ),
            new Vector3(boardW - 0.006f, 0.001f, SPACING * 0.6f),
            new Color(0.85f, 0.1f, 0.1f));

        float gndBotZ = -(FILAS + 1.5f) * SPACING;
        CrearRiel(protoRoot.transform, "Riel_GND_Bot",
            new Vector3(centerX, SLOT_Y * 0.4f, gndBotZ),
            new Vector3(boardW - 0.006f, 0.001f, SPACING * 0.6f),
            new Color(0.1f, 0.15f, 0.7f));

        // ── Reubicar slots VCC y GND existentes ───────────────────────────
        ReposicionarRieles(slotsRoot, vccTopZ, gndTopZ);

        // ── VoltageSource entre VCC y GND ─────────────────────────────────
        CrearVoltageSource(protoRoot, slotsRoot);

        EditorUtility.SetDirty(protoRoot);
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Selection.activeGameObject = protoRoot;

        EditorUtility.DisplayDialog("Protoboard creada",
            "Modelo 3D generado correctamente:\n\n" +
            "  Base beige + franja central\n" +
            "  Riel VCC (rojo) borde superior e inferior\n" +
            "  Riel GND (azul) borde superior e inferior\n" +
            "  Slots VCC/GND reposicionados\n" +
            "  VoltageSource conectada VCC→GND (9V)\n\n" +
            "Selecciona 'Protoboard_Model' en la jerarquía y\n" +
            "posiciónalo sobre la mesa del Explorador.", "OK");
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Reubicar los Slot_VCC y Slot_GND existentes a los bordes correctos
    // ─────────────────────────────────────────────────────────────────────
    static void ReposicionarRieles(GameObject slotsRoot, float vccZ, float gndZ)
    {
        int movedVCC = 0, movedGND = 0;
        foreach (Transform child in slotsRoot.transform)
        {
            var slot = child.GetComponent<ProtoboardSlot>();
            if (slot == null) continue;

            if (slot.railId == "VCC")
            {
                var lp = child.localPosition;
                child.localPosition = new Vector3(lp.x, SLOT_Y, vccZ);
                movedVCC++;
            }
            else if (slot.railId == "GND")
            {
                var lp = child.localPosition;
                child.localPosition = new Vector3(lp.x, SLOT_Y, gndZ);
                movedGND++;
            }
        }
        Debug.Log($"[ProtoboardModel] Slots reposicionados — VCC:{movedVCC}  GND:{movedGND}");
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Crear VoltageSource entre VCC[0] y GND[0]
    // ─────────────────────────────────────────────────────────────────────
    static void CrearVoltageSource(GameObject protoRoot, GameObject slotsRoot)
    {
        // Buscar nodos asignados en slots VCC y GND
        ElectricalNode nodeVCC = null, nodeGND = null;
        foreach (Transform child in slotsRoot.transform)
        {
            var slot = child.GetComponent<ProtoboardSlot>();
            if (slot == null) continue;
            if (slot.railId == "VCC" && nodeVCC == null)
                nodeVCC = child.GetComponent<ElectricalNode>();
            if (slot.railId == "GND" && nodeGND == null)
                nodeGND = child.GetComponent<ElectricalNode>();
            if (nodeVCC != null && nodeGND != null) break;
        }

        // Crear GO de la fuente de alimentación
        var srcGO = new GameObject("PowerRail_9V");
        Undo.RegisterCreatedObjectUndo(srcGO, "Crear VoltageSource");
        srcGO.transform.SetParent(protoRoot.transform, false);
        srcGO.transform.localPosition = new Vector3(-0.025f, 0, -(FILAS * 0.5f) * SPACING);

        var vs = srcGO.AddComponent<VoltageSource>();
        vs.voltage = 9f;

        if (nodeVCC != null) vs.nodeA = nodeVCC;
        if (nodeGND != null) vs.nodeB = nodeGND;

        // Indicador visual (pequeño cubo naranja)
        var indicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
        indicator.name = "PowerSource_Visual";
        indicator.transform.SetParent(srcGO.transform, false);
        indicator.transform.localScale    = new Vector3(0.012f, 0.018f, 0.012f);
        indicator.transform.localPosition = Vector3.zero;
        Object.DestroyImmediate(indicator.GetComponent<Collider>());
        SetColor(indicator.GetComponent<Renderer>(), new Color(1f, 0.55f, 0f));

        string msg = $"[ProtoboardModel] VoltageSource 9V creada.";
        if (nodeVCC == null || nodeGND == null)
            msg += " AVISO: conecta nodeA→VCC y nodeB→GND manualmente en Inspector.";
        Debug.Log(msg);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────
    static GameObject CrearPrimitivo(Transform parent, string nombre,
        PrimitiveType tipo, Vector3 localPos, Vector3 escala, Color color)
    {
        var go = GameObject.CreatePrimitive(tipo);
        go.name = nombre;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = escala;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        SetColor(go.GetComponent<Renderer>(), color);
        return go;
    }

    static void CrearRiel(Transform parent, string nombre,
        Vector3 localPos, Vector3 escala, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = nombre;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale    = escala;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        SetColor(go.GetComponent<Renderer>(), color);
    }

    static void SetColor(Renderer rend, Color color)
    {
        if (rend == null) return;
        var mat = new Material(rend.sharedMaterial != null
            ? rend.sharedMaterial
            : new Material(Shader.Find("Universal Render Pipeline/Lit")
                        ?? Shader.Find("Standard")));
        mat.color = color;
        rend.sharedMaterial = mat;
    }
}
#endif
