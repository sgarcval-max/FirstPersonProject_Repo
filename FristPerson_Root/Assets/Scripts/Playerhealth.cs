using System;
using UnityEngine;

/// <summary>
/// Vida del jugador. Los guardias (y cualquier otra cosa peligrosa) llaman a TakeDamage.
/// Al llegar a 0, el jugador muere: se bloquean sus controles, se congela el juego, se libera
/// el cursor y se avisa con OnDied para que salga la pantalla de Game Over.
/// </summary>
[RequireComponent(typeof(FirstPersonController))]
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;

    /// <summary>True mientras el jugador esté muerto. Otros sistemas (como la pausa) lo consultan.</summary>
    public static bool IsPlayerDead { get; private set; }

    /// <summary>Se dispara al cambiar la vida. Parámetros: vida actual y vida máxima.</summary>
    public event Action<float, float> OnHealthChanged;

    /// <summary>Se dispara cada vez que el jugador recibe daño (para efectos como el flash rojo).</summary>
    public event Action OnDamaged;

    /// <summary>Se dispara una sola vez, cuando la vida llega a 0.</summary>
    public event Action OnDied;

    public float CurrentHealth { get; private set; }

    public float MaxHealth
    {
        get { return maxHealth; }
    }

    public bool IsDead
    {
        get { return CurrentHealth <= 0f; }
    }

    private FirstPersonController controller;

    private void Awake()
    {
        controller = GetComponent<FirstPersonController>();
        CurrentHealth = maxHealth;

        // Esta variable es estática y sobrevive al recargar la escena, así que la reseteamos aquí.
        IsPlayerDead = false;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);

        if (OnHealthChanged != null) OnHealthChanged.Invoke(CurrentHealth, maxHealth);
        if (OnDamaged != null) OnDamaged.Invoke();

        if (CurrentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        IsPlayerDead = true;

        // Desactivar FirstPersonController bloquea el movimiento, la cámara y también las
        // acciones de armas e inventario (que comprueban si está activo).
        controller.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;

        Debug.Log("[Jugador] Ha muerto.");

        if (OnDied != null) OnDied.Invoke();
    }

    // Botón de prueba: en Play, pulsa los tres puntitos del componente Player Health y elige esta opción.
    [ContextMenu("Probar: recibir 25 de daño")]
    private void DebugTakeDamage()
    {
        TakeDamage(25f);
    }
}
