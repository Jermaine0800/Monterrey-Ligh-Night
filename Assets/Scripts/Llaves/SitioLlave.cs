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

    [Header("Notas (siempre dos, juntas)")]
    [Tooltip("Pistas de ayer: tu propia letra, de un bucle anterior")]
    public NotaLeible notaAyer;
    [Tooltip("De alguien que ya estuvo en el bucle: qué hacer para conseguir la llave")]
    public NotaLeible notaBucle;
    [Tooltip("Segundos dentro del lugar antes de que algo aparezca aunque no leas las notas")]
    public float esperaMonstruo = 240f;

    public bool Completado { get; protected set; }
    /// <summary>El lugar que la historia está jugando ahorita (los demás se pueden visitar en cualquier orden).</summary>
    public static SitioLlave Actual { get; private set; }
    bool llegadaVista;
    /// <summary>Ya se resolvió el reto de este lugar (para no explicarlo otra vez).</summary>
    protected virtual bool RetoHecho => false;

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
        Actual = this;
        LlavesBucle hud = LlavesBucle.Instancia;
        if (hud != null) { hud.destino = transform; hud.nombreDestino = nombreLugar.ToUpper(); }

        // 1) El Vigía aparece adelante, señala hacia el lugar y se va
        Vector3 hacia = transform.position - h.Jugador.position; hacia.y = 0;
        Vector3 punto = Mundo.EnSuelo(h.Jugador.position + hacia.normalized * 9f + Vector3.Cross(Vector3.up, hacia.normalized) * 2.5f);
        yield return h.EscenaVigia(punto, GuiaController.Pose.Senalar, LineasPartida, false, 25f);
        h.Objetivo(ObjetivoLlegar);

        // 2) Te espera en la entrada (solo cuando ya estás cerca)
        yield return h.EsperarHasta(() => h.Distancia(transform.position) < radioLlegada + 25f);
        bool yaVista = llegadaVista; llegadaVista = true;
        if (!yaVista && !string.IsNullOrEmpty(VozAlLlegar)) VozProtagonista.Decir(VozAlLlegar, 0f);
        if (!yaVista && puntoVigia != null && LineasLlegada != null && LineasLlegada.Length > 0)
            yield return h.EscenaVigia(puntoVigia.position, GuiaController.Pose.Sentado, LineasLlegada, false, 40f);
        yield return h.EsperarHasta(() => h.Distancia(transform.position) < radioLlegada);

        SaludJugador s = h.Jugador.GetComponent<SaludJugador>();
        if (s != null) s.GuardarPuntoControl();
        h.StartCoroutine(Presencia(h));

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

    /// <summary>
    /// Al entrar al lugar: las dos notas brillan. Cuando las lees (o pasan ~4 minutos) algo se hace presente:
    /// lo ves y te sigue.
    /// </summary>
    IEnumerator Presencia(MisionManager h)
    {
        if (notaAyer != null && !notaAyer.Leida)
            DialogoUI.Susurrar("", "Hay dos hojas juntas ahí... con mi letra.   [A / E] leer", 4f);
        float t = 0f;
        while (!Completado)
        {
            bool leidas = notaAyer != null && notaBucle != null && notaAyer.Leida && notaBucle.Leida;
            if (leidas || t >= esperaMonstruo) break;
            if (h.Distancia(transform.position) < radioLlegada + 25f) t += Time.deltaTime;
            yield return null;
        }
        if (Completado) yield break;
        while (DocumentoUI.Abierto || DialogoUI.Activo) yield return null;
        yield return new WaitForSeconds(1.2f);
        if (Completado) yield break;

        VHSOverlay.Glitch(0.7f, 0.6f);
        if (MusicaAdaptativa.Instancia != null) MusicaAdaptativa.Instancia.Silencio(2f);
        DialogoUI.Susurrar("", "...ya saben que estoy aquí.", 3f);
        yield return new WaitForSeconds(1.5f);
        // Si hay otro monstruo ocupado, espera a que se vaya y vuelve a intentar
        for (int i = 0; i < 40 && !Completado; i++)
        {
            AcechoDirector d = AcechoDirector.Instancia;
            if (d != null && !d.HayMonstruoActivo) { d.ForzarEn(h.Jugador.position); yield break; }
            yield return new WaitForSeconds(3f);
        }
    }

    // ================================================================== Llegar en cualquier orden
    // Si llegas a un lugar que todavía no te tocaba: cinemática del lugar, el Vigía te explica qué hacer
    // y te avisa de las notas. Así cada punto tiene su presentación aunque explores libre.
    void LateUpdate()
    {
        if (llegadaVista || Completado || Actual == this || RetoHecho || (llave != null && llave.Tomada)) return;
        if (SaludJugador.Instancia == null || !HorrorGameState.PuedeJugar || DialogoUI.Activo) return;
        Vector3 a = transform.position - SaludJugador.Instancia.transform.position; a.y = 0;
        if (a.magnitude > radioLlegada + 12f) return;
        llegadaVista = true;
        MisionManager mm = MisionManager.Instancia;
        if (mm != null) mm.StartCoroutine(LlegadaLibre(mm));
    }

    IEnumerator LlegadaLibre(MisionManager h)
    {
        yield return h.CinematicaLugar(transform, nombreLugar);
        if (!string.IsNullOrEmpty(VozAlLlegar)) VozProtagonista.Decir(VozAlLlegar, 0f);
        if (puntoVigia != null && LineasLlegada != null && LineasLlegada.Length > 0)
            yield return h.EscenaVigia(puntoVigia.position, GuiaController.Pose.Sentado, LineasLlegada, false, 40f);
        if (notaAyer != null && !notaAyer.Leida)
            DialogoUI.Susurrar("", "Hay dos hojas juntas por aquí... con mi letra. Léelas: dicen qué hacer.   [A / E]", 4.5f);
        h.StartCoroutine(Presencia(h));
    }
}
