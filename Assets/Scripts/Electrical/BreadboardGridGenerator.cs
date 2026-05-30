using UnityEngine;
using UnityEditor;

/// <summary>
/// Herramienta para generar una matriz de imanes invisibles sobre la protoboard.
/// </summary>
public class BreadboardGridGenerator : EditorWindow
{
    private GameObject bareboard;
    private int filas = 10;
    private int columnas = 5;
    private float espaciado = 0.1f; // Distancia entre agujeros en metros

    [MenuItem("Tools/TITA/Generar Matriz Protoboard")]
    public static void ShowWindow()
    {
        GetWindow<BreadboardGridGenerator>("Generador de Slots");
    }

    void OnGUI()
    {
        GUILayout.Label("Configuración de la Protoboard", EditorStyles.boldLabel);
        bareboard = (GameObject)EditorGUILayout.ObjectField("Bareboard (Padre)", bareboard, typeof(GameObject), true);
        filas = EditorGUILayout.IntField("Filas (Largo)", filas);
        columnas = EditorGUILayout.IntField("Columnas (Ancho)", columnas);
        espaciado = EditorGUILayout.FloatField("Espaciado entre nodos", espaciado);

        if (GUILayout.Button("Generar Matriz Magnética"))
        {
            if (bareboard == null)
            {
                Debug.LogError("Asigna el objeto Bareboard primero.");
                return;
            }
            GenerarMatriz();
        }
    }

    void GenerarMatriz()
    {
        // Creamos un contenedor limpio para mantener la jerarquía ordenada
        GameObject contenedor = new GameObject("Slots_Matriz");
        contenedor.transform.SetParent(bareboard.transform);
        contenedor.transform.localPosition = Vector3.zero;

        Vector3 startPos = new Vector3(-((columnas - 1) * espaciado) / 2f, 0.02f, -((filas - 1) * espaciado) / 2f);

        for (int x = 0; x < columnas; x++)
        {
            for (int z = 0; z < filas; z++)
            {
                GameObject slotObj = new GameObject($"Slot_{x}_{z}");
                slotObj.transform.SetParent(contenedor.transform);
                slotObj.transform.localPosition = startPos + new Vector3(x * espaciado, 0, z * espaciado);
                
                // Le agregamos el collider físico (Trigger)
                BoxCollider col = slotObj.AddComponent<BoxCollider>();
                col.isTrigger = true;
                col.size = new Vector3(0.08f, 0.05f, 0.08f); // Tamaño del imán

                // Le agregamos tu script de validación
                ComponentSlot slot = slotObj.AddComponent<ComponentSlot>();
                slot.acceptedType = ComponentSlotType.Any; // Listo para aceptar cualquier pieza

                // Le creamos un punto de anclaje para que la pieza quede perfectamente centrada
                GameObject anchor = new GameObject("Anchor");
                anchor.transform.SetParent(slotObj.transform);
                anchor.transform.localPosition = Vector3.zero;
                slot.installAnchor = anchor.transform;
            }
        }
        Debug.Log($"Se generaron {filas * columnas} slots magnéticos.");
    }
}