using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// UnityEvent que lleva la posición del ruido oído.
/// </summary>
[System.Serializable]
public class NoiseHeardEvent : UnityEvent<Vector3> { }

/// <summary>
/// Da oído a un humano: se registra en el NoiseSystem y reacciona a los ruidos que ocurren
/// dentro de su radio de audición. Al oír uno, sube un poco su sospecha (sin llegar nunca al
/// máximo solo por ruido: para estar seguro tiene que verte), guarda dónde fue el ruido y
/// avisa con un evento, para que más adelante la IA pueda ir a investigar.
/// </summary>
public class HumanHearing : MonoBehaviour
{
    [Header("Oído")]
    [Tooltip("Sensibilidad. 1 = normal, 2 = oye el doble de lejos, 0.5 = oye la mitad.")]
    [SerializeField] private float hearingSensitivity = 1f;

    [Header("Reacción")]
    [Tooltip("Cuánta sospecha gana por cada ruido oído (más si el ruido es cercano).")]
    [SerializeField] private float suspicionPerNoise = 0.3f;
    [Tooltip("Límite de sospecha que se puede alcanzar solo con ruidos, sin ver al jugador.")]
    [Range(0f, 1f)]
    [SerializeField] private float maxSuspicionFromNoise = 0.7f;
    [Tooltip("Si se deja vacío, usa el HumanVision del mismo objeto.")]
    [SerializeField] private HumanVision vision;

    [Header("Eventos")]
    [Tooltip("Se dispara cada vez que oye un ruido, con la posición de donde vino.")]
    public NoiseHeardEvent OnNoiseHeard;

    /// <summary>Dónde fue el último ruido que oyó.</summary>
    public Vector3 LastNoisePosition { get; private set; }

    /// <summary>True si ha oído algún ruido desde la última vez que se reseteó.</summary>
    public bool HasHeardNoise { get; set; }

    private void Awake()
    {
        if (vision == null)
        {
            vision = GetComponent<HumanVision>();
        }
    }

    private void OnEnable()
    {
        NoiseSystem.Register(this);
    }

    private void OnDisable()
    {
        NoiseSystem.Unregister(this);
    }

    /// <summary>Lo llama NoiseSystem cuando hay un ruido. Comprueba si está lo bastante cerca.</summary>
    public void Hear(Vector3 position, float radius)
    {
        float effectiveRadius = radius * hearingSensitivity;
        float distance = Vector3.Distance(transform.position, position);

        if (distance > effectiveRadius) return;

        LastNoisePosition = position;
        HasHeardNoise = true;

        float closeness = 1f - Mathf.Clamp01(distance / effectiveRadius);

        if (vision != null)
        {
            float amount = suspicionPerNoise * Mathf.Lerp(0.4f, 1f, closeness);
            vision.AddSuspicion(amount, maxSuspicionFromNoise);
        }

        Debug.Log("[" + name + "] Ha oído un ruido a " + distance.ToString("0.0") + " metros.");

        if (OnNoiseHeard != null) OnNoiseHeard.Invoke(position);
    }
}