using UnityEngine;
using UnityEngine.Events;
using UnityEngine.AI;

/// <summary>
/// UnityEvent que lleva la posición del ruido oído.
/// </summary>
[System.Serializable]
public class NoiseHeardEvent : UnityEvent<Vector3> { }

/// <summary>
/// Da oído a un humano. Durante el tutorial (TutorialManager.IsPlayerProtected = true),
/// ignora cualquier ruido o pisada generada en el juego.
/// Ajustado para validar y proyectar ruidos en zonas caminables del NavMesh (evita atascamientos en mesas/props).
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

    [Header("Ajuste de Navegación")]
    [Tooltip("Radio de búsqueda en metros para encontrar suelo caminable cerca del origen del ruido.")]
    [SerializeField] private float navMeshSampleRadius = 3.0f;

    [Header("Eventos")]
    [Tooltip("Se dispara cada vez que oye un ruido, con la posición de donde vino.")]
    public NoiseHeardEvent OnNoiseHeard;

    public Vector3 LastNoisePosition { get; private set; }
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

    public void Hear(Vector3 position, float radius)
    {
        // Ignorar todo ruido mientras el jugador está protegido por el tutorial
        if (TutorialManager.IsPlayerProtected) return;

        float effectiveRadius = radius * hearingSensitivity;
        float distance = Vector3.Distance(transform.position, position);

        if (distance > effectiveRadius) return;

        // Corregir y ajustar la posición hacia una zona del NavMesh caminable bordeando el obstáculo
        Vector3 validPosition = GetValidNavMeshPosition(position);

        LastNoisePosition = validPosition;
        HasHeardNoise = true;

        float closeness = 1f - Mathf.Clamp01(distance / effectiveRadius);

        if (vision != null)
        {
            float amount = suspicionPerNoise * Mathf.Lerp(0.4f, 1f, closeness);
            vision.AddSuspicion(amount, maxSuspicionFromNoise);
        }

        if (OnNoiseHeard != null) OnNoiseHeard.Invoke(validPosition);
    }

    /// <summary>
    /// Proyecta la coordenada del ruido hacia el suelo y busca la posición de NavMesh más cercana.
    /// </summary>
    private Vector3 GetValidNavMeshPosition(Vector3 rawPosition)
    {
        Vector3 targetPoint = rawPosition;

        // 1. Si el ruido ocurrió en alto (encima de una mesa/objeto), lanzamos un Raycast hacia el suelo
        if (Physics.Raycast(rawPosition, Vector3.down, out RaycastHit rayHit, 5f))
        {
            targetPoint = rayHit.point;
        }

        // 2. Buscamos el punto caminable más cercano en el NavMesh respecto a la mesa
        if (NavMesh.SamplePosition(targetPoint, out NavMeshHit navHit, navMeshSampleRadius, NavMesh.AllAreas))
        {
            return navHit.position;
        }

        return rawPosition;
    }
}