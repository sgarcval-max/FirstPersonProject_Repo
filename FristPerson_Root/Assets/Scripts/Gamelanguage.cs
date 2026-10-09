/// <summary>
/// Pequeña utilidad para consultar el idioma actual del juego sin tener que escribir la
/// misma comprobación en cada script. Usa el LocalizationManager existente.
/// </summary>
public static class GameLanguage
{
    public static bool IsEnglish
    {
        get
        {
            return LocalizationManager.Instance != null &&
                   LocalizationManager.Instance.CurrentLanguage == LocalizationManager.Language.English;
        }
    }
}
