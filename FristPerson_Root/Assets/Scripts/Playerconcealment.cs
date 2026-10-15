using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Guarda cómo de "tapado" está el jugador en cada momento. Cada escondite (armario, zona bajo
/// una mesa...) le dice aquí cuánto reduce la vista y el ruido mientras el jugador lo usa.
/// Los guardias (HumanVision) y el ruido del jugador (PlayerNoise) consultan estos valores.
///
/// Si hay varios escondites activos a la vez, se aplica el más fuerte de cada tipo.
///   SightMultiplier: 1 = te ven con normalidad, 0.3 = te ven desde un 30 % de distancia, 0 = invisible.
///   NoiseMultiplier: 1 = tu ruido suena con normalidad, 0.5 = la mitad de alcance, 0 = silencio total.
/// </summary>
public class PlayerConcealment : MonoBehaviour
{
    public static PlayerConcealment Instance { get; private set; }

    private class Source
    {
        public float sight = 1f;
        public float noise = 1f;
    }

    private readonly Dictionary<Object, Source> sources = new Dictionary<Object, Source>();

    public float SightMultiplier { get; private set; }
    public float NoiseMultiplier { get; private set; }

    /// <summary>True si algún escondite está reduciendo la vista o el ruido.</summary>
    public bool IsConcealed
    {
        get { return SightMultiplier < 1f || NoiseMultiplier < 1f; }
    }

    private void Awake()
    {
        Instance = this;
        SightMultiplier = 1f;
        NoiseMultiplier = 1f;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>Un escondite activa (o actualiza) su efecto. "owner" identifica al escondite.</summary>
    public void SetSource(Object owner, float sightMultiplier, float noiseMultiplier)
    {
        Source source;
        if (!sources.TryGetValue(owner, out source))
        {
            source = new Source();
            sources[owner] = source;
        }

        source.sight = Mathf.Clamp01(sightMultiplier);
        source.noise = Mathf.Clamp01(noiseMultiplier);
        Recalculate();
    }

    /// <summary>Un escondite deja de tener efecto (el jugador sale de él).</summary>
    public void RemoveSource(Object owner)
    {
        if (sources.Remove(owner))
        {
            Recalculate();
        }
    }

    private void Recalculate()
    {
        float sight = 1f;
        float noise = 1f;

        foreach (Source source in sources.Values)
        {
            sight = Mathf.Min(sight, source.sight);
            noise = Mathf.Min(noise, source.noise);
        }

        SightMultiplier = sight;
        NoiseMultiplier = noise;
    }
}
