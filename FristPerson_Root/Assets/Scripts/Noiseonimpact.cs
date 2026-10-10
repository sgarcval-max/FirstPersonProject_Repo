using UnityEngine;

/// <summary>
/// Hace ruido cuando este objeto choca con fuerza contra algo (por ejemplo, un arma soltada
/// que cae al suelo). Ignora los golpes suaves y no se repite mientras rueda.
/// PlayerWeaponInventory se lo añade solo a las armas que suelta.
/// </summary>
public class NoiseOnImpact : MonoBehaviour
{
    [SerializeField] private float radius = 7f;
    [Tooltip("Velocidad mínima del golpe para que haga ruido.")]
    [SerializeField] private float minImpactSpeed = 2f;
    [Tooltip("Segundos mínimos entre un ruido y el siguiente.")]
    [SerializeField] private float cooldown = 0.5f;

    private float lastNoiseTime = -999f;

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.relativeVelocity.magnitude < minImpactSpeed) return;
        if (Time.time - lastNoiseTime < cooldown) return;

        lastNoiseTime = Time.time;
        NoiseSystem.Emit(transform.position, radius);
    }
}