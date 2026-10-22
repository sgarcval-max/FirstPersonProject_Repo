using System;
using UnityEngine;
using TMPro;

/// <summary>
/// Gestiona la partida y la cuenta atrás del HUD (mostrando únicamente el tiempo formateado).
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

    [Header("Aviso Sonoro de Tiempo")]
    [Tooltip("Segundos restantes para reproducir el efecto de sonido de la cuenta atrás.")]
    [SerializeField] private float warningSoundThreshold = 10f;

    public event Action<int, bool, int> OnMatchEnded;
    public float TimeRemaining { get; private set; }

    private FirstPersonController playerController;
    private int lastDisplayedSeconds = -1;
    private bool hasPlayedWarningSound = false;

    private void Awake()
    {
        Instance = this;
        IsMatchOver = false;
        TimeRemaining = matchDuration;
        hasPlayedWarningSound = false;
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

        TimeRemaining = Mathf.Max(0f, TimeRemaining - Time.deltaTime);
        RefreshText(false);

        // COMPROBACIÓN DEL EFECTO DE SONIDO (10 SEGUNDOS)
        if (!hasPlayedWarningSound && TimeRemaining <= warningSoundThreshold && TimeRemaining > 0f)
        {
            hasPlayedWarningSound = true;

            if (UIAudioManager.Instance != null)
            {
                UIAudioManager.Instance.PlayCountdownWarning(true);
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

        // Detener el sonido de la cuenta atrás al acabar la partida
        if (UIAudioManager.Instance != null)
        {
            UIAudioManager.Instance.StopCountdownWarning();
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

        // Muestra únicamente el tiempo en formato MM:SS
        timerText.text = (seconds / 60) + ":" + (seconds % 60).ToString("00");
        timerText.color = TimeRemaining <= lowTimeThreshold ? lowTimeColor : normalColor;
    }
}