using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Añade esto a CUALQUIER control de Ajustes (botón, slider, toggle). En vez de escribir
/// el texto directamente, escribe una CLAVE (key) que exista en LocalizationManager,
/// igual que haces con LocalizedText. Así el tooltip también cambia de idioma solo.
/// </summary>
public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Tooltip("Debe coincidir EXACTAMENTE con la clave (key) de este texto en LocalizationManager.")]
    [SerializeField] private string key;

    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.Instance?.Hide();
    }

    public void OnSelect(BaseEventData eventData)
    {
        ShowTooltip();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        TooltipManager.Instance?.Hide();
    }

    private void ShowTooltip()
    {
        if (LocalizationManager.Instance == null || string.IsNullOrEmpty(key)) return;

        string text = LocalizationManager.Instance.GetText(key);
        TooltipManager.Instance?.Show(text);
    }
}