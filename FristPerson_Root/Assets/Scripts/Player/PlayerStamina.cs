using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gestiona la estamina del jugador al correr y actualiza la barra en la UI.
/// </summary>
public class PlayerStamina : MonoBehaviour
{
    [Header("Configuración de Estamina")]
    [SerializeField] private float maxStamina = 100f;
    [Tooltip("Puntos por segundo que se gastan al correr.")]
    [SerializeField] private float drainRate = 20f;
    [Tooltip("Puntos por segundo que se recuperan al andar o estar parado.")]
    [SerializeField] private float recoveryRate = 16f;

    [Header("Interfaz UI")]
    [Tooltip("La imagen de la barra (debe tener Image Type = Filled).")]
    [SerializeField] private Image staminaFillImage;
    [Tooltip("Contenedor/marco opcional de la barra.")]
    [SerializeField] private GameObject staminaBarContainer;

    public float CurrentStamina { get; private set; }
    public bool IsExhausted { get; private set; }

    /// <summary>Devuelve true solo si al jugador le queda estamina y no está agotado.</summary>
    public bool CanRun => !IsExhausted && CurrentStamina > 0f;

    private FirstPersonController controller;
    private bool isRunning;

    private void Awake()
    {
        CurrentStamina = maxStamina;
        controller = GetComponent<FirstPersonController>();
    }

    private void OnEnable()
    {
        if (controller != null)
        {
            controller.OnStartRunning.AddListener(OnStartRunning);
            controller.OnStopRunning.AddListener(OnStopRunning);
        }
    }

    private void OnDisable()
    {
        if (controller != null)
        {
            controller.OnStartRunning.RemoveListener(OnStartRunning);
            controller.OnStopRunning.RemoveListener(OnStopRunning);
        }
    }

    private void OnStartRunning() => isRunning = true;
    private void OnStopRunning() => isRunning = false;

    private void Update()
    {
        if (controller == null) return;

        // Si el jugador está intentando correr y no está agotado
        if (isRunning && !IsExhausted)
        {
            CurrentStamina = Mathf.Max(0f, CurrentStamina - drainRate * Time.deltaTime);

            if (CurrentStamina <= 0f)
            {
                IsExhausted = true;
                isRunning = false;
            }
        }
        else
        {
            // Recuperación de estamina progresiva
            CurrentStamina = Mathf.Min(maxStamina, CurrentStamina + recoveryRate * Time.deltaTime);

            // Solo se quita el estado de agotamiento al llegar al 100%
            if (IsExhausted && CurrentStamina >= maxStamina)
            {
                IsExhausted = false;
            }
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (staminaFillImage != null)
        {
            staminaFillImage.fillAmount = CurrentStamina / maxStamina;
        }

        if (staminaBarContainer != null)
        {
            bool show = CurrentStamina < maxStamina || IsExhausted;
            staminaBarContainer.SetActive(show);
        }
    }
}