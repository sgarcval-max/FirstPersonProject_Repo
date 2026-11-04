using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// UnityEvent que lleva el arma equipada como parámetro (null = manos vacías).
/// Hace falta esta clase intermedia porque un UnityEvent normal no admite parámetros
/// personalizados en el Inspector.
/// </summary>
[System.Serializable]
public class WeaponChangedEvent : UnityEvent<WeaponData> { }

/// <summary>
/// Gestiona las armas del jugador: tiene 3 "huecos" en total (manos vacías y 2 armas).
/// Permite cambiar de hueco con animación procedural de guardado/sacado (Holster/Draw),
/// recoge y suelta armas, y gestiona capas dinámicas (WeaponOverlay / Default).
/// </summary>
public class PlayerWeaponInventory : MonoBehaviour
{
    private const int SLOT_COUNT = 2;
    private const int HANDS = -1;

    [Header("Inicio")]
    [Tooltip("Arma con la que empieza el jugador, ya equipada (ej: la pistola).")]
    [SerializeField] private WeaponData startingWeapon;

    [Header("Arma en la mano")]
    [Tooltip("Punto de la cámara donde aparece el arma equipada (un objeto vacío hijo de la cámara, abajo a la derecha).")]
    [SerializeField] private Transform handPoint;

    [Header("Animación de Guardar / Sacar Arma")]
    [Tooltip("Tiempo en segundos que tarda el arma en bajarse o subirse.")]
    [SerializeField] private float equipDuration = 0.2f;

    [Tooltip("Desplazamiento hacia abajo para ocultar el arma fuera de la vista de la cámara.")]
    [SerializeField] private Vector3 holsterOffset = new Vector3(0f, -0.6f, -0.1f);

    [Header("Soltar armas")]
    [Tooltip("Punto delante del jugador donde aparece el arma al soltarla (un objeto vacío hijo de la cámara).")]
    [SerializeField] private Transform dropPoint;
    [SerializeField] private float dropForce = 3f;

    [Header("Capa de Cámara de Armas")]
    [Tooltip("Nombre exacto de la capa asignada exclusivamente a la cámara secundaria del arma.")]
    [SerializeField] private string weaponOverlayLayerName = "WeaponOverlay";

    [Header("Eventos")]
    [Tooltip("Se dispara cada vez que cambia lo que lleva el jugador en la mano (null = manos vacías).")]
    public WeaponChangedEvent OnEquippedChanged;

    private readonly WeaponData[] slots = new WeaponData[SLOT_COUNT];
    private int equippedIndex = HANDS;
    private GameObject heldInstance;

    private Coroutine switchCoroutine;
    private bool isTransitioning = false;

    /// <summary>El arma que lleva en la mano ahora mismo, o null si tiene las manos vacías.</summary>
    public WeaponData EquippedWeapon
    {
        get { return equippedIndex == HANDS ? null : slots[equippedIndex]; }
    }

    private void Start()
    {
        if (startingWeapon != null)
        {
            slots[0] = startingWeapon;
            equippedIndex = 0;
        }

        // Carga inicial instantánea sin animación al arrancar la escena
        InstantRefreshHeldModel();
        if (OnEquippedChanged != null) OnEquippedChanged.Invoke(EquippedWeapon);
    }

    /// <summary>Pasa a lo siguiente: manos, arma 1, arma 2, manos... saltándose los huecos vacíos.</summary>
    public void CycleEquipped()
    {
        // Evitar múltiples pulsaciones seguidas mientras se está guardando/sacando un arma
        if (isTransitioning) return;

        int total = SLOT_COUNT + 1;
        int position = equippedIndex + 1; // manos = 0, hueco 0 = 1, hueco 1 = 2

        for (int step = 1; step <= total; step++)
        {
            int candidate = ((position + step) % total) - 1;
            if (candidate == HANDS || slots[candidate] != null)
            {
                SetEquipped(candidate, false);
                return;
            }
        }
    }

    /// <summary>
    /// Intenta guardar un arma recogida del suelo. Devuelve true si lo consigue.
    /// </summary>
    public bool TryPickup(WeaponData weapon)
    {
        if (weapon == null) return false;

        for (int i = 0; i < SLOT_COUNT; i++)
        {
            if (slots[i] == null)
            {
                slots[i] = weapon;
                SetEquipped(i, true);
                return true;
            }
        }

        if (equippedIndex != HANDS)
        {
            SpawnDroppedWeapon(slots[equippedIndex]);
            slots[equippedIndex] = weapon;
            SetEquipped(equippedIndex, true);
            return true;
        }

        return false;
    }

    /// <summary>Suelta al suelo el arma equipada. Con las manos vacías no hace nada.</summary>
    public bool DropEquipped()
    {
        if (equippedIndex == HANDS) return false;

        if (switchCoroutine != null) StopCoroutine(switchCoroutine);
        isTransitioning = false;

        WeaponData weapon = slots[equippedIndex];
        slots[equippedIndex] = null;
        SpawnDroppedWeapon(weapon);

        equippedIndex = HANDS;
        InstantRefreshHeldModel();
        if (OnEquippedChanged != null) OnEquippedChanged.Invoke(null);
        return true;
    }

    private void SetEquipped(int index, bool forceNotify)
    {
        if (index == equippedIndex && !forceNotify) return;

        if (switchCoroutine != null)
        {
            StopCoroutine(switchCoroutine);
        }

        switchCoroutine = StartCoroutine(SwitchWeaponRoutine(index));
    }

    /// <summary>
    /// Corrutina que anima el descenso del arma actual y el ascenso de la nueva.
    /// </summary>
    private IEnumerator SwitchWeaponRoutine(int targetIndex)
    {
        isTransitioning = true;

        // --- 1. ANIMACIÓN DE GUARDAR (BAJAR ARMA ACTUAL) ---
        if (heldInstance != null && EquippedWeapon != null)
        {
            Vector3 startPos = heldInstance.transform.localPosition;
            Vector3 targetPos = startPos + holsterOffset;
            float elapsed = 0f;

            while (elapsed < equipDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / equipDuration;
                t = Mathf.SmoothStep(0f, 1f, t); // Suavizado de entrada y salida

                if (heldInstance != null)
                {
                    heldInstance.transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
                }
                yield return null;
            }
        }

        // Destruir el modelo viejo al quedar oculto
        if (heldInstance != null)
        {
            Destroy(heldInstance);
            heldInstance = null;
        }

        // Cambiar inventario
        equippedIndex = targetIndex;
        WeaponData newWeapon = EquippedWeapon;

        // Notificar cambios de UI y Audio al cambiar de ítem
        if (OnEquippedChanged != null) OnEquippedChanged.Invoke(newWeapon);
        if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayButtonClick();

        // --- 2. ANIMACIÓN DE SACAR (SUBIR NUEVA ARMA) ---
        if (newWeapon != null && newWeapon.worldPrefab != null && handPoint != null)
        {
            heldInstance = Instantiate(newWeapon.worldPrefab, handPoint);
            ConfigureHeldInstance(newWeapon);

            Vector3 finalPos = newWeapon.heldLocalPosition;
            Vector3 startPos = finalPos + holsterOffset;

            heldInstance.transform.localPosition = startPos;
            heldInstance.transform.localEulerAngles = newWeapon.heldLocalEuler;

            float elapsed = 0f;
            while (elapsed < equipDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / equipDuration;
                t = Mathf.SmoothStep(0f, 1f, t);

                if (heldInstance != null)
                {
                    heldInstance.transform.localPosition = Vector3.Lerp(startPos, finalPos, t);
                }
                yield return null;
            }

            if (heldInstance != null)
            {
                heldInstance.transform.localPosition = finalPos;
            }
        }

        isTransitioning = false;
    }

    /// <summary>
    /// Cambia el modelo de forma instantánea sin animación (usado al iniciar o al soltar arma al suelo).
    /// </summary>
    private void InstantRefreshHeldModel()
    {
        if (heldInstance != null)
        {
            Destroy(heldInstance);
            heldInstance = null;
        }

        WeaponData weapon = EquippedWeapon;
        if (weapon == null || weapon.worldPrefab == null || handPoint == null) return;

        heldInstance = Instantiate(weapon.worldPrefab, handPoint);
        heldInstance.transform.localPosition = weapon.heldLocalPosition;
        heldInstance.transform.localEulerAngles = weapon.heldLocalEuler;

        ConfigureHeldInstance(weapon);
    }

    /// <summary>
    /// Aplica capas de renderizado exclusivo y desactiva componentes físicos del modelo visual de la mano.
    /// </summary>
    private void ConfigureHeldInstance(WeaponData weapon)
    {
        if (heldInstance == null) return;

        int overlayLayer = LayerMask.NameToLayer(weaponOverlayLayerName);
        if (overlayLayer != -1)
        {
            SetLayerRecursively(heldInstance, overlayLayer);
        }
        else
        {
            Debug.LogWarning($"PlayerWeaponInventory: La capa '{weaponOverlayLayerName}' no existe en Unity (Edit > Project Settings > Tags and Layers).");
        }

        foreach (Collider col in heldInstance.GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }
        foreach (Rigidbody body in heldInstance.GetComponentsInChildren<Rigidbody>())
        {
            body.isKinematic = true;
        }
        foreach (WeaponPickup pickup in heldInstance.GetComponentsInChildren<WeaponPickup>())
        {
            pickup.enabled = false;
        }
    }

    /// <summary>
    /// Suelta TODAS las armas al suelo y deja al jugador con las manos vacías.
    /// </summary>
    public void DropAllAt(Vector3 center, float radius)
    {
        if (switchCoroutine != null) StopCoroutine(switchCoroutine);
        isTransitioning = false;

        for (int i = 0; i < SLOT_COUNT; i++)
        {
            if (slots[i] == null) continue;

            SpawnWeaponOnGround(slots[i], center, radius);
            slots[i] = null;
        }

        equippedIndex = HANDS;
        InstantRefreshHeldModel();
        if (OnEquippedChanged != null) OnEquippedChanged.Invoke(null);
    }

    private void SpawnWeaponOnGround(WeaponData weapon, Vector3 center, float radius)
    {
        if (weapon.worldPrefab == null)
        {
            Debug.LogWarning("PlayerWeaponInventory: el arma " + weapon.name + " no tiene World Prefab, desaparece al soltarla.");
            return;
        }

        Vector3 position = DropUtility.GetGroundPoint(center, radius);
        Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

        GameObject dropped = Instantiate(weapon.worldPrefab, position, rotation);

        int defaultLayer = LayerMask.NameToLayer("Default");
        SetLayerRecursively(dropped, defaultLayer != -1 ? defaultLayer : 0);

        Rigidbody rb = dropped.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
    }

    private void SpawnDroppedWeapon(WeaponData weapon)
    {
        if (weapon == null) return;

        if (weapon.worldPrefab == null || dropPoint == null)
        {
            Debug.LogWarning("PlayerWeaponInventory: falta el World Prefab del arma o el Drop Point, el arma desaparece al soltarla.");
            return;
        }

        GameObject dropped = Instantiate(weapon.worldPrefab, dropPoint.position, dropPoint.rotation);

        int defaultLayer = LayerMask.NameToLayer("Default");
        SetLayerRecursively(dropped, defaultLayer != -1 ? defaultLayer : 0);

        if (dropped.GetComponent<NoiseOnImpact>() == null)
        {
            dropped.AddComponent<NoiseOnImpact>();
        }

        Rigidbody rb = dropped.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.AddForce(dropPoint.forward * dropForce, ForceMode.VelocityChange);
        }
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
}