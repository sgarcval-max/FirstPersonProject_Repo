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
/// Gestiona las armas del jugador: tiene 3 "huecos" en total. Las manos (siempre disponibles,
/// no se pueden perder) y hasta 2 armas. Permite cambiar de hueco, recoger armas y soltarlas,
/// y muestra el arma equipada delante de la cámara.
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

    [Header("Soltar armas")]
    [Tooltip("Punto delante del jugador donde aparece el arma al soltarla (un objeto vacío hijo de la cámara).")]
    [SerializeField] private Transform dropPoint;
    [SerializeField] private float dropForce = 3f;

    [Header("Eventos")]
    [Tooltip("Se dispara cada vez que cambia lo que lleva el jugador en la mano (null = manos vacías).")]
    public WeaponChangedEvent OnEquippedChanged;

    private readonly WeaponData[] slots = new WeaponData[SLOT_COUNT];
    private int equippedIndex = HANDS;
    private GameObject heldInstance;

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

        RefreshHeldModel();
        if (OnEquippedChanged != null) OnEquippedChanged.Invoke(EquippedWeapon);
    }

    /// <summary>Pasa a lo siguiente: manos, arma 1, arma 2, manos... saltándose los huecos vacíos.</summary>
    public void CycleEquipped()
    {
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
    /// Si hay un hueco libre, la guarda y la equipa. Si los 2 huecos están llenos, se
    /// intercambia por el arma que lleves en la mano (que cae al suelo). Si los huecos están
    /// llenos y tienes las manos vacías, no se puede recoger.
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

        WeaponData weapon = slots[equippedIndex];
        slots[equippedIndex] = null;
        SpawnDroppedWeapon(weapon);
        SetEquipped(HANDS, false);
        return true;
    }

    private void SetEquipped(int index, bool forceNotify)
    {
        if (index == equippedIndex && !forceNotify) return;

        equippedIndex = index;
        RefreshHeldModel();
        if (OnEquippedChanged != null) OnEquippedChanged.Invoke(EquippedWeapon);

        if (UIAudioManager.Instance != null)
        {
            UIAudioManager.Instance.PlayButtonClick();
        }
    }

    /// <summary>Quita el modelo anterior de la mano y pone el del arma equipada (si hay).</summary>
    private void RefreshHeldModel()
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

        // El modelo de la mano es solo visual: sin colisiones, sin física y sin poder recogerse.
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

    private void SpawnDroppedWeapon(WeaponData weapon)
    {
        if (weapon == null) return;

        if (weapon.worldPrefab == null || dropPoint == null)
        {
            Debug.LogWarning("PlayerWeaponInventory: falta el World Prefab del arma o el Drop Point, el arma desaparece al soltarla.");
            return;
        }

        GameObject dropped = Instantiate(weapon.worldPrefab, dropPoint.position, dropPoint.rotation);

        Rigidbody rb = dropped.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(dropPoint.forward * dropForce, ForceMode.VelocityChange);
        }
    }
}