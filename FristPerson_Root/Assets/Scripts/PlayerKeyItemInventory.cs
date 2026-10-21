using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Representa una pila/grupo de objetos clave del mismo tipo en el inventario.
/// </summary>
[System.Serializable]
public class KeyItemStack
{
    public KeyItemData itemData;
    public int quantity = 1;
    public List<GameObject> worldObjects = new List<GameObject>();

    /// <summary>Suma total de puntos de todas las unidades de esta pila.</summary>
    public int TotalPoints => itemData != null ? itemData.points * quantity : 0;

    /// <summary>
    /// Devuelve el nombre formateado para la UI (ejemplo: "Collar de Perlas x2" o "Llave").
    /// </summary>
    public string GetFormattedName()
    {
        if (itemData == null) return "";
        string displayName = itemData.GetDisplayName();
        return quantity > 1 ? $"{displayName} x{quantity}" : displayName;
    }
}

/// <summary>
/// Guarda los objetos clave que el jugador ha recogido. Limita la capacidad a 5 huecos/tipos de objetos.
/// Admite apilado de objetos duplicados (stacking) acumulando cantidad y puntos.
/// Notifica cuando se intenta añadir un objeto nuevo estando los 5 huecos llenos.
/// </summary>
public class PlayerKeyItemInventory : MonoBehaviour
{
    [Header("Capacidad")]
    [Tooltip("Número máximo de huecos/tipos de objetos clave distintos que puede llevar el jugador.")]
    [SerializeField] private int maxCapacity = 5;

    private readonly List<KeyItemStack> stacks = new List<KeyItemStack>();
    private readonly List<KeyItemData> flatItemsList = new List<KeyItemData>();

    /// <summary>Se dispara cada vez que cambia el contenido del inventario.</summary>
    public event Action OnChanged;

    /// <summary>Se dispara cuando el jugador intenta coger un objeto nuevo pero el inventario está lleno.</summary>
    public event Action OnInventoryFull;

    /// <summary>Lista de todas las unidades individuales (incluyendo duplicados).</summary>
    public IReadOnlyList<KeyItemData> Items => flatItemsList;

    /// <summary>Lista de las pilas de objetos clave para renderizado en la UI.</summary>
    public IReadOnlyList<KeyItemStack> Stacks => stacks;

    public int MaxCapacity => maxCapacity;

    /// <summary>Número de huecos/slots ocupados actualmente en la interfaz (máximo 5).</summary>
    public int Count => stacks.Count;

    /// <summary>Cantidad total de objetos individuales sumando todas las pilas.</summary>
    public int TotalItemCount => flatItemsList.Count;

    /// <summary>Suma total de puntos de todos los objetos clave en el inventario.</summary>
    public int TotalPoints
    {
        get
        {
            int total = 0;
            for (int i = 0; i < stacks.Count; i++)
            {
                total += stacks[i].TotalPoints;
            }
            return total;
        }
    }

    public bool Has(KeyItemData item)
    {
        if (item == null) return false;
        return stacks.Exists(s => s.itemData == item && s.quantity > 0);
    }

    /// <summary>
    /// Devuelve la cantidad almacenada de un objeto concreto.
    /// </summary>
    public int GetQuantity(KeyItemData item)
    {
        if (item == null) return 0;
        KeyItemStack stack = stacks.Find(s => s.itemData == item);
        return stack != null ? stack.quantity : 0;
    }

    /// <summary>
    /// Intenta añadir un objeto. Si ya lo llevas, se suma a la pila existente sin gastar un hueco.
    /// Devuelve false y avisa a la UI si es un objeto nuevo y ya hay 5 huecos ocupados.
    /// </summary>
    public bool AddItem(KeyItemData item, GameObject worldObject)
    {
        if (item == null) return false;

        // 1. Comprobar si ya existe una pila de este objeto para agruparlo
        KeyItemStack existingStack = stacks.Find(s => s.itemData == item);

        if (existingStack != null)
        {
            existingStack.quantity++;
            if (worldObject != null)
            {
                existingStack.worldObjects.Add(worldObject);
            }

            RefreshFlatList();
            OnChanged?.Invoke();

            if (UIAudioManager.Instance != null)
            {
                UIAudioManager.Instance.PlayButtonClick();
            }
            return true;
        }

        // 2. Si es un objeto nuevo, comprobar si hemos alcanzado el límite de 5 huecos
        if (stacks.Count >= maxCapacity)
        {
            Debug.Log("[Inventario] Lleno (" + stacks.Count + "/" + maxCapacity + " huecos). No puedes llevar más tipos de objetos.");
            OnInventoryFull?.Invoke();
            return false;
        }

        // 3. Crear un nuevo hueco/pila
        KeyItemStack newStack = new KeyItemStack
        {
            itemData = item,
            quantity = 1
        };

        if (worldObject != null)
        {
            newStack.worldObjects.Add(worldObject);
        }

        stacks.Add(newStack);
        RefreshFlatList();
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

    /// <summary>
    /// Suelta 1 unidad del objeto indicado. Si hay varios en la pila, reduce la cantidad a x1
    /// antes de borrar la casilla del inventario.
    /// </summary>
    public bool DropItem(KeyItemData item)
    {
        if (item == null) return false;

        KeyItemStack stack = stacks.Find(s => s.itemData == item);
        if (stack == null || stack.quantity <= 0) return false;

        Vector3 position = DropUtility.GetGroundPoint(transform.position + transform.forward * 1.6f, 0.8f);

        // Recuperar el GameObject original del mundo o instanciar uno nuevo
        GameObject worldObject = null;
        if (stack.worldObjects.Count > 0)
        {
            int lastIndex = stack.worldObjects.Count - 1;
            worldObject = stack.worldObjects[lastIndex];
            stack.worldObjects.RemoveAt(lastIndex);
        }

        if (worldObject != null)
        {
            RestoreWorldObject(worldObject, position);
        }
        else
        {
            KeyItemPickup.SpawnInWorld(item, position);
        }

        // Reducir la pila en 1 unidad
        stack.quantity--;

        // Si ya no quedan más unidades de este objeto, vaciamos el hueco
        if (stack.quantity <= 0)
        {
            stacks.Remove(stack);
        }

        RefreshFlatList();
        OnChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Suelta todos los objetos acumulados al ser atrapado por el enemigo.
    /// </summary>
    public void DropAllAt(Vector3 center, float radius)
    {
        for (int s = 0; s < stacks.Count; s++)
        {
            KeyItemStack stack = stacks[s];
            for (int i = 0; i < stack.quantity; i++)
            {
                Vector3 position = DropUtility.GetGroundPoint(center, radius);
                GameObject worldObject = (i < stack.worldObjects.Count) ? stack.worldObjects[i] : null;

                if (worldObject != null)
                {
                    RestoreWorldObject(worldObject, position);
                }
                else
                {
                    KeyItemPickup.SpawnInWorld(stack.itemData, position);
                }
            }
        }

        stacks.Clear();
        RefreshFlatList();
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

    private void RefreshFlatList()
    {
        flatItemsList.Clear();
        for (int i = 0; i < stacks.Count; i++)
        {
            for (int q = 0; q < stacks[i].quantity; q++)
            {
                flatItemsList.Add(stacks[i].itemData);
            }
        }
    }
}