using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Selecciona automáticamente un botón concreto cuando este panel se activa,
/// para que la navegación por teclado (W/S/Intro) funcione desde el primer momento
/// sin necesidad de hacer click con el ratón antes.
/// Añade este script a CADA panel que tenga botones (PausePanel, SettingsMainPage,
/// SoundSection, MouseKeyboardSection, KeybindingSection...).
/// </summary>
public class AutoSelectFirstButton : MonoBehaviour
{
    [Tooltip("El botón que se seleccionará automáticamente al abrir este panel.")]
    [SerializeField] private Selectable firstSelectable;

    private void OnEnable()
    {
        // Se espera un frame porque, si el panel se acaba de activar, el EventSystem
        // a veces todavía no tiene la jerarquía lista para recibir la selección.
        StartCoroutine(SelectNextFrame());
    }

    private System.Collections.IEnumerator SelectNextFrame()
    {
        yield return null;

        if (EventSystem.current != null && firstSelectable != null)
        {
            EventSystem.current.SetSelectedGameObject(null); // limpia selección previa
            EventSystem.current.SetSelectedGameObject(firstSelectable.gameObject);
        }
    }
}
