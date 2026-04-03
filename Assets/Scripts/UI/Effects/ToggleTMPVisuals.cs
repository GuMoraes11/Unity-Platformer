using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ToggleTMPVisual : MonoBehaviour
{
    public Toggle toggle;
    public TMP_Text targetText;

    [Header("Colors")]
    public Color onColor = Color.white;
    public Color offColor = new Color(1f, 1f, 1f, 0.4f);

    private void Start()
    {
        toggle.onValueChanged.AddListener(UpdateVisual);
        UpdateVisual(toggle.isOn);
    }

    private void UpdateVisual(bool isOn)
    {
        targetText.color = isOn ? onColor : offColor;
    }
}