using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Gestiona el lanzamiento de objetos (comida, de momento) desde el jugador.
/// Escucha la acción "Throw" del Action Map "Combate" (InputManager.Controls),
/// instancia el prefab del proyectil en el punto de lanzamiento, y lo empuja
/// hacia adelante con física real.
/// </summary>
[RequireComponent(typeof(FirstPersonController))]
public class PlayerThrowController : MonoBehaviour
{
    [Header("Lanzamiento")]
    [Tooltip("El prefab del objeto que se lanza (ej: comida). Debe tener Rigidbody y ThrowableProjectile.")]
    [SerializeField] private GameObject projectilePrefab;
    [Tooltip("Punto desde el que sale el objeto lanzado (normalmente un hijo vacío delante de la cámara).")]
    [SerializeField] private Transform throwPoint;
    [SerializeField] private float throwForce = 15f;

    [Header("Eventos")]
    public UnityEvent OnThrow;

    private FirstPersonController playerController;
    private PlayerControls controls => InputManager.Controls;

    private void Awake()
    {
        playerController = GetComponent<FirstPersonController>();
    }

    private void OnEnable()
    {
        controls.Combate.Enable();
        controls.Combate.Throw.performed += HandleThrow;
    }

    private void OnDisable()
    {
        controls.Combate.Throw.performed -= HandleThrow;
        controls.Combate.Disable();
    }

    private void HandleThrow(InputAction.CallbackContext context)
    {
        // Si el juego está en pausa, FirstPersonController está desactivado: ignoramos el lanzamiento.
        if (playerController != null && !playerController.enabled) return;
        if (projectilePrefab == null || throwPoint == null) return;

        GameObject projectile = Instantiate(projectilePrefab, throwPoint.position, throwPoint.rotation);

        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(throwPoint.forward * throwForce, ForceMode.VelocityChange);
        }

        OnThrow?.Invoke();
    }
}
