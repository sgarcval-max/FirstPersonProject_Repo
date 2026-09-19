using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controla el panel de "¿Seguro que quieres salir?" que aparece POR ENCIMA del
/// menú de pausa (sin ocultarlo), animando su RectTransform (Left/Top/Right/Bottom)
/// desde un tamaño pequeño hasta el tamaño final, y activando su contenido justo
/// al terminar la animación. Al cancelar, hace el proceso inverso.
///
/// IMPORTANTE: el RectTransform de este panel debe tener sus anchors en modo
/// "stretch" completo (Anchor Min = 0,0 y Anchor Max = 1,1) para que existan los
/// campos Left/Top/Right/Bottom en el Inspector, que es lo que este script anima.
/// </summary>
public class ExitConfirmationController : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El RectTransform del panel que se va a animar (agrandar/encoger).")]
    [SerializeField] private RectTransform panelRect;
    [Tooltip("El GameObject hijo con los textos y botones (SALIR AL ESCRITORIO, Cancelar, Confirmar). Empieza desactivado.")]
    [SerializeField] private GameObject panelContent;

    [Header("Valores iniciales (panel pequeño / oculto)")]
    [SerializeField] private float startLeft = 399.45f;
    [SerializeField] private float startTop = 223.9f;
    [SerializeField] private float startRight = 399.45f;
    [SerializeField] private float startBottom = 223.9f;

    [Header("Valores finales (panel grande / visible)")]
    [SerializeField] private float targetLeft = 271.15f;
    [SerializeField] private float targetTop = 158.8999f;
    [SerializeField] private float targetRight = 265.3f;
    [SerializeField] private float targetBottom = 154.1f;

    [Header("Fondo oscurecido (detrás del panel)")]
    [Tooltip("Imagen negra a pantalla completa, hija de un objeto DISTINTO al que se anima (para que no crezca/encoja con el panel). Debe empezar con Alpha 0.")]
    [SerializeField] private Image dimBackground;
    [Tooltip("Opacidad máxima del fondo oscurecido, de 0 (invisible) a 1 (negro sólido). Ej: 0.6 = bastante notable pero sin tapar del todo.")]
    [Range(0f, 1f)]
    [SerializeField] private float dimTargetAlpha = 0.6f;
    [SerializeField] private float dimFadeDuration = 0.3f;

    [Header("Fundido a negro al confirmar salida")]
    [Tooltip("Imagen negra a pantalla completa, inicialmente transparente, colocada la ÚLTIMA en la Hierarchy (para quedar por encima de todo).")]
    [SerializeField] private Image fadeToBlackImage;
    [SerializeField] private float fadeToBlackDuration = 1f;

    [Header("Animación")]
    [SerializeField] private float animationDuration = 0.3f;

    [Header("Navegación por teclado")]
    [Tooltip("El botón que debe quedar seleccionado al cerrar este panel (ej: 'Salir del juego' o 'Reanudar' del menú de pausa).")]
    [SerializeField] private UnityEngine.UI.Selectable selectOnClose;

    /// <summary>True mientras este panel está abierto (creciendo, visible, o encogiéndose todavía).</summary>
    public bool IsOpen => panelRect != null && panelRect.gameObject.activeSelf;

    private Coroutine animationRoutine;
    private Coroutine dimFadeRoutine;

    private void Awake()
    {
        // Estado inicial: contenido oculto, panel pequeño, y el propio panel desactivado del todo
        if (panelContent != null) panelContent.SetActive(false);
        ApplyOffsets(startLeft, startTop, startRight, startBottom);
        if (panelRect != null) panelRect.gameObject.SetActive(false);

        if (dimBackground != null)
        {
            SetImageAlpha(dimBackground, 0f);
            dimBackground.gameObject.SetActive(false);
        }

        if (fadeToBlackImage != null)
        {
            SetImageAlpha(fadeToBlackImage, 0f);
            fadeToBlackImage.gameObject.SetActive(false);
        }
    }

    /// <summary>Llamar desde el botón "Salir del juego" del menú de pausa.</summary>
    public void OpenExitConfirmation()
    {
        if (panelContent != null) panelContent.SetActive(false);
        panelRect.gameObject.SetActive(true);
        ApplyOffsets(startLeft, startTop, startRight, startBottom);

        if (dimBackground != null)
        {
            dimBackground.gameObject.SetActive(true);
            StartDimFade(dimTargetAlpha, null);
        }

        if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayPanelOpen();

        StartAnimation(targetLeft, targetTop, targetRight, targetBottom, onComplete: () =>
        {
            if (panelContent != null) panelContent.SetActive(true);
        });
    }

    /// <summary>Llamar desde el botón "CANCELAR" dentro de este mismo panel (o desde ESC).</summary>
    public void OnCancelPressed()
    {
        if (panelContent != null) panelContent.SetActive(false);

        if (dimBackground != null)
        {
            StartDimFade(0f, () => { dimBackground.gameObject.SetActive(false); });
        }

        if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayPanelClose();

        StartAnimation(startLeft, startTop, startRight, startBottom, onComplete: () =>
        {
            panelRect.gameObject.SetActive(false);

            // Devolvemos la selección de teclado a un botón conocido del menú de pausa,
            // porque el que estaba seleccionado (dentro de este panel) ya no existe activo.
            if (selectOnClose != null && UnityEngine.EventSystems.EventSystem.current != null)
            {
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(selectOnClose.gameObject);
            }
        });
    }

    /// <summary>Llamar desde el botón "CONFIRMAR" dentro de este mismo panel.</summary>
    public void OnConfirmPressed()
    {
        StartCoroutine(FadeToBlackThenQuit());
    }

    private IEnumerator FadeToBlackThenQuit()
    {
        if (fadeToBlackImage != null)
        {
            fadeToBlackImage.gameObject.SetActive(true);
            SetImageAlpha(fadeToBlackImage, 0f);

            float elapsed = 0f;
            while (elapsed < fadeToBlackDuration)
            {
                // Unscaled porque el juego está en pausa (Time.timeScale = 0) en este momento
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeToBlackDuration);
                SetImageAlpha(fadeToBlackImage, t);
                yield return null;
            }

            SetImageAlpha(fadeToBlackImage, 1f);
        }

        QuitNow();
    }

    private void SetImageAlpha(Image image, float alpha)
    {
        Color c = image.color;
        c.a = alpha;
        image.color = c;
    }

    private void QuitNow()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void StartDimFade(float targetAlpha, Action onComplete)
    {
        if (dimFadeRoutine != null)
        {
            StopCoroutine(dimFadeRoutine);
        }
        dimFadeRoutine = StartCoroutine(DimFadeRoutine(targetAlpha, onComplete));
    }

    private IEnumerator DimFadeRoutine(float targetAlpha, Action onComplete)
    {
        float startAlpha = dimBackground.color.a;
        float elapsed = 0f;

        while (elapsed < dimFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / dimFadeDuration);
            SetImageAlpha(dimBackground, Mathf.Lerp(startAlpha, targetAlpha, t));
            yield return null;
        }

        SetImageAlpha(dimBackground, targetAlpha);
        onComplete?.Invoke();
        dimFadeRoutine = null;
    }

    private void StartAnimation(float left, float top, float right, float bottom, Action onComplete)
    {
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
        }
        animationRoutine = StartCoroutine(AnimateRoutine(left, top, right, bottom, onComplete));
    }

    private IEnumerator AnimateRoutine(float targetLeftVal, float targetTopVal, float targetRightVal, float targetBottomVal, Action onComplete)
    {
        // Leemos los valores actuales directamente del RectTransform, para que la animación
        // siempre parta de donde esté realmente el panel en ese momento.
        float currentLeft = panelRect.offsetMin.x;
        float currentBottom = panelRect.offsetMin.y;
        float currentRight = -panelRect.offsetMax.x;
        float currentTop = -panelRect.offsetMax.y;

        float elapsed = 0f;

        // Unscaled porque esto se usa con el juego en pausa (Time.timeScale = 0)
        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);

            float left = Mathf.Lerp(currentLeft, targetLeftVal, t);
            float top = Mathf.Lerp(currentTop, targetTopVal, t);
            float right = Mathf.Lerp(currentRight, targetRightVal, t);
            float bottom = Mathf.Lerp(currentBottom, targetBottomVal, t);

            ApplyOffsets(left, top, right, bottom);
            yield return null;
        }

        ApplyOffsets(targetLeftVal, targetTopVal, targetRightVal, targetBottomVal);
        onComplete?.Invoke();
        animationRoutine = null;
    }

    private void ApplyOffsets(float left, float top, float right, float bottom)
    {
        panelRect.offsetMin = new Vector2(left, bottom);
        panelRect.offsetMax = new Vector2(-right, -top);
    }
}