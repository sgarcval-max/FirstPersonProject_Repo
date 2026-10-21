using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Genera objetos coleccionables en posiciones aleatorias al iniciar cada partida
/// seleccionándolas desde una lista de puntos candidatos sin repetir ubicación.
/// </summary>
public class RandomItemSpawner : MonoBehaviour
{
    [System.Serializable]
    public class ItemSpawnConfig
    {
        [Tooltip("Prefab del objeto coleccionable.")]
        public GameObject itemPrefab;

        [Tooltip("Cantidad de instancias de este objeto que deben aparecer en el mapa.")]
        public int amountToSpawn = 1;
    }

    [Header("Objetos a Generar")]
    [Tooltip("Configura aquí qué prefabs quieres que aparezcan y cuántas unidades de cada uno.")]
    [SerializeField] private List<ItemSpawnConfig> itemsToSpawn = new List<ItemSpawnConfig>();

    [Header("Puntos de Spawn Candidatos")]
    [Tooltip("Arrastra aquí todos los Transforms vacíos colocados por el escenario.")]
    [SerializeField] private List<Transform> candidateSpawnPoints = new List<Transform>();

    [Header("Visualización en Editor")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmoColor = Color.cyan;
    [SerializeField] private float gizmoRadius = 0.35f;

    private void Start()
    {
        SpawnItems();
    }

    /// <summary>
    /// Selecciona puntos aleatorios sin repetición e instancia los objetos.
    /// </summary>

    public void SpawnItems()
    {
        if (candidateSpawnPoints == null || candidateSpawnPoints.Count == 0)
        {
            Debug.LogWarning("[RandomItemSpawner] No hay puntos de spawn asignados en la lista.");
            return;
        }

        // Copiamos la lista de puntos para ir descartando los seleccionados y evitar duplicados en la misma posición
        List<Transform> availablePoints = new List<Transform>(candidateSpawnPoints);

        foreach (var config in itemsToSpawn)
        {
            if (config.itemPrefab == null) continue;

            for (int i = 0; i < config.amountToSpawn; i++)
            {
                if (availablePoints.Count == 0)
                {
                    Debug.LogWarning("[RandomItemSpawner] Se acabaron los puntos disponibles para generar más objetos.");
                    return;
                }

                // Elegir un índice aleatorio de la lista disponible
                int randomIndex = Random.Range(0, availablePoints.Count);
                Transform chosenPoint = availablePoints[randomIndex];

                // Instanciar el objeto en el punto seleccionado
                Instantiate(config.itemPrefab, chosenPoint.position, chosenPoint.rotation);

                // Remover el punto para que otro objeto no vuelva a salir ahí
                availablePoints.RemoveAt(randomIndex);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos || candidateSpawnPoints == null) return;

        Gizmos.color = gizmoColor;
        foreach (Transform point in candidateSpawnPoints)
        {
            if (point != null)
            {
                Gizmos.DrawSphere(point.position, gizmoRadius);
                Gizmos.DrawWireSphere(point.position, gizmoRadius * 1.2f);
            }
        }
    }
}
