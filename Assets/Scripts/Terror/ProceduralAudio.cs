using UnityEngine;

/// <summary>
/// Genera sonidos de terror por código (viento, drone, estática, pasos, golpes).
/// No usa archivos de audio: el juego no pesa ni un MB más.
/// </summary>
public static class ProceduralAudio
{
    const int Hz = 22050;

    static AudioClip Crear(string nombre, float[] datos)
    {
        AudioClip c = AudioClip.Create(nombre, datos.Length, 1, Hz, false);
        c.SetData(datos, 0);
        return c;
    }

    static float[] Normalizar(float[] d, float pico)
    {
        float max = 0.0001f;
        for (int i = 0; i < d.Length; i++) max = Mathf.Max(max, Mathf.Abs(d[i]));
        float k = pico / max;
        for (int i = 0; i < d.Length; i++) d[i] *= k;
        return d;
    }

    // ---------------- Ambientes (loops) ----------------

    public static AudioClip Viento(int semilla = 1)
    {
        System.Random r = new System.Random(semilla);
        int n = Hz * 12;
        float[] d = new float[n];
        float lp1 = 0, lp2 = 0;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Hz;
            float blanco = (float)(r.NextDouble() * 2 - 1);
            // filtro pasa-bajas variable = ráfagas
            float rafaga = 0.5f + 0.5f * Mathf.Sin(t * 0.55f) * Mathf.Sin(t * 0.23f + 1.3f);
            float corte = 0.008f + 0.03f * rafaga;
            lp1 += (blanco - lp1) * corte;
            lp2 += (lp1 - lp2) * corte;
            d[i] = lp2 * (0.35f + 0.65f * rafaga);
        }
        Normalizar(d, 0.8f);
        float[] loop = (float[])d.Clone();
        int f = (int)(0.8f * Hz);
        for (int i = 0; i < f; i++) { float k = i / (float)f; loop[i] = d[i] * k + d[n - f + i] * (1 - k); }
        System.Array.Resize(ref loop, n - f);
        return Crear("Viento", loop);
    }

    public static AudioClip Drone(int semilla = 2)
    {
        System.Random r = new System.Random(semilla);
        int n = Hz * 16;
        float[] d = new float[n];
        float lp = 0;
        float[] freqs = { 41.2f, 41.7f, 61.8f, 82.4f, 87.3f }; // disonante a propósito
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Hz;
            float s = 0;
            for (int k = 0; k < freqs.Length; k++)
                s += Mathf.Sin(2 * Mathf.PI * freqs[k] * t + k) * (1f / (k + 1.5f));
            float ruido = (float)(r.NextDouble() * 2 - 1);
            lp += (ruido - lp) * 0.02f;
            float respiracion = 0.6f + 0.4f * Mathf.Sin(2 * Mathf.PI * t / 8f);
            d[i] = (s * 0.7f + lp * 1.5f) * respiracion;
        }
        Normalizar(d, 0.7f);
        float[] loop = (float[])d.Clone();
        int f = Hz;
        for (int i = 0; i < f; i++) { float k = i / (float)f; loop[i] = d[i] * k + d[n - f + i] * (1 - k); }
        System.Array.Resize(ref loop, n - f);
        return Crear("Drone", loop);
    }

    /// <summary>Estática de TV/VHS (loop corto).</summary>
    public static AudioClip Estatica(int semilla = 3)
    {
        System.Random r = new System.Random(semilla);
        int n = Hz * 3;
        float[] d = new float[n];
        float hp = 0, prev = 0;
        for (int i = 0; i < n; i++)
        {
            float b = (float)(r.NextDouble() * 2 - 1);
            hp = 0.9f * (hp + b - prev); prev = b; // pasa-altas: siseo
            float zumbido = Mathf.Sin(2 * Mathf.PI * 60f * i / Hz) * 0.15f; // 60 Hz eléctrico
            d[i] = hp * 0.6f + zumbido;
        }
        Normalizar(d, 0.6f);
        return Crear("Estatica", d);
    }

    // ---------------- Efectos (one-shot) ----------------

    /// <summary>Clic seco de menú (como botón de videocasetera).</summary>
    public static AudioClip ClicMenu()
    {
        int n = (int)(Hz * 0.07f);
        float[] d = new float[n];
        System.Random r = new System.Random(7);
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n;
            float env = Mathf.Exp(-t * 18f);
            d[i] = (Mathf.Sin(2 * Mathf.PI * 1800f * i / Hz) * 0.5f + (float)(r.NextDouble() * 2 - 1) * 0.5f) * env;
        }
        return Crear("Clic", Normalizar(d, 0.5f));
    }

    /// <summary>Golpe grave para confirmar / abrir maletín.</summary>
    public static AudioClip GolpeGrave()
    {
        int n = (int)(Hz * 0.9f);
        float[] d = new float[n];
        System.Random r = new System.Random(9);
        float lp = 0;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Hz;
            float f = 55f * Mathf.Exp(-t * 3f) + 30f;
            float env = Mathf.Exp(-t * 4.5f);
            float ruido = (float)(r.NextDouble() * 2 - 1);
            lp += (ruido - lp) * 0.05f;
            d[i] = (Mathf.Sin(2 * Mathf.PI * f * t) + lp * 0.8f) * env;
        }
        return Crear("Golpe", Normalizar(d, 0.9f));
    }

    /// <summary>Chirrido metálico lejano (puerta, columpio, reja).</summary>
    public static AudioClip Rechinido(int semilla)
    {
        System.Random r = new System.Random(semilla);
        float dur = 1.2f + (float)r.NextDouble() * 1.2f;
        int n = (int)(Hz * dur);
        float[] d = new float[n];
        float f0 = 380f + (float)r.NextDouble() * 400f;
        float fase = 0;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n;
            float f = f0 + Mathf.Sin(t * 9f) * 60f + t * 120f + (float)(r.NextDouble() - 0.5) * 30f;
            fase += 2 * Mathf.PI * f / Hz;
            float env = Mathf.Sin(Mathf.PI * t) * (0.6f + 0.4f * Mathf.Sin(t * 37f));
            float s = Mathf.Sin(fase) + 0.4f * Mathf.Sin(fase * 2.01f) + 0.2f * Mathf.Sin(fase * 3.03f);
            d[i] = s * env;
        }
        return Crear("Rechinido", Normalizar(d, 0.5f));
    }

    /// <summary>Tres golpes lejanos, como alguien tocando madera.</summary>
    public static AudioClip Toquidos(int semilla)
    {
        System.Random r = new System.Random(semilla);
        int golpes = 2 + r.Next(3);
        int n = (int)(Hz * (0.45f * golpes + 0.5f));
        float[] d = new float[n];
        for (int g = 0; g < golpes; g++)
        {
            int inicio = (int)(Hz * (g * 0.45f + (float)r.NextDouble() * 0.06f));
            float lp = 0;
            for (int i = 0; i < Hz * 0.3f && inicio + i < n; i++)
            {
                float t = i / (float)Hz;
                float ruido = (float)(r.NextDouble() * 2 - 1);
                lp += (ruido - lp) * 0.12f;
                d[inicio + i] += (lp * 2f + Mathf.Sin(2 * Mathf.PI * 110f * t) * 0.6f) * Mathf.Exp(-t * 28f);
            }
        }
        return Crear("Toquidos", Normalizar(d, 0.8f));
    }

    /// <summary>Susurro: ruido filtrado con forma de voz.</summary>
    public static AudioClip Susurro(int semilla)
    {
        System.Random r = new System.Random(semilla);
        float dur = 1.5f + (float)r.NextDouble();
        int n = (int)(Hz * dur);
        float[] d = new float[n];
        float bp1 = 0, bp2 = 0;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n;
            float b = (float)(r.NextDouble() * 2 - 1);
            float corte = 0.25f + 0.15f * Mathf.Sin(t * 40f + semilla);
            bp1 += (b - bp1) * corte;
            bp2 += (bp1 - bp2) * corte;
            float silabas = Mathf.Max(0, Mathf.Sin(t * Mathf.PI * (6 + semilla % 4)));
            d[i] = (bp1 - bp2) * silabas * Mathf.Sin(Mathf.PI * t);
        }
        return Crear("Susurro", Normalizar(d, 0.45f));
    }

    /// <summary>Paso sobre tierra/hojas.</summary>
    public static AudioClip Paso(int semilla)
    {
        System.Random r = new System.Random(semilla);
        int n = (int)(Hz * 0.22f);
        float[] d = new float[n];
        float lp = 0;
        float corte = 0.15f + (float)r.NextDouble() * 0.15f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Hz;
            float b = (float)(r.NextDouble() * 2 - 1);
            lp += (b - lp) * corte;
            float crujido = r.NextDouble() < 0.02 ? (float)(r.NextDouble() * 2 - 1) * 2f : 0f;
            float env = Mathf.Exp(-t * 22f) + 0.3f * Mathf.Exp(-Mathf.Abs(t - 0.05f) * 60f);
            d[i] = (lp * 1.4f + crujido) * env;
        }
        return Crear("Paso", Normalizar(d, 0.6f));
    }

    /// <summary>Latido (para cuando algo está cerca).</summary>
    public static AudioClip Latido()
    {
        int n = (int)(Hz * 0.9f);
        float[] d = new float[n];
        for (int k = 0; k < 2; k++)
        {
            int ini = (int)(Hz * (k == 0 ? 0f : 0.22f));
            for (int i = 0; i < Hz * 0.25f && ini + i < n; i++)
            {
                float t = i / (float)Hz;
                d[ini + i] += Mathf.Sin(2 * Mathf.PI * (48f - t * 40f) * t) * Mathf.Exp(-t * 14f) * (k == 0 ? 1f : 0.7f);
            }
        }
        return Crear("Latido", Normalizar(d, 0.9f));
    }

    /// <summary>"Sílaba" de voz distorsionada para el diálogo del Vigía.</summary>
    public static AudioClip VozBlip(int semilla)
    {
        System.Random r = new System.Random(semilla);
        int n = (int)(Hz * 0.075f);
        float[] d = new float[n];
        float f = 95f + (float)r.NextDouble() * 70f;
        float fase = 0;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)n;
            fase += 2 * Mathf.PI * f * (1f - t * 0.3f) / Hz;
            float cuadrada = Mathf.Sign(Mathf.Sin(fase)) * 0.5f + Mathf.Sin(fase * 2.02f) * 0.3f;
            float ruido = (float)(r.NextDouble() * 2 - 1) * 0.25f;
            float env = Mathf.Sin(Mathf.PI * t);
            d[i] = (cuadrada + ruido) * env;
        }
        return Crear("Voz", Normalizar(d, 0.5f));
    }
}
