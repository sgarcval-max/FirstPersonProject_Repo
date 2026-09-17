using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Componente reutilizable: representa UNA fila de "reasignación de tecla" en la UI.
/// A diferencia de la primera versión, esta busca la acción por NOMBRE dentro de
/// InputManager.Controls (la única instancia compartida de todo el juego), en vez de
/// usar un InputActionReference que apuntaba al asset del Project y no al runtime real.
/// </summary>
public class RebindActionUI : MonoBehaviour
{
    [Header("Acción a reasignar (nombres EXACTOS como en el asset PlayerControls)")]
    [Tooltip("Ej: 'Movimiento'")]
    [SerializeField] private string actionMapName;
    [Tooltip("Ej: 'Jump' o 'Run'")]
    [SerializeField] private string actionName;

    [Header("Referencias UI")]
    [SerializeField] private TMP_Text actionNameText;
    [SerializeField] private TMP_Text bindingDisplayText;
    [SerializeField] private Button rebindButton;

    private InputAction action;
    private InputActionRebindingExtensions.RebindingOperation rebindingOperation;
    private const string REBINDS_KEY = "input_rebinds";

    private void OnEnable()
    {
        // Buscamos la acción real dentro de la instancia COMPARTIDA (la misma que usa el gameplay)
        action = InputManager.Controls.asset.FindActionMap(actionMapName).FindAction(actionName);

        if (actionNameText != null)
        {
            actionNameText.text = actionName;
        }
        UpdateBindingDisplay();

        if (rebindButton != null)
        {
            rebindButton.onClick.AddListener(StartRebind);
        }
    }

    private void OnDisable()
    {
        if (rebindButton != null)
        {
            rebindButton.onClick.RemoveListener(StartRebind);
        }
        rebindingOperation?.Dispose();
    }

    private void UpdateBindingDisplay()
    {
        bindingDisplayText.text = action.GetBindingDisplayString();
    }

    public void StartRebind()
    {
        if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayKeyRebindStart();

        action.Disable();
        bindingDisplayText.text = "Pulsa una tecla...";

        rebindingOperation = action.PerformInteractiveRebinding()
            .WithControlsExcluding("Mouse")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(operation =>
            {
                rebindingOperation.Dispose();
                action.Enable();
                UpdateBindingDisplay();
                SaveAllRebinds();
            })
            .OnCancel(operation =>
            {
                rebindingOperation.Dispose();
                action.Enable();
                UpdateBindingDisplay();
            })
            .Start();
    }

    /// <summary>Guarda TODAS las reasignaciones del asset completo en PlayerPrefs.</summary>
    private void SaveAllRebinds()
    {
        string json = InputManager.Controls.asset.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString(REBINDS_KEY, json);
    }

    /// <summary>Llamar desde un botón "Restaurar por defecto" para esta acción concreta.</summary>
    public void ResetToDefault()
    {
        action.RemoveAllBindingOverrides();
        UpdateBindingDisplay();
        SaveAllRebinds();
    }

    /// <summary>Se llama UNA vez al arrancar el juego (desde InputManager) para cargar reasignaciones guardadas.</summary>
    public static void LoadSavedRebinds(InputActionAsset asset)
    {
        if (PlayerPrefs.HasKey(REBINDS_KEY))
        {
            string json = PlayerPrefs.GetString(REBINDS_KEY);
            asset.LoadBindingOverridesFromJson(json);
        }
    }
}