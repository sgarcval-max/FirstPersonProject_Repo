using System.Collections;
using UnityEngine;

/// <summary>
/// Controla la música o sonido de ambiente en bucle para la escena de juego.
/// Aplica un Fade-In progresivo al iniciar para evitar cortes bruscos de audio.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class GameAmbientAudio : MonoBehaviour
{
    [Header("Configuración de Ambiente")]
    [SerializeField] private AudioClip ambientClip;
    [Range(0f, 1f)]
    [SerializeField] private float targetVolume = 0.4f;

    [Header("Transición")]
    [Tooltip("Tiempo en segundos que tarda el audio en alcanzar el volumen objetivo al cargar la escena.")]
    [SerializeField] private float fadeInDuration = 1.5f;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        // Configurar AudioSource para sonido ambiente 2D en bucle
        audioSource.clip = ambientClip;
        audioSource.loop = true;
        audioSource.spatialBlend = 0f; // 2D Sound
        audioSource.playOnAwake = false;
        audioSource.volume = 0f;
    }

    private void Start()
    {
        if (ambientClip != null)
        {
            audioSource.Play();
            StartCoroutine(FadeInAmbient());
        }
    }

    private IEnumerator FadeInAmbient()
    {
        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / fadeInDuration);
            yield return null;
        }

        audioSource.volume = targetVolume;
    }

    /// <summary>
    /// Método público por si necesitas apagar el ambiente suavemente (ej. al terminar la partida o cambiar de escena).
    /// </summary>
    public void StopAmbientWithFade(float fadeOutDuration = 1.0f)
    {
        StartCoroutine(FadeOutAmbient(fadeOutDuration));
    }

    private IEnumerator FadeOutAmbient(float duration)
    {
        float startVolume = audioSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        audioSource.Stop();
    }
}