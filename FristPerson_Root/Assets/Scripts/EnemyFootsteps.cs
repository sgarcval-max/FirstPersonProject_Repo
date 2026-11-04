using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestiona la reproducción de pisadas en 3D para el enemigo.
/// Las funciones de este script son invocadas por Animation Events en los clips de animación.
/// </summary>
public class EnemyFootsteps : MonoBehaviour
{
    [Header("Configuración de Audio")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("Lista de efectos de sonido para las pisadas del enemigo.")]
    [SerializeField] private List<AudioClip> footstepClips = new List<AudioClip>();

    [Header("Ajustes")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.8f;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // Configurar el AudioSource para que el sonido sea 3D
        audioSource.spatialBlend = 1f; // 1.0 = Sonido 3D totalmente posicional
        audioSource.playOnAwake = false;
    }

    /// <summary>
    /// Esta función debe ser llamada desde el Animation Event del clip de animación (Walk / Run).
    /// </summary>
    public void PlayFootstep()
    {
        if (footstepClips == null || footstepClips.Count == 0 || audioSource == null) return;

        AudioClip clip = footstepClips[Random.Range(0, footstepClips.Count)];
        audioSource.PlayOneShot(clip, volume);
    }
}