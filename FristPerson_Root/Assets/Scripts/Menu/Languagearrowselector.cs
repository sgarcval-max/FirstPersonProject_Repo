using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Selector de idioma tipo "carrusel": un texto en el centro mostrando el idioma
/// actual, con una flecha a cada lado para ir cambiando entre los idiomas disponibles
/// (con salto circular: al llegar al último, la flecha derecha vuelve al primero).
/// </summary>
public class LanguageArrowSelector : MonoBehaviour
{
    // Los nombres de idioma se muestran siempre igual (Español/English), no se traducen entre sí
    private readonly LocalizationManager.Language[] languages =
    {
        LocalizationManager.Language.Spanish,
        LocalizationManager.Language.English
    };
    private readonly string[] displayNames = { "Español", "English" };

    [Header("Referencias UI")]
    [SerializeField] private TMP_Text currentLanguageText;
    [SerializeField] private Button leftArrowButton;
    [SerializeField] private Button rightArrowButton;

    private int currentIndex;

    private void OnEnable()
    {
        currentIndex = Array.IndexOf(languages, LocalizationManager.Instance.CurrentLanguage);
        if (currentIndex < 0) currentIndex = 0;
        UpdateDisplay();

        if (leftArrowButton != null) leftArrowButton.onClick.AddListener(Previous);
        if (rightArrowButton != null) rightArrowButton.onClick.AddListener(Next);
    }

    private void OnDisable()
    {
        if (leftArrowButton != null) leftArrowButton.onClick.RemoveListener(Previous);
        if (rightArrowButton != null) rightArrowButton.onClick.RemoveListener(Next);
    }

    public void Next()
    {
        currentIndex = (currentIndex + 1) % languages.Length;
        ApplyLanguage();
    }

    public void Previous()
    {
        currentIndex = (currentIndex - 1 + languages.Length) % languages.Length;
        ApplyLanguage();
    }

    private void ApplyLanguage()
    {
        LocalizationManager.Instance.SetLanguage(languages[currentIndex]);
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (currentLanguageText != null)
        {
            currentLanguageText.text = displayNames[currentIndex];
        }
    }
}
