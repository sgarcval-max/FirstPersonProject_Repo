using UnityEngine;
using TMPro;

/// <summary>
/// Texto de HUD que muestra el nombre del objeto actualmente seleccionado.
/// Conéctalo al evento "On Item Changed" de PlayerInventory desde el Inspector,
/// apuntando a este método "UpdateDisplay".
/// </summary>
public class CurrentItemDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text itemNameText;

    public void UpdateDisplay(ThrowableItemData item)
    {
        if (itemNameText != null)
        {
            itemNameText.text = item != null ? item.itemName : "-";
        }
    }
}