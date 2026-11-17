using System.Collections;
using UnityEngine;

/// <summary>
/// LLAVE 2 — EL PANTEÓN. Una cripta cerrada con reja. Para abrirla hay que prender
/// las 3 velas de las tumbas marcadas. Cada vela que prendes despierta algo.
/// </summary>
public class SitioPanteon : SitioLlave
{
    public VelaPanteon[] velas;
    public PuertaBisagra rejaCripta;

    public override string[] LineasPartida => new[]
    {
        "Senalar|Siguiente: el panteón. No pongas esa cara, ahí todos son muy tranquilos.",
        "Intimidar|Casi todos.",
    };

    public override string[] LineasLlegada => new[]
    {
        "Sentado|La cripta está cerrada. Los muertos de aquí solo abren si les prendes una vela. Tres velas.",
        "Hablar|Busca las tumbas con calavera. Y si oyes que alguien te sigue entre las cruces... métete a una fosa abierta. Ahí nadie busca.",
    };

    public override string VozAlLlegar => "panteon";

    public override string ObjetivoLlegar => "Llave 2: el panteón";

    protected override IEnumerator Reto(MisionManager h)
    {
        int total = velas != null ? velas.Length : 0;
        h.Objetivo("Prende las velas de las tumbas  [RB]   0/" + total);
        int previas = 0;
        while (true)
        {
            int prendidas = 0;
            if (velas != null) foreach (VelaPanteon v in velas) if (v != null && v.Prendida) prendidas++;
            if (prendidas != previas)
            {
                previas = prendidas;
                h.Objetivo("Prende las velas de las tumbas  [RB]   " + prendidas + "/" + total);
                if (prendidas == 2) { SoltarMonstruo(h.Jugador.position); }
            }
            if (prendidas >= total) break;
            yield return null;
        }

        yield return new WaitForSeconds(0.8f);
        VHSOverlay.Glitch(0.7f, 0.8f);
        if (rejaCripta != null) rejaCripta.Abrir();
        DialogoUI.Susurrar("", "La reja de la cripta... se abrió sola.", 3f);
        h.Objetivo("Entra a la cripta");
    }
}
