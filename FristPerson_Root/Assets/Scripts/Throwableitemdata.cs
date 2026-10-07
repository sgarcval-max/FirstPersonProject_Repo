using UnityEngine;

/// <summary>
/// Representa UN TIPO de objeto lanzable (comida, una piedra, un cuchillo...).
/// Se crea como asset en el Project (click derecho > Create > Juego > Objeto Lanzable),
/// no se pone directamente en la escena. El inventario del jugador guarda una lista
/// de estos assets.
/// </summary>
[CreateAssetMenu(fileName = "NuevoObjetoLanzable", menuName = "Juego/Objeto Lanzable")]
public class ThrowableItemData : ScriptableObject
{
    [Tooltip("Nombre que se mostrará en pantalla (ej: 'Comida', 'Cuchillo').")]
    public string itemName = "Comida";

    [Tooltip("El prefab que se instancia al lanzar este objeto (debe tener Rigidbody y ThrowableProjectile).")]
    public GameObject projectilePrefab;

    [Tooltip("Fuerza con la que sale disparado este objeto en concreto.")]
    public float throwForce = 15f;
}
