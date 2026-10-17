using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Permite apuntar con un arma a distancia mientras mantienes pulsada la tecla de apuntar
/// (clic derecho por defecto): la cámara hace zoom, el arma se coloca en el centro de la pantalla,
/// la cámara gira más despacio, andas más despacio, no puedes correr y aparece la mira.
///
/// Usa la acción "Aim" del mapa "Combate" de PlayerControls.
/// </summary>
[RequireComponent(typeof(FirstPersonController))]
[RequireComponent(typeof(PlayerWeaponInventory))]
public class PlayerAim : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("La cámara del jugador. Si se deja vacío, usa la cámara principal.")]
    [SerializeField] private Camera playerCamera;
    [Tooltip("El HandPoint (donde se ve el arma en la mano). Se mueve al centro al apuntar.")]
    [SerializeField] private Transform handPoint;
    [Tooltip("Opcional: la mira de la pantalla (un objeto del HUD). Solo se ve mientras apuntas.")]
    [SerializeField] private GameObject crosshair;

    [Header("Cómo se siente")]
    [Tooltip("Campo de visión al apuntar. Más pequeño = más zoom (el normal suele ser 60).")]
    [SerializeField] private float aimFieldOfView = 40f;
    [Tooltip("Posición del arma respecto a la cámara al apuntar (centrada y un poco más cerca).")]
    [SerializeField] private Vector3 aimHandLocalPosition = new Vector3(0f, -0.2f, 0.7f);
    [Tooltip("Lo rápido que entra y sale del modo apuntar.")]
    [SerializeField] private float transitionSpeed = 12f;
    [Tooltip("Multiplicador de la sensibilidad de la cámara al apuntar (0.5 = la mitad de rápido).")]
    [Range(0.1f, 1f)]
    [SerializeField] private float aimLookMultiplier = 0.5f;
    [Tooltip("Multiplicador de la velocidad al andar apuntando.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float aimSpeedMultiplier = 0.5f;

    /// <summary>True mientras mantienes apuntar con un arma a distancia en la mano.</summary>
    public bool IsAiming { get; private set; }

    public float LookMultiplier
    {
        get { return IsAiming ? aimLookMultiplier : 1f; }
    }

    public float SpeedMultiplier
    {
        get { return IsAiming ? aimSpeedMultiplier : 1f; }
    }

    private FirstPersonController playerController;
    private PlayerWeaponInventory inventory;
    private InputAction aimAction;
    private bool searchedForAction;

    private float normalFieldOfView;
    private Vector3 hipHandLocalPosition;

    private void Awake()
    {
        playerController = GetComponent<FirstPersonController>();
        inventory = GetComponent<PlayerWeaponInventory>();

        if (playerCamera == null) playerCamera = Camera.main;

        if (playerCamera != null) normalFieldOfView = playerCamera.fieldOfView;
        if (handPoint != null) hipHandLocalPosition = handPoint.localPosition;

        if (crosshair != null) crosshair.SetActive(false);
    }

    private void Update()
    {
        IsAiming = ShouldAim();

        ApplyVisuals();
    }

    private bool ShouldAim()
    {
        // En pausa, atrapado o escondido, los controles están desactivados: no se apunta.
        if (!playerController.enabled) return false;

        // Solo se apunta con un arma a distancia en la mano.
        WeaponData weapon = inventory.EquippedWeapon;
        if (weapon == null || !weapon.isRanged) return false;

        if (aimAction == null)
        {
            if (searchedForAction) return false;

            searchedForAction = true;
            aimAction = InputManager.Controls.asset.FindAction("Combate/Aim");

            if (aimAction == null)
            {
                Debug.LogWarning("PlayerAim: no existe la acción 'Aim' en el mapa 'Combate' de PlayerControls. Créala y asígnale el clic derecho del ratón.");
                return false;
            }
        }

        return aimAction.IsPressed();
    }

    private void ApplyVisuals()
    {
        // Tiempo sin escalar: así, si se pausa mientras apuntas, la vista vuelve a la normalidad igualmente.
        float step = transitionSpeed * Time.unscaledDeltaTime;

        if (playerCamera != null)
        {
            float targetFov = IsAiming ? aimFieldOfView : normalFieldOfView;
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, step);
        }

        if (handPoint != null)
        {
            Vector3 targetPosition = IsAiming ? aimHandLocalPosition : hipHandLocalPosition;
            handPoint.localPosition = Vector3.Lerp(handPoint.localPosition, targetPosition, step);
        }

        if (crosshair != null && crosshair.activeSelf != IsAiming)
        {
            crosshair.SetActive(IsAiming);
        }
    }
}
