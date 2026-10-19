using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Pantalla que sale cuando se acaba el tiempo. Muestra la puntuación final en grande, subiendo
/// desde 0 hasta la cifra con una pequeña animación, el récord, un aviso parpadeante de "nuevo
/// récord" si lo has batido, y dos botones: volver a jugar y volver al menú.
///
/// Debe ir en un objeto que esté siempre activo (por ejemplo el Canvas del HUD), NO en el propio
/// panel, porque el panel empieza oculto. Los textos se ponen aquí según el idioma.
/// </summary>
public class ResultsScreen : MonoBehaviour
{
    [Header("Panel")]
    [Tooltip("El panel de resultados. Se oculta solo al empezar y se muestra al acabar el tiempo.")]
    [SerializeField] private GameObject panel;

    [Header("Textos")]
    [SerializeField] private TMP_Text titleText;
    [Tooltip("El número grande de la puntuación. Es el que se anima.")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text recordText;
    [Tooltip("El aviso de nuevo récord (solo se ve si has batido el récord).")]
    [SerializeField] private TMP_Text newRecordText;
    [SerializeField] private TMP_Text restartButtonText;
    [SerializeField] private TMP_Text menuButtonText;

    [Header("Animación")]
    [Tooltip("Segundos que tarda el número en subir de 0 a la puntuación final.")]
    [SerializeField] private float countDuration = 2.5f;
    [Tooltip("Cuánto se agranda el número al llegar a la cifra final, antes de volver a su tamaño.")]
    [SerializeField] private float popScale = 1.4f;

    [Header("Escenas")]
    [Tooltip("Nombre exacto de la escena del menú principal (debe estar en Build Settings).")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private MatchManager match;

    private void Start()
    {
        if (panel != null) panel.SetActive(false);

        match = MatchManager.Instance;
        if (match != null)
        {
            match.OnMatchEnded += ShowResults;
        }
        else
        {
            Debug.LogWarning("ResultsScreen: no se encontró MatchManager en la escena.");
        }
    }

    private void OnDestroy()
    {
        if (match != null)
        {
            match.OnMatchEnded -= ShowResults;
        }
    }

    private void ShowResults(int finalScore, bool newRecord, int bestScore)
    {
        bool english = GameLanguage.IsEnglish;

        if (titleText != null) titleText.text = english ? "TIME'S UP!" : "¡SE ACABÓ EL TIEMPO!";
        if (recordText != null) recordText.text = (english ? "Best: " : "Récord: ") + bestScore;
        if (newRecordText != null)
        {
            newRecordText.text = english ? "NEW RECORD!" : "¡NUEVO RÉCORD!";
            newRecordText.gameObject.SetActive(false);
        }
        if (restartButtonText != null) restartButtonText.text = english ? "Play again" : "Volver a jugar";
        if (menuButtonText != null) menuButtonText.text = english ? "Main menu" : "Menú principal";
        if (scoreText != null) scoreText.text = "0";

        if (panel != null) panel.SetActive(true);

        if (UIAudioManager.Instance != null)
        {
            UIAudioManager.Instance.PlayPanelOpen();
        }

        StartCoroutine(AnimateScore(finalScore, newRecord));
    }

    private IEnumerator AnimateScore(int finalScore, bool newRecord)
    {
        if (scoreText == null) yield break;

        RectTransform scoreRect = scoreText.rectTransform;

        // 1) El número sube de 0 a la puntuación, rápido al principio y frenando al llegar.
        //    Se usa tiempo sin escalar porque el juego está congelado (Time.timeScale = 0).
        float elapsed = 0f;
        while (elapsed < countDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / countDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            scoreText.text = Mathf.RoundToInt(finalScore * eased).ToString();

            // Un pequeño latido mientras sube, para que se sienta que "trabaja".
            scoreRect.localScale = Vector3.one * (1f + 0.05f * Mathf.Sin(elapsed * 20f));

            yield return null;
        }

        scoreText.text = finalScore.ToString();

        // 2) Al llegar, el número "salta": se agranda de golpe y vuelve a su tamaño.
        float popTime = 0.35f;
        float popElapsed = 0f;
        while (popElapsed < popTime)
        {
            popElapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(popElapsed / popTime);
            float scale = Mathf.Lerp(popScale, 1f, 1f - Mathf.Pow(1f - t, 2f));
            scoreRect.localScale = Vector3.one * scale;
            yield return null;
        }
        scoreRect.localScale = Vector3.one;

        // 3) Si es récord, aparece el aviso y se queda latiendo.
        if (newRecord && newRecordText != null)
        {
            newRecordText.gameObject.SetActive(true);
            RectTransform recordRect = newRecordText.rectTransform;

            while (true)
            {
                float pulse = 1f + 0.12f * Mathf.Sin(Time.unscaledTime * 6f);
                recordRect.localScale = Vector3.one * pulse;
                yield return null;
            }
        }
    }

    /// <summary>Conectar al botón Volver a jugar: empieza una partida nueva desde cero.</summary>
    public void PlayAgain()
    {
        LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>Conectar al botón Menú principal.</summary>
    public void GoToMainMenu()
    {
        LoadScene(mainMenuSceneName);
    }

    private void LoadScene(string sceneName)
    {
        // Al acabar el tiempo se congela el juego: hay que devolverlo a la normalidad antes de cambiar de escena.
        Time.timeScale = 1f;

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadScene(sceneName);
        }
        else
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}
