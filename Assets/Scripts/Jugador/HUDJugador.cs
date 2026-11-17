using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra de vida (estilo cinta: bloques) y el arma que traes en la mano.
/// Se esconde en menús, diálogos y cinemáticas.
/// </summary>
public class HUDJugador : MonoBehaviour
{
    Image fondo, barra;
    Text textoVida, textoArma;
    SaludJugador salud;
    PlayerCombate combate;
    float mostrado = 1f, parpadeo;

    void Start()
    {
        Canvas c = HorrorUI.CrearCanvas("HUD_Jugador", 121, transform);

        textoVida = HorrorUI.CrearTexto(c.transform, "TextoVida", "VIDA", 22, HorrorUI.Hueso, TextAnchor.MiddleLeft);
        Fijar(textoVida.rectTransform, new Vector2(70, -200), new Vector2(90, 26));

        fondo = HorrorUI.CrearImagen(c.transform, "BarraFondo", new Color(0, 0, 0, 0.55f));
        Fijar(fondo.rectTransform, new Vector2(160, -200), new Vector2(300, 18));
        barra = HorrorUI.CrearImagen(fondo.transform, "Barra", HorrorUI.Hueso);
        RectTransform rt = barra.rectTransform;
        rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(0, 0.5f);
        rt.offsetMin = new Vector2(2, 2); rt.offsetMax = new Vector2(-2, -2);

        textoArma = HorrorUI.CrearTexto(c.transform, "Arma", "", 22, HorrorUI.HuesoTenue, TextAnchor.MiddleLeft);
        Fijar(textoArma.rectTransform, new Vector2(70, -232), new Vector2(500, 26));
    }

    static void Fijar(RectTransform rt, Vector2 pos, Vector2 tam)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
    }

    void Update()
    {
        if (salud == null) salud = SaludJugador.Instancia != null ? SaludJugador.Instancia : FindAnyObjectByType<SaludJugador>();
        if (combate == null) combate = FindAnyObjectByType<PlayerCombate>();
        bool ver = HorrorGameState.PuedeJugar;
        textoVida.enabled = fondo.enabled = barra.enabled = textoArma.enabled = ver;
        if (!ver || salud == null) return;

        float v = salud.Vida01;
        mostrado = Mathf.MoveTowards(mostrado, v, Time.deltaTime * 0.8f);
        barra.rectTransform.localScale = new Vector3(Mathf.Max(0.001f, mostrado), 1, 1);

        Color c = Color.Lerp(HorrorUI.Sangre, HorrorUI.Hueso, Mathf.InverseLerp(0.2f, 0.7f, v));
        if (v < 0.3f) { parpadeo += Time.deltaTime * 6f; c.a = 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(parpadeo)); }
        barra.color = c;

        textoArma.text = combate != null && combate.TieneArma ? "▌ " + combate.NombreArma.ToUpper() + "   " + combate.UsosArma + " golpes" : "▌ manos";
    }
}
