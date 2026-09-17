using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// Gestiona el estado de pausa del juego: detecta ESC, congela el tiempo,
/// muestra/oculta los paneles de UI (Pausa y Ajustes), y expone métodos
/// públicos para conectar directamente a los botones del menú desde el Inspector.
/// </summary>
public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("Referencias UI")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;
    [Tooltip("El SettingsNavigationController que vive en el SettingsPanel. Se busca solo si se deja vacío.")]
    [SerializeField] private SettingsNavigationController settingsNavigation;

    [Header("Jugador")]
    [Tooltip("Se busca automáticamente en la escena si se deja vacío.")]
    [SerializeField] private FirstPersonController playerController;

    [Header("Audio")]
    [Tooltip("Se busca automáticamente en la escena si se deja vacío.")]
    [SerializeField] private MusicDucking musicDucking;

    [Header("Escenas")]
    [Tooltip("Nombre exacto de la escena del menú principal (debe estar en Build Settings).")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Eventos")]
    public UnityEvent OnGamePaused;
    public UnityEvent OnGameResumed;

    public bool IsPaused { get; private set; } = false;

    private void Awake()
    {
        // Singleton simple para poder acceder desde cualquier script (ej: bloquear inputs del jugador mientras está pausado)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Aseguramos que al iniciar la escena todo está en estado "jugando"
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        Time.timeScale = 1f;
        IsPaused = false;

        if (settingsNavigation == null && settingsPanel != null)
        {
            settingsNavigation = settingsPanel.GetComponent<SettingsNavigationController>();
        }

        if (playerController == null)
        {
            playerController = FindObjectOfType<FirstPersonController>();
        }

        if (musicDucking == null)
        {
            musicDucking = FindObjectOfType<MusicDucking>();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Si el panel de ajustes está abierto, ESC retrocede una página dentro de Ajustes
            // (o cierra Ajustes del todo si ya estamos en la página principal). Esto lo decide
            // SettingsNavigationController.GoBack(), que sabe en qué página estamos.
            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                if (settingsNavigation != null)
                {
                    settingsNavigation.GoBack();
                }
                else
                {
                    CloseSettings();
                }
                return;
            }

            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (IsPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        IsPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null) pausePanel.SetActive(true);
        if (playerController != null) playerController.enabled = false;
        if (musicDucking != null) musicDucking.Duck();
        if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayPanelOpen();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        OnGamePaused?.Invoke();
    }

    public void Resume()
    {
        IsPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (playerController != null) playerController.enabled = true;
        if (musicDucking != null) musicDucking.Restore();
        if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayPanelClose();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        OnGameResumed?.Invoke();
    }

    /// <summary>Abre el panel de Ajustes desde el menú de pausa.</summary>
    public void OpenSettings()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayPanelOpen();
    }

    /// <summary>Cierra Ajustes y vuelve al menú de pausa (no reanuda el juego).</summary>
    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(true);
        if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayPanelClose();
    }

    /// <summary>Vuelve al menú principal. Importante: siempre restaurar Time.timeScale antes de cambiar de escena.</summary>
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    /// <summary>Sale de la aplicación. En el Editor de Unity esto no cierra nada (comportamiento normal de Unity).</summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}