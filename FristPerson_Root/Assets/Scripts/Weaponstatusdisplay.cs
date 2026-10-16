using UnityEngine;
using TMPro;

/// <summary>
/// Texto de HUD que muestra el estado del arma a distancia que llevas en la mano: "Lista" o
/// "Recargando 1:23" con la cuenta atrás. Si no llevas un arma a distancia (manos vacías,
/// cuchillo...), el texto se queda vacío. Se conecta solo al inventario y al disparo del jugador.
/// </summary>
public class WeaponStatusDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text statusText;

    private PlayerWeaponInventory inventory;
    private PlayerWeaponFire fire;
    private string lastText = null;

    private void Start()
    {
        inventory = FindObjectOfType<PlayerWeaponInventory>();
        fire = FindObjectOfType<PlayerWeaponFire>();

        if (inventory == null || fire == null)
        {
            Debug.LogWarning("WeaponStatusDisplay: no se encontró PlayerWeaponInventory o PlayerWeaponFire en la escena.");
        }
    }

    private void Update()
    {
        if (statusText == null || inventory == null || fire == null) return;

        string text = BuildText();

        // Solo tocamos el texto cuando cambia, para no generar basura cada fotograma.
        if (text != lastText)
        {
            lastText = text;
            statusText.text = text;
        }
    }

    private string BuildText()
    {
        WeaponData weapon = inventory.EquippedWeapon;
        if (weapon == null || !weapon.isRanged) return "";

        bool english = GameLanguage.IsEnglish;
        float remaining = fire.GetRemainingCooldown(weapon);

        if (remaining <= 0f)
        {
            return english ? "Ready" : "Lista";
        }

        int totalSeconds = Mathf.CeilToInt(remaining);
        string time = (totalSeconds / 60) + ":" + (totalSeconds % 60).ToString("00");
        return (english ? "Recharging " : "Recargando ") + time;
    }
}
