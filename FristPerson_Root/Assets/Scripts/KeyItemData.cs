using UnityEngine;

/// <summary>
/// Representa UN objeto clave de los que hay que encontrar para escapar del hotel
/// (una llave, una tarjeta...). Se crea como asset en el Project
/// (click derecho > Create > Juego > Objeto Clave).
/// </summary>
[CreateAssetMenu(fileName = "NuevoObjetoClave", menuName = "Juego/Objeto Clave")]
public class KeyItemData : ScriptableObject
{
    [Header("Nombre")]
    public string spanishName = "Objeto";
    public string englishName = "Item";

    /// <summary>El nombre en el idioma actual del juego.</summary>
    public string GetDisplayName()
    {
        if (GameLanguage.IsEnglish && !string.IsNullOrEmpty(englishName))
        {
            return englishName;
        }
        return spanishName;
    }
}