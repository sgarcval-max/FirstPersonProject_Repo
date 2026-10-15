using UnityEngine;

/// <summary>
/// Zona de escondite en la que no se "entra" con ninguna tecla, simplemente te metes dentro
/// (por ejemplo, el hueco debajo de una mesa o de una cama). Mientras estés dentro, y agachado
/// si "Require Crouch" está marcado, los guardias te ven peor.
///
/// Se pone en un objeto con un Collider que cubra la zona (se convierte solo en Trigger).
///   Sight Multiplier: 0 = nadie te ve; 0.3 = te ven solo desde muy cerca; 1 = no te protege.
///   Noise Multiplier: 1 = el ruido que hagas suena con normalidad (te pueden oír aunque no te vean).
/// </summary>
[RequireComponent(typeof(Collider))]
public class HidingZone : MonoBehaviour
{
    [Header("Cuánto te protege")]
    [Range(0f, 1f)]
    [SerializeField] private float sightMultiplier = 0.3f;
    [Range(0f, 1f)]
    [SerializeField] private float noiseMultiplier = 1f;

    [Header("Condiciones")]
    [Tooltip("Si está marcado, solo te protege mientras estás agachado dentro de la zona.")]
    [SerializeField] private bool requireCrouch = true;

    private PlayerConcealment concealment;
    private PlayerCrouch crouch;
    private bool playerInside;
    private bool active;

    private void Awake()
    {
        // La zona tiene que ser un Trigger para que el jugador pueda pasar por ella.
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        FirstPersonController playerController = other.GetComponentInParent<FirstPersonController>();
        if (playerController == null) return;

        playerInside = true;
        concealment = playerController.GetComponent<PlayerConcealment>();
        crouch = playerController.GetComponent<PlayerCrouch>();

        if (concealment == null)
        {
            Debug.LogWarning("HidingZone (" + name + "): el Player necesita el componente Player Concealment.");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<FirstPersonController>() == null) return;

        playerInside = false;
        SetActive(false);
    }

    private void Update()
    {
        if (concealment == null) return;

        bool shouldProtect = playerInside && (!requireCrouch || (crouch != null && crouch.IsCrouching));
        SetActive(shouldProtect);
    }

    private void SetActive(bool value)
    {
        if (concealment == null || value == active) return;

        active = value;

        if (active)
        {
            concealment.SetSource(this, sightMultiplier, noiseMultiplier);
        }
        else
        {
            concealment.RemoveSource(this);
        }
    }

    private void OnDisable()
    {
        SetActive(false);
        playerInside = false;
    }
}
