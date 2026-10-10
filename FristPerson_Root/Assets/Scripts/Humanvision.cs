using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Da visión a un humano: ve al jugador si está dentro de su cono de visión y no hay nada
/// (paredes, objetos) entre los dos. Mientras lo ve, una barra de sospecha (0 a 1) se va
/// llenando, más rápido cuanto más cerca esté el jugador. Al llegar a 1 el humano lo ha
/// detectado. Cuando deja de verlo, la sospecha baja sola.
///
/// De momento el aviso es solo visual (el humano cambia de color) y por eventos, para que
/// más adelante la IA pueda reaccionar (investigar, perseguir...).
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
    [Tooltip("Qué punto del jugador intenta ver (relativo a los pies del jugador).")]
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1f, 0f);
    [Tooltip("Qué capas pueden bloquear la visión.")]
    [SerializeField] private LayerMask obstacleMask = ~0;

    [Header("Sospecha")]
    [Tooltip("Segundos que tarda en detectarte si estás pegado a él.")]
    [SerializeField] private float timeToDetectClose = 1f;
    [Tooltip("Segundos que tarda en detectarte si estás en el límite de su visión.")]
    [SerializeField] private float timeToDetectFar = 3f;
    [Tooltip("Cuánta sospecha pierde por segundo cuando no te ve.")]
    [SerializeField] private float decayPerSecond = 0.5f;

    [Header("Aviso visual (provisional)")]
    [Tooltip("El modelo que cambia de color. Si se deja vacío, usa el primero que encuentre.")]
    [SerializeField] private Renderer colorRenderer;
    [SerializeField] private Color calmColor = Color.white;
    [SerializeField] private Color suspiciousColor = Color.yellow;
    [SerializeField] private Color alertColor = Color.red;

    [Header("Eventos")]
    [Tooltip("Se dispara cuando la sospecha llega al máximo: te ha detectado.")]
    public UnityEvent OnDetected;
    [Tooltip("Se dispara cuando, tras haberte detectado, la sospecha vuelve a 0.")]
    public UnityEvent OnDetectionReset;

    private Transform player;
    private Material materialInstance;
    private readonly RaycastHit[] hitBuffer = new RaycastHit[16];
    private bool detected;

    /// <summary>Nivel de sospecha actual, de 0 (tranquilo) a 1 (te ha detectado).</summary>
    public float Suspicion { get; private set; }

    /// <summary>True si, en este mismo momento, el humano está viendo al jugador.</summary>
    public bool CanSeePlayerNow { get; private set; }

    /// <summary>El último sitio donde el humano vio al jugador.</summary>
    public Vector3 LastSeenPosition { get; private set; }

    public bool HasDetectedPlayer
    {
        get { return detected; }
    }

    /// <summary>
    /// Sube la sospecha desde fuera (por ejemplo, al oír un ruido), pero sin pasar de "maxLevel".
    /// Así un ruido puede poner nervioso al humano, pero no detectarte del todo: para eso tiene que verte.
    /// </summary>
    public void AddSuspicion(float amount, float maxLevel)
    {
        if (Suspicion >= maxLevel) return;
        Suspicion = Mathf.Min(maxLevel, Suspicion + amount);
    }

    private void Awake()
    {
        if (colorRenderer == null)
        {
            colorRenderer = GetComponentInChildren<Renderer>();
        }
        if (colorRenderer != null)
        {
            materialInstance = colorRenderer.material;
        }
    }

    private void Start()
    {
        FirstPersonController playerController = FindObjectOfType<FirstPersonController>();
        if (playerController != null)
        {
            player = playerController.transform;
        }
        else
        {
            Debug.LogWarning("HumanVision: no se encontró al jugador en la escena.");
        }
    }

    private void OnDestroy()
    {
        if (materialInstance != null)
        {
            Destroy(materialInstance);
        }
    }

    private void Update()
    {
        if (player == null) return;

        float distance;
        bool seen = CanSeePlayer(out distance);

        CanSeePlayerNow = seen;
        if (seen)
        {
            LastSeenPosition = player.position;
        }

        if (seen)
        {
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
            Debug.Log("[" + name + "] ¡Te ha visto!");
            if (OnDetected != null) OnDetected.Invoke();
        }
        else if (detected && Suspicion <= 0f)
        {
            detected = false;
            Debug.Log("[" + name + "] Ha perdido el rastro.");
            if (OnDetectionReset != null) OnDetectionReset.Invoke();
        }

        UpdateColor();
    }

    private bool CanSeePlayer(out float distance)
    {
        Vector3 eye = transform.TransformPoint(eyeOffset);
        Vector3 target = player.position + targetOffset;
        Vector3 direction = target - eye;
        distance = direction.magnitude;

        if (distance > viewDistance) return false;
        if (Vector3.Angle(transform.forward, direction) > viewAngle * 0.5f) return false;

        // Lanzamos un rayo hacia el jugador: lo primero que choque (ignorando el propio cuerpo
        // del humano) decide si lo ve o hay algo tapándolo.
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

        Color color;
        if (Suspicion < 0.5f)
        {
            color = Color.Lerp(calmColor, suspiciousColor, Suspicion / 0.5f);
        }
        else
        {
            color = Color.Lerp(suspiciousColor, alertColor, (Suspicion - 0.5f) / 0.5f);
        }

        materialInstance.color = color;
    }

    // Dibuja el cono de visión en la vista Scene cuando seleccionas al humano.
    private void OnDrawGizmosSelected()
    {
        Vector3 eye = transform.TransformPoint(eyeOffset);
        float half = viewAngle * 0.5f;

        Gizmos.color = Color.cyan;

        Vector3 left = Quaternion.AngleAxis(-half, Vector3.up) * transform.forward;
        Vector3 right = Quaternion.AngleAxis(half, Vector3.up) * transform.forward;

        Gizmos.DrawLine(eye, eye + left * viewDistance);
        Gizmos.DrawLine(eye, eye + right * viewDistance);

        int segments = 20;
        Vector3 previous = eye + left * viewDistance;
        for (int i = 1; i <= segments; i++)
        {
            float angle = Mathf.Lerp(-half, half, i / (float)segments);
            Vector3 point = eye + Quaternion.AngleAxis(angle, Vector3.up) * transform.forward * viewDistance;
            Gizmos.DrawLine(previous, point);
            previous = point;
        }
    }
}