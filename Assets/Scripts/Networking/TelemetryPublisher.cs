using System.Collections;
using UnityEngine;

/// <summary>
/// Publica la telemetría del sandbox del Reto 4 (que corre en el Explorador) hacia el
/// Técnico vía <see cref="GameSession.RPC_PublicarTelemetria"/>. El Técnico/Host no tiene
/// los motores (ProtoboardSimulator/ArduinoCore) localmente, así que sin esto su panel no
/// mostraría V/I/P/ADC en el setup asimétrico de 2 escenas.
///
/// Se auto-arranca (no requiere ponerlo en ninguna escena) y solo publica cuando hay
/// GameSession en red Y un ProtoboardSimulator presente (i.e., en el Explorador). En la
/// escena del Técnico no hay simulador → queda inactivo sin coste.
/// </summary>
public class TelemetryPublisher : MonoBehaviour
{
    [Tooltip("Frecuencia de publicación en Hz.")]
    public float rateHz = 5f;

    private ProtoboardSimulator _sim;
    private ArduinoCore         _core;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        var go = new GameObject("[TelemetryPublisher]");
        DontDestroyOnLoad(go);
        go.AddComponent<TelemetryPublisher>();
    }

    IEnumerator Start()
    {
        while (true)
        {
            float interval = rateHz > 0f ? 1f / rateHz : 0.2f;
            yield return new WaitForSeconds(interval);

            // Sin red compartida no hay a quién publicar (offline/escena única usa lectura local).
            if (GameSession.Instance == null) continue;

            // Unity sobrecarga ==, así que un fake-null tras cambio de escena re-dispara la búsqueda.
            if (_sim  == null) _sim  = FindAnyObjectByType<ProtoboardSimulator>();
            if (_sim  == null) continue;   // no hay sandbox en esta escena (p.ej. Técnico) → nada que publicar
            if (_core == null) _core = FindAnyObjectByType<ArduinoCore>();

            int status = _sim.isShortCircuited ? 1
                       : (_sim.totalCurrentmA <= 0.0001f ? 2 : 0);
            int adc    = _core != null ? _core.GetAnalogReadA0() : 0;

            GameSession.Instance.RPC_PublicarTelemetria(
                _sim.sourceVoltage, _sim.totalCurrentmA, _sim.totalPowerW, adc, status);
        }
    }
}
