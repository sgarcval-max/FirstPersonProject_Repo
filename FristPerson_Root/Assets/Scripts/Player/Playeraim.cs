using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Permite apuntar con un arma a distancia.
/// Expone 'CurrentBaseHandPosition' para que los efectos de bamboleo (Bobbing/Sway) 
/// se apliquen suavemente sobre la mira centrada sin tirones.
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
    [Tooltip("Multiplicador de la sensibilidad de la cámara al apuntar.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float aimLookMultiplier = 0.5f;
    [Tooltip("Multiplicador de la velocidad al andar apuntando.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float aimSpeedMultiplier = 0.5f;

    /// <summary>True mientras mantienes apuntar con un arma a distancia en la mano.</summary>
    public bool IsAiming { get; private set; }

    /// <summary>Posición local base del arma (transicionando libremente entre cadera y mira).</summary>
    public Vector3 CurrentBaseHandPosition { get; private set; }

    public float LookMultiplier => IsAiming ? aimLookMultiplier : 1f;
    public float SpeedMultiplier => IsAiming ? aimSpeedMultiplier : 1f;

    private FirstPersonController playerController;
    private PlayerWeaponInventory inventory;
    private PlayerBobAndSway bobAndSway;
    private InputAction aimAction;
    private bool searchedForAction;

    private float normalFieldOfView;
    private Vector3 hipHandLocalPosition;

    private void Awake()
    {
        playerController = GetComponent<FirstPersonController>();
        inventory = GetComponent<PlayerWeaponInventory>();
        bobAndSway = GetComponent<PlayerBobAndSway>();
        if (bobAndSway == null) bobAndSway = GetComponentInChildren<PlayerBobAndSway>();

        if (playerCamera == null) playerCamera = Camera.main;

        if (playerCamera != null) normalFieldOfView = playerCamera.fieldOfView;
        if (handPoint != null)
        {
            hipHandLocalPosition = handPoint.localPosition;
            CurrentBaseHandPosition = hipHandLocalPosition;
        }

        if (crosshair != null) crosshair.SetActive(false);
    }

    private void Update()
    {
        IsAiming = ShouldAim();

        if (bobAndSway != null)
        {
            bobAndSway.SetAiming(IsAiming);
        }

        ApplyVisuals();
    }

    private bool ShouldAim()
    {
        if (!playerController.enabled) return false;
        if (playerController.UiFocused) return false;

        WeaponData weapon = inventory.EquippedWeapon;
        if (weapon == null || !weapon.isRanged) return false;

        if (aimAction == null)
        {
            if (searchedForAction) return false;

            searchedForAction = true;
            aimAction = InputManager.Controls.asset.FindAction("Combate/Aim");

            if (aimAction == null)
            {
                Debug.LogWarning("PlayerAim: no existe la acción 'Aim' en el mapa 'Combate' de PlayerControls.");
                return false;
            }
        }

        return aimAction.IsPressed();
    }

    private void ApplyVisuals()
    {
        float step = transitionSpeed * Time.unscaledDeltaTime;

        if (playerCamera != null)
        {
            float targetFov = IsAiming ? aimFieldOfView : normalFieldOfView;
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, step);
        }

        if (handPoint != null)
        {
            Vector3 targetPosition = IsAiming ? aimHandLocalPosition : hipHandLocalPosition;
            CurrentBaseHandPosition = Vector3.Lerp(CurrentBaseHandPosition, targetPosition, step);

            // Si no hay script de bamboleo, aplica la posición base directamente
            if (bobAndSway == null)
            {
                handPoint.localPosition = CurrentBaseHandPosition;
            }
        }

        if (crosshair != null && crosshair.activeSelf != IsAiming)
        {
            crosshair.SetActive(IsAiming);
        }
    }
}
