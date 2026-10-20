using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Guarda los objetos clave que el jugador ha recogido. Limita la capacidad máxima a 5 objetos.
/// Notifica cuando se intenta añadir un objeto estando lleno mediante OnInventoryFull.
/// </summary>
public class PlayerKeyItemInventory : MonoBehaviour
{
    [Header("Capacidad")]
    [Tooltip("Número máximo de objetos clave que puede llevar el jugador a la vez.")]
    [SerializeField] private int maxCapacity = 5;

    private readonly List<KeyItemData> items = new List<KeyItemData>();
    private readonly List<GameObject> worldObjects = new List<GameObject>();

    /// <summary>Se dispara cada vez que cambia el contenido del inventario.</summary>
    public event Action OnChanged;

    /// <summary>Se dispara cuando el jugador intenta coger un objeto pero el inventario está lleno.</summary>
    public event Action OnInventoryFull;

    public IReadOnlyList<KeyItemData> Items => items;
    public int MaxCapacity => maxCapacity;
    public int Count => items.Count;

    public bool Has(KeyItemData item)
    {
        return items.Contains(item);
    }

    /// <summary>
    /// Intenta añadir un objeto. Devuelve false y avisa a la UI si el inventario ya tiene 5 objetos.
    /// </summary>
    public bool AddItem(KeyItemData item, GameObject worldObject)
    {
        if (item == null) return false;

        if (items.Count >= maxCapacity)
        {
            Debug.Log("[Inventario] Lleno (" + items.Count + "/" + maxCapacity + "). No puedes llevar más objetos.");
            OnInventoryFull?.Invoke();
            return false;
        }

        items.Add(item);
        worldObjects.Add(worldObject);
        OnChanged?.Invoke();

        if (UIAudioManager.Instance != null)
        {
            UIAudioManager.Instance.PlayButtonClick();
        }
        return true;
    }

    public bool AddItem(KeyItemData item)
    {
        return AddItem(item, null);
    }

    public bool DropItem(KeyItemData item)
    {
        int index = items.IndexOf(item);
        if (index < 0) return false;

        GameObject worldObject = worldObjects[index];
        Vector3 position = DropUtility.GetGroundPoint(transform.position + transform.forward * 1.6f, 0.8f);

        items.RemoveAt(index);
        worldObjects.RemoveAt(index);

        if (worldObject != null)
        {
            RestoreWorldObject(worldObject, position);
        }
        else
        {
            KeyItemPickup.SpawnInWorld(item, position);
        }

        OnChanged?.Invoke();
        return true;
    }

    public void DropAllAt(Vector3 center, float radius)
    {
        for (int i = 0; i < items.Count; i++)
        {
            Vector3 position = DropUtility.GetGroundPoint(center, radius);
            GameObject worldObject = worldObjects[i];

            if (worldObject != null)
            {
                RestoreWorldObject(worldObject, position);
            }
            else
            {
                KeyItemPickup.SpawnInWorld(items[i], position);
            }
        }

        items.Clear();
        worldObjects.Clear();
        OnChanged?.Invoke();
    }

    private void RestoreWorldObject(GameObject worldObject, Vector3 position)
    {
        worldObject.transform.position = position;
        worldObject.SetActive(true);

        Rigidbody body = worldObject.GetComponent<Rigidbody>();
        if (body == null)
        {
            body = worldObject.AddComponent<Rigidbody>();
        }
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }
}