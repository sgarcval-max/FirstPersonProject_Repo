using UnityEngine;

/// <summary>
/// Cualquier objeto del mundo con el que el jugador pueda interactuar (armas del suelo,
/// objetos clave, puertas más adelante...) implementa esta interfaz. El jugador mira lo
/// que tiene delante y, si es IInteractable, muestra su aviso y lo activa al pulsar Interactuar.
/// </summary>
public interface IInteractable
{
    /// <summary>Texto del aviso en pantalla, ya traducido (ej: "Recoger Pistola").</summary>
    string GetPrompt();

    /// <summary>Se llama cuando el jugador pulsa Interactuar mirando a este objeto.</summary>
    void Interact(GameObject player);
}
