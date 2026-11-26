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

    public bool CriptaAbierta { get; private set; }
    protected override bool RetoHecho => CriptaAbierta || abriendo;
    int prendidasVistas;
    bool abriendo;

    // Funciona en cualquier orden: aunque todavía no te toque el panteón, si prendes las velas, pasa.
    void Update()
    {
        if (velas == null || velas.Length == 0 || CriptaAbierta || abriendo) return;
        int prendidas = 0;
        foreach (VelaPanteon v in velas) if (v != null && v.Prendida) prendidas++;
        if (prendidas != prendidasVistas)
        {
            prendidasVistas = prendidas;
            Transform j = SaludJugador.Instancia != null ? SaludJugador.Instancia.transform : null;
            if (j != null && prendidas == 1) Asomarse(j.position);                                   // algo te mira entre las cruces
            if (j != null && prendidas == 2 && prendidas < velas.Length) SoltarMonstruo(j.position);  // y luego viene
            DialogoUI.NuevoObjetivo(HorrorInput.Traducir("Prende las velas de las tumbas con calavera  (mantén [A])   " + prendidas + "/" + velas.Length));
        }
        if (prendidas >= velas.Length) { abriendo = true; StartCoroutine(AbrirCripta()); }
    }

    /// <summary>Pequeña cinemática: la cámara voltea a la cripta, la tierra tiembla y la reja se abre sola.</summary>
    IEnumerator AbrirCripta()
    {
        yield return new WaitForSeconds(0.8f);
        MisionManager mm = FindAnyObjectByType<MisionManager>();
        Transform reja = rejaCripta != null ? rejaCripta.transform : transform;
        Vector3 objetivo = reja.position + Vector3.up * 1.2f;
        if (mm != null && Camera.main != null)
        {
            Vector3 cam = Camera.main.transform.position;
            Vector3 hasta = objetivo + (cam - objetivo).normalized * 4.5f + Vector3.up * 0.6f;
            StartCoroutine(AbrirDuranteToma());
            yield return mm.Toma(cam + Vector3.up * 0.8f, hasta, objetivo, 4.2f, "LAS TRES VELAS", "la cripta se abre");
        }
        else
        {
            if (rejaCripta != null) rejaCripta.Abrir();
        }
        CriptaAbierta = true;
        if (llave != null)
        {
            GameObject lz = new GameObject("LuzCripta");
            lz.transform.position = llave.transform.position + Vector3.up * 1.2f;
            Light l = lz.AddComponent<Light>(); l.type = LightType.Point; l.range = 6f; l.intensity = 2.2f; l.color = new Color(1f, 0.55f, 0.25f);
            if (LlavesBucle.Instancia != null) { LlavesBucle.Instancia.destino = llave.transform; LlavesBucle.Instancia.nombreDestino = "LLAVE 2"; }
        }
        DialogoUI.Susurrar("", "La reja de la cripta... se abrió sola. Y algo más despertó.", 3.5f);
        DialogoUI.NuevoObjetivo("Entra a la cripta y toma la llave");
        // Los dos se activan: uno ya viene y el otro acecha
        yield return new WaitForSeconds(2f);
        Transform j = SaludJugador.Instancia != null ? SaludJugador.Instancia.transform : null;
        if (j != null)
        {
            SoltarMonstruo(j.position);
            if (AcechoDirector.Instancia != null) { AcechoDirector.Instancia.Habilitar(true); AcechoDirector.Instancia.ForzarAparicion(); }
        }
    }

    IEnumerator AbrirDuranteToma()
    {
        yield return new WaitForSeconds(1.4f);
        VHSOverlay.Glitch(0.9f, 0.9f);
        HorrorInput.Vibrar(0.7f, 0.4f, 0.8f, this);
        AudioSource.PlayClipAtPoint(ProceduralAudio.Rechinido(5), rejaCripta != null ? rejaCripta.transform.position : transform.position, 1f);
        if (rejaCripta != null) rejaCripta.Abrir();
    }

    protected override IEnumerator Reto(MisionManager h)
    {
        int total = velas != null ? velas.Length : 0;
        if (!CriptaAbierta) h.Objetivo("Prende las velas de las tumbas con calavera  (mantén [A])   " + prendidasVistas + "/" + total);
        while (!CriptaAbierta && total > 0) yield return null;
        h.Objetivo("Entra a la cripta y toma la llave");
    }

    /// <summary>Un monstruo se deja ver un momento entre las tumbas (no ataca).</summary>
    void Asomarse(Vector3 jugador)
    {
        EnemigoBase[] todos = FindObjectsByType<EnemigoBase>(FindObjectsSortMode.None);
        if (todos.Length == 0) return;
        EnemigoBase e = todos[Random.Range(0, todos.Length)];
        if (!e.isActiveAndEnabled) return;
        Vector3 punto = Mundo.PuntoFueraDeVista(jugador, 9f, 15f);
        e.StartCoroutine(e.Vistazo(punto, 6f));
    }
}
