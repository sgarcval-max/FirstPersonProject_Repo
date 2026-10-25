using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Gestiona la secuencia interactiva del tutorial soportando múltiples Action Maps (Movimiento y Combate),
/// reasignación dinámica de teclas (RebindActionUI), formateo limpio en español (Clic Derecho, Clic Izquierdo, CTRL, etc.)
/// y control automático del cursor y scripts del jugador.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    private const string TUTORIAL_KEY = "TutorialCompleted";

    public enum ActionType
    {
        Movement,
        JumpAndCrouch,
        Shoot,
        Aim,
        DropWeapon
    }

    [System.Serializable]
    public class TutorialStep
    {
        public string stepTitle;
        [TextArea(2, 4)]
        [Tooltip("Usa las etiquetas {MOVE}, {JUMP}, {CROUCH}, {SHOOT}, {AIM}, {DROP}. ¡No escribas las teclas a mano!")]
        public string stepDescription;
        public ActionType requiredAction;
    }

    [Header("1. Nombres de los Action Maps")]
    [Tooltip("Nombre del mapa donde están Move, Jump, Crouch...")]
    [SerializeField] private string movementMapName = "Movimiento";
    [Tooltip("Nombre del mapa donde están Aim, Drop, Throw...")]
    [SerializeField] private string combatMapName = "Combate";

    [Header("2. Nombres de Acciones en 'Movimiento'")]
    [SerializeField] private string moveActionName = "Move";
    [SerializeField] private string jumpActionName = "Jump";
    [SerializeField] private string crouchActionName = "Crouch";

    [Header("3. Nombres de Acciones en 'Combate'")]
    [SerializeField] private string shootActionName = "Throw";
    [SerializeField] private string aimActionName = "Aim";
    [SerializeField] private string dropActionName = "Drop";

    [Header("4. Control de Jugador y Armas")]
    [Tooltip("Asigna el script de movimiento del jugador para pausarlo en la pantalla de bienvenida.")]
    [SerializeField] private MonoBehaviour playerMovementScript;
    [Tooltip("Asigna el script de disparos/armas para evitar disparar al pulsar 'Aceptar'.")]
    [SerializeField] private MonoBehaviour weaponScript;

    [Header("5. UI del Tutorial")]
    [SerializeField] private GameObject welcomePanel;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button cancelButton;

    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TextMeshProUGUI stepCountText;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Header("6. Pasos del Tutorial")]
    [SerializeField] private List<TutorialStep> steps = new List<TutorialStep>();

    // Referencias a las InputActions resueltas desde InputManager.Controls
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction crouchAction;
    private InputAction shootAction;
    private InputAction aimAction;
    private InputAction dropAction;

    private int currentStepIndex = 0;
    private bool isTutorialActive = false;
    private bool hasJumped = false;
    private bool hasCrouched = false;

    private void Awake()
    {
        InitializeInputActions();
    }

    private void Start()
    {
        if (PlayerPrefs.GetInt(TUTORIAL_KEY, 0) == 1)
        {
            EnablePlayerControls(true);
            SetCursorState(false);
            DisableAllPanels();
            return;
        }

        SetupWelcomeUI();
    }

    private void InitializeInputActions()
    {
        if (InputManager.Controls == null)
        {
            Debug.LogError("TutorialManager: InputManager.Controls no está inicializado.");
            return;
        }

        InputActionAsset asset = InputManager.Controls.asset;

        // Cargar las reasignaciones guardadas de PlayerPrefs
        RebindActionUI.LoadSavedRebinds(asset);

        // 1. Mapa Movimiento
        InputActionMap moveMap = asset.FindActionMap(movementMapName);
        if (moveMap != null)
        {
            moveAction = moveMap.FindAction(moveActionName);
            jumpAction = moveMap.FindAction(jumpActionName);
            crouchAction = moveMap.FindAction(crouchActionName);
        }
        else
        {
            Debug.LogError($"TutorialManager: No se encontró el ActionMap '{movementMapName}'.");
        }

        // 2. Mapa Combate
        InputActionMap combatMap = asset.FindActionMap(combatMapName);
        if (combatMap != null)
        {
            shootAction = combatMap.FindAction(shootActionName);
            aimAction = combatMap.FindAction(aimActionName);
            dropAction = combatMap.FindAction(dropActionName);
        }
        else
        {
            Debug.LogError($"TutorialManager: No se encontró el ActionMap '{combatMapName}'.");
        }
    }

    private void SetupWelcomeUI()
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(false);

        EnablePlayerControls(false);
        SetCursorState(true);

        if (welcomePanel != null)
        {
            welcomePanel.SetActive(true);

            if (acceptButton != null)
            {
                acceptButton.onClick.RemoveAllListeners();
                acceptButton.onClick.AddListener(OnAcceptTutorial);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveAllListeners();
                cancelButton.onClick.AddListener(OnSkipTutorial);
            }
        }
    }

    public void OnAcceptTutorial()
    {
        if (welcomePanel != null) welcomePanel.SetActive(false);

        if (steps == null || steps.Count == 0)
        {
            CompleteTutorial();
            return;
        }

        isTutorialActive = true;
        currentStepIndex = 0;

        EnablePlayerControls(true);
        SetCursorState(false);

        if (tutorialPanel != null) tutorialPanel.SetActive(true);

        ShowCurrentStep();
    }

    public void OnSkipTutorial()
    {
        CompleteTutorial();
    }

    private void ShowCurrentStep()
    {
        if (currentStepIndex >= steps.Count)
        {
            CompleteTutorial();
            return;
        }

        hasJumped = false;
        hasCrouched = false;

        TutorialStep current = steps[currentStepIndex];

        if (stepCountText != null) stepCountText.text = $"{currentStepIndex + 1}/{steps.Count}";
        if (titleText != null) titleText.text = current.stepTitle;

        if (descriptionText != null)
        {
            descriptionText.text = FormatDescription(current.stepDescription);
        }
    }

    private string FormatDescription(string rawText)
    {
        string formatted = rawText;

        formatted = formatted.Replace("{MOVE}", GetDisplayString(moveAction, "WASD"));
        formatted = formatted.Replace("{JUMP}", GetDisplayString(jumpAction, "ESPACIO"));
        formatted = formatted.Replace("{CROUCH}", GetDisplayString(crouchAction, "CTRL"));

        // Formateo del botón de disparo / uso
        string shootText = GetDisplayString(shootAction, "Clic Izquierdo");
        if (string.IsNullOrEmpty(shootText) || shootText.Equals("E", System.StringComparison.OrdinalIgnoreCase))
        {
            shootText = "Clic Izquierdo / E";
        }
        formatted = formatted.Replace("{SHOOT}", shootText);

        // Formateo del botón de apuntar
        string aimText = GetDisplayString(aimAction, "Clic Derecho");
        formatted = formatted.Replace("{AIM}", aimText);

        formatted = formatted.Replace("{DROP}", GetDisplayString(dropAction, "G"));

        return formatted;
    }

    /// <summary>
    /// Obtiene el nombre del binding actual y traduce términos en inglés / siglas (RMB, LMB, Left Control...)
    /// a nombres limpios en español.
    /// </summary>
    private string GetDisplayString(InputAction action, string fallback)
    {
        if (action == null) return fallback;

        // Cargar la última configuración activa
        if (InputManager.Controls != null)
        {
            RebindActionUI.LoadSavedRebinds(InputManager.Controls.asset);
        }

        string display = action.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);

        if (string.IsNullOrEmpty(display)) return fallback;

        // TRADUCCIÓN Y FORMATEO DE TECLAS Y BOTONES DE RATÓN
        display = display.Replace("RMB", "Clic Derecho")
                         .Replace("LMB", "Clic Izquierdo")
                         .Replace("MMB", "Clic Central")
                         .Replace("Right Button", "Clic Derecho")
                         .Replace("Left Button", "Clic Izquierdo")
                         .Replace("Middle Button", "Clic Central")
                         .Replace("Right Mouse", "Clic Derecho")
                         .Replace("Left Mouse", "Clic Izquierdo")
                         .Replace("Left Control", "CTRL")
                         .Replace("Right Control", "CTRL")
                         .Replace("Left Shift", "SHIFT")
                         .Replace("Right Shift", "SHIFT")
                         .Replace("Left Alt", "ALT")
                         .Replace("Right Alt", "ALT")
                         .Replace("Space", "ESPACIO");

        return display;
    }

    private void Update()
    {
        if (!isTutorialActive || currentStepIndex >= steps.Count) return;

        CheckCurrentStepCompletion();
    }

    private void CheckCurrentStepCompletion()
    {
        TutorialStep current = steps[currentStepIndex];

        switch (current.requiredAction)
        {
            case ActionType.Movement:
                if (moveAction != null && moveAction.IsPressed()) AdvanceStep();
                break;

            case ActionType.JumpAndCrouch:
                if (jumpAction != null && jumpAction.WasPressedThisFrame()) hasJumped = true;
                if (crouchAction != null && crouchAction.WasPressedThisFrame()) hasCrouched = true;
                if (hasJumped && hasCrouched) AdvanceStep();
                break;

            case ActionType.Shoot:
                bool actionShootPressed = shootAction != null && shootAction.WasPressedThisFrame();
                bool mouseLeftClicked = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) || Input.GetMouseButtonDown(0);

                if (actionShootPressed || mouseLeftClicked)
                {
                    AdvanceStep();
                }
                break;

            case ActionType.Aim:
                bool actionAimPressed = aimAction != null && aimAction.WasPressedThisFrame();
                bool mouseRightClicked = (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) || Input.GetMouseButtonDown(1);

                if (actionAimPressed || mouseRightClicked)
                {
                    AdvanceStep();
                }
                break;

            case ActionType.DropWeapon:
                if (dropAction != null && dropAction.WasPressedThisFrame()) AdvanceStep();
                break;
        }
    }

    private void AdvanceStep()
    {
        currentStepIndex++;
        if (currentStepIndex < steps.Count)
        {
            ShowCurrentStep();
        }
        else
        {
            CompleteTutorial();
        }
    }

    private void CompleteTutorial()
    {
        isTutorialActive = false;

        PlayerPrefs.SetInt(TUTORIAL_KEY, 1);
        PlayerPrefs.Save();

        EnablePlayerControls(true);
        SetCursorState(false);

        DisableAllPanels();
    }

    private void DisableAllPanels()
    {
        if (welcomePanel != null) welcomePanel.SetActive(false);
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
    }

    private void SetCursorState(bool isUnlockedForUI)
    {
        if (isUnlockedForUI)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void EnablePlayerControls(bool state)
    {
        if (playerMovementScript != null) playerMovementScript.enabled = state;
        if (weaponScript != null) weaponScript.enabled = state;
    }

    [ContextMenu("Reset Tutorial Progress")]
    public void ResetTutorialProgress()
    {
        PlayerPrefs.DeleteKey(TUTORIAL_KEY);
        PlayerPrefs.Save();
        Debug.Log("Tutorial reiniciado.");
    }
}