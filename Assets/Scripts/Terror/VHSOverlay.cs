using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Capa de cinta VHS sobre la pantalla: scanlines, ruido, vineta, REC, fecha/hora,
/// batería de la linterna y glitches. Llama VHSOverlay.Glitch(...) desde cualquier script
/// (por ejemplo cuando el monstruo se acerca).
/// </summary>
public class VHSOverlay : MonoBehaviour
{
    public static VHSOverlay Instancia { get; private set; }

    [Header("Modo")]
    public bool modoMenu = false;               // en el menú muestra PLAY en vez de REC

    [Header("Cinta")]
    public string fechaInicial = "2003-11-11 23:47:00";
    public string etiquetaCinta = "CINTA 07 // CERRO DE LA SILLA";

    [Header("Intensidad")]
    [Range(0, 1)] public float grano = 0.03f;
    [Range(0, 1)] public float scanlinesAlpha = 0.55f;

    RawImage ruido, scanlines, vineta, bandaTracking;
    Text rec, hora, etiqueta, bateria, modoTexto;
    Image puntoRec;
    DateTime reloj;
    float glitch, glitchFin;
    FlashlightController linterna;

    void Awake()
    {
        Instancia = this;
        if (!DateTime.TryParse(fechaInicial, out reloj)) reloj = new DateTime(2003, 11, 11, 23, 47, 0);
        Construir();
    }

    void Construir()
    {
        Canvas c = HorrorUI.CrearCanvas("VHS_Canvas", 500, transform);

        vineta = HorrorUI.CrearRaw(c.transform, "Vineta", HorrorUI.Vineta, Color.white);
        HorrorUI.Estirar(vineta.rectTransform);

        ruido = HorrorUI.CrearRaw(c.transform, "Ruido", HorrorUI.Ruido, new Color(1, 1, 1, grano));
        HorrorUI.Estirar(ruido.rectTransform);

        scanlines = HorrorUI.CrearRaw(c.transform, "Scanlines", HorrorUI.Scanlines, new Color(1, 1, 1, scanlinesAlpha));
        HorrorUI.Estirar(scanlines.rectTransform);
        scanlines.uvRect = new Rect(0, 0, 1, 1080f / 4f / 1.5f);

        bandaTracking = HorrorUI.CrearRaw(c.transform, "Tracking", HorrorUI.Ruido, new Color(1, 1, 1, 0f));
        RectTransform rb = bandaTracking.rectTransform;
        rb.anchorMin = new Vector2(0, 0); rb.anchorMax = new Vector2(1, 0);
        rb.pivot = new Vector2(0.5f, 0.5f); rb.sizeDelta = new Vector2(0, 26);

        // Esquina superior izquierda: REC / PLAY
        puntoRec = HorrorUI.CrearImagen(c.transform, "PuntoRec", HorrorUI.Sangre);
        HorrorUI.Colocar(puntoRec.rectTransform, new Vector2(0, 1), new Vector2(70, -62), new Vector2(26, 26));
        rec = HorrorUI.CrearTexto(c.transform, "REC", modoMenu ? "PLAY ▶" : "REC", 44, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
        HorrorUI.Colocar(rec.rectTransform, new Vector2(0, 1), new Vector2(modoMenu ? 70 : 110, -50), new Vector2(400, 50));
        puntoRec.gameObject.SetActive(!modoMenu);

        modoTexto = HorrorUI.CrearTexto(c.transform, "SP", "SP", 34, Color.white, TextAnchor.MiddleRight, FontStyle.Bold);
        HorrorUI.Colocar(modoTexto.rectTransform, new Vector2(1, 1), new Vector2(-70, -50), new Vector2(200, 50));

        // Esquina inferior izquierda: fecha y hora
        hora = HorrorUI.CrearTexto(c.transform, "Hora", "", 40, Color.white, TextAnchor.LowerLeft, FontStyle.Bold);
        HorrorUI.Colocar(hora.rectTransform, new Vector2(0, 0), new Vector2(70, 50), new Vector2(700, 100));

        etiqueta = HorrorUI.CrearTexto(c.transform, "Etiqueta", etiquetaCinta, 24, new Color(1, 1, 1, 0.7f), TextAnchor.LowerRight);
        HorrorUI.Colocar(etiqueta.rectTransform, new Vector2(1, 0), new Vector2(-70, 50), new Vector2(700, 40));

        // Batería (solo en juego)
        bateria = HorrorUI.CrearTexto(c.transform, "Bateria", "", 30, Color.white, TextAnchor.UpperRight, FontStyle.Bold);
        HorrorUI.Colocar(bateria.rectTransform, new Vector2(1, 1), new Vector2(-70, -100), new Vector2(500, 40));
        bateria.gameObject.SetActive(!modoMenu);
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        reloj = reloj.AddSeconds(dt);

        // Ruido animado: mover las UV al azar cada frame
        ruido.uvRect = new Rect(UnityEngine.Random.value, UnityEngine.Random.value, 6f, 3.4f);
        float g = Time.unscaledTime < glitchFin ? glitch : 0f;
        ruido.color = new Color(1, 1, 1, Mathf.Clamp01(grano + g * 0.5f));

        // REC parpadeando
        bool encendido = (Time.unscaledTime % 1.2f) < 0.75f;
        if (!modoMenu) puntoRec.enabled = encendido;
        else rec.color = new Color(1, 1, 1, 0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * 3f));

        hora.text = reloj.ToString("tt", System.Globalization.CultureInfo.InvariantCulture) + "  " +
                    reloj.ToString("hh:mm:ss") + "\n" +
                    reloj.ToString("MMM. dd yyyy", System.Globalization.CultureInfo.InvariantCulture).ToUpperInvariant();

        // Banda de tracking que sube por la pantalla de vez en cuando
        float ciclo = (Time.unscaledTime * 0.07f) % 1f;
        RectTransform rb = bandaTracking.rectTransform;
        rb.anchoredPosition = new Vector2(UnityEngine.Random.Range(-6f, 6f) * (1 + g * 10), ciclo * 1200f - 60f);
        bandaTracking.uvRect = new Rect(UnityEngine.Random.value, UnityEngine.Random.value, 4f, 0.1f);
        bandaTracking.color = new Color(1, 1, 1, 0.08f + g * 0.4f);
        rb.sizeDelta = new Vector2(0, 26 + g * 220f);

        // Desplazamiento horizontal de los textos durante un glitch
        if (g > 0.01f)
        {
            float off = UnityEngine.Random.Range(-14f, 14f) * g;
            hora.rectTransform.anchoredPosition = new Vector2(70 + off, 50);
            rec.rectTransform.anchoredPosition = new Vector2((modoMenu ? 70 : 110) - off, -50);
        }

        // Batería
        if (!modoMenu)
        {
            if (linterna == null) linterna = FindFirstObjectByType<FlashlightController>();
            if (linterna != null && linterna.TieneLinterna)
            {
                int barras = Mathf.CeilToInt(linterna.Bateria01 * 4f);
                string s = "BAT [";
                for (int i = 0; i < 4; i++) s += i < barras ? "█" : " ";
                s += "]";
                bateria.text = s;
                bateria.color = linterna.Bateria01 < 0.2f && encendido ? HorrorUI.Sangre : Color.white;
            }
            else bateria.text = "";
        }
    }

    /// <summary>Interferencia en la cinta. intensidad 0..1</summary>
    public static void Glitch(float intensidad, float segundos)
    {
        if (Instancia == null) return;
        Instancia.glitch = Mathf.Max(Instancia.glitch * (Time.unscaledTime < Instancia.glitchFin ? 1 : 0), intensidad);
        Instancia.glitchFin = Time.unscaledTime + segundos;
    }
}
