using UnityEngine;

/// <summary>
/// Punto único y central de acceso a los controles del jugador (PlayerControls).
/// TODOS los demás scripts (FirstPersonController, RebindActionUI, etc.) deben usar
/// InputManager.Controls en vez de crear su propio "new PlayerControls()".
/// Esto es imprescindible para que el rebinding funcione: si cada script tuviera su propia
/// instancia, reasignar una tecla en una no afectaría a las demás (que es justo el bug que teníamos).
/// </summary>
public class InputManager : MonoBehaviour
{
    public static PlayerControls Controls { get; private set; }

    private void Awake()
    {
        // Si ya existe una instancia (por ejemplo al recargar escena), no crear otra
        if (Controls == null)
        {
            Controls = new PlayerControls();
            RebindActionUI.LoadSavedRebinds(Controls.asset);
        }
    }

    private void OnEnable()
    {
        Controls.Enable();
    }

    private void OnDisable()
    {
        Controls.Disable();
    }
}