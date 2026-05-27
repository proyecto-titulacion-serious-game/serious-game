#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TMPro;

/// <summary>
/// Añade un panel ArduinoPinDisplay a la Reto4_Zone ya construida en escena.
/// Muestra PIN D4 [INCORRECTO] / PIN D2 [CORRECTO] en color rojo/verde/naranja.
///
/// Uso: Tools → TITA → Reto 4 → Añadir Display de Pin Arduino
/// Prerrequisito: ejecutar "Construir Retos en Escena" primero.
/// </summary>
public static class Reto4DisplaySetup
{
    [MenuItem("Tools/TITA/Reto 4/Añadir Display de Pin Arduino")]
    public static void AddPinDisplay()
    {
        var zone = GameObject.Find("Reto4_Zone");
        if (zone == null)
        {
            EditorUtility.DisplayDialog("Error",
                "No se encontró 'Reto4_Zone' en la escena.\n\n" +
                "Ejecuta primero:\n" +
                "Tools → TITA → Vol.2 Electronics → Construir Retos en Escena",
                "OK");
            return;
        }

        var board = zone.transform.Find("Arduino_WrongPin");
        if (board == null)
        {
            EditorUtility.DisplayDialog("Error",
                "No se encontró 'Arduino_WrongPin' dentro de Reto4_Zone.", "OK");
            return;
        }

        var pin = board.GetComponent<ArduinoPin>();
        if (pin == null)
        {
            EditorUtility.DisplayDialog("Error",
                "'Arduino_WrongPin' no tiene componente ArduinoPin.", "OK");
            return;
        }

        // Evitar duplicados
        if (zone.transform.Find("PinDisplay") != null)
        {
            bool recreate = EditorUtility.DisplayDialog("Ya existe",
                "Ya existe un 'PinDisplay' en Reto4_Zone. ¿Recrearlo?",
                "Sí", "Cancelar");
            if (!recreate) return;
            Undo.DestroyObjectImmediate(zone.transform.Find("PinDisplay").gameObject);
        }

        // Panel físico (cubo plano)
        var panelGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panelGO.name = "PinDisplay";
        panelGO.transform.SetParent(zone.transform);
        panelGO.transform.localPosition = new Vector3(0.10f, 0.10f, -0.15f);
        panelGO.transform.localScale    = new Vector3(0.18f, 0.10f, 0.008f);
        Object.DestroyImmediate(panelGO.GetComponent<Collider>());
        Undo.RegisterCreatedObjectUndo(panelGO, "Crear PinDisplay");

        // Color inicial (rojo = fallo)
        var panelRend = panelGO.GetComponent<Renderer>();
        var shader    = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (shader != null)
        {
            var mat = new Material(shader);
            mat.SetColor("_BaseColor", new Color(0.7f, 0.1f, 0.1f));
            panelRend.sharedMaterial = mat;
        }

        // Texto TMP
        var textGO = new GameObject("PinText");
        textGO.transform.SetParent(panelGO.transform);
        textGO.transform.localPosition = new Vector3(0f, 0f, -0.6f);
        textGO.transform.localScale    = new Vector3(4f, 7f, 1f);
        Undo.RegisterCreatedObjectUndo(textGO, "PinText");

        var tmp          = textGO.AddComponent<TextMeshPro>();
        tmp.text         = $"PIN D{pin.pinNumber}\n[INCORRECTO]";
        tmp.fontSize     = 2f;
        tmp.alignment    = TextAlignmentOptions.Center;
        tmp.color        = Color.white;

        // ArduinoPinDisplay script
        var display          = panelGO.AddComponent<ArduinoPinDisplay>();
        display.linkedPin    = pin;
        display.statusText   = tmp;
        display.panelRenderer = panelRend;

        EditorUtility.SetDirty(panelGO);

        EditorUtility.DisplayDialog("Listo",
            "Panel 'PinDisplay' añadido a Reto4_Zone.\n\n" +
            "Muestra el estado del pin en tiempo real:\n" +
            "  Rojo    = pin incorrecto\n" +
            "  Naranja = cable suelto\n" +
            "  Verde   = pin correcto\n\n" +
            "Ajusta posición en Inspector si es necesario.",
            "OK");
    }

    [MenuItem("Tools/TITA/Reto 4/Añadir Display de Pin Arduino", true)]
    static bool Validate() =>
        UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().IsValid();
}
#endif
