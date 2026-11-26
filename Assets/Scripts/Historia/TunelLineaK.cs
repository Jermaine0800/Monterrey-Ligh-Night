using System.Collections;
using UnityEngine;

/// <summary>
/// El Túnel Línea K: un muro de concreto con una marca pintada. Si la alumbras con la linterna,
/// la cinta se "traba" y aparece la puerta oxidada del EXPEDIENTE 08.
/// El interior (andén, caseta, casillero) está construido bajo tierra; las puertas te llevan de un lado a otro.
/// </summary>
public class TunelLineaK : MonoBehaviour
{
    [Header("Exterior")]
    public Transform muro;
    public Transform marca;
    public GameObject puertaExterior;

    [Header("Interior")]
    public Transform interior;
    public Transform monitor;
    public Casillero casillero;
    public Transform puertaSalida;
    public Light[] luces;
    public GameObject silueta;          // la figura de cabeza radial al fondo de las vías

    [Header("Ajustes")]
    public float segundosAlumbrando = 0.8f;
    public float distanciaMaxima = 16f;

    public bool Revelado { get; private set; }
    public bool JugadorDentro => jugador != null && interior != null && jugador.position.y < interior.position.y + 15f;

    Transform jugador;
    FlashlightController linterna;
    float alumbrado;
    AudioSource fuente;

    void Start()
    {
        if (puertaExterior != null) puertaExterior.SetActive(false);
        if (silueta != null) silueta.SetActive(false);
        fuente = gameObject.AddComponent<AudioSource>();
        fuente.spatialBlend = 1f; fuente.minDistance = 3f; fuente.maxDistance = 30f;
        CrearRampas();
        EntradaVisible();
        distanciaMaxima = Mathf.Max(distanciaMaxima, 25f);
        if (interior != null && MetroTunel.Instancia == null) MetroTunel.Crear(this);
        // un poco más de luz en el andén (antes casi no se veía)
        if (luces != null) foreach (Light l in luces) if (l != null) { l.intensity *= 1.5f; l.range *= 1.25f; }
    }

    /// <summary>
    /// Rampas para subir de las vías al andén (el andén mide 1 m: si te caías a las vías ya no podías subir).
    /// Dos a los lados del andén y una en la punta donde se corta la obra.
    /// </summary>
    void CrearRampas()
    {
        if (interior == null || interior.Find("Rampa_Anden_0") != null) return;
        Transform anden = interior.Find("Anden");
        Material mat = anden != null && anden.GetComponent<Renderer>() != null ? anden.GetComponent<Renderer>().sharedMaterial : null;
        float largo = Mathf.Sqrt(1.6f * 1.6f + 1f);      // baja 1 m en 1.6 m (≈32°)
        float[] zs = { -14f, 1.5f };
        for (int i = 0; i < zs.Length; i++)
            Rampa("Rampa_Anden_" + i, new Vector3(0.247f, 0.415f, zs[i]), Quaternion.Euler(0f, 0f, -32f), new Vector3(largo + 0.15f, 0.2f, 2.2f), mat);
        Rampa("Rampa_Anden_Punta", new Vector3(-2.75f, 0.415f, 6.75f), Quaternion.Euler(32f, 0f, 0f), new Vector3(4.4f, 0.2f, largo + 0.15f), mat);
    }

    void Rampa(string nombre, Vector3 local, Quaternion giro, Vector3 tam, Material mat)
    {
        GameObject r = GameObject.CreatePrimitive(PrimitiveType.Cube);
        r.name = nombre;
        r.transform.SetParent(interior, false);
        r.transform.localPosition = local;
        r.transform.localRotation = giro;
        r.transform.localScale = tam;
        if (mat != null) r.GetComponent<Renderer>().sharedMaterial = mat;
        r.isStatic = false;
        // franja amarilla de precaución en la orilla
        GameObject f = GameObject.CreatePrimitive(PrimitiveType.Cube);
        f.name = nombre + "_Franja";
        Destroy(f.GetComponent<Collider>());
        f.transform.SetParent(r.transform, false);
        f.transform.localPosition = new Vector3(0f, 0.51f, 0f);
        f.transform.localScale = new Vector3(1f, 0.02f, 0.12f);
        Renderer fr = f.GetComponent<Renderer>();
        Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh != null) { Material m = new Material(sh); m.SetColor("_BaseColor", new Color(0.85f, 0.75f, 0.1f)); fr.sharedMaterial = m; }
    }

    void Update()
    {
        if (jugador == null)
        {
            PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
            if (pm == null) return;
            jugador = pm.transform;
            linterna = pm.GetComponent<FlashlightController>();
            if (linterna == null) linterna = FindAnyObjectByType<FlashlightController>();
        }
        if (JugadorDentro && !sustosCorriendo) StartCoroutine(SustosTunel());
        if (Revelado || marca == null || linterna == null || !linterna.Encendida) return;

        Camera cam = Camera.main;
        if (cam == null) return;
        Vector3 a = marca.position - cam.transform.position;
        bool apuntando = a.magnitude < distanciaMaxima && Vector3.Angle(cam.transform.forward, a) < 18f;
        alumbrado = apuntando ? alumbrado + Time.deltaTime : Mathf.Max(0f, alumbrado - Time.deltaTime);
        if (alumbrado > segundosAlumbrando) StartCoroutine(Revelar());
    }

    IEnumerator Revelar()
    {
        Revelado = true;
        fuente.transform.position = marca.position;
        fuente.PlayOneShot(ProceduralAudio.Estatica(41), 1f);
        VHSOverlay.Glitch(1f, 1.1f);
        HorrorInput.Vibrar(0.4f, 0.8f, 0.5f, this);
        yield return new WaitForSeconds(0.35f);
        if (puertaExterior != null) puertaExterior.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        fuente.PlayOneShot(ProceduralAudio.Rechinido(8), 0.8f);
    }

    /// <summary>Se van las luces. Algo está parado en las vías. Vuelven las luces: ya no está.</summary>
    public IEnumerator Apagon()
    {
        float[] intensidades = new float[luces.Length];
        for (int i = 0; i < luces.Length; i++) if (luces[i] != null) intensidades[i] = luces[i].intensity;

        // Fallan
        for (int k = 0; k < 6; k++)
        {
            foreach (Light l in luces) if (l != null) l.enabled = k % 2 == 1;
            yield return new WaitForSeconds(Random.Range(0.05f, 0.2f));
        }
        foreach (Light l in luces) if (l != null) l.enabled = false;

        // Zumbido grave y silueta al fondo, con una sola luz roja
        AudioSource s = fuente;
        if (silueta != null)
        {
            silueta.SetActive(true);
            s.transform.position = silueta.transform.position;
            Light roja = silueta.GetComponentInChildren<Light>();
            if (roja != null) roja.enabled = true;
        }
        s.PlayOneShot(ProceduralAudio.Drone(66), 1f);
        VHSOverlay.Glitch(0.5f, 2.5f);
        yield return new WaitForSeconds(2.8f);

        // Pasos sobre la grava... acercándose
        for (int i = 0; i < 4; i++)
        {
            if (silueta != null)
            {
                Vector3 d = jugador.position - silueta.transform.position; d.y = 0;
                silueta.transform.position += d.normalized * 2.5f;
            }
            s.PlayOneShot(ProceduralAudio.Paso(700 + i), 1f);
            yield return new WaitForSeconds(0.5f);
        }

        // Vuelve la luz: no hay nada
        VHSOverlay.Glitch(1f, 0.6f);
        if (silueta != null) silueta.SetActive(false);
        for (int i = 0; i < luces.Length; i++) if (luces[i] != null) { luces[i].enabled = true; luces[i].intensity = intensidades[i] * 0.6f; }
        HorrorInput.Vibrar(0.7f, 0.5f, 0.4f, this);
    }

    // ================================================================== Entrada más clara

    void EntradaVisible()
    {
        if (muro == null) return;
        // Reflector cálido apuntando al muro, y una luz roja que parpadea sobre la marca (se ve desde lejos)
        GameObject r = new GameObject("Reflector_Muro");
        r.transform.position = muro.position + muro.forward * 7f + Vector3.up * 5f;
        r.transform.LookAt(muro.position + Vector3.up * 1.5f);
        Light lr = r.AddComponent<Light>(); lr.type = LightType.Spot; lr.spotAngle = 55f; lr.range = 20f; lr.intensity = 4f;
        lr.color = new Color(1f, 0.85f, 0.6f); lr.shadows = LightShadows.None;
        if (marca != null)
        {
            GameObject m = new GameObject("Baliza_Marca");
            m.transform.position = marca.position + muro.forward * 1.2f + Vector3.up * 0.6f;
            luzMarca = m.AddComponent<Light>(); luzMarca.type = LightType.Point; luzMarca.range = 12f; luzMarca.intensity = 3f;
            luzMarca.color = new Color(1f, 0.15f, 0.1f); luzMarca.shadows = LightShadows.None;
        }
    }

    Light luzMarca;

    void LateUpdate()
    {
        if (luzMarca != null) luzMarca.intensity = Revelado ? 1.2f : (Mathf.Repeat(Time.time, 1.2f) < 0.6f ? 3.5f : 0.6f);
    }

    // ================================================================== Sustos dentro del túnel

    bool sustosCorriendo;
    static readonly System.Reflection.MethodInfo mostrar = typeof(EnemigoBase).GetMethod("Mostrar", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

    /// <summary>Mientras estés adentro, cada 15–25 s algo aparece al fondo y corre hacia ti. Escóndete o huye.</summary>
    IEnumerator SustosTunel()
    {
        sustosCorriendo = true;
        yield return new WaitForSeconds(8f);
        while (JugadorDentro)
        {
            if (HorrorGameState.PuedeJugar && !DialogoUI.Activo && !MetroTunel.PeleaActiva) yield return Embestida();
            yield return new WaitForSeconds(Random.Range(15f, 25f));
        }
        sustosCorriendo = false;
    }

    IEnumerator Embestida()
    {
        EnemigoBase[] todos = FindObjectsByType<EnemigoBase>(FindObjectsInactive.Include);
        EnemigoBase e = null;
        foreach (EnemigoBase x in todos) if (x != null && x.enabled && !x.MuertoParaSiempre && x.EstadoActual == EnemigoBase.Estado.Inactivo) { e = x; break; }
        if (MetroTunel.PeleaActiva) yield break;
        if (e == null) yield break;

        // un punto lejos de ti (al fondo de las vías o al inicio del andén)
        Vector3[] locales = { new Vector3(2.15f, 0f, 16f), new Vector3(2.15f, 0f, -18f), new Vector3(-2.75f, 1f, -18.5f), new Vector3(-2.75f, 1f, 4f) };
        Vector3 desde = interior.TransformPoint(locales[0]); float mejor = 0f;
        foreach (Vector3 l in locales)
        {
            Vector3 w = interior.TransformPoint(l);
            float d = Vector3.Distance(w, jugador.position);
            if (d > mejor) { mejor = d; desde = w; }
        }

        // las luces fallan
        if (luces != null) foreach (Light l in luces) if (l != null) l.enabled = false;
        fuente.transform.position = desde;
        fuente.PlayOneShot(ProceduralAudio.Grito(Random.Range(1, 50)), 0.9f);
        yield return new WaitForSeconds(0.6f);
        if (luces != null) foreach (Light l in luces) if (l != null) l.enabled = true;

        e.gameObject.SetActive(true);
        e.enabled = false;
        e.transform.position = desde;
        mostrar?.Invoke(e, new object[] { true });
        Animator a = e.GetComponentInChildren<Animator>();
        if (a != null && a.HasState(0, Animator.StringToHash("Run"))) a.Play("Run", 0, 0f);
        VHSOverlay.Glitch(0.5f, 0.4f);

        PlayerEscondite esc = jugador.GetComponent<PlayerEscondite>();
        float piso = desde.y;
        for (float t = 0; t < 7f; t += Time.deltaTime)
        {
            Vector3 meta = jugador.position; meta.y = piso;
            Vector3 dir = meta - e.transform.position; dir.y = 0;
            if (dir.sqrMagnitude > 0.01f) e.transform.rotation = Quaternion.LookRotation(dir);
            if (esc != null && esc.Escondido) { yield return new WaitForSeconds(1.5f); break; }      // te escondiste: se queda buscando y se va
            if (dir.magnitude < 1.6f)
            {
                // te alcanzó
                if (a != null && a.HasState(0, Animator.StringToHash("Attack"))) a.Play("Attack", 0, 0f);
                VHSOverlay.Glitch(1f, 0.8f);
                HorrorInput.Vibrar(1f, 1f, 0.5f, this);
                fuente.transform.position = e.transform.position;
                fuente.PlayOneShot(ProceduralAudio.Grito(Random.Range(50, 99)), 1f);
                if (SaludJugador.Instancia != null) SaludJugador.Instancia.RecibirDanio(18f, e.transform.position);
                yield return new WaitForSeconds(0.6f);
                break;
            }
            // sube al andén / baja a las vías siguiendo tu altura
            piso = Mathf.MoveTowards(piso, jugador.position.y, Time.deltaTime * 2f);
            e.transform.position = Vector3.MoveTowards(e.transform.position, new Vector3(meta.x, piso, meta.z), 5.2f * Time.deltaTime);
            yield return null;
        }
        VHSOverlay.Glitch(0.6f, 0.4f);
        mostrar?.Invoke(e, new object[] { false });
        e.enabled = true;
        e.Reiniciar();
    }
}
