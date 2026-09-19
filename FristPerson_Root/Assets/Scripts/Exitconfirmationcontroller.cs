using System;
using System.Collections;
using UnityEngine;

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

    [Header("Animación")]
    [SerializeField] private float animationDuration = 0.3f;

    [Header("Navegación por teclado")]
    [Tooltip("El botón que debe quedar seleccionado al cerrar este panel (ej: 'Salir del juego' o 'Reanudar' del menú de pausa).")]
    [SerializeField] private UnityEngine.UI.Selectable selectOnClose;

    /// <summary>True mientras este panel está abierto (creciendo, visible, o encogiéndose todavía).</summary>
    public bool IsOpen => panelRect != null && panelRect.gameObject.activeSelf;

    private Coroutine animationRoutine;

    private void Awake()
    {
        // Estado inicial: contenido oculto, panel pequeño, y el propio panel desactivado del todo
        if (panelContent != null) panelContent.SetActive(false);
        ApplyOffsets(startLeft, startTop, startRight, startBottom);
        if (panelRect != null) panelRect.gameObject.SetActive(false);
    }

    /// <summary>Llamar desde el botón "Salir del juego" del menú de pausa.</summary>
    public void OpenExitConfirmation()
    {
        if (panelContent != null) panelContent.SetActive(false);
        panelRect.gameObject.SetActive(true);
        ApplyOffsets(startLeft, startTop, startRight, startBottom);

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
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
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