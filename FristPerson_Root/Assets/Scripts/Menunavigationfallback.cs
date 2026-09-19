using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Soluciona un comportamiento típico de Unity: si haces click con el ratón fuera de
/// cualquier botón, el EventSystem deselecciona todo, y entonces la navegación por
/// teclado (W/S/flechas) deja de funcionar porque no hay ningún botón "activo" desde
/// el que moverse. Este script recuerda el último botón seleccionado y lo restaura en
/// cuanto detecta que se pulsa una tecla de navegación sin nada seleccionado.
/// Debe ir en el mismo GameObject que el EventSystem (uno solo en toda la escena).
/// </summary>
public class MenuNavigationFallback : MonoBehaviour
{
    private GameObject lastSelected;
    private Vector3 lastMousePosition;

    private void Start()
    {
        lastMousePosition = Input.mousePosition;
    }

    private void Update()
    {
        EventSystem es = EventSystem.current;
        if (es == null) return;

        // Si el ratón se ha movido, quitamos la selección de teclado para que no
        // queden dos botones "resaltados" a la vez (el seleccionado por teclado y
        // el que tiene el ratón encima).
        bool mouseMoved = (Input.mousePosition - lastMousePosition).sqrMagnitude > 0.01f;
        lastMousePosition = Input.mousePosition;

        if (mouseMoved && es.currentSelectedGameObject != null)
        {
            lastSelected = es.currentSelectedGameObject;
            es.SetSelectedGameObject(null);
            return;
        }

        if (es.currentSelectedGameObject != null)
        {
            // Hay algo seleccionado ahora mismo: lo recordamos por si se pierde luego
            lastSelected = es.currentSelectedGameObject;
            return;
        }

        bool navigationKeyPressed =
            Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.S) ||
            Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D) ||
            Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow) ||
            Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow);

        // No hay nada seleccionado, pero el jugador quiere navegar: recuperamos el último botón válido
        if (navigationKeyPressed && lastSelected != null && lastSelected.activeInHierarchy)
        {
            es.SetSelectedGameObject(lastSelected);
        }
    }
}