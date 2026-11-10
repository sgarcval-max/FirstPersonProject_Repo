using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Permite aturdir a un guardia (por ejemplo con un disparo de la pistola). Mientras está
/// aturdido se queda parado, no ve ni oye (su barra de detección desaparece), se pone de color
/// azulado, activa el sistema de partículas/VFX de mareo y reproduce un sonido SFX en bucle.
/// Pasado el tiempo se recupera, detiene el sonido y vuelve a patrullar.
/// </summary>
public class HumanStun : MonoBehaviour
{
    [Header("Configuración Visual")]
    [SerializeField] private Color stunColor = new Color(0.4f, 0.6f, 1f, 1f);

    [Header("Efectos de Mareo (VFX Cabeza)")]
    [Tooltip("Sistema de partículas de estrellas colocado sobre la cabeza del enemigo.")]
    [SerializeField] private ParticleSystem dizzyParticles;

    [Tooltip("Opcional: GameObject contenedor del efecto si usas un modelo/animación 3D en la cabeza.")]
    [SerializeField] private GameObject dizzyVFXObject;

    [Header("Efecto de Audio (SFX Mareo)")]
    [Tooltip("Componente AudioSource que reproducirá el sonido (si no se asigna, intentará coger el del objeto).")]
    [SerializeField] private AudioSource stunAudioSource;

    [Tooltip("Clip de audio SFX de mareo/estrellas que sonará en bucle durante el aturdimiento.")]
    [SerializeField] private AudioClip stunSFX;

    /// <summary>True mientras el guardia está aturdido.</summary>
    public bool IsStunned { get; private set; }

    private HumanAI ai;
    private HumanPatrol patrol;
    private HumanVision vision;
    private HumanHearing hearing;
    private NavMeshAgent agent;
    private Renderer bodyRenderer;

    private Color originalColor = Color.white;
    private float timer;

    private void Awake()
    {
        ai = GetComponent<HumanAI>();
        patrol = GetComponent<HumanPatrol>();
        vision = GetComponent<HumanVision>();
        hearing = GetComponent<HumanHearing>();
        agent = GetComponent<NavMeshAgent>();
        bodyRenderer = GetComponentInChildren<Renderer>();

        if (bodyRenderer != null)
        {
            originalColor = bodyRenderer.material.color;
        }

        if (stunAudioSource == null)
        {
            stunAudioSource = GetComponent<AudioSource>();
        }

        // Asegurar que efectos de partículas y audio empiezan desactivados
        StopDizzyEffect();
    }

    /// <summary>Aturde al guardia durante "duration" segundos. Si ya estaba aturdido, reinicia el tiempo.</summary>
    public void Stun(float duration)
    {
        timer = duration;

        // Cancelar persecución en la música al aturdir al enemigo
        if (ai != null && MusicManager.Instance != null)
        {
            MusicManager.Instance.SetChasing(ai, false);
        }

        PlayDizzyEffect();

        if (IsStunned) return;

        IsStunned = true;

        // Cambiar al material de taseado
        if (ai != null)
        {
            ai.SetTasedVisualState(true);
            ai.enabled = false;
        }

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

        // Control dinámico de audio durante la pausa del juego
        if (stunAudioSource != null && stunAudioSource.clip == stunSFX)
        {
            if (Time.timeScale == 0f && stunAudioSource.isPlaying)
            {
                stunAudioSource.Pause();
            }
            else if (Time.timeScale > 0f && !stunAudioSource.isPlaying && timer > 0f)
            {
                stunAudioSource.UnPause();
            }
        }

        // Si el juego está en pausa (Time.timeScale == 0), no descontamos tiempo de aturdimiento
        if (Time.timeScale > 0f)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                Recover();
            }
        }
    }

    private void Recover()
    {
        IsStunned = false;

        // Apagar partículas y cortar el audio SFX
        StopDizzyEffect();

        // Restaurar el color original del material y volver al material normal
        if (bodyRenderer != null)
        {
            bodyRenderer.material.color = originalColor;
        }

        if (vision != null) vision.enabled = true;
        if (hearing != null) hearing.enabled = true;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }

        if (ai != null)
        {
            ai.SetTasedVisualState(false);
            ai.enabled = true;
            ai.ResumePatrol();
        }

        Debug.Log("[" + name + "] Se ha recuperado del aturdimiento.");
    }

    private void PlayDizzyEffect()
    {
        if (dizzyParticles != null && !dizzyParticles.isPlaying)
        {
            dizzyParticles.Play();
        }

        if (dizzyVFXObject != null)
        {
            dizzyVFXObject.SetActive(true);
        }

        if (stunAudioSource != null && stunSFX != null)
        {
            if (!stunAudioSource.isPlaying)
            {
                stunAudioSource.clip = stunSFX;
                stunAudioSource.loop = true;
                stunAudioSource.Play();
            }
        }
    }

    private void StopDizzyEffect()
    {
        if (dizzyParticles != null)
        {
            dizzyParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (dizzyVFXObject != null)
        {
            dizzyVFXObject.SetActive(false);
        }

        if (stunAudioSource != null && stunAudioSource.isPlaying)
        {
            stunAudioSource.Stop();
        }
    }

    private void OnDisable()
    {
        StopDizzyEffect();

        if (ai != null && MusicManager.Instance != null)
        {
            MusicManager.Instance.SetChasing(ai, false);
        }
    }
}