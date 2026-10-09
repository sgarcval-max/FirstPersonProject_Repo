using UnityEngine;

/// <summary>
/// Se añade al objeto del mundo que representa un objeto clave (necesita un Collider para
/// poder ser detectado al mirarlo). Al pulsar Interactuar, se guarda en el inventario de
/// objetos clave del jugador y desaparece del mundo.
/// </summary>
public class KeyItemPickup : MonoBehaviour, IInteractable
{
    [Tooltip("Qué objeto clave representa este objeto del mundo.")]
    [SerializeField] private KeyItemData item;

    public string GetPrompt()
    {
        string pickupText = GameLanguage.IsEnglish ? "Pick up" : "Recoger";
        string itemName = item != null ? item.GetDisplayName() : "?";
        return pickupText + " " + itemName;
    }

    public void Interact(GameObject player)
    {
        if (item == null) return;

        PlayerKeyItemInventory inventory = player.GetComponent<PlayerKeyItemInventory>();
        if (inventory == null) return;

        if (inventory.AddItem(item))
        {
            Destroy(gameObject);
        }
    }
}
