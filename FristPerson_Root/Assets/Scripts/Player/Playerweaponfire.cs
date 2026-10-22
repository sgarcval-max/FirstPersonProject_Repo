using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

/// <summary>
/// Hace que las armas a distancia (la pistola) disparen al pulsar la tecla de atacar. El disparo
/// es un rayo que sale del centro de la cámara: si da en un guardia, lo aturde. Cada arma solo
/// dispara una vez y tarda un rato en recargar (WeaponData.rechargeTime); el tiempo de recarga
/// sigue corriendo aunque sueltes el arma.
///
/// Usa la acción "Attack" del mapa "Combate" si existe, y si no, la acción "Throw" que ya tenías
/// (clic izquierdo, o la tecla que le hayas asignado).
/// </summary>
[RequireComponent(typeof(FirstPersonController))]
[RequireComponent(typeof(PlayerWeaponInventory))]
public class PlayerWeaponFire : MonoBehaviour
{
    [Header("Disparo")]
    [Tooltip("La cámara del jugador. Si se deja vacío, usa la cámara principal.")]
    [SerializeField] private Transform cameraTransform;
    [Tooltip("Opcional: de dónde sale el trazador visual del disparo (por ejemplo el HandPoint). Si se deja vacío, sale de la derecha de la cámara.")]
    [SerializeField] private Transform muzzlePoint;

    [Header("Trazador visual (provisional)")]
    [SerializeField] private Color tracerColor = new Color(1f, 0.9f, 0.4f, 1f);
    [SerializeField] private float tracerDuration = 0.06f;
    [Tooltip("Opcional: el material del trazador. Si se deja vacío, copia el material del arma que llevas, que seguro funciona en la build.")]
    [SerializeField] private Material tracerMaterial;

    [Header("Eventos")]
    [Tooltip("Se dispara cada vez que disparas (para sonidos o efectos futuros).")]
    [SerializeField] public UnityEvent OnFired;
    [Tooltip("Se dispara si intentas disparar mientras el arma aún está recargando (por ejemplo, para el sonido de 'clic').")]
    [SerializeField] public UnityEvent OnTriedWhileRecharging;

    private FirstPersonController playerController;
    private PlayerWeaponInventory inventory;
    private InputAction attackAction;
    private bool searchedForAction;
    private bool wasGameplayActive;

    // Momento (Time.time) en el que cada arma vuelve a estar lista.
    private readonly Dictionary<WeaponData, float> readyAt = new Dictionary<WeaponData, float>();

    private void Awake()
    {
        playerController = GetComponent<FirstPersonController>();
        inventory = GetComponent<PlayerWeaponInventory>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        // En pausa, atrapado o escondido, los controles están desactivados: no se dispara.
        if (!playerController.enabled)
        {
            wasGameplayActive = false;
            return;
        }

        // Con un panel abierto (inventario), el clic es para los botones, no para disparar.
        if (playerController.UiFocused) return;

        // El primer fotograma tras recuperar el control lo ignoramos: así el clic que pulsa el
        // botón Reanudar del menú no se convierte en un disparo.
        if (!wasGameplayActive)
        {
            wasGameplayActive = true;
            return;
        }

        if (attackAction == null)
        {
            if (searchedForAction) return;

            searchedForAction = true;
            InputActionAsset asset = InputManager.Controls.asset;
            attackAction = asset.FindAction("Combate/Attack");
            if (attackAction == null) attackAction = asset.FindAction("Combate/Throw");

            if (attackAction == null)
            {
                Debug.LogWarning("PlayerWeaponFire: no existe ninguna acción 'Attack' ni 'Throw' en el mapa 'Combate' de PlayerControls.");
                return;
            }
        }

        if (attackAction.WasPressedThisFrame())
        {
            TryFire();
        }
    }

    /// <summary>Segundos que le faltan al arma para poder volver a usarse (0 = lista).</summary>
    public float GetRemainingCooldown(WeaponData weapon)
    {
        float ready;
        if (weapon != null && readyAt.TryGetValue(weapon, out ready))
        {
            return Mathf.Max(0f, ready - Time.time);
        }
        return 0f;
    }

    private void TryFire()
    {
        WeaponData weapon = inventory.EquippedWeapon;
        if (weapon == null || !weapon.isRanged) return;

        if (GetRemainingCooldown(weapon) > 0f)
        {
            if (OnTriedWhileRecharging != null) OnTriedWhileRecharging.Invoke();
            return;
        }

        readyAt[weapon] = Time.time + weapon.rechargeTime;
        Fire(weapon);
    }

    private void Fire(WeaponData weapon)
    {
        if (cameraTransform == null) return;

        Vector3 origin = cameraTransform.position;
        Vector3 direction = cameraTransform.forward;
        Vector3 endPoint = origin + direction * weapon.range;

        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, weapon.range, ~0, QueryTriggerInteraction.Ignore))
        {
            endPoint = hit.point;

            HumanStun target = hit.collider.GetComponentInParent<HumanStun>();
            if (target != null)
            {
                target.Stun(weapon.stunDuration);
                Debug.Log("[" + weapon.GetDisplayName() + "] Le has dado a " + target.name + ".");
            }
        }

        Vector3 tracerStart = muzzlePoint != null
            ? muzzlePoint.position
            : origin + cameraTransform.right * 0.25f - cameraTransform.up * 0.2f + direction * 0.5f;
        SpawnTracer(tracerStart, endPoint, weapon);

        // Un disparo suena: los guardias cercanos que no estén aturdidos irán a investigar.
        if (weapon.shotNoiseRadius > 0f)
        {
            NoiseSystem.Emit(transform.position, weapon.shotNoiseRadius);
        }

        // --- SONIDO DE DISPARO CENTRALIZADO ---
        if (UIAudioManager.Instance != null)
        {
            UIAudioManager.Instance.PlayWeaponShoot();
        }

        if (OnFired != null) OnFired.Invoke();
    }

    private void SpawnTracer(Vector3 from, Vector3 to, WeaponData weapon)
    {
        Vector3 delta = to - from;
        float length = delta.magnitude;
        if (length < 0.01f) return;

        GameObject tracer = GameObject.CreatePrimitive(PrimitiveType.Cube);

        Collider tracerCollider = tracer.GetComponent<Collider>();
        if (tracerCollider != null) Destroy(tracerCollider);

        tracer.transform.position = from + delta * 0.5f;
        tracer.transform.rotation = Quaternion.LookRotation(delta);
        tracer.transform.localScale = new Vector3(0.02f, 0.02f, length);

        Renderer tracerRenderer = tracer.GetComponent<Renderer>();
        if (tracerRenderer != null)
        {
            Material source = ResolveTracerMaterial(weapon);
            if (source != null)
            {
                Material instance = new Material(source);

                if (instance.HasProperty("_BaseColor")) instance.SetColor("_BaseColor", tracerColor);
                else if (instance.HasProperty("_Color")) instance.SetColor("_Color", tracerColor);

                tracerRenderer.sharedMaterial = instance;
                Destroy(instance, tracerDuration + 0.1f);
            }
        }

        Destroy(tracer, tracerDuration);
    }

    private Material ResolveTracerMaterial(WeaponData weapon)
    {
        if (tracerMaterial != null) return tracerMaterial;

        if (weapon != null && weapon.worldPrefab != null)
        {
            Renderer weaponRenderer = weapon.worldPrefab.GetComponentInChildren<Renderer>(true);
            if (weaponRenderer != null) return weaponRenderer.sharedMaterial;
        }

        return null;
    }
}