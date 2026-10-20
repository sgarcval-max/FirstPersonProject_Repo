using System;
using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Lleva la puntuación con animación de conteo progresivo y cambios de color (verde al subir, rojo al bajar).
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Tooltip("El texto del HUD donde se muestra la puntuación.")]
    [SerializeField] private TMP_Text scoreText;

    [Header("Colores de Animación")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color gainColor = new Color(0.2f, 0.9f, 0.3f, 1f); // Verde
    [SerializeField] private Color lossColor = new Color(1f, 0.25f, 0.2f, 1f);  // Rojo

    [Header("Animación")]
    [SerializeField] private float animDuration = 0.6f;

    public int Score { get; private set; }
    public event Action<int> OnScoreChanged;

    private PlayerKeyItemInventory inventory;
    private Coroutine scoreAnimRoutine;
    private int currentDisplayedScore = 0;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += ForceRefreshText;
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= ForceRefreshText;
    }

    private void Start()
    {
        inventory = FindObjectOfType<PlayerKeyItemInventory>();

        if (inventory != null)
        {
            inventory.OnChanged += Recalculate;
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
                if (item != null) total += item.points;
            }
        }

        int previousScore = Score;
        Score = total;

        if (previousScore != Score)
        {
            if (scoreAnimRoutine != null) StopCoroutine(scoreAnimRoutine);
            scoreAnimRoutine = StartCoroutine(AnimateScoreRoutine(previousScore, Score));

            if (OnScoreChanged != null) OnScoreChanged.Invoke(Score);
        }
        else
        {
            currentDisplayedScore = Score;
            ForceRefreshText();
        }
    }

    private IEnumerator AnimateScoreRoutine(int startVal, int targetVal)
    {
        bool isGaining = targetVal > startVal;
        if (scoreText != null)
        {
            scoreText.color = isGaining ? gainColor : lossColor;
        }

        float elapsed = 0f;
        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animDuration);
            float eased = 1f - Mathf.Pow(1f - t, 2f);

            currentDisplayedScore = Mathf.RoundToInt(Mathf.Lerp(startVal, targetVal, eased));
            UpdateTextDisplay(currentDisplayedScore);

            yield return null;
        }

        currentDisplayedScore = targetVal;
        UpdateTextDisplay(currentDisplayedScore);

        if (scoreText != null)
        {
            scoreText.color = normalColor;
        }

        scoreAnimRoutine = null;
    }

    private void ForceRefreshText()
    {
        UpdateTextDisplay(currentDisplayedScore);
        if (scoreText != null && scoreAnimRoutine == null)
        {
            scoreText.color = normalColor;
        }
    }

    private void UpdateTextDisplay(int value)
    {
        if (scoreText == null) return;
        scoreText.text = (GameLanguage.IsEnglish ? "Score: " : "Puntos: ") + value;
    }
}
