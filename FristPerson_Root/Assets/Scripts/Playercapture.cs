using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Gestiona cuando un guardia atrapa al jugador y lo que viene después.
///
/// Al ser atrapado: el jugador pierde el control, su cámara gira hacia el guardia, y todo lo que
/// llevaba (armas e inventario de objetos clave) cae al suelo en ese sitio. Después se congela
/// el juego y se avisa con OnCaught para que salga el Game Over.
///
/// Al reiniciar (RespawnWithFade): pantalla a negro, el jugador reaparece en su punto de inicio,
/// los guardias vuelven a su ruta, y lo que soltó se queda donde lo atraparon.
/// </summary>
[RequireComponent(typeof(FirstPersonController))]
[RequireComponent(typeof(CharacterController))]
public class PlayerCapture : MonoBehaviour
{
    public static PlayerCapture Instance { get; private set; }

    /// <summary>True desde que un guardia empieza a atrapar al jugador. La pausa y otros guardias lo consultan.</summary>
    public static bool IsCaught { get; private set; }

    [Header("Captura")]
    [Tooltip("Segundos que pasan desde que el guardia te detecta del todo hasta que sale el Game Over.")]
    [SerializeField] private float captureDuration = 1.2f;

    [Header("Al ser atrapado")]
    [Tooltip("A qué distancia máxima del sitio donde te atrapan caen tus cosas.")]
    [SerializeField] private float dropRadius = 1.5f;

    /// <summary>Se dispara cuando termina la captura y debe mostrarse el Game Over.</summary>
    public event Action OnCaught;

    /// <summary>Se dispara cuando el jugador ha reaparecido tras un Game Over.</summary>
    public event Action OnRespawned;

    private FirstPersonController controller;
    private CharacterController characterController;
    private PlayerWeaponInventory weapons;
    private PlayerKeyItemInventory keyItems;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private Vector3 caughtPosition;

    private void Awake()
    {
        Instance = this;
        IsCaught = false; // es estática y sobrevive a recargar la escena, así que se resetea aquí

        controller = GetComponent<FirstPersonController>();
        characterController = GetComponent<CharacterController>();
        weapons = GetComponent<PlayerWeaponInventory>();
        keyItems = GetComponent<PlayerKeyItemInventory>();

        // El punto donde empieza el jugador es al que volverá tras un Game Over.
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>Lo llama el guardia que acaba de detectar al jugador del todo.</summary>
    public void BeginCapture(Transform guard)
    {
        if (IsCaught) return;

        IsCaught = true;
        caughtPosition = transform.position;

        // Desactivar FirstPersonController bloquea el movimiento, la cámara y también las
        // acciones de armas e inventario (que comprueban si está activo).
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

            // La cámara gira poco a poco hacia la cara del guardia.
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

        // Todo lo que llevaba cae al suelo en el sitio donde lo han atrapado.
        DropEverything();

        // Fin de la captura: se congela el juego y se muestra el cursor para el Game Over.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;

        Debug.Log("[Jugador] Atrapado.");

        if (OnCaught != null) OnCaught.Invoke();
    }

    private void DropEverything()
    {
        if (weapons != null) weapons.DropAllAt(caughtPosition, dropRadius);
        if (keyItems != null) keyItems.DropAllAt(caughtPosition, dropRadius);
    }

    /// <summary>Conectado al botón Reiniciar del Game Over: reaparece con un fundido a negro.</summary>
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
        // 1) El jugador vuelve al punto de inicio (el CharacterController hay que apagarlo para teletransportarlo).
        characterController.enabled = false;
        transform.SetPositionAndRotation(startPosition, startRotation);
        characterController.enabled = true;

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.transform.localRotation = Quaternion.identity;
        }

        // 2) Los guardias vuelven a su sitio y a su ruta, tranquilos.
        foreach (HumanAI guard in FindObjectsOfType<HumanAI>())
        {
            guard.ResetToStart();
        }

        // 3) Se reanuda el juego.
        Time.timeScale = 1f;
        IsCaught = false;
        controller.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (OnRespawned != null) OnRespawned.Invoke();
    }

    private static float NormalizeAngle(float angle)
    {
        return angle > 180f ? angle - 360f : angle;
    }
}