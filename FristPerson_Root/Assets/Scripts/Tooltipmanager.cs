using UnityEngine;
using TMPro;

/// <summary>
/// Gestor central del texto descriptivo (tooltip) de Ajustes.
/// Cualquier control (botón, slider, toggle) con un TooltipTrigger le pide a este
/// gestor que muestre u oculte su descripción cuando el ratón pasa por encima o
/// cuando se selecciona con teclado.
/// </summary>
public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance { get; private set; }

    [Header("Referencias UI")]
    [Tooltip("El contenedor (panel/GameObject) que se activa/desactiva para mostrar u ocultar el texto.")]
    [SerializeField] private GameObject tooltipContainer;
    [SerializeField] private TMP_Text tooltipText;

    private void Awake()
    {
        Instance = this;
        Hide();
    }

    public void Show(string description)
    {
        if (string.IsNullOrEmpty(description)) return;

        if (tooltipText != null) tooltipText.text = description;
        if (tooltipContainer != null) tooltipContainer.SetActive(true);
    }

    public void Hide()
    {
        if (tooltipContainer != null) tooltipContainer.SetActive(false);
    }
}
