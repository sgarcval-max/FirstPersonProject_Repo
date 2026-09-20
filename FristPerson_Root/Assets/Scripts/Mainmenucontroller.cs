using UnityEngine;

/// <summary>
/// Versión simplificada para el Menú Principal: abre y cierra el SettingsPanel
/// sin tocar Time.timeScale ni buscar un jugador (a diferencia de PauseManager,
/// que está pensado para la escena de juego).
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [SerializeField] private GameObject settingsPanel;
    [Tooltip("El panel con los botones Jugar/Ajustes/Salir. Se activa automáticamente al arrancar la escena.")]
    [SerializeField] private GameObject mainMenuPage;
    [Tooltip("Nombre exacto de la escena de juego (debe estar en Build Settings).")]
    [SerializeField] private string gameSceneName = "Game";

    private void Start()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainMenuPage != null) mainMenuPage.SetActive(true);
    }

    /// <summary>Conectar al botón "Jugar" del Menú Principal.</summary>
    public void PlayGame()
    {
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadScene(gameSceneName);
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(gameSceneName);
        }
    }

    /// <summary>Conectar al botón "Ajustes" del Menú Principal.</summary>
    public void OpenSettings()
    {
        if (mainMenuPage != null) mainMenuPage.SetActive(false);
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayPanelOpen();
        }
    }

    /// <summary>Conectar al botón "Volver" de la página principal de Ajustes, cuando estás en el Menú.</summary>
    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayPanelClose();
        }
        if (mainMenuPage != null) mainMenuPage.SetActive(true);
    }

    /// <summary>Conectar al botón "Salir del juego" del Menú Principal.</summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}