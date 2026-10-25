using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// Gestiona la secuencia interactiva del tutorial soportando múltiples Action Maps, reasignaciones dinámicas,
/// formateo en español, efecto de mecanografiado con audio SFX aleatorio en títulos,
/// transiciones en fundido (Fade In/Out) y fases dinámicas para interacción e inventario.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    private const string TUTORIAL_KEY = "TutorialCompleted";

    public enum ActionType
    {
        Movement,
        JumpAndCrouch,
        Shoot,
        Aim,
        DropWeapon,
        InteractAndInventory
    }

    public enum PointStepState
    {
        SearchingObject,
        NearObject,
        ObjectPickedUp
    }

    [System.Serializable]
    public class TutorialStep
    {
        public string stepTitle;
        [TextArea(2, 4)]
        [Tooltip("Usa las etiquetas {MOVE}, {RUN}, {JUMP}, {CROUCH}, {SHOOT}, {AIM}, {DROP}, {INTERACT}, {INVENTORY}.")]
        public string stepDescription;
        public ActionType requiredAction;
    }

    [Header("1. Nombres de los Action Maps")]
    [SerializeField] private string movementMapName = "Movimiento";
    [SerializeField] private string combatMapName = "Combate";

    [Header("2. Nombres de Acciones en 'Movimiento'")]
    [SerializeField] private string moveActionName = "Move";
    [SerializeField] private string runActionName = "Run";
    [SerializeField] private string jumpActionName = "Jump";
    [SerializeField] private string crouchActionName = "Crouch";

    [Header("3. Nombres de Acciones en 'Combate / Interacción'")]
    [SerializeField] private string shootActionName = "Throw";
    [SerializeField] private string aimActionName = "Aim";
    [SerializeField] private string dropActionName = "Drop";
    [SerializeField] private string interactActionName = "Interact";
    [SerializeField] private string inventoryActionName = "Inventory";

    [Header("4. Control de Jugador y Armas")]
    [SerializeField] private MonoBehaviour playerMovementScript;
    [SerializeField] private MonoBehaviour weaponScript;

    [Header("5. UI de Bienvenida (Welcome Panel)")]
    [SerializeField] private GameObject welcomePanel;
    [SerializeField] private CanvasGroup welcomePanelCanvasGroup;
    [SerializeField] private TextMeshProUGUI welcomeTitleText;
    [SerializeField] private CanvasGroup welcomeBodyCanvasGroup;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button cancelButton;

    [Header("6. UI del Tutorial")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private CanvasGroup tutorialPanelCanvasGroup;
    [SerializeField] private TextMeshProUGUI stepCountText;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private CanvasGroup bodyCanvasGroup;

    [Header("7. Tiempos de Animación y Escritura")]
    [Tooltip("Duración general del fade out / fade in.")]
    [SerializeField] private float fadeDuration = 0.25f;
    [Tooltip("Tiempo mínimo entre letras al mecanografiar títulos.")]
    [SerializeField] private float minTypeSpeed = 0.02f;
    [Tooltip("Tiempo máximo entre letras al mecanografiar títulos.")]
    [SerializeField] private float maxTypeSpeed = 0.06f;

    [Header("8. Sonido de Mecanografiado (SFX)")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("Arrastra aquí tus 5 efectos de sonido de tecleo.")]
    [SerializeField] private AudioClip[] typingSounds;
    [Tooltip("Si está activado, no sonará ningún audio cuando el carácter sea un espacio en blanco.")]
    [SerializeField] private bool ignoreSpaceSound = true;

    [Header("9. Pasos del Tutorial")]
    [SerializeField] private List<TutorialStep> steps = new List<TutorialStep>();

    private InputAction moveAction;
    private InputAction runAction;
    private InputAction jumpAction;
    private InputAction crouchAction;
    private InputAction shootAction;
    private InputAction aimAction;
    private InputAction dropAction;
    private InputAction interactAction;
    private InputAction inventoryAction;

    private int currentStepIndex = 0;
    private bool isTutorialActive = false;
    private bool isTransitioning = false;

    // Banderas de estado
    private bool hasMoved = false;
    private bool hasRun = false;
    private bool hasJumped = false;
    private bool hasCrouched = false;

    // Estado del paso dinámico
    private PointStepState currentPointState = PointStepState.SearchingObject;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

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
        RebindActionUI.LoadSavedRebinds(asset);

        InputActionMap moveMap = asset.FindActionMap(movementMapName);
        if (moveMap != null)
        {
            moveAction = moveMap.FindAction(moveActionName);
            runAction = moveMap.FindAction(runActionName);
            jumpAction = moveMap.FindAction(jumpActionName);
            crouchAction = moveMap.FindAction(crouchActionName);
        }

        InputActionMap combatMap = asset.FindActionMap(combatMapName);
        if (combatMap != null)
        {
            shootAction = combatMap.FindAction(shootActionName);
            aimAction = combatMap.FindAction(aimActionName);
            dropAction = combatMap.FindAction(dropActionName);
            interactAction = combatMap.FindAction(interactActionName);
            inventoryAction = combatMap.FindAction(inventoryActionName);
        }
    }

    private void SetupWelcomeUI()
    {
        if (tutorialPanel != null) tutorialPanel.SetActive(false);

        EnablePlayerControls(false);
        SetCursorState(true);

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

        if (welcomePanel != null)
        {
            welcomePanel.SetActive(true);
            StartCoroutine(ShowWelcomeRoutine());
        }
    }

    /// <summary>
    /// Selecciona y reproduce un audio aleatorio de la lista de SFX al escribir una letra.
    /// </summary>
    private void PlayRandomTypeSound(char character)
    {
        if (audioSource == null || typingSounds == null || typingSounds.Length == 0) return;
        if (ignoreSpaceSound && character == ' ') return;

        int randomIndex = Random.Range(0, typingSounds.Length);
        AudioClip clip = typingSounds[randomIndex];

        if (clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private IEnumerator ShowWelcomeRoutine()
    {
        isTransitioning = true;

        if (welcomePanelCanvasGroup != null) welcomePanelCanvasGroup.alpha = 1f;
        if (welcomeBodyCanvasGroup != null) welcomeBodyCanvasGroup.alpha = 0f;

        if (welcomeTitleText != null)
        {
            string originalWelcomeTitle = welcomeTitleText.text;
            welcomeTitleText.text = "";

            foreach (char character in originalWelcomeTitle)
            {
                welcomeTitleText.text += character;
                PlayRandomTypeSound(character);

                float delay = Random.Range(minTypeSpeed, maxTypeSpeed);
                if (character == ' ' || character == ',' || character == '.')
                {
                    delay *= 1.8f;
                }

                yield return new WaitForSeconds(delay);
            }
        }

        if (welcomeBodyCanvasGroup != null)
        {
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                welcomeBodyCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
                yield return null;
            }
            welcomeBodyCanvasGroup.alpha = 1f;
        }

        isTransitioning = false;
    }

    public void OnAcceptTutorial()
    {
        if (isTransitioning) return;
        StartCoroutine(AcceptTutorialRoutine());
    }

    private IEnumerator AcceptTutorialRoutine()
    {
        isTransitioning = true;

        if (welcomePanelCanvasGroup != null)
        {
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                welcomePanelCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
                yield return null;
            }
            welcomePanelCanvasGroup.alpha = 0f;
        }

        if (welcomePanel != null) welcomePanel.SetActive(false);

        if (steps == null || steps.Count == 0)
        {
            CompleteTutorial();
            yield break;
        }

        isTutorialActive = true;
        currentStepIndex = 0;

        EnablePlayerControls(true);
        SetCursorState(false);

        if (tutorialPanelCanvasGroup != null) tutorialPanelCanvasGroup.alpha = 1f;
        if (tutorialPanel != null) tutorialPanel.SetActive(true);

        yield return StartCoroutine(TransitionToStepRoutine(0));
    }

    public void OnSkipTutorial()
    {
        if (isTransitioning) return;
        StartCoroutine(SkipTutorialRoutine());
    }

    private IEnumerator SkipTutorialRoutine()
    {
        isTransitioning = true;

        if (welcomePanelCanvasGroup != null)
        {
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                welcomePanelCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
                yield return null;
            }
            welcomePanelCanvasGroup.alpha = 0f;
        }

        CompleteTutorial();
    }

    private string FormatDescription(string rawText)
    {
        string formatted = rawText;

        formatted = formatted.Replace("{MOVE}", GetDisplayString(moveAction, "WASD"));
        formatted = formatted.Replace("{RUN}", GetDisplayString(runAction, "SHIFT"));
        formatted = formatted.Replace("{JUMP}", GetDisplayString(jumpAction, "ESPACIO"));
        formatted = formatted.Replace("{CROUCH}", GetDisplayString(crouchAction, "CTRL"));

        string shootText = GetDisplayString(shootAction, "Clic Izquierdo");
        if (string.IsNullOrEmpty(shootText) || shootText.Equals("E", System.StringComparison.OrdinalIgnoreCase))
        {
            shootText = "Clic Izquierdo / E";
        }
        formatted = formatted.Replace("{SHOOT}", shootText);

        string aimText = GetDisplayString(aimAction, "Clic Derecho");
        formatted = formatted.Replace("{AIM}", aimText);

        formatted = formatted.Replace("{DROP}", GetDisplayString(dropAction, "G"));
        formatted = formatted.Replace("{INTERACT}", GetDisplayString(interactAction, "E"));
        formatted = formatted.Replace("{INVENTORY}", GetDisplayString(inventoryAction, "I / TAB"));

        return formatted;
    }

    private string GetDisplayString(InputAction action, string fallback)
    {
        if (action == null) return fallback;

        string display = action.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontIncludeInteractions);

        if (string.IsNullOrEmpty(display)) return fallback;

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
                         .Replace("Space", "ESPACIO")
                         .Replace("Tab", "TAB");

        return display;
    }

    private void Update()
    {
        if (!isTutorialActive || isTransitioning || currentStepIndex >= steps.Count) return;

        CheckCurrentStepCompletion();
    }

    private void CheckCurrentStepCompletion()
    {
        TutorialStep current = steps[currentStepIndex];

        switch (current.requiredAction)
        {
            case ActionType.Movement:
                if (moveAction != null && moveAction.IsPressed()) hasMoved = true;
                if (runAction != null && runAction.IsPressed()) hasRun = true;
                if (hasMoved && hasRun) AdvanceStep();
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

            case ActionType.InteractAndInventory:
                bool isInteractPressed = (interactAction != null && interactAction.WasPressedThisFrame()) || Input.GetKeyDown(KeyCode.E);

                if (currentPointState == PointStepState.SearchingObject || currentPointState == PointStepState.NearObject)
                {
                    if (isInteractPressed)
                    {
                        OnObjectPickedUp();
                    }
                }
                else if (currentPointState == PointStepState.ObjectPickedUp)
                {
                    bool isInventoryPressed = (inventoryAction != null && inventoryAction.WasPressedThisFrame()) || Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.Tab);
                    if (isInventoryPressed)
                    {
                        AdvanceStep();
                    }
                }
                break;
        }
    }

    public void SetNearInteractable(bool isNear)
    {
        if (!isTutorialActive || currentStepIndex >= steps.Count) return;

        TutorialStep current = steps[currentStepIndex];
        if (current.requiredAction != ActionType.InteractAndInventory) return;

        if (isNear && currentPointState == PointStepState.SearchingObject)
        {
            currentPointState = PointStepState.NearObject;
            UpdateStepDescriptionText();
        }
        else if (!isNear && currentPointState == PointStepState.NearObject)
        {
            currentPointState = PointStepState.SearchingObject;
            UpdateStepDescriptionText();
        }
    }

    public void OnObjectPickedUp()
    {
        if (!isTutorialActive || currentStepIndex >= steps.Count) return;

        TutorialStep current = steps[currentStepIndex];
        if (current.requiredAction != ActionType.InteractAndInventory) return;

        if (currentPointState != PointStepState.ObjectPickedUp)
        {
            currentPointState = PointStepState.ObjectPickedUp;
            UpdateStepDescriptionText();
        }
    }

    private void UpdateStepDescriptionText()
    {
        if (descriptionText == null) return;

        switch (currentPointState)
        {
            case PointStepState.SearchingObject:
                descriptionText.text = FormatDescription("Ve a un objeto para equiparlo.");
                break;
            case PointStepState.NearObject:
                descriptionText.text = FormatDescription("Pulsa la tecla {INTERACT} para coger el objeto.");
                break;
            case PointStepState.ObjectPickedUp:
                descriptionText.text = FormatDescription("Pulsa {INVENTORY} para ver tu inventario.");
                break;
        }
    }

    private void AdvanceStep()
    {
        if (isTransitioning) return;

        // Evaluamos si el paso recién completado correspondía al uso del inventario
        bool isInventoryStep = false;
        if (currentStepIndex >= 0 && currentStepIndex < steps.Count)
        {
            isInventoryStep = steps[currentStepIndex].requiredAction == ActionType.InteractAndInventory;
        }

        currentStepIndex++;
        if (currentStepIndex < steps.Count)
        {
            StartCoroutine(TransitionToStepRoutine(currentStepIndex));
        }
        else
        {
            // Si el paso final abría el inventario, no forzamos el bloqueo del ratón
            CompleteTutorial(leaveCursorUnlocked: isInventoryStep);
        }
    }

    private IEnumerator TransitionToStepRoutine(int nextIndex)
    {
        isTransitioning = true;

        hasMoved = false;
        hasRun = false;
        hasJumped = false;
        hasCrouched = false;
        currentPointState = PointStepState.SearchingObject;

        if (bodyCanvasGroup != null && bodyCanvasGroup.alpha > 0f)
        {
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                bodyCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
                yield return null;
            }
            bodyCanvasGroup.alpha = 0f;
        }
        else if (bodyCanvasGroup != null)
        {
            bodyCanvasGroup.alpha = 0f;
        }

        TutorialStep current = steps[nextIndex];

        if (stepCountText != null) stepCountText.text = $"{nextIndex + 1}/{steps.Count}";

        if (current.requiredAction == ActionType.InteractAndInventory)
        {
            UpdateStepDescriptionText();
        }
        else if (descriptionText != null)
        {
            descriptionText.text = FormatDescription(current.stepDescription);
        }

        if (titleText != null)
        {
            titleText.text = "";
            string fullTitle = current.stepTitle;

            foreach (char character in fullTitle)
            {
                titleText.text += character;
                PlayRandomTypeSound(character);

                float delay = Random.Range(minTypeSpeed, maxTypeSpeed);
                if (character == ' ' || character == ',' || character == '.')
                {
                    delay *= 1.8f;
                }

                yield return new WaitForSeconds(delay);
            }
        }

        if (bodyCanvasGroup != null)
        {
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                bodyCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
                yield return null;
            }
            bodyCanvasGroup.alpha = 1f;
        }

        isTransitioning = false;
    }

    public void CompleteTutorial(bool leaveCursorUnlocked = false)
    {
        if (!isTutorialActive && welcomePanel != null && !welcomePanel.activeSelf)
        {
            FinishCompletion(leaveCursorUnlocked);
            return;
        }

        StartCoroutine(CompleteTutorialRoutine(leaveCursorUnlocked));
    }

    private IEnumerator CompleteTutorialRoutine(bool leaveCursorUnlocked = false)
    {
        isTransitioning = true;
        isTutorialActive = false;

        if (tutorialPanelCanvasGroup != null)
        {
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                tutorialPanelCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
                yield return null;
            }
            tutorialPanelCanvasGroup.alpha = 0f;
        }

        FinishCompletion(leaveCursorUnlocked);
    }

    private void FinishCompletion(bool leaveCursorUnlocked = false)
    {
        isTutorialActive = false;
        isTransitioning = false;

        PlayerPrefs.SetInt(TUTORIAL_KEY, 1);
        PlayerPrefs.Save();

        EnablePlayerControls(true);

        if (leaveCursorUnlocked)
        {
            SetCursorState(true);
        }
        else
        {
            SetCursorState(false);
        }

        DisableAllPanels();

        if (welcomePanelCanvasGroup != null) welcomePanelCanvasGroup.alpha = 1f;
        if (tutorialPanelCanvasGroup != null) tutorialPanelCanvasGroup.alpha = 1f;
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