// Archivo legacy — la version activa esta en Assets/Editor/BreadboardGridGenerator.cs
// Esta clase fue renombrada para evitar el conflicto CS0436 entre assemblies.
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Version legacy (reemplazada por Assets/Editor/BreadboardGridGenerator.cs).
/// Conservada por compatibilidad de escenas anteriores.
/// Menu: Tools > TITA > Generar Matriz Protoboard (Legacy)
/// </summary>
public class BreadboardGridGeneratorLegacy : EditorWindow
{
    private GameObject bareboard;
    private int filas = 10;
    private int columnas = 5;
    private float espaciado = 0.1f;

    [MenuItem("Tools/TITA/Generar Matriz Protoboard (Legacy)")]
    public static void ShowWindow()
    {
        GetWindow<BreadboardGridGeneratorLegacy>("Generador Slots Legacy");
    }

    void OnGUI()
    {
        GUILayout.Label("Configuracion de la Protoboard (Legacy)", EditorStyles.boldLabel);
        bareboard = (GameObject)EditorGUILayout.ObjectField("Bareboard (Padre)", bareboard, typeof(GameObject), true);
        filas     = EditorGUILayout.IntField("Filas (Largo)",         filas);
        columnas  = EditorGUILayout.IntField("Columnas (Ancho)",       columnas);
        espaciado = EditorGUILayout.FloatField("Espaciado entre nodos", espaciado);

        if (GUILayout.Button("Generar Matriz Magnetica"))
        {
            if (bareboard == null) { Debug.LogError("Asigna el Bareboard primero."); return; }
            GenerarMatriz();
        }
    }

    void GenerarMatriz()
    {
        var contenedor = new GameObject("Slots_Matriz");
        contenedor.transform.SetParent(bareboard.transform);
        contenedor.transform.localPosition = Vector3.zero;

        Vector3 startPos = new Vector3(
            -((columnas - 1) * espaciado) / 2f,
            0.02f,
            -((filas    - 1) * espaciado) / 2f);

        for (int x = 0; x < columnas; x++)
        {
            for (int z = 0; z < filas; z++)
            {
                var slotObj = new GameObject($"Slot_{x}_{z}");
                slotObj.transform.SetParent(contenedor.transform);
                slotObj.transform.localPosition = startPos + new Vector3(x * espaciado, 0, z * espaciado);

                var col = slotObj.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.size      = new Vector3(0.08f, 0.05f, 0.08f);

                var slot = slotObj.AddComponent<ComponentSlot>();
                slot.acceptedType = ComponentSlotType.Any;

                var anchor = new GameObject("Anchor");
                anchor.transform.SetParent(slotObj.transform);
                anchor.transform.localPosition = Vector3.zero;
                slot.installAnchor = anchor.transform;
            }
        }
        Debug.Log($"[LegacyGrid] {filas * columnas} slots generados.");
    }
}
#endif
