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

    /// <summary>
    /// Crea en el mundo un objeto clave recogible (de momento un cubito, hasta que haya modelos
    /// de verdad). Se usa para soltar los objetos clave del jugador cuando lo atrapan.
    /// </summary>
    public static KeyItemPickup SpawnInWorld(KeyItemData data, Vector3 position)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = data.GetDisplayName() + " (suelo)";
        go.transform.position = position;
        go.transform.localScale = Vector3.one * 0.2f;

        KeyItemPickup pickup = go.AddComponent<KeyItemPickup>();
        pickup.item = data;

        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        return pickup;
    }

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

        if (inventory.AddItem(item, gameObject))
        {
            // No se destruye: el inventario lo guarda para devolverlo al suelo tal cual si te atrapan.
            gameObject.SetActive(false);
        }
    }
}