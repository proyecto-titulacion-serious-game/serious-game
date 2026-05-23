using UnityEngine;

/// <summary>
/// Añade ComponentSmokeEffect automáticamente a todos los componentes eléctricos
/// de la escena que puedan tener fallas (Resistor, LED, Capacitor, ArduinoPin).
///
/// SETUP: Añadir este script a cualquier GameObject vacío en la escena (ej. "FX_Manager").
/// No requiere configuración adicional.
/// </summary>
public class AutoSmokeSetup : MonoBehaviour
{
    void Awake()
    {
        int added = 0;

        foreach (var comp in FindObjectsByType<ElectricalComponent>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            // Solo tipos que pueden tener fallas visuales
            bool isFaultable = comp is Resistor
                            || comp is LED
                            || comp is Capacitor
                            || comp is ArduinoPin;

            if (!isFaultable) continue;
            if (comp.GetComponent<ComponentSmokeEffect>() != null) continue;

            comp.gameObject.AddComponent<ComponentSmokeEffect>();
            added++;
        }

        if (added > 0)
            Debug.Log($"[AutoSmokeSetup] ComponentSmokeEffect añadido a {added} componente(s).");
    }
}
