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

        HorrorGameState.modo = HorrorGameState.Modo.Jugando;
        yield return Fundido(negro, 1f, 0f, 2.5f);
        Object.Destroy(c.gameObject);
    }
}
