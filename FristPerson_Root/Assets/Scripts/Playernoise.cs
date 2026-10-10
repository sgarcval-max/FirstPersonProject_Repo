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

    private FirstPersonController controller;
    private bool running;

    private void Awake()
    {
        controller = GetComponent<FirstPersonController>();
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
        Emit(running ? runNoiseRadius : walkNoiseRadius);
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

        NoiseSystem.Emit(transform.position, radius);
    }
}
