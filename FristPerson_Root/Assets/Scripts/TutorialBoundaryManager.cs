using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Limita la zona de juego durante el tutorial usando un Box Collider Trigger.
/// Mantiene el filtro a blanco y negro hasta que la pantalla esté totalmente negra antes de hacer respawn.
/// </summary>
public class TutorialBoundaryManager : MonoBehaviour
{
    [Header("1. Zona de Límite (Box Collider)")]
    [Tooltip("El Box Collider (Trigger) que delimita el área permitida durante el tutorial.")]
    [SerializeField] private BoxCollider boundaryCollider;

    [Tooltip("Distancia en metros desde el borde del Box Collider donde empieza a cambiar a blanco y negro.")]
    [SerializeField] private float warningDistance = 3f;

    [Header("2. Referencias del Jugador")]
    [Tooltip("Transform del jugador. Si se deja vacío, se busca automáticamente al iniciar.")]
    [SerializeField] private Transform playerTransform;

    [Tooltip("Punto de reaparición. Si se deja vacío, guardará la posición inicial del jugador.")]
    [SerializeField] private Transform respawnPoint;

    [Header("3. UI de Advertencia y Cuenta Atrás")]
    [SerializeField] private GameObject warningPanel;
    [SerializeField] private TextMeshProUGUI countdownText;

    [Header("4. Animación Pop-Up del Contador")]
    [Tooltip("Multiplicador de escala al cambiar de número (1.4 = 40% más grande en el impacto).")]
    [SerializeField] private float popScaleMultiplier = 1.4f;

    [Tooltip("Duración total del rebote en segundos.")]
    [SerializeField] private float popDuration = 0.15f;

    [Header("5. Sonido de Cuenta Atrás (SFX)")]
    [SerializeField] private AudioSource countdownAudioSource;
    [SerializeField] private AudioClip countdownTickSound;

    [Header("6. Efecto de Audio Opaco (Low Pass Filter)")]
    [Tooltip("Frecuencia de corte cuando estás DENTRO (22000 = Sonido normal sin opacar).")]
    [SerializeField] private float normalAudioCutoff = 22000f;

    [Tooltip("Frecuencia de corte cuando estás FUERA (500 = Sonido grave y opaco/amortiguado).")]
    [SerializeField] private float muffledAudioCutoff = 500f;

    [Tooltip("Velocidad de transición del filtro de audio.")]
    [SerializeField] private float audioTransitionSpeed = 6f;

    [Header("7. Transición de Fundido a Negro (Fade Out)")]
    [SerializeField] private CanvasGroup fadeToBlackCanvasGroup;
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("8. Efecto Blanco y Negro (URP)")]
    [SerializeField] private Volume urpVolume;

    [Header("9. Tiempos")]
    [SerializeField] private float countdownTime = 5f;

    private ColorAdjustments colorAdjustments;
    private AudioLowPassFilter audioLowPassFilter;
    private float currentTimer;
    private int lastDisplayedSecond = -1;
    private bool isCountingDown = false;
    private bool isRespawning = false;
    private Vector3 initialSpawnPosition;
    private Quaternion initialSpawnRotation;
    private Vector3 originalTextScale = Vector3.one;
    private Coroutine popCoroutine;

    private float targetAudioCutoff = 22000f;

    private void Start()
    {
        if (fadeDuration <= 0f) fadeDuration = 0.5f;

        if (PlayerPrefs.GetInt("TutorialCompleted", 0) == 1)
        {
            DisableBoundarySystem();
            return;
        }

        if (playerTransform == null)
        {
            FirstPersonController fpc = FindObjectOfType<FirstPersonController>();
            if (fpc != null) playerTransform = fpc.transform;
        }

        if (playerTransform != null)
        {
            if (respawnPoint != null)
            {
                initialSpawnPosition = respawnPoint.position;
                initialSpawnRotation = respawnPoint.rotation;
            }
            else
            {
                initialSpawnPosition = playerTransform.position;
                initialSpawnRotation = playerTransform.rotation;
            }
        }

        AudioListener listener = FindObjectOfType<AudioListener>();
        if (listener != null)
        {
            audioLowPassFilter = listener.GetComponent<AudioLowPassFilter>();
            if (audioLowPassFilter == null)
            {
                audioLowPassFilter = listener.gameObject.AddComponent<AudioLowPassFilter>();
            }
            audioLowPassFilter.cutoffFrequency = normalAudioCutoff;
        }

        if (countdownAudioSource != null)
        {
            countdownAudioSource.bypassListenerEffects = true;
            countdownAudioSource.bypassEffects = true;
            countdownAudioSource.bypassReverbZones = true;
            countdownAudioSource.spatialBlend = 0f;
        }

        if (countdownText != null)
        {
            originalTextScale = countdownText.transform.localScale;
        }

        if (urpVolume != null && urpVolume.profile != null)
        {
            urpVolume.profile.TryGet(out colorAdjustments);
        }

        if (warningPanel != null) warningPanel.SetActive(false);

        if (fadeToBlackCanvasGroup != null)
        {
            fadeToBlackCanvasGroup.alpha = 0f;
            fadeToBlackCanvasGroup.blocksRaycasts = false;
            fadeToBlackCanvasGroup.gameObject.SetActive(false);
        }

        currentTimer = countdownTime;
        targetAudioCutoff = normalAudioCutoff;
    }

    private void Update()
    {
        UpdateAudioFilter();

        // Durante el respawn no interrumpimos el blanco y negro ni el audio
        if (isRespawning) return;

        if (!TutorialManager.IsPlayerProtected)
        {
            if (warningPanel != null && warningPanel.activeSelf) warningPanel.SetActive(false);
            SetGrayscaleAmount(0f);
            targetAudioCutoff = normalAudioCutoff;
            return;
        }

        if (playerTransform == null || boundaryCollider == null) return;

        float marginDistance = GetDistanceToBoundaryEdge(playerTransform.position);

        // JUGADOR DENTRO DEL LÍMITE
        if (marginDistance > 0f)
        {
            if (isCountingDown)
            {
                CancelCountdown();
            }

            float desaturationFactor = Mathf.Clamp01(1f - (marginDistance / Mathf.Max(0.1f, warningDistance)));
            SetGrayscaleAmount(desaturationFactor);
            targetAudioCutoff = normalAudioCutoff;
        }
        // JUGADOR FUERA DEL LÍMITE
        else
        {
            SetGrayscaleAmount(1f);
            targetAudioCutoff = muffledAudioCutoff;

            if (!isCountingDown)
            {
                StartCountdown();
            }

            ProcessCountdown();
        }
    }

    private void UpdateAudioFilter()
    {
        if (audioLowPassFilter != null)
        {
            audioLowPassFilter.cutoffFrequency = Mathf.Lerp(
                audioLowPassFilter.cutoffFrequency,
                targetAudioCutoff,
                Time.deltaTime * audioTransitionSpeed
            );
        }
    }

    private float GetDistanceToBoundaryEdge(Vector3 worldPos)
    {
        Vector3 localPos = boundaryCollider.transform.InverseTransformPoint(worldPos) - boundaryCollider.center;
        Vector3 halfSize = boundaryCollider.size * 0.5f;

        float marginX = halfSize.x - Mathf.Abs(localPos.x);
        float marginY = halfSize.y - Mathf.Abs(localPos.y);
        float marginZ = halfSize.z - Mathf.Abs(localPos.z);

        return Mathf.Min(marginX, Mathf.Min(marginY, marginZ));
    }

    private void SetGrayscaleAmount(float amount)
    {
        if (colorAdjustments != null)
        {
            colorAdjustments.saturation.value = Mathf.Lerp(0f, -100f, amount);
        }
    }

    private void StartCountdown()
    {
        isCountingDown = true;
        currentTimer = countdownTime;
        lastDisplayedSecond = -1;

        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.SetOutsideBoundary(true);
        }

        if (warningPanel != null) warningPanel.SetActive(true);
    }

    private void CancelCountdown()
    {
        isCountingDown = false;
        currentTimer = countdownTime;
        lastDisplayedSecond = -1;

        if (warningPanel != null) warningPanel.SetActive(false);

        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.SetOutsideBoundary(false);
        }
    }

    private void ProcessCountdown()
    {
        currentTimer -= Time.deltaTime;

        int currentSecond = Mathf.Max(0, Mathf.CeilToInt(currentTimer));

        if (currentSecond != lastDisplayedSecond)
        {
            lastDisplayedSecond = currentSecond;

            if (countdownText != null)
            {
                countdownText.text = currentSecond.ToString();

                if (popCoroutine != null) StopCoroutine(popCoroutine);
                popCoroutine = StartCoroutine(PopTextRoutine());
            }

            PlayCountdownSFX();
        }

        if (currentTimer <= 0f && !isRespawning)
        {
            StartCoroutine(RespawnRoutine());
        }
    }

    private void PlayCountdownSFX()
    {
        if (countdownAudioSource != null && countdownTickSound != null)
        {
            countdownAudioSource.bypassListenerEffects = true;
            countdownAudioSource.bypassEffects = true;
            countdownAudioSource.spatialBlend = 0f;

            countdownAudioSource.PlayOneShot(countdownTickSound);
        }
    }

    private IEnumerator PopTextRoutine()
    {
        if (countdownText == null) yield break;

        Transform textTransform = countdownText.transform;
        Vector3 peakScale = originalTextScale * popScaleMultiplier;
        float halfDuration = popDuration * 0.5f;

        float timer = 0f;
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            textTransform.localScale = Vector3.Lerp(originalTextScale, peakScale, timer / halfDuration);
            yield return null;
        }

        timer = 0f;
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            textTransform.localScale = Vector3.Lerp(peakScale, originalTextScale, timer / halfDuration);
            yield return null;
        }

        textTransform.localScale = originalTextScale;
    }

    private IEnumerator RespawnRoutine()
    {
        isRespawning = true;

        if (warningPanel != null) warningPanel.SetActive(false);

        // Forzamos el blanco y negro al 100% durante toda la transición a negro
        SetGrayscaleAmount(1f);

        // 1. Fade Out a negro
        if (fadeToBlackCanvasGroup != null)
        {
            Transform parentTransform = fadeToBlackCanvasGroup.transform.parent;
            while (parentTransform != null)
            {
                if (!parentTransform.gameObject.activeSelf) parentTransform.gameObject.SetActive(true);
                parentTransform = parentTransform.parent;
            }

            fadeToBlackCanvasGroup.gameObject.SetActive(true);

            Canvas parentCanvas = fadeToBlackCanvasGroup.GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                parentCanvas.overrideSorting = true;
                parentCanvas.sortingOrder = 999;
            }

            fadeToBlackCanvasGroup.transform.SetAsLastSibling();
            fadeToBlackCanvasGroup.blocksRaycasts = true;

            Image blackImg = fadeToBlackCanvasGroup.GetComponent<Image>();
            if (blackImg == null) blackImg = fadeToBlackCanvasGroup.GetComponentInChildren<Image>();

            if (blackImg != null)
            {
                blackImg.enabled = true;
                blackImg.color = new Color(0f, 0f, 0f, 1f);
            }

            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                fadeToBlackCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
                yield return null;
            }
            fadeToBlackCanvasGroup.alpha = 1f;
        }

        // 2. Reaparecer al jugador estando 100% a oscuras
        if (playerTransform != null)
        {
            CharacterController cc = playerTransform.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            playerTransform.position = initialSpawnPosition;
            playerTransform.rotation = initialSpawnRotation;

            if (cc != null) cc.enabled = true;
        }

        // AHORA QUE LA PANTALLA ESTÁ TOTALMENTE EN NEGRO, RESTAURAMOS EL COLOR Y AUDIO
        SetGrayscaleAmount(0f);
        targetAudioCutoff = normalAudioCutoff;
        isCountingDown = false;

        yield return new WaitForSeconds(0.2f);

        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.SetOutsideBoundary(false);
        }

        // 3. Fade In desde negro (con el color y sonido de la escena ya restablecidos)
        if (fadeToBlackCanvasGroup != null)
        {
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                fadeToBlackCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
                yield return null;
            }
            fadeToBlackCanvasGroup.alpha = 0f;
            fadeToBlackCanvasGroup.blocksRaycasts = false;
            fadeToBlackCanvasGroup.gameObject.SetActive(false);
        }

        isRespawning = false;
    }

    private void DisableBoundarySystem()
    {
        if (warningPanel != null) warningPanel.SetActive(false);
        SetGrayscaleAmount(0f);
        if (audioLowPassFilter != null) audioLowPassFilter.cutoffFrequency = normalAudioCutoff;
        enabled = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (boundaryCollider == null) return;

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.matrix = boundaryCollider.transform.localToWorldMatrix;
        Gizmos.DrawCube(boundaryCollider.center, boundaryCollider.size);
    }
}