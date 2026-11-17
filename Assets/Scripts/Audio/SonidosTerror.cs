using UnityEngine;

/// <summary>
/// Sonidos "psicológicos" generados por código. Técnicas reales del diseño sonoro de terror:
/// - Tono de Shepard: parece que sube para siempre (tensión que nunca se resuelve).
/// - Casi-infrasonido (18–30 Hz) con pulsos: incomodidad física, se siente más que se oye.
/// - Tritono y clusters disonantes (cuerdas / órgano): el oído espera que se resuelva y no pasa.
/// - Caja musical desafinada: lo infantil vuelto siniestro.
/// - Zumbido eléctrico de 60 Hz con armónicos: lugares "muertos" (túnel).
/// - Campanillas de viento lejanas y viento entre lápidas (panteón).
/// - Respiración y susurros paneados (binaurales) cuando algo está cerca.
/// Todo en mono 22 kHz y en bucle, para que el juego no pese.
/// </summary>
public static class SonidosTerror
{
    const int Hz = 22050;

    static AudioClip Crear(string n, float[] d)
    {
        float max = 0.0001f;
        foreach (float v in d) max = Mathf.Max(max, Mathf.Abs(v));
        for (int i = 0; i < d.Length; i++) d[i] = d[i] / max * 0.8f;
        // fundido en los bordes para que el bucle no truene
        int f = Hz / 20;
        for (int i = 0; i < f && i < d.Length; i++) { float k = (float)i / f; d[i] *= k; d[d.Length - 1 - i] *= k; }
        AudioClip c = AudioClip.Create(n, d.Length, 1, Hz, false);
        c.SetData(d, 0);
        return c;
    }

    /// <summary>Tono de Shepard ascendente (bucle de 12 s).</summary>
    public static AudioClip Shepard()
    {
        int n = Hz * 12; float[] d = new float[n];
        float[] fase = new float[7];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n, s = 0;
            for (int k = 0; k < 7; k++)
            {
                float oct = Mathf.Repeat(k + t, 7f);                 // cada voz sube una octava por bucle
                float f = 55f * Mathf.Pow(2f, oct);
                float amp = Mathf.Exp(-Mathf.Pow((oct - 3.5f) / 1.6f, 2f)); // campana: entra y sale sin notarse
                fase[k] += 2 * Mathf.PI * f / Hz;
                s += Mathf.Sin(fase[k]) * amp;
            }
            d[i] = s;
        }
        return Crear("Shepard", d);
    }

    /// <summary>Retumbo casi-infrasónico con pulsos irregulares (bucle de 10 s).</summary>
    public static AudioClip Infrasonido()
    {
        int n = Hz * 10; float[] d = new float[n];
        System.Random r = new System.Random(19);
        float ph = 0, ph2 = 0;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / Hz;
            ph += 2 * Mathf.PI * 19f / Hz; ph2 += 2 * Mathf.PI * 38.5f / Hz;
            float pulso = 0.6f + 0.4f * Mathf.Sin(t * 2 * Mathf.PI * 0.7f) * Mathf.Sin(t * 2 * Mathf.PI * 0.13f);
            d[i] = (Mathf.Sin(ph) * 0.8f + Mathf.Sin(ph2) * 0.25f + (float)(r.NextDouble() * 2 - 1) * 0.04f) * pulso;
        }
        return Crear("Infrasonido", d);
    }

    /// <summary>Cuerdas disonantes (cluster + tritono) que respiran (bucle de 14 s).</summary>
    public static AudioClip CuerdasDisonantes()
    {
        int n = Hz * 14; float[] d = new float[n];
        float[] f = { 110f, 116.5f, 155.6f, 220f, 233f, 311f };   // La, Si♭, Mi♭ (tritono), octavas
        float[] ph = new float[f.Length];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / Hz, s = 0;
            for (int k = 0; k < f.Length; k++)
            {
                float vib = 1f + 0.004f * Mathf.Sin(t * (4.5f + k * 0.3f));
                ph[k] += 2 * Mathf.PI * f[k] * vib / Hz;
                // diente de sierra suavizado (sonido de cuerda)
                float saw = 0; for (int h = 1; h <= 5; h++) saw += Mathf.Sin(ph[k] * h) / h;
                s += saw * (0.5f + 0.5f * Mathf.Sin(t * 0.45f + k));
            }
            d[i] = s;
        }
        return Crear("Cuerdas", d);
    }

    /// <summary>Caja musical desafinada tocando una canción de cuna rota (bucle de 9.6 s).</summary>
    public static AudioClip CajaMusical()
    {
        int n = (int)(Hz * 9.6f); float[] d = new float[n];
        int[] notas = { 76, 74, 72, 74, 76, 76, 76, -1, 74, 74, 74, -1, 76, 79, 79, -1 }; // melodía conocida... rota
        float paso = 0.6f;
        for (int k = 0; k < notas.Length; k++)
        {
            if (notas[k] < 0) continue;
            float f = 440f * Mathf.Pow(2f, (notas[k] - 69) / 12f) * (1f - 0.012f * (k % 3)); // desafinada
            int ini = (int)(k * paso * Hz);
            for (int i = 0; i < Hz * 1.5f && ini + i < n; i++)
            {
                float t = (float)i / Hz;
                d[ini + i] += Mathf.Sin(2 * Mathf.PI * f * t) * Mathf.Exp(-t * 3.5f) + 0.3f * Mathf.Sin(2 * Mathf.PI * f * 4.01f * t) * Mathf.Exp(-t * 9f);
            }
        }
        return Crear("CajaMusical", d);
    }

    /// <summary>Órgano de capilla, acorde disminuido que no se resuelve (bucle de 12 s).</summary>
    public static AudioClip Organo()
    {
        int n = Hz * 12; float[] d = new float[n];
        float[] f = { 73.4f, 87.3f, 103.8f, 146.8f, 174.6f, 207.6f }; // Re disminuido
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / Hz, s = 0;
            for (int k = 0; k < f.Length; k++)
                for (int h = 1; h <= 4; h++)
                    s += Mathf.Sin(2 * Mathf.PI * f[k] * h * t) / (h * h) * (0.7f + 0.3f * Mathf.Sin(t * 0.3f + k));
            d[i] = s * (0.8f + 0.2f * Mathf.Sin(t * 2 * Mathf.PI * 5.5f)); // trémolo del fuelle
        }
        return Crear("Organo", d);
    }

    /// <summary>Zumbido eléctrico de 60 Hz con chisporroteos (bucle de 8 s).</summary>
    public static AudioClip ZumbidoElectrico()
    {
        int n = Hz * 8; float[] d = new float[n];
        System.Random r = new System.Random(60);
        float chispa = 0;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / Hz;
            float s = Mathf.Sin(2 * Mathf.PI * 60 * t) * 0.6f + Mathf.Sin(2 * Mathf.PI * 120 * t) * 0.35f + Mathf.Sin(2 * Mathf.PI * 180 * t) * 0.2f;
            if (r.NextDouble() < 0.00015) chispa = 1f;
            chispa *= 0.9985f;
            s += (float)(r.NextDouble() * 2 - 1) * chispa * 1.5f;
            d[i] = s;
        }
        return Crear("Zumbido", d);
    }

    /// <summary>Campanillas de viento lejanas + viento entre lápidas (bucle de 12 s).</summary>
    public static AudioClip Campanillas()
    {
        int n = Hz * 12; float[] d = new float[n];
        System.Random r = new System.Random(7);
        float b = 0;
        for (int i = 0; i < n; i++) { b = b * 0.995f + (float)(r.NextDouble() * 2 - 1) * 0.02f; d[i] = b * (0.6f + 0.4f * Mathf.Sin(i / (float)Hz * 0.8f)); }
        float[] tonos = { 1046f, 1174f, 1396f, 1567f, 1760f };
        for (int k = 0; k < 9; k++)
        {
            int ini = r.Next(0, n - Hz * 2); float f = tonos[r.Next(tonos.Length)];
            for (int i = 0; i < Hz * 2; i++) { float t = (float)i / Hz; d[ini + i] += Mathf.Sin(2 * Mathf.PI * f * t) * Mathf.Exp(-t * 2.2f) * 0.25f; }
        }
        return Crear("Campanillas", d);
    }

    /// <summary>Fogata crepitando (bucle de 6 s).</summary>
    public static AudioClip Fogata()
    {
        int n = Hz * 6; float[] d = new float[n];
        System.Random r = new System.Random(3);
        float b = 0, crack = 0;
        for (int i = 0; i < n; i++)
        {
            b = b * 0.98f + (float)(r.NextDouble() * 2 - 1) * 0.05f;
            if (r.NextDouble() < 0.0008) crack = 1f;
            crack *= 0.97f;
            d[i] = b * 0.5f + (float)(r.NextDouble() * 2 - 1) * crack;
        }
        return Crear("Fogata", d);
    }

    /// <summary>Coro grave sin palabras (formantes "oo/aa"), para el mirador (bucle de 12 s).</summary>
    public static AudioClip Coro()
    {
        int n = Hz * 12; float[] d = new float[n];
        float[] f = { 98f, 103.8f, 146.8f, 196f };
        float[] ph = new float[f.Length];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / Hz, s = 0;
            for (int k = 0; k < f.Length; k++)
            {
                ph[k] += 2 * Mathf.PI * f[k] * (1 + 0.006f * Mathf.Sin(t * 5f + k)) / Hz;
                float v = 0;
                for (int h = 1; h <= 12; h++)
                {
                    float fh = f[k] * h;
                    float formante = Mathf.Exp(-Mathf.Pow((fh - 450f) / 180f, 2)) + 0.6f * Mathf.Exp(-Mathf.Pow((fh - 800f) / 250f, 2));
                    v += Mathf.Sin(ph[k] * h) * formante;
                }
                s += v * (0.6f + 0.4f * Mathf.Sin(t * 0.25f + k * 1.3f));
            }
            d[i] = s;
        }
        return Crear("Coro", d);
    }

    /// <summary>Una voz lejana que dice "¿hola?" (sin palabras reales: formantes), para engañar al oído.</summary>
    public static AudioClip Llamado(int semilla)
    {
        int n = (int)(Hz * 1.2f); float[] d = new float[n];
        System.Random r = new System.Random(semilla);
        float f0 = 150f + (float)r.NextDouble() * 60f, ph = 0;
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / Hz;
            float silaba = t < 0.35f ? 1f : (t > 0.45f && t < 0.95f ? 1f : 0f);
            float f = f0 * (t < 0.4f ? 1f : 1.25f - (t - 0.45f) * 0.3f);   // "¿ho-LA?" sube
            ph += 2 * Mathf.PI * f / Hz;
            float v = 0;
            for (int h = 1; h <= 10; h++)
            {
                float fh = f * h, F1 = t < 0.4f ? 500f : 750f, F2 = t < 0.4f ? 900f : 1250f;
                v += Mathf.Sin(ph * h) * (Mathf.Exp(-Mathf.Pow((fh - F1) / 120f, 2)) + 0.7f * Mathf.Exp(-Mathf.Pow((fh - F2) / 160f, 2)));
            }
            d[i] = v * silaba * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 1.0f));
        }
        return Crear("Llamado", d);
    }
}
