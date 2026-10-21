using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

/// <summary>
/// Administrador central de música. Controla listas de reproducción para menú y gameplay,
/// gestionando el paso entre canciones y escenas de forma fluida y sin bloqueos.
/// </summary>
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    public enum PlaylistType { None, Menu, Gameplay }

    [Header("Listas de Reproducción")]
    [Tooltip("Canciones para los menús.")]
    [SerializeField] private List<AudioClip> menuPlaylist = new List<AudioClip>();

    [Tooltip("Canciones para la partida / juego.")]
    [SerializeField] private List<AudioClip> gameplayPlaylist = new List<AudioClip>();

    [Header("Configuración de Mezclador")]
    [Tooltip("Arrastra aquí el grupo de música de tu AudioMixer.")]
    [SerializeField] private AudioMixerGroup musicOutputGroup;

    [Header("Transiciones")]
    [Tooltip("Tiempo de fundido (en segundos) al entrar una canción o cambiar de escena.")]
    [SerializeField] private float fadeDuration = 0.8f;

    public PlaylistType CurrentPlaylistType { get; private set; } = PlaylistType.None;

    private AudioSource audioSource;
    private List<AudioClip> currentList;
    private int currentTrackIndex = 0;

    private Coroutine playlistCoroutine;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitAudioSource();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.loop = false;
        audioSource.playOnAwake = false;
        audioSource.volume = 1f;

        if (musicOutputGroup != null)
        {
            audioSource.outputAudioMixerGroup = musicOutputGroup;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name.ToLower();
        if (sceneName.Contains("menu"))
        {
            PlayPlaylist(menuPlaylist, PlaylistType.Menu);
        }
        else
        {
            PlayPlaylist(gameplayPlaylist, PlaylistType.Gameplay);
        }
    }

    public void PlayPlaylist(List<AudioClip> playlist, PlaylistType type)
    {
        // Si ya está sonando esta misma lista, no la cortamos
        if (CurrentPlaylistType == type && currentList == playlist && playlistCoroutine != null)
        {
            return;
        }

        CurrentPlaylistType = type;
        currentList = playlist;
        currentTrackIndex = 0;

        if (playlistCoroutine != null)
        {
            StopCoroutine(playlistCoroutine);
        }

        if (currentList != null && currentList.Count > 0)
        {
            playlistCoroutine = StartCoroutine(PlaylistLoopRoutine());
        }
        else
        {
            StopMusic();
        }
    }

    private IEnumerator PlaylistLoopRoutine()
    {
        while (currentList != null && currentList.Count > 0)
        {
            AudioClip clipToPlay = currentList[currentTrackIndex];

            if (clipToPlay != null)
            {
                // Reproduce la canción con fundido de entrada (Fade In)
                yield return PlayClipWithFadeIn(clipToPlay);

                // Espera hasta que la canción esté a punto de acabar
                float trackDuration = clipToPlay.length;
                float elapsed = 0f;
                float waitTime = Mathf.Max(0.1f, trackDuration - fadeDuration);

                while (elapsed < waitTime)
                {
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }

                // Fundido de salida (Fade Out) al terminar
                yield return FadeOutRoutine(fadeDuration);
            }

            // Pasa a la siguiente canción de la lista (en bucle)
            currentTrackIndex = (currentTrackIndex + 1) % currentList.Count;
        }
    }

    private IEnumerator PlayClipWithFadeIn(AudioClip clip)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

        audioSource.clip = clip;
        audioSource.volume = 0f;
        audioSource.Play();

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            audioSource.volume = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            yield return null;
        }

        audioSource.volume = 1f;
    }

    private IEnumerator FadeOutRoutine(float duration)
    {
        float startVol = audioSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            audioSource.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
            yield return null;
        }

        audioSource.volume = 0f;
        audioSource.Stop();
    }

    public void StopMusic()
    {
        if (playlistCoroutine != null)
        {
            StopCoroutine(playlistCoroutine);
            playlistCoroutine = null;
        }
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.volume = 0f;
        }

        currentList = null;
        CurrentPlaylistType = PlaylistType.None;
    }
}