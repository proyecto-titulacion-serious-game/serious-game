using UnityEngine;

/// <summary>
/// Añade ComponentSmokeEffect a todos los componentes eléctricos faultables
/// e instancia los prefabs de Cartoon FX (o cualquier otro) como efectos visuales.
///
/// SETUP (una sola vez):
///   1. Añadir este script a un GameObject vacío en la escena (ej. "FX_Manager").
///   2. Asignar en el Inspector:
///        smokePrefab  → "CFXR Smoke Source 3D"
///        sparkPrefab  → "CFXR Electrified 3"  (o "CFXR2 Sparks Rain")
///   3. Dar Play — el resto es automático.
///
/// Si smokePrefab / sparkPrefab quedan vacíos, ComponentSmokeEffect genera
/// sus propias partículas por código (igual que antes).
/// </summary>
public class AutoSmokeSetup : MonoBehaviour
{
    [Header("Prefabs de Cartoon FX (arrastrar desde Project)")]
    [Tooltip("CFXR Smoke Source 3D  — efecto de humo continuo.")]
    public GameObject smokePrefab;

    [Tooltip("CFXR Electrified 3  o  CFXR2 Sparks Rain — efecto de chispas.")]
    public GameObject sparkPrefab;

    [Header("Escala de los efectos")]
    [Tooltip("Ajusta el tamaño de los efectos CFXR para que encajen con los componentes.")]
    public float smokeScale = 0.15f;
    public float sparkScale = 0.12f;

    void Awake()
    {
        int added = 0;

        foreach (var comp in FindObjectsByType<ElectricalComponent>(FindObjectsInactive.Include))
        {
            bool isFaultable = comp is Resistor
                            || comp is LED
                            || comp is Capacitor
                            || comp is ArduinoPin;

            if (!isFaultable) continue;

            // Las LEDs además explotan/salen volando si reciben sobrecarga catastrófica.
            if (comp is LED && comp.GetComponent<LEDBlowEffect>() == null)
                comp.gameObject.AddComponent<LEDBlowEffect>();

            if (comp.GetComponent<ComponentSmokeEffect>() != null) continue;

            var fx = comp.gameObject.AddComponent<ComponentSmokeEffect>();

            // ── Humo ──────────────────────────────────────────────────────
            if (smokePrefab != null)
            {
                var smokeGO = Instantiate(smokePrefab, comp.transform);
                smokeGO.name = "Smoke_VFX";
                smokeGO.transform.localPosition = Vector3.up * 0.04f;
                smokeGO.transform.localRotation = Quaternion.identity;
                smokeGO.transform.localScale    = Vector3.one * smokeScale;

                // Obtener el ParticleSystem raíz del prefab CFXR
                var ps = smokeGO.GetComponent<ParticleSystem>()
                      ?? smokeGO.GetComponentInChildren<ParticleSystem>();

                if (ps != null)
                {
                    fx.smokeEffect = ps;
                    // Detener hasta que el circuito lo active
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }

            // ── Chispas ───────────────────────────────────────────────────
            if (sparkPrefab != null)
            {
                var sparkGO = Instantiate(sparkPrefab, comp.transform);
                sparkGO.name = "Sparks_VFX";
                sparkGO.transform.localPosition = Vector3.up * 0.01f;
                sparkGO.transform.localRotation = Quaternion.identity;
                sparkGO.transform.localScale    = Vector3.one * sparkScale;

                var ps = sparkGO.GetComponent<ParticleSystem>()
                      ?? sparkGO.GetComponentInChildren<ParticleSystem>();

                if (ps != null)
                {
                    fx.sparkEffect = ps;
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }

            added++;
        }

        if (added > 0)
            Debug.Log($"[AutoSmokeSetup] Efectos añadidos a {added} componente(s).");
    }
}
