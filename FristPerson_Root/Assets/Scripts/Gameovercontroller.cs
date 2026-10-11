using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Muestra el panel de Game Over cuando un guardia atrapa al jugador (PlayerCapture), y gestiona
/// sus dos botones: Reiniciar y Menú principal. Los textos se ponen aquí según el idioma, sin
/// depender del LocalizationManager.
///
/// Debe ir en un objeto que esté siempre activo (por ejemplo el Canvas del HUD), NO en el
/// propio panel, porque el panel empieza oculto.
/// </summary>
public class GameOverController : MonoBehaviour
{
    [Header("Panel")]
    [Tooltip("El panel de Game Over. Se oculta solo al empezar y se muestra al ser atrapado.")]
    [SerializeField] private GameObject panel;

    [Header("Textos (opcionales)")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text restartButtonText;
    [SerializeField] private TMP_Text menuButtonText;

    [Header("Escenas")]
    [Tooltip("Nombre exacto de la escena del menú principal (debe estar en Build Settings).")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private PlayerCapture capture;

    private void Start()
    {
        if (panel != null) panel.SetActive(false);

        capture = FindObjectOfType<PlayerCapture>();
        if (capture != null)
        {
            capture.OnCaught += ShowGameOver;
        }
        else
        {
            Debug.LogWarning("GameOverController: no se encontró PlayerCapture en la escena.");
        }
    }

    private void OnDestroy()
    {
        if (capture != null)
        {
            capture.OnCaught -= ShowGameOver;
        }
    }

    private void ShowGameOver()
    {
        ApplyTexts();

        if (panel != null) panel.SetActive(true);

        if (UIAudioManager.Instance != null)
        {
            UIAudioManager.Instance.PlayPanelOpen();
        }
    }

    private void ApplyTexts()
    {
        bool english = GameLanguage.IsEnglish;

        if (titleText != null) titleText.text = "GAME OVER";
        if (restartButtonText != null) restartButtonText.text = english ? "Restart" : "Reiniciar";
        if (menuButtonText != null) menuButtonText.text = english ? "Main menu" : "Menú principal";
    }

    /// <summary>Conectar al botón Reiniciar. De momento recarga la escena entera.</summary>
    public void Restart()
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
        // Al ser atrapado congelamos el tiempo: hay que devolverlo a la normalidad antes de cambiar de escena.
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
