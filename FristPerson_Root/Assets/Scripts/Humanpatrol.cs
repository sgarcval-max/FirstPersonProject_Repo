using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Hace que un humano patrulle de forma indefinida entre una lista de puntos, esperando un
/// momento en cada uno. Usa NavMeshAgent, así que esquiva paredes y obstáculos solo.
///
/// Está pensado para poder activarse y desactivarse: más adelante, cuando el humano detecte
/// al mono, desactivaremos este componente para que persiga, y lo volveremos a activar para
/// que retome la patrulla.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class HumanPatrol : MonoBehaviour
{
    [Header("Ruta")]
    [Tooltip("Los puntos por los que pasa, en orden. Al llegar al último vuelve al primero.")]
    [SerializeField] private Transform[] waypoints;

    [Header("Movimiento")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float waitTimeAtWaypoint = 2f;
    [Tooltip("A qué distancia de un punto se considera que ya ha llegado.")]
    [SerializeField] private float arriveDistance = 0.5f;

    private NavMeshAgent agent;
    private int currentIndex;
    private float waitTimer;
    private bool waiting;
    private bool started;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void OnEnable()
    {
        // Al (re)activarse, retoma la ruta desde el punto en el que se había quedado.
        agent.speed = walkSpeed;
        started = false;
        waiting = false;
    }

    private void Update()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        if (!agent.isOnNavMesh) return;

        if (!started)
        {
            started = true;
            MoveToCurrentWaypoint();
            return;
        }

        if (waiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                waiting = false;
                currentIndex = (currentIndex + 1) % waypoints.Length;
                MoveToCurrentWaypoint();
            }
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= arriveDistance)
        {
            waiting = true;
            waitTimer = waitTimeAtWaypoint;
        }
    }

    private void MoveToCurrentWaypoint()
    {
        Transform target = waypoints[currentIndex];
        if (target == null) return;

        agent.isStopped = false;
        agent.SetDestination(target.position);
    }

    // Dibuja la ruta en la vista Scene cuando seleccionas al guardia, para colocar los puntos fácilmente.
    private void OnDrawGizmosSelected()
    {
        if (waypoints == null) return;

        Gizmos.color = Color.yellow;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;

            Gizmos.DrawSphere(waypoints[i].position, 0.2f);

            Transform next = waypoints[(i + 1) % waypoints.Length];
            if (next != null)
            {
                Gizmos.DrawLine(waypoints[i].position, next.position);
            }
        }
    }
}
