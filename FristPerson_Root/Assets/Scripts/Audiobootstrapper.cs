using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Aplica los volúmenes guardados (Master/Música/SFX) al Audio Mixer INMEDIATAMENTE
/// al arrancar la escena, sin depender de que el jugador abra el panel de Ajustes.
/// Debe ir en un GameObject SIEMPRE activo desde el principio (no dentro de SettingsPanel,
/// que empieza desactivado). AudioSettingsController se sigue encargando de actualizar
/// los sliders y aplicar cambios en vivo mientras Ajustes está abierto.
/// </summary>
public class AudioBootstrapper : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;

    private const string MASTER_PARAM = "MasterVolume";
    private const string MUSIC_PARAM = "MusicVolume";
    private const string SFX_PARAM = "SFXVolume";

    private const string MASTER_KEY = "volume_master";
    private const string MUSIC_KEY = "volume_music";
    private const string SFX_KEY = "volume_sfx";

    private void Awake()
    {
        ApplySavedVolume(MASTER_PARAM, MASTER_KEY);
        ApplySavedVolume(MUSIC_PARAM, MUSIC_KEY);
        ApplySavedVolume(SFX_PARAM, SFX_KEY);
    }

    private void ApplySavedVolume(string mixerParam, string prefKey)
    {
        float linear = PlayerPrefs.GetFloat(prefKey, 1f);
        float clamped = Mathf.Max(linear, 0.0001f);
        float dB = Mathf.Log10(clamped) * 20f;
        audioMixer.SetFloat(mixerParam, dB);
    }
}
