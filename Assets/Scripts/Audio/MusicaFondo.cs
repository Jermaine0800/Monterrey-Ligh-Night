using System.Collections;
using UnityEngine;

/// <summary>
/// La canción del menú (Assets/Resources/Audio/TemaMenu.mp3). Suena en el menú principal,
/// sigue sonando mientras carga el juego y se va apagando cuando termina el prólogo.
/// </summary>
public class MusicaFondo : MonoBehaviour
{
    static MusicaFondo instancia;
    AudioSource src;

    public static void Menu(float volumen = 0.65f)
    {
        if (instancia != null) { instancia.src.volume = volumen; return; }
        AudioClip c = Resources.Load<AudioClip>("Audio/TemaMenu");
        if (c == null) { Debug.LogWarning("[Música] No encontré Resources/Audio/TemaMenu"); return; }
        GameObject g = new GameObject("MusicaFondo");
        DontDestroyOnLoad(g);
        instancia = g.AddComponent<MusicaFondo>();
        instancia.src = g.AddComponent<AudioSource>();
        instancia.src.clip = c;
        instancia.src.loop = true;
        instancia.src.spatialBlend = 0f;
        instancia.src.volume = 0f;
        instancia.src.Play();
        instancia.StartCoroutine(instancia.Fundir(volumen, 3f, false));
    }

    /// <summary>Apaga la canción poco a poco y la destruye.</summary>
    public static void Desvanecer(float segundos)
    {
        if (instancia == null) return;
        instancia.StartCoroutine(instancia.Fundir(0f, segundos, true));
    }

    public static bool Sonando => instancia != null;

    IEnumerator Fundir(float hasta, float segundos, bool destruir)
    {
        float desde = src.volume;
        for (float t = 0; t < segundos; t += Time.unscaledDeltaTime)
        {
            src.volume = Mathf.Lerp(desde, hasta, t / segundos);
            yield return null;
        }
        src.volume = hasta;
        if (destruir) { Destroy(gameObject); instancia = null; }
    }
}
