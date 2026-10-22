using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestor central de efectos de sonido para la interfaz de usuario (UI), paneles, inventario y pisadas del jugador.
/// </summary>
public class UIAudioManager : MonoBehaviour
{
    public static UIAudioManager Instance { get; private set; }

    [Header("Configuración de AudioSource")]
    [SerializeField] private AudioSource uiAudioSource;

    [Header("Botones")]
    [SerializeField] private AudioClip buttonClickClip;
    [SerializeField] private AudioClip buttonHoverClip;

    [Header("Paneles Generales")]
    [SerializeField] private AudioClip panelOpenClip;
    [SerializeField] private AudioClip panelCloseClip;

    [Header("Inventario / Mochila (Exclusivos)")]
    [Tooltip("Sonido único al abrir el inventario. Si no asignas nada, usará el sonido de abrir panel.")]
    [SerializeField] private AudioClip inventoryOpenClip;
    [Tooltip("Sonido único al cerrar el inventario. Si no asignas nada, usará el sonido de cerrar panel.")]
    [SerializeField] private AudioClip inventoryCloseClip;

    [Header("Ajustes y Reasignación de Teclas")]
    [SerializeField] private AudioClip keyRebindStartClip;

    [Header("Cuenta Atrás")]
    [Tooltip("Efecto de sonido cuando quedan 10 segundos en la partida.")]
    [SerializeField] private AudioClip countdownWarningClip;

    [Header("SFX de Pisadas")]
    [Tooltip("Lista desplegable de efectos de sonido para las pisadas. Se elegirá uno al azar en cada paso.")]
    [SerializeField] private List<AudioClip> footstepClips = new List<AudioClip>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (uiAudioSource == null)
        {
            uiAudioSource = GetComponent<AudioSource>();
            if (uiAudioSource == null)
            {
                uiAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    // --- MÉTODOS DE BOTONES ---
    public void PlayButtonClick()
    {
        PlayClip(buttonClickClip);
    }

    public void PlayButtonHover()
    {
        PlayClip(buttonHoverClip);
    }

    // --- MÉTODOS DE PANELES Y MENÚS ---
    public void PlayPanelOpen()
    {
        PlayClip(panelOpenClip);
    }

    public void PlayPanelClose()
    {
        PlayClip(panelCloseClip);
    }

    // --- MÉTODOS EXCLUSIVOS DE INVENTARIO ---
    public void PlayInventoryOpen()
    {
        AudioClip clip = inventoryOpenClip != null ? inventoryOpenClip : panelOpenClip;
        PlayClip(clip);
    }

    public void PlayInventoryClose()
    {
        AudioClip clip = inventoryCloseClip != null ? inventoryCloseClip : panelCloseClip;
        PlayClip(clip);
    }

    // --- MÉTODOS DE CONFIGURACIÓN ---
    public void PlayKeyRebindStart()
    {
        PlayClip(keyRebindStartClip);
    }

    // --- MÉTODOS DE CUENTA ATRÁS ---
    public void PlayCountdownWarning(bool loop = true)
    {
        if (uiAudioSource == null || countdownWarningClip == null) return;

        if (uiAudioSource.isPlaying && uiAudioSource.clip == countdownWarningClip) return;

        uiAudioSource.clip = countdownWarningClip;
        uiAudioSource.loop = loop;
        uiAudioSource.Play();
    }

    public void StopCountdownWarning()
    {
        if (uiAudioSource != null && uiAudioSource.clip == countdownWarningClip)
        {
            uiAudioSource.Stop();
            uiAudioSource.clip = null;
            uiAudioSource.loop = false;
        }
    }

    // --- MÉTODOS DE PISADAS ---
    /// <summary>
    /// Reproduce un efecto de pisada aleatorio de la lista asignada en el Inspector.
    /// </summary>
    public void PlayRandomFootstep()
    {
        if (footstepClips == null || footstepClips.Count == 0) return;

        int randomIndex = Random.Range(0, footstepClips.Count);
        AudioClip clip = footstepClips[randomIndex];
        PlayClip(clip);
    }

    private void PlayClip(AudioClip clip)
    {
        if (uiAudioSource != null && clip != null)
        {
            uiAudioSource.PlayOneShot(clip);
        }
    }
}