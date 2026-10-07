using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Añade esto a CUALQUIER slider y muestra su valor al lado como un número de 0
/// (cuando el slider está al mínimo) a 50 (cuando está al máximo), sin importar
/// el rango interno real del slider (0-1, 0-100, etc.). Se actualiza en vivo
/// mientras se arrastra.
/// </summary>
[RequireComponent(typeof(Slider))]
public class SliderValueDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text valueText;

    [Tooltip("El número que se muestra cuando el slider está al máximo (0 siempre es el mínimo).")]
    [SerializeField] private int displayMax = 50;

    private Slider slider;

    private void Awake()
    {
        slider = GetComponent<Slider>();
    }

    private void OnEnable()
    {
        UpdateText(slider.value);
        slider.onValueChanged.AddListener(UpdateText);
    }

    private void OnDisable()
    {
        slider.onValueChanged.RemoveListener(UpdateText);
    }

    private void UpdateText(float rawValue)
    {
        if (valueText == null) return;

        float normalized = Mathf.InverseLerp(slider.minValue, slider.maxValue, rawValue);
        int displayValue = Mathf.RoundToInt(normalized * displayMax);
        valueText.text = displayValue.ToString();
    }
}
