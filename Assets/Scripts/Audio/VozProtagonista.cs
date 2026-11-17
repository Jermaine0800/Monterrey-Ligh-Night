using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Voz del senderista (solo en momentos clave). Los audios salen de Resources/Voces/&lt;id&gt;.wav,
/// generados con una voz sintética en español latino (menú Monterrey/13). Si un audio no existe,
/// solo se muestra el subtítulo. Se le pone un filtro de "micrófono de videocámara".
/// </summary>
public static class VozProtagonista
{
    /// <summary>Todas las frases: id → texto. El generador de voces usa esta misma lista.</summary>
    public static readonly Dictionary<string, string> Frases = new Dictionary<string, string>
    {
        { "despertar",   "¿Dónde... dónde están todos? ¿Hola?" },
        { "linterna",    "Bueno... por lo menos todavía prende." },
        { "vigia",       "¿Quién eres tú? ¿Qué haces aquí?" },
        { "saludo",      "Hay alguien entre los árboles... me está saludando." },
        { "campamento",  "Es nuestro campamento. Aquí estábamos hace rato. Hace rato... ¿verdad?" },
        { "panteon",     "No me gustan los panteones. Y menos de noche." },
        { "tunel",       "Esto no debería estar aquí. No hay metro en el cerro." },
        { "capilla",     "Una capilla... Dios, que no haya nadie adentro." },
        { "mirador",     "La luz roja... ya casi. Ya casi salgo de aquí." },
        { "llave",       "Una más." },
        { "susto",       "¡¿Qué fue eso?! ¡¿Qué fue eso?!" },
        { "escondido",   "No respires... no respires..." },
        { "herido",      "Me duele... tengo que salir de aquí." },
        { "bucle",       "Otra vez aquí. Otra vez la misma noche." },
        { "final",       "Se abrió... del otro lado está el mismo bosque." },
    };

    static float ultimo;
    static AudioSource fuente;

    public static void Decir(string id, float esperaMinima = 4f)
    {
        if (!Frases.TryGetValue(id, out string texto)) return;
        if (Time.time - ultimo < esperaMinima) return;
        ultimo = Time.time;

        AudioClip c = Resources.Load<AudioClip>("Voces/" + id);
        if (c != null)
        {
            if (fuente == null)
            {
                GameObject g = new GameObject("VozProtagonista");
                Object.DontDestroyOnLoad(g);
                fuente = g.AddComponent<AudioSource>();
                fuente.spatialBlend = 0f;
                // "micrófono de videocámara": sin graves, sin agudos, un poco saturado
                g.AddComponent<AudioHighPassFilter>().cutoffFrequency = 220f;
                g.AddComponent<AudioLowPassFilter>().cutoffFrequency = 4200f;
                g.AddComponent<AudioDistortionFilter>().distortionLevel = 0.18f;
            }
            fuente.pitch = 0.96f;
            fuente.Stop();
            fuente.PlayOneShot(c, 1f);
        }
        DialogoUI.Susurrar("TÚ", texto, Mathf.Max(2.5f, c != null ? c.length + 0.6f : texto.Length * 0.07f));
    }
}
