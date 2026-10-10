using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dibuja la barra de vida del jugador y, opcionalmente, un flash rojo en toda la pantalla
/// cada vez que recibe daño. Se conecta solo al PlayerHealth de la escena.
///
/// Debe ir en un objeto que esté siempre activo (por ejemplo el Canvas del HUD).
/// </summary>
public class HealthHUD : MonoBehaviour
{
    [Header("Barra de vida")]
    [Tooltip("La imagen roja de relleno, hija de la barra de fondo. Este script le cambia el ancho.")]
    [SerializeField] private RectTransform fillRect;

    [Header("Flash de daño (opcional)")]
    [Tooltip("Imagen roja a pantalla completa, con Raycast Target desmarcado.")]
    [SerializeField] private Image damageFlash;
    [SerializeField] private float flashAlpha = 0.35f;
    [Tooltip("Lo rápido que se desvanece el flash.")]
    [SerializeField] private float flashFadeSpeed = 1.5f;

    private PlayerHealth health;

    private void Start()
    {
        health = FindObjectOfType<PlayerHealth>();

        if (health != null)
        {
            health.OnHealthChanged += UpdateBar;
            health.OnDamaged += TriggerFlash;
            UpdateBar(health.CurrentHealth, health.MaxHealth);
        }
        else
        {
            Debug.LogWarning("HealthHUD: no se encontró PlayerHealth en la escena.");
        }

        SetFlashAlpha(0f);
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnHealthChanged -= UpdateBar;
            health.OnDamaged -= TriggerFlash;
        }
    }

    private void Update()
    {
        if (damageFlash == null) return;

        float alpha = damageFlash.color.a;
        if (alpha > 0f)
        {
            // Tiempo sin escalar: el flash se desvanece aunque el juego esté congelado (al morir).
            SetFlashAlpha(Mathf.Max(0f, alpha - flashFadeSpeed * Time.unscaledDeltaTime));
        }
    }

    private void UpdateBar(float current, float max)
    {
        if (fillRect == null) return;

        float normalized = max > 0f ? Mathf.Clamp01(current / max) : 0f;

        // El relleno ocupa desde el borde izquierdo de la barra hasta una fracción de su ancho.
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(normalized, 1f);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
    }

    private void TriggerFlash()
    {
        SetFlashAlpha(flashAlpha);
    }

    private void SetFlashAlpha(float alpha)
    {
        if (damageFlash == null) return;

        Color color = damageFlash.color;
        color.a = alpha;
        damageFlash.color = color;
    }
}
