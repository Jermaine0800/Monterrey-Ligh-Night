using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Utilidades para construir la interfaz de terror analógico por código
/// (sin prefabs ni imágenes: no agrega peso al juego).
/// </summary>
public static class HorrorUI
{
    // Paleta estilo Silent Hill / VHS
    public static readonly Color Hueso      = new Color(0.82f, 0.79f, 0.72f, 1f);
    public static readonly Color HuesoTenue = new Color(0.82f, 0.79f, 0.72f, 0.35f);
    public static readonly Color Sangre     = new Color(0.62f, 0.06f, 0.05f, 1f);
    public static readonly Color Negro      = new Color(0f, 0f, 0f, 1f);
    public static readonly Color VerdeMonitor = new Color(0.35f, 0.95f, 0.45f, 1f);
    public static readonly Color Metal      = new Color(0.55f, 0.55f, 0.57f, 1f);

    static Font fuente;
    public static Font Fuente
    {
        get
        {
            if (fuente == null) fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return fuente;
        }
    }

    // ---------- Canvas ----------
    public static Canvas CrearCanvas(string nombre, int orden, Transform padre = null)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        if (padre != null) go.transform.SetParent(padre, false);
        Canvas c = go.GetComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.overrideSorting = true;
        c.sortingOrder = orden;
        CanvasScaler s = go.GetComponent<CanvasScaler>();
        s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        s.referenceResolution = new Vector2(1920, 1080);
        s.matchWidthOrHeight = 0.5f;
        return c;
    }

    // ---------- Rect helpers ----------
    public static RectTransform Rect(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        return rt;
    }

    public static void Estirar(RectTransform rt, float margen = 0f)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(margen, margen);
        rt.offsetMax = new Vector2(-margen, -margen);
    }

    /// <summary>Coloca un rect con ancla/pivote en un punto (0..1) de la pantalla.</summary>
    public static void Colocar(RectTransform rt, Vector2 ancla, Vector2 posicion, Vector2 tamano)
    {
        rt.anchorMin = ancla;
        rt.anchorMax = ancla;
        rt.pivot = ancla;
        rt.anchoredPosition = posicion;
        rt.sizeDelta = tamano;
    }

    // ---------- Elementos ----------
    public static Image CrearImagen(Transform padre, string nombre, Color color)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(padre, false);
        Image img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    public static RawImage CrearRaw(Transform padre, string nombre, Texture tex, Color color)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(RawImage));
        go.transform.SetParent(padre, false);
        RawImage img = go.GetComponent<RawImage>();
        img.texture = tex;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    public static Text CrearTexto(Transform padre, string nombre, string contenido, int tamano, Color color,
                                  TextAnchor alineacion = TextAnchor.MiddleLeft, FontStyle estilo = FontStyle.Normal)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(padre, false);
        Text t = go.GetComponent<Text>();
        t.font = Fuente;
        t.text = contenido;
        t.fontSize = tamano;
        t.color = color;
        t.alignment = alineacion;
        t.fontStyle = estilo;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        return t;
    }

    /// <summary>Marco de 4 líneas (borde) alrededor de un rect.</summary>
    public static GameObject CrearMarco(Transform padre, string nombre, Color color, float grosor)
    {
        GameObject marco = new GameObject(nombre, typeof(RectTransform));
        marco.transform.SetParent(padre, false);
        Estirar(Rect(marco));

        string[] lados = { "Arriba", "Abajo", "Izq", "Der" };
        for (int i = 0; i < 4; i++)
        {
            Image l = CrearImagen(marco.transform, lados[i], color);
            RectTransform r = l.rectTransform;
            switch (i)
            {
                case 0: r.anchorMin = new Vector2(0, 1); r.anchorMax = new Vector2(1, 1); r.pivot = new Vector2(0.5f, 1); r.sizeDelta = new Vector2(0, grosor); break;
                case 1: r.anchorMin = new Vector2(0, 0); r.anchorMax = new Vector2(1, 0); r.pivot = new Vector2(0.5f, 0); r.sizeDelta = new Vector2(0, grosor); break;
                case 2: r.anchorMin = new Vector2(0, 0); r.anchorMax = new Vector2(0, 1); r.pivot = new Vector2(0, 0.5f); r.sizeDelta = new Vector2(grosor, 0); break;
                case 3: r.anchorMin = new Vector2(1, 0); r.anchorMax = new Vector2(1, 1); r.pivot = new Vector2(1, 0.5f); r.sizeDelta = new Vector2(grosor, 0); break;
            }
            r.anchoredPosition = Vector2.zero;
        }
        return marco;
    }

    // ---------- Texturas procedurales ----------
    static Texture2D texScanlines, texRuido, texVineta;

    public static Texture2D Scanlines
    {
        get
        {
            if (texScanlines != null) return texScanlines;
            texScanlines = new Texture2D(1, 4, TextureFormat.RGBA32, false);
            texScanlines.wrapMode = TextureWrapMode.Repeat;
            texScanlines.filterMode = FilterMode.Point;
            texScanlines.SetPixel(0, 0, new Color(0, 0, 0, 0.55f));
            texScanlines.SetPixel(0, 1, new Color(0, 0, 0, 0.0f));
            texScanlines.SetPixel(0, 2, new Color(0, 0, 0, 0.0f));
            texScanlines.SetPixel(0, 3, new Color(0, 0, 0, 0.25f));
            texScanlines.Apply();
            return texScanlines;
        }
    }

    public static Texture2D Ruido
    {
        get
        {
            if (texRuido != null) return texRuido;
            const int n = 256;
            texRuido = new Texture2D(n, n, TextureFormat.RGBA32, false);
            texRuido.wrapMode = TextureWrapMode.Repeat;
            texRuido.filterMode = FilterMode.Point;
            Color[] px = new Color[n * n];
            System.Random r = new System.Random(1337);
            for (int i = 0; i < px.Length; i++)
            {
                float v = (float)r.NextDouble();
                px[i] = new Color(v, v, v, 1f);
            }
            texRuido.SetPixels(px);
            texRuido.Apply();
            return texRuido;
        }
    }

    public static Texture2D Vineta
    {
        get
        {
            if (texVineta != null) return texVineta;
            const int n = 128;
            texVineta = new Texture2D(n, n, TextureFormat.RGBA32, false);
            texVineta.wrapMode = TextureWrapMode.Clamp;
            Color[] px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x / (float)(n - 1)) * 2f - 1f;
                    float dy = (y / (float)(n - 1)) * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx * 0.8f + dy * dy);
                    float k = Mathf.Clamp01((d - 0.6f) / 0.75f);
                    float a = k * k * (3f - 2f * k) * 0.85f;
                    px[y * n + x] = new Color(0, 0, 0, a);
                }
            texVineta.SetPixels(px);
            texVineta.Apply();
            return texVineta;
        }
    }
}
