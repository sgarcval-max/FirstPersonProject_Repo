using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Gestiona el tutorial interactivo controlando el estado del cursor y los scripts del jugador
/// para permitir interactuar con la UI de bienvenida y disparar/apuntar normalmente durante los pasos.
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
        public string stepDescription;
        public ActionType requiredAction;
    }

    [Header("1. Control de Jugador y Armas")]
    [Tooltip("Asigna aquí el script de movimiento del jugador para desactivarlo mientras la bienvenida está en pantalla.")]
    [SerializeField] private MonoBehaviour playerMovementScript;
    [Tooltip("Asigna aquí el script de disparo/arma para evitar que dispare al hacer clic en 'Aceptar'.")]
    [SerializeField] private MonoBehaviour weaponScript;

    [Header("2. Panel de Bienvenida")]
    [SerializeField] private GameObject welcomePanel;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button cancelButton;

    [Header("3. Panel del Tutorial")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TextMeshProUGUI stepCountText;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Header("4. Lista de Pasos del Tutorial")]
    [SerializeField] private List<TutorialStep> steps = new List<TutorialStep>();

    [Header("5. Teclas de Control")]
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode crouchKey = KeyCode.C;
    [SerializeField] private KeyCode dropKey = KeyCode.G;

    private int currentStepIndex = 0;
    private bool isTutorialActive = false;

    private bool hasJumped = false;
    private bool hasCrouched = false;

    private void Start()
    {
        // Si ya completó el tutorial previamente
        if (PlayerPrefs.GetInt(TUTORIAL_KEY, 0) == 1)
        {
            EnablePlayerControls(true);
            SetCursorState(false); // Bloquear cursor para jugar normalmente
            DisableAllPanels();
            return;
        }

        SetupWelcomeUI();
    }

    private void SetupWelcomeUI()
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(false);

        // Desactivar controles y liberar cursor para poder pulsar los botones de la UI
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

        // Activar controles del jugador y bloquear cursor para poder moverse/disparar en el juego
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
        if (descriptionText != null) descriptionText.text = current.stepDescription;
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
                if (CheckMovementInput()) AdvanceStep();
                break;

            case ActionType.JumpAndCrouch:
                CheckJumpAndCrouchInput();
                if (hasJumped && hasCrouched) AdvanceStep();
                break;

            case ActionType.Shoot:
                if (CheckShootInput()) AdvanceStep();
                break;

            case ActionType.Aim:
                if (CheckAimInput()) AdvanceStep();
                break;

            case ActionType.DropWeapon:
                if (CheckDropInput()) AdvanceStep();
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

        // Asegurar que al terminar el jugador tenga el control total y el cursor bloqueado
        EnablePlayerControls(true);
        SetCursorState(false);

        DisableAllPanels();
    }

    private void DisableAllPanels()
    {
        if (welcomePanel != null) welcomePanel.SetActive(false);
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
    }

    /// <summary>
    /// Cambia el estado del ratón entre libre (UI) y bloqueado (Juego FPS).
    /// </summary>
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

    /// <summary>
    /// Activa o desactiva los scripts de movimiento y armas del jugador.
    /// </summary>
    private void EnablePlayerControls(bool state)
    {
        if (playerMovementScript != null) playerMovementScript.enabled = state;
        if (weaponScript != null) weaponScript.enabled = state;
    }

    // --- DETECCIÓN DE INPUTS ---

    private bool CheckMovementInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            return Keyboard.current.wKey.isPressed || Keyboard.current.aKey.isPressed ||
                   Keyboard.current.sKey.isPressed || Keyboard.current.dKey.isPressed;
        }
#endif
        return Input.GetAxisRaw("Horizontal") != 0 || Input.GetAxisRaw("Vertical") != 0;
    }

    private void CheckJumpAndCrouchInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame) hasJumped = true;
            if (Keyboard.current.cKey.wasPressedThisFrame) hasCrouched = true;
            return;
        }
#endif
        if (Input.GetKeyDown(jumpKey)) hasJumped = true;
        if (Input.GetKeyDown(crouchKey)) hasCrouched = true;
    }

    private bool CheckShootInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
#endif
        return Input.GetMouseButtonDown(0);
    }

    private bool CheckAimInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) return true;
#endif
        return Input.GetMouseButtonDown(1);
    }

    private bool CheckDropInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame) return true;
#endif
        return Input.GetKeyDown(dropKey);
    }

    [ContextMenu("Reset Tutorial Progress")]
    public void ResetTutorialProgress()
    {
        PlayerPrefs.DeleteKey(TUTORIAL_KEY);
        PlayerPrefs.Save();
        Debug.Log("Tutorial reiniciado.");
    }
}