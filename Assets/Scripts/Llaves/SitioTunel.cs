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
        if (!tunel.Revelado)
        {
            h.Objetivo("Alumbra la marca roja del muro con la linterna  [X]");
            yield return h.EsperarHasta(() => tunel.Revelado);
        }
        h.Objetivo("Entra  —  EXPEDIENTE 08  [A]");
        yield return h.EsperarHasta(() => tunel.JugadorDentro);
        SaludJugador s = h.Jugador.GetComponent<SaludJugador>();
        if (s != null) s.GuardarPuntoControl();
        yield return new WaitForSeconds(1f);
        DialogoUI.Susurrar("", "Una estación... y un vagón del metro. Nunca lo sacaron de aquí.", 3.5f);
        MetroTunel m = MetroTunel.Instancia;
        Transform vagon = tunel.interior != null ? tunel.interior.Find("Vagon_Piso") : null;
        if (LlavesBucle.Instancia != null && vagon != null) { LlavesBucle.Instancia.destino = vagon; LlavesBucle.Instancia.nombreDestino = "VAGÓN"; }
        h.Objetivo("Entra al vagón del metro");

        // La pelea la maneja el MetroTunel: hasta que la venzas no sales. Luego la llave aparece en el vagón.
        yield return h.EsperarHasta(() => llave == null || llave.Tomada);
        yield return new WaitForSeconds(1f);
        h.Objetivo("Sal del túnel");
        if (LlavesBucle.Instancia != null) { LlavesBucle.Instancia.destino = tunel.puertaSalida; LlavesBucle.Instancia.nombreDestino = "SALIDA"; }
        yield return h.EsperarHasta(() => !tunel.JugadorDentro);
        if (h.Director != null) h.Director.Habilitar(true);
        if (s != null) s.GuardarPuntoControl();
    }
}
