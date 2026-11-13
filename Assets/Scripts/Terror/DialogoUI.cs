using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Diálogos del Vigía: caja de subtítulos con letras que aparecen una por una,
/// voz distorsionada, letras que se corrompen, barras de cine y latido de fondo.
/// Uso: StartCoroutine(DialogoUI.Instancia.Conversar(guia, lineas));
/// Cada línea puede empezar con una pose: "Reir|Qué valiente."
/// </summary>
public class DialogoUI : MonoBehaviour
{
    public static DialogoUI Instancia { get; private set; }
    public static bool Activo { get; private set; }

    [Header("Ritmo")]
    public float letrasPorSegundo = 32f;
    [Range(0, 0.1f)] public float corrupcion = 0.025f;

    const string Simbolos = "#%&@?*§¤∆◊¥ΞΨ";

    GameObject caja, toast;
    Text nombre, texto, flecha, susurro, toastTitulo, toastTexto;
    RectTransform barraArriba, barraAbajo;
    AudioSource srcVoz, srcLatido;
    AudioClip[] voces;
    float finSusurro, finToast, finCorrupcion;
    string lineaActual = "";

    void Awake()
    {
        Instancia = this;
        Construir();
        srcVoz = gameObject.AddComponent<AudioSource>();
        srcVoz.playOnAwake = false;
        srcLatido = gameObject.AddComponent<AudioSource>();
        srcLatido.clip = ProceduralAudio.Latido();
        srcLatido.loop = true;
        srcLatido.volume = 0f;
        srcLatido.Play();
        voces = new AudioClip[6];
        for (int i = 0; i < voces.Length; i++) voces[i] = ProceduralAudio.VozBlip(40 + i);
    }

    void OnDestroy()
    {
        Activo = false;
        if (Instancia == this) Instancia = null;
    }

    void Construir()
    {
        Canvas c = HorrorUI.CrearCanvas("Dialogo_Canvas", 350, transform);
        Transform r = c.transform;

        // Barras de cine
        barraArriba = HorrorUI.CrearImagen(r, "BarraArriba", Color.black).rectTransform;
        barraArriba.anchorMin = new Vector2(0, 1); barraArriba.anchorMax = new Vector2(1, 1);
        barraArriba.pivot = new Vector2(0.5f, 1); barraArriba.sizeDelta = new Vector2(0, 0);
        barraAbajo = HorrorUI.CrearImagen(r, "BarraAbajo", Color.black).rectTransform;
        barraAbajo.anchorMin = new Vector2(0, 0); barraAbajo.anchorMax = new Vector2(1, 0);
        barraAbajo.pivot = new Vector2(0.5f, 0); barraAbajo.sizeDelta = new Vector2(0, 0);

        // Caja de diálogo
        Image fondo = HorrorUI.CrearImagen(r, "Caja", new Color(0, 0, 0, 0.78f));
        caja = fondo.gameObject;
        HorrorUI.Colocar(fondo.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 165), new Vector2(1500, 220));
        Image linea = HorrorUI.CrearImagen(caja.transform, "Linea", HorrorUI.Sangre);
        linea.rectTransform.anchorMin = new Vector2(0, 1); linea.rectTransform.anchorMax = new Vector2(1, 1);
        linea.rectTransform.pivot = new Vector2(0.5f, 1); linea.rectTransform.sizeDelta = new Vector2(0, 3);

        nombre = HorrorUI.CrearTexto(caja.transform, "Nombre", "", 26, HorrorUI.Sangre, TextAnchor.UpperLeft, FontStyle.Bold);
        HorrorUI.Colocar(nombre.rectTransform, new Vector2(0, 1), new Vector2(40, -18), new Vector2(800, 36));

        texto = HorrorUI.CrearTexto(caja.transform, "Texto", "", 36, HorrorUI.Hueso, TextAnchor.UpperLeft);
        texto.horizontalOverflow = HorizontalWrapMode.Wrap;
        texto.lineSpacing = 1.1f;
        HorrorUI.Colocar(texto.rectTransform, new Vector2(0, 1), new Vector2(40, -62), new Vector2(1420, 150));

        flecha = HorrorUI.CrearTexto(caja.transform, "Flecha", "▼  [A]", 22, HorrorUI.HuesoTenue, TextAnchor.LowerRight);
        HorrorUI.Colocar(flecha.rectTransform, new Vector2(1, 0), new Vector2(-30, 16), new Vector2(200, 30));
        caja.SetActive(false);

        // Susurros (subtítulo suelto, no bloquea)
        susurro = HorrorUI.CrearTexto(r, "Susurro", "", 30, HorrorUI.Hueso, TextAnchor.MiddleCenter, FontStyle.Italic);
        HorrorUI.Colocar(susurro.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 250), new Vector2(1600, 60));

        // Aviso de objetivo
        toast = new GameObject("Objetivo", typeof(RectTransform));
        toast.transform.SetParent(r, false);
        HorrorUI.Colocar(HorrorUI.Rect(toast), new Vector2(0.5f, 1), new Vector2(0, -120), new Vector2(1000, 110));
        toastTitulo = HorrorUI.CrearTexto(toast.transform, "Titulo", "O B J E T I V O   A C T U A L I Z A D O", 22, HorrorUI.Sangre, TextAnchor.UpperCenter);
        HorrorUI.Estirar(toastTitulo.rectTransform);
        toastTexto = HorrorUI.CrearTexto(toast.transform, "Texto", "", 34, HorrorUI.Hueso, TextAnchor.LowerCenter);
        HorrorUI.Estirar(toastTexto.rectTransform);
        toast.SetActive(false);
    }

    void Update()
    {
        // Barras de cine entran y salen
        float objetivo = Activo ? 130f : 0f;
        float h = Mathf.Lerp(barraArriba.sizeDelta.y, objetivo, Time.unscaledDeltaTime * 4f);
        barraArriba.sizeDelta = new Vector2(0, h);
        barraAbajo.sizeDelta = new Vector2(0, h);

        srcLatido.volume = Mathf.MoveTowards(srcLatido.volume, Activo ? 0.6f : 0f, Time.unscaledDeltaTime * 0.5f);

        if (Time.unscaledTime > finSusurro) susurro.color = Color.Lerp(susurro.color, new Color(1, 1, 1, 0), Time.unscaledDeltaTime * 3f);
        if (toast.activeSelf)
        {
            float a = Mathf.Clamp01(finToast - Time.unscaledTime);
            toastTitulo.color = new Color(HorrorUI.Sangre.r, HorrorUI.Sangre.g, HorrorUI.Sangre.b, a);
            toastTexto.color = new Color(HorrorUI.Hueso.r, HorrorUI.Hueso.g, HorrorUI.Hueso.b, a);
            if (a <= 0) toast.SetActive(false);
        }
        // La línea ya escrita se corrompe de vez en cuando
        if (Activo && caja.activeSelf && lineaActual.Length > 0)
        {
            if (Time.unscaledTime > finCorrupcion && texto.text != lineaActual && texto.text.Length == lineaActual.Length) texto.text = lineaActual;
            else if (texto.text == lineaActual && UnityEngine.Random.value < 0.006f)
            {
                texto.text = Corromper(lineaActual, corrupcion * 3f);
                finCorrupcion = Time.unscaledTime + 0.08f;
            }
        }
        if (Activo) flecha.color = new Color(1, 1, 1, 0.3f + 0.5f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f)));
    }

    // ------------------------------------------------------------------ API

    /// <summary>Conversación que bloquea al jugador. Espera a que termine con yield return.</summary>
    public IEnumerator Conversar(GuiaController guia, string[] lineas, Action alTerminar = null)
    {
        Activo = true;
        HorrorGameState.modo = HorrorGameState.Modo.Dialogo;
        CameraController cam = FindFirstObjectByType<CameraController>();
        if (cam != null && guia != null) cam.Enfocar(guia.transform);

        caja.SetActive(true);
        nombre.text = Espaciar(guia != null ? guia.nombre : "???");
        yield return null;

        foreach (string bruta in lineas)
        {
            string linea = bruta;
            string pose = "Hablar";
            int sep = bruta.IndexOf('|');
            if (sep > 0) { pose = bruta.Substring(0, sep); linea = bruta.Substring(sep + 1); }
            if (guia != null) guia.AnimarPorNombre(pose);
            if (pose == "Intimidar") { VHSOverlay.Glitch(0.8f, 0.4f); HorrorInput.Vibrar(0.6f, 0.4f, 0.3f, this); }

            lineaActual = "";
            yield return Escribir(linea);

            // Esperar A (o Enter) para continuar
            while (!(HorrorInput.Aceptar || HorrorInput.RB)) yield return null;
            yield return null;
        }

        caja.SetActive(false);
        if (cam != null) cam.Enfocar(null);
        if (guia != null) guia.AnimarPorNombre("Idle");
        Activo = false;
        HorrorGameState.modo = HorrorGameState.Modo.Jugando;
        alTerminar?.Invoke();
    }

    IEnumerator Escribir(string linea)
    {
        float t = 0;
        int mostradas = 0, ultimaVoz = -1;
        while (mostradas < linea.Length)
        {
            if (HorrorInput.Aceptar || HorrorInput.RB) { mostradas = linea.Length; break; }
            t += Time.unscaledDeltaTime * letrasPorSegundo;
            // Pausas dramáticas en puntos y comas
            int nuevas = Mathf.Min(linea.Length, Mathf.FloorToInt(t));
            mostradas = nuevas;
            if (mostradas > 0)
            {
                char ult = linea[mostradas - 1];
                if (ult == '.' || ult == '?' || ult == '!') t -= Time.unscaledDeltaTime * letrasPorSegundo * 0.85f;
            }
            if (mostradas / 2 != ultimaVoz && mostradas < linea.Length && linea[Mathf.Max(0, mostradas - 1)] != ' ')
            {
                ultimaVoz = mostradas / 2;
                srcVoz.pitch = UnityEngine.Random.Range(0.75f, 1.05f);
                srcVoz.PlayOneShot(voces[UnityEngine.Random.Range(0, voces.Length)], 0.5f);
            }
            texto.text = Corromper(linea.Substring(0, mostradas));
            yield return null;
        }
        texto.text = linea;
        lineaActual = linea;
        yield return null;
    }

    string Corromper(string s, float prob = -1f)
    {
        if (prob < 0) prob = corrupcion;
        char[] cs = s.ToCharArray();
        for (int i = 0; i < cs.Length; i++)
            if (cs[i] != ' ' && UnityEngine.Random.value < prob) cs[i] = Simbolos[UnityEngine.Random.Range(0, Simbolos.Length)];
        return new string(cs);
    }

    /// <summary>Frase suelta que no detiene el juego (comentarios del Vigía).</summary>
    public static void Susurrar(string quien, string frase, float segundos = 3.5f)
    {
        if (Instancia == null) return;
        Instancia.susurro.text = (string.IsNullOrEmpty(quien) ? "" : quien + ":  ") + "“" + frase + "”";
        Instancia.susurro.color = HorrorUI.Hueso;
        Instancia.finSusurro = Time.unscaledTime + segundos;
        Instancia.srcVoz.pitch = 0.7f;
        Instancia.srcVoz.PlayOneShot(Instancia.voces[0], 0.4f);
    }

    /// <summary>Aviso de "OBJETIVO ACTUALIZADO" arriba de la pantalla.</summary>
    public static void NuevoObjetivo(string texto)
    {
        InventoryUI.Objetivo = texto;
        if (Instancia == null) return;
        Instancia.toastTexto.text = texto;
        Instancia.toast.SetActive(true);
        Instancia.finToast = Time.unscaledTime + 5f;
        VHSOverlay.Glitch(0.3f, 0.2f);
    }

    static string Espaciar(string s)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (char ch in s) { sb.Append(ch); sb.Append(' '); }
        return sb.ToString().TrimEnd();
    }
}
