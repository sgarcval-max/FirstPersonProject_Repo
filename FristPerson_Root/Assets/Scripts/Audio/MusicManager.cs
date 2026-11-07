using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

/// <summary>
/// Administrador central de música. Controla dos canales independientes (BGM y Tensión)
/// para gestionar fundidos en menú, gameplay y persecuciones de enemigos de forma limpia.
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

    [Header("Música de Tensión / Persecución")]
    [Tooltip("Pista de música de tensión que entra por encima cuando te persigue un enemigo.")]
    [SerializeField] private AudioClip tensionClip;

    [Range(0.05f, 1f)]
    [Tooltip("Porcentaje de volumen al que baja la música ambiental al ser perseguido (ej: 0.3 = 30%).")]
    [SerializeField] private float bgmDuckedVolumeMultiplier = 0.3f;

    [Range(0f, 1f)]
    [Tooltip("Volumen máximo de la música de tensión.")]
    [SerializeField] private float tensionMaxVolume = 0.8f;

    [Tooltip("Velocidad de transición entre la música normal y la de tensión.")]
    [SerializeField] private float tensionFadeSpeed = 2.5f;

    [Header("Configuración de Mezclador")]
    [Tooltip("Arrastra aquí el grupo de música de tu AudioMixer.")]
    [SerializeField] private AudioMixerGroup musicOutputGroup;

    [Header("Transiciones")]
    [Tooltip("Tiempo de fundido (en segundos) al entrar una canción o cambiar de escena.")]
    [SerializeField] private float fadeDuration = 0.8f;

    public PlaylistType CurrentPlaylistType { get; private set; } = PlaylistType.None;

    // Canales de audio generados automáticamente
    private AudioSource bgmAudioSource;
    private AudioSource tensionAudioSource;

    private List<AudioClip> currentList;
    private int currentTrackIndex = 0;

    private Coroutine playlistCoroutine;
    private Coroutine fadeCoroutine;

    private float currentBgmFadeVolume = 1f;
    private float currentDuckFactor = 1f;
    private readonly HashSet<MonoBehaviour> activeChasers = new HashSet<MonoBehaviour>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SetupAudioSources();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void SetupAudioSources()
    {
        // 1. Canal dedicado exclusivamente a la música de fondo (BGM)
        GameObject bgmObj = new GameObject("BGM_AudioChannel");
        bgmObj.transform.SetParent(transform);
        bgmAudioSource = bgmObj.AddComponent<AudioSource>();
        bgmAudioSource.loop = false;
        bgmAudioSource.playOnAwake = false;
        bgmAudioSource.spatialBlend = 0f;
        if (musicOutputGroup != null) bgmAudioSource.outputAudioMixerGroup = musicOutputGroup;

        // 2. Canal dedicado exclusivamente a la música de tensión
        GameObject tensionObj = new GameObject("Tension_AudioChannel");
        tensionObj.transform.SetParent(transform);
        tensionAudioSource = tensionObj.AddComponent<AudioSource>();
        tensionAudioSource.loop = true;
        tensionAudioSource.playOnAwake = false;
        tensionAudioSource.volume = 0f;
        tensionAudioSource.spatialBlend = 0f;
        if (musicOutputGroup != null) tensionAudioSource.outputAudioMixerGroup = musicOutputGroup;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        // Eliminar referencias de guardias destruidos o desactivados
        activeChasers.RemoveWhere(enemy => enemy == null || !enemy.enabled || !enemy.gameObject.activeInHierarchy);

        bool isBeingChased = activeChasers.Count > 0 && CurrentPlaylistType == PlaylistType.Gameplay;

        // 1. Atenuación (Ducking) de la música de fondo
        float targetDuck = isBeingChased ? bgmDuckedVolumeMultiplier : 1f;
        currentDuckFactor = Mathf.MoveTowards(currentDuckFactor, targetDuck, tensionFadeSpeed * Time.unscaledDeltaTime);

        if (bgmAudioSource != null)
        {
            bgmAudioSource.volume = currentBgmFadeVolume * currentDuckFactor;
        }

        // 2. Control dinámico del volumen de tensión
        float targetTensionVol = isBeingChased ? tensionMaxVolume : 0f;
        if (tensionAudioSource != null)
        {
            if (isBeingChased && tensionClip != null)
            {
                if (tensionAudioSource.clip != tensionClip)
                {
                    tensionAudioSource.clip = tensionClip;
                }
                if (!tensionAudioSource.isPlaying)
                {
                    tensionAudioSource.Play();
                }
            }

            tensionAudioSource.volume = Mathf.MoveTowards(tensionAudioSource.volume, targetTensionVol, tensionFadeSpeed * Time.unscaledDeltaTime);

            // Pausar la pista de tensión cuando el volumen llega a 0 para no consumir memoria
            if (tensionAudioSource.volume <= 0.001f && tensionAudioSource.isPlaying && !isBeingChased)
            {
                tensionAudioSource.Pause();
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        activeChasers.Clear();
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

    public void SetChasing(MonoBehaviour enemy, bool isChasing)
    {
        if (enemy == null) return;

        if (isChasing)
        {
            activeChasers.Add(enemy);
        }
        else
        {
            activeChasers.Remove(enemy);
        }
    }

    public void ClearAllChasers()
    {
        activeChasers.Clear();
    }

    public void PlayPlaylist(List<AudioClip> playlist, PlaylistType type)
    {
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
            playlistCoroutine = null;
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
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
                yield return PlayClipWithFadeIn(clipToPlay);

                float trackDuration = clipToPlay.length;
                float elapsed = 0f;
                float waitTime = Mathf.Max(0.1f, trackDuration - fadeDuration);

                while (elapsed < waitTime)
                {
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }

                yield return FadeOutRoutine(fadeDuration);
            }

            currentTrackIndex = (currentTrackIndex + 1) % currentList.Count;
        }
    }

    private IEnumerator PlayClipWithFadeIn(AudioClip clip)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

        bgmAudioSource.clip = clip;
        currentBgmFadeVolume = 0f;
        bgmAudioSource.volume = 0f;
        bgmAudioSource.Play();

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            currentBgmFadeVolume = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            yield return null;
        }

        currentBgmFadeVolume = 1f;
    }

    public void FadeOut(float duration)
    {
        if (playlistCoroutine != null)
        {
            StopCoroutine(playlistCoroutine);
            playlistCoroutine = null;
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(SmoothFadeOutRoutine(duration));
    }

    private IEnumerator SmoothFadeOutRoutine(float duration)
    {
        if (bgmAudioSource == null) yield break;

        float startVol = currentBgmFadeVolume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            currentBgmFadeVolume = Mathf.Lerp(startVol, 0f, elapsed / duration);
            yield return null;
        }

        currentBgmFadeVolume = 0f;
        if (bgmAudioSource != null)
        {
            bgmAudioSource.Stop();
        }

        CurrentPlaylistType = PlaylistType.None;
        currentList = null;
    }

    private IEnumerator FadeOutRoutine(float duration)
    {
        float startVol = currentBgmFadeVolume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            currentBgmFadeVolume = Mathf.Lerp(startVol, 0f, elapsed / duration);
            yield return null;
        }

        currentBgmFadeVolume = 0f;
        if (bgmAudioSource != null)
        {
            bgmAudioSource.Stop();
        }
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

        if (bgmAudioSource != null)
        {
            bgmAudioSource.Stop();
            currentBgmFadeVolume = 0f;
            bgmAudioSource.volume = 0f;
        }

        if (tensionAudioSource != null)
        {
            tensionAudioSource.Stop();
            tensionAudioSource.volume = 0f;
        }

        activeChasers.Clear();
        currentList = null;
        CurrentPlaylistType = PlaylistType.None;
    }
}