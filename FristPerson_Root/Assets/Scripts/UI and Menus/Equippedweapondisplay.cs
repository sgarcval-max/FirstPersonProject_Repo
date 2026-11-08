using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Muestra en el HUD el nombre del arma equipada, el progreso de recarga de la barra
/// y gestiona el mensaje de aviso con animación Pop-Up y fundido a transparente (Fade Out).
/// </summary>
public class EquippedWeaponDisplay : MonoBehaviour
{
    [Header("Texto del Arma")]
    [SerializeField] private TMP_Text weaponNameText;
    [SerializeField] private string handsNameSpanish = "Manos";
    [SerializeField] private string handsNameEnglish = "Hands";

    [Header("Barra de Recarga del HUD")]
    [Tooltip("Imagen de la barra de recarga (Image Type debe ser Filled en Unity).")]
    [SerializeField] private Image cooldownFillBar;

    [Header("Aviso de Carga Incompleta")]
    [Tooltip("Texto flotante donde aparecerá el mensaje de advertencia.")]
    [SerializeField] private TMP_Text warningText;
    [Tooltip("CanvasGroup para el fundido de salida. Si se deja vacío, se añade automáticamente.")]
    [SerializeField] private CanvasGroup warningCanvasGroup;
    [SerializeField] private string spanishWarning = "¡Carga incompleta! Espera al 100%";
    [SerializeField] private string englishWarning = "Incomplete charge! Wait for 100%";

    [Header("Ajustes de Animación del Aviso")]
    [Tooltip("Tiempo en segundos que se mantiene el aviso visible antes de hacer el fade out.")]
    [SerializeField] private float warningDuration = 1.0f;
    [Tooltip("Multiplicador de tamaño máximo durante el Pop-Up (1.25 = 25% más grande).")]
    [SerializeField] private float popScaleMultiplier = 1.25f;
    [Tooltip("Duración del efecto Pop-Up inicial.")]
    [SerializeField] private float popDuration = 0.15f;
    [Tooltip("Duración del fundido a transparente (Fade Out).")]
    [SerializeField] private float fadeDuration = 0.4f;
    [SerializeField] private Color warningFlashColor = Color.red;

    private PlayerWeaponInventory inventory;
    private PlayerWeaponFire weaponFire;
    private WeaponData currentWeapon;

    private Color originalBarColor = Color.white;
    private Vector3 originalWarningScale = Vector3.one;
    private Coroutine warningCoroutine;

    private void Awake()
    {
        inventory = FindObjectOfType<PlayerWeaponInventory>();
        weaponFire = FindObjectOfType<PlayerWeaponFire>();

        if (cooldownFillBar != null)
        {
            originalBarColor = cooldownFillBar.color;
        }

        if (warningText != null)
        {
            originalWarningScale = warningText.transform.localScale;

            // Busca o crea automáticamente el CanvasGroup para el Fade Out
            if (warningCanvasGroup == null)
            {
                warningCanvasGroup = warningText.GetComponent<CanvasGroup>();
                if (warningCanvasGroup == null)
                {
                    warningCanvasGroup = warningText.gameObject.AddComponent<CanvasGroup>();
                }
            }

            warningCanvasGroup.alpha = 0f;
            warningText.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        LocalizationManager.OnLanguageChanged += Refresh;

        if (weaponFire != null)
        {
            weaponFire.OnTriedWhileRecharging.AddListener(OnShootFailed);
        }

        Refresh();
    }

    private void OnDisable()
    {
        LocalizationManager.OnLanguageChanged -= Refresh;

        if (weaponFire != null)
        {
            weaponFire.OnTriedWhileRecharging.RemoveListener(OnShootFailed);
        }
    }

    private void Start()
    {
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

    private void Update()
    {
        UpdateCooldownBar();
    }

    public void UpdateDisplay(WeaponData weapon)
    {
        currentWeapon = weapon;
        Refresh();
    }

    private void UpdateCooldownBar()
    {
        if (cooldownFillBar == null) return;

        if (currentWeapon == null || !currentWeapon.isRanged || weaponFire == null || currentWeapon.rechargeTime <= 0f)
        {
            cooldownFillBar.fillAmount = 1f;
            return;
        }

        float remaining = weaponFire.GetRemainingCooldown(currentWeapon);
        float progress = 1f - (remaining / currentWeapon.rechargeTime);
        cooldownFillBar.fillAmount = Mathf.Clamp01(progress);
    }

    private void OnShootFailed()
    {
        if (warningCoroutine != null)
        {
            StopCoroutine(warningCoroutine);
        }
        warningCoroutine = StartCoroutine(ShowWarningRoutine());
    }

    private IEnumerator ShowWarningRoutine()
    {
        if (warningText == null) yield break;

        warningText.text = GameLanguage.IsEnglish ? englishWarning : spanishWarning;
        warningText.gameObject.SetActive(true);

        if (cooldownFillBar != null)
        {
            cooldownFillBar.color = warningFlashColor;
        }

        Transform textTransform = warningText.transform;

        // --- 1. ANIMACIÓN POP-UP (ENTRADA RÁPIDA CON REBOTE) ---
        if (warningCanvasGroup != null) warningCanvasGroup.alpha = 1f;

        float elapsed = 0f;
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popDuration;

            // Crece rápido de 0.3x a 1.25x para dar la sensación de golpe/pop
            float scaleFactor = Mathf.Lerp(0.3f, popScaleMultiplier, t);
            textTransform.localScale = originalWarningScale * scaleFactor;
            yield return null;
        }

        // Se asienta en su tamaño normal
        textTransform.localScale = originalWarningScale;

        // --- 2. TIEMPO MANSTENIDO EN PANTALLA ---
        yield return new WaitForSeconds(warningDuration);

        // --- 3. ANIMACIÓN FADE OUT (DESVANECIMIENTO SUAVE) ---
        if (warningCanvasGroup != null)
        {
            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                warningCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                yield return null;
            }
            warningCanvasGroup.alpha = 0f;
        }

        // Restaurar estado e imagen de la barra
        warningText.gameObject.SetActive(false);

        if (cooldownFillBar != null)
        {
            cooldownFillBar.color = originalBarColor;
        }

        warningCoroutine = null;
    }

    private void Refresh()
    {
        if (weaponNameText != null)
        {
            if (currentWeapon != null)
            {
                weaponNameText.text = currentWeapon.GetDisplayName();
            }
            else
            {
                weaponNameText.text = GameLanguage.IsEnglish ? handsNameEnglish : handsNameSpanish;
            }
        }

        if (warningText != null && warningText.gameObject.activeSelf)
        {
            warningText.text = GameLanguage.IsEnglish ? englishWarning : spanishWarning;
        }
    }
}