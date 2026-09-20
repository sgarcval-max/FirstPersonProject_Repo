using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestiona la navegación entre las distintas "páginas" del menú de Ajustes
/// (Sonido, Ratón y Teclado, Asignación de teclas, etc.), mostrando solo una a la vez
/// y recordando el historial para poder volver atrás correctamente con un botón "Volver".
/// Este script debe ir en el mismo GameObject que el SettingsPanel (el panel raíz de Ajustes).
/// </summary>
public class SettingsNavigationController : MonoBehaviour
{
    [Header("Todas las páginas de Ajustes (incluida la página principal)")]
    [Tooltip("Arrastra aquí TODOS los paneles de página: SettingsMainPage, SoundSection, MouseKeyboardSection, KeybindingSection, etc.")]
    [SerializeField] private List<GameObject> allPages;

    [Header("Página principal de Ajustes")]
    [Tooltip("La página que se muestra nada más entrar en Ajustes (con los botones Sonido / Ratón y Teclado).")]
    [SerializeField] private GameObject homePage;

    private readonly Stack<GameObject> history = new Stack<GameObject>();
    private GameObject currentPage;

    private void OnEnable()
    {
        // Cada vez que se abre el panel de Ajustes (se activa este GameObject),
        // reseteamos la navegación para empezar siempre desde la página principal.
        history.Clear();
        ShowPageInternal(homePage);
    }

    /// <summary>Navega a una página nueva, guardando la actual en el historial. Úsalo en los botones (Sonido, Ratón y Teclado, Asignar teclas...).</summary>
    public void NavigateTo(GameObject page)
    {
        if (currentPage != null)
        {
            history.Push(currentPage);
        }
        ShowPageInternal(page);

        if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayPanelOpen();
    }

    /// <summary>Vuelve a la página anterior. Si no hay historial (estamos en la página principal), cierra Ajustes: usa PauseManager si existe (escena de juego) o MainMenuController si no (Menú Principal).</summary>
    public void GoBack()
    {
        if (history.Count > 0)
        {
            GameObject previous = history.Pop();
            ShowPageInternal(previous);
            if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayPanelClose();
        }
        else if (PauseManager.Instance != null)
        {
            PauseManager.Instance.CloseSettings();
        }
        else
        {
            MainMenuController mainMenu = FindObjectOfType<MainMenuController>();
            if (mainMenu != null)
            {
                mainMenu.CloseSettings();
            }
        }
    }

    private void ShowPageInternal(GameObject pageToShow)
    {
        foreach (GameObject page in allPages)
        {
            if (page != null)
            {
                page.SetActive(page == pageToShow);
            }
        }
        currentPage = pageToShow;
    }
}