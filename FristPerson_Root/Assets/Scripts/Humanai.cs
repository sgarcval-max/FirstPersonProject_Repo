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
///  - Chase: te ha visto del todo; corre hacia ti y, al alcanzarte, te ataca.
///  - Search: te ha perdido de vista; va a donde te vio por última vez y busca por los
///    alrededores. Si te vuelve a ver te persigue al instante; si no, se rinde y patrulla.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class HumanAI : MonoBehaviour
{
    public enum State { Patrol, Investigate, Chase, Search }

    [Header("Velocidades")]
    [SerializeField] private float investigateSpeed = 2.5f;
    [SerializeField] private float chaseSpeed = 5f;
    [SerializeField] private float searchSpeed = 2.5f;

    [Header("Investigar un ruido")]
    [Tooltip("Segundos que se queda mirando alrededor al llegar al sitio del ruido.")]
    [SerializeField] private float lookAroundTime = 3f;
    [Tooltip("Lo rápido que gira sobre sí mismo al mirar alrededor (grados por segundo).")]
    [SerializeField] private float lookAroundTurnSpeed = 60f;

    [Header("Perseguir")]
    [Tooltip("Cada cuántos segundos recalcula el camino hacia el jugador.")]
    [SerializeField] private float repathInterval = 0.2f;
    [Tooltip("Segundos sin ver al jugador tras los que deja de perseguir y pasa a buscar.")]
    [SerializeField] private float chaseMemoryTime = 4f;

    [Header("Atacar")]
    [Tooltip("A qué distancia del jugador empieza a atacar.")]
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackDamage = 15f;
    [Tooltip("Segundos que tarda en dar el primer golpe al llegar hasta el jugador.")]
    [SerializeField] private float attackWindup = 0.5f;
    [Tooltip("Segundos entre un golpe y el siguiente.")]
    [SerializeField] private float attackCooldown = 1.2f;
    [Tooltip("Lo rápido que se gira hacia el jugador mientras ataca.")]
    [SerializeField] private float attackTurnSpeed = 8f;

    [Header("Buscar")]
    [Tooltip("Cuántos puntos revisa en total (el primero es donde te vio por última vez).")]
    [SerializeField] private int searchPointCount = 3;
    [Tooltip("A qué distancia máxima del último punto visto elige los demás sitios.")]
    [SerializeField] private float searchRadius = 5f;
    [Tooltip("Segundos que se queda mirando en cada punto de búsqueda.")]
    [SerializeField] private float searchWaitTime = 2f;

    [Header("Eventos")]
    [Tooltip("Se dispara cada vez que da un golpe (para sonidos o animaciones futuras).")]
    public UnityEvent OnAttack;

    public State CurrentState { get; private set; }

    private NavMeshAgent agent;
    private HumanPatrol patrol;
    private HumanVision vision;
    private HumanHearing hearing;
    private PlayerHealth playerHealth;

    private float repathTimer;
    private float timeSinceSeen;
    private float attackTimer;
    private float stateTimer;
    private bool arrived;
    private bool waiting;
    private int pointsLeft;
    private Vector3 searchCenter;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        patrol = GetComponent<HumanPatrol>();
        vision = GetComponent<HumanVision>();
        hearing = GetComponent<HumanHearing>();

        if (patrol == null || vision == null)
        {
            Debug.LogWarning("HumanAI: este guardia necesita también los componentes Human Patrol y Human Vision.");
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

    private void Start()
    {
        playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogWarning("HumanAI: no se encontró PlayerHealth en la escena.");
        }
    }

    private void Update()
    {
        if (playerHealth == null || vision == null || patrol == null) return;
        if (PlayerHealth.IsPlayerDead) return;
        if (!agent.isOnNavMesh) return;

        switch (CurrentState)
        {
            case State.Patrol: UpdatePatrol(); break;
            case State.Investigate: UpdateInvestigate(); break;
            case State.Chase: UpdateChase(); break;
            case State.Search: UpdateSearch(); break;
        }
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

    private void EnterChase()
    {
        SetState(State.Chase);
        patrol.enabled = false;

        agent.speed = chaseSpeed;
        agent.isStopped = false;

        timeSinceSeen = 0f;
        repathTimer = 0f;
        attackTimer = attackWindup;
    }

    private void EnterSearch(Vector3 center)
    {
        SetState(State.Search);
        patrol.enabled = false;

        agent.speed = searchSpeed;
        searchCenter = center;
        pointsLeft = searchPointCount;

        MoveToNextSearchPoint(true);
    }

    // ------------------------------------------------------------------ Lo que hace en cada estado

    private void UpdatePatrol()
    {
        if (vision.HasDetectedPlayer && vision.CanSeePlayerNow)
        {
            EnterChase();
        }
    }

    private void UpdateInvestigate()
    {
        if (vision.HasDetectedPlayer && vision.CanSeePlayerNow)
        {
            EnterChase();
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

    private void UpdateChase()
    {
        bool sees = vision.CanSeePlayerNow;

        if (sees) timeSinceSeen = 0f;
        else timeSinceSeen += Time.deltaTime;

        Vector3 playerPosition = playerHealth.transform.position;
        Vector3 toPlayer = playerPosition - transform.position;
        toPlayer.y = 0f;

        // Si está lo bastante cerca y lo ve, deja de moverse y ataca.
        if (sees && toPlayer.magnitude <= attackRange)
        {
            agent.isStopped = true;
            FaceTowards(toPlayer);

            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f)
            {
                attackTimer = attackCooldown;
                playerHealth.TakeDamage(attackDamage);
                if (OnAttack != null) OnAttack.Invoke();
            }
            return;
        }

        // Si no, sigue corriendo: hacia el jugador si lo ve, o hacia donde lo vio por última vez.
        agent.isStopped = false;
        attackTimer = attackWindup;

        repathTimer -= Time.deltaTime;
        if (repathTimer <= 0f)
        {
            repathTimer = repathInterval;
            Vector3 target = sees ? playerPosition : vision.LastSeenPosition;
            agent.SetDestination(SnapToNavMesh(target));
        }

        if (!sees && (timeSinceSeen > chaseMemoryTime || HasArrived()))
        {
            EnterSearch(vision.LastSeenPosition);
        }
    }

    private void UpdateSearch()
    {
        // Ya está en alerta: si te ve, te persigue al instante, sin esperar a sospechar.
        if (vision.CanSeePlayerNow)
        {
            EnterChase();
            return;
        }

        if (waiting)
        {
            stateTimer -= Time.deltaTime;
            transform.Rotate(0f, lookAroundTurnSpeed * Time.deltaTime, 0f);

            if (stateTimer <= 0f)
            {
                if (pointsLeft <= 0) EnterPatrol();
                else MoveToNextSearchPoint(false);
            }
            return;
        }

        if (HasArrived())
        {
            waiting = true;
            stateTimer = searchWaitTime;
            agent.isStopped = true;
        }
    }

    // ------------------------------------------------------------------ Auxiliares

    private void HandleNoiseHeard(Vector3 position)
    {
        if (PlayerHealth.IsPlayerDead) return;

        // Si ya está persiguiendo o buscando, un ruido no le cambia nada: ya está en alerta.
        if (CurrentState == State.Patrol || CurrentState == State.Investigate)
        {
            EnterInvestigate(position);
        }
    }

    private void MoveToNextSearchPoint(bool first)
    {
        pointsLeft--;
        waiting = false;

        Vector3 target = searchCenter;
        if (!first)
        {
            Vector2 offset = Random.insideUnitCircle * searchRadius;
            target = searchCenter + new Vector3(offset.x, 0f, offset.y);
        }

        agent.isStopped = false;
        agent.SetDestination(SnapToNavMesh(target));
    }

    private bool HasArrived()
    {
        return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f;
    }

    private void FaceTowards(Vector3 flatDirection)
    {
        if (flatDirection.sqrMagnitude < 0.001f) return;

        Quaternion target = Quaternion.LookRotation(flatDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, attackTurnSpeed * Time.deltaTime);
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
