using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lo que ves estando escondido: rendijas de un casillero/ropero u hojas de un arbusto alrededor de la vista,
/// con los bordes oscuros. Se crea al esconderse y se destruye al salir.
/// </summary>
public static class VistaEscondite
{
    static Texture2D rendijas, hojas;

    public static GameObject Crear(bool mueble)
    {
        GameObject g = new GameObject("VistaEscondite");
        Canvas c = g.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 40;
        RawImage img = new GameObject("Capa").AddComponent<RawImage>();
        img.transform.SetParent(g.transform, false);
        img.rectTransform.anchorMin = Vector2.zero; img.rectTransform.anchorMax = Vector2.one;
        img.rectTransform.offsetMin = img.rectTransform.offsetMax = Vector2.zero;
        img.raycastTarget = false;
        img.texture = mueble ? Rendijas() : Hojas();
        return g;
    }

    static Texture2D Rendijas()
    {
        if (rendijas != null) return rendijas;
        int w = 256, h = 144;
        rendijas = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float v = y / (float)h;
                // 4 rendijas horizontales en el centro de la puerta
                bool rendija = v > 0.30f && v < 0.70f && (Mathf.Repeat(v * 10f, 1f) > 0.45f) && x > w * 0.12f && x < w * 0.88f;
                float borde = rendija ? Mathf.Clamp01(1f - Mathf.Abs(x / (float)w - 0.5f) * 2.4f) : 0f;
                float a = rendija ? 1f - Mathf.SmoothStep(0f, 1f, borde) * 0.95f : 0.97f;
                rendijas.SetPixel(x, y, new Color(0.03f, 0.03f, 0.025f, a));
            }
        rendijas.Apply();
        return rendijas;
    }

    static Texture2D Hojas()
    {
        if (hojas != null) return hojas;
        int w = 256, h = 144;
        hojas = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        Random.State st = Random.state; Random.InitState(7);
        float[,] m = new float[w, h];
        for (int i = 0; i < 260; i++)
        {
            // hojas: elipses oscuras concentradas en los bordes
            float ang = Random.value * Mathf.PI * 2f;
            float r = Mathf.Lerp(0.55f, 1.15f, Mathf.Sqrt(Random.value));
            float cx = 0.5f + Mathf.Cos(ang) * r * 0.5f, cy = 0.5f + Mathf.Sin(ang) * r * 0.5f;
            float rx = Random.Range(0.03f, 0.08f), ry = rx * Random.Range(0.35f, 0.6f), rot = Random.value * Mathf.PI;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = x / (float)w - cx, dy = (y / (float)h - cy) * h / w;
                    float px = dx * Mathf.Cos(rot) + dy * Mathf.Sin(rot), py = -dx * Mathf.Sin(rot) + dy * Mathf.Cos(rot);
                    if ((px * px) / (rx * rx) + (py * py) / (ry * ry) < 1f) m[x, y] = 1f;
                }
        }
        Random.state = st;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float dx = x / (float)w - 0.5f, dy = y / (float)h - 0.5f;
                float vin = Mathf.SmoothStep(0.25f, 0.62f, Mathf.Sqrt(dx * dx + dy * dy));
                float a = Mathf.Max(m[x, y] * 0.93f, vin * 0.95f);
                hojas.SetPixel(x, y, new Color(0.02f, 0.05f, 0.02f, a));
            }
        hojas.Apply();
        return hojas;
    }
}
