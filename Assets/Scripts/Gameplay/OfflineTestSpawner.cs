using UnityEngine;

/// <summary>
/// Herramienta de prueba para simular que el Técnico envía un componente
/// sin necesitar Fusion ni la escena del Técnico activa.
///
/// SETUP EN UNITY (escena Explorador):
///   1. Añadir este script a cualquier GameObject vacío.
///   2. Configurar testType y testValue según el reto a probar.
///   3. Presionar la tecla configurada en spawnKey, o llamar SpawnTestComponent()
///      desde un XRSimpleInteractable.selectEntered para probarlo en VR.
///
/// RETOS DE REFERENCIA:
///   Reto 1 — Resistor: testType=Resistor, testValue=100 (correcto) / 10 (incorrecto)
///   Reto 2 — LED:      testType=LED, testValue=1 (correcto) / -1 (invertido)
///   Reto 3 — Capacitor: testType=Capacitor, testValue=1 o -1
///   Reto 4 — Arduino:   testType=ArduinoPin, testValue=2 (pin correcto)
/// </summary>
public class OfflineTestSpawner : MonoBehaviour
{
    [Header("Componente a simular")]
    public ComponentType testType  = ComponentType.Resistor;
    [Tooltip("Ohms para Resistor | 1/-1 para LED/Capacitor | pin# para Arduino")]
    public float         testValue = 100f;

    [Header("Spawn automático al inicio")]
    [Tooltip("Spawnea automáticamente después de autoSpawnDelay segundos.")]
    public bool  autoSpawnOnStart = true;
    public float autoSpawnDelay   = 3f;

    [Header("Tecla de teclado (editor/PC)")]
    public KeyCode spawnKey = KeyCode.T;

    void Start()
    {
        if (autoSpawnOnStart)
            Invoke(nameof(SpawnTestComponent), autoSpawnDelay);
    }

    void Update()
    {
        if (Input.GetKeyDown(spawnKey))
            SpawnTestComponent();
    }

    /// <summary>
    /// Dispara el evento que ExplorerComponentReceiver escucha para spawnear
    /// el componente en la bandeja del Explorador.
    /// Puede conectarse a XRSimpleInteractable.selectEntered desde el Inspector.
    /// </summary>
    public void SpawnTestComponent()
    {
        ComponentSendingTray.RaiseOnComponentSentLocal(testType, testValue);
        Debug.Log($"[OfflineTestSpawner] Componente de prueba enviado: {testType} = {testValue}");
    }
}
