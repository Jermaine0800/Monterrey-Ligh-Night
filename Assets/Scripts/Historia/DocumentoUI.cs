using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Muestra un documento en pantalla (papel viejo) hasta que lo cierras con A / B / RB.</summary>
public static class DocumentoUI
{
    public static bool Abierto { get; private set; }

    public static IEnumerator Mostrar(string titulo, string cuerpo, float espera = 0f)
    {
        if (Abierto) yield break;
        Abierto = true;
        if (espera > 0f) yield return new WaitForSeconds(espera);

        HorrorGameState.Modo previo = HorrorGameState.modo;
        HorrorGameState.modo = HorrorGameState.Modo.Dialogo;

        Canvas c = HorrorUI.CrearCanvas("Documento_Canvas", 650);
        Image fondo = HorrorUI.CrearImagen(c.transform, "Oscuro", new Color(0, 0, 0, 0.75f));
        HorrorUI.Estirar(fondo.rectTransform);

        Image papel = HorrorUI.CrearImagen(c.transform, "Papel", new Color(0.78f, 0.74f, 0.62f));
        HorrorUI.Colocar(papel.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 900));
        papel.rectTransform.localRotation = Quaternion.Euler(0, 0, -1.2f);

        Text t = HorrorUI.CrearTexto(papel.transform, "Titulo", titulo, 34, new Color(0.45f, 0.05f, 0.04f), TextAnchor.UpperCenter, FontStyle.Bold);
        HorrorUI.Colocar(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -50), new Vector2(820, 60));
        Text b = HorrorUI.CrearTexto(papel.transform, "Cuerpo", cuerpo, 23, new Color(0.12f, 0.11f, 0.1f), TextAnchor.UpperLeft);
        HorrorUI.Colocar(b.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -30), new Vector2(800, 740));
        Text sello = HorrorUI.CrearTexto(papel.transform, "Sello", "CONFIDENCIAL", 54, new Color(0.6f, 0.05f, 0.04f, 0.35f), TextAnchor.MiddleCenter, FontStyle.Bold);
        HorrorUI.Colocar(sello.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(120, -260), new Vector2(600, 90));
        sello.rectTransform.localRotation = Quaternion.Euler(0, 0, 18f);

        Text ayuda = HorrorUI.CrearTexto(c.transform, "Ayuda", "[A] / [B]  cerrar", 22, HorrorUI.HuesoTenue, TextAnchor.LowerRight);
        HorrorUI.Colocar(ayuda.rectTransform, new Vector2(1f, 0f), new Vector2(-180, 40), new Vector2(300, 40));

        AudioSource a = c.gameObject.AddComponent<AudioSource>();
        a.PlayOneShot(ProceduralAudio.Rechinido(3), 0.3f);

        yield return new WaitForSeconds(0.6f);
        while (!(HorrorInput.Aceptar || HorrorInput.Cancelar || HorrorInput.RB)) yield return null;

        Object.Destroy(c.gameObject);
        HorrorGameState.modo = previo == HorrorGameState.Modo.Dialogo ? HorrorGameState.Modo.Jugando : previo;
        yield return null;
        Abierto = false;
    }
}
