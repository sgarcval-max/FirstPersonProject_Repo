using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Proporciona visión a un humano adaptado a cualquier escala.
/// Respeta la postura del jugador (agachado/de pie) y escala el tiempo de sospecha.
/// </summary>
public class HumanVision : MonoBehaviour
{
    [Header("Referencias de Visión")]
    [Tooltip("Transform opcional de la cabeza u ojos del modelo.")]
    [SerializeField] private Transform eyeTransform;

    [Tooltip("Posición relativa de los ojos si no se asigna 'eyeTransform'.")]
    [SerializeField] private Vector3 eyeOffset = new Vector3(0f, 1.6f, 0f);

    [Header("Parámetros de Visión")]
    [Tooltip("Distancia máxima de visión estando el jugador de pie.")]
    [SerializeField] private float viewDistance = 30f;

    [Tooltip("Ángulo horizontal total del cono de visión (grados).")]
    [Range(10f, 360f)]
    [SerializeField] private float viewAngle = 110f;

    [Tooltip("Radio de presencia inmediata alrededor del gigante (de pie).")]
    [SerializeField] private float proximityRadius = 3.5f;

    [Tooltip("Multiplicador de visión y proximidad cuando el jugador está agachado (0.5 = 50% menos visión).")]
    [Range(0.1f, 1f)]
    [SerializeField] private float crouchViewMultiplier = 0.5f;

    [Tooltip("Punto del jugador que intenta ver (offset vertical).")]
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 0.9f, 0f);

    [Tooltip("Capas de obstáculos que bloquean la visión.")]
    [SerializeField] private LayerMask obstacleMask = ~0;

    [Header("Sospecha")]
    [Tooltip("Tiempo (en segundos) para ser detectado a corta distancia.")]
    [SerializeField] private float timeToDetectClose = 0.6f;
    [Tooltip("Tiempo (en segundos) para ser detectado a larga distancia.")]
    [SerializeField] private float timeToDetectFar = 2.2f;
    [SerializeField] private float decayPerSecond = 0.4f;

    [Header("Aviso visual (provisional)")]
    [SerializeField] private Renderer colorRenderer;
    [SerializeField] private Color calmColor = Color.white;
    [SerializeField] private Color suspiciousColor = Color.yellow;
    [SerializeField] private Color alertColor = Color.red;

    [Header("Eventos")]
    public UnityEvent OnDetected;
    public UnityEvent OnDetectionReset;

    private Transform player;
    private Collider playerCollider;
    private PlayerCrouch playerCrouch;
    private Material materialInstance;
    private readonly RaycastHit[] hitBuffer = new RaycastHit[32];
    private bool detected;

    public float Suspicion { get; private set; }
    public bool CanSeePlayerNow { get; private set; }
    public Vector3 LastSeenPosition { get; private set; }
    public bool HasDetectedPlayer => detected;

    public void AddSuspicion(float amount, float maxLevel)
    {
        if (Suspicion >= maxLevel) return;
        Suspicion = Mathf.Min(maxLevel, Suspicion + amount);
    }

    public void ResetSuspicion()
    {
        Suspicion = 0f;
        detected = false;
        CanSeePlayerNow = false;
    }

    private static readonly List<HumanVision> allVisions = new List<HumanVision>();

    public static float HighestSuspicion
    {
        get
        {
            float highest = 0f;
            for (int i = 0; i < allVisions.Count; i++)
            {
                if (allVisions[i] != null && allVisions[i].Suspicion > highest)
                {
                    highest = allVisions[i].Suspicion;
                }
            }
            return highest;
        }
    }

    private void OnEnable()
    {
        if (!allVisions.Contains(this)) allVisions.Add(this);
    }

    private void OnDisable()
    {
        allVisions.Remove(this);
    }

    private void Awake()
    {
        if (colorRenderer == null) colorRenderer = GetComponentInChildren<Renderer>();
        if (colorRenderer != null) materialInstance = colorRenderer.material;
    }

    private void Start()
    {
        FirstPersonController playerController = FindObjectOfType<FirstPersonController>();
        if (playerController != null)
        {
            player = playerController.transform;
            playerCollider = playerController.GetComponent<Collider>();
            playerCrouch = playerController.GetComponent<PlayerCrouch>();
        }
    }

    private void OnDestroy()
    {
        if (materialInstance != null) Destroy(materialInstance);
    }

    private void Update()
    {
        if (player == null) return;

        bool isCrouched = playerCrouch != null && playerCrouch.IsCrouching;
        bool seen = CanSeePlayer(isCrouched, out float distance);
        CanSeePlayerNow = seen;

        if (seen)
        {
            LastSeenPosition = player.position;
            float distance01 = Mathf.Clamp01(distance / viewDistance);
            float baseDetectionTime = Mathf.Lerp(timeToDetectClose, timeToDetectFar, distance01);

            // Si el jugador está agachado, el tiempo necesario para detectarlo se multiplica
            if (isCrouched)
            {
                baseDetectionTime /= crouchViewMultiplier;
            }

            Suspicion = Mathf.Min(1f, Suspicion + Time.deltaTime / Mathf.Max(0.01f, baseDetectionTime));
        }
        else
        {
            Suspicion = Mathf.Max(0f, Suspicion - decayPerSecond * Time.deltaTime);
        }

        if (!detected && Suspicion >= 1f)
        {
            detected = true;
            OnDetected?.Invoke();
        }
        else if (detected && Suspicion <= 0f)
        {
            detected = false;
            OnDetectionReset?.Invoke();
        }

        UpdateColor();
    }

    private bool CanSeePlayer(bool isCrouched, out float distance)
    {
        Vector3 eye = eyeTransform != null ? eyeTransform.position : transform.TransformPoint(eyeOffset);
        Vector3 target = playerCollider != null ? playerCollider.bounds.center : player.position + targetOffset;

        Vector3 direction = target - eye;
        distance = direction.magnitude;

        float maxDistance = isCrouched ? viewDistance * crouchViewMultiplier : viewDistance;
        float currentProximityRadius = isCrouched ? proximityRadius * crouchViewMultiplier : proximityRadius;

        PlayerConcealment concealment = PlayerConcealment.Instance;
        if (concealment != null)
        {
            if (concealment.SightMultiplier <= 0.001f) return false;
            maxDistance *= concealment.SightMultiplier;
        }

        // 1. Detección por proximidad (agachado reduce el radio a la mitad)
        bool insideProximity = distance <= currentProximityRadius;

        if (!insideProximity)
        {
            if (distance > maxDistance) return false;

            // 2. Comprobación del cono de visión horizontal (XZ)
            Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            Vector3 flatDirection = Vector3.ProjectOnPlane(direction, Vector3.up).normalized;

            if (flatDirection.sqrMagnitude > 0.001f)
            {
                float horizontalAngle = Vector3.Angle(flatForward, flatDirection);
                if (horizontalAngle > viewAngle * 0.5f) return false;
            }
        }

        // 3. Comprobación de visión despejada (Raycast)
        int count = Physics.RaycastNonAlloc(eye, direction.normalized, hitBuffer, distance, obstacleMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Transform hitTransform = hitBuffer[i].transform;

            if (hitTransform == transform || hitTransform.IsChildOf(transform)) continue;
            if (hitTransform == player || hitTransform.IsChildOf(player)) continue;

            // Si hay una pared o cobertura en medio -> No te ve
            return false;
        }

        return true;
    }

    private void UpdateColor()
    {
        if (materialInstance == null) return;

        Color color = Suspicion < 0.5f
            ? Color.Lerp(calmColor, suspiciousColor, Suspicion / 0.5f)
            : Color.Lerp(suspiciousColor, alertColor, (Suspicion - 0.5f) / 0.5f);

        materialInstance.color = color;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 eye = eyeTransform != null ? eyeTransform.position : transform.TransformPoint(eyeOffset);

        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(eye, 0.25f);

        Gizmos.color = new Color(0f, 0.7f, 1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, proximityRadius);

        Gizmos.color = Color.red;
        Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

        if (flatForward.sqrMagnitude > 0.001f)
        {
            float halfAngle = viewAngle * 0.5f;
            Vector3 leftRay = Quaternion.AngleAxis(-halfAngle, Vector3.up) * flatForward;
            Vector3 rightRay = Quaternion.AngleAxis(halfAngle, Vector3.up) * flatForward;

            Gizmos.DrawRay(eye, leftRay * viewDistance);
            Gizmos.DrawRay(eye, rightRay * viewDistance);
            Gizmos.DrawRay(eye, flatForward * viewDistance);
        }

        if (Application.isPlaying && player != null)
        {
            Vector3 target = playerCollider != null ? playerCollider.bounds.center : player.position + targetOffset;
            Gizmos.color = CanSeePlayerNow ? Color.green : Color.gray;
            Gizmos.DrawLine(eye, target);
        }
    }
}