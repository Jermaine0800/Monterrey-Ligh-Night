using System.Collections;
using UnityEngine;

/// <summary>
/// LLAVE 1 — EL CAMPAMENTO del equipo de Urbex. Casas de campaña rotas, una fogata que se apaga sola
/// y un radio que todavía transmite. La llave está tirada en algún lado... solo se ve con la linterna.
/// Cuando la tomas, La Sonriente ya está ahí.
/// </summary>
public class SitioCampamento : SitioLlave
{
    public Light fogata;
    public Transform radio;

    public override string[] LineasPartida => new[]
    {
        "Senalar|Tus amigos acamparon por allá. Dejaron algo. Siempre dejan algo.",
        "Hablar|Son cinco llaves. Cinco. Juntas abren la salida de esta noche. Sí, esta noche se repite. ¿No lo habías notado?",
        "Reir|Ve. Yo no puedo acercarme a las fogatas. Me caen mal.",
    };

    public override string[] LineasLlegada => new[]
    {
        "Sentado|Aquí se separaron. Uno gritó, los otros corrieron. Clásico.",
        "Hablar|La llave no brilla sola. Ilumínala. Y no te tardes: aquí ya hay alguien más buscando.",
    };

    public override string VozAlLlegar => "campamento";

    public override string ObjetivoLlegar => "Llave 1: el campamento del equipo";

    FogataCampamento fuego;
    protected override bool RetoHecho => fuego != null && fuego.Prendida;

    void Start()
    {
        if (fogata != null)
        {
            fuego = fogata.GetComponent<FogataCampamento>();
            if (fuego == null) { fuego = fogata.gameObject.AddComponent<FogataCampamento>(); }
            fuego.luz = fogata; fuego.sitio = this;
        }
        // Carro viejo junto al campamento: arréglalo y su alarma espanta a los monstruos
        if (FindAnyObjectByType<CarroCampamento>() == null)
        {
            Vector3 lugar = LugarLibre(out float giro);
            CarroCampamento.Crear(lugar, giro);
        }
    }

    protected override IEnumerator Reto(MisionManager h)
    {
        // El radio todavía transmite
        if (radio != null)
        {
            AudioSource a = radio.GetComponent<AudioSource>();
            if (a == null) a = radio.gameObject.AddComponent<AudioSource>();
            a.spatialBlend = 1f; a.maxDistance = 20f; a.rolloffMode = AudioRolloffMode.Linear;
            a.clip = ProceduralAudio.Estatica(23); a.loop = true; a.volume = 0.35f; a.Play();
        }
        yield return new WaitForSeconds(1.5f);
        DialogoUI.Susurrar("RADIO", "...¿alguien me copia? ...no suban al cerro... la luz... ¡NO ES EL GUÍA!...", 5f);

        // Hay que volver a prender la fogata: el fuego deja ver la llave
        if (fuego != null && !fuego.Prendida)
        {
            h.Objetivo("Prende la fogata  (mantén [A] junto al fuego)");
            yield return h.EsperarHasta(() => fuego.Prendida);
            yield return new WaitForSeconds(3f);
        }
        h.Objetivo("Toma la llave junto al fuego  [A]");
    }

    protected override IEnumerator AlTomarLlave(MisionManager h)
    {
        yield return new WaitForSeconds(1.2f);
        SoltarMonstruo(h.Jugador.position);
        DialogoUI.Susurrar("", "...algo se movió detrás de las casas de campaña.", 3f);
    }

    /// <summary>Busca alrededor del campamento un lugar plano y despejado (sin casas de campaña, árboles ni rocas) para el carro.</summary>
    Vector3 LugarLibre(out float giro)
    {
        Terrain ter = Terrain.activeTerrain;
        for (int intento = 0; intento < 60; intento++)
        {
            float ang = intento * 37f;
            float dist = 9f + (intento % 6) * 1.5f;
            Vector3 p = Mundo.EnSuelo(transform.position + Quaternion.Euler(0, ang, 0) * transform.forward * dist);
            giro = ang + 90f;
            Quaternion q = Quaternion.Euler(0, giro, 0);
            bool libre = true;
            foreach (Collider c in Physics.OverlapBox(p + Vector3.up * 1.1f, new Vector3(1.6f, 0.9f, 3.2f), q, ~0, QueryTriggerInteraction.Ignore))
                if (!(c is TerrainCollider)) { libre = false; break; }
            if (!libre) continue;
            // que no esté en pendiente fuerte
            float h1 = Mundo.AlturaSuelo(p + q * Vector3.forward * 2f), h2 = Mundo.AlturaSuelo(p - q * Vector3.forward * 2f);
            if (Mathf.Abs(h1 - h2) > 0.6f) continue;
            // y que no haya árboles del terreno encima
            bool arbol = false;
            foreach (Vector3 a in Mundo.ArbolesCerca(p, 0f, 3.5f, 3)) { arbol = true; break; }
            if (arbol) continue;
            return p;
        }
        giro = transform.eulerAngles.y + 90f;
        return Mundo.EnSuelo(transform.position + transform.forward * 12f);
    }
}
