using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Añade esto a CUALQUIER control de Ajustes (botón, slider, toggle) y escribe su
/// descripción en el campo "Description". Al pasar el ratón por encima, o al
/// seleccionarlo navegando con teclado, se mostrará ese texto a través de
/// TooltipManager. Al dejar de estar encima/seleccionado, el texto desaparece.
/// </summary>
public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [Tooltip("El texto que explica para qué sirve este control. Escribe uno distinto en cada elemento.")]
    [TextArea(2, 4)]
    [SerializeField] private string description;

    public void OnPointerEnter(PointerEventData eventData)
    {
        TooltipManager.Instance?.Show(description);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.Instance?.Hide();
    }

    public void OnSelect(BaseEventData eventData)
    {
        TooltipManager.Instance?.Show(description);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        TooltipManager.Instance?.Hide();
    }
}