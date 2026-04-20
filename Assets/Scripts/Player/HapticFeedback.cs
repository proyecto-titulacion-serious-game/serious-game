using System.Collections;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Centraliza la retroalimentación háptica del Explorador:
/// - Controladores Meta Quest (vibración de manos)
/// - Chaleco háptico bHaptics / OWO (cuando el SDK esté instalado)
///
/// Uso desde otros scripts:
///   haptics.PlayLight();    → toque suave (conectar punta multímetro)
///   haptics.PlayMedium();   → pulso (agarrar componente)
///   haptics.PlayStrong();   → vibración fuerte (reparación exitosa)
///   haptics.PlayError();    → patrón de error (componente incorrecto)
///   haptics.PlayCurrent(0.5f); → vibración proporcional a corriente
/// </summary>
public class HapticFeedback : MonoBehaviour
{
    // ─────────────────────────────────────────────
    //  Inspector
    // ─────────────────────────────────────────────
    [Header("Meta Quest — controladores")]
    [Range(0f, 1f)] public float lightIntensity  = 0.3f;
    [Range(0f, 1f)] public float mediumIntensity = 0.6f;
    [Range(0f, 1f)] public float strongIntensity = 1.0f;

    [Header("Chaleco háptico")]
    [Tooltip("True cuando el SDK del chaleco esté instalado.")]
    public bool chalecoEnabled = false;

    [Header("Proporcional a corriente")]
    [Tooltip("Corriente máxima (A) que equivale a vibración máxima.")]
    public float maxCurrentForHaptic = 0.1f;

    // ─────────────────────────────────────────────
    //  Dispositivos XR
    // ─────────────────────────────────────────────
    private InputDevice _leftController;
    private InputDevice _rightController;

    void Start()
    {
        RefreshDevices();
    }

    void RefreshDevices()
    {
        var leftDevices  = new System.Collections.Generic.List<InputDevice>();
        var rightDevices = new System.Collections.Generic.List<InputDevice>();

        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Left  | InputDeviceCharacteristics.Controller, leftDevices);
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.Right | InputDeviceCharacteristics.Controller, rightDevices);

        if (leftDevices.Count  > 0) _leftController  = leftDevices[0];
        if (rightDevices.Count > 0) _rightController = rightDevices[0];
    }

    // ─────────────────────────────────────────────
    //  API Pública
    // ─────────────────────────────────────────────

    /// <summary>Toque suave — conectar punta del multímetro.</summary>
    public void PlayLight() =>
        Vibrate(lightIntensity, 0.1f);

    /// <summary>Pulso medio — agarrar componente, insertar cable.</summary>
    public void PlayMedium() =>
        Vibrate(mediumIntensity, 0.2f);

    /// <summary>Vibración fuerte — reparación completada con éxito.</summary>
    public void PlayStrong() =>
        Vibrate(strongIntensity, 0.4f);

    /// <summary>Patrón de error — acción incorrecta.</summary>
    public void PlayError() =>
        StartCoroutine(ErrorPattern());

    /// <summary>
    /// Vibración proporcional a la corriente del circuito.
    /// Llamar desde CircuitManager.OnCircuitChanged.
    /// </summary>
    public void PlayCurrent(float current)
    {
        float normalizedIntensity = Mathf.Clamp01(current / maxCurrentForHaptic);
        Vibrate(normalizedIntensity, 0.1f);

        if (chalecoEnabled)
            SendToVest(normalizedIntensity);
    }

    // ─────────────────────────────────────────────
    //  Internos — Controladores Meta Quest
    // ─────────────────────────────────────────────

    void Vibrate(float amplitude, float duration)
    {
        if (!_leftController.isValid || !_rightController.isValid)
            RefreshDevices();

        _leftController.SendHapticImpulse( 0, amplitude, duration);
        _rightController.SendHapticImpulse(0, amplitude, duration);
    }

    IEnumerator ErrorPattern()
    {
        Vibrate(strongIntensity, 0.1f);
        yield return new WaitForSeconds(0.15f);
        Vibrate(strongIntensity, 0.1f);
        yield return new WaitForSeconds(0.15f);
        Vibrate(mediumIntensity, 0.1f);
    }

    // ─────────────────────────────────────────────
    //  Internos — Chaleco háptico
    // ─────────────────────────────────────────────

    void SendToVest(float intensity)
    {
        // ──────────────────────────────────────────────────────────────────
        // INTEGRACIÓN CHALECO HÁPTICO (bHaptics / OWO Suit)
        //
        // bHaptics (Tactsuit):
        //   BhapticsManager.PlayParam("ChestFront", intensity: intensity);
        //
        // OWO Suit:
        //   OWO.Send(OWOSensation.Vest(intensity));
        //
        // El SDK se importa desde el Asset Store del fabricante.
        // Desactivar chalecoEnabled hasta que el SDK esté instalado.
        // ──────────────────────────────────────────────────────────────────
        Debug.Log($"[HapticFeedback] Chaleco: intensidad {intensity:F2} (SDK pendiente)");
    }
}