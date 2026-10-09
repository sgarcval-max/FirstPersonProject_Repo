using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Muestra u oculta el panel de inventario de objetos clave con la acción "Inventory" del
/// mapa "Combate" (Tab por defecto) y escribe en él la lista de lo recogido.
///
/// IMPORTANTE: este script debe ir en un objeto que esté SIEMPRE activo (por ejemplo el
/// Canvas del HUD), NO en el propio panel, porque si el panel se desactiva el script dejaría
/// de escuchar la tecla y no podría volver a abrirlo.
/// </summary>
public class KeyItemInventoryUI : MonoBehaviour
{
    [Tooltip("El panel que se muestra/oculta (fondo del inventario). Empieza oculto.")]
    [SerializeField] private GameObject panel;
    [Tooltip("El texto dentro del panel donde se escribe la lista.")]
    [SerializeField] private TMP_Text listText;

    private PlayerKeyItemInventory inventory;
    private FirstPersonController playerController;
    private InputAction toggleAction;

    private void OnEnable()
    {
        // Buscamos la acción por nombre. Si aún no existe en PlayerControls, avisamos en vez de fallar.
        toggleAction = InputManager.Controls.asset.FindAction("Combate/Inventory");

        if (toggleAction != null)
        {
            toggleAction.performed += OnTogglePerformed;
        }
        else
        {
            Debug.LogWarning("KeyItemInventoryUI: no existe la acción 'Inventory' en el mapa 'Combate' de PlayerControls. Créala y asígnale la tecla Tab.");
        }

        LocalizationManager.OnLanguageChanged += Refresh;
    }

    private void OnDisable()
    {
        if (toggleAction != null)
        {
            toggleAction.performed -= OnTogglePerformed;
        }
        LocalizationManager.OnLanguageChanged -= Refresh;
    }

    private void Start()
    {
        inventory = FindObjectOfType<PlayerKeyItemInventory>();
        playerController = FindObjectOfType<FirstPersonController>();

        if (inventory != null)
        {
            inventory.OnChanged += Refresh;
        }

        if (panel != null) panel.SetActive(false);
        Refresh();
    }

    private void OnDestroy()
    {
        if (inventory != null)
        {
            inventory.OnChanged -= Refresh;
        }
    }

    private void Update()
    {
        // Si el juego entra en pausa con el inventario abierto, lo cerramos.
        if (panel != null && panel.activeSelf && !IsGameplayActive())
        {
            panel.SetActive(false);
        }
    }

    private bool IsGameplayActive()
    {
        return playerController == null || playerController.enabled;
    }

    private void OnTogglePerformed(InputAction.CallbackContext context)
    {
        if (panel == null || !IsGameplayActive()) return;

        bool opening = !panel.activeSelf;
        panel.SetActive(opening);

        if (opening) Refresh();

        if (UIAudioManager.Instance != null)
        {
            if (opening) UIAudioManager.Instance.PlayPanelOpen();
            else UIAudioManager.Instance.PlayPanelClose();
        }
    }

    private void Refresh()
    {
        if (listText == null) return;

        bool english = GameLanguage.IsEnglish;
        StringBuilder text = new StringBuilder();

        text.Append("<b>").Append(english ? "Inventory" : "Inventario").Append("</b>\n\n");

        if (inventory == null || inventory.Items.Count == 0)
        {
            text.Append(english ? "Nothing found yet." : "Aún no has encontrado nada.");
        }
        else
        {
            foreach (KeyItemData item in inventory.Items)
            {
                text.Append("- ").Append(item.GetDisplayName()).Append('\n');
            }
        }

        listText.text = text.ToString();
    }
}
