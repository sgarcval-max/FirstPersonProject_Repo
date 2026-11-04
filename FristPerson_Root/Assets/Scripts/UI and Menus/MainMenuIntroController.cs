using System.Collections;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Controla la secuencia completa de introducción del menú principal:
/// 1. Solo la PRIMERA vez que se inicia el juego reproduce la animación (logo, texto parpadeante, rotaciones y deslizamientos).
/// 2. Si se vuelve al menú principal desde cualquier otra escena, se salta la intro y muestra el menú listo directamente.
/// </summary>
public class MainMenuIntroController : MonoBehaviour
{
    // Variable estática: persiste en memoria entre escenas durante toda la sesión del juego
    private static bool hasPlayedIntro = false;

    [Header("1. Entrada Inicial del Logo")]
    [SerializeField] private RectTransform logoRect;
    [Tooltip("Tiempo que tarda el logo en aparecer agrandándose al iniciar.")]
    [SerializeField] private float logoEntranceDuration = 0.8f;
    [Tooltip("Objeto o punto de referencia en el Canvas hacia donde se moverá y escalará el logo tras pulsar ESPACIO.")]
    [SerializeField] private RectTransform logoTargetTransform;
    [SerializeField] private float logoAnimDuration = 0.8f;

    [Header("1b. Imagen Trasera del Logo")]
    [Tooltip("Imagen casi transparente que se encuentra justo detrás del logo.")]
    [SerializeField] private GameObject logoBgImage;
    [Tooltip("Tiempo que tarda la imagen trasera en hacer Fade Out al pulsar ESPACIO.")]
    [SerializeField] private float logoBgFadeDuration = 0.6f;

    [Header("2. Texto 'Presiona ESPACIO'")]
    [SerializeField] private GameObject pressSpaceText;
    [Tooltip("Velocidad del parpadeo (Fade In / Fade Out).")]
    [SerializeField] private float textPulseSpeed = 3f;

    [Header("3. Configuración de la Imagen Especial")]
    [SerializeField] private RectTransform specialImageRect;
    [Tooltip("Duración de la rotación de -180 a -75 grados.")]
    [SerializeField] private float imageRotationDuration = 0.7f;
    [Tooltip("Duración del movimiento hacia las posiciones objetivo.")]
    [SerializeField] private float imageMoveDuration = 0.6f;

    [Header("4. Panel de Botones del Menú")]
    [SerializeField] private RectTransform menuButtonsContainer;
    [SerializeField] private float buttonsSlideDuration = 0.5f;
    [Tooltip("Distancia fuera de la pantalla desde la derecha desde la que aparecen los botones.")]
    [SerializeField] private float offscreenOffsetX = 1200f;

    private bool canPressSpace = false;
    private bool sequenceStarted = false;

    private Vector2 buttonsTargetPosition;
    private Vector3 initialLogoScale;
    private Vector2 initialLogoPosition;

    private CanvasGroup textCanvasGroup;
    private CanvasGroup logoCanvasGroup;
    private CanvasGroup logoBgCanvasGroup;
    private Coroutine textPulseCoroutine;

    private void Start()
    {
        // Guardar posiciones base necesarias
        if (menuButtonsContainer != null)
        {
            buttonsTargetPosition = menuButtonsContainer.anchoredPosition;
        }

        if (logoRect != null)
        {
            initialLogoPosition = logoRect.anchoredPosition;
            initialLogoScale = logoRect.localScale;
        }

        // SI YA SE HA REPRODUCIDO LA INTRO EN ESTA SESIÓN DE JUEGO:
        if (hasPlayedIntro)
        {
            SetFinalStateInstant();
            return;
        }

        // MARCAR QUE YA SE REPRODUJO PARA LAS PRÓXIMAS VECES
        hasPlayedIntro = true;

        // Configurar componentes para la primera animación
        if (pressSpaceText != null)
        {
            textCanvasGroup = pressSpaceText.GetComponent<CanvasGroup>();
            if (textCanvasGroup == null) textCanvasGroup = pressSpaceText.AddComponent<CanvasGroup>();

            textCanvasGroup.alpha = 0f;
            pressSpaceText.SetActive(false);
        }

        if (logoRect != null)
        {
            logoCanvasGroup = logoRect.GetComponent<CanvasGroup>();
            if (logoCanvasGroup == null) logoCanvasGroup = logoRect.AddComponent<CanvasGroup>();

            logoRect.localScale = Vector3.zero;
            logoCanvasGroup.alpha = 0f;
        }

        if (logoBgImage != null)
        {
            logoBgCanvasGroup = logoBgImage.GetComponent<CanvasGroup>();
            if (logoBgCanvasGroup == null) logoBgCanvasGroup = logoBgImage.AddComponent<CanvasGroup>();

            logoBgCanvasGroup.alpha = 0f;
        }

        if (menuButtonsContainer != null)
        {
            menuButtonsContainer.anchoredPosition = buttonsTargetPosition + new Vector2(offscreenOffsetX, 0f);
            menuButtonsContainer.gameObject.SetActive(false);
        }

        if (specialImageRect != null)
        {
            specialImageRect.localScale = Vector3.one * 2.2f;
            specialImageRect.localRotation = Quaternion.Euler(0f, 0f, -180f);

            float currentTop = -specialImageRect.offsetMax.y;
            float currentBottom = specialImageRect.offsetMin.y;
            SetLTRB(specialImageRect, 161f, currentTop, 161f, currentBottom);
        }

        // Iniciar la animación inicial
        StartCoroutine(PlayInitialEntrance());
    }

    /// <summary>
    /// Coloca todos los elementos directamente en el estado final sin reproducir animaciones.
    /// </summary>
    private void SetFinalStateInstant()
    {
        sequenceStarted = true;
        canPressSpace = false;

        // 1. Desactivar texto y fondo del logo
        if (pressSpaceText != null) pressSpaceText.SetActive(false);
        if (logoBgImage != null) logoBgImage.SetActive(false);

        // 2. Colocar Logo en su posición y tamaño final
        if (logoRect != null && logoTargetTransform != null)
        {
            logoCanvasGroup = logoRect.GetComponent<CanvasGroup>();
            if (logoCanvasGroup == null) logoCanvasGroup = logoRect.AddComponent<CanvasGroup>();

            logoCanvasGroup.alpha = 1f;
            logoRect.anchoredPosition = logoTargetTransform.anchoredPosition;
            logoRect.localScale = logoTargetTransform.localScale;
        }

        // 3. Colocar la Imagen Especial en su rotación (-75) y márgenes finales
        if (specialImageRect != null)
        {
            specialImageRect.localScale = Vector3.one * 2.2f;
            specialImageRect.localRotation = Quaternion.Euler(0f, 0f, -75f);
            SetLTRB(specialImageRect, 62f, 799f, -62f, -799f);
        }

        // 4. Mostrar botones en su posición final
        if (menuButtonsContainer != null)
        {
            menuButtonsContainer.gameObject.SetActive(true);
            menuButtonsContainer.anchoredPosition = buttonsTargetPosition;
        }
    }

    private IEnumerator PlayInitialEntrance()
    {
        float elapsed = 0f;
        while (elapsed < logoEntranceDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / logoEntranceDuration);

            if (logoRect != null)
            {
                logoRect.localScale = Vector3.Lerp(Vector3.zero, initialLogoScale, t);
                logoCanvasGroup.alpha = t;
            }

            if (logoBgCanvasGroup != null)
            {
                logoBgCanvasGroup.alpha = t;
            }

            yield return null;
        }

        if (logoRect != null)
        {
            logoRect.localScale = initialLogoScale;
            logoCanvasGroup.alpha = 1f;
        }

        if (logoBgCanvasGroup != null)
        {
            logoBgCanvasGroup.alpha = 1f;
        }

        if (pressSpaceText != null)
        {
            pressSpaceText.SetActive(true);
            textPulseCoroutine = StartCoroutine(AnimateTextPulsing());
        }

        canPressSpace = true;
    }

    private IEnumerator AnimateTextPulsing()
    {
        while (!sequenceStarted)
        {
            float alpha = (Mathf.Sin(Time.time * textPulseSpeed) + 1f) / 2f;
            if (textCanvasGroup != null)
            {
                textCanvasGroup.alpha = Mathf.Lerp(0.15f, 1f, alpha);
            }
            yield return null;
        }
    }

    private void Update()
    {
        if (!canPressSpace || sequenceStarted) return;

        bool spacePressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            spacePressed = true;
        }
#else
        if (Input.GetKeyDown(KeyCode.Space))
        {
            spacePressed = true;
        }
#endif

        if (spacePressed)
        {
            StartIntroSequence();
        }
    }

    public void StartIntroSequence()
    {
        sequenceStarted = true;

        if (textPulseCoroutine != null)
        {
            StopCoroutine(textPulseCoroutine);
        }

        if (pressSpaceText != null)
        {
            pressSpaceText.SetActive(false);
        }

        if (UIAudioManager.Instance != null)
        {
            UIAudioManager.Instance.PlayButtonClick();
        }

        StartCoroutine(ExecuteSequenceRoutine());
    }

    private IEnumerator ExecuteSequenceRoutine()
    {
        if (logoBgImage != null)
        {
            StartCoroutine(FadeOutLogoBg());
        }

        if (logoRect != null && logoTargetTransform != null)
        {
            StartCoroutine(AnimateLogoToTarget());
        }

        if (specialImageRect != null)
        {
            yield return StartCoroutine(AnimateImageRotationAndMovement());
        }

        if (menuButtonsContainer != null)
        {
            menuButtonsContainer.gameObject.SetActive(true);
            yield return StartCoroutine(AnimateButtonsSlide());
        }
    }

    private IEnumerator FadeOutLogoBg()
    {
        if (logoBgCanvasGroup == null) yield break;

        float elapsed = 0f;
        float startAlpha = logoBgCanvasGroup.alpha;

        while (elapsed < logoBgFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / logoBgFadeDuration);

            logoBgCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);

            yield return null;
        }

        logoBgCanvasGroup.alpha = 0f;
        logoBgImage.SetActive(false);
    }

    private IEnumerator AnimateLogoToTarget()
    {
        float elapsed = 0f;
        Vector2 targetPos = logoTargetTransform.anchoredPosition;
        Vector3 targetScale = logoTargetTransform.localScale;

        while (elapsed < logoAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / logoAnimDuration);

            logoRect.anchoredPosition = Vector2.Lerp(initialLogoPosition, targetPos, t);
            logoRect.localScale = Vector3.Lerp(initialLogoScale, targetScale, t);

            yield return null;
        }

        logoRect.anchoredPosition = targetPos;
        logoRect.localScale = targetScale;
    }

    private IEnumerator AnimateImageRotationAndMovement()
    {
        float elapsed = 0f;
        bool movementTriggered = false;
        Coroutine moveCoroutine = null;

        while (elapsed < imageRotationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / imageRotationDuration);

            float currentRotZ = Mathf.Lerp(-180f, -75f, t);
            specialImageRect.localRotation = Quaternion.Euler(0f, 0f, currentRotZ);

            if (!movementTriggered && currentRotZ >= -85f)
            {
                movementTriggered = true;
                moveCoroutine = StartCoroutine(AnimateImagePosition());
            }

            yield return null;
        }

        specialImageRect.localRotation = Quaternion.Euler(0f, 0f, -75f);

        if (moveCoroutine != null)
        {
            yield return moveCoroutine;
        }
    }

    private IEnumerator AnimateImagePosition()
    {
        float elapsed = 0f;

        float startLeft = specialImageRect.offsetMin.x;
        float startBottom = specialImageRect.offsetMin.y;
        float startRight = -specialImageRect.offsetMax.x;
        float startTop = -specialImageRect.offsetMax.y;

        float targetLeft = 62f;
        float targetTop = 799f;
        float targetRight = -62f;
        float targetBottom = -799f;

        while (elapsed < imageMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / imageMoveDuration);

            float curLeft = Mathf.Lerp(startLeft, targetLeft, t);
            float curTop = Mathf.Lerp(startTop, targetTop, t);
            float curRight = Mathf.Lerp(startRight, targetRight, t);
            float curBottom = Mathf.Lerp(startBottom, targetBottom, t);

            SetLTRB(specialImageRect, curLeft, curTop, curRight, curBottom);

            yield return null;
        }

        SetLTRB(specialImageRect, targetLeft, targetTop, targetRight, targetBottom);
    }

    private IEnumerator AnimateButtonsSlide()
    {
        float elapsed = 0f;
        Vector2 startPos = menuButtonsContainer.anchoredPosition;

        while (elapsed < buttonsSlideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / buttonsSlideDuration);

            menuButtonsContainer.anchoredPosition = Vector2.Lerp(startPos, buttonsTargetPosition, t);

            yield return null;
        }

        menuButtonsContainer.anchoredPosition = buttonsTargetPosition;
    }

    private void SetLTRB(RectTransform rt, float left, float top, float right, float bottom)
    {
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
    }
}
