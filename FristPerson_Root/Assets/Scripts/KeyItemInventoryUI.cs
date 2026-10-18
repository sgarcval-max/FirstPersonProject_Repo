using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Inventario de objetos clave. Se abre y se cierra con la acción "Inventory" del mapa "Combate"
/// (Tab por defecto). Muestra una fila por cada objeto, con su nombre, sus puntos y un botón
/// "Soltar". Al soltar uno, su fila desaparece, las de debajo suben para no dejar huecos, y el
/// objeto cae al suelo delante del jugador (se puede volver a recoger).
///
/// Las filas se crean por código, no hay que montarlas en la escena.
///
/// Mientras el inventario está abierto aparece el cursor para poder pulsar los botones, y el jugador
/// no gira la cámara ni dispara (para que un clic en "Soltar" no sea un disparo). Sí puede andar.
///
/// IMPORTANTE: este script debe ir en un objeto que esté SIEMPRE activo (por ejemplo el Canvas del
/// HUD), NO en el propio panel, porque si el panel se desactiva el script dejaría de escuchar la
/// tecla y no podría volver a abrirlo.
/// </summary>
public class KeyItemInventoryUI : MonoBehaviour
{
    [Header("Panel")]
    [Tooltip("El panel que se muestra/oculta (fondo del inventario). Empieza oculto.")]
    [SerializeField] private GameObject panel;
    [Tooltip("El texto de arriba del panel: aquí se escribe el título y, si no hay nada, el aviso de inventario vacío.")]
    [SerializeField] private TMP_Text listText;

    [Header("Filas (se crean solas)")]
    [Tooltip("Espacio reservado arriba del panel para el título, antes de empezar las filas.")]
    [SerializeField] private float titleHeight = 90f;
    [SerializeField] private float rowHeight = 48f;
    [SerializeField] private float rowSpacing = 10f;
    [SerializeField] private float rowFontSize = 28f;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color pointsColor = new Color(1f, 0.85f, 0.3f, 1f);
    [SerializeField] private Color dropButtonColor = new Color(0.75f, 0.2f, 0.2f, 1f);
    [SerializeField] private float dropButtonWidth = 130f;

    private PlayerKeyItemInventory inventory;
    private FirstPersonController playerController;
    private InputAction toggleAction;
    private RectTransform listContainer;
    private readonly List<GameObject> rows = new List<GameObject>();

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

        BuildListContainer();

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
        // Si el juego entra en pausa (o te atrapan, o te escondes) con el inventario abierto, lo cerramos.
        // El cursor ya lo gestiona quien haya causado el cambio, así que no lo tocamos.
        if (panel != null && panel.activeSelf && !IsGameplayActive())
        {
            CloseInventory(false);
        }
    }

    private bool IsGameplayActive()
    {
        return playerController == null || playerController.enabled;
    }

    private void OnTogglePerformed(InputAction.CallbackContext context)
    {
        if (panel == null || !IsGameplayActive()) return;

        if (panel.activeSelf) CloseInventory(true);
        else OpenInventory();
    }

    private void OpenInventory()
    {
        panel.SetActive(true);
        Refresh();

        if (playerController != null) playerController.UiFocused = true;

        // Cursor libre para poder pulsar los botones "Soltar".
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayPanelOpen();
    }

    private void CloseInventory(bool relockCursor)
    {
        panel.SetActive(false);

        if (playerController != null) playerController.UiFocused = false;

        if (relockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayPanelClose();
    }

    // ------------------------------------------------------------------ Filas

    // Crea (una sola vez) el contenedor donde se colocan las filas, debajo del título del panel.
    private void BuildListContainer()
    {
        if (panel == null) return;

        GameObject containerObject = new GameObject("ItemList", typeof(RectTransform), typeof(VerticalLayoutGroup));
        containerObject.transform.SetParent(panel.transform, false);

        listContainer = containerObject.GetComponent<RectTransform>();
        listContainer.anchorMin = Vector2.zero;
        listContainer.anchorMax = Vector2.one;
        listContainer.offsetMin = new Vector2(20f, 20f);
        listContainer.offsetMax = new Vector2(-20f, -titleHeight);

        VerticalLayoutGroup layout = containerObject.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.spacing = rowSpacing;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }

    private void Refresh()
    {
        bool english = GameLanguage.IsEnglish;
        bool empty = inventory == null || inventory.Items.Count == 0;

        if (listText != null)
        {
            string title = "<b>" + (english ? "Inventory" : "Inventario") + "</b>";
            listText.text = empty
                ? title + "\n\n" + (english ? "Nothing found yet." : "Aún no has encontrado nada.")
                : title;
        }

        ClearRows();
        if (listContainer == null || inventory == null) return;

        foreach (KeyItemData item in inventory.Items)
        {
            CreateRow(item, english);
        }
    }

    private void ClearRows()
    {
        foreach (GameObject row in rows)
        {
            if (row == null) continue;

            // Se desactiva antes de destruirla: así el Layout Group la ignora ya y las demás filas
            // suben al instante, sin esperar al final del fotograma.
            row.SetActive(false);
            Destroy(row);
        }
        rows.Clear();
    }

    private void CreateRow(KeyItemData item, bool english)
    {
        GameObject row = new GameObject("Fila_" + item.name,
            typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(listContainer, false);
        rows.Add(row);

        row.GetComponent<LayoutElement>().preferredHeight = rowHeight;

        HorizontalLayoutGroup rowLayout = row.GetComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 12f;
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = true;

        // Texto: nombre del objeto y sus puntos.
        GameObject labelObject = new GameObject("Texto", typeof(RectTransform), typeof(LayoutElement));
        labelObject.transform.SetParent(row.transform, false);
        labelObject.GetComponent<LayoutElement>().flexibleWidth = 1f;

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = item.GetDisplayName() + "   " + ColorTag(pointsColor) + item.points + " pts</color>";
        label.fontSize = rowFontSize;
        label.color = textColor;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.raycastTarget = false;

        // Botón "Soltar".
        GameObject buttonObject = new GameObject("BotonSoltar",
            typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(row.transform, false);
        buttonObject.GetComponent<LayoutElement>().preferredWidth = dropButtonWidth;

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = dropButtonColor;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonImage;

        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.85f, 0.6f, 1f);
        colors.selectedColor = new Color(1f, 0.85f, 0.6f, 1f);
        button.colors = colors;

        GameObject buttonTextObject = new GameObject("Texto", typeof(RectTransform));
        buttonTextObject.transform.SetParent(buttonObject.transform, false);

        RectTransform buttonTextRect = buttonTextObject.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;

        TextMeshProUGUI buttonLabel = buttonTextObject.AddComponent<TextMeshProUGUI>();
        buttonLabel.text = english ? "Drop" : "Soltar";
        buttonLabel.fontSize = rowFontSize * 0.85f;
        buttonLabel.color = Color.white;
        buttonLabel.alignment = TextAlignmentOptions.Center;
        buttonLabel.raycastTarget = false;

        // Sonidos de hover y click, igual que los demás botones del juego.
        buttonObject.AddComponent<UIButtonAudioFeedback>();

        KeyItemData itemToDrop = item;
        button.onClick.AddListener(() => OnDropClicked(itemToDrop));
    }

    private void OnDropClicked(KeyItemData item)
    {
        if (inventory != null)
        {
            // Al soltarlo, el inventario avisa con OnChanged y la lista se reconstruye sola.
            inventory.DropItem(item);
        }
    }

    private static string ColorTag(Color color)
    {
        return "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">";
    }
}
