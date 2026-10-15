using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Permite al jugador agacharse con la acción "Crouch" del mapa "Movimiento" (C por defecto).
/// Agachado: baja la cámara y encoge el cuerpo, anda más despacio, no puede correr ni saltar,
/// hace mucho menos ruido (PlayerNoise) y los guardias lo ven peor (HumanVision).
/// No te deja levantarte si hay algo justo encima de tu cabeza.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(FirstPersonController))]
public class PlayerCrouch : MonoBehaviour
{
    [Header("Control")]
    [Tooltip("Marcado: pulsas una vez para agacharte y otra para levantarte. Desmarcado: te agachas solo mientras mantienes la tecla.")]
    [SerializeField] private bool toggleMode = true;

    [Header("Agachado")]
    [Tooltip("Altura del cuerpo cuando estás agachado (de pie suele ser 2).")]
    [SerializeField] private float crouchHeight = 1f;
    [Tooltip("Multiplicador de velocidad al ir agachado (0.5 = la mitad de rápido).")]
    [SerializeField] private float crouchSpeedMultiplier = 0.5f;
    [Tooltip("Lo rápido que baja y sube el cuerpo y la cámara.")]
    [SerializeField] private float transitionSpeed = 10f;

    [Header("Cámara")]
    [Tooltip("La cámara del jugador. Si se deja vacío, usa la cámara principal.")]
    [SerializeField] private Transform cameraTransform;

    /// <summary>True mientras el jugador está agachado (o no puede levantarse todavía).</summary>
    public bool IsCrouching { get; private set; }

    /// <summary>Por cuánto se multiplica la velocidad de movimiento en el estado actual.</summary>
    public float SpeedMultiplier
    {
        get { return IsCrouching ? crouchSpeedMultiplier : 1f; }
    }

    private CharacterController characterController;
    private FirstPersonController playerController;
    private InputAction crouchAction;

    private float standingHeight;
    private float feetOffsetY;      // altura de los pies respecto al origen, para que no floten al cambiar de altura
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

    private void Update()
    {
        // Buscamos la acción por nombre. Si aún no existe en PlayerControls, avisamos una sola vez.
        if (crouchAction == null)
        {
            crouchAction = InputManager.Controls.asset.FindAction("Movimiento/Crouch");
            if (crouchAction == null)
            {
                Debug.LogWarning("PlayerCrouch: no existe la acción 'Crouch' en el mapa 'Movimiento' de PlayerControls. Créala y asígnale la tecla C.");
                enabled = false;
                return;
            }
        }

        // Si el juego está en pausa o has sido atrapado, ignoramos la tecla.
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
            // Solo te levantas si no hay nada encima de tu cabeza.
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

    /// <summary>Comprueba si hay sitio para ponerse de pie (nada sólido justo encima de la cabeza).</summary>
    private bool HasHeadroom()
    {
        float radius = characterController.radius * 0.95f;
        Vector3 feet = transform.position + Vector3.up * feetOffsetY;

        // Una esfera en la zona que ocuparía el cuerpo de pie por encima de la altura agachada.
        Vector3 bottom = feet + Vector3.up * (crouchHeight + radius);
        Vector3 top = feet + Vector3.up * (standingHeight - radius);

        int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, overlapBuffer, ~0, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            // Ignoramos nuestro propio cuerpo.
            if (overlapBuffer[i].transform.IsChildOf(transform)) continue;
            return false;
        }

        return true;
    }
}