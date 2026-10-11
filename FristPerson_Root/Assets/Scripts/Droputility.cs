using UnityEngine;

/// <summary>
/// Utilidad para soltar objetos en el suelo alrededor de un punto (por ejemplo, donde atrapan
/// al jugador). Elige posiciones al azar en un círculo y las baja hasta el suelo con un rayo,
/// para que los objetos aparezcan ya sobre el suelo y no flotando.
/// </summary>
public static class DropUtility
{
    private const float MIN_DISTANCE = 0.7f; // fuera del cuerpo del jugador, que mide unos 0.5 de radio
    private const float RAY_START_HEIGHT = 3f;
    private const float RAY_LENGTH = 10f;
    private const float HEIGHT_ABOVE_GROUND = 0.15f;

    /// <summary>Devuelve un punto sobre el suelo, a entre 0.7 metros y "radius" metros de "center".</summary>
    public static Vector3 GetGroundPoint(Vector3 center, float radius)
    {
        float distance = Random.Range(MIN_DISTANCE, Mathf.Max(MIN_DISTANCE, radius));
        float angle = Random.Range(0f, Mathf.PI * 2f);

        Vector3 flat = center + new Vector3(Mathf.Cos(angle) * distance, 0f, Mathf.Sin(angle) * distance);
        Vector3 rayStart = flat + Vector3.up * RAY_START_HEIGHT;

        RaycastHit hit;
        if (Physics.Raycast(rayStart, Vector3.down, out hit, RAY_LENGTH, ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.point + Vector3.up * HEIGHT_ABOVE_GROUND;
        }

        // Si no encuentra suelo, lo deja a la altura del centro.
        return flat + Vector3.up * HEIGHT_ABOVE_GROUND;
    }
}