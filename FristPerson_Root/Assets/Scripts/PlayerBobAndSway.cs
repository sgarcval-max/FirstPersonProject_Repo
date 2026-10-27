using UnityEngine;

/// <summary>
/// Control de bamboleo (Bobbing), balanceo (Sway) e impactos de salto/caída.
/// Adapta automáticamente el movimiento si el jugador camina, corre, va agachado, apunta o salta.
/// Corregido el temblor al agacharse respetando la altura dinámica de la cámara.
/// </summary>
public class PlayerBobAndSway : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Cámara del jugador en primera persona.")]
    [SerializeField] private Transform playerCamera;

    [Tooltip("Contenedor/Pivot del arma u objeto sostenido (HandPoint).")]
    [SerializeField] private Transform weaponHolder;

    [Tooltip("CharacterController para detectar velocidad y si está en el suelo.")]
    [SerializeField] private CharacterController characterController;

    [Header("Bamboleo Base (Caminar Normal)")]
    [SerializeField] private bool enableCameraBob = true;
    [SerializeField] private bool enableWeaponBob = true;
    [SerializeField] private float camBobFrequency = 11f;
    [SerializeField] private float camBobHorizontalAmount = 0.025f;
    [SerializeField] private float camBobVerticalAmount = 0.04f;
    [SerializeField] private float weaponBobHorizontalAmount = 0.035f;
    [SerializeField] private float weaponBobVerticalAmount = 0.035f;

    [Header("Multiplicadores por Estado")]
    [Tooltip("Velocidad y amplitud al CORRER.")]
    [SerializeField] private float sprintFreqMultiplier = 1.35f;
    [SerializeField] private float sprintAmpMultiplier = 1.4f;

    [Tooltip("Velocidad y amplitud al ir AGACHADO.")]
    [SerializeField] private float crouchFreqMultiplier = 0.65f;
    [SerializeField] private float crouchAmpMultiplier = 0.35f;

    [Header("Balanceo con Ratón (Sway)")]
    [SerializeField] private bool enableWeaponSway = true;
    [SerializeField] private float swayAmount = 0.04f;
    [SerializeField] private float maxSwayAmount = 0.1f;
    [SerializeField] private float swaySmoothness = 8f;

    [Header("Impacto de Salto y Aterrizaje")]
    [Tooltip("Efecto de sacudida vertical al saltar o tocar suelo.")]
    [SerializeField] private bool enableJumpLandImpact = true;
    [SerializeField] private float jumpDipAmount = -0.06f;
    [SerializeField] private float landImpactAmount = -0.1f;
    [SerializeField] private float impactRecoverySpeed = 8f;

    [Header("Ajustes al Apuntar (ADS)")]
    [SerializeField] private float aimingCameraBobMultiplier = 0.15f;
    [SerializeField] private float aimingWeaponBobMultiplier = 0.15f;
    [SerializeField] private float aimingSwayMultiplier = 0.15f;

    private PlayerAim playerAim;
    private PlayerCrouch playerCrouch;

    private Vector3 initialWeaponLocalPos;

    private float bobTimer = 0f;
    private bool isAiming = false;
    private bool wasGrounded = true;

    private Vector3 currentCamImpactOffset = Vector3.zero;
    private Vector3 currentWeaponImpactOffset = Vector3.zero;

    private Vector3 currentCamBobOffset = Vector3.zero;

    public void SetAiming(bool aiming)
    {
        isAiming = aiming;
    }

    private void Start()
    {
        playerAim = GetComponent<PlayerAim>();
        if (playerAim == null) playerAim = GetComponentInParent<PlayerAim>();

        playerCrouch = GetComponent<PlayerCrouch>();
        if (playerCrouch == null) playerCrouch = GetComponentInParent<PlayerCrouch>();

        if (characterController == null)
            characterController = GetComponentInParent<CharacterController>();

        if (weaponHolder != null)
            initialWeaponLocalPos = weaponHolder.localPosition;
    }

    private void Update()
    {
        bool isGrounded = characterController != null && characterController.isGrounded;
        float speed = GetPlayerSpeed();
        bool isMoving = speed > 0.1f && isGrounded;
        bool isCrouching = playerCrouch != null && playerCrouch.IsCrouching;
        bool isSprinting = speed > 5.5f && !isCrouching;

        // 1. Detectar salto y aterrizaje (se omite agachado para evitar micro-impactos continuos)
        if (!isCrouching)
        {
            HandleJumpAndLand(isGrounded);
        }

        // 2. Determinar multiplicadores de bamboleo según el estado
        float freqMult = 1f;
        float ampMult = 1f;

        if (isCrouching)
        {
            freqMult = crouchFreqMultiplier;
            ampMult = crouchAmpMultiplier;
        }
        else if (isSprinting)
        {
            freqMult = sprintFreqMultiplier;
            ampMult = sprintAmpMultiplier;
        }

        Vector3 currentBasePos = (playerAim != null) ? playerAim.CurrentBaseHandPosition : initialWeaponLocalPos;
        Vector3 weaponBobOffset = Vector3.zero;
        Vector3 weaponSwayOffset = Vector3.zero;

        // 3. Balanceo con el ratón (Sway)
        if (enableWeaponSway && weaponHolder != null)
        {
            weaponSwayOffset = CalculateWeaponSwayOffset();
        }

        // 4. Bamboleo al caminar/correr/agacharse
        if (isMoving)
        {
            bobTimer += Time.deltaTime * (camBobFrequency * freqMult);

            if (enableCameraBob && playerCamera != null)
            {
                float camAimMult = isAiming ? aimingCameraBobMultiplier : 1f;
                CalculateCameraBobOffset(ampMult * camAimMult);
            }

            if (enableWeaponBob && weaponHolder != null)
            {
                float weaponAimMult = isAiming ? aimingWeaponBobMultiplier : 1f;
                weaponBobOffset = CalculateWeaponBobOffset(ampMult * weaponAimMult);
            }
        }
        else
        {
            bobTimer = 0f;
            currentCamBobOffset = Vector3.Lerp(currentCamBobOffset, Vector3.zero, Time.deltaTime * 6f);
        }

        // 5. Recuperar suavemente la posición tras sacudidas de salto/aterrizaje
        currentCamImpactOffset = Vector3.Lerp(currentCamImpactOffset, Vector3.zero, Time.deltaTime * impactRecoverySpeed);
        currentWeaponImpactOffset = Vector3.Lerp(currentWeaponImpactOffset, Vector3.zero, Time.deltaTime * impactRecoverySpeed);

        // 6. Aplicar posición final al contenedor del arma
        if (weaponHolder != null)
        {
            Vector3 targetWeaponPos = currentBasePos + weaponBobOffset + weaponSwayOffset + currentWeaponImpactOffset;
            weaponHolder.localPosition = Vector3.Lerp(weaponHolder.localPosition, targetWeaponPos, Time.deltaTime * 10f);
        }

        wasGrounded = isGrounded;
    }

    private void HandleJumpAndLand(bool isGrounded)
    {
        if (!enableJumpLandImpact) return;

        // Despegue (Salto)
        if (wasGrounded && !isGrounded && characterController.velocity.y > 0f)
        {
            currentCamImpactOffset.y = jumpDipAmount * 0.5f;
            currentWeaponImpactOffset.y = jumpDipAmount;
        }
        // Aterrizaje (Tocar suelo)
        else if (!wasGrounded && isGrounded)
        {
            currentCamImpactOffset.y = landImpactAmount * 0.5f;
            currentWeaponImpactOffset.y = landImpactAmount;
        }
    }

    private float GetPlayerSpeed()
    {
        if (characterController == null) return 0f;
        Vector3 horizontalVelocity = new Vector3(characterController.velocity.x, 0f, characterController.velocity.z);
        return horizontalVelocity.magnitude;
    }

    private void CalculateCameraBobOffset(float intensity)
    {
        float xOffset = Mathf.Cos(bobTimer * 0.5f) * camBobHorizontalAmount * intensity;
        float yOffset = Mathf.Sin(bobTimer) * camBobVerticalAmount * intensity;

        Vector3 targetBob = new Vector3(xOffset, yOffset, 0f);
        currentCamBobOffset = Vector3.Lerp(currentCamBobOffset, targetBob, Time.deltaTime * 10f);
    }

    private Vector3 CalculateWeaponBobOffset(float intensity)
    {
        float xOffset = Mathf.Sin(bobTimer * 0.5f) * weaponBobHorizontalAmount * intensity;
        float yOffset = Mathf.Cos(bobTimer) * weaponBobVerticalAmount * intensity;

        return new Vector3(xOffset, yOffset, 0f);
    }

    private Vector3 CalculateWeaponSwayOffset()
    {
        float currentSway = isAiming ? swayAmount * aimingSwayMultiplier : swayAmount;

        float mouseX = -Input.GetAxis("Mouse X") * currentSway;
        float mouseY = -Input.GetAxis("Mouse Y") * currentSway;

        mouseX = Mathf.Clamp(mouseX, -maxSwayAmount, maxSwayAmount);
        mouseY = Mathf.Clamp(mouseY, -maxSwayAmount, maxSwayAmount);

        return new Vector3(mouseX, mouseY, 0f);
    }
}