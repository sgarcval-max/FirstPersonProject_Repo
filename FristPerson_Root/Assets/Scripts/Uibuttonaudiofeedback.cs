using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Añade esto a CUALQUIER botón de la UI y sonará automáticamente al pasar el ratón
/// por encima (hover) y al pulsarlo (click), sin tener que cablear nada manualmente
/// en el OnClick() del Inspector.
/// </summary>
[RequireComponent(typeof(Button))]
public class UIButtonAudioFeedback : MonoBehaviour, IPointerEnterHandler
{
    [Tooltip("Desmarca esto en botones que ya reproducen su propio sonido especial al pulsarlos (ej: el botón de Reasignar tecla), para no duplicar el sonido de click.")]
    [SerializeField] private bool playClickSound = true;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (playClickSound)
        {
            button.onClick.AddListener(PlayClick);
        }
    }

    private void OnDisable()
    {
        button.onClick.RemoveListener(PlayClick);
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
        if (UIAudioManager.Instance != null)
        {
            UIAudioManager.Instance.PlayButtonHover();
        }
    }
}
