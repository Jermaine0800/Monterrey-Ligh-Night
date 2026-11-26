using System.Collections;
using UnityEngine;

/// <summary>
/// Carro viejo del campamento (no se maneja). Si lo arreglas manteniendo el botón de acción
/// (abrir el cofre → conectar los cables de la batería → puentear el encendido sin moverte),
/// arranca: prende los faros y la alarma suena tan fuerte que espanta a los monstruos cercanos.
/// Después de unos segundos se ahoga y se calla; hay que volver a puentearlo (y la batería necesita reposo).
/// </summary>
public class CarroCampamento : MonoBehaviour, IInteractable
{
    public bool Encendido { get; private set; }
    public bool Arreglando { get; private set; }
    public float Listo01 => Mathf.Clamp01(1f - (siguienteUso - Time.time) / Reposo);

    const float DuracionAlarma = 12f;
    const float Reposo = 60f;
    const float RadioEspanto = 40f;

    Transform cofre;
    Light[] faros;
    Renderer[] focos;
    Material focoApagado, focoPrendido;
    AudioSource alarma, motor;
    Light chispa;
    float siguienteUso;

    static readonly string[] Pasos = { "Abriendo el cofre...", "Conectando los cables de la batería...", "Puenteando el encendido... no te muevas" };
    static readonly float[] Tiempos = { 1.0f, 1.8f, 1.6f };

    /// <summary>Construye el carro (estilo PSX, con piezas simples) junto a un punto.</summary>
    public static CarroCampamento Crear(Vector3 posicion, float giroY)
    {
        GameObject raiz = new GameObject("Carro_Campamento");
        raiz.transform.position = Mundo.EnSuelo(posicion);
        raiz.transform.rotation = Quaternion.Euler(0f, giroY, 0f);
        CarroCampamento c = raiz.AddComponent<CarroCampamento>();
        c.Construir();
        return c;
    }

    static Material Mat(Color col, bool brilla = false)
    {
        Shader sh = brilla ? Shader.Find("Universal Render Pipeline/Unlit") : Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/Unlit");
        Material m = new Material(sh);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
        m.color = col;
        return m;
    }

    Transform Pieza(PrimitiveType tipo, string nombre, Transform padre, Vector3 pos, Vector3 escala, Material m, bool choque = false, Vector3? giro = null)
    {
        GameObject g = GameObject.CreatePrimitive(tipo);
        g.name = nombre;
        g.transform.SetParent(padre, false);
        g.transform.localPosition = pos;
        g.transform.localScale = escala;
        if (giro.HasValue) g.transform.localRotation = Quaternion.Euler(giro.Value);
        g.GetComponent<Renderer>().sharedMaterial = m;
        if (!choque) Destroy(g.GetComponent<Collider>());
        return g.transform;
    }

    void Construir()
    {
        Material pintura = Mat(new Color(0.42f, 0.12f, 0.1f)), oxido = Mat(new Color(0.3f, 0.2f, 0.14f));
        Material vidrio = Mat(new Color(0.1f, 0.13f, 0.15f)), llanta = Mat(new Color(0.05f, 0.05f, 0.05f)), cromo = Mat(new Color(0.55f, 0.55f, 0.5f));
        focoApagado = Mat(new Color(0.35f, 0.33f, 0.25f)); focoPrendido = Mat(new Color(1f, 0.95f, 0.75f), true);

        Transform t = transform;
        Pieza(PrimitiveType.Cube, "Carroceria", t, new Vector3(0, 0.75f, 0), new Vector3(1.8f, 0.7f, 4.2f), pintura, true);
        Pieza(PrimitiveType.Cube, "Cabina", t, new Vector3(0, 1.35f, -0.3f), new Vector3(1.6f, 0.55f, 2.0f), pintura, true);
        Pieza(PrimitiveType.Cube, "Parabrisas", t, new Vector3(0, 1.36f, 0.71f), new Vector3(1.5f, 0.5f, 0.02f), vidrio, false, new Vector3(-18f, 0, 0));
        Pieza(PrimitiveType.Cube, "Ventana_I", t, new Vector3(-0.81f, 1.38f, -0.3f), new Vector3(0.02f, 0.42f, 1.7f), vidrio);
        Pieza(PrimitiveType.Cube, "Ventana_D", t, new Vector3(0.81f, 1.38f, -0.3f), new Vector3(0.02f, 0.42f, 1.7f), vidrio);
        Pieza(PrimitiveType.Cube, "Defensa", t, new Vector3(0, 0.45f, 2.12f), new Vector3(1.85f, 0.18f, 0.12f), cromo);
        Pieza(PrimitiveType.Cube, "Mancha_Oxido", t, new Vector3(0.55f, 0.9f, -1.6f), new Vector3(0.7f, 0.4f, 0.9f), oxido);
        for (int i = 0; i < 4; i++)
        {
            float x = i % 2 == 0 ? -0.85f : 0.85f, z = i < 2 ? 1.35f : -1.35f;
            Pieza(PrimitiveType.Cylinder, "Llanta_" + i, t, new Vector3(x, 0.38f, z), new Vector3(0.72f, 0.12f, 0.72f), llanta, false, new Vector3(0, 0, 90f));
        }
        // Cofre con bisagra (se abre al arreglarlo)
        cofre = new GameObject("Bisagra_Cofre").transform;
        cofre.SetParent(t, false); cofre.localPosition = new Vector3(0, 1.11f, 0.75f);
        Pieza(PrimitiveType.Cube, "Cofre", cofre, new Vector3(0, 0.02f, 0.68f), new Vector3(1.75f, 0.05f, 1.35f), pintura);
        Pieza(PrimitiveType.Cube, "Motor", t, new Vector3(0, 0.95f, 1.4f), new Vector3(1.2f, 0.3f, 0.9f), oxido);
        Pieza(PrimitiveType.Cube, "Bateria", t, new Vector3(0.5f, 1.05f, 1.75f), new Vector3(0.3f, 0.22f, 0.25f), Mat(new Color(0.1f, 0.12f, 0.1f)));

        // Faros
        faros = new Light[2]; focos = new Renderer[2];
        for (int i = 0; i < 2; i++)
        {
            float x = i == 0 ? -0.62f : 0.62f;
            focos[i] = Pieza(PrimitiveType.Cube, "Faro_" + i, t, new Vector3(x, 0.82f, 2.11f), new Vector3(0.3f, 0.2f, 0.04f), focoApagado).GetComponent<Renderer>();
            GameObject l = new GameObject("LuzFaro_" + i);
            l.transform.SetParent(t, false); l.transform.localPosition = new Vector3(x, 0.82f, 2.2f); l.transform.localRotation = Quaternion.Euler(6f, 0, 0);
            faros[i] = l.AddComponent<Light>(); faros[i].type = LightType.Spot; faros[i].range = 28f; faros[i].spotAngle = 60f;
            faros[i].intensity = 6f; faros[i].color = new Color(1f, 0.95f, 0.8f); faros[i].enabled = false;
        }
        GameObject ch = new GameObject("Chispa"); ch.transform.SetParent(t, false); ch.transform.localPosition = new Vector3(0.5f, 1.4f, 1.75f);
        chispa = ch.AddComponent<Light>(); chispa.type = LightType.Point; chispa.range = 3f; chispa.intensity = 0f; chispa.color = new Color(0.6f, 0.8f, 1f);

        // Zona para interactuar (al frente, junto al cofre)
        SphereCollider s = gameObject.AddComponent<SphereCollider>();
        s.isTrigger = true; s.radius = 1.6f; s.center = new Vector3(0, 0.9f, 2.6f);

        alarma = gameObject.AddComponent<AudioSource>();
        alarma.clip = Sirena(); alarma.loop = true; alarma.spatialBlend = 0.85f; alarma.maxDistance = 80f; alarma.volume = 1f;
        alarma.rolloffMode = AudioRolloffMode.Linear;
        motor = gameObject.AddComponent<AudioSource>();
        motor.clip = Motor(); motor.loop = true; motor.spatialBlend = 1f; motor.maxDistance = 30f; motor.volume = 0.6f;
    }

    public void Interact(PlayerInteraction jugador)
    {
        if (Encendido || Arreglando || AccionMantenida.Ocupado) return;
        if (Time.time < siguienteUso)
        {
            DialogoUI.Susurrar("", "La batería está muerta... hay que dejarla descansar un poco.", 2.5f);
            return;
        }
        Arreglando = true;
        StartCoroutine(Efectos());
        StartCoroutine(AccionMantenida.Ejecutar(jugador.transform, Pasos, Tiempos, 2, ok =>
        {
            Arreglando = false;
            if (ok) StartCoroutine(Arrancar());
            else { StartCoroutine(Cerrar()); DialogoUI.Susurrar("", "Se soltó un cable. Otra vez.", 2f); }
        }));
    }

    // El cofre se abre y saltan chispas mientras conectas los cables
    IEnumerator Efectos()
    {
        for (float t = 0; t < 0.6f && Arreglando; t += Time.deltaTime) { cofre.localRotation = Quaternion.Euler(-Mathf.Lerp(0, 65f, t / 0.6f), 0, 0); yield return null; }
        while (Arreglando)
        {
            chispa.intensity = Random.value < 0.25f ? Random.Range(1.5f, 4f) : 0f;
            yield return new WaitForSeconds(0.06f);
        }
        chispa.intensity = 0f;
    }

    IEnumerator Cerrar()
    {
        for (float t = 0; t < 0.4f; t += Time.deltaTime) { cofre.localRotation = Quaternion.Euler(-Mathf.Lerp(65f, 0, t / 0.4f), 0, 0); yield return null; }
        cofre.localRotation = Quaternion.identity;
    }

    IEnumerator Arrancar()
    {
        Encendido = true;
        StartCoroutine(Cerrar());
        motor.Play();
        yield return new WaitForSeconds(0.5f);
        Faros(true);
        alarma.Play();
        VHSOverlay.Glitch(0.6f, 0.5f);
        SensacionJuego.Sacudir(0.4f);
        DialogoUI.Susurrar("", "¡PRENDIÓ! ...eso los va a espantar.", 2.5f);

        for (float t = 0; t < DuracionAlarma; t += Time.deltaTime)
        {
            Espantar();
            // los faros parpadean al ritmo de la alarma
            bool on = Mathf.Repeat(t, 0.5f) < 0.3f;
            foreach (Light f in faros) f.intensity = on ? 7f : 3f;
            yield return null;
        }
        // se ahoga y se calla
        for (float t = 0; t < 1.5f; t += Time.deltaTime)
        {
            alarma.volume = Mathf.Lerp(1f, 0f, t / 1.5f);
            motor.pitch = Mathf.Lerp(1f, 0.4f, t / 1.5f);
            foreach (Light f in faros) f.intensity = Random.Range(0f, 3f);
            yield return null;
        }
        alarma.Stop(); motor.Stop(); alarma.volume = 1f; motor.pitch = 1f;
        Faros(false);
        Encendido = false;
        siguienteUso = Time.time + Reposo;
        DialogoUI.Susurrar("", "Se murió el carro. Y ya no se oye nada...", 2.5f);
    }

    void Faros(bool on)
    {
        foreach (Light f in faros) f.enabled = on;
        foreach (Renderer r in focos) r.sharedMaterial = on ? focoPrendido : focoApagado;
    }

    static readonly System.Reflection.MethodInfo retirarse = typeof(EnemigoBase).GetMethod("Retirarse", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

    void Espantar()
    {
        foreach (EnemigoBase e in FindObjectsByType<EnemigoBase>(FindObjectsInactive.Exclude))
        {
            if (e == null || !e.Activo) continue;
            if (Vector3.Distance(e.transform.position, transform.position) > RadioEspanto) continue;
            retirarse?.Invoke(e, new object[] { true });
        }
    }

    // ------------------------------------------------------------------ sonidos generados (funcionan también en la web)

    static AudioClip Sirena()
    {
        int sr = 22050, n = sr * 2;
        float[] d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)sr;
            float f = Mathf.Repeat(t, 1f) < 0.5f ? 440f : 560f;     // claxon de alarma: dos tonos
            float s = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * f * t)) * 0.5f + Mathf.Sin(2f * Mathf.PI * f * 2f * t) * 0.2f;
            d[i] = s * 0.55f;
        }
        AudioClip c = AudioClip.Create("Alarma", n, 1, sr, false);
        c.SetData(d, 0);
        return c;
    }

    static AudioClip Motor()
    {
        int sr = 22050, n = sr;
        float[] d = new float[n];
        System.Random r = new System.Random(7);
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)sr;
            d[i] = (Mathf.Sin(2f * Mathf.PI * 38f * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * 76f * t) * 0.25f + ((float)r.NextDouble() - 0.5f) * 0.15f) * (0.7f + 0.3f * Mathf.Sin(2f * Mathf.PI * 6f * t));
        }
        AudioClip c = AudioClip.Create("Motor", n, 1, sr, false);
        c.SetData(d, 0);
        return c;
    }
}
