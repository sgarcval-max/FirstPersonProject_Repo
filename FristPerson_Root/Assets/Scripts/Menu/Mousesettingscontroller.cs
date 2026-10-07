using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controla los ajustes de ratón (invertir eje Y, sensibilidad general) desde la UI,
/// los aplica en tiempo real al FirstPersonController, y los guarda con PlayerPrefs
/// para que persistan entre sesiones.
/// </summary>
public class MouseSettingsController : MonoBehaviour
{
    [Header("Referencia al jugador")]
    [Tooltip("Si se deja vacío, se busca automáticamente en la escena con FindObjectOfType.")]
    [SerializeField] private FirstPersonController playerController;

    [Header("Referencias UI")]
    [SerializeField] private Toggle invertYToggle;
    [SerializeField] private Slider sensitivitySlider;

    // Claves de PlayerPrefs
    private const string INVERT_Y_KEY = "mouse_invert_y";
    private const string SENS_KEY = "mouse_sensitivity";

    // Rango de sensibilidad real que se aplicará al FirstPersonController.
    // El slider va de 0 a 100 (como pediste), y aquí lo mapeamos a un rango de sensibilidad usable.
    private const float MIN_REAL_SENSITIVITY = 5f;
    private const float MAX_REAL_SENSITIVITY = 150f;

    private void Start()
    {
        if (playerController == null)
        {
            playerController = FindObjectOfType<FirstPersonController>();
        }

        LoadAndApplySettings();

        if (invertYToggle != null) invertYToggle.onValueChanged.AddListener(SetInvertY);
        if (sensitivitySlider != null) sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
    }

    private void LoadAndApplySettings()
    {
        bool invertY = PlayerPrefs.GetInt(INVERT_Y_KEY, 0) == 1;
        float sens = PlayerPrefs.GetFloat(SENS_KEY, 50f); // 50 = punto medio por defecto

        if (invertYToggle != null) invertYToggle.SetIsOnWithoutNotify(invertY);
        if (sensitivitySlider != null) sensitivitySlider.SetValueWithoutNotify(sens);

        ApplyInvertY(invertY);
        ApplySensitivity(sens);
    }

    public void SetInvertY(bool value)
    {
        ApplyInvertY(value);
        PlayerPrefs.SetInt(INVERT_Y_KEY, value ? 1 : 0);
    }

    public void SetSensitivity(float sliderValue)
    {
        ApplySensitivity(sliderValue);
        PlayerPrefs.SetFloat(SENS_KEY, sliderValue);
    }

    private void ApplyInvertY(bool value)
    {
        if (playerController != null) playerController.InvertY = value;
    }

    private void ApplySensitivity(float sliderValue)
    {
        if (playerController == null) return;
        float t = Mathf.Clamp01(sliderValue / 100f);
        float realValue = Mathf.Lerp(MIN_REAL_SENSITIVITY, MAX_REAL_SENSITIVITY, t);
        playerController.MouseSensitivity = realValue;
    }
}
