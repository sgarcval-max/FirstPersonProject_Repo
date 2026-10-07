using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Atenúa suavemente la música al pausar, usando un parámetro DEDICADO del Mixer
/// ("MusicDuckVolume", en el grupo hijo MusicDuck) que es independiente del parámetro
/// "MusicVolume" que controla el slider de Ajustes. Así, aunque el jugador mueva el
/// slider de música mientras está en pausa, no interfiere con el ducking ni al revés.
/// </summary>
public class MusicDucking : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;

    [Tooltip("Cuántos decibelios baja la música en pausa (un número NEGATIVO, ej: -15). 0 = sin atenuar.")]
    [SerializeField] private float duckAmountDB = -15f;

    [Tooltip("Duración de la transición de subida/bajada, en segundos.")]
    [SerializeField] private float transitionDuration = 0.3f;

    private const string DUCK_PARAM = "MusicDuckVolume";
    private const float NORMAL_DB = 0f; // 0dB en este parámetro dedicado = "sin atenuar", no afecta al volumen real

    private Coroutine transitionRoutine;

    /// <summary>Llamar al pausar el juego.</summary>
    public void Duck() => StartTransition(duckAmountDB);

    /// <summary>Llamar al reanudar el juego.</summary>
    public void Restore() => StartTransition(NORMAL_DB);

    private void StartTransition(float targetDB)
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }
        transitionRoutine = StartCoroutine(TransitionRoutine(targetDB));
    }

    private IEnumerator TransitionRoutine(float targetDB)
    {
        audioMixer.GetFloat(DUCK_PARAM, out float startDB);
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            audioMixer.SetFloat(DUCK_PARAM, Mathf.Lerp(startDB, targetDB, t));
            yield return null;
        }

        audioMixer.SetFloat(DUCK_PARAM, targetDB);
        transitionRoutine = null;
    }
}
