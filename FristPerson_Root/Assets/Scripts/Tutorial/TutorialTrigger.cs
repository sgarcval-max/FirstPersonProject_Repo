using UnityEngine;

/// <summary>
/// Coloca este script en el objeto interactuable (o en un Trigger a su alrededor)
/// para notificar al TutorialManager cuando el jugador se acerca o se aleja.
/// </summary>
public class TutorialTrigger : MonoBehaviour
{
    [Tooltip("Etiqueta del jugador (por defecto 'Player').")]
    [SerializeField] private string playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.SetNearInteractable(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.SetNearInteractable(false);
            }
        }
    }
}
