using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Escondite interactivo con desplazamiento y rotación suave de entrada/salida.
/// </summary>
public class HidingSpot : MonoBehaviour, IInteractable
{
    [Header("Dentro del escondite")]
    [Tooltip("Dónde se coloca el jugador al entrar (a la altura del suelo, dentro del escondite). Su eje azul (Z) debe apuntar hacia donde mirará, normalmente hacia fuera por la puerta.")]
    [SerializeField] private Transform hidePoint;

    [Header("Transición de Movimiento")]
    [Tooltip("Duración en segundos del movimiento al entrar y salir del escondite.")]
    [SerializeField] private float transitionDuration = 0.45f;

    [Header("Cuánto te protege")]
    [Range(0f, 1f)]
    [SerializeField] private float sightMultiplier = 0f;
    [Range(0f, 1f)]
    [SerializeField] private float noiseMultiplier = 0f;

    /// <summary>True mientras el jugador está dentro o entrando al escondite.</summary>
    public bool IsOccupied { get; private set; }

    private GameObject player;
    private FirstPersonController controller;
    private CharacterController characterController;
    private PlayerConcealment concealment;
    private InputAction interactAction;

    private Vector3 returnPosition;
    private Quaternion returnRotation;
    private int enterFrame;
    private bool isTransitioning;
    private Coroutine currentTransition;

    public string GetPrompt()
    {
        bool english = GameLanguage.IsEnglish;

        if (IsOccupied)
        {
            return english ? "Leave" : "Salir";
        }
        return english ? "Hide" : "Esconderse";
    }

    public void Interact(GameObject playerObject)
    {
        if (IsOccupied || isTransitioning) return;
        Enter(playerObject);
    }

    private void Enter(GameObject playerObject)
    {
        if (hidePoint == null)
        {
            Debug.LogWarning("HidingSpot (" + name + "): falta asignar el Hide Point.");
            return;
        }

        player = playerObject;
        controller = player.GetComponent<FirstPersonController>();
        characterController = player.GetComponent<CharacterController>();
        concealment = player.GetComponent<PlayerConcealment>();

        if (controller == null || characterController == null || concealment == null)
        {
            Debug.LogWarning("HidingSpot (" + name + "): el Player necesita el componente Player Concealment.");
            return;
        }

        returnPosition = player.transform.position;
        returnRotation = player.transform.rotation;

        IsOccupied = true;
        enterFrame = Time.frameCount;

        // Bloqueamos los controles del jugador inmediatamente
        controller.enabled = false;
        concealment.SetSource(this, sightMultiplier, noiseMultiplier);

        if (UIAudioManager.Instance != null)
        {
            UIAudioManager.Instance.PlayPanelOpen();
        }

        // Iniciar desplazamiento suave hacia el interior
        Quaternion targetRotation = Quaternion.Euler(0f, hidePoint.eulerAngles.y, 0f);

        if (currentTransition != null) StopCoroutine(currentTransition);
        currentTransition = StartCoroutine(MovePlayerRoutine(hidePoint.position, targetRotation, true));
    }

    private void Update()
    {
        if (!IsOccupied) return;

        // Con la partida terminada, el jugador se queda donde está
        if (MatchManager.IsMatchOver) return;

        // Si te atrapan estando dentro o entrando
        if (PlayerCapture.IsCaught)
        {
            if (currentTransition != null) StopCoroutine(currentTransition);
            isTransitioning = false;
            concealment.RemoveSource(this);
            IsOccupied = false;
            return;
        }

        // En pausa no procesamos interacción
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused) return;

        // Mantener deshabilitados los controles mientras se está escondido
        if (controller != null && controller.enabled)
        {
            controller.enabled = false;
        }

        // Si se está realizando el movimiento de entrada o salida, ignoramos pulsaciones
        if (isTransitioning) return;

        if (interactAction == null)
        {
            interactAction = InputManager.Controls.asset.FindAction("Combate/Interact");
        }

        // Evita salir en el mismo frame en el que entraste
        if (interactAction != null && Time.frameCount > enterFrame && interactAction.WasPressedThisFrame())
        {
            Exit();
        }
    }

    private void Exit()
    {
        if (isTransitioning) return;

        if (UIAudioManager.Instance != null)
        {
            UIAudioManager.Instance.PlayPanelClose();
        }

        if (currentTransition != null) StopCoroutine(currentTransition);
        currentTransition = StartCoroutine(MovePlayerRoutine(returnPosition, returnRotation, false, () =>
        {
            concealment.RemoveSource(this);
            controller.enabled = true;
            IsOccupied = false;
        }));
    }

    /// <summary>
    /// Desplaza y rota suavemente al jugador (y resetea la cámara si entra al escondite).
    /// </summary>
    private IEnumerator MovePlayerRoutine(Vector3 targetPos, Quaternion targetRot, bool isEntering, Action onComplete = null)
    {
        isTransitioning = true;
        characterController.enabled = false;

        Vector3 startPos = player.transform.position;
        Quaternion startRot = player.transform.rotation;

        Transform mainCam = Camera.main != null ? Camera.main.transform : null;
        Quaternion startCamRot = mainCam != null ? mainCam.localRotation : Quaternion.identity;

        float elapsedTime = 0f;

        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / transitionDuration);

            // Curva de aceleración/desaceleración suave
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            player.transform.position = Vector3.Lerp(startPos, targetPos, smoothT);
            player.transform.rotation = Quaternion.Slerp(startRot, targetRot, smoothT);

            // Al entrar, centramos suavemente la vista de la cámara hacia el frente del escondite
            if (isEntering && mainCam != null)
            {
                mainCam.localRotation = Quaternion.Slerp(startCamRot, Quaternion.identity, smoothT);
            }

            yield return null;
        }

        player.transform.SetPositionAndRotation(targetPos, targetRot);

        if (isEntering && mainCam != null)
        {
            mainCam.localRotation = Quaternion.identity;
        }

        characterController.enabled = true;
        isTransitioning = false;

        onComplete?.Invoke();
    }

    private void OnDestroy()
    {
        if (IsOccupied && concealment != null)
        {
            concealment.RemoveSource(this);
        }
    }
}