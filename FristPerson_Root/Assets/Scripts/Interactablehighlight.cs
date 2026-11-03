using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resalta cualquier prop interactuable garantizando que NO se altere la transparencia
/// original del objeto y dando soporte a múltiples tipos de shaders (URP Lit, Unlit, Custom).
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
        public bool hasColor;
        public string colorPropName;
        public Color originalColor;

        public bool hasEmission;
        public Color originalEmission;
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
                // 1. Aplicar tinte conservando estrictamente el Alpha (transparencia) original
                if (state.hasColor)
                {
                    Color tintedColor = Color.Lerp(state.originalColor, highlightColor, tintAmount);
                    tintedColor.a = state.originalColor.a; // Impide que el objeto se vuelva transparente

                    state.material.SetColor(state.colorPropName, tintedColor);
                }

                // 2. Activar emisión si el shader lo soporta
                if (state.hasEmission)
                {
                    state.material.EnableKeyword("_EMISSION");
                    state.material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

                    Color finalEmission = highlightColor * emissionIntensity;
                    state.material.SetColor("_EmissionColor", finalEmission);
                }
            }
            else
            {
                // Restaurar color y transparencia originales
                if (state.hasColor)
                {
                    state.material.SetColor(state.colorPropName, state.originalColor);
                }

                // Restaurar estado de emisión original
                if (state.hasEmission)
                {
                    state.material.SetColor("_EmissionColor", state.originalEmission);

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

            foreach (Material mat in rend.materials)
            {
                if (mat == null) continue;

                MaterialState state = new MaterialState { material = mat };

                // Buscar la propiedad de color principal compatible
                if (mat.HasProperty("_BaseColor"))
                {
                    state.hasColor = true;
                    state.colorPropName = "_BaseColor";
                    state.originalColor = mat.GetColor("_BaseColor");
                }
                else if (mat.HasProperty("_Color"))
                {
                    state.hasColor = true;
                    state.colorPropName = "_Color";
                    state.originalColor = mat.GetColor("_Color");
                }
                else if (mat.HasProperty("_TintColor"))
                {
                    state.hasColor = true;
                    state.colorPropName = "_TintColor";
                    state.originalColor = mat.GetColor("_TintColor");
                }

                // Buscar la propiedad de emisión
                if (mat.HasProperty("_EmissionColor"))
                {
                    state.hasEmission = true;
                    state.originalEmission = mat.GetColor("_EmissionColor");
                    state.hadEmissionKeyword = mat.IsKeywordEnabled("_EMISSION");
                }

                if (!state.hasColor && !state.hasEmission) continue;

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
