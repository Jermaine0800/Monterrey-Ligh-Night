using UnityEngine;

/// <summary>
/// Monitor CRT de la caseta: una cinta de los 80 en bucle, dibujada en tiempo real.
/// Trabajadores corriendo hacia la cámara y, al fondo, una silueta alta con la cabeza radial
/// que se acerca un poco más en cada vuelta.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class MonitorCRT : MonoBehaviour
{
    public int ancho = 96, alto = 72;
    public float duracionBucle = 7f;
    public float cuadrosPorSegundo = 12f;

    Texture2D tex;
    Color32[] px;
    float siguiente;
    int vuelta;
    float ultimoT;

    void Start()
    {
        tex = new Texture2D(ancho, alto, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        px = new Color32[ancho * alto];
        Renderer r = GetComponent<Renderer>();
        r.material.SetTexture("_BaseMap", tex);
        r.material.SetColor("_BaseColor", new Color(0.8f, 0.95f, 0.85f));
    }

    void Update()
    {
        if (Time.time < siguiente) return;
        siguiente = Time.time + 1f / cuadrosPorSegundo;
        float t = Mathf.Repeat(Time.time, duracionBucle) / duracionBucle;
        if (t < ultimoT) vuelta++;
        ultimoT = t;
        Dibujar(t);
        tex.SetPixels32(px);
        tex.Apply(false);
    }

    void Dibujar(float t)
    {
        // Fondo: túnel gris con ruido
        for (int y = 0; y < alto; y++)
            for (int x = 0; x < ancho; x++)
            {
                byte n = (byte)Random.Range(10, 45);
                byte g = (byte)(y < alto / 3 ? n + 15 : n);
                px[y * ancho + x] = new Color32(g, (byte)(g + 4), g, 255);
            }
        // Vías en perspectiva
        for (int y = 0; y < alto / 2; y++)
        {
            int c = ancho / 2, d = (alto / 2 - y) / 2 + 2;
            Pon(c - d, y, 70); Pon(c + d, y, 70);
        }

        // La silueta radial al fondo (más grande cada vuelta)
        float escala = Mathf.Clamp(0.6f + vuelta * 0.12f, 0.6f, 1.6f);
        int sx = ancho / 2, sy = (int)(alto * 0.42f);
        int h = (int)(18 * escala);
        for (int y = 0; y < h; y++) for (int x = -1; x <= 1; x++) Pon(sx + x, sy + y, 0);
        int cabeza = sy + h;
        for (int k = 0; k < 10; k++)
        {
            float ang = k * Mathf.PI / 5f;
            for (int r = 2; r < (int)(6 * escala); r++)
                Pon(sx + (int)(Mathf.Cos(ang) * r), cabeza + (int)(Mathf.Sin(ang) * r), 0);
        }

        // Trabajadores corriendo hacia la cámara
        for (int w = 0; w < 3; w++)
        {
            float tw = Mathf.Repeat(t + w * 0.27f, 1f);
            int wx = ancho / 2 + (int)((w - 1) * (8 + tw * 30));
            int wy = (int)Mathf.Lerp(alto * 0.4f, -10, tw);
            int wh = (int)Mathf.Lerp(6, 34, tw);
            for (int y = 0; y < wh; y++)
                for (int x = -wh / 8 - 1; x <= wh / 8 + 1; x++)
                    Pon(wx + x + ((y / 3 + (int)(Time.time * 12)) % 2), wy + y, 200);
        }

        // Scanline que baja y glitch ocasional
        int sl = (int)(Mathf.Repeat(Time.time * 20f, alto));
        for (int x = 0; x < ancho; x++) Pon(x, sl, 160);
        if (Random.value < 0.08f)
        {
            int gy = Random.Range(0, alto - 4);
            for (int y = gy; y < gy + 3; y++) for (int x = 0; x < ancho; x++) Pon(x, y, (byte)Random.Range(80, 255));
        }
    }

    void Pon(int x, int y, byte v)
    {
        if (x < 0 || y < 0 || x >= ancho || y >= alto) return;
        px[y * ancho + x] = new Color32(v, (byte)Mathf.Min(255, v + 10), v, 255);
    }
}
