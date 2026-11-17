using System.Collections;
using UnityEngine;

/// <summary>
/// LLAVE 3 — TÚNEL LÍNEA K. La marca pintada, la puerta del EXPEDIENTE 08, el andén,
/// el monitor, el casillero (batería + documento + llave) y el apagón en las vías.
/// </summary>
public class SitioTunel : SitioLlave
{
    public TunelLineaK tunel;

    public override string[] LineasPartida => new[]
    {
        "Senalar|Ahora el túnel. Hay un muro de concreto en la ladera. Nadie sabe por qué está ahí. Yo sí.",
        "Hablar|Llévate luz. Allá abajo la oscuridad es... más oscura.",
    };

    public override string[] LineasLlegada => new[]
    {
        "Sentado|Mira la pintura del muro. Con la linterna. No muerde. Creo.",
    };

    public override string VozAlLlegar => "tunel";

    public override string ObjetivoLlegar => "Llave 3: el muro de concreto en la ladera";

    protected override IEnumerator Reto(MisionManager h)
    {
        if (tunel == null) yield break;
        h.Objetivo("Alumbra la marca pintada  [X]");
        yield return h.EsperarHasta(() => tunel.Revelado);
        h.Objetivo("Entra  —  EXPEDIENTE 08  [RB]");

        yield return h.EsperarHasta(() => tunel.JugadorDentro);
        bool directorPrendido = h.Director != null && h.Director.habilitado;
        if (h.Director != null) h.Director.Habilitar(false);
        if (LlavesBucle.Instancia != null) LlavesBucle.Instancia.destino = tunel.casillero != null ? tunel.casillero.transform : null;
        SaludJugador s = h.Jugador.GetComponent<SaludJugador>();
        if (s != null) s.GuardarPuntoControl();
        yield return new WaitForSeconds(1f);
        DialogoUI.Susurrar("", "Una estación... nunca la terminaron.", 3.5f);
        h.Objetivo("Revisa la caseta del vigilante");

        yield return h.EsperarHasta(() => tunel.monitor != null && Vector3.Distance(h.Jugador.position, tunel.monitor.position) < 2.8f);
        yield return new WaitForSeconds(1.5f);
        DialogoUI.Susurrar("", "La cinta del monitor se repite... gente corriendo de algo con la cabeza como un sol negro.", 5f);
        yield return new WaitForSeconds(2.5f);
        h.Objetivo("Abre el casillero  [RB]");

        yield return h.EsperarHasta(() => tunel.casillero != null && tunel.casillero.Abierto && !DocumentoUI.Abierto);
        h.Objetivo("Toma la llave del casillero  [RB]");
        yield return h.EsperarHasta(() => llave == null || llave.Tomada);

        yield return new WaitForSeconds(1f);
        yield return tunel.Apagon();
        h.Objetivo("Sal del túnel. Ya.");
        if (LlavesBucle.Instancia != null) { LlavesBucle.Instancia.destino = tunel.puertaSalida; LlavesBucle.Instancia.nombreDestino = "SALIDA"; }
        yield return h.EsperarHasta(() => !tunel.JugadorDentro);
        if (h.Director != null && directorPrendido) h.Director.Habilitar(true);
        if (s != null) s.GuardarPuntoControl();
    }
}
