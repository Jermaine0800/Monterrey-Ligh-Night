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
    public float danioSusto = 34f;
    public float enfriamientoSusto = 25f;

    [Header("Destierro")]
    [Tooltip("Segundos que desaparece cuando lo derrotas a golpes")]
    public float tiempoDesterrado = 90f;

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
        if (EstadoActual == Estado.Desterrado || !EstaVivo) return;
        BuscarJugador();
        EstadoActual = Estado.Acechando;
        AlActivar();
    }

    protected virtual void AlActivar() { }

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
        Retirarse();
    }

    // ------------------------------------------------------------------ Golpes del jugador

    /// <summary>Solo le puedes pegar cuando está ahí (no a un fantasma escondido).</summary>
    protected override float ModificarDanio(float cantidad) => Activo ? cantidad : 0f;

    protected override void AlRecibirDanio(float cantidad, Vector3 origen)
    {
        if (!Activo) return;
        voz.PlayOneShot(golpeado, 0.9f);
        voz.PlayOneShot(grito, 0.35f);
        VHSOverlay.Glitch(0.6f, 0.3f);
        if (EstaVivo) StartCoroutine(RutinaAturdido());
    }

    IEnumerator RutinaAturdido()
    {
        if (rutinaSusto != null) { StopCoroutine(rutinaSusto); rutinaSusto = null; voz.spatialBlend = 1f; }
        EstadoActual = Estado.Aturdido;
        Animar("Death", 0.05f);
        yield return new WaitForSeconds(0.6f);
        Retirarse();
    }

    protected override void Morir()
    {
        StopAllCoroutines();
        StartCoroutine(RutinaDesterrado());
    }

    IEnumerator RutinaDesterrado()
    {
        EstadoActual = Estado.Desterrado;
        Animar("Death", 0.05f);
        DialogoUI.Susurrar("", nombre + " se desvaneció... por ahora.", 3f);
        yield return new WaitForSeconds(1.2f);
        Mostrar(false);
        AlTerminar?.Invoke(this);
        yield return new WaitForSeconds(tiempoDesterrado);
        Revivir();
        EstadoActual = Estado.Inactivo;
    }
}
