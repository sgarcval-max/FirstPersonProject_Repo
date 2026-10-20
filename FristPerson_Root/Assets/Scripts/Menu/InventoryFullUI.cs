using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Muestra un texto en pantalla ("¡Inventario Lleno!") con animación rápida de escalado ("pop"),
/// lo mantiene durante 2.5 segundos y se desvanece suavemente (fade out).
/// </summary>
public class InventoryFullUI : MonoBehaviour
{
    [Header("Referencias UI")]
    [Tooltip("El CanvasGroup del objeto para controlar la transparencia (opcional, lo añade solo si falta).")]
    [SerializeField] private CanvasGroup canvasGroup;
    [Tooltip("El texto de TextMeshPro que mostrará el mensaje.")]
    [SerializeField] private TMP_Text notificationText;
    [Tooltip("El RectTransform del texto o contenedor para la animación de rebote (pop).")]
    [SerializeField] private RectTransform popTransform;

    [Header("Tiempos de Animación")]
    [Tooltip("Tiempo en segundos que tarda la animación del pop inicial.")]
    [SerializeField] private float popDuration = 0.15f;
    [Tooltip("Tiempo en segundos que se mantiene el texto visible en pantalla.")]
    [SerializeField] private float holdDuration = 2.5f;
    [Tooltip("Tiempo en segundos que tarda en desaparecer (fade out).")]
    [SerializeField] private float fadeDuration = 0.5f;

    private PlayerKeyItemInventory inventory;
    private Coroutine notificationRoutine;

    private void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (popTransform == null) popTransform = GetComponent<RectTransform>();

        // Ocultar completamente al arrancar la partida
        canvasGroup.alpha = 0f;
    }

    private void Start()
    {
        inventory = FindObjectOfType<PlayerKeyItemInventory>();
        if (inventory != null)
        {
            inventory.OnInventoryFull += TriggerNotification;
        }
    }

    private void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.OnInventoryFull -= TriggerNotification;
        }
    }

    public void TriggerNotification()
    {
        if (notificationRoutine != null) StopCoroutine(notificationRoutine);
        notificationRoutine = StartCoroutine(ShowNotificationRoutine());
    }

    private IEnumerator ShowNotificationRoutine()
    {
        bool isEnglish = GameLanguage.IsEnglish;
        if (notificationText != null)
        {
            notificationText.text = isEnglish ? "Inventory Full!" : "¡Inventario Lleno!";
        }

        // 1. Resetear visibilidad y escala inicial para el "POP"
        canvasGroup.alpha = 1f;

        float elapsed = 0f;
        while (elapsed < popDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / popDuration;

            // Crece rápido de 0.3 a 1.15 para simular un golpe/pop
            float scale = Mathf.Lerp(0.3f, 1.15f, t);
            if (popTransform != null) popTransform.localScale = Vector3.one * scale;

            yield return null;
        }

        // Asentar al tamaño normal (1.0)
        if (popTransform != null) popTransform.localScale = Vector3.one;

        // 2. Mantener estático en pantalla
        yield return new WaitForSeconds(holdDuration);

        // 3. Fundido de salida (Fade Out)
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            if (canvasGroup != null) canvasGroup.alpha = alpha;

            yield return null;
        }

        canvasGroup.alpha = 0f;
        notificationRoutine = null;
    }
}
