using UnityEngine;

/// <summary>
/// Comportamiento básico de cualquier objeto lanzable (comida, por ahora).
/// Usa física real (Rigidbody) para que vuele y rebote de forma natural, y se
/// autodestruye pasado un tiempo para no acumular objetos por el escenario.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ThrowableProjectile : MonoBehaviour
{
    [Tooltip("Segundos que tarda en desaparecer tras ser lanzado.")]
    [SerializeField] private float lifeTime = 5f;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }
}
