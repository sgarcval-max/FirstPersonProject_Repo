using UnityEngine;

/// <summary>
/// Representa UN TIPO de arma (pistola, cuchillo...). Se crea como asset en el Project
/// (click derecho > Create > Juego > Arma), no se pone directamente en la escena.
/// </summary>
[CreateAssetMenu(fileName = "NuevaArma", menuName = "Juego/Arma")]
public class WeaponData : ScriptableObject
{
    [Tooltip("Clave de LocalizationManager con el nombre del arma (ej: weapon_pistol).")]
    public string nameKey;

    [Tooltip("Prefab que aparece en el mundo cuando el arma está en el suelo o se suelta. Debe tener Rigidbody, un Collider y el componente WeaponPickup.")]
    public GameObject worldPrefab;
}