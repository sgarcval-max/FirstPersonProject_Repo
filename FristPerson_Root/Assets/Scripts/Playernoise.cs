using UnityEngine;

/// <summary>
/// Hace que el jugador genere ruido al moverse. Escucha los eventos que ya tiene
/// FirstPersonController (pasos, correr, salto, aterrizaje), así que no hace falta tocar
/// ese script. Caminar hace poco ruido; correr, mucho más.
/// </summary>
[RequireComponent(typeof(FirstPersonController))]
public class PlayerNoise : MonoBehaviour
{
    [Header("Radio del ruido (metros)")]
    [SerializeField] private float walkNoiseRadius = 2.5f;
    [SerializeField] private float runNoiseRadius = 8f;
    [SerializeField] private float jumpNoiseRadius = 4f;
    [SerializeField] private float landNoiseRadius = 6f;

    [Tooltip("Radio del ruido de los pasos cuando vas agachado (casi silencioso).")]
    [SerializeField] private float crouchNoiseRadius = 0.8f;

    private FirstPersonController controller;
    private PlayerCrouch crouch;
    private bool running;

    private void Awake()
    {
        controller = GetComponent<FirstPersonController>();
        crouch = GetComponent<PlayerCrouch>();
    }

    private void OnEnable()
    {
        controller.OnFootstep.AddListener(HandleFootstep);
        controller.OnStartRunning.AddListener(HandleStartRunning);
        controller.OnStopRunning.AddListener(HandleStopRunning);
        controller.OnJump.AddListener(HandleJump);
        controller.OnLanded.AddListener(HandleLanded);
    }

    private void OnDisable()
    {
        controller.OnFootstep.RemoveListener(HandleFootstep);
        controller.OnStartRunning.RemoveListener(HandleStartRunning);
        controller.OnStopRunning.RemoveListener(HandleStopRunning);
        controller.OnJump.RemoveListener(HandleJump);
        controller.OnLanded.RemoveListener(HandleLanded);
    }

    private void HandleStartRunning()
    {
        running = true;
    }

    private void HandleStopRunning()
    {
        running = false;
    }

    private void HandleFootstep()
    {
        if (crouch != null && crouch.IsCrouching)
        {
            Emit(crouchNoiseRadius);
        }
        else
        {
            Emit(running ? runNoiseRadius : walkNoiseRadius);
        }
    }

    private void HandleJump()
    {
        Emit(jumpNoiseRadius);
    }

    private void HandleLanded()
    {
        Emit(landNoiseRadius);
    }

    private void Emit(float radius)
    {
        // Al empezar la partida el jugador "aterriza" en el suelo y eso no debe contar como ruido.
        if (Time.timeSinceLevelLoad < 1f) return;

        // Un escondite puede amortiguar (o anular del todo) el ruido que haces.
        PlayerConcealment concealment = PlayerConcealment.Instance;
        if (concealment != null)
        {
            radius *= concealment.NoiseMultiplier;
        }
        if (radius <= 0.01f) return;

        NoiseSystem.Emit(transform.position, radius);
    }
}
