using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Guarda los objetos clave que el jugador ha recogido. A diferencia de las armas, no tiene
/// ninguna forma de soltarlos: una vez recogidos, se quedan. Avisa con OnChanged cada vez que
/// se añade uno, para que la interfaz se actualice sola.
/// </summary>
public class PlayerKeyItemInventory : MonoBehaviour
{
    private readonly List<KeyItemData> items = new List<KeyItemData>();

    /// <summary>Se dispara cada vez que se recoge un objeto clave nuevo.</summary>
    public event Action OnChanged;

    public IReadOnlyList<KeyItemData> Items
    {
        get { return items; }
    }

    public bool Has(KeyItemData item)
    {
        return items.Contains(item);
    }

    /// <summary>Guarda el objeto. Devuelve false si es nulo o si ya lo tenías.</summary>
    public bool AddItem(KeyItemData item)
    {
        if (item == null || items.Contains(item)) return false;

        items.Add(item);
        if (OnChanged != null) OnChanged.Invoke();

        if (UIAudioManager.Instance != null)
        {
            UIAudioManager.Instance.PlayButtonClick();
        }
        return true;
    }
}
