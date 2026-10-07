using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

/// <summary>
/// Controla los volúmenes del juego (Master, Música, Efectos) a través del AudioMixer,
/// conecta los sliders de la UI de Ajustes, y guarda/carga las preferencias del jugador
/// usando PlayerPrefs para que persistan entre sesiones.
/// </summary>
public class AudioSettingsController : MonoBehaviour
{
    [Header("Referencia al Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Sliders de la UI (rango 0 a 1)")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    // Nombres EXACTOS de los parámetros expuestos en el Audio Mixer
    private const string MASTER_PARAM = "MasterVolume";
    private const string MUSIC_PARAM = "MusicVolume";
    private const string SFX_PARAM = "SFXVolume";

    // Claves de PlayerPrefs
    private const string MASTER_KEY = "volume_master";
    private const string MUSIC_KEY = "volume_music";
    private const string SFX_KEY = "volume_sfx";

    private void Start()
    {
        LoadAndApplySettings();

        // Nos suscribimos a los eventos de los sliders para que reaccionen en tiempo real al arrastrarlos
        if (masterSlider != null) masterSlider.onValueChanged.AddListener(SetMasterVolume);
        if (musicSlider != null) musicSlider.onValueChanged.AddListener(SetMusicVolume);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(SetSFXVolume);
    }

    /// <summary>Carga los valores guardados (o 1 = 100% por defecto la primera vez) y los aplica al Mixer y a los sliders.</summary>
    private void LoadAndApplySettings()
    {
        float master = PlayerPrefs.GetFloat(MASTER_KEY, 1f);
        float music = PlayerPrefs.GetFloat(MUSIC_KEY, 1f);
        float sfx = PlayerPrefs.GetFloat(SFX_KEY, 1f);

        // Actualizamos los sliders SIN disparar sus eventos (para no generar un bucle al iniciar)
        if (masterSlider != null) masterSlider.SetValueWithoutNotify(master);
        if (musicSlider != null) musicSlider.SetValueWithoutNotify(music);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(sfx);

        ApplyVolumeToMixer(MASTER_PARAM, master);
        ApplyVolumeToMixer(MUSIC_PARAM, music);
        ApplyVolumeToMixer(SFX_PARAM, sfx);
    }

    public void SetMasterVolume(float linearValue)
    {
        ApplyVolumeToMixer(MASTER_PARAM, linearValue);
        PlayerPrefs.SetFloat(MASTER_KEY, linearValue);
    }

    public void SetMusicVolume(float linearValue)
    {
        ApplyVolumeToMixer(MUSIC_PARAM, linearValue);
        PlayerPrefs.SetFloat(MUSIC_KEY, linearValue);
    }

    public void SetSFXVolume(float linearValue)
    {
        ApplyVolumeToMixer(SFX_PARAM, linearValue);
        PlayerPrefs.SetFloat(SFX_KEY, linearValue);
    }

    /// <summary>
    /// Convierte un valor lineal de slider (0 a 1) a decibelios (escala logarítmica que usa el Mixer)
    /// y lo aplica al parámetro correspondiente.
    /// </summary>
    private void ApplyVolumeToMixer(string parameterName, float linearValue)
    {
        // Evitamos log(0), que da -infinito; usamos un mínimo de 0.0001 (equivale a -80dB, prácticamente silencio)
        float clamped = Mathf.Max(linearValue, 0.0001f);
        float dB = Mathf.Log10(clamped) * 20f;
        audioMixer.SetFloat(parameterName, dB);
    }
}
