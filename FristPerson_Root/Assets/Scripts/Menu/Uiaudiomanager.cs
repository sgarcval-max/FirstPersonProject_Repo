using UnityEngine;

/// <summary>
/// Gestor central de los efectos de sonido de la interfaz (menús, botones).
/// Reproduce los clips a través de un AudioSource cuyo Output debe apuntar al grupo
/// "SFX" del Audio Mixer, para que respete el slider de volumen de Efectos de Ajustes.
/// </summary>
public class UIAudioManager : MonoBehaviour
{
    public static UIAudioManager Instance { get; private set; }

    [Header("Fuente de audio (Output debe ser el grupo SFX del Mixer)")]
    [SerializeField] private AudioSource sfxSource;

    [Header("Clips")]
    [SerializeField] private AudioClip panelOpenClip;
    [SerializeField] private AudioClip panelCloseClip;
    [SerializeField] private AudioClip buttonClickClip;
    [SerializeField] private AudioClip buttonHoverClip;
    [SerializeField] private AudioClip keyRebindClip;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void PlayPanelOpen() => PlayClip(panelOpenClip);
    public void PlayPanelClose() => PlayClip(panelCloseClip);
    public void PlayButtonClick() => PlayClip(buttonClickClip);
    public void PlayButtonHover() => PlayClip(buttonHoverClip);
    public void PlayKeyRebindStart() => PlayClip(keyRebindClip);

    private void PlayClip(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            // PlayOneShot permite que se solapen varios sonidos sin cortar el anterior
            sfxSource.PlayOneShot(clip);
        }
    }
}