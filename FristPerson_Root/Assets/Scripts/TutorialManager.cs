using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.Events;

/// <summary>
/// Gestiona la secuencia interactiva del tutorial soportando múltiples Action Maps, reasignaciones dinámicas,
/// formateo en español, efecto de mecanografiado con audio SFX aleatorio en títulos,
/// transiciones en fundido (Fade In/Out), fases dinámicas para interacción/inventario,
/// detección automática de cercanía a objetos, 3 barras de progreso secuenciales estrictas,
/// y pantalla final de confirmación con protección contra enemigos.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    private const string TUTORIAL_KEY = "TutorialCompleted";

    /// <summary>
    /// Propiedad estática accesible desde cualquier script de Enemigos o Salud.
    /// Devuelve true mientras el tutorial o el panel de bienvenida estén activos.
    /// </summary>
    public static bool IsPlayerProtected
    {
        get
        {
            if (Instance == null) return false;
            return Instance.isTutorialActive || Instance.isWelcomeActive;
        }
    }

    public enum ActionType
    {
        Movement,
        JumpAndCrouch,
        Shoot,
        Aim,
        DropWeapon,
        InteractAndInventory,
        FinalCompletion // Nuevo paso final de confirmación
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

    [Header("7. UI Botón Final de Empezar a Jugar")]
    [Tooltip("Botón 'EMPEZAR A JUGAR' que aparece en el último paso para finalizar el tutorial.")]
    [SerializeField] private Button startPlayingButton;

    [Header("8. UI Barra de Progreso de Sub-Pasos (3 Barras)")]
    [Tooltip("Objeto contenedor donde están alojadas las 3 barras de progreso.")]
    [SerializeField] private GameObject subStepProgressContainer;
    [Tooltip("Arrastra aquí las 3 imágenes internas de relleno (Image Type = Filled).")]
    [SerializeField] private List<Image> subStepProgressFills = new List<Image>();
    [Tooltip("Velocidad de llenado animado de las barras.")]
    [SerializeField] private float progressFillSpeed = 8f;

    [Header("9. Detección Automática de Cercanía")]
    [Tooltip("Si está activado, el tutorial detectará automáticamente cuando el jugador se acerque a un objeto.")]
    [SerializeField] private bool useAutoProximityDetection = true;
    [Tooltip("Distancia en metros alrededor del jugador para detectar que está cerca de un objeto.")]
    [SerializeField] private float proximityRadius = 2.5f;
    [Tooltip("Capa de objetos que se pueden coger u objetos interactuables.")]
    [SerializeField] private LayerMask interactableLayer = ~0;

    [Header("10. Tiempos de Animación y Escritura")]
    [Tooltip("Duración general del fade out / fade in.")]
    [SerializeField] private float fadeDuration = 0.25f;
    [Tooltip("Tiempo mínimo entre letras al mecanografiar títulos.")]
    [SerializeField] private float minTypeSpeed = 0.02f;
    [Tooltip("Tiempo máximo entre letras al mecanografiar títulos.")]
    [SerializeField] private float maxTypeSpeed = 0.06f;

    [Header("11. Sonido de Mecanografiado (SFX)")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("Arrastra aquí tus efectos de sonido de tecleo.")]
    [SerializeField] private AudioClip[] typingSounds;
    [Tooltip("Si está activado, no sonará ningún audio cuando el carácter sea un espacio en blanco.")]
    [SerializeField] private bool ignoreSpaceSound = true;

    [Header("12. Eventos de Protección de Enemigos")]
    [Tooltip("Evento disparado cuando el tutorial finaliza para avisar a los enemigos.")]
    public UnityEvent OnTutorialFinished;

    [Header("13. Pasos del Tutorial")]
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
    private bool isWelcomeActive = false;
    private bool isTutorialActive = false;
    private bool isTransitioning = false;
    private float inputCooldown = 0f;

    // Fases del sub-paso: 0 = Vacío (0/3), 1 = Cerca (1/3), 2 = Cogido (2/3), 3 = Inventario (3/3)
    private int currentSubStepPhase = 0;
    private float[] targetFills = new float[3];

    private bool hasMoved = false;
    private bool hasRun = false;
    private bool hasJumped = false;
    private bool hasCrouched = false;

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
        if (startPlayingButton != null)
        {
            startPlayingButton.onClick.RemoveAllListeners();
            startPlayingButton.onClick.AddListener(OnStartPlayingClicked);
            startPlayingButton.gameObject.SetActive(false);
        }

        if (PlayerPrefs.GetInt(TUTORIAL_KEY, 0) == 1)
        {
            isWelcomeActive = false;
            isTutorialActive = false;
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
        isWelcomeActive = true;
        isTutorialActive = false;

        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        if (subStepProgressContainer != null) subStepProgressContainer.SetActive(false);

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

        isWelcomeActive = false;
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
        UpdateSubStepProgressUI();

        if (inputCooldown > 0f) inputCooldown -= Time.deltaTime;

        if (!isTutorialActive || isTransitioning || currentStepIndex >= steps.Count) return;

        CheckCurrentStepCompletion();
    }

    private void UpdateSubStepProgressUI()
    {
        if (subStepProgressFills == null || subStepProgressFills.Count == 0) return;

        for (int i = 0; i < subStepProgressFills.Count; i++)
        {
            if (subStepProgressFills[i] != null)
            {
                float target = (i < targetFills.Length) ? targetFills[i] : 0f;
                subStepProgressFills[i].fillAmount = Mathf.MoveTowards(
                    subStepProgressFills[i].fillAmount,
                    target,
                    Time.deltaTime * progressFillSpeed
                );
            }
        }
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

                if (actionShootPressed || mouseLeftClicked) AdvanceStep();
                break;

            case ActionType.Aim:
                bool actionAimPressed = aimAction != null && aimAction.WasPressedThisFrame();
                bool mouseRightClicked = (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) || Input.GetMouseButtonDown(1);

                if (actionAimPressed || mouseRightClicked) AdvanceStep();
                break;

            case ActionType.DropWeapon:
                if (dropAction != null && dropAction.WasPressedThisFrame()) AdvanceStep();
                break;

            case ActionType.InteractAndInventory:
                CheckProximityAutomatic();

                if (inputCooldown > 0f) return;

                bool isInteractPressed = (interactAction != null && interactAction.WasPressedThisFrame()) || Input.GetKeyDown(KeyCode.E);

                if (currentSubStepPhase == 1)
                {
                    if (isInteractPressed)
                    {
                        currentSubStepPhase = 2;
                        UpdateSubStepStateUI();
                        inputCooldown = 0.25f;
                    }
                }
                else if (currentSubStepPhase == 2)
                {
                    bool isInventoryPressed = (inventoryAction != null && inventoryAction.WasPressedThisFrame()) || Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.Tab);
                    if (isInventoryPressed)
                    {
                        StartCoroutine(FinishInventoryStepRoutine());
                    }
                }
                break;

            case ActionType.FinalCompletion:
                // El paso final aguarda la pulsación del botón "EMPEZAR A JUGAR"
                break;
        }
    }

    private void CheckProximityAutomatic()
    {
        if (!useAutoProximityDetection || currentSubStepPhase >= 2 || playerMovementScript == null) return;

        Collider[] hits = Physics.OverlapSphere(playerMovementScript.transform.position, proximityRadius, interactableLayer);
        bool isNearObject = false;

        foreach (var hit in hits)
        {
            if (hit.gameObject != playerMovementScript.gameObject && !hit.transform.IsChildOf(playerMovementScript.transform))
            {
                isNearObject = true;
                break;
            }
        }

        SetNearInteractable(isNearObject);
    }

    public void SetNearInteractable(bool isNear)
    {
        if (!isTutorialActive || currentStepIndex >= steps.Count) return;

        TutorialStep current = steps[currentStepIndex];
        if (current.requiredAction != ActionType.InteractAndInventory) return;

        if (currentSubStepPhase < 2)
        {
            if (isNear && currentSubStepPhase == 0)
            {
                currentSubStepPhase = 1;
                UpdateSubStepStateUI();
            }
            else if (!isNear && currentSubStepPhase == 1)
            {
                currentSubStepPhase = 0;
                UpdateSubStepStateUI();
            }
        }
    }

    public void OnObjectPickedUp()
    {
        if (!isTutorialActive || currentStepIndex >= steps.Count) return;

        TutorialStep current = steps[currentStepIndex];
        if (current.requiredAction != ActionType.InteractAndInventory) return;

        if (currentSubStepPhase < 2)
        {
            currentSubStepPhase = 2;
            UpdateSubStepStateUI();
        }
    }

    private void UpdateSubStepStateUI()
    {
        if (descriptionText == null) return;

        switch (currentSubStepPhase)
        {
            case 0:
                descriptionText.text = FormatDescription("Localiza y acércate a un objeto.");
                SetTargetFills(0f, 0f, 0f);
                break;

            case 1:
                descriptionText.text = FormatDescription("Pulsa la tecla {INTERACT} para coger el objeto.");
                SetTargetFills(1f, 0f, 0f);
                break;

            case 2:
                descriptionText.text = FormatDescription("Pulsa {INVENTORY} para ver tu inventario.");
                SetTargetFills(1f, 1f, 0f);
                break;

            case 3:
                SetTargetFills(1f, 1f, 1f);
                break;
        }
    }

    private IEnumerator FinishInventoryStepRoutine()
    {
        isTransitioning = true;
        currentSubStepPhase = 3;
        UpdateSubStepStateUI();

        yield return new WaitForSeconds(0.35f);

        AdvanceStep();
    }

    private void SetTargetFills(float fill1, float fill2, float fill3)
    {
        if (targetFills.Length >= 3)
        {
            targetFills[0] = fill1;
            targetFills[1] = fill2;
            targetFills[2] = fill3;
        }
    }

    private void ResetSubStepFills()
    {
        currentSubStepPhase = 0;
        SetTargetFills(0f, 0f, 0f);

        if (subStepProgressFills != null)
        {
            foreach (var img in subStepProgressFills)
            {
                if (img != null)
                {
                    img.fillAmount = 0f;
                }
            }
        }
    }

    private void AdvanceStep()
    {
        if (isTransitioning && currentSubStepPhase != 3) return;

        currentStepIndex++;
        if (currentStepIndex < steps.Count)
        {
            StartCoroutine(TransitionToStepRoutine(currentStepIndex));
        }
        else
        {
            CompleteTutorial();
        }
    }

    private IEnumerator TransitionToStepRoutine(int nextIndex)
    {
        isTransitioning = true;
        inputCooldown = 0.3f;

        hasMoved = false;
        hasRun = false;
        hasJumped = false;
        hasCrouched = false;

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

        TutorialStep current = steps[nextIndex];

        bool isFinalStep = (current.requiredAction == ActionType.FinalCompletion);

        // Si es el paso final, activamos el botón "EMPEZAR A JUGAR" y liberamos el cursor
        if (startPlayingButton != null)
        {
            startPlayingButton.gameObject.SetActive(isFinalStep);
        }

        if (isFinalStep)
        {
            SetCursorState(true);
        }

        bool hasSubSteps = (current.requiredAction == ActionType.InteractAndInventory);
        if (subStepProgressContainer != null)
        {
            subStepProgressContainer.SetActive(hasSubSteps);
            if (hasSubSteps)
            {
                ResetSubStepFills();
            }
        }

        if (stepCountText != null) stepCountText.text = $"{nextIndex + 1}/{steps.Count}";

        if (current.requiredAction == ActionType.InteractAndInventory)
        {
            UpdateSubStepStateUI();
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

    public void OnStartPlayingClicked()
    {
        if (isTransitioning) return;
        CompleteTutorial();
    }

    public void CompleteTutorial(bool leaveCursorUnlocked = false)
    {
        if (!isTutorialActive && !isWelcomeActive && welcomePanel != null && !welcomePanel.activeSelf)
        {
            FinishCompletion(leaveCursorUnlocked);
            return;
        }

        StartCoroutine(CompleteTutorialRoutine(leaveCursorUnlocked));
    }

    private IEnumerator CompleteTutorialRoutine(bool leaveCursorUnlocked = false)
    {
        isTransitioning = true;

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
        isWelcomeActive = false;
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

        // Disparar evento para restaurar detección y ataques de enemigos
        OnTutorialFinished?.Invoke();
    }

    private void DisableAllPanels()
    {
        if (welcomePanel != null) welcomePanel.SetActive(false);
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
        if (subStepProgressContainer != null) subStepProgressContainer.SetActive(false);
        if (startPlayingButton != null) startPlayingButton.gameObject.SetActive(false);
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