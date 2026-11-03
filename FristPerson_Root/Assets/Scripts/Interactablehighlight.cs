using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resalta un objeto cuando el jugador lo está mirando y puede interactuar con él.
/// Aplica un tinte de color y un brillo HDR (Emisión) compatible tanto en Editor
/// como en Builds finales (URP y Built-in Render Pipeline).
/// </summary>
public class InteractableHighlight : MonoBehaviour
{
    [Header("Configuración de Resaltado")]
    [SerializeField] private Color highlightColor = new Color(1f, 0.85f, 0.2f, 1f);

    [Tooltip("Cuánto se tiñe el objeto del color de resaltado (0 = nada, 1 = por completo).")]
    [Range(0f, 1f)]
    [SerializeField] private float tintAmount = 0.55f;

    [Tooltip("Intensidad del brillo (emisión). Potenciado para renderizado en espacio Lineal/HDR en Builds.")]
    [SerializeField] private float emissionIntensity = 2.5f;

    private class MaterialState
    {
        public Material material;
        public bool hasBaseColor;
        public string baseColorProp;
        public Color originalBaseColor;

        public bool hasLegacyColor;
        public Color originalLegacyColor;

        public bool hasEmissionProp;
        public Color originalEmissionColor;
        public bool hadEmissionKeyword;
    }

    private readonly List<MaterialState> states = new List<MaterialState>();
    private bool initialized;

    public void SetHighlighted(bool highlighted)
    {
        Initialize();

        foreach (MaterialState state in states)
        {
            if (state.material == null) continue;

            if (highlighted)
            {
                // 1. Aplicar tinte al color base (Soporta URP _BaseColor y Standard _Color)
                if (state.hasBaseColor)
                {
                    Color tintedColor = Color.Lerp(state.originalBaseColor, highlightColor, tintAmount);
                    state.material.SetColor(state.baseColorProp, tintedColor);
                }

                if (state.hasLegacyColor && state.baseColorProp != "_Color")
                {
                    Color tintedLegacy = Color.Lerp(state.originalLegacyColor, highlightColor, tintAmount);
                    state.material.SetColor("_Color", tintedLegacy);
                }

                // 2. Activar y forzar la Emisión HDR para la Build
                if (state.hasEmissionProp)
                {
                    state.material.EnableKeyword("_EMISSION");

                    // Forzar a Unity a actualizar la iluminación emisiva en tiempo de ejecución
                    state.material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

                    Color finalEmission = highlightColor * emissionIntensity;
                    state.material.SetColor("_EmissionColor", finalEmission);
                }
            }
            else
            {
                // Restaurar color base original
                if (state.hasBaseColor)
                {
                    state.material.SetColor(state.baseColorProp, state.originalBaseColor);
                }

                if (state.hasLegacyColor && state.baseColorProp != "_Color")
                {
                    state.material.SetColor("_Color", state.originalLegacyColor);
                }

                // Restaurar emisión original
                if (state.hasEmissionProp)
                {
                    state.material.SetColor("_EmissionColor", state.originalEmissionColor);

                    if (!state.hadEmissionKeyword)
                    {
                        state.material.DisableKeyword("_EMISSION");
                    }

                    state.material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
                }
            }
        }
    }

    private void Initialize()
    {
        if (initialized) return;
        initialized = true;

        foreach (Renderer rend in GetComponentsInChildren<Renderer>())
        {
            if (rend == null) continue;

            // rend.materials crea una copia de instancia del material para este objeto único
            foreach (Material mat in rend.materials)
            {
                if (mat == null) continue;

                MaterialState state = new MaterialState { material = mat };

                // Detección de propiedades de color base (URP vs Standard)
                if (mat.HasProperty("_BaseColor"))
                {
                    state.hasBaseColor = true;
                    state.baseColorProp = "_BaseColor";
                    state.originalBaseColor = mat.GetColor("_BaseColor");
                }
                else if (mat.HasProperty("_Color"))
                {
                    state.hasBaseColor = true;
                    state.baseColorProp = "_Color";
                    state.originalBaseColor = mat.GetColor("_Color");
                }

                if (mat.HasProperty("_Color"))
                {
                    state.hasLegacyColor = true;
                    state.originalLegacyColor = mat.GetColor("_Color");
                }

                // Detección de propiedad de emisión
                if (mat.HasProperty("_EmissionColor"))
                {
                    state.hasEmissionProp = true;
                    state.originalEmissionColor = mat.GetColor("_EmissionColor");
                    state.hadEmissionKeyword = mat.IsKeywordEnabled("_EMISSION");
                }

                if (!state.hasBaseColor && !state.hasLegacyColor && !state.hasEmissionProp) continue;

                states.Add(state);
            }
        }
    }

    private void OnDestroy()
    {
        foreach (MaterialState state in states)
        {
            if (state.material != null)
            {
                Destroy(state.material);
            }
        }
    }
}
