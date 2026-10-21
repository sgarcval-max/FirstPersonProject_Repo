using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Da visión a un humano: ve al jugador dentro de su cono de visión o por proximidad directa.
/// </summary>
public class HumanVision : MonoBehaviour
{
    [Header("Visión")]
    [Tooltip("Posición de los ojos respecto al centro del humano.")]
    [SerializeField] private Vector3 eyeOffset = new Vector3(0f, 0.6f, 0f);
    [SerializeField] private float viewDistance = 10f;
    [Tooltip("Ángulo total del cono de visión, en grados.")]
    [Range(10f, 360f)]
    [SerializeField] private float viewAngle = 100f;

    [Tooltip("Radio de presencia inmediata: si el mono está a esta distancia o menos de las piernas del guardia, lo detecta aunque esté fuera del cono visual.")]
    [SerializeField] private float proximityRadius = 1.2f;

    [Tooltip("Qué punto del jugador intenta ver (si no se encuentra colisionador).")]
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 0.3f, 0f);
    [Tooltip("Cuando el jugador está agachado, el guardia solo lo ve a esta fracción de su distancia de visión (0.6 = un 60 %).")]
    [Range(0.1f, 1f)]
    [SerializeField] private float crouchViewMultiplier = 0.6f;
    [Tooltip("Qué capas pueden bloquear la visión.")]
    [SerializeField] private LayerMask obstacleMask = ~0;

    [Header("Sospecha")]
    [SerializeField] private float timeToDetectClose = 1f;
    [SerializeField] private float timeToDetectFar = 3f;
    [SerializeField] private float decayPerSecond = 0.5f;

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
    private readonly RaycastHit[] hitBuffer = new RaycastHit[16];
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

        bool seen = CanSeePlayer(out float distance);
        CanSeePlayerNow = seen;

        if (seen)
        {
            LastSeenPosition = player.position;
            float distance01 = Mathf.Clamp01(distance / viewDistance);
            float secondsToFill = Mathf.Lerp(timeToDetectClose, timeToDetectFar, distance01);
            Suspicion = Mathf.Min(1f, Suspicion + Time.deltaTime / Mathf.Max(0.01f, secondsToFill));
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

    private bool CanSeePlayer(out float distance)
    {
        Vector3 eye = transform.TransformPoint(eyeOffset);
        bool crouched = playerCrouch != null && playerCrouch.IsCrouching;

        Vector3 target = playerCollider != null ? playerCollider.bounds.center : player.position + targetOffset;
        float maxDistance = crouched ? viewDistance * crouchViewMultiplier : viewDistance;

        Vector3 direction = target - eye;
        distance = direction.magnitude;

        PlayerConcealment concealment = PlayerConcealment.Instance;
        if (concealment != null)
        {
            if (concealment.SightMultiplier <= 0.001f) return false;
            maxDistance *= concealment.SightMultiplier;
        }

        // DETECCIÓN POR PROXIMIDAD: Si está pegado a sus pies, ignora el ángulo del cono
        bool insideProximity = distance <= proximityRadius;

        if (!insideProximity)
        {
            if (distance > maxDistance) return false;
            if (Vector3.Angle(transform.forward, direction) > viewAngle * 0.5f) return false;
        }

        // Comprobación de paredes u obstáculos
        int count = Physics.RaycastNonAlloc(eye, direction.normalized, hitBuffer, distance,
                                            obstacleMask, QueryTriggerInteraction.Ignore);

        float nearestDistance = float.MaxValue;
        Transform nearest = null;

        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = hitBuffer[i];
            if (hit.transform.IsChildOf(transform)) continue;

            if (hit.distance < nearestDistance)
            {
                nearestDistance = hit.distance;
                nearest = hit.transform;
            }
        }

        if (nearest == null) return false;
        return nearest == player || nearest.IsChildOf(player);
    }

    private void UpdateColor()
    {
        if (materialInstance == null) return;

        Color color = Suspicion < 0.5f
            ? Color.Lerp(calmColor, suspiciousColor, Suspicion / 0.5f)
            : Color.Lerp(suspiciousColor, alertColor, (Suspicion - 0.5f) / 0.5f);

        materialInstance.color = color;
    }
}