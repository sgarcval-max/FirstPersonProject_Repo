using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Inventario de objetos clave. Agrupa objetos idénticos (ej: Vaso x2),
/// permite soltarlos uno a uno y posiciona las filas ordenadamente desde arriba.
/// </summary>
public class KeyItemInventoryUI : MonoBehaviour
{
    [Header("Panel")]
    [Tooltip("El panel que se muestra/oculta (fondo del inventario). Empieza oculto.")]
    [SerializeField] private GameObject panel;
    [Tooltip("Texto del título superior.")]
    [SerializeField] private TMP_Text listText;

    [Header("Ajustes de Filas")]
    [SerializeField] private float titleHeight = 70f;
    [SerializeField] private float rowHeight = 40f;
    [SerializeField] private float rowSpacing = 8f;
    [SerializeField] private float rowFontSize = 22f;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color pointsColor = new Color(1f, 0.85f, 0.3f, 1f);
    [SerializeField] private Color dropButtonColor = new Color(0.75f, 0.2f, 0.2f, 1f);
    [SerializeField] private float dropButtonWidth = 100f;

    private PlayerKeyItemInventory inventory;
    private FirstPersonController playerController;
    private InputAction toggleAction;
    private RectTransform listContainer;
    private readonly List<GameObject> rows = new List<GameObject>();

    private void OnEnable()
    {
        toggleAction = InputManager.Controls.asset.FindAction("Combate/Inventory");

        if (toggleAction != null)
        {
            toggleAction.performed += OnTogglePerformed;
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

    private void BuildListContainer()
    {
        if (panel == null) return;

        GameObject containerObject = new GameObject("ItemListContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
        containerObject.transform.SetParent(panel.transform, false);

        listContainer = containerObject.GetComponent<RectTransform>();

        // Anclar arriba para que las filas no se centren verticalmente en el medio
        listContainer.anchorMin = new Vector2(0f, 1f);
        listContainer.anchorMax = new Vector2(1f, 1f);
        listContainer.pivot = new Vector2(0.5f, 1f);

        listContainer.anchoredPosition = new Vector2(0f, -titleHeight);
        listContainer.sizeDelta = new Vector2(-40f, 0f);

        VerticalLayoutGroup layout = containerObject.GetComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.spacing = rowSpacing;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }

    private void Refresh()
    {
        bool english = GameLanguage.IsEnglish;

        Dictionary<KeyItemData, int> groupedItems = new Dictionary<KeyItemData, int>();
        if (inventory != null)
        {
            foreach (KeyItemData item in inventory.Items)
            {
                if (item == null) continue;
                if (groupedItems.ContainsKey(item)) groupedItems[item]++;
                else groupedItems[item] = 1;
            }
        }

        bool empty = groupedItems.Count == 0;

        if (listText != null)
        {
            string title = "<b>" + (english ? "Inventory" : "Inventario") + "</b>";
            listText.text = empty
                ? title + "\n\n" + (english ? "Nothing found yet." : "Aún no has encontrado nada.")
                : title;
        }

        ClearRows();
        if (listContainer == null || empty) return;

        foreach (KeyValuePair<KeyItemData, int> pair in groupedItems)
        {
            CreateRow(pair.Key, pair.Value, english);
        }
    }

    private void ClearRows()
    {
        foreach (GameObject row in rows)
        {
            if (row == null) continue;
            row.SetActive(false);
            Destroy(row);
        }
        rows.Clear();
    }

    private void CreateRow(KeyItemData item, int count, bool english)
    {
        GameObject row = new GameObject("Fila_" + item.name,
            typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(listContainer, false);
        rows.Add(row);

        row.GetComponent<LayoutElement>().preferredHeight = rowHeight;

        HorizontalLayoutGroup rowLayout = row.GetComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 10f;
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = true;

        // Texto: Nombre (xCantidad) + Puntos
        GameObject labelObject = new GameObject("Texto", typeof(RectTransform), typeof(LayoutElement));
        labelObject.transform.SetParent(row.transform, false);
        labelObject.GetComponent<LayoutElement>().flexibleWidth = 1f;

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();

        string nameString = item.GetDisplayName();
        if (count > 1) nameString += " <color=#AAAAAA>x" + count + "</color>";

        int totalPoints = item.points * count;
        label.text = nameString + "   " + ColorTag(pointsColor) + totalPoints + " pts</color>";
        label.fontSize = rowFontSize;
        label.color = textColor;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.raycastTarget = false;

        // Botón "Soltar"
        GameObject buttonObject = new GameObject("BotonSoltar",
            typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(row.transform, false);
        buttonObject.GetComponent<LayoutElement>().preferredWidth = dropButtonWidth;

        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = dropButtonColor;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonImage;

        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.4f, 0.4f, 1f);
        colors.selectedColor = new Color(1f, 0.4f, 0.4f, 1f);
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

        buttonObject.AddComponent<UIButtonAudioFeedback>();

        KeyItemData itemToDrop = item;
        button.onClick.AddListener(() => OnDropClicked(itemToDrop));
    }

    private void OnDropClicked(KeyItemData item)
    {
        if (inventory != null)
        {
            inventory.DropItem(item);
        }
    }

    private static string ColorTag(Color color)
    {
        return "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">";
    }
}