using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Permite aturdir a un guardia (por ejemplo con un disparo de la pistola). Mientras está
/// aturdido se queda parado, no ve ni oye (su barra de detección desaparece) y se pone de color
/// azulado. Pasado el tiempo se recupera y vuelve a patrullar.
/// HumanAI se lo añade solo a cada guardia: no hay que montar nada en la escena.
/// </summary>
public class HumanStun : MonoBehaviour
{
    [SerializeField] private Color stunColor = new Color(0.4f, 0.6f, 1f, 1f);

    /// <summary>True mientras el guardia está aturdido.</summary>
    public bool IsStunned { get; private set; }

    private HumanAI ai;
    private HumanPatrol patrol;
    private HumanVision vision;
    private HumanHearing hearing;
    private NavMeshAgent agent;
    private Renderer bodyRenderer;
    private float timer;

    private void Awake()
    {
        ai = GetComponent<HumanAI>();
        patrol = GetComponent<HumanPatrol>();
        vision = GetComponent<HumanVision>();
        hearing = GetComponent<HumanHearing>();
        agent = GetComponent<NavMeshAgent>();
        bodyRenderer = GetComponentInChildren<Renderer>();
    }

    /// <summary>Aturde al guardia durante "duration" segundos. Si ya estaba aturdido, reinicia el tiempo.</summary>
    public void Stun(float duration)
    {
        timer = duration;
        if (IsStunned) return;

        IsStunned = true;

        // Se apaga todo lo que le hace actuar, ver y oír.
        if (ai != null) ai.enabled = false;
        if (patrol != null) patrol.enabled = false;
        if (hearing != null) hearing.enabled = false;
        if (vision != null)
        {
            vision.ResetSuspicion();
            vision.enabled = false;
        }

        if (agent != null && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }

        if (bodyRenderer != null)
        {
            bodyRenderer.material.color = stunColor;
        }

        Debug.Log("[" + name + "] Aturdido durante " + duration + " segundos.");
    }

    /// <summary>Lo recupera al instante (se usa al reaparecer el jugador tras un Game Over).</summary>
    public void RecoverImmediately()
    {
        if (IsStunned)
        {
            Recover();
        }
    }

    private void Update()
    {
        if (!IsStunned) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            Recover();
        }
    }

    private void Recover()
    {
        IsStunned = false;

        if (vision != null) vision.enabled = true;
        if (hearing != null) hearing.enabled = true;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }

        if (ai != null)
        {
            ai.enabled = true;
            ai.ResumePatrol();
        }

        Debug.Log("[" + name + "] Se ha recuperado del aturdimiento.");
    }
}