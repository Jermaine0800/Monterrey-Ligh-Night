using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra de vida (estilo cinta: bloques) y el arma que traes en la mano.
/// Se esconde en menús, diálogos y cinemáticas.
/// </summary>
public class HUDJugador : MonoBehaviour
{
    Image fondo, barra, fondoC, barraC, topeC;
    Text textoVida, textoArma, textoC;
    PlayerMovement mov;
    float parpadeoC;
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

        // Condición física (aguante): se vacía al correr y al golpear; herido, el tope baja
        textoC = HorrorUI.CrearTexto(c.transform, "TextoCondicion", "CONDICIÓN", 16, HorrorUI.HuesoTenue, TextAnchor.MiddleLeft);
        Fijar(textoC.rectTransform, new Vector2(70, -224), new Vector2(110, 20));
        fondoC = HorrorUI.CrearImagen(c.transform, "CondicionFondo", new Color(0, 0, 0, 0.55f));
        Fijar(fondoC.rectTransform, new Vector2(160, -224), new Vector2(300, 10));
        topeC = HorrorUI.CrearImagen(fondoC.transform, "Tope", new Color(0.35f, 0.08f, 0.06f, 0.6f));
        RectTransform rtt = topeC.rectTransform;
        rtt.anchorMin = new Vector2(0, 0); rtt.anchorMax = new Vector2(1, 1); rtt.pivot = new Vector2(1, 0.5f);
        rtt.offsetMin = new Vector2(2, 2); rtt.offsetMax = new Vector2(-2, -2);
        barraC = HorrorUI.CrearImagen(fondoC.transform, "Condicion", new Color(0.55f, 0.75f, 0.55f));
        RectTransform rc = barraC.rectTransform;
        rc.anchorMin = new Vector2(0, 0); rc.anchorMax = new Vector2(1, 1); rc.pivot = new Vector2(0, 0.5f);
        rc.offsetMin = new Vector2(2, 2); rc.offsetMax = new Vector2(-2, -2);

        textoArma = HorrorUI.CrearTexto(c.transform, "Arma", "", 22, HorrorUI.HuesoTenue, TextAnchor.MiddleLeft);
        Fijar(textoArma.rectTransform, new Vector2(70, -252), new Vector2(500, 26));
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
        if (mov == null) mov = FindAnyObjectByType<PlayerMovement>();
        textoVida.enabled = fondo.enabled = barra.enabled = textoArma.enabled = ver;
        textoC.enabled = fondoC.enabled = barraC.enabled = topeC.enabled = ver && mov != null;
        if (ver && mov != null)
        {
            float a = mov.Aguante01;
            barraC.rectTransform.localScale = new Vector3(Mathf.Max(0.001f, a), 1, 1);
            topeC.rectTransform.localScale = new Vector3(Mathf.Max(0.001f, 1f - mov.Tope01), 1, 1);   // la parte que perdiste por estar herido
            Color cc = mov.Agotado ? new Color(0.8f, 0.35f, 0.2f) : Color.Lerp(new Color(0.85f, 0.6f, 0.25f), new Color(0.55f, 0.75f, 0.55f), a);
            if (mov.Agotado) { parpadeoC += Time.deltaTime * 8f; cc.a = 0.45f + 0.55f * Mathf.Abs(Mathf.Sin(parpadeoC)); }
            barraC.color = cc;
            textoC.text = mov.Agotado ? "SIN AIRE" : "CONDICIÓN";
        }
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
