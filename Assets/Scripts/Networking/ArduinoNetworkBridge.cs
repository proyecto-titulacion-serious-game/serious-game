using System;
using UnityEngine;
using Fusion; // Librería de Photon Fusion

/// <summary>
/// Enlace de red asimétrico. Comunica la interfaz del Técnico (PC) con el Arduino físico en VR.
/// </summary>
public class ArduinoNetworkBridge : NetworkBehaviour
{
    private ArduinoCore _arduinoCore;

    /// <summary>Dispara en todos los clientes cuando el Técnico sube un nuevo sketch.</summary>
    public static event Action<int, PinMode, PinState, bool, int> OnSketchReceived;

    // ─── TELEMETRÍA CONTINUA (De VR a PC) ───
    // Usamos una variable sincronizada en red para que el Técnico vea el valor del sensor en vivo
    [Networked] 
    public int NetworkedAnalogValue { get; set; }

    void Awake()
    {
        _arduinoCore = GetComponent<ArduinoCore>();
    }

    public override void FixedUpdateNetwork()
    {
        // El servidor (o el jugador con autoridad sobre el Arduino) lee el valor real y lo sube a la red
        if (HasStateAuthority && _arduinoCore != null)
        {
            NetworkedAnalogValue = _arduinoCore.GetAnalogReadA0();
        }
    }

    // ─── COMANDOS DE PROGRAMACIÓN (De PC a VR) ───
    // Este RPC (Remote Procedure Call) lo dispara la PC y se ejecuta en todos los clientes
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_SubirCodigoArduino(NetworkBool isOutput, NetworkBool isHigh, float delayMs, NetworkBool isBlink)
    {
        if (_arduinoCore != null)
        {
            Debug.Log("[NetworkBridge] Instrucción recibida por red. Flasheando ArduinoCore...");
            _arduinoCore.RecibirCodigoDePC(isOutput, isHigh, delayMs, isBlink);
        }

        PinMode  mode    = isOutput ? PinMode.OUTPUT : PinMode.INPUT;
        PinState state   = isHigh   ? PinState.HIGH  : PinState.LOW;
        int      blinkMs = Mathf.RoundToInt(delayMs);
        OnSketchReceived?.Invoke(_arduinoCore != null ? _arduinoCore.activePinNumber : 13,
                                 mode, state, isBlink, blinkMs);
    }
}