using UnityEngine;

/// <summary>
/// Controla los botones de "Español" / "English" en la página de Subtítulos.
/// </summary>
public class LanguageSettingsController : MonoBehaviour
{
    public void SetSpanish()
    {
        LocalizationManager.Instance.SetLanguage(LocalizationManager.Language.Spanish);
    }

    public void SetEnglish()
    {
        LocalizationManager.Instance.SetLanguage(LocalizationManager.Language.English);
    }
}