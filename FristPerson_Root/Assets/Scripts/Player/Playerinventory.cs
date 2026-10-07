using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// UnityEvent que pasa el ThrowableItemData actual. Un UnityEvent normal no puede llevar
/// parámetros personalizados en el Inspector, así que se necesita esta clase intermedia.
/// </summary>
[System.Serializable]
public class ThrowableItemEvent : UnityEvent<ThrowableItemData> { }

/// <summary>
/// Gestiona qué objetos lanzables tiene el jugador y cuál está seleccionado ahora mismo.
/// Cambiar de objeto (NextItem) avisa mediante OnItemChanged para que la UI (o cualquier
/// otro script) se entere, sin tener que consultar este inventario constantemente.
/// </summary>
public class PlayerInventory : MonoBehaviour
{
    [Tooltip("Los objetos lanzables que tiene el jugador, en el orden en que se ciclan con 'Cambiar objeto'.")]
    [SerializeField] private List<ThrowableItemData> items = new List<ThrowableItemData>();

    [Tooltip("Se dispara cada vez que cambia el objeto seleccionado (incluida la primera vez, al arrancar).")]
    public ThrowableItemEvent OnItemChanged;

    private int currentIndex = 0;

    public ThrowableItemData CurrentItem => items.Count > 0 ? items[currentIndex] : null;

    private void Start()
    {
        OnItemChanged?.Invoke(CurrentItem);
    }

    /// <summary>Pasa al siguiente objeto de la lista, volviendo al principio al llegar al final.</summary>
    public void NextItem()
    {
        if (items.Count == 0) return;

        currentIndex = (currentIndex + 1) % items.Count;
        OnItemChanged?.Invoke(CurrentItem);

        if (UIAudioManager.Instance != null)
        {
            UIAudioManager.Instance.PlayButtonClick();
        }
    }
}
