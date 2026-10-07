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
    [Tooltip("El ExitConfirmationController del panel de salida. Se busca solo si se deja vacío.")]
    [SerializeField] private ExitConfirmationController exitConfirmation;

    [Header("Jugador")]
    [Tooltip("Se busca automáticamente en la escena si se deja vacío.")]
    [SerializeField] private FirstPersonController playerController;

    [Header("Audio")]
    [Tooltip("Se busca automáticamente en la escena si se deja vacío.")]
    [SerializeField] private MusicDucking musicDucking;

    [Header("Escenas")]
    [Tooltip("Nombre exacto de la escena del menú principal (debe estar en Build Settings).")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Activar/Desactivar (para el Menú Principal)")]
    [Tooltip("Actívalo en la escena de JUEGO. Desactívalo en la instancia del MENÚ PRINCIPAL (sobrescritura de instancia del Prefab) para que ESC no haga nada relacionado con pausa ahí.")]
    [SerializeField] private bool enablePauseFunctionality = true;

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

        if (exitConfirmation == null)
        {
            exitConfirmation = FindObjectOfType<ExitConfirmationController>(true); // true = incluir objetos inactivos
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Estas dos comprobaciones (salir y ajustes) funcionan SIEMPRE, tanto en el
            // Juego como en el Menú Principal, independientemente de "Enable Pause Functionality".
            if (exitConfirmation != null && exitConfirmation.IsOpen)
            {
                exitConfirmation.OnCancelPressed();
                return;
            }

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

            // Esto SÍ depende del interruptor: en el Menú Principal no hay "pausa" que alternar.
            if (enablePauseFunctionality)
            {
                TogglePause();
            }
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

    /// <summary>Cierra Ajustes. En el Juego vuelve al menú de pausa; en el Menú Principal vuelve a MainMenuPage.</summary>
    public void CloseSettings()
    {
        if (!enablePauseFunctionality)
        {
            // Estamos en el Menú Principal (no en el Juego): delega a MainMenuController
            MainMenuController mainMenu = FindObjectOfType<MainMenuController>();
            if (mainMenu != null)
            {
                mainMenu.CloseSettings();
                return;
            }
        }

        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(true);
        if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayPanelClose();
    }

    /// <summary>Vuelve al menú principal. Importante: siempre restaurar Time.timeScale antes de cambiar de escena.</summary>
    public void GoToMainMenu()
    {
        Time.timeScale = 1f;

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadScene(mainMenuSceneName);
        }
        else
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
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