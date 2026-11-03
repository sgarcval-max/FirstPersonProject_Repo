using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestor central de efectos de sonido para la interfaz de usuario (UI), paneles, inventario, pisadas, armas, saltos y fin de partida.
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

    [Header("Cuenta Atrás y Fin de Partida")]
    [Tooltip("Lista de sonidos por segundo (Elemento 0 = Queda 1 seg, Elemento 1 = Quedan 2 segs... Elemento 9 = Quedan 10 segs).")]
    [SerializeField] private List<AudioClip> countdownClips = new List<AudioClip>();

    [Tooltip("Efecto de sonido al terminar la partida (panel de resultados).")]
    [SerializeField] private AudioClip matchEndClip;

    [Header("SFX de Movimiento, Salto y Aterrizaje")]
    [Tooltip("Lista desplegable de efectos de sonido para las pisadas. Se elegirá uno al azar en cada paso.")]
    [SerializeField] private List<AudioClip> footstepClips = new List<AudioClip>();
    [Tooltip("Efecto de sonido al despegar/saltar.")]
    [SerializeField] private AudioClip jumpClip;
    [Tooltip("Efecto de sonido al impactar contra el suelo tras saltar o caer.")]
    [SerializeField] private AudioClip landClip;

    [Header("Armas")]
    [Tooltip("Efecto de sonido al disparar el arma.")]
    [SerializeField] private AudioClip weaponShootClip;

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

    // --- MÉTODOS DE CUENTA ATRÁS Y FIN DE PARTIDA ---
    /// <summary>
    /// Reproduce el efecto asignado para el segundo exacto recibido.
    /// </summary>
    public void PlayCountdownWarning(int second)
    {
        if (countdownClips == null || countdownClips.Count == 0) return;

        // Convertir el segundo actual (1 a 10) al índice de la lista (0 a 9)
        int index = second - 1;

        if (index >= 0 && index < countdownClips.Count)
        {
            AudioClip clipForSecond = countdownClips[index];
            if (clipForSecond != null)
            {
                PlayClip(clipForSecond);
            }
        }
    }

    public void StopCountdownWarning()
    {
        if (uiAudioSource != null)
        {
            uiAudioSource.Stop();
        }
    }

    public void PlayMatchEnd()
    {
        PlayClip(matchEndClip);
    }

    // --- MÉTODOS DE MOVIMIENTO, SALTO Y ATERRIZAJE ---
    public void PlayRandomFootstep()
    {
        if (footstepClips == null || footstepClips.Count == 0) return;

        int randomIndex = Random.Range(0, footstepClips.Count);
        AudioClip clip = footstepClips[randomIndex];
        PlayClip(clip);
    }

    public void PlayJump()
    {
        PlayClip(jumpClip);
    }

    public void PlayLand()
    {
        PlayClip(landClip);
    }

    // --- MÉTODOS DE ARMAS ---
    public void PlayWeaponShoot()
    {
        PlayClip(weaponShootClip);
    }

    private void PlayClip(AudioClip clip)
    {
        if (uiAudioSource != null && clip != null)
        {
            uiAudioSource.PlayOneShot(clip);
        }
    }
}