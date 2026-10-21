using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Permite al jugador agacharse con la acción "Crouch" del mapa "Movimiento" (C por defecto).
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(FirstPersonController))]
public class PlayerCrouch : MonoBehaviour
{
    [Header("Control")]
    [SerializeField] private bool toggleMode = true;

    [Header("Agachado")]
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchSpeedMultiplier = 0.5f;
    [SerializeField] private float transitionSpeed = 10f;

    [Header("Cámara")]
    [SerializeField] private Transform cameraTransform;

    public bool IsCrouching { get; private set; }

    public float SpeedMultiplier
    {
        get { return IsCrouching ? crouchSpeedMultiplier : 1f; }
    }

    private CharacterController characterController;
    private FirstPersonController playerController;
    private InputAction crouchAction;

    private float standingHeight;
    private float feetOffsetY;
    private float centerX;
    private float centerZ;
    private float standingCameraY;
    private float currentHeight;
    private bool wantsCrouch;

    private readonly Collider[] overlapBuffer = new Collider[8];

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        playerController = GetComponent<FirstPersonController>();

        standingHeight = characterController.height;
        currentHeight = standingHeight;
        centerX = characterController.center.x;
        centerZ = characterController.center.z;
        feetOffsetY = characterController.center.y - standingHeight * 0.5f;

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
        if (cameraTransform != null)
        {
            standingCameraY = cameraTransform.localPosition.y;
        }
    }

    /// <summary>
    /// Restablece el estado de agachado al reaparecer tras un Game Over.
    /// </summary>
    public void ResetCrouch()
    {
        wantsCrouch = false;
        IsCrouching = false;
        currentHeight = standingHeight;

        if (characterController != null)
        {
            characterController.height = standingHeight;
            characterController.center = new Vector3(centerX, feetOffsetY + standingHeight * 0.5f, centerZ);
        }

        if (cameraTransform != null)
        {
            Vector3 camPos = cameraTransform.localPosition;
            camPos.y = standingCameraY;
            cameraTransform.localPosition = camPos;
        }
    }

    private void Update()
    {
        if (crouchAction == null)
        {
            crouchAction = InputManager.Controls.asset.FindAction("Movimiento/Crouch");
            if (crouchAction == null)
            {
                Debug.LogWarning("PlayerCrouch: no existe la acción 'Crouch' en el mapa 'Movimiento' de PlayerControls.");
                enabled = false;
                return;
            }
        }

        if (playerController.enabled)
        {
            if (toggleMode)
            {
                if (crouchAction.WasPressedThisFrame()) wantsCrouch = !wantsCrouch;
            }
            else
            {
                wantsCrouch = crouchAction.IsPressed();
            }
        }

        if (wantsCrouch)
        {
            IsCrouching = true;
        }
        else if (IsCrouching && HasHeadroom())
        {
            IsCrouching = false;
        }

        ApplyHeight();
    }

    private void ApplyHeight()
    {
        float targetHeight = IsCrouching ? crouchHeight : standingHeight;
        currentHeight = Mathf.Lerp(currentHeight, targetHeight, transitionSpeed * Time.deltaTime);

        characterController.height = currentHeight;
        characterController.center = new Vector3(centerX, feetOffsetY + currentHeight * 0.5f, centerZ);

        if (cameraTransform != null)
        {
            Vector3 cameraPosition = cameraTransform.localPosition;
            cameraPosition.y = standingCameraY - (standingHeight - currentHeight);
            cameraTransform.localPosition = cameraPosition;
        }
    }

    private bool HasHeadroom()
    {
        float radius = characterController.radius * 0.95f;
        Vector3 feet = transform.position + Vector3.up * feetOffsetY;

        Vector3 bottom = feet + Vector3.up * (crouchHeight + radius);
        Vector3 top = feet + Vector3.up * (standingHeight - radius);

        int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, overlapBuffer, ~0, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            if (overlapBuffer[i].transform.IsChildOf(transform)) continue;
            return false;
        }

        return true;
    }
}