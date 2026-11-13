using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// ACTO III — EL MIRADOR. La niebla se pone roja, la cinta tiembla. Una baliza parpadea
/// en el mirador. Cuando llegas, el Guía cae de las copas de los árboles frente a la cámara,
/// la imagen se congela y la cinta se corta: SEÑAL PERDIDA.
/// </summary>
public class CapActoIII : CapituloBase
{
    public override string Titulo => "Acto III: el mirador";

    public override IEnumerator Jugar(MisionManager h)
    {
        SaludJugador salud = h.Jugador.GetComponent<SaludJugador>();
        if (salud != null) salud.GuardarPuntoControl();
        h.StartCoroutine(NieblaRoja(8f));
        VHSOverlay.Glitch(0.8f, 1.5f);
        yield return new WaitForSeconds(2f);

        Vector3 cerca = h.Adelante(6f, 25f);
        yield return h.EscenaVigia(cerca, GuiaController.Pose.Intimidar, new[]
        {
            "Intimidar|Leíste el expediente. Qué mal. Ahora él sabe que sabes.",
            "Hablar|¿Que quién es él? Ay, por favor. Lee otra vez la designación.",
            "Senalar|¿La salida? Arriba, en el mirador. Donde parpadea la luz roja. Te lo prometo.",
            "Reir|Yo nunca miento. Bueno... casi nunca. Bueno...",
        });

        if (h.Director != null)
        {
            h.Director.tensionPorSegundo *= 1.6f;
            h.Director.Habilitar(true);
        }

        if (h.mirador == null) { Debug.LogWarning("[Historia] No hay mirador en la escena (menú Monterrey/10)."); yield break; }
        h.Objetivo("Sube al mirador  (sigue la baliza roja)");
        yield return h.EsperarHasta(() => h.Distancia(h.mirador.position) < 6f, true, () => h.mirador.position);

        if (h.Director != null) h.Director.Habilitar(false);
        yield return new WaitForSeconds(2.5f);   // un momento de calma... demasiado
        yield return Caida(h);
        yield return SenalPerdida(h);
    }

    static IEnumerator NieblaRoja(float segundos)
    {
        Color desde = RenderSettings.fogColor, hasta = new Color(0.16f, 0.02f, 0.02f);
        float d0 = RenderSettings.fogDensity, d1 = d0 + 0.012f;
        for (float t = 0; t < segundos; t += Time.deltaTime)
        {
            float k = t / segundos;
            RenderSettings.fogColor = Color.Lerp(desde, hasta, k);
            RenderSettings.fogDensity = Mathf.Lerp(d0, d1, k);
            yield return null;
        }
    }

    /// <summary>El Guía cae de las copas de los árboles justo frente a la cámara.</summary>
    static IEnumerator Caida(MisionManager h)
    {
        HorrorGameState.modo = HorrorGameState.Modo.Final;
        Camera cam = Camera.main;
        GuiaController g = h.guia;

        Vector3 f = cam.transform.forward; f.y = 0; f.Normalize();
        Vector3 suelo = Mundo.EnSuelo(cam.transform.position + f * 1.4f);
        g.Aparecer(suelo, cam.transform.position, GuiaController.Pose.Intimidar);

        // Ramas que truenan arriba
        AudioSource s = g.GetComponent<AudioSource>();
        if (s != null) { s.PlayOneShot(ProceduralAudio.Rama(7), 1f); }

        Vector3 arriba = suelo + Vector3.up * 9f;
        for (float t = 0; t < 0.28f; t += Time.deltaTime)
        {
            g.transform.position = Vector3.Lerp(arriba, suelo, (t / 0.28f) * (t / 0.28f));
            yield return null;
        }
        g.transform.position = suelo;
        g.Animar(GuiaController.Pose.Intimidar, 0f);
        if (s != null) { s.spatialBlend = 0f; s.PlayOneShot(ProceduralAudio.Grito(666), 1f); s.PlayOneShot(ProceduralAudio.GolpeGrave(), 1f); }
        VHSOverlay.Glitch(1f, 1f);
        HorrorInput.Vibrar(1f, 1f, 0.8f, h);
        yield return new WaitForSeconds(0.45f);
    }

    /// <summary>Imagen congelada, barras de tracking, pitido agudo y corte a negro.</summary>
    IEnumerator SenalPerdida(MisionManager h)
    {
        Time.timeScale = 0f;
        AudioListener.pause = false;

        Canvas c = HorrorUI.CrearCanvas("SenalPerdida_Canvas", 900);
        RectTransform barras = HorrorUI.Rect(new GameObject("Barras"));
        barras.SetParent(c.transform, false);
        HorrorUI.Estirar(barras);
        Image[] b = new Image[7];
        for (int i = 0; i < b.Length; i++)
        {
            b[i] = HorrorUI.CrearImagen(barras, "Barra" + i, new Color(1, 1, 1, Random.Range(0.25f, 0.6f)));
            b[i].rectTransform.anchorMin = new Vector2(0, 0); b[i].rectTransform.anchorMax = new Vector2(1, 0);
            b[i].rectTransform.sizeDelta = new Vector2(0, Random.Range(6f, 26f));
        }

        AudioSource pitido = c.gameObject.AddComponent<AudioSource>();
        pitido.ignoreListenerPause = true;
        pitido.spatialBlend = 0f;
        pitido.clip = Pitido();
        pitido.loop = true;
        pitido.volume = 0.35f;
        pitido.Play();

        for (float t = 0; t < 2.6f; t += Time.unscaledDeltaTime)
        {
            for (int i = 0; i < b.Length; i++)
            {
                float y = Mathf.Repeat(t * (90f + i * 37f) + i * 140f, 1100f) - 40f;
                b[i].rectTransform.anchoredPosition = new Vector2(Random.Range(-30f, 30f), y);
            }
            yield return null;
        }
        pitido.Stop();

        Image negro = HorrorUI.CrearImagen(c.transform, "Negro", Color.black);
        HorrorUI.Estirar(negro.rectTransform);
        Text t1 = HorrorUI.CrearTexto(c.transform, "T1", "", 40, HorrorUI.Hueso, TextAnchor.MiddleCenter);
        HorrorUI.Colocar(t1.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(1400, 120));
        Text t2 = HorrorUI.CrearTexto(c.transform, "T2", "", 28, HorrorUI.Sangre, TextAnchor.MiddleCenter);
        HorrorUI.Colocar(t2.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -50), new Vector2(1400, 80));

        yield return new WaitForSecondsRealtime(1.2f);
        yield return Maquina(t1, "SEÑAL PERDIDA  //  ERROR DE LECTURA EN CINTA.", 22f, null, null, true);
        yield return new WaitForSecondsRealtime(0.8f);
        yield return Maquina(t2, "NO SE LOCALIZÓ AL SUJETO.", 18f, null, null, true);
        yield return new WaitForSecondsRealtime(6f);

        Time.timeScale = 1f;
        PlayerPrefs.SetInt("partida_guardada", 0);
        if (Application.CanStreamedLevelBeLoaded(h.escenaMenu)) SceneManager.LoadScene(h.escenaMenu);
    }

    static AudioClip Pitido()
    {
        int hz = 44100, n = hz / 2;
        float[] d = new float[n];
        for (int i = 0; i < n; i++) d[i] = Mathf.Sin(2f * Mathf.PI * 2200f * i / hz) * 0.6f;
        AudioClip a = AudioClip.Create("Pitido", n, 1, hz, false);
        a.SetData(d, 0);
        return a;
    }
}
