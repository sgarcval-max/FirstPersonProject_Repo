using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Componente reutilizable: representa UNA fila de "reasignación de tecla" en la UI.
/// Busca la acción por NOMBRE dentro de InputManager.Controls (la instancia compartida
/// de todo el juego). Soporta tanto acciones simples (Jump, Run) como una tecla
/// individual dentro de un Composite de varias teclas (ej: la "W" dentro de "Move").
/// </summary>
public class RebindActionUI : MonoBehaviour
{
    [Header("Acción a reasignar (nombres EXACTOS como en el asset PlayerControls)")]
    [Tooltip("Ej: 'Movimiento'")]
    [SerializeField] private string actionMapName;
    [Tooltip("Ej: 'Jump', 'Run' o 'Move'")]
    [SerializeField] private string actionName;

    [Header("Solo si la acción es un Composite (ej: Move = WASD)")]
    [Tooltip("Déjalo VACÍO si la acción es simple (Jump, Run). Si es una tecla dentro de un composite, escribe EXACTAMENTE el nombre de esa parte tal como aparece en el asset: 'up', 'down', 'left' o 'right'.")]
    [SerializeField] private string compositePartName;

    [Header("Referencias UI")]
    [SerializeField] private TMP_Text actionNameText;
    [SerializeField] private TMP_Text bindingDisplayText;
    [SerializeField] private Button rebindButton;

    private InputAction action;
    private int bindingIndex = -1; // -1 = acción simple (usa el binding tal cual); >=0 = índice de la parte del composite
    private InputActionRebindingExtensions.RebindingOperation rebindingOperation;
    private const string REBINDS_KEY = "input_rebinds";

    private void OnEnable()
    {
        action = InputManager.Controls.asset.FindActionMap(actionMapName).FindAction(actionName);
        bindingIndex = ResolveBindingIndex();

        if (actionNameText != null)
        {
            actionNameText.text = string.IsNullOrEmpty(compositePartName) ? actionName : compositePartName;
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

    /// <summary>Busca el índice del binding correspondiente a la parte del composite (por nombre), o -1 si la acción es simple.</summary>
    private int ResolveBindingIndex()
    {
        if (string.IsNullOrEmpty(compositePartName))
        {
            return -1;
        }

        for (int i = 0; i < action.bindings.Count; i++)
        {
            InputBinding b = action.bindings[i];
            if (b.isPartOfComposite && string.Equals(b.name, compositePartName, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        Debug.LogWarning($"No se encontró la parte '{compositePartName}' del composite en la acción '{actionName}'. Revisa que el nombre coincida exactamente.");
        return -1;
    }

    private void UpdateBindingDisplay()
    {
        bindingDisplayText.text = bindingIndex >= 0
            ? action.GetBindingDisplayString(bindingIndex)
            : action.GetBindingDisplayString();
    }

    public void StartRebind()
    {
        if (UIAudioManager.Instance != null) UIAudioManager.Instance.PlayKeyRebindStart();

        action.Disable();
        bindingDisplayText.text = "Pulsa una tecla...";

        InputActionRebindingExtensions.RebindingOperation rebind = bindingIndex >= 0
            ? action.PerformInteractiveRebinding(bindingIndex)
            : action.PerformInteractiveRebinding();

        rebindingOperation = rebind
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

    /// <summary>Llamar desde un botón "Restaurar por defecto" para esta acción/tecla concreta.</summary>
    public void ResetToDefault()
    {
        if (bindingIndex >= 0)
        {
            action.RemoveBindingOverride(bindingIndex);
        }
        else
        {
            action.RemoveAllBindingOverrides();
        }
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