using System.Collections;
using UnityEngine;

/// <summary>
/// ACTO II — TÚNEL LÍNEA K. Un muro de concreto entre la maleza; la linterna revela
/// la puerta del EXPEDIENTE 08. Adentro: andén sin terminar, la caseta con el monitor
/// y el casillero con la batería y el documento clasificado.
/// </summary>
public class CapActoII : CapituloBase
{
    public override string Titulo => "Acto II: Túnel Línea K";

    public override IEnumerator Jugar(MisionManager h)
    {
        TunelLineaK tunel = h.tunel;
        if (tunel == null) { Debug.LogWarning("[Historia] No hay Túnel Línea K en la escena (menú Monterrey/10)."); yield break; }

        // 1) Llegar al muro
        yield return h.EsperarHasta(() => h.Distancia(tunel.muro.position) < 12f, true, () => tunel.muro.position);
        DialogoUI.Susurrar("", "Concreto... aquí, en medio del cerro. Hay algo pintado.", 4f);
        h.Objetivo("Alumbra la marca pintada  [X]");

        // 2) La linterna revela la puerta
        yield return h.EsperarHasta(() => tunel.Revelado);
        h.Objetivo("Entra  —  EXPEDIENTE 08  [RB]");

        // 3) Adentro
        yield return h.EsperarHasta(() => tunel.JugadorDentro);
        if (h.Director != null) h.Director.Habilitar(false);
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

        // 4) Apagón: algo en las vías
        yield return new WaitForSeconds(1.5f);
        yield return tunel.Apagon();
        h.Objetivo("Sal del túnel. Ya.");

        yield return h.EsperarHasta(() => !tunel.JugadorDentro);
    }
}
