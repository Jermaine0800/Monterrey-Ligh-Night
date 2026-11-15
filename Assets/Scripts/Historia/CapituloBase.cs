using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Un acto de la historia. Cada acto (prólogo, claro, túnel, mirador) es una subclase
/// que decide qué pasa en <see cref="Jugar"/>. El MisionManager solo los recorre en orden.
/// </summary>
public abstract class CapituloBase
{
    public abstract string Titulo { get; }
    public abstract IEnumerator Jugar(MisionManager h);

    // ------------------------------------------------------------------ Utilidades para pantallas de texto (prólogo / final)

    protected static Canvas PantallaNegra(string nombre, out Image fondo, int orden = 700)
    {
        Canvas c = HorrorUI.CrearCanvas(nombre, orden);
        fondo = HorrorUI.CrearImagen(c.transform, "Negro", Color.black);
        HorrorUI.Estirar(fondo.rectTransform);
        return c;
    }

    /// <summary>Escribe el texto letra por letra con clics de máquina. Se puede adelantar con A / Enter.</summary>
    protected static IEnumerator Maquina(Text t, string texto, float letrasPorSegundo, AudioSource fuente, AudioClip clic, bool tiempoReal = false)
    {
        t.text = "";
        float acumulado = 0f;
        int i = 0;
        while (i < texto.Length)
        {
            if (HorrorInput.Aceptar) { t.text = texto; yield break; }
            acumulado += (tiempoReal ? Time.unscaledDeltaTime : Time.deltaTime) * letrasPorSegundo;
            while (acumulado >= 1f && i < texto.Length)
            {
                acumulado -= 1f;
                char ch = texto[i++];
                t.text += ch;
                if (fuente != null && clic != null && !char.IsWhiteSpace(ch) && i % 2 == 0)
                {
                    fuente.pitch = Random.Range(0.85f, 1.15f);
                    fuente.PlayOneShot(clic, 0.35f);
                }
            }
            yield return null;
        }
    }

    protected static IEnumerator Fundido(Graphic g, float desde, float hasta, float segundos, bool tiempoReal = false)
    {
        Color c = g.color;
        for (float t = 0; t < segundos; t += tiempoReal ? Time.unscaledDeltaTime : Time.deltaTime)
        {
            c.a = Mathf.Lerp(desde, hasta, t / segundos);
            g.color = c;
            yield return null;
        }
        c.a = hasta; g.color = c;
    }
}
