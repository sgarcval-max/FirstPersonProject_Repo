using System;
using UnityEngine;
using TMPro;

/// <summary>
/// Gestiona la partida: una cuenta atrás que, al llegar a 0, termina la partida (no es perder: es
/// el final normal). Al terminar, calcula la puntuación final, comprueba si es un nuevo récord (que
/// se guarda en el dispositivo), congela el juego y avisa con OnMatchEnded para que salga la
/// pantalla de resultados.
///
/// La cuenta atrás usa el tiempo del juego, así que se detiene sola en la pausa y mientras sale el
/// Game Over, y sigue por donde iba cuando el jugador reaparece (no se reinicia).
///
/// Debe ir en un objeto que esté siempre activo (por ejemplo el Canvas del HUD).
/// </summary>
public class MatchManager : MonoBehaviour
{
    public static MatchManager Instance { get; private set; }

    /// <summary>True cuando el tiempo se ha acabado. La pausa, la captura y otros sistemas lo consultan.</summary>
    public static bool IsMatchOver { get; private set; }

    /// <summary>Clave con la que se guarda la mejor puntuación.</summary>
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

    /// <summary>
    /// Se dispara al terminar la partida. Parámetros: puntuación final, si es un nuevo récord y la
    /// mejor puntuación (incluida esta partida).
    /// </summary>
    public event Action<int, bool, int> OnMatchEnded;

    /// <summary>Segundos que quedan de partida.</summary>
    public float TimeRemaining { get; private set; }

    private FirstPersonController playerController;
    private int lastDisplayedSeconds = -1;

    private void Awake()
    {
        Instance = this;

        // Es estática y sobrevive a recargar la escena, así que se resetea aquí al empezar cada partida.
        IsMatchOver = false;
        TimeRemaining = matchDuration;
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

        // Time.deltaTime vale 0 con el juego en pausa, así que el tiempo se para solo.
        TimeRemaining = Mathf.Max(0f, TimeRemaining - Time.deltaTime);
        RefreshText(false);

        if (TimeRemaining <= 0f)
        {
            EndMatch();
        }
    }

    private void EndMatch()
    {
        IsMatchOver = true;

        int finalScore = ScoreManager.Instance != null ? ScoreManager.Instance.Score : 0;
        int previousBest = PlayerPrefs.GetInt(BestScoreKey, 0);
        bool newRecord = finalScore > previousBest;

        if (newRecord)
        {
            PlayerPrefs.SetInt(BestScoreKey, finalScore);
            PlayerPrefs.Save();
        }

        int best = Mathf.Max(previousBest, finalScore);

        // El jugador pierde el control, aparece el cursor y se congela el juego.
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

        // Solo tocamos el texto cuando cambia el segundo que se muestra.
        if (!force && seconds == lastDisplayedSeconds) return;
        lastDisplayedSeconds = seconds;

        string prefix = GameLanguage.IsEnglish ? "Time: " : "Tiempo: ";
        timerText.text = prefix + (seconds / 60) + ":" + (seconds % 60).ToString("00");
        timerText.color = TimeRemaining <= lowTimeThreshold ? lowTimeColor : normalColor;
    }
}
