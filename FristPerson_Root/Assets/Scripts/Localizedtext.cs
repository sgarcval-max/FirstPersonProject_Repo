using UnityEngine;
using TMPro;

/// <summary>
/// Añade esto a CUALQUIER Text (TMP) del juego que deba cambiar de idioma.
/// Escribe la clave (key) correspondiente a ese texto en LocalizationManager,
/// y este componente se encarga de mostrarlo en el idioma actual y de actualizarse
/// solo cada vez que el jugador cambie el idioma en Ajustes.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class LocalizedText : MonoBehaviour
{
    [Tooltip("Debe coincidir EXACTAMENTE con la clave (key) de este texto en LocalizationManager.")]
    [SerializeField] private string key;

    private TMP_Text textComponent;

    private void Awake()
    {
        textComponent = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        Refresh();
        LocalizationManager.OnLanguageChanged += Refresh;
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= Refresh;
    }

    private void Refresh()
    {
        if (LocalizationManager.Instance != null && !string.IsNullOrEmpty(key))
        {
            textComponent.text = LocalizationManager.Instance.GetText(key);
        }
    }
}
