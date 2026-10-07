using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gestor central de idiomas. Guarda una lista de "entradas" (una por cada texto del
/// juego), cada una con su versión en español y en inglés, identificadas por una
/// clave única (key). Cambiar de idioma avisa a todos los LocalizedText de la escena
/// para que se actualicen solos. El idioma elegido se guarda con PlayerPrefs.
/// </summary>
public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    [Serializable]
    public class LocalizedEntry
    {
        [Tooltip("Identificador único de este texto, ej: 'menu_resume', 'settings_sound'.")]
        public string key;
        [TextArea(1, 3)]
        public string spanish;
        [TextArea(1, 3)]
        public string english;
    }

    public enum Language { Spanish, English }

    [Header("Todos los textos del juego (uno por fila)")]
    [SerializeField] private List<LocalizedEntry> entries;

    private Dictionary<string, LocalizedEntry> lookup;

    /// <summary>Se dispara cada vez que cambia el idioma, para que todos los LocalizedText se actualicen.</summary>
    public static event Action OnLanguageChanged;

    public Language CurrentLanguage { get; private set; } = Language.Spanish;

    private const string LANGUAGE_KEY = "game_language";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        BuildLookup();
        LoadSavedLanguage();
    }

    private void BuildLookup()
    {
        lookup = new Dictionary<string, LocalizedEntry>();
        foreach (LocalizedEntry entry in entries)
        {
            if (!string.IsNullOrEmpty(entry.key) && !lookup.ContainsKey(entry.key))
            {
                lookup.Add(entry.key, entry);
            }
        }
    }

    private void LoadSavedLanguage()
    {
        int saved = PlayerPrefs.GetInt(LANGUAGE_KEY, (int)Language.Spanish);
        CurrentLanguage = (Language)saved;
    }

    public void SetLanguage(Language language)
    {
        CurrentLanguage = language;
        PlayerPrefs.SetInt(LANGUAGE_KEY, (int)language);
        OnLanguageChanged?.Invoke();
    }

    /// <summary>Devuelve el texto correspondiente a esta clave, en el idioma actual.</summary>
    public string GetText(string key)
    {
        if (lookup != null && lookup.TryGetValue(key, out LocalizedEntry entry))
        {
            return CurrentLanguage == Language.Spanish ? entry.spanish : entry.english;
        }

        // Si no se encuentra la clave, devolvemos la propia clave para que sea fácil detectar el fallo
        Debug.LogWarning($"LocalizationManager: no se encontró la clave '{key}'.");
        return key;
    }
}
