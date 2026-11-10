using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

/// <summary>
/// Cerebro del guardia: decide qué hacer según lo que ve y oye, usando los componentes que ya
/// tiene (HumanPatrol, HumanVision y HumanHearing).
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class HumanAI : MonoBehaviour
{
    public enum State { Patrol, Investigate, Catch }

    [Header("Materiales de Estado (Normal vs Taseado)")]
    [Tooltip("El Renderer (MeshRenderer o SkinnedMeshRenderer) del personaje.")]
    [SerializeField] private Renderer npcRenderer;

    [Tooltip("Material del NPC en estado normal. Si se deja vacío, guardará el material inicial.")]
    [SerializeField] private Material normalMaterial;

    [Tooltip("Material del NPC mientras está taseado / mareado.")]
    [SerializeField] private Material tasedMaterial;

    [Header("Investigar un ruido")]
    [SerializeField] private float investigateSpeed = 2.5f;
    [Tooltip("Segundos que se queda mirando alrededor al llegar al sitio del ruido.")]
    [SerializeField] private float lookAroundTime = 3f;
    [Tooltip("Lo rápido que gira sobre sí mismo al mirar alrededor (grados por segundo).")]
    [SerializeField] private float lookAroundTurnSpeed = 60f;
    [Tooltip("Tiempo máximo en segundos que el guardia intentará caminar hacia el ruido antes de rendirse si la ruta está bloqueada por obstáculos.")]
    [SerializeField] private float maxInvestigateWalkTime = 6f;

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
    private float investigateWalkTimer;
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

        if (vision != null && GetComponent<DetectionBar>() == null)
        {
            gameObject.AddComponent<DetectionBar>();
        }

        if (GetComponent<HumanStun>() == null)
        {
            gameObject.AddComponent<HumanStun>();
        }

        if (npcRenderer == null)
        {
            npcRenderer = GetComponentInChildren<Renderer>();
        }

        if (npcRenderer != null && normalMaterial == null)
        {
            normalMaterial = npcRenderer.sharedMaterial;
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

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.SetChasing(this, false);
        }
    }

    /// <summary>
    /// Cambia el material visual del NPC entre el estado normal y taseado.
    /// </summary>
    public void SetTasedVisualState(bool isTased)
    {
        if (npcRenderer == null) npcRenderer = GetComponentInChildren<Renderer>();
        if (npcRenderer == null) return;

        Material targetMat = isTased ? tasedMaterial : normalMaterial;
        if (targetMat != null)
        {
            npcRenderer.material = targetMat;
        }
    }

    /// <summary>
    /// Método puente para aturdir al NPC desde el Taser.
    /// </summary>
    public void ApplyTaserStun(float duration = 5f)
    {
        HumanStun stun = GetComponent<HumanStun>();
        if (stun != null)
        {
            stun.Stun(duration);
        }
    }

    private void Update()
    {
        if (vision == null || patrol == null) return;
        if (!agent.isOnNavMesh) return;

        UpdateMusicChasingState();

        if (TutorialManager.IsPlayerProtected)
        {
            if (CurrentState != State.Patrol)
            {
                EnterPatrol();
            }
            return;
        }

        if (PlayerCapture.IsCaught && CurrentState != State.Catch) return;

        switch (CurrentState)
        {
            case State.Patrol: UpdatePatrol(); break;
            case State.Investigate: UpdateInvestigate(); break;
            case State.Catch: UpdateCatch(); break;
        }
    }

    private void UpdateMusicChasingState()
    {
        if (MusicManager.Instance == null) return;

        bool isTense = (CurrentState == State.Investigate) ||
                       (CurrentState == State.Catch) ||
                       (vision != null && vision.Suspicion > 0.01f);

        MusicManager.Instance.SetChasing(this, isTense);
    }

    public void ResetToStart()
    {
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.SetChasing(this, false);
        }

        SetTasedVisualState(false);

        HumanStun stun = GetComponent<HumanStun>();
        if (stun != null) stun.RecoverImmediately();

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

        patrol.enabled = false;
        EnterPatrol();
    }

    public void ResetForRespawn(Vector3 playerSpawnPoint, float safeDistance = 5f)
    {
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.SetChasing(this, false);
        }

        if (Vector3.Distance(transform.position, playerSpawnPoint) < safeDistance)
        {
            ResetToStart();
            return;
        }

        HumanStun stun = GetComponent<HumanStun>();
        if (stun != null && stun.IsStunned) return;

        if (vision != null) vision.ResetSuspicion();
        if (hearing != null) hearing.HasHeardNoise = false;

        patrol.enabled = false;
        EnterPatrol();
    }

    public void ResumePatrol()
    {
        EnterPatrol();
    }

    private void SetState(State newState)
    {
        CurrentState = newState;
    }

    private void EnterPatrol()
    {
        SetState(State.Patrol);

        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.isStopped = false;
        }

        if (patrol != null)
        {
            patrol.enabled = false;
            patrol.enabled = true;
        }
    }

    private void EnterInvestigate(Vector3 noisePosition)
    {
        if (TutorialManager.IsPlayerProtected) return;

        SetState(State.Investigate);

        if (patrol != null)
        {
            patrol.enabled = false;
        }

        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.speed = investigateSpeed;
            agent.isStopped = false;
            agent.SetDestination(SnapToNavMesh(noisePosition));
        }

        arrived = false;
        investigateWalkTimer = 0f;
    }

    private void BeginCatch()
    {
        if (TutorialManager.IsPlayerProtected || PlayerCapture.Instance == null || PlayerCapture.IsCaught) return;

        SetState(State.Catch);

        if (patrol != null)
        {
            patrol.enabled = false;
        }

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }

        PlayerCapture.Instance.BeginCapture(transform);

        if (OnCaughtPlayer != null) OnCaughtPlayer.Invoke();
    }

    private void UpdatePatrol()
    {
        if (!TutorialManager.IsPlayerProtected && vision.HasDetectedPlayer)
        {
            BeginCatch();
            return;
        }

        if (agent != null && agent.isOnNavMesh && agent.isStopped)
        {
            agent.isStopped = false;
        }
    }

    private void UpdateInvestigate()
    {
        if (!TutorialManager.IsPlayerProtected && vision.HasDetectedPlayer)
        {
            BeginCatch();
            return;
        }

        if (!arrived)
        {
            investigateWalkTimer += Time.deltaTime;

            if (HasArrived())
            {
                arrived = true;
                stateTimer = lookAroundTime;

                if (agent != null && agent.isOnNavMesh)
                {
                    agent.isStopped = true;
                    agent.ResetPath();
                }

                if (hearing != null)
                {
                    hearing.HasHeardNoise = false;
                }
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

        Vector3 toPlayer = PlayerCapture.Instance.transform.position - transform.position;
        toPlayer.y = 0f;
        FaceTowards(toPlayer);
    }

    private void HandleNoiseHeard(Vector3 position)
    {
        if (TutorialManager.IsPlayerProtected || PlayerCapture.IsCaught) return;

        if (CurrentState == State.Patrol || CurrentState == State.Investigate)
        {
            EnterInvestigate(position);
        }
    }

    private bool HasArrived()
    {
        if (agent == null || !agent.isOnNavMesh) return true;
        if (agent.pathPending) return false;

        if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            return true;
        }

        if (agent.remainingDistance <= agent.stoppingDistance + 0.5f)
        {
            return true;
        }

        if (agent.pathStatus == NavMeshPathStatus.PathPartial && agent.remainingDistance <= agent.stoppingDistance + 1.5f)
        {
            return true;
        }

        if (investigateWalkTimer > 0.6f &&
            agent.remainingDistance <= agent.stoppingDistance + 1.8f &&
            agent.velocity.sqrMagnitude < 0.05f)
        {
            return true;
        }

        if (investigateWalkTimer >= maxInvestigateWalkTime)
        {
            return true;
        }

        return false;
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
        if (NavMesh.SamplePosition(position, out hit, 4.0f, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return position;
    }
}