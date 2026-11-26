using System.Collections;
using UnityEngine;

/// <summary>
/// LLAVE 5 — EL MIRADOR. La niebla se vuelve roja y la baliza parpadea arriba.
/// La subida es larga y los dos monstruos te siguen. La llave cuelga de la baliza.
/// </summary>
public class SitioMirador : SitioLlave
{
    public override string[] LineasPartida => new[]
    {
        "Intimidar|Cuatro. Ya casi. ¿Sabes qué pasa cuando tienes cinco? Yo tampoco. Nadie ha llegado.",
        "Senalar|La última está arriba, en el mirador. Donde parpadea la luz roja.",
        "Reir|Te lo prometo. Yo nunca miento. Bueno... casi nunca.",
    };

    public override string[] LineasLlegada => new string[0];

    public override string VozAlLlegar => "mirador";

    public override string ObjetivoLlegar => "Llave 5: el mirador (sigue la baliza roja)";

    public override IEnumerator Jugar(MisionManager h)
    {
        h.StartCoroutine(NieblaRoja(10f));
        if (h.Director != null) { h.Director.tensionPorSegundo *= 1.8f; h.Director.Habilitar(true); }
        yield return base.Jugar(h);
    }

    protected override IEnumerator Reto(MisionManager h)
    {
        h.Objetivo("Toma la llave de la baliza  [RB]");
        yield break;
    }

    protected override IEnumerator AlTomarLlave(MisionManager h)
    {
        VHSOverlay.Glitch(0.8f, 1.2f);
        yield return new WaitForSeconds(1f);
        SoltarMonstruo(h.Jugador.position);
    }

    static IEnumerator NieblaRoja(float segundos)
    {
        Color desde = RenderSettings.fogColor, hasta = new Color(0.16f, 0.02f, 0.02f);
        float d0 = RenderSettings.fogDensity, d1 = d0 + 0.01f;
        for (float t = 0; t < segundos; t += Time.deltaTime)
        {
            float k = t / segundos;
            RenderSettings.fogColor = Color.Lerp(desde, hasta, k);
            RenderSettings.fogDensity = Mathf.Lerp(d0, d1, k);
            if (Camera.main != null) Camera.main.backgroundColor = RenderSettings.fogColor;
            yield return null;
        }
    }
}
