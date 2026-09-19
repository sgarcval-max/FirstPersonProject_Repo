using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Controlador de personaje en primera persona usando el New Input System.
/// Lee las acciones del Action Map "Movimiento" del asset PlayerControls
/// (generado a partir de PlayerControls.inputactions con "Generate C# Class").
/// Mantiene los mismos UnityEvents que antes para no romper nada ya conectado en el Inspector.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform cameraTransform;

    [Header("Movimiento")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float runSpeed = 7f;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Cámara / Ratón")]
    [SerializeField] private float mouseSensitivity = 50f;
    [SerializeField] private bool invertY = false;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    [Header("Eventos - Movimiento")]
    public UnityEvent OnStartMoving;
    public UnityEvent OnStopMoving;
    public UnityEvent OnStartRunning;
    public UnityEvent OnStopRunning;

    [Header("Eventos - Salto / Suelo")]
    public UnityEvent OnJump;
    public UnityEvent OnLanded;

    [Header("Eventos - Pasos")]
    [SerializeField] private float stepDistance = 2f;
    public UnityEvent OnFootstep;

    private CharacterController controller;

    private Vector3 velocity;
    private float pitch = 0f;
    private bool wasMovingLastFrame = false;
    private bool wasRunningLastFrame = false;
    private bool wasGroundedLastFrame = true;
    private float distanceSinceLastStep = 0f;

    // Acceso directo a los controles compartidos (una sola instancia para todo el juego)
    private PlayerControls controls => InputManager.Controls;

    // Multiplicador extra para que el Mouse Delta del Input System se sienta similar a antes
    private const float LOOK_SCALE = 0.02f;

    public float MouseSensitivity
    {
        get => mouseSensitivity;
        set => mouseSensitivity = value;
    }

    public bool InvertY
    {
        get => invertY;
        set => invertY = value;
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void OnEnable()
    {
        controls.Movimiento.Enable();
    }

    private void OnDisable()
    {
        controls.Movimiento.Disable();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleMouseLook();
        HandleMovement();
    }

    private void HandleMouseLook()
    {
        Vector2 lookInput = controls.Movimiento.Look.ReadValue<Vector2>();

        float mouseX = lookInput.x * mouseSensitivity * LOOK_SCALE;
        float mouseY = lookInput.y * mouseSensitivity * LOOK_SCALE;

        if (invertY)
        {
            mouseY *= -1f;
        }

        transform.Rotate(Vector3.up * mouseX);

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void HandleMovement()
    {
        bool isGrounded = controller.isGrounded;

        if (isGrounded && !wasGroundedLastFrame)
        {
            OnLanded?.Invoke();
        }
        wasGroundedLastFrame = isGrounded;

        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        Vector2 moveInput = controls.Movimiento.Move.ReadValue<Vector2>();
        bool isRunning = controls.Movimiento.Run.IsPressed();

        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        move = Vector3.ClampMagnitude(move, 1f);

        float currentSpeed = isRunning ? runSpeed : walkSpeed;
        controller.Move(move * currentSpeed * Time.deltaTime);

        bool isMoving = move.sqrMagnitude > 0.001f;

        if (isMoving && !wasMovingLastFrame) OnStartMoving?.Invoke();
        if (!isMoving && wasMovingLastFrame) OnStopMoving?.Invoke();
        wasMovingLastFrame = isMoving;

        bool runningNow = isMoving && isRunning;
        if (runningNow && !wasRunningLastFrame) OnStartRunning?.Invoke();
        if (!runningNow && wasRunningLastFrame) OnStopRunning?.Invoke();
        wasRunningLastFrame = runningNow;

        if (isMoving && isGrounded)
        {
            distanceSinceLastStep += currentSpeed * Time.deltaTime;
            if (distanceSinceLastStep >= stepDistance)
            {
                distanceSinceLastStep = 0f;
                OnFootstep?.Invoke();
            }
        }

        if (controls.Movimiento.Jump.WasPressedThisFrame() && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            OnJump?.Invoke();
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}