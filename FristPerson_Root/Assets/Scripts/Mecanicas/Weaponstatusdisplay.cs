using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Muestra el estado del arma a distancia equipada en el HUD mediante texto y/o barra visual.
/// La barra se vacía de golpe al disparar y se va llenando progresivamente hasta estar lista (100%).
/// </summary>
public class WeaponStatusDisplay : MonoBehaviour
{
    [Header("Texto (Opcional)")]
    [SerializeField] private TMP_Text statusText;

    [Header("Barra de Recarga Visual")]
    [Tooltip("La imagen de la barra (debe tener Image Type = Filled).")]
    [SerializeField] private Image cooldownFillImage;
    [Tooltip("Contenedor/marco opcional para ocultar la barra cuando no lleves un arma a distancia.")]
    [SerializeField] private GameObject cooldownBarContainer;

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
        if (inventory == null || fire == null) return;

        WeaponData weapon = inventory.EquippedWeapon;

        // Si no llevamos arma o no es a distancia (manos vacías, cuchillo...), ocultamos todo
        if (weapon == null || !weapon.isRanged)
        {
            if (statusText != null && lastText != "")
            {
                lastText = "";
                statusText.text = "";
            }

            if (cooldownBarContainer != null) cooldownBarContainer.SetActive(false);
            if (cooldownFillImage != null && cooldownFillImage.gameObject != cooldownBarContainer)
            {
                cooldownFillImage.gameObject.SetActive(false);
            }
            return;
        }

        // Si llevamos un arma a distancia, mostramos la barra y el contenedor
        if (cooldownBarContainer != null) cooldownBarContainer.SetActive(true);
        if (cooldownFillImage != null) cooldownFillImage.gameObject.SetActive(true);

        float remaining = fire.GetRemainingCooldown(weapon);
        float totalRecharge = Mathf.Max(0.01f, weapon.rechargeTime);

        // 1. Actualizar la Barra de Recarga (cae a 0 al disparar y sube hasta 1 al estar lista)
        if (cooldownFillImage != null)
        {
            if (remaining <= 0f)
            {
                cooldownFillImage.fillAmount = 1f; // Lista al 100%
            }
            else
            {
                // Calcula el porcentaje de llenado (de 0 a 1)
                cooldownFillImage.fillAmount = 1f - (remaining / totalRecharge);
            }
        }

        // 2. Actualizar el Texto (Ready / Recargando MM:SS)
        if (statusText != null)
        {
            string text = BuildText(remaining);
            if (text != lastText)
            {
                lastText = text;
                statusText.text = text;
            }
        }
    }

    private string BuildText(float remaining)
    {
        bool english = GameLanguage.IsEnglish;

        if (remaining <= 0f)
        {
            return english ? "Ready" : "Lista";
        }

        int totalSeconds = Mathf.CeilToInt(remaining);
        string time = (totalSeconds / 60) + ":" + (totalSeconds % 60).ToString("00");
        return (english ? "Recharging " : "Recargando ") + time;
    }
}
