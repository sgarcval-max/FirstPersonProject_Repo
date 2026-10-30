using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Limita la zona de juego durante el tutorial usando un Box Collider Trigger.
/// Forzado de Canvas sortingOrder = 999 para garantizar la visibilidad del Fade a negro.
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

    [Header("4. Transición de Fundido a Negro (Fade Out)")]
    [Tooltip("CanvasGroup del objeto de pantalla negra a pantalla completa.")]
    [SerializeField] private CanvasGroup fadeToBlackCanvasGroup;
    [SerializeField] private float fadeDuration = 0.5f;

    [Header("5. Efecto Blanco y Negro (URP)")]
    [SerializeField] private Volume urpVolume;

    [Header("6. Tiempos")]
    [SerializeField] private float countdownTime = 5f;

    private ColorAdjustments colorAdjustments;
    private float currentTimer;
    private bool isCountingDown = false;
    private bool isRespawning = false;
    private Vector3 initialSpawnPosition;
    private Quaternion initialSpawnRotation;

    private void Start()
    {
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

        if (urpVolume != null && urpVolume.profile != null)
        {
            urpVolume.profile.TryGet(out colorAdjustments);
        }

        if (warningPanel != null) warningPanel.SetActive(false);

        // Validar e inicializar el CanvasGroup de la pantalla negra
        if (fadeToBlackCanvasGroup == null)
        {
            Debug.LogError("<color=red>[TutorialBoundaryManager]</color> ¡ATENCIÓN! No has asignado el 'Fade To Black Canvas Group' en el Inspector.");
        }
        else
        {
            fadeToBlackCanvasGroup.alpha = 0f;
            fadeToBlackCanvasGroup.blocksRaycasts = false;
            fadeToBlackCanvasGroup.gameObject.SetActive(false);
        }

        currentTimer = countdownTime;
    }

    private void Update()
    {
        if (!TutorialManager.IsPlayerProtected || isRespawning)
        {
            if (warningPanel != null && warningPanel.activeSelf) warningPanel.SetActive(false);
            SetGrayscaleAmount(0f);
            return;
        }

        if (playerTransform == null || boundaryCollider == null) return;

        float marginDistance = GetDistanceToBoundaryEdge(playerTransform.position);

        if (marginDistance > 0f)
        {
            if (isCountingDown)
            {
                CancelCountdown();
            }

            float desaturationFactor = Mathf.Clamp01(1f - (marginDistance / Mathf.Max(0.1f, warningDistance)));
            SetGrayscaleAmount(desaturationFactor);
        }
        else
        {
            SetGrayscaleAmount(1f);

            if (!isCountingDown)
            {
                StartCountdown();
            }

            ProcessCountdown();
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

        if (warningPanel != null) warningPanel.SetActive(false);

        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.SetOutsideBoundary(false);
        }
    }

    private void ProcessCountdown()
    {
        currentTimer -= Time.deltaTime;

        if (countdownText != null)
        {
            countdownText.text = Mathf.Max(0, Mathf.CeilToInt(currentTimer)).ToString();
        }

        if (currentTimer <= 0f && !isRespawning)
        {
            StartCoroutine(RespawnRoutine());
        }
    }

    private IEnumerator RespawnRoutine()
    {
        isRespawning = true;

        if (warningPanel != null) warningPanel.SetActive(false);

        // 1. Preparar la pantalla negra
        if (fadeToBlackCanvasGroup != null)
        {
            fadeToBlackCanvasGroup.gameObject.SetActive(true);

            // Elevar la capa del Canvas del Fade por encima de cualquier otro elemento de la UI
            Canvas parentCanvas = fadeToBlackCanvasGroup.GetComponentInParent<Canvas>();
            if (parentCanvas != null)
            {
                parentCanvas.overrideSorting = true;
                parentCanvas.sortingOrder = 999;
            }

            fadeToBlackCanvasGroup.transform.SetAsLastSibling();
            fadeToBlackCanvasGroup.blocksRaycasts = true;

            Image blackImg = fadeToBlackCanvasGroup.GetComponent<Image>();
            if (blackImg != null)
            {
                blackImg.color = new Color(0f, 0f, 0f, 1f);
            }

            // Realizar Fade Out
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                fadeToBlackCanvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
                yield return null;
            }
            fadeToBlackCanvasGroup.alpha = 1f;
        }

        // 2. Teletransportar al jugador estando en negro
        if (playerTransform != null)
        {
            CharacterController cc = playerTransform.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            playerTransform.position = initialSpawnPosition;
            playerTransform.rotation = initialSpawnRotation;

            if (cc != null) cc.enabled = true;
        }

        SetGrayscaleAmount(0f);
        isCountingDown = false;

        yield return new WaitForSeconds(0.2f); // Pausa breve en negro

        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.SetOutsideBoundary(false);
        }

        // 3. Realizar Fade In (volver a la vista normal)
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