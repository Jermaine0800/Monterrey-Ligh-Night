using System.Collections;
using UnityEngine;

/// <summary>
/// Un lugar donde está una de las 5 llaves (campamento, panteón, túnel, iglesia, mirador).
/// Plantilla común: el Vigía te manda hacia allá → llegas → el reto propio del lugar → tomas la llave.
/// Cada lugar hereda y decide su reto (polimorfismo): <see cref="Reto"/>.
/// </summary>
public abstract class SitioLlave : MonoBehaviour
{
    [Header("Sitio")]
    public string nombreLugar = "???";
    public LlaveBucle llave;
    [Tooltip("Donde el Vigía te espera al llegar (a la entrada del lugar)")]
    public Transform puntoVigia;
    public float radioLlegada = 14f;

    public bool Completado { get; protected set; }

    /// <summary>Lo que dice el Vigía cuando te manda hacia acá (al terminar el lugar anterior).</summary>
    public abstract string[] LineasPartida { get; }
    /// <summary>Lo que dice al llegar (pista del reto).</summary>
    public abstract string[] LineasLlegada { get; }
    public abstract string ObjetivoLlegar { get; }
    /// <summary>Frase del protagonista al ver el lugar (id de VozProtagonista).</summary>
    public virtual string VozAlLlegar => null;

    /// <summary>Flujo completo del lugar. Normalmente no se sobreescribe; se sobreescribe <see cref="Reto"/>.</summary>
    public virtual IEnumerator Jugar(MisionManager h)
    {
        LlavesBucle hud = LlavesBucle.Instancia;
        if (hud != null) { hud.destino = transform; hud.nombreDestino = nombreLugar.ToUpper(); }

        // 1) El Vigía aparece adelante, señala hacia el lugar y se va
        Vector3 hacia = transform.position - h.Jugador.position; hacia.y = 0;
        Vector3 punto = Mundo.EnSuelo(h.Jugador.position + hacia.normalized * 9f + Vector3.Cross(Vector3.up, hacia.normalized) * 2.5f);
        yield return h.EscenaVigia(punto, GuiaController.Pose.Senalar, LineasPartida, false, 25f);
        h.Objetivo(ObjetivoLlegar);

        // 2) Te espera en la entrada (solo cuando ya estás cerca)
        yield return h.EsperarHasta(() => h.Distancia(transform.position) < radioLlegada + 25f);
        if (!string.IsNullOrEmpty(VozAlLlegar)) VozProtagonista.Decir(VozAlLlegar, 0f);
        if (puntoVigia != null && LineasLlegada != null && LineasLlegada.Length > 0)
            yield return h.EscenaVigia(puntoVigia.position, GuiaController.Pose.Sentado, LineasLlegada, false, 40f);
        yield return h.EsperarHasta(() => h.Distancia(transform.position) < radioLlegada);

        SaludJugador s = h.Jugador.GetComponent<SaludJugador>();
        if (s != null) s.GuardarPuntoControl();

        // 3) El reto propio del lugar
        yield return Reto(h);

        // 4) La llave
        if (llave != null && !llave.Tomada)
        {
            llave.Disponible = true;
            yield return h.EsperarHasta(() => llave.Tomada);
        }
        VozProtagonista.Decir("llave", 0f);
        yield return AlTomarLlave(h);
        Completado = true;
        if (hud != null) hud.destino = null;
    }

    /// <summary>Lo que hay que hacer en este lugar antes (o para) poder tomar la llave.</summary>
    protected abstract IEnumerator Reto(MisionManager h);

    /// <summary>Lo que pasa justo después de tomar la llave (persecuciones, sustos...).</summary>
    protected virtual IEnumerator AlTomarLlave(MisionManager h) { yield break; }

    /// <summary>Suelta a un monstruo cerca de un punto (para sustos del lugar).</summary>
    protected void SoltarMonstruo(Vector3 cerca)
    {
        if (AcechoDirector.Instancia != null) AcechoDirector.Instancia.ForzarEn(cerca);
    }
}
