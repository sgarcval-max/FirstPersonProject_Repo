using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

/// <summary>
/// Cerebro del guardia: decide qué hacer según lo que ve y oye, usando los componentes que ya
/// tiene (HumanPatrol, HumanVision y HumanHearing).
///
/// Estados:
///  - Patrol: patrulla su ruta (lo hace HumanPatrol).
///  - Investigate: ha oído un ruido; va hasta allí, mira alrededor y vuelve a patrullar.
///  - Catch: su barra de detección ha llegado al máximo. Se para, se gira hacia el jugador y lo
///    atrapa (PlayerCapture se encarga del Game Over).
///
/// No hay persecución: en cuanto te detecta del todo, pierdes, estés lejos o cerca.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class HumanAI : MonoBehaviour
{
    public enum State { Patrol, Investigate, Catch }

    [Header("Investigar un ruido")]
    [SerializeField] private float investigateSpeed = 2.5f;
    [Tooltip("Segundos que se queda mirando alrededor al llegar al sitio del ruido.")]
    [SerializeField] private float lookAroundTime = 3f;
    [Tooltip("Lo rápido que gira sobre sí mismo al mirar alrededor (grados por segundo).")]
    [SerializeField] private float lookAroundTurnSpeed = 60f;

    [Header("Atrapar")]
    [Tooltip("Lo rápido que se da la vuelta hacia el jugador al atraparlo.")]
    [SerializeField] private float catchTurnSpeed = 8f;

    [Header("Eventos")]
    [Tooltip("Se dispara cuando este guardia te atrapa (para sonidos o animaciones futuras).")]
    public UnityEvent OnCaughtPlayer;

    public State CurrentState { get; private set; }

    private NavMeshAgent agent;
    private HumanPatrol patrol;
    private HumanVision vision;
    private HumanHearing hearing;

    private float stateTimer;
    private bool arrived;
    private Vector3 startPosition;
    private Quaternion startRotation;

    private void Awake()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;

        agent = GetComponent<NavMeshAgent>();
        patrol = GetComponent<HumanPatrol>();
        vision = GetComponent<HumanVision>();
        hearing = GetComponent<HumanHearing>();

        if (patrol == null || vision == null)
        {
            Debug.LogWarning("HumanAI: este guardia necesita también los componentes Human Patrol y Human Vision.");
        }

        // La barra flotante encima de la cabeza se la añadimos automáticamente a cada guardia.
        if (vision != null && GetComponent<DetectionBar>() == null)
        {
            gameObject.AddComponent<DetectionBar>();
        }

        CurrentState = State.Patrol;
    }

    private void OnEnable()
    {
        if (hearing != null && hearing.OnNoiseHeard != null)
        {
            hearing.OnNoiseHeard.AddListener(HandleNoiseHeard);
        }
    }

    private void OnDisable()
    {
        if (hearing != null && hearing.OnNoiseHeard != null)
        {
            hearing.OnNoiseHeard.RemoveListener(HandleNoiseHeard);
        }
    }

    private void Update()
    {
        if (vision == null || patrol == null) return;
        if (!agent.isOnNavMesh) return;

        // Si otro guardia ya ha atrapado al jugador, este se queda como estaba.
        if (PlayerCapture.IsCaught && CurrentState != State.Catch) return;

        switch (CurrentState)
        {
            case State.Patrol: UpdatePatrol(); break;
            case State.Investigate: UpdateInvestigate(); break;
            case State.Catch: UpdateCatch(); break;
        }
    }

    /// <summary>
    /// Devuelve al guardia a su sitio inicial, tranquilo y patrullando desde cero. Se usa cuando
    /// el jugador reaparece tras un Game Over.
    /// </summary>
    public void ResetToStart()
    {
        if (agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.Warp(startPosition);
        }
        else
        {
            transform.position = startPosition;
        }

        transform.rotation = startRotation;
        agent.isStopped = false;

        if (vision != null) vision.ResetSuspicion();
        if (hearing != null) hearing.HasHeardNoise = false;

        // Desactivar y volver a activar la patrulla hace que empiece su ruta de nuevo.
        patrol.enabled = false;
        EnterPatrol();
    }

    // ------------------------------------------------------------------ Cambios de estado

    private void SetState(State newState)
    {
        CurrentState = newState;
        Debug.Log("[" + name + "] Estado: " + newState);
    }

    private void EnterPatrol()
    {
        SetState(State.Patrol);
        patrol.enabled = true; // HumanPatrol recupera su velocidad y retoma la ruta al activarse
    }

    private void EnterInvestigate(Vector3 noisePosition)
    {
        SetState(State.Investigate);
        patrol.enabled = false;

        agent.speed = investigateSpeed;
        agent.isStopped = false;
        agent.SetDestination(SnapToNavMesh(noisePosition));

        arrived = false;
    }

    private void BeginCatch()
    {
        if (PlayerCapture.Instance == null || PlayerCapture.IsCaught) return;

        SetState(State.Catch);
        patrol.enabled = false;
        agent.isStopped = true;

        PlayerCapture.Instance.BeginCapture(transform);

        if (OnCaughtPlayer != null) OnCaughtPlayer.Invoke();
    }

    // ------------------------------------------------------------------ Lo que hace en cada estado

    private void UpdatePatrol()
    {
        if (vision.HasDetectedPlayer)
        {
            BeginCatch();
        }
    }

    private void UpdateInvestigate()
    {
        if (vision.HasDetectedPlayer)
        {
            BeginCatch();
            return;
        }

        if (!arrived)
        {
            if (HasArrived())
            {
                arrived = true;
                stateTimer = lookAroundTime;
                agent.isStopped = true;
            }
            return;
        }

        stateTimer -= Time.deltaTime;
        transform.Rotate(0f, lookAroundTurnSpeed * Time.deltaTime, 0f);

        if (stateTimer <= 0f)
        {
            EnterPatrol();
        }
    }

    private void UpdateCatch()
    {
        if (PlayerCapture.Instance == null) return;

        // Se da la vuelta hacia el jugador mientras la cámara de este gira hacia él.
        Vector3 toPlayer = PlayerCapture.Instance.transform.position - transform.position;
        toPlayer.y = 0f;
        FaceTowards(toPlayer);
    }

    // ------------------------------------------------------------------ Auxiliares

    private void HandleNoiseHeard(Vector3 position)
    {
        if (PlayerCapture.IsCaught) return;

        // Solo reacciona a un ruido si está tranquilo; si ya está capturando, no le afecta.
        if (CurrentState == State.Patrol || CurrentState == State.Investigate)
        {
            EnterInvestigate(position);
        }
    }

    private bool HasArrived()
    {
        return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f;
    }

    private void FaceTowards(Vector3 flatDirection)
    {
        if (flatDirection.sqrMagnitude < 0.001f) return;

        Quaternion target = Quaternion.LookRotation(flatDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, catchTurnSpeed * Time.deltaTime);
    }

    private Vector3 SnapToNavMesh(Vector3 position)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(position, out hit, 3f, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return position;
    }
}
