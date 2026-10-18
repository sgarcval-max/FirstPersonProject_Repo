using System;
using UnityEngine;
using TMPro;

/// <summary>
/// Lleva la puntuación del jugador: es la suma de los puntos de los objetos clave que lleva en el
/// inventario en este momento. Recoger un objeto suma sus puntos; soltarlo (o que te atrapen y se
/// te caiga todo) los resta. Así no hace falta llevar ninguna cuenta aparte.
///
/// Debe ir en un objeto que esté siempre activo (por ejemplo el Canvas del HUD).
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Tooltip("El texto del HUD donde se muestra la puntuación.")]
    [SerializeField] private TMP_Text scoreText;

    /// <summary>La puntuación actual: los puntos de todo lo que llevas en el inventario.</summary>
    public int Score { get; private set; }

    /// <summary>Se dispara cada vez que cambia la puntuación.</summary>
    public event Action<int> OnScoreChanged;

    private PlayerKeyItemInventory inventory;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += RefreshText;
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= RefreshText;
    }

    private void Start()
    {
        inventory = FindObjectOfType<PlayerKeyItemInventory>();

        if (inventory != null)
        {
            inventory.OnChanged += Recalculate;
        }
        else
        {
            Debug.LogWarning("ScoreManager: no se encontró PlayerKeyItemInventory en la escena.");
        }

        Recalculate();
    }

    private void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.OnChanged -= Recalculate;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Recalculate()
    {
        int total = 0;

        if (inventory != null)
        {
            foreach (KeyItemData item in inventory.Items)
            {
                total += item.points;
            }
        }

        bool changed = total != Score;
        Score = total;

        RefreshText();

        if (changed && OnScoreChanged != null)
        {
            OnScoreChanged.Invoke(Score);
        }
    }

    private void RefreshText()
    {
        if (scoreText == null) return;

        scoreText.text = (GameLanguage.IsEnglish ? "Score: " : "Puntos: ") + Score;
    }
}
