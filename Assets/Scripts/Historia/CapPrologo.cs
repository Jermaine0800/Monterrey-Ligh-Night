using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PRÓLOGO — la cinta recuperada. Texto de máquina sobre negro y luego "PLAY".
/// </summary>
public class CapPrologo : CapituloBase
{
    public override string Titulo => "Prólogo: cinta recuperada";

    const string Texto =
        "27 de Noviembre de 1998.\n\n" +
        "Expedición de exploración urbana (Urbex) en las faldas del Cerro de la Silla.\n\n" +
        "El equipo se separó en el Sector La Pastora.\n\n" +
        "Esta cinta fue recuperada por la policía local semanas después.\n\n" +
        "Se recomienda discreción.";

    // Tiempos en segundos del video IntroLineaK.mp4 (24 fps → cuadro / 24)
    static readonly CinematicaVideo.Sub[] Subtitulos =
    {
        new CinematicaVideo.Sub(0.3f, 3.6f, "", "27/11/1998  ·  11:31 PM  ·  Cerro de la Silla, Sector La Pastora"),
        new CinematicaVideo.Sub(4.0f, 8.4f, "MARIO", "Según el foro, la entrada de la Línea K está por aquí. Nunca la terminaron."),
        new CinematicaVideo.Sub(8.8f, 11.0f, "CHUY", "Ya es bien noche, güey. ¿Y si nos regresamos?"),
        new CinematicaVideo.Sub(11.2f, 13.8f, "MARIO", "Mira. Expediente cero ocho. Es aquí."),
        new CinematicaVideo.Sub(14.3f, 16.6f, "TÚ", "Sigue grabando... sigue grabando."),
        new CinematicaVideo.Sub(19.6f, 21.4f, "CHUY", "Esperen... ¿oyeron eso?"),
        new CinematicaVideo.Sub(22.0f, 24.0f, "MARIO", "Hay alguien allá al fondo... ¿Hola?"),
        new CinematicaVideo.Sub(24.1f, 25.0f, "CHUY", "No es una persona... ¡no es una persona!"),
        new CinematicaVideo.Sub(27.2f, 29.4f, "MARIO", "¡Corre! ¡CORRE!"),
        new CinematicaVideo.Sub(31.6f, 34.0f, "TÚ", "No... no puedo moverme..."),
    };

    public override IEnumerator Jugar(MisionManager h)
    {
        HorrorGameState.modo = HorrorGameState.Modo.Dialogo;

        Canvas c = PantallaNegra("Prologo_Canvas", out Image negro);
        Text t = HorrorUI.CrearTexto(c.transform, "Texto", "", 30, HorrorUI.Hueso, TextAnchor.MiddleLeft);
        HorrorUI.Colocar(t.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100, 520));
        Text sello = HorrorUI.CrearTexto(c.transform, "Sello", "EVIDENCIA  #  NL-98-1127", 22, new Color(HorrorUI.Sangre.r, HorrorUI.Sangre.g, HorrorUI.Sangre.b, 0f), TextAnchor.LowerRight);
        HorrorUI.Colocar(sello.rectTransform, new Vector2(1f, 0f), new Vector2(-220, 60), new Vector2(400, 40));

        AudioSource fuente = c.gameObject.AddComponent<AudioSource>();
        fuente.spatialBlend = 0f;
        AudioClip clic = ProceduralAudio.ClicMenu();

        yield return new WaitForSeconds(1f);
        yield return Maquina(t, Texto, 26f, fuente, clic);
        yield return Fundido(sello, 0f, 0.9f, 0.6f);

        float espera = 0f;
        while (espera < 4f && !HorrorInput.Aceptar) { espera += Time.deltaTime; yield return null; }

        // PLAY ▶ y entra la imagen
        t.text = "";
        sello.text = "";
        Text play = HorrorUI.CrearTexto(c.transform, "Play", "PLAY  ▶", 46, HorrorUI.Hueso, TextAnchor.UpperLeft);
        HorrorUI.Colocar(play.rectTransform, new Vector2(0f, 1f), new Vector2(200, -70), new Vector2(360, 60));
        fuente.PlayOneShot(ProceduralAudio.Estatica(5), 0.5f);
        VHSOverlay.Glitch(1f, 1.2f);
        yield return new WaitForSeconds(1.2f);

        play.text = "";
        MusicaFondo.Desvanecer(3f);

        // La cinta: lo que pasó en la Línea K (renderizado en Blender)
        c.enabled = false;   // la pantalla negra del prólogo va encima de todo; se esconde mientras corre la cinta
        yield return CinematicaVideo.Reproducir("IntroLineaK.mp4", Subtitulos);
        c.enabled = true;

        // Señal perdida
        Text perdida = HorrorUI.CrearTexto(c.transform, "Perdida", "S E Ñ A L   P E R D I D A", 40, HorrorUI.Sangre, TextAnchor.MiddleCenter);
        HorrorUI.Colocar(perdida.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200, 80));
        fuente.PlayOneShot(ProceduralAudio.Estatica(9), 0.7f);
        VHSOverlay.Glitch(1f, 2f);
        yield return new WaitForSeconds(2.2f);
        perdida.text = "";
        Text horas = HorrorUI.CrearTexto(c.transform, "Horas", "", 28, HorrorUI.Hueso, TextAnchor.MiddleCenter);
        HorrorUI.Colocar(horas.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200, 80));
        yield return Maquina(horas, "...horas después.", 18f, fuente, clic);
        yield return new WaitForSeconds(1.4f);
        horas.text = "";

        h.StartCoroutine(Fundido(negro, 1f, 0f, 2.5f));
        yield return h.CinematicaIntro();
        Object.Destroy(c.gameObject);
        yield return h.Despertar();
        HorrorGameState.modo = HorrorGameState.Modo.Jugando;
        yield return new WaitForSeconds(0.6f);
        VozProtagonista.Decir("despertar", 0f);
    }
}
