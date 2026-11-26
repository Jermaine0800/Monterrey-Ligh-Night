using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Director del acecho: hace que SIENTAS que algo te sigue aunque no lo veas.
/// Va subiendo la tensión con pasos detrás de ti, ramas que truenan y respiraciones;
/// cuando la tensión llega al límite, suelta a uno de los monstruos.
/// La Historia lo habilita o lo apaga (no hay monstruos durante diálogos ni antes de tener la linterna).
/// </summary>
public class AcechoDirector : MonoBehaviour
{
    public static AcechoDirector Instancia { get; private set; }

    [Tooltip("Lo prende la historia cuando el jugador ya tiene la linterna")]
    public bool habilitado = false;

    [Header("Tensión")]
    public float tensionPorSegundo = 1f;
    public float tensionParaSoltarMonstruo = 60f;
    public float esperaTrasMonstruo = 35f;

    [Header("Señales")]
    public float cadaCuantoSenal = 9f;

    readonly List<EnemigoBase> enemigos = new List<EnemigoBase>();
    EnemigoBase actual;
    float tension, enfriamiento, siguienteSenal;
    Transform jugador;
    PlayerMovement mov;
    AudioSource fuente;
    AudioClip[] pasos, ramas;
    AudioClip respiracion;

    void Awake()
    {
        Instancia = this;
        // Los monstruos salían demasiado tarde: ahora la tensión sube el doble de rápido y descansan menos
        tensionParaSoltarMonstruo = Mathf.Min(tensionParaSoltarMonstruo, 30f);
        esperaTrasMonstruo = Mathf.Min(esperaTrasMonstruo, 20f);
        cadaCuantoSenal = Mathf.Min(cadaCuantoSenal, 6f);
    }

    void Start()
    {
        foreach (EnemigoBase e in FindObjectsByType<EnemigoBase>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            enemigos.Add(e);
            e.AlTerminar += AlTerminarEnemigo;
        }

        GameObject g = new GameObject("FuentePresencia");
        g.transform.SetParent(transform);
        fuente = g.AddComponent<AudioSource>();
        fuente.spatialBlend = 1f; fuente.minDistance = 2f; fuente.maxDistance = 25f;
        fuente.rolloffMode = AudioRolloffMode.Linear;

        pasos = new AudioClip[4];
        for (int i = 0; i < pasos.Length; i++) pasos[i] = ProceduralAudio.Paso(500 + i);
        ramas = new[] { ProceduralAudio.Rama(1), ProceduralAudio.Rama(2), ProceduralAudio.Rama(3) };
        respiracion = ProceduralAudio.Respiracion(9);
    }

    int llavesTomadas;
    InventoryUI inventarioUI;
    bool autoEncendido;
    float tiempoConMochila;

    /// <summary>
    /// Cada llave que tomas despierta algo: susto inmediato y un monstruo te busca.
    /// Además cada llave hace que salgan más seguido.
    /// </summary>
    public void SustoPorLlave()
    {
        llavesTomadas++;
        habilitado = true;
        tensionParaSoltarMonstruo = Mathf.Max(10f, 26f - llavesTomadas * 3.5f);
        esperaTrasMonstruo = Mathf.Max(7f, 18f - llavesTomadas * 2.5f);
        cadaCuantoSenal = Mathf.Max(3f, 6f - llavesTomadas * 0.6f);
        StartCoroutine(RutinaSustoLlave());
    }

    System.Collections.IEnumerator RutinaSustoLlave()
    {
        yield return new WaitForSeconds(1.6f);
        if (jugador == null) yield break;
        TunelLineaK t = FindAnyObjectByType<TunelLineaK>();
        if (t != null && t.JugadorDentro) yield break;          // en el túnel hay sus propios sustos
        VHSOverlay.Glitch(0.8f, 0.7f);
        HorrorInput.Vibrar(0.5f, 0.9f, 0.5f, this);
        if (fuente != null && respiracion != null)
        {
            fuente.transform.position = jugador.position - (Camera.main != null ? Camera.main.transform.forward : jugador.forward) * 2f;
            fuente.PlayOneShot(respiracion, 0.9f);
        }
        DialogoUI.Susurrar("", "...la llave estaba tibia. Alguien la estaba cuidando.", 3f);
        yield return new WaitForSeconds(1.2f);
        if (actual != null && actual.Activo) yield break;
        ForzarEn(jugador.position);
    }

    public void Habilitar(bool v)
    {
        habilitado = v;
        if (!v && actual != null) { actual.Reiniciar(); actual = null; }
    }

    void Update()
    {
        if (jugador == null)
        {
            mov = FindFirstObjectByType<PlayerMovement>();
            if (mov == null) return;
            jugador = mov.transform;
        }
        SustosAlAcercarte();

        // Seguro: si exploras por tu cuenta (sin seguir el sendero del Vigía), después de un rato con la mochila ya te siguen
        if (!habilitado && !autoEncendido && HorrorGameState.PuedeJugar)
        {
            if (inventarioUI == null) inventarioUI = FindAnyObjectByType<InventoryUI>();
            if (inventarioUI != null && inventarioUI.hasBackPack) tiempoConMochila += Time.deltaTime;
            if (tiempoConMochila > 75f) { autoEncendido = true; habilitado = true; }
        }
        if (!habilitado || !HorrorGameState.PuedeJugar || DialogoUI.Activo) return;
        Eventos();
        if (actual != null) return;      // uno a la vez

        if (enfriamiento > 0f) { enfriamiento -= Time.deltaTime; return; }

        // Más tensión si estás a oscuras o corriendo (haces ruido)
        FlashlightController l = jugador.GetComponent<FlashlightController>();
        float factor = 1f;
        if (l == null || !l.Encendida) factor += 0.6f;
        factor += mov.NivelRuido * 0.5f;
        tension += tensionPorSegundo * factor * Time.deltaTime;

        if (Time.time > siguienteSenal && tension > tensionParaSoltarMonstruo * 0.25f)
        {
            siguienteSenal = Time.time + cadaCuantoSenal * Random.Range(0.6f, 1.4f);
            StartCoroutine(Senal());
        }

        if (tension >= tensionParaSoltarMonstruo) SoltarMonstruo();
    }

    /// <summary>Algo detrás de ti: pasos que imitan los tuyos, una rama, una respiración.</summary>
    IEnumerator Senal()
    {
        Transform cam = Camera.main != null ? Camera.main.transform : jugador;
        Vector3 atras = -cam.forward; atras.y = 0; atras.Normalize();
        Vector3 lado = Vector3.Cross(Vector3.up, atras) * Random.Range(-3f, 3f);
        float r = Random.value;

        if (r < 0.45f)
        {
            // Pasos que se detienen cuando tú te detienes
            for (int i = 0; i < Random.Range(3, 6); i++)
            {
                fuente.transform.position = jugador.position + atras * Random.Range(6f, 10f) + lado;
                fuente.pitch = Random.Range(0.8f, 1f);
                fuente.PlayOneShot(pasos[Random.Range(0, pasos.Length)], 0.8f);
                yield return new WaitForSeconds(0.55f);
                if (mov.NivelRuido < 0.1f) break;
            }
        }
        else if (r < 0.8f)
        {
            fuente.transform.position = jugador.position + atras * Random.Range(8f, 16f) + lado * 2f;
            fuente.PlayOneShot(ramas[Random.Range(0, ramas.Length)], 1f);
        }
        else
        {
            fuente.transform.position = jugador.position + atras * 2.5f;
            fuente.PlayOneShot(respiracion, 0.45f);
            VHSOverlay.Glitch(0.2f, 0.4f);
        }
    }

    void SoltarMonstruo()
    {
        List<EnemigoBase> libres = enemigos.FindAll(e => e != null && e.EstadoActual == EnemigoBase.Estado.Inactivo);
        if (libres.Count == 0) { tension = tensionParaSoltarMonstruo * 0.5f; return; }
        actual = Siguiente(libres);
        if (MusicaAdaptativa.Instancia != null) MusicaAdaptativa.Instancia.Silencio(3f);
        actual.Activar();
        tension = 0f;
    }

    void AlTerminarEnemigo(EnemigoBase e)
    {
        if (e != actual) return;
        actual = null;
        enfriamiento = esperaTrasMonstruo;
    }

    /// <summary>Para la historia: forzar una aparición ahora.</summary>
    public void ForzarAparicion() => tension = tensionParaSoltarMonstruo;

    public bool HayMonstruoActivo => actual != null && actual.Activo;

    /// <summary>Para la historia: suelta un monstruo YA, cerca de un punto (aunque el director esté apagado).</summary>
    public void ForzarEn(Vector3 punto)
    {
        if (actual != null && actual.Activo) return;
        List<EnemigoBase> libres = enemigos.FindAll(e => e != null && e.EstadoActual == EnemigoBase.Estado.Inactivo);
        if (libres.Count == 0) return;
        actual = Siguiente(libres);
        Vector2 r = Random.insideUnitCircle.normalized * Random.Range(8f, 14f);
        if (MusicaAdaptativa.Instancia != null) MusicaAdaptativa.Instancia.Silencio(2.5f);
        actual.ActivarEn(punto + new Vector3(r.x, 0, r.y));
        tension = 0f;
    }

    // ------------------------------------------------------------------ Latido cuando algo está cerca

    AudioSource latido;

    void LateUpdate()
    {
        if (jugador == null) return;
        if (latido == null)
        {
            latido = gameObject.AddComponent<AudioSource>();
            latido.clip = ProceduralAudio.Latido();
            latido.loop = true; latido.spatialBlend = 0f; latido.volume = 0f;
            latido.Play();
        }
        float cerca = 999f;
        foreach (EnemigoBase e in enemigos)
            if (e != null && e.Activo) cerca = Mathf.Min(cerca, Vector3.Distance(e.transform.position, jugador.position));
        float objetivo = cerca < 25f ? Mathf.InverseLerp(25f, 3f, cerca) : 0f;
        latido.volume = Mathf.MoveTowards(latido.volume, objetivo * 0.9f, Time.deltaTime);
        latido.pitch = 1f + objetivo * 0.6f;
        if (objetivo > 0.6f && Random.value < 0.01f) VHSOverlay.Glitch(objetivo * 0.4f, 0.15f);
    }

    // ================================================================== Sustos al acercarte a cada lugar
    // Mientras te acercas a un lugar donde todavía está la llave, algo se deja ver entre los árboles
    // (solo para asustarte: desaparece en cuanto lo miras). Cuando tomas la llave, ahí sí te persiguen.

    SitioLlave[] sitios;
    float siguienteRevision, siguienteAsomo;

    void SustosAlAcercarte()
    {
        if (Time.time < siguienteRevision || jugador == null || !HorrorGameState.PuedeJugar || DialogoUI.Activo) return;
        siguienteRevision = Time.time + 1f;
        if (sitios == null) sitios = FindObjectsByType<SitioLlave>(FindObjectsInactive.Exclude);
        if (Time.time < siguienteAsomo || (actual != null && actual.Activo)) return;
        TunelLineaK t = FindAnyObjectByType<TunelLineaK>();
        if (t != null && t.JugadorDentro) return;

        foreach (SitioLlave s in sitios)
        {
            if (s == null || s.Completado || (s.llave != null && s.llave.Tomada)) continue;
            Vector3 a = s.transform.position - jugador.position; a.y = 0;
            if (a.magnitude > s.radioLlegada + 35f) continue;

            EnemigoBase e = enemigos.Find(x => x != null && x.EstadoActual == EnemigoBase.Estado.Inactivo && !x.MuertoParaSiempre);
            if (e == null) return;
            Transform cam = Camera.main != null ? Camera.main.transform : jugador;
            Vector3 f = cam.forward; f.y = 0; f.Normalize();
            Vector3 punto = jugador.position + Quaternion.Euler(0, Random.Range(-40f, 40f), 0) * f * Random.Range(12f, 18f);
            StartCoroutine(e.Vistazo(punto, 6f));
            StartCoroutine(Senal());
            siguienteAsomo = Time.time + Random.Range(22f, 38f);
            return;
        }
    }

    // ================================================================== Más acción
    float siguienteEvento = 40f, siguienteSegundo;
    EnemigoBase segundo;

    /// <summary>Con 2 llaves o más, a veces viene otro mientras el primero te persigue.</summary>
    void SegundoMonstruo()
    {
        if (llavesTomadas < 2 || Time.time < siguienteSegundo || !actual.Activo) return;
        siguienteSegundo = Time.time + Random.Range(25f, 45f);
        if (segundo != null && segundo.Activo) return;
        if (Random.value > 0.25f + llavesTomadas * 0.1f) return;
        TunelLineaK t = FindAnyObjectByType<TunelLineaK>();
        if (t != null && t.JugadorDentro) return;
        segundo = enemigos.Find(e => e != null && e != actual && e.EstadoActual == EnemigoBase.Estado.Inactivo && !e.MuertoParaSiempre);
        if (segundo == null) return;
        Vector2 r = Random.insideUnitCircle.normalized * Random.Range(14f, 20f);
        segundo.ActivarEn(jugador.position + new Vector3(r.x, 0, r.y));
        DialogoUI.Susurrar("", "...son dos. SON DOS.", 2.5f);
        SensacionJuego.Sacudir(0.3f);
    }

    /// <summary>Que siempre esté pasando algo: cada 35–60 s un evento (algo te mira, un grito, la linterna falla, algo corre entre los árboles).</summary>
    void Eventos()
    {
        if (Time.time < siguienteEvento || jugador == null) return;
        siguienteEvento = Time.time + Random.Range(35f, 60f) - llavesTomadas * 4f;
        TunelLineaK t = FindAnyObjectByType<TunelLineaK>();
        if (t != null && t.JugadorDentro) return;
        Transform cam = Camera.main != null ? Camera.main.transform : jugador;
        Vector3 f = cam.forward; f.y = 0; f.Normalize();
        float r = Random.value;
        if (r < 0.4f)
        {
            EnemigoBase e = enemigos.Find(x => x != null && x.EstadoActual == EnemigoBase.Estado.Inactivo && !x.MuertoParaSiempre);
            if (e != null) StartCoroutine(e.Vistazo(jugador.position + Quaternion.Euler(0, Random.Range(-50f, 50f), 0) * f * Random.Range(12f, 20f), 5f));
        }
        else if (r < 0.65f)
        {
            fuente.transform.position = jugador.position - f * Random.Range(15f, 25f);
            fuente.PlayOneShot(ProceduralAudio.Grito(Random.Range(1, 99)), 1f);
            SensacionJuego.Sacudir(0.15f);
        }
        else if (r < 0.85f) StartCoroutine(LinternaFalla());
        else StartCoroutine(Senal());
    }

    System.Collections.IEnumerator LinternaFalla()
    {
        FlashlightController l = jugador.GetComponent<FlashlightController>();
        Light luz = l != null ? l.GetComponentInChildren<Light>() : null;
        VHSOverlay.Glitch(0.6f, 1.2f);
        for (int i = 0; i < 8; i++)
        {
            foreach (Light x in jugador.GetComponentsInChildren<Light>()) if (x.type == LightType.Spot) x.intensity *= 0.1f;
            yield return new WaitForSeconds(Random.Range(0.05f, 0.2f));
            yield return null;   // el controlador de la linterna la restablece solo
        }
        DialogoUI.Susurrar("", "La linterna... no, no, no.", 2f);
    }

    // Uno y luego el otro: se turnan (La Sonriente, El Asomado, La Sonriente...)
    EnemigoBase ultimoSoltado;
    EnemigoBase Siguiente(List<EnemigoBase> libres)
    {
        EnemigoBase e = libres.Find(x => x != ultimoSoltado);
        if (e == null) e = libres[0];
        ultimoSoltado = e;
        return e;
    }
}
