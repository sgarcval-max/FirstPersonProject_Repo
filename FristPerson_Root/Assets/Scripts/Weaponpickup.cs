using UnityEngine;

/// <summary>
/// Se añade al prefab "mundo" de un arma (el que está tirado en el suelo). Cuando el jugador
/// lo mira y pulsa Interactuar, intenta guardarlo en su inventario de armas; si lo consigue,
/// este objeto del suelo desaparece.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class WeaponPickup : MonoBehaviour, IInteractable
{
    [Tooltip("Qué arma representa este objeto del suelo.")]
    [SerializeField] private WeaponData weapon;

    public string GetPrompt()
    {
        string pickupText = Localize("prompt_pickup");
        string weaponName = weapon != null ? Localize(weapon.nameKey) : "?";
        return pickupText + " " + weaponName;
    }

    public void Interact(GameObject player)
    {
        if (weapon == null) return;

        PlayerWeaponInventory inventory = player.GetComponent<PlayerWeaponInventory>();
        if (inventory == null) return;

        if (inventory.TryPickup(weapon))
        {
            Destroy(gameObject);
        }
    }

    private static string Localize(string key)
    {
        if (LocalizationManager.Instance != null)
        {
            return LocalizationManager.Instance.GetText(key);
        }
        return key;
    }
}
