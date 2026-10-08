using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hace que un objeto brille (emisión) cuando el jugador lo está mirando y puede interactuar
/// con él. No hace falta añadirlo a mano: PlayerInteractionController lo añade solo a cualquier
/// objeto interactuable la primera vez que lo mira. Si quieres otro color, añádelo tú al prefab
/// y cámbialo en el Inspector.
///
/// Funciona con materiales que tengan emisión (Standard del pipeline clásico y Lit de URP).
/// </summary>
public class InteractableHighlight : MonoBehaviour
{
    [SerializeField] private Color highlightColor = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private float intensity = 1.2f;

    private class MaterialState
    {
        public Material material;
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
                state.material.EnableKeyword("_EMISSION");
                state.material.SetColor("_EmissionColor", highlightColor * intensity);
            }
            else
            {
                state.material.SetColor("_EmissionColor", state.originalEmission);
                if (!state.hadEmissionKeyword)
                {
                    state.material.DisableKeyword("_EMISSION");
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
                if (!mat.HasProperty("_EmissionColor")) continue;

                MaterialState state = new MaterialState();
                state.material = mat;
                state.originalEmission = mat.GetColor("_EmissionColor");
                state.hadEmissionKeyword = mat.IsKeywordEnabled("_EMISSION");
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
