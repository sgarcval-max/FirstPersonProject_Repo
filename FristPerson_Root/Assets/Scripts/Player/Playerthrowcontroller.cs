using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Gestiona el lanzamiento de objetos del jugador. Escucha "Throw" (lanzar el objeto
/// actual del inventario) y "NextItem" (cambiar de objeto) del Action Map "Combate".
/// </summary>
[RequireComponent(typeof(FirstPersonController))]
[RequireComponent(typeof(PlayerInventory))]
public class PlayerThrowController : MonoBehaviour
{
    [Header("Lanzamiento")]
    [Tooltip("Punto desde el que sale el objeto lanzado (normalmente un hijo vacío delante de la cámara).")]
    [SerializeField] private Transform throwPoint;

    [Header("Eventos")]
    public UnityEvent OnThrow;

    private FirstPersonController playerController;
    private PlayerInventory inventory;
    private PlayerControls controls => InputManager.Controls;

    private void Awake()
    {
        playerController = GetComponent<FirstPersonController>();
        inventory = GetComponent<PlayerInventory>();
    }

    private void OnEnable()
    {
        controls.Combate.Enable();
        controls.Combate.Throw.performed += HandleThrow;
        controls.Combate.NextItem.performed += HandleNextItem;
    }

    private void OnDisable()
    {
        controls.Combate.Throw.performed -= HandleThrow;
        controls.Combate.NextItem.performed -= HandleNextItem;
        controls.Combate.Disable();
    }

    private bool IsGameplayActive()
    {
        // Si el juego está en pausa, FirstPersonController está desactivado: ignoramos el input.
        return playerController == null || playerController.enabled;
    }

    private void HandleNextItem(InputAction.CallbackContext context)
    {
        if (!IsGameplayActive()) return;
        inventory.NextItem();
    }

    private void HandleThrow(InputAction.CallbackContext context)
    {
        if (!IsGameplayActive()) return;

        ThrowableItemData item = inventory.CurrentItem;
        if (item == null || item.projectilePrefab == null || throwPoint == null) return;

        GameObject projectile = Instantiate(item.projectilePrefab, throwPoint.position, throwPoint.rotation);

        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(throwPoint.forward * item.throwForce, ForceMode.VelocityChange);
        }

        OnThrow?.Invoke();
    }
}
