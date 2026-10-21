using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Gestiona la captura del jugador por los guardias y la reaparición tras Game Over.
/// </summary>
[RequireComponent(typeof(FirstPersonController))]
[RequireComponent(typeof(CharacterController))]
public class PlayerCapture : MonoBehaviour
{
    public static PlayerCapture Instance { get; private set; }
    public static bool IsCaught { get; private set; }

    [Header("Captura")]
    [SerializeField] private float captureDuration = 1.2f;

    [Header("Al ser atrapado")]
    [SerializeField] private float dropRadius = 1.5f;

    public event Action OnCaught;
    public event Action OnRespawned;

    private FirstPersonController controller;
    private CharacterController characterController;
    private PlayerWeaponInventory weapons;
    private PlayerKeyItemInventory keyItems;
    private PlayerCrouch crouch;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private Vector3 caughtPosition;

    private void Awake()
    {
        Instance = this;
        IsCaught = false;

        controller = GetComponent<FirstPersonController>();
        characterController = GetComponent<CharacterController>();
        weapons = GetComponent<PlayerWeaponInventory>();
        keyItems = GetComponent<PlayerKeyItemInventory>();
        crouch = GetComponent<PlayerCrouch>();

        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void BeginCapture(Transform guard)
    {
        if (IsCaught || MatchManager.IsMatchOver) return;

        IsCaught = true;
        caughtPosition = transform.position;
        controller.enabled = false;

        StartCoroutine(CaptureRoutine(guard));
    }

    private IEnumerator CaptureRoutine(Transform guard)
    {
        Transform cam = Camera.main != null ? Camera.main.transform : null;

        float startYaw = transform.eulerAngles.y;
        float startPitch = cam != null ? NormalizeAngle(cam.localEulerAngles.x) : 0f;
        float elapsed = 0f;

        while (elapsed < captureDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / captureDuration));

            if (guard != null && cam != null)
            {
                Vector3 toGuard = (guard.position + Vector3.up * 0.6f) - cam.position;
                if (toGuard.sqrMagnitude > 0.0001f)
                {
                    Quaternion look = Quaternion.LookRotation(toGuard);
                    float yaw = Mathf.LerpAngle(startYaw, look.eulerAngles.y, t);
                    float pitch = Mathf.Lerp(startPitch, NormalizeAngle(look.eulerAngles.x), t);

                    transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                    cam.localRotation = Quaternion.Euler(pitch, 0f, 0f);
                }
            }

            yield return null;
        }

        if (MatchManager.IsMatchOver) yield break;

        DropEverything();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;

        OnCaught?.Invoke();
    }

    private void DropEverything()
    {
        if (weapons != null) weapons.DropAllAt(caughtPosition, dropRadius);
        if (keyItems != null) keyItems.DropAllAt(caughtPosition, dropRadius);
    }

    public void RespawnWithFade()
    {
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.RunWithFade(PerformRespawn);
        }
        else
        {
            PerformRespawn();
        }
    }

    private void PerformRespawn()
    {
        Time.timeScale = 1f;
        IsCaught = false;

        // 1. Limpiar agachado
        if (crouch != null)
        {
            crouch.ResetCrouch();
        }

        // 2. Reposicionar en punto inicial
        characterController.enabled = false;
        transform.SetPositionAndRotation(startPosition, startRotation);
        characterController.enabled = true;

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.transform.localRotation = Quaternion.identity;
        }

        // 3. Limpiar físicas e inercia de movimiento
        if (controller != null)
        {
            controller.ResetState();
            controller.enabled = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        foreach (HumanAI guard in FindObjectsOfType<HumanAI>())
        {
            guard.ResetForRespawn();
        }

        OnRespawned?.Invoke();
    }

    private static float NormalizeAngle(float angle)
    {
        return angle > 180f ? angle - 360f : angle;
    }
}