using UnityEngine;

/// <summary>
/// Sistema avanzado de sonido físico 3D para vajillas y objetos del entorno.
/// Soporta variación aleatoria de clips, modulación de pitch/volumen por fuerza,
/// y diferenciación entre impactos objeto-objeto y objeto-suelo.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
public class PropImpactAudio : MonoBehaviour
{
    [Header("1. Sonidos entre Objetos (Vaso con Vaso / Plato)")]
    [Tooltip("Lista de los 5 o 6 audios de impacto entre objetos de vajilla.")]
    [SerializeField] private AudioClip[] propImpactClips;

    [Header("2. Sonidos de Impacto con el Suelo / Entorno")]
    [Tooltip("Sonidos de impacto contra el suelo, mesas o paredes. Si se deja vacío, usará los sonidos de vajilla.")]
    [SerializeField] private AudioClip[] environmentImpactClips;

    [Header("3. Ajustes de Variación y Tono (Pitch)")]
    [Range(0.8f, 1.0f)][SerializeField] private float minPitch = 0.9f;
    [Range(1.0f, 1.2f)][SerializeField] private float maxPitch = 1.1f;

    [Header("4. Físicas y Volumen")]
    [Tooltip("Fuerza mínima (velocidad) para que empiece a sonar. Evita ruidos por pequeños roces.")]
    [SerializeField] private float minImpactForce = 1.2f;
    [Tooltip("Fuerza para alcanzar el volumen máximo.")]
    [SerializeField] private float maxImpactForce = 7.0f;
    [Range(0f, 1f)][SerializeField] private float minVolume = 0.15f;
    [Range(0f, 1f)][SerializeField] private float maxVolume = 1.0f;

    [Header("5. Atenuación 3D en el Espacio")]
    [SerializeField] private float soundMinDistance = 0.8f;
    [SerializeField] private float soundMaxDistance = 12f;

    private AudioSource audioSource;
    private float lastSoundTime;
    private const float SOUND_COOLDOWN = 0.06f; // Previene spam en rodaduras/rebotes continuos

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        Configure3DAudioSource();
    }

    private void Configure3DAudioSource()
    {
        if (audioSource == null) return;

        audioSource.spatialBlend = 1.0f; // 100% Sonido Posicional 3D
        audioSource.playOnAwake = false;
        audioSource.minDistance = soundMinDistance;
        audioSource.maxDistance = soundMaxDistance;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // 1. Calcular la velocidad del impacto
        float impactForce = collision.relativeVelocity.magnitude;

        // Ignorar roces mínimos o impactos insignificantes
        if (impactForce < minImpactForce) return;

        // Prevenir saturación por múltiples contactos en el mismo milisegundo
        if (Time.time - lastSoundTime < SOUND_COOLDOWN) return;

        // 2. Comprobar qué tipo de objeto hemos tocado
        PropImpactAudio otherProp = collision.gameObject.GetComponent<PropImpactAudio>();

        // Si chocan dos vajillas a la vez, se usa el ID para que SOLO UNO dispare el sonido en ese frame
        if (otherProp != null && GetInstanceID() > collision.gameObject.GetInstanceID())
        {
            return;
        }

        lastSoundTime = Time.time;

        // 3. Seleccionar la lista de clips adecuada
        AudioClip[] targetClips = propImpactClips;

        // Si choca contra el suelo/pared (objeto sin PropImpactAudio) y tenemos audios de entorno asignados
        if (otherProp == null && environmentImpactClips != null && environmentImpactClips.Length > 0)
        {
            targetClips = environmentImpactClips;
        }

        if (targetClips == null || targetClips.Length == 0) return;

        // 4. Elegir un audio aleatorio de la lista
        AudioClip chosenClip = targetClips[Random.Range(0, targetClips.Length)];

        // 5. Modular volumen y pitch según la fuerza del choque
        float forceRatio = Mathf.InverseLerp(minImpactForce, maxImpactForce, impactForce);
        float calculatedVolume = Mathf.Lerp(minVolume, maxVolume, forceRatio);

        // A mayor fuerza, el tono es ligeramente más agudo
        float calculatedPitch = Mathf.Lerp(minPitch, maxPitch, forceRatio) + Random.Range(-0.03f, 0.03f);

        // 6. Reproducir el efecto de sonido
        audioSource.pitch = calculatedPitch;
        audioSource.PlayOneShot(chosenClip, calculatedVolume);
    }
}
