using UnityEngine;

/// <summary>
/// Punto único y central de acceso a los controles del jugador (PlayerControls).
/// TODOS los demás scripts (FirstPersonController, PlayerThrowController, RebindActionUI,
/// etc.) deben usar InputManager.Controls en vez de crear su propio "new PlayerControls()".
///
/// IMPORTANTE: Controls se crea de forma "perezosa" (la primera vez que alguien lo pide),
/// no en Awake(). Esto es necesario porque Unity no garantiza en qué orden se ejecutan
/// los Awake()/OnEnable() de distintos objetos — si otro script accede a los controles
/// antes de que InputManager haya tenido su turno, con Awake() normal daría un error de
/// referencia nula. Con esta propiedad "perezosa", el primero que lo pide lo crea, sin
/// importar el orden.
/// </summary>
public class InputManager : MonoBehaviour
{
    private static PlayerControls controlsInstance;

    public static PlayerControls Controls
    {
        get
        {
            if (controlsInstance == null)
            {
                controlsInstance = new PlayerControls();
                RebindActionUI.LoadSavedRebinds(controlsInstance.asset);
            }
            return controlsInstance;
        }
    }

    private void OnEnable()
    {
        Controls.Enable();
    }

    private void OnDisable()
    {
        controlsInstance?.Disable();
    }
}