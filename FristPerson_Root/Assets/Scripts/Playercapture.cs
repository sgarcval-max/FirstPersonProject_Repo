using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Gestiona el momento en que un guardia atrapa al jugador. Un guardia llama a BeginCapture
/// cuando su barra de detección llega al máximo: el jugador pierde el control, su cámara gira
/// hacia el guardia durante un momento, y después se congela el juego y se avisa con OnCaught
/// para que salga el panel de Game Over.
/// </summary>
[RequireComponent(typeof(FirstPersonController))]
public class PlayerCapture : MonoBehaviour
{
    public static PlayerCapture Instance { get; private set; }

    /// <summary>True desde que un guardia empieza a atrapar al jugador. La pausa y otros guardias lo consultan.</summary>
    public static bool IsCaught { get; private set; }

    [Header("Captura")]
    [Tooltip("Segundos que pasan desde que el guardia te detecta del todo hasta que sale el Game Over.")]
    [SerializeField] private float captureDuration = 1.2f;

    /// <summary>Se dispara cuando termina la captura y debe mostrarse el Game Over.</summary>
    public event Action OnCaught;

    private FirstPersonController controller;

    private void Awake()
    {
        Instance = this;
        IsCaught = false; // es estática y sobrevive a recargar la escena, así que se resetea aquí
        controller = GetComponent<FirstPersonController>();
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

        // Fin de la captura: se congela el juego y se muestra el cursor para el Game Over.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;

        Debug.Log("[Jugador] Atrapado.");

        if (OnCaught != null) OnCaught.Invoke();
    }

    private static float NormalizeAngle(float angle)
    {
        return angle > 180f ? angle - 360f : angle;
    }
}
