using System;
using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Gestiona la partida y la cuenta atrás del HUD.
/// Pausa el tiempo automáticamente mientras el TutorialManager esté activo.
/// Oculta el reloj fuera de pantalla si hay tutorial y lo anima con SFX al finalizar.
/// </summary>
public class MatchManager : MonoBehaviour
{
    public static MatchManager Instance { get; private set; }
    public static bool IsMatchOver { get; private set; }
    public const string BestScoreKey = "best_score";

    [Header("Partida")]
    [Tooltip("Duración de la partida en segundos (300 = 5 minutos).")]
    [SerializeField] private float matchDuration = 300f;

    [Header("Cuenta atrás en pantalla")]
    [Tooltip("El texto del HUD donde se muestra el tiempo restante.")]
    [SerializeField] private TMP_Text timerText;
    [Tooltip("Cuando queden estos segundos o menos, el tiempo se pone de otro color.")]
    [SerializeField] private float lowTimeThreshold = 30f;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color lowTimeColor = new Color(1f, 0.25f, 0.2f, 1f);

    [Header("Animación de Entrada del Reloj (Marco + Texto)")]
    [Tooltip("Contenedor padre (RectTransform) que agrupa el Marco y el Texto del temporizador.")]
    [SerializeField] private RectTransform timerContainer;
    [Tooltip("Offset en píxeles para esconder el reloj fuera de pantalla (ej: Y = 500 para moverlo hacia arriba).")]
    [SerializeField] private Vector2 offScreenOffset = new Vector2(0f, 500f);
    [Tooltip("Duración del deslizamiento de entrada.")]
    [SerializeField] private float timerEntryDuration = 0.5f;
    [Tooltip("Sonido (SFX) que suena cuando el marco y el temporizador entran en pantalla.")]
    [SerializeField] private AudioClip timerEntrySFX;
    [Tooltip("AudioSource para reproducir el SFX de entrada. Si se deja vacío, usará la cámara principal.")]
    [SerializeField] private AudioSource audioSource;

    [Header("Aviso Sonoro de Tiempo")]
    [Tooltip("Segundos restantes para reproducir el efecto de sonido de la cuenta atrás.")]
    [SerializeField] private float warningSoundThreshold = 10f;

    public event Action<int, bool, int> OnMatchEnded;
    public float TimeRemaining { get; private set; }

    private FirstPersonController playerController;
    private int lastDisplayedSeconds = -1;
    private int lastAudioWarningSecond = -1;
    private bool hasStartedMusicFade = false;

    private Vector2 originalTimerPosition;
    private bool isAnimatingEntry = false;

    private void Awake()
    {
        Instance = this;
        IsMatchOver = false;
        TimeRemaining = matchDuration;
        hasStartedMusicFade = false;
        lastAudioWarningSecond = -1;
    }

    private void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += ForceRefreshText;
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= ForceRefreshText;
    }

    private void Start()
    {
        playerController = FindObjectOfType<FirstPersonController>();
        ForceRefreshText();

        StartCoroutine(SetupTimerUIStateRoutine());
    }

    /// <summary>
    /// Sincroniza la posición del reloj esperando a que todos los scripts de la escena estén listos.
    /// </summary>
    private IEnumerator SetupTimerUIStateRoutine()
    {
        if (timerContainer == null) yield break;

        // Guardamos la posición original deseada en pantalla
        originalTimerPosition = timerContainer.anchoredPosition;

        // Ocultado instantáneo preventivo si el tutorial no está completado en PlayerPrefs
        if (PlayerPrefs.GetInt("TutorialCompleted", 0) == 0)
        {
            timerContainer.anchoredPosition = originalTimerPosition + offScreenOffset;
        }

        // Esperamos 1 frame para garantizar que TutorialManager.Start() haya configurado sus variables de estado
        yield return null;

        bool isTutorialActive = TutorialManager.IsPlayerProtected || PlayerPrefs.GetInt("TutorialCompleted", 0) == 0;

        if (isTutorialActive)
        {
            // Mover fuera de pantalla
            timerContainer.anchoredPosition = originalTimerPosition + offScreenOffset;

            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.OnTutorialFinished.RemoveListener(OnTutorialCompletedHandler);
                TutorialManager.Instance.OnTutorialFinished.AddListener(OnTutorialCompletedHandler);
            }
        }
        else
        {
            // Si el tutorial ya fue completado, asegurar posición visible original
            timerContainer.anchoredPosition = originalTimerPosition;
        }
    }

    private void OnTutorialCompletedHandler()
    {
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.OnTutorialFinished.RemoveListener(OnTutorialCompletedHandler);
        }

        StartCoroutine(AnimateTimerEntryRoutine());
    }

    private IEnumerator AnimateTimerEntryRoutine()
    {
        isAnimatingEntry = true;
        PlayEntrySFX();

        Vector2 startPos = timerContainer.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < timerEntryDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / timerEntryDuration;

            // Curva de suavizado (Ease Out)
            t = Mathf.Sin(t * Mathf.PI * 0.5f);

            timerContainer.anchoredPosition = Vector2.Lerp(startPos, originalTimerPosition, t);
            yield return null;
        }

        timerContainer.anchoredPosition = originalTimerPosition;
        isAnimatingEntry = false;
    }

    private void PlayEntrySFX()
    {
        if (timerEntrySFX == null) return;

        if (audioSource != null)
        {
            audioSource.PlayOneShot(timerEntrySFX);
        }
        else if (Camera.main != null)
        {
            AudioSource.PlayClipAtPoint(timerEntrySFX, Camera.main.transform.position);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (IsMatchOver) return;

        // Si el tutorial o la pantalla de bienvenida están activos, el tiempo no avanza
        if (TutorialManager.IsPlayerProtected) return;

        TimeRemaining = Mathf.Max(0f, TimeRemaining - Time.deltaTime);
        RefreshText(false);

        // REPRODUCCIÓN DEL SFX ESPECÍFICO DE CADA SEGUNDO (10 a 1)
        if (TimeRemaining <= warningSoundThreshold && TimeRemaining > 0f)
        {
            int currentSecond = Mathf.CeilToInt(TimeRemaining);

            if (currentSecond != lastAudioWarningSecond)
            {
                lastAudioWarningSecond = currentSecond;

                if (UIAudioManager.Instance != null)
                {
                    UIAudioManager.Instance.PlayCountdownWarning(currentSecond);
                }
            }

            if (!hasStartedMusicFade)
            {
                hasStartedMusicFade = true;

                if (MusicManager.Instance != null)
                {
                    MusicManager.Instance.FadeOut(TimeRemaining);
                }
            }
        }

        if (TimeRemaining <= 0f)
        {
            EndMatch();
        }
    }

    private void EndMatch()
    {
        IsMatchOver = true;

        if (UIAudioManager.Instance != null)
        {
            UIAudioManager.Instance.StopCountdownWarning();
        }

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.StopMusic();
        }

        int finalScore = ScoreManager.Instance != null ? ScoreManager.Instance.Score : 0;
        int previousBest = PlayerPrefs.GetInt(BestScoreKey, 0);
        bool newRecord = finalScore > previousBest;

        if (newRecord)
        {
            PlayerPrefs.SetInt(BestScoreKey, finalScore);
            PlayerPrefs.Save();
        }

        int best = Mathf.Max(previousBest, finalScore);

        if (playerController != null)
        {
            playerController.enabled = false;
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;

        Debug.Log("[Partida] Fin del tiempo. Puntuación: " + finalScore + (newRecord ? " (nuevo récord)" : "") + ".");

        if (OnMatchEnded != null) OnMatchEnded.Invoke(finalScore, newRecord, best);
    }

    private void ForceRefreshText()
    {
        RefreshText(true);
    }

    private void RefreshText(bool force)
    {
        if (timerText == null) return;

        int seconds = Mathf.CeilToInt(TimeRemaining);

        if (!force && seconds == lastDisplayedSeconds) return;
        lastDisplayedSeconds = seconds;

        timerText.text = (seconds / 60) + ":" + (seconds % 60).ToString("00");
        timerText.color = TimeRemaining <= lowTimeThreshold ? lowTimeColor : normalColor;
    }
}