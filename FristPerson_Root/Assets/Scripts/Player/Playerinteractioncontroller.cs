using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Conecta las teclas del jugador con el inventario de armas y con los objetos del mundo:
/// NextItem (cambiar lo que llevas en la mano), Drop (soltar el arma equipada) e
/// Interact (recoger lo que estás mirando). Muestra el aviso en pantalla
/// ("[F] Recoger Pistola") y hace brillar el objeto que tienes delante.
/// </summary>
[RequireComponent(typeof(FirstPersonController))]
[RequireComponent(typeof(PlayerWeaponInventory))]
public class PlayerInteractionController : MonoBehaviour
{
    [Header("Interacción")]
    [Tooltip("La cámara del jugador. Si se deja vacío, usa la cámara principal.")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float interactDistance = 3f;
    [Tooltip("Texto de la pantalla donde se muestra el aviso (ej: [F] Recoger Pistola).")]
    [SerializeField] private TMP_Text promptText;

    private FirstPersonController playerController;
    private PlayerWeaponInventory inventory;
    private IInteractable currentTarget;

    private PlayerControls controls
    {
        get { return InputManager.Controls; }
    }

    private void Awake()
    {
        playerController = GetComponent<FirstPersonController>();
        inventory = GetComponent<PlayerWeaponInventory>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void OnEnable()
    {
        controls.Combate.Enable();
        controls.Combate.NextItem.performed += HandleNextItem;
        controls.Combate.Drop.performed += HandleDrop;
        controls.Combate.Interact.performed += HandleInteract;
        LocalizationManager.OnLanguageChanged += RefreshPrompt;
    }

    private void OnDisable()
    {
        controls.Combate.NextItem.performed -= HandleNextItem;
        controls.Combate.Drop.performed -= HandleDrop;
        controls.Combate.Interact.performed -= HandleInteract;
        LocalizationManager.OnLanguageChanged -= RefreshPrompt;
        controls.Combate.Disable();
    }

    private void Update()
    {
        if (!IsGameplayActive())
        {
            SetTarget(null);
            return;
        }

        IInteractable found = null;

        if (cameraTransform != null &&
            Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit,
                            interactDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            found = hit.collider.GetComponentInParent<IInteractable>();
        }

        SetTarget(found);
    }

    private bool IsGameplayActive()
    {
        // En pausa, FirstPersonController está desactivado: ignoramos todo el input de gameplay.
        return playerController == null || playerController.enabled;
    }

    private void SetTarget(IInteractable newTarget)
    {
        if (newTarget == currentTarget) return;

        SetHighlight(currentTarget, false);
        currentTarget = newTarget;
        SetHighlight(currentTarget, true);

        RefreshPrompt();
    }

    /// <summary>Enciende o apaga el resplandor del objeto. Si no tiene el componente, se lo añade.</summary>
    private void SetHighlight(IInteractable target, bool on)
    {
        Component component = target as Component;
        if (component == null) return;

        InteractableHighlight highlight = component.GetComponent<InteractableHighlight>();
        if (highlight == null)
        {
            if (!on) return;
            highlight = component.gameObject.AddComponent<InteractableHighlight>();
        }

        highlight.SetHighlighted(on);
    }

    private void RefreshPrompt()
    {
        if (promptText == null) return;

        if (currentTarget == null)
        {
            promptText.gameObject.SetActive(false);
            return;
        }

        string keyName = controls.Combate.Interact.GetBindingDisplayString();
        promptText.text = "[" + keyName + "] " + currentTarget.GetPrompt();
        promptText.gameObject.SetActive(true);
    }

    private void HandleNextItem(InputAction.CallbackContext context)
    {
        if (!IsGameplayActive()) return;
        inventory.CycleEquipped();
    }

    private void HandleDrop(InputAction.CallbackContext context)
    {
        if (!IsGameplayActive()) return;
        inventory.DropEquipped();
    }

    private void HandleInteract(InputAction.CallbackContext context)
    {
        if (!IsGameplayActive() || currentTarget == null) return;

        IInteractable target = currentTarget;
        SetHighlight(target, false);

        // Limpiamos el objetivo antes de interactuar: el objeto puede destruirse al recogerlo.
        currentTarget = null;
        RefreshPrompt();

        target.Interact(gameObject);
    }
}
