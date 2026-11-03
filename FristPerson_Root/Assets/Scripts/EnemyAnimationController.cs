using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Mantiene la animación de aturdimiento activada en bucle todo el tiempo que dura el efecto del disparo,
/// y regresa a la animación de caminar únicamente cuando finaliza el tiempo de aturdimiento.
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
        // 1. Si está aturdido, descontar tiempo y mantener activado el estado
        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f)
            {
                // El efecto ha terminado: desactivar aturdimiento y volver a andar
                isStunned = false;
                if (animator != null)
                {
                    animator.SetBool("IsStunned", false);
                }
            }
            return;
        }

        // 2. Si no está aturdido, actualizar velocidad normal de navegación
        if (animator != null && agent != null)
        {
            float velocidadActual = agent.velocity.magnitude;
            animator.SetFloat("Speed", velocidadActual);
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
        }
        else
        {
            Debug.LogError("¡OJO! El componente Animator es NULL en EnemyAnimationController.");
        }
    }
}
