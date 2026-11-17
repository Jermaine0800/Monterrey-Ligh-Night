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

        // La fogata se apaga sola
        if (fogata != null)
        {
            float i0 = fogata.intensity;
            for (float t = 0; t < 3f; t += Time.deltaTime) { fogata.intensity = Mathf.Lerp(i0, 0.15f, t / 3f); yield return null; }
        }
        h.Objetivo("Busca la llave con la linterna  [X]");
    }

    protected override IEnumerator AlTomarLlave(MisionManager h)
    {
        yield return new WaitForSeconds(1.2f);
        SoltarMonstruo(h.Jugador.position);
        DialogoUI.Susurrar("", "...algo se movió detrás de las casas de campaña.", 3f);
    }
}
