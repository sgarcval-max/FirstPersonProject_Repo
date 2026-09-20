using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Gestiona el cambio de escena con un fundido a negro sincronizado con la música:
/// 1) Funde la pantalla a negro MIENTRAS la música baja hasta silenciarse.
/// 2) Carga la nueva escena (con la pantalla ya en negro).
/// 3) Funde la pantalla de vuelta a transparente MIENTRAS la música sube a su volumen guardado.
///
/// Este objeto sobrevive al cambio de escena (DontDestroyOnLoad), así que el fundido
/// no se corta a mitad de camino. Debe existir UNA copia de este objeto en cada escena
/// (Menú y Juego) con el patrón singleton: la segunda que se encuentre se autodestruye.
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Referencias")]
    [SerializeField] private CanvasGroup fadeCanvasGroup;
    [SerializeField] private AudioMixer audioMixer;

    [Header("Duración")]
    [SerializeField] private float fadeOutDuration = 1f;
    [SerializeField] private float fadeInDuration = 1f;

    private const string MUSIC_PARAM = "MusicVolume";
    private const string MUSIC_KEY = "volume_music";
    private const float SILENT_DB = -80f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = 0f;
    }

    /// <summary>Llamar desde los botones "Jugar" o "Salir al Menú Principal" en vez de cargar la escena directamente.</summary>
    public void LoadScene(string sceneName)
    {
        StartCoroutine(TransitionRoutine(sceneName));
    }

    private IEnumerator TransitionRoutine(string sceneName)
    {
        yield return FadeOut();

        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);
        while (!loadOperation.isDone)
        {
            yield return null;
        }

        yield return FadeIn();
    }

    private float GetSavedMusicDB()
    {
        float linear = PlayerPrefs.GetFloat(MUSIC_KEY, 1f);
        float clamped = Mathf.Max(linear, 0.0001f);
        return Mathf.Log10(clamped) * 20f;
    }

    private IEnumerator FadeOut()
    {
        float savedDB = GetSavedMusicDB();
        float elapsed = 0f;

        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutDuration);

            if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            SetMusicDB(Mathf.Lerp(savedDB, SILENT_DB, t));

            yield return null;
        }

        if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = 1f;
        SetMusicDB(SILENT_DB);
    }

    private IEnumerator FadeIn()
    {
        // Forzamos silencio de nuevo por si el AudioBootstrapper de la escena nueva
        // ya restauró el volumen a su valor guardado en su propio Awake().
        SetMusicDB(SILENT_DB);

        float savedDB = GetSavedMusicDB();
        float elapsed = 0f;

        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeInDuration);

            if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            SetMusicDB(Mathf.Lerp(SILENT_DB, savedDB, t));

            yield return null;
        }

        if (fadeCanvasGroup != null) fadeCanvasGroup.alpha = 0f;
        SetMusicDB(savedDB);
    }

    private void SetMusicDB(float db)
    {
        if (audioMixer != null) audioMixer.SetFloat(MUSIC_PARAM, db);
    }
}
