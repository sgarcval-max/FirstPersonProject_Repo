using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Guarda los objetos clave que el jugador ha recogido. A diferencia de las armas, no tiene
/// ninguna forma de soltarlos: una vez recogidos, se quedan. Avisa con OnChanged cada vez que
/// se añade uno, para que la interfaz se actualice sola.
///
/// Al recoger un objeto clave, el objeto del mundo no se destruye: se guarda desactivado. Así, si
/// te atrapan y todo cae al suelo, vuelve a aparecer exactamente igual que antes (mismo modelo y
/// material), en vez de fabricar uno nuevo.
/// </summary>
public class PlayerKeyItemInventory : MonoBehaviour
{
    private readonly List<KeyItemData> items = new List<KeyItemData>();
    private readonly List<GameObject> worldObjects = new List<GameObject>();

    /// <summary>Se dispara cada vez que cambia el contenido del inventario.</summary>
    public event Action OnChanged;

    public IReadOnlyList<KeyItemData> Items
    {
        get { return items; }
    }

    public bool Has(KeyItemData item)
    {
        return items.Contains(item);
    }

    /// <summary>
    /// Guarda el objeto. "worldObject" es el objeto del mundo que se acaba de recoger (el que se
    /// ve en el escenario): se guarda para poder devolverlo al suelo tal cual. Devuelve false si
    /// el objeto es nulo o si ya lo tenías.
    /// </summary>
    public bool AddItem(KeyItemData item, GameObject worldObject)
    {
        if (item == null || items.Contains(item)) return false;

        items.Add(item);
        worldObjects.Add(worldObject);
        if (OnChanged != null) OnChanged.Invoke();

        if (UIAudioManager.Instance != null)
        {
            UIAudioManager.Instance.PlayButtonClick();
        }
        return true;
    }

    /// <summary>Versión sin objeto del mundo (al soltarlo se creará un cubo provisional).</summary>
    public bool AddItem(KeyItemData item)
    {
        return AddItem(item, null);
    }

    /// <summary>
    /// Suelta UN objeto concreto al suelo, delante del jugador (es lo que hace el botón "Soltar" del
    /// inventario). El objeto vuelve a aparecer tal cual era y se puede volver a recoger. Devuelve
    /// false si no lo tenías.
    /// </summary>
    public bool DropItem(KeyItemData item)
    {
        int index = items.IndexOf(item);
        if (index < 0) return false;

        GameObject worldObject = worldObjects[index];

        // Cae un poco por delante del jugador, para no aparecer dentro de su propio cuerpo.
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

        if (OnChanged != null) OnChanged.Invoke();
        return true;
    }

    /// <summary>
    /// Suelta TODOS los objetos clave al suelo, repartidos alrededor de un punto, y vacía el
    /// inventario. Normalmente los objetos clave no se pueden soltar: esto solo se usa cuando
    /// te atrapa un guardia, para que tengas que volver a recogerlos.
    /// </summary>
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
        if (OnChanged != null) OnChanged.Invoke();
    }

    // Vuelve a poner en el mundo el objeto original (el que estaba guardado desactivado).
    private void RestoreWorldObject(GameObject worldObject, Vector3 position)
    {
        worldObject.transform.position = position;
        worldObject.SetActive(true);

        // Los objetos que colocaste a mano no suelen tener Rigidbody: se lo ponemos para que caigan al suelo.
        Rigidbody body = worldObject.GetComponent<Rigidbody>();
        if (body == null)
        {
            body = worldObject.AddComponent<Rigidbody>();
        }
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }
}