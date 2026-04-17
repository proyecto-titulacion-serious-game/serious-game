using UnityEngine;
using UnityEngine.UI;

public class ManualUI : MonoBehaviour
{
    public TechnicianManual technicianManual;
    public Text manualText;

    void Update()
    {
        if (technicianManual == null || manualText == null) return;

        manualText.text = technicianManual.GetManualText();
    }
}