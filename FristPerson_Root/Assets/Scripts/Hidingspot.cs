using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Escondite en el que se entra con la tecla de interactuar (por ejemplo un armario). Al entrar,
/// el jugador se coloca en el punto de escondite, mirando hacia fuera, y no puede moverse. Pulsando
/// Interactuar otra vez sale y vuelve al sitio donde estaba.
///
/// Cuánto te protege se configura con dos valores:
///   Sight Multiplier: 0 = nadie te ve; 0.3 = te ven solo desde muy cerca; 1 = no te protege.
///   Noise Multiplier: 0 = nadie te oye; 1 = tu ruido suena con normalidad.
/// </summary>
public class HidingSpot : MonoBehaviour, IInteractable
{
    [Header("Dentro del escondite")]
    [Tooltip("Dónde se coloca el jugador al entrar (a la altura del suelo, dentro del escondite). Su eje azul (Z) debe apuntar hacia donde mirará, normalmente hacia fuera por la puerta.")]
    [SerializeField] private Transform hidePoint;

    [Header("Cuánto te protege")]
    [Range(0f, 1f)]
    [SerializeField] private float sightMultiplier = 0f;
    [Range(0f, 1f)]
    [SerializeField] private float noiseMultiplier = 0f;

    /// <summary>True mientras el jugador está dentro.</summary>
    public bool IsOccupied { get; private set; }

    private GameObject player;
    private FirstPersonController controller;
    private CharacterController characterController;
    private PlayerConcealment concealment;
    private InputAction interactAction;

    private Vector3 returnPosition;
    private Quaternion returnRotation;
    private int enterFrame;

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
        if (IsOccupied) return;
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

        // Bloqueamos los controles y colocamos al jugador dentro, mirando hacia fuera.
        controller.enabled = false;
        Teleport(hidePoint.position, Quaternion.Euler(0f, hidePoint.eulerAngles.y, 0f));

        if (Camera.main != null)
        {
            Camera.main.transform.localRotation = Quaternion.identity;
        }

        concealment.SetSource(this, sightMultiplier, noiseMultiplier);

        IsOccupied = true;
        enterFrame = Time.frameCount;

        if (UIAudioManager.Instance != null)
        {
            UIAudioManager.Instance.PlayPanelOpen();
        }
    }

    private void Update()
    {
        if (!IsOccupied) return;

        // Si te atrapan estando dentro, soltamos el escondite sin mover al jugador:
        // PlayerCapture se encarga de todo lo que viene después.
        if (PlayerCapture.IsCaught)
        {
            concealment.RemoveSource(this);
            IsOccupied = false;
            return;
        }

        // En pausa no hacemos nada.
        if (PauseManager.Instance != null && PauseManager.Instance.IsPaused) return;

        // Al reanudar tras una pausa, PauseManager vuelve a activar los controles: los bloqueamos otra vez.
        if (controller.enabled)
        {
            controller.enabled = false;
        }

        if (interactAction == null)
        {
            interactAction = InputManager.Controls.asset.FindAction("Combate/Interact");
        }

        // No contamos el mismo pulso que nos hizo entrar (enterFrame) para que no salgas al instante.
        if (interactAction != null && Time.frameCount > enterFrame && interactAction.WasPressedThisFrame())
        {
            Exit();
        }
    }

    private void Exit()
    {
        Teleport(returnPosition, returnRotation);

        concealment.RemoveSource(this);
        controller.enabled = true;
        IsOccupied = false;

        if (UIAudioManager.Instance != null)
        {
            UIAudioManager.Instance.PlayPanelClose();
        }
    }

    // El CharacterController hay que apagarlo un instante para poder teletransportar al jugador.
    private void Teleport(Vector3 position, Quaternion rotation)
    {
        characterController.enabled = false;
        player.transform.SetPositionAndRotation(position, rotation);
        characterController.enabled = true;
    }

    private void OnDestroy()
    {
        if (IsOccupied && concealment != null)
        {
            concealment.RemoveSource(this);
        }
    }
}