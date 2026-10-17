using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resalta un objeto cuando el jugador lo está mirando y puede interactuar con él. No hace falta
/// añadirlo a mano: PlayerInteractionController lo añade solo a cualquier objeto interactuable la
/// primera vez que lo mira. Si quieres otro color, añádelo tú al prefab y cámbialo en el Inspector.
///
/// El resaltado usa DOS efectos a la vez:
///  - Un tinte del color del material hacia el color de resaltado. Esto funciona siempre, también en
///    las builds, porque el color es una propiedad que todos los shaders tienen disponible.
///  - Un brillo (emisión). Se ve en el editor, pero en las builds Unity puede descartar esa variante
///    del shader si ningún material la usa de antemano, así que no podemos depender de él.
/// </summary>
public class InteractableHighlight : MonoBehaviour
{
    [SerializeField] private Color highlightColor = new Color(1f, 0.85f, 0.2f, 1f);

    [Tooltip("Cuánto se tiñe el objeto del color de resaltado (0 = nada, 1 = por completo).")]
    [Range(0f, 1f)]
    [SerializeField] private float tintAmount = 0.55f;

    [Tooltip("Intensidad del brillo (emisión) añadido al tinte.")]
    [SerializeField] private float emissionIntensity = 1.2f;

    private class MaterialState
    {
        public Material material;
        public string colorProperty;      // "_BaseColor" (URP), "_Color" (clásico) o null si no tiene color
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
                if (state.colorProperty != null)
                {
                    state.material.SetColor(state.colorProperty,
                        Color.Lerp(state.originalColor, highlightColor, tintAmount));
                }

                if (state.hasEmission)
                {
                    state.material.EnableKeyword("_EMISSION");
                    state.material.SetColor("_EmissionColor", highlightColor * emissionIntensity);
                }
            }
            else
            {
                if (state.colorProperty != null)
                {
                    state.material.SetColor(state.colorProperty, state.originalColor);
                }

                if (state.hasEmission)
                {
                    state.material.SetColor("_EmissionColor", state.originalEmission);
                    if (!state.hadEmissionKeyword)
                    {
                        state.material.DisableKeyword("_EMISSION");
                    }
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
            // rend.materials crea una copia propia de los materiales de este objeto,
            // así resaltarlo no afecta a otros objetos que compartan el mismo material.
            foreach (Material mat in rend.materials)
            {
                MaterialState state = new MaterialState();
                state.material = mat;

                if (mat.HasProperty("_BaseColor")) state.colorProperty = "_BaseColor";
                else if (mat.HasProperty("_Color")) state.colorProperty = "_Color";

                if (state.colorProperty != null)
                {
                    state.originalColor = mat.GetColor(state.colorProperty);
                }

                state.hasEmission = mat.HasProperty("_EmissionColor");
                if (state.hasEmission)
                {
                    state.originalEmission = mat.GetColor("_EmissionColor");
                    state.hadEmissionKeyword = mat.IsKeywordEnabled("_EMISSION");
                }

                // Si el material no tiene ni color ni emisión, no hay nada que resaltar en él.
                if (state.colorProperty == null && !state.hasEmission) continue;

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
