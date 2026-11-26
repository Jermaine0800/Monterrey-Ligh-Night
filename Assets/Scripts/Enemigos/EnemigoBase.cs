using System.Collections;
using UnityEngine;

/// <summary>
/// Base de todos los monstruos que acechan. Se encarga de lo común:
/// percibir al jugador (si lo ve, si lo alumbran, si está escondido), aparecer/desaparecer,
/// el susto (screamer), recibir golpes y desterrarse. Cada monstruo decide CÓMO acecha en Tick().
/// </summary>
public abstract class EnemigoBase : Entidad, IReiniciable
{
    public enum Estado { Inactivo, Acechando, Atacando, Aturdido, Desterrado }

    [Header("Identidad")]
    public string nombre = "???";

    [Header("Susto")]
    public float distanciaSusto = 2.2f;
    public float danioSusto = 25f;
    public float enfriamientoSusto = 25f;

    [Header("Destierro")]
    [Tooltip("Segundos que desaparece cuando lo derrotas a golpes (antes de tener las 5 llaves)")]
    public float tiempoDesterrado = 45f;

    /// <summary>Vida mínima: cuesta muchos golpes (pala ~6, hacha ~5). Se sube en Awake si en la escena está más baja.</summary>
    protected virtual float VidaRecomendada => 320f;
    /// <summary>Con las 5 llaves ya se pueden matar de verdad: no vuelven.</summary>
    public bool MuertoParaSiempre { get; private set; }
    float danioEncuentro; int sustosEncuentro;

    public Estado EstadoActual { get; protected set; } = Estado.Inactivo;
    public bool Activo => EstadoActual == Estado.Acechando || EstadoActual == Estado.Atacando;
    /// <summary>Se dispara cuando termina su intento (asustó, lo corriste o te escondiste).</summary>
    public event System.Action<EnemigoBase> AlTerminar;

    protected Transform jugador;
    protected PlayerMovement movJugador;
    protected PlayerEscondite escJugador;
    protected FlashlightController linterna;
    protected Animator anim;
    protected Renderer[] renderers;
    protected Collider[] colliders;
    protected AudioSource voz;
    protected AudioClip grito, golpeado, susurro;
    float ultimoSusto = -999f;
    Coroutine rutinaSusto;

    protected override void Awake()
    {
        vidaMaxima = Mathf.Max(vidaMaxima, VidaRecomendada);
        invulnerableTrasGolpe = Mathf.Max(invulnerableTrasGolpe, 0.5f);
        base.Awake();
        anim = GetComponentInChildren<Animator>();
        if (anim != null) anim.applyRootMotion = false;
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);
        voz = gameObject.AddComponent<AudioSource>();
        voz.spatialBlend = 1f; voz.minDistance = 2f; voz.maxDistance = 40f;
        voz.rolloffMode = AudioRolloffMode.Linear;
        grito = ProceduralAudio.Grito(name.GetHashCode());
        golpeado = ProceduralAudio.GolpeGrave();
        susurro = ProceduralAudio.Susurro((name.GetHashCode() & 255));
    }

    protected virtual void Start()
    {
        if (GetComponent<EnemigoVida>() == null) gameObject.AddComponent<EnemigoVida>();   // que no se vean tiesos
        Mostrar(false);
    }

    // ------------------------------------------------------------------ Ciclo

    void Update()
    {
        if (!BuscarJugador()) return;
        if (EstadoActual != Estado.Acechando) return;
        if (!HorrorGameState.PuedeJugar) return;
        Tick();
    }

    /// <summary>Comportamiento propio de cada monstruo mientras acecha.</summary>
    protected abstract void Tick();

    /// <summary>Lo llama el AcechoDirector para que empiece a acechar.</summary>
    public virtual void Activar()
    {
        if (MuertoParaSiempre || EstadoActual == Estado.Desterrado || !EstaVivo) return;
        enVistazo = false;
        danioEncuentro = 0f; sustosEncuentro = 0;
        BuscarJugador();
        EstadoActual = Estado.Acechando;
        AlActivar();
    }

    protected virtual void AlActivar() { }

    bool enVistazo;

    /// <summary>
    /// Aparición psicológica: se deja ver un momento en un punto (entre los árboles, detrás de ti)
    /// y desaparece en cuanto lo miras de frente. No ataca.
    /// </summary>
    public IEnumerator Vistazo(Vector3 punto, float maximo)
    {
        if (EstadoActual != Estado.Inactivo || !BuscarJugador()) yield break;
        enVistazo = true;
        transform.position = Mundo.EnSuelo(punto);
        MirarJugador(1f);
        Mostrar(true);
        Animar("Idle", 0f);
        float t = 0f;
        while (t < maximo && enVistazo)
        {
            t += Time.deltaTime;
            if (Mundo.Visible(Centro, 0.3f, false)) { yield return new WaitForSeconds(0.15f); break; }
            yield return null;
        }
        if (enVistazo && EstadoActual == Estado.Inactivo)
        {
            VHSOverlay.Glitch(0.25f, 0.12f);
            Mostrar(false);
        }
        enVistazo = false;
    }

    /// <summary>Lo suelta la historia en un lugar concreto (la entrada de la capilla, junto a ti...).</summary>
    public virtual void ActivarEn(Vector3 punto)
    {
        Activar();
        if (EstadoActual != Estado.Acechando) return;
        Teletransportar(Mundo.EnSuelo(punto));
    }

    /// <summary>Termina su intento y se esconde.</summary>
    protected void Retirarse(bool conGlitch = true)
    {
        if (conGlitch && renderers.Length > 0 && renderers[0].enabled) VHSOverlay.Glitch(0.4f, 0.25f);
        Mostrar(false);
        EstadoActual = Estado.Inactivo;
        AlTerminar?.Invoke(this);
    }

    public virtual void Reiniciar()
    {
        if (MuertoParaSiempre) return;
        StopAllCoroutines();
        Revivir();
        Mostrar(false);
        EstadoActual = Estado.Inactivo;
    }

    // ------------------------------------------------------------------ Percepción

    protected bool BuscarJugador()
    {
        if (jugador != null) return true;
        movJugador = FindFirstObjectByType<PlayerMovement>();
        if (movJugador == null) return false;
        jugador = movJugador.transform;
        escJugador = movJugador.GetComponent<PlayerEscondite>();
        linterna = movJugador.GetComponent<FlashlightController>();
        return true;
    }

    protected float DistanciaJugador => jugador == null ? 999f : Vector3.Distance(transform.position, jugador.position);
    protected bool JugadorEscondido => escJugador != null && escJugador.Escondido;
    protected Vector3 Centro => transform.position + Vector3.up * 1.4f;

    /// <summary>¿La cámara lo está viendo?</summary>
    protected bool MeVe() => renderers.Length > 0 && renderers[0].enabled && Mundo.Visible(Centro, 0.15f);

    /// <summary>¿La linterna le está pegando de frente?</summary>
    protected bool MeAlumbra()
    {
        if (linterna == null || !linterna.Encendida || Camera.main == null) return false;
        Vector3 a = Centro - Camera.main.transform.position;
        return a.magnitude < 22f && Vector3.Angle(Camera.main.transform.forward, a) < 18f && MeVe();
    }

    // ------------------------------------------------------------------ Movimiento y visual

    protected void Mostrar(bool v)
    {
        foreach (Renderer r in renderers) if (r != null) r.enabled = v;
        foreach (Collider c in colliders) if (c != null) c.enabled = v;
        if (anim != null) anim.enabled = v;
    }

    protected void Teletransportar(Vector3 p, bool mirarJugador = true)
    {
        transform.position = Mundo.EnSuelo(p);
        if (mirarJugador) MirarJugador(1f);
        Mostrar(true);
    }

    protected void MirarJugador(float suavidad = 0.15f)
    {
        if (jugador == null) return;
        Vector3 d = jugador.position - transform.position; d.y = 0;
        if (d.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), suavidad);
    }

    protected void MoverHacia(Vector3 objetivo, float velocidad)
    {
        Vector3 d = objetivo - transform.position; d.y = 0;
        if (d.sqrMagnitude < 0.01f) return;
        Vector3 p = transform.position + d.normalized * velocidad * Time.deltaTime;
        transform.position = Mundo.EnSuelo(p);
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), 8f * Time.deltaTime);
    }

    protected void Animar(string estado, float fundido = 0.15f)
    {
        if (anim == null || !anim.enabled) return;
        int h = Animator.StringToHash(estado);
        if (anim.HasState(0, h)) anim.CrossFadeInFixedTime(h, fundido);
    }

    protected void Susurrar(float volumen = 0.6f)
    {
        voz.pitch = Random.Range(0.7f, 1f);
        voz.PlayOneShot(susurro, volumen);
    }

    // ------------------------------------------------------------------ Susto (screamer)

    protected bool PuedeAsustar => Time.time - ultimoSusto > enfriamientoSusto && !JugadorEscondido;

    protected void Asustar()
    {
        if (!PuedeAsustar) return;
        rutinaSusto = StartCoroutine(RutinaSusto());
    }

    protected virtual IEnumerator RutinaSusto()
    {
        ultimoSusto = Time.time;
        EstadoActual = Estado.Atacando;
        SensacionJuego.Sacudir(0.85f);
        Camera cam = Camera.main;

        // Se pone en tu cara
        if (cam != null)
        {
            Vector3 frente = cam.transform.forward; frente.y = 0; frente.Normalize();
            Vector3 p = cam.transform.position + frente * 0.9f;
            transform.position = new Vector3(p.x, cam.transform.position.y - 1.55f, p.z);
            transform.rotation = Quaternion.LookRotation(-frente);
        }
        Mostrar(true);
        Animar("Attack", 0.02f);
        voz.pitch = 1f;
        voz.spatialBlend = 0f;
        voz.PlayOneShot(grito, 1f);
        VHSOverlay.Glitch(1f, 0.9f);
        HorrorInput.Vibrar(1f, 1f, 0.6f, this);

        yield return new WaitForSeconds(0.55f);
        SaludJugador s = jugador != null ? jugador.GetComponent<SaludJugador>() : null;
        if (s != null) s.RecibirDanio(danioSusto, transform.position);
        yield return new WaitForSeconds(0.35f);

        voz.spatialBlend = 1f;
        VozProtagonista.Decir("susto");
        sustosEncuentro++;
        // Más interacción: la primera vez no se va. Retrocede, se queda viéndote y sigue cazándote.
        if (sustosEncuentro < 2 && cam != null && EstaVivo)
        {
            Vector3 atras = transform.position - cam.transform.forward * 0.1f + (transform.position - cam.transform.position).normalized * 4.5f;
            atras.y = transform.position.y;
            transform.position = Mundo.EnSuelo(atras);
            Animar("Idle", 0.1f);
            ultimoSusto = Time.time - enfriamientoSusto + 7f;   // el siguiente ataque en ~7 s
            EstadoActual = Estado.Acechando;
            AlRecuperarse();
        }
        else Retirarse();
    }

    /// <summary>Después de un golpe o un susto, si se queda: qué hace (el Asomado vuelve a correr hacia ti).</summary>
    protected virtual void AlRecuperarse() { }

    // ------------------------------------------------------------------ Golpes del jugador

    /// <summary>Solo le puedes pegar cuando está ahí (no a un fantasma escondido).</summary>
    protected override float ModificarDanio(float cantidad) => Activo ? cantidad : 0f;

    protected override void AlRecibirDanio(float cantidad, Vector3 origen)
    {
        if (EstadoActual == Estado.Desterrado) return;
        voz.PlayOneShot(golpeado, 0.9f);
        voz.PlayOneShot(grito, 0.35f);
        VHSOverlay.Glitch(0.6f, 0.3f);
        HorrorInput.Vibrar(0.5f, 0.3f, 0.15f, this);
        danioEncuentro += cantidad;
        if (EstaVivo) StartCoroutine(RutinaAturdido(origen));
    }

    /// <summary>Se tambalea hacia atrás y sigue. Solo huye si en este encuentro le quitaste casi la mitad de la vida.</summary>
    IEnumerator RutinaAturdido(Vector3 origen)
    {
        if (rutinaSusto != null) { StopCoroutine(rutinaSusto); rutinaSusto = null; voz.spatialBlend = 1f; }
        EstadoActual = Estado.Aturdido;
        Vector3 empuje = transform.position - origen; empuje.y = 0;
        empuje = empuje.sqrMagnitude > 0.01f ? empuje.normalized : -transform.forward;
        if (anim != null) anim.speed = 0.2f;
        Vector3 p0 = transform.position, p1 = Mundo.EnSuelo(p0 + empuje * 0.9f);
        for (float t = 0; t < 0.25f; t += Time.deltaTime) { transform.position = Vector3.Lerp(p0, p1, t / 0.25f); yield return null; }
        yield return new WaitForSeconds(0.45f);
        if (anim != null) anim.speed = 1f;
        if (danioEncuentro >= vidaMaxima * 0.45f)
        {
            DialogoUI.Susurrar("", nombre + " huyó entre los árboles... va a volver.", 2.5f);
            Susurrar(0.5f);
            Retirarse();
            yield break;
        }
        EstadoActual = Estado.Acechando;
        MirarJugador(1f);
        voz.PlayOneShot(grito, 0.5f);
        AlRecuperarse();
    }

    protected override void Morir()
    {
        StopAllCoroutines();
        bool final = LlavesBucle.Instancia != null && LlavesBucle.Instancia.Llaves >= LlavesBucle.Total;
        StartCoroutine(final ? RutinaMuerteFinal() : RutinaDesterrado());
    }

    IEnumerator RutinaDesterrado()
    {
        EstadoActual = Estado.Desterrado;
        Animar("Death", 0.05f);
        yield return CinematicaMuerte.Reproducir(this, false);
        Mostrar(false);
        AlTerminar?.Invoke(this);
        yield return new WaitForSeconds(tiempoDesterrado);
        Revivir();
        EstadoActual = Estado.Inactivo;
    }

    /// <summary>Con las 5 llaves el bucle se rompe y ellos también: esta vez no regresan.</summary>
    IEnumerator RutinaMuerteFinal()
    {
        MuertoParaSiempre = true;
        EstadoActual = Estado.Desterrado;
        Animar("Death", 0.05f);
        yield return CinematicaMuerte.Reproducir(this, true);
        Mostrar(false);
        AlTerminar?.Invoke(this);
    }

    /// <summary>Para la cinemática: centro del cuerpo.</summary>
    public Vector3 CentroPublico => Centro;
}
