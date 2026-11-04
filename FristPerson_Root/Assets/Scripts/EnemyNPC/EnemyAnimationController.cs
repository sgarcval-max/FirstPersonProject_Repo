using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Controla las transiciones entre Walk, Idle y Hit.
/// Detecta de forma precisa la parada en puntos de patrulla para activar Idle sin retardo.
/// </summary>
public class EnemyAnimationController : MonoBehaviour
{
    private Animator animator;
    private NavMeshAgent agent;

    private bool isStunned = false;
    private float stunTimer = 0f;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        // 1. Temporizador de aturdimiento
        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f)
            {
                isStunned = false;
                if (animator != null)
                {
                    animator.SetBool("IsStunned", false);
                }
            }
        }

        // 2. Control de velocidad preciso para Idle y Walk
        if (animator != null)
        {
            float velocidadAnimacion = 0f;

            if (!isStunned && agent != null)
            {
                // Solo consideramos que camina si el agente NO está detenido, tiene ruta y supera la velocidad mínima
                bool estaCaminando = !agent.isStopped && agent.hasPath && agent.velocity.sqrMagnitude > 0.05f;

                if (estaCaminando)
                {
                    velocidadAnimacion = agent.velocity.magnitude;
                }
            }

            animator.SetFloat("Speed", velocidadAnimacion);
        }
    }

    /// <summary>
    /// Inicia el bucle de animación de aturdimiento durante los segundos de duración del arma.
    /// </summary>
    public void RecibirDisparo(float duration)
    {
        Debug.Log("¡IMPACTO RECIBIDO EN EL ENEMIGO! Duración: " + duration);
        isStunned = true;
        stunTimer = duration;

        if (animator != null)
        {
            animator.SetBool("IsStunned", true);
            animator.SetFloat("Speed", 0f);
        }
    }
}