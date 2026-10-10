using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sistema central de ruidos. Cualquier cosa del juego puede llamar a
/// NoiseSystem.Emit(posición, radio) y todos los humanos con oído (HumanHearing) que estén
/// dentro de ese radio lo oirán. Así no hace falta que cada fuente de ruido conozca a los
/// humanos, ni al revés.
/// </summary>
public static class NoiseSystem
{
    private static readonly List<HumanHearing> listeners = new List<HumanHearing>();

    public static void Register(HumanHearing listener)
    {
        if (!listeners.Contains(listener))
        {
            listeners.Add(listener);
        }
    }

    public static void Unregister(HumanHearing listener)
    {
        listeners.Remove(listener);
    }

    /// <summary>Hace un ruido en esta posición, que se oye hasta "radius" metros.</summary>
    public static void Emit(Vector3 position, float radius)
    {
        for (int i = listeners.Count - 1; i >= 0; i--)
        {
            if (listeners[i] == null)
            {
                listeners.RemoveAt(i);
                continue;
            }

            listeners[i].Hear(position, radius);
        }
    }
}