using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controla los ajustes de ratón (invertir eje Y, sensibilidad X e Y) desde la UI,
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
    [SerializeField] private Slider sensitivityXSlider;
    [SerializeField] private Slider sensitivityYSlider;

    // Claves de PlayerPrefs
    private const string INVERT_Y_KEY = "mouse_invert_y";
    private const string SENS_X_KEY = "mouse_sens_x";
    private const string SENS_Y_KEY = "mouse_sens_y";

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
        if (sensitivityXSlider != null) sensitivityXSlider.onValueChanged.AddListener(SetSensitivityX);
        if (sensitivityYSlider != null) sensitivityYSlider.onValueChanged.AddListener(SetSensitivityY);
    }

    private void LoadAndApplySettings()
    {
        bool invertY = PlayerPrefs.GetInt(INVERT_Y_KEY, 0) == 1; // 0 = false por defecto
        float sensX = PlayerPrefs.GetFloat(SENS_X_KEY, 50f);     // 50 = punto medio por defecto
        float sensY = PlayerPrefs.GetFloat(SENS_Y_KEY, 50f);

        if (invertYToggle != null) invertYToggle.SetIsOnWithoutNotify(invertY);
        if (sensitivityXSlider != null) sensitivityXSlider.SetValueWithoutNotify(sensX);
        if (sensitivityYSlider != null) sensitivityYSlider.SetValueWithoutNotify(sensY);

        ApplyInvertY(invertY);
        ApplySensitivityX(sensX);
        ApplySensitivityY(sensY);
    }

    public void SetInvertY(bool value)
    {
        ApplyInvertY(value);
        PlayerPrefs.SetInt(INVERT_Y_KEY, value ? 1 : 0);
    }

    public void SetSensitivityX(float sliderValue)
    {
        ApplySensitivityX(sliderValue);
        PlayerPrefs.SetFloat(SENS_X_KEY, sliderValue);
    }

    public void SetSensitivityY(float sliderValue)
    {
        ApplySensitivityY(sliderValue);
        PlayerPrefs.SetFloat(SENS_Y_KEY, sliderValue);
    }

    private void ApplyInvertY(bool value)
    {
        if (playerController != null) playerController.InvertY = value;
    }

    private void ApplySensitivityX(float sliderValue)
    {
        if (playerController == null) return;
        float realValue = MapSliderToRealSensitivity(sliderValue);
        playerController.MouseSensitivityX = realValue;
    }

    private void ApplySensitivityY(float sliderValue)
    {
        if (playerController == null) return;
        float realValue = MapSliderToRealSensitivity(sliderValue);
        playerController.MouseSensitivityY = realValue;
    }

    /// <summary>Convierte un valor de slider (0-100) al rango real de sensibilidad que usa el controlador.</summary>
    private float MapSliderToRealSensitivity(float sliderValue0To100)
    {
        float t = Mathf.Clamp01(sliderValue0To100 / 100f);
        return Mathf.Lerp(MIN_REAL_SENSITIVITY, MAX_REAL_SENSITIVITY, t);
    }
}
