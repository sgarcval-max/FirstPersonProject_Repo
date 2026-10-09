using UnityEngine;
using TMPro;

/// <summary>
/// Texto de HUD que muestra el nombre del arma equipada (o "Manos" si no llevas ninguna).
/// Se conecta SOLO al PlayerWeaponInventory de la escena: no hace falta cablear ningún
/// evento en el Inspector. Se actualiza al cambiar de arma y al cambiar de idioma.
/// </summary>
public class EquippedWeaponDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text weaponNameText;

    [Header("Texto cuando no llevas ningún arma")]
    [SerializeField] private string handsNameSpanish = "Manos";
    [SerializeField] private string handsNameEnglish = "Hands";

    private PlayerWeaponInventory inventory;
    private WeaponData currentWeapon;

    private void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= Refresh;
    }

    private void Start()
    {
        inventory = FindObjectOfType<PlayerWeaponInventory>();

        if (inventory != null)
        {
            if (inventory.OnEquippedChanged != null)
            {
                inventory.OnEquippedChanged.AddListener(UpdateDisplay);
            }
            UpdateDisplay(inventory.EquippedWeapon);
        }
    }

    private void OnDestroy()
    {
        if (inventory != null && inventory.OnEquippedChanged != null)
        {
            inventory.OnEquippedChanged.RemoveListener(UpdateDisplay);
        }
    }

    public void UpdateDisplay(WeaponData weapon)
    {
        currentWeapon = weapon;
        Refresh();
    }

    private void Refresh()
    {
        if (weaponNameText == null) return;

        if (currentWeapon != null)
        {
            weaponNameText.text = currentWeapon.GetDisplayName();
        }
        else
        {
            weaponNameText.text = GameLanguage.IsEnglish ? handsNameEnglish : handsNameSpanish;
        }
    }
}