using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Inventario compacto estilo nota en la esquina inferior izquierda con contador de capacidad (ej: 0/5).
/// Permite personalizar totalmente el Padding y espaciado del Vertical Layout Group desde el Inspector.
/// </summary>
public class KeyItemInventoryUI : MonoBehaviour
{
    [Header("Panel")]
    [Tooltip("El panel que se muestra/oculta. Debe estar anclado abajo a la izquierda en el Canvas.")]
    [SerializeField] private GameObject panel;
    [Tooltip("Texto del título superior donde se muestra el contador (ej: Inventario 2/5).")]
    [SerializeField] private TMP_Text listText;

    [Header("Ajustes de Filas (Estilo Nota)")]
    [SerializeField] private float titleHeight = 45f;
    [SerializeField] private float rowHeight = 32f;
    [SerializeField] private float rowSpacing = 6f;
    [SerializeField] private float rowFontSize = 18f;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Color pointsColor = new Color(1f, 0.85f, 0.3f, 1f);
    [SerializeField] private Color dropButtonColor = new Color(0.75f, 0.2f, 0.2f, 1f);
    [SerializeField] private float dropButtonWidth = 80f;

    [Header("Padding / Márgenes de la Lista (Vertical Layout Group)")]
    [Tooltip("Margen izquierdo en píxeles.")]
    [SerializeField] private int paddingLeft = 10;
    [Tooltip("Margen derecho en píxeles.")]
    [SerializeField] private int paddingRight = 10;
    [Tooltip("Margen superior en píxeles.")]
    [SerializeField] private int paddingTop = 5;
    [Tooltip("Margen inferior en píxeles.")]
    [SerializeField] private int paddingBottom = 5;

    private PlayerKeyItemInventory inventory;
    private FirstPersonController playerController;
    private InputAction toggleAction;
    private RectTransform listContainer;
    private VerticalLayoutGroup containerLayoutGroup;
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

        if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayInventoryOpen();
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

        if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayInventoryClose();
    }

    private void BuildListContainer()
    {
        if (panel == null) return;

        GameObject containerObject = new GameObject("ItemListContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
        containerObject.transform.SetParent(panel.transform, false);

        listContainer = containerObject.GetComponent<RectTransform>();

        // Anclar arriba en la parte superior del panel
        listContainer.anchorMin = new Vector2(0f, 1f);
        listContainer.anchorMax = new Vector2(1f, 1f);
        listContainer.pivot = new Vector2(0.5f, 1f);

        listContainer.anchoredPosition = new Vector2(0f, -titleHeight);
        listContainer.sizeDelta = new Vector2(0f, 0f);

        containerLayoutGroup = containerObject.GetComponent<VerticalLayoutGroup>();
        ApplyLayoutSettings();
    }

    private void ApplyLayoutSettings()
    {
        if (containerLayoutGroup == null) return;

        // Configuración de Padding personalizada desde el Inspector
        containerLayoutGroup.padding = new RectOffset(paddingLeft, paddingRight, paddingTop, paddingBottom);
        containerLayoutGroup.spacing = rowSpacing;
        containerLayoutGroup.childAlignment = TextAnchor.UpperLeft;
        containerLayoutGroup.childControlWidth = true;
        containerLayoutGroup.childControlHeight = false;
        containerLayoutGroup.childForceExpandWidth = true;
        containerLayoutGroup.childForceExpandHeight = false;
    }

    private void Refresh()
    {
        ApplyLayoutSettings();

        bool english = GameLanguage.IsEnglish;

        int currentCount = inventory != null ? inventory.Count : 0;
        int maxCapacity = inventory != null ? inventory.MaxCapacity : 5;

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
            string titleName = english ? "Inventory" : "Inventario";
            string title = "<b>" + titleName + " " + currentCount + "/" + maxCapacity + "</b>";
            listText.text = empty
                ? title + "\n<size=14><color=#AAAAAA>" + (english ? "Empty" : "Vacío") + "</color></size>"
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
        rowLayout.spacing = 8f;
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = true;

        // Texto compacto: Nombre (xCant) + Puntos
        GameObject labelObject = new GameObject("Texto", typeof(RectTransform), typeof(LayoutElement));
        labelObject.transform.SetParent(row.transform, false);
        labelObject.GetComponent<LayoutElement>().flexibleWidth = 1f;

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();

        string nameString = item.GetDisplayName();
        if (count > 1) nameString += " <color=#AAAAAA>x" + count + "</color>";

        int totalPoints = item.points * count;
        label.text = nameString + "  " + ColorTag(pointsColor) + totalPoints + "pts</color>";
        label.fontSize = rowFontSize;
        label.color = textColor;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.raycastTarget = false;

        // Botón "Soltar" compacto
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
        buttonLabel.fontSize = rowFontSize * 0.8f;
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