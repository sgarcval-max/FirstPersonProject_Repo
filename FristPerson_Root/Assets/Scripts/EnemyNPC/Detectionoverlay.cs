using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tiñe la pantalla de rojo a medida que algún guardia te va detectando: sin sospecha no se ve
/// nada, y cuanto más cerca está de detectarte, más rojo se ve. El juego sigue visible de fondo,
/// porque el rojo nunca llega a ser opaco (se controla con Max Alpha).
///
/// Debe ir en un objeto que esté siempre activo (por ejemplo el Canvas del HUD).
/// </summary>
public class DetectionOverlay : MonoBehaviour
{
    [Tooltip("Imagen roja a pantalla completa, con Raycast Target desmarcado.")]
    [SerializeField] private Image overlay;

    [Tooltip("Opacidad máxima del rojo cuando el guardia está a punto de pillarte (0 a 1). Con 0.6 se sigue viendo el juego de fondo.")]
    [Range(0f, 1f)]
    [SerializeField] private float maxAlpha = 0.6f;

    [Tooltip("Lo rápido que sigue el rojo a la sospecha real. Más alto = más reactivo.")]
    [SerializeField] private float responseSpeed = 6f;

    private float currentAlpha;

    private void Update()
    {
        if (overlay == null) return;

        float targetAlpha = HumanVision.HighestSuspicion * maxAlpha;

        // Tiempo sin escalar: así el rojo sigue animándose aunque el juego esté congelado.
        currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, responseSpeed * Time.unscaledDeltaTime);

        Color color = overlay.color;
        color.a = currentAlpha;
        overlay.color = color;
    }
}
