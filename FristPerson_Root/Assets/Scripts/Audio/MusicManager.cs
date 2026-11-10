using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

/// <summary>
/// Administrador central de música.
/// Acelera la música durante persecuciones, soporta bucles continuos para 1 sola canción,
/// realiza transiciones fluidas entre múltiples canciones leyendo el tiempo real del reproductor,
/// soporta Alt+Tab (pérdida de foco de ventana) sin saltar canciones por error,
/// y pausa/reanuda la música automáticamente al abrir el menú de pausa (Time.timeScale == 0).
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

    [Header("Efecto de Tensión en Persecución")]
    [Tooltip("Multiplicador de velocidad/pitch al ser perseguido (ej: 1.35 = 35% más rápido).")]
    [SerializeField] private float chasePitchMultiplier = 1.35f;

    [Tooltip("Velocidad a la que la música acelera o desacelera en las transiciones.")]
    [SerializeField] private float pitchChangeSpeed = 2f;

    [Header("Configuración de Mezclador")]
    [Tooltip("Arrastra aquí el grupo de música de tu AudioMixer.")]
    [SerializeField] private AudioMixerGroup musicOutputGroup;

    [Header("Transiciones")]
    [Tooltip("Tiempo de fundido (en segundos) al cambiar de canción o de escena.")]
    [SerializeField] private float fadeDuration = 0.8f;

    public PlaylistType CurrentPlaylistType { get; private set; } = PlaylistType.None;

    // Canal de audio principal (BGM)
    private AudioSource bgmAudioSource;

    private List<AudioClip> currentList;
    private int currentTrackIndex = 0;

    private Coroutine playlistCoroutine;
    private Coroutine fadeCoroutine;

    private float currentBgmFadeVolume = 1f;
    private readonly HashSet<MonoBehaviour> activeChasers = new HashSet<MonoBehaviour>();

    private bool isAudioPausedByGame = false;

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
        GameObject bgmObj = new GameObject("BGM_AudioChannel");
        bgmObj.transform.SetParent(transform);
        bgmAudioSource = bgmObj.AddComponent<AudioSource>();
        bgmAudioSource.loop = false;
        bgmAudioSource.playOnAwake = false;
        bgmAudioSource.spatialBlend = 0f;
        bgmAudioSource.pitch = 1f;
        if (musicOutputGroup != null) bgmAudioSource.outputAudioMixerGroup = musicOutputGroup;
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
        // 1. Control automático de Pausa cuando Time.timeScale es 0 (Menú de pausa activo)
        bool isPaused = Time.timeScale == 0f;

        if (isPaused && !isAudioPausedByGame)
        {
            isAudioPausedByGame = true;
            if (bgmAudioSource != null && bgmAudioSource.isPlaying)
            {
                bgmAudioSource.Pause();
            }
        }
        else if (!isPaused && isAudioPausedByGame)
        {
            isAudioPausedByGame = false;
            if (bgmAudioSource != null)
            {
                bgmAudioSource.UnPause();
            }
        }

        // Si el juego está en pausa, congelamos las actualizaciones de volumen y pitch
        if (isAudioPausedByGame) return;

        // 2. Limpiar guardias destruidos o desactivados
        activeChasers.RemoveWhere(enemy => enemy == null || !enemy.enabled || !enemy.gameObject.activeInHierarchy);

        // Si el jugador fue atrapado (Game Over), desactivar tensión de inmediato
        if (PlayerCapture.IsCaught)
        {
            activeChasers.Clear();
        }

        bool isBeingChased = activeChasers.Count > 0 && CurrentPlaylistType == PlaylistType.Gameplay && !PlayerCapture.IsCaught;

        // Determinar velocidad (1.35x en persecución, 1x en normal)
        float targetPitch = isBeingChased ? chasePitchMultiplier : 1f;

        if (bgmAudioSource != null)
        {
            bgmAudioSource.volume = currentBgmFadeVolume;
            bgmAudioSource.pitch = Mathf.MoveTowards(bgmAudioSource.pitch, targetPitch, pitchChangeSpeed * Time.unscaledDeltaTime);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ClearAllChasers();
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
        if (bgmAudioSource != null)
        {
            bgmAudioSource.pitch = 1f;
        }
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

        if (currentList != null && currentList.Count > 0 && HasValidClips(currentList))
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
        while (currentList != null && currentList.Count > 0 && HasValidClips(currentList))
        {
            // SI SOLO HAY 1 CANCIÓN: Se reproduce en bucle continuo sin fundidos intermedios
            if (currentList.Count == 1)
            {
                AudioClip singleClip = currentList[0];
                if (singleClip != null)
                {
                    bgmAudioSource.loop = true;
                    yield return PlayClipWithFadeIn(singleClip);

                    while (currentList != null && currentList.Count == 1 && currentList[0] == singleClip)
                    {
                        yield return null;
                    }
                }
                else
                {
                    yield return null;
                }
            }
            // SI HAY VARIAS CANCIONES: Pasa a la siguiente en orden con fundido
            else
            {
                bgmAudioSource.loop = false;
                AudioClip clipToPlay = currentList[currentTrackIndex];

                if (clipToPlay != null)
                {
                    yield return PlayClipWithFadeIn(clipToPlay);

                    // Esperamos 1 frame para asegurar que el AudioSource ha comenzado a reproducir
                    yield return null;

                    // Esperar mientras la canción suena hasta que queden 'fadeDuration' segundos de pista
                    while (bgmAudioSource != null && bgmAudioSource.clip == clipToPlay)
                    {
                        // Si se perdió el foco de la ventana (Alt+Tab), el menú de pausa está activo o el AudioListener silenciado,
                        // congelamos la corrutina aquí sin pasar de canción.
                        if (!Application.isFocused || isAudioPausedByGame || AudioListener.pause)
                        {
                            yield return null;
                            continue;
                        }

                        float timeRemaining = clipToPlay.length - bgmAudioSource.time;

                        // Si la canción realmente está a punto de acabar o ya finalizó de forma natural
                        if (timeRemaining <= fadeDuration || !bgmAudioSource.isPlaying)
                        {
                            break;
                        }

                        yield return null;
                    }

                    // Fundido de salida suave al final de la canción antes de pasar a la siguiente
                    yield return FadeOutRoutine(fadeDuration);
                }
                else
                {
                    yield return null;
                }

                currentTrackIndex = (currentTrackIndex + 1) % currentList.Count;
            }
        }

        StopMusic();
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
            if (Time.timeScale > 0f)
            {
                elapsed += Time.unscaledDeltaTime;
                currentBgmFadeVolume = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            }
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
            if (Time.timeScale > 0f)
            {
                elapsed += Time.unscaledDeltaTime;
                currentBgmFadeVolume = Mathf.Lerp(startVol, 0f, elapsed / duration);
            }
            yield return null;
        }

        currentBgmFadeVolume = 0f;
        if (bgmAudioSource != null)
        {
            bgmAudioSource.Stop();
            bgmAudioSource.pitch = 1f;
            bgmAudioSource.loop = false;
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
            if (Time.timeScale > 0f)
            {
                elapsed += Time.unscaledDeltaTime;
                currentBgmFadeVolume = Mathf.Lerp(startVol, 0f, elapsed / duration);
            }
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
            bgmAudioSource.pitch = 1f;
            bgmAudioSource.loop = false;
        }

        activeChasers.Clear();
        currentList = null;
        CurrentPlaylistType = PlaylistType.None;
    }

    private bool HasValidClips(List<AudioClip> list)
    {
        if (list == null) return false;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != null) return true;
        }
        return false;
    }
}