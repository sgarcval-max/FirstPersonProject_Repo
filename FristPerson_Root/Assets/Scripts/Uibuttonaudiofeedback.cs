using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Añade esto a CUALQUIER control de la UI (botón, slider, toggle...) y sonará
/// automáticamente al pasar el ratón por encima o al seleccionarlo con teclado (hover),
/// sin tener que cablear nada manualmente en el Inspector.
///
/// Además, si el control es un Button o un Toggle, también suena el "click" al
/// pulsarlo/activarlo. Los Slider solo reciben el sonido de hover (no de click),
/// porque arrastrarlos dispara el evento de cambio constantemente y sonaría a spam.
/// </summary>
public class UIButtonAudioFeedback : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    [Tooltip("Desmarca esto en botones/toggles que ya reproducen su propio sonido especial al pulsarlos (ej: el botón de Reasignar tecla), para no duplicar el sonido de click.")]
    [SerializeField] private bool playClickSound = true;

    private Button button;
    private Toggle toggle;

    private void Awake()
    {
        // Ninguno de estos es obligatorio: un Slider no tendrá ni Button ni Toggle,
        // y solo recibirá el sonido de hover, que es lo que queremos para él.
        button = GetComponent<Button>();
        toggle = GetComponent<Toggle>();
    }

    private void OnEnable()
    {
        if (!playClickSound) return;

        if (button != null) button.onClick.AddListener(PlayClick);
        if (toggle != null) toggle.onValueChanged.AddListener(OnToggleChanged);
    }

    private void OnDisable()
    {
        if (button != null) button.onClick.RemoveListener(PlayClick);
        if (toggle != null) toggle.onValueChanged.RemoveListener(OnToggleChanged);
    }

    private void OnToggleChanged(bool _)
    {
        PlayClick();
    }

    private void PlayClick()
    {
        if (UIAudioManager.Instance != null)
        {
            UIAudioManager.Instance.PlayButtonClick();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlayHover();
    }

    /// <summary>Se llama cuando el control queda "seleccionado" navegando con teclado (W/S), no solo con ratón.</summary>
    public void OnSelect(BaseEventData eventData)
    {
        PlayHover();
    }

    private void PlayHover()
    {
        if (UIAudioManager.Instance != null)
        {
            UIAudioManager.Instance.PlayButtonHover();
        }
    }
}