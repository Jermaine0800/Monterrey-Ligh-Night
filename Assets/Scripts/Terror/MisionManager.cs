using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dirige la historia de "Monterrey Light Night" siguiendo el guion:
/// Prólogo (cinta recuperada, 1998) → Acto I (el claro y el saludo) →
/// Acto II (Túnel Línea K) → Acto III (el mirador y la señal perdida).
/// Cada acto es una clase que hereda de <see cref="CapituloBase"/> (polimorfismo):
/// aquí solo se recorren en orden y se ofrecen herramientas comunes.
/// </summary>
public class MisionManager : MonoBehaviour
{
    public static MisionManager Instancia { get; private set; }

    [Header("Referencias")]
    public GuiaController guia;
    public ItemDefinition llave;          // (ya no se usa en el guion; se deja para no romper escenas viejas)
    public Transform casa;

    [Header("Escenario del guion (lo llena el menú Monterrey/10)")]
    public Transform linternaEnSuelo;
    public Transform puntoSendero;        // donde el Guía saluda entre los árboles
    public TunelLineaK tunel;
    public Transform mirador;

    [Header("Ajustes")]
    public float distanciaHablar = 5.5f;
    public string escenaMenu = "MainMenu";
    public bool saltarPrologo = false;

    public Transform Jugador { get; private set; }
    public PlayerMovement Movimiento { get; private set; }
    public FlashlightController Linterna { get; private set; }
    public InventoryUI InventarioUI { get; private set; }
    public AcechoDirector Director => AcechoDirector.Instancia;
    public CapituloBase Actual { get; private set; }

    List<CapituloBase> capitulos;

    void Awake() { Instancia = this; }

    void Start() { StartCoroutine(Historia()); }

    bool Buscar()
    {
        if (Movimiento == null) { Movimiento = FindAnyObjectByType<PlayerMovement>(); if (Movimiento) Jugador = Movimiento.transform; }
        if (Linterna == null) Linterna = FindAnyObjectByType<FlashlightController>();
        if (InventarioUI == null) InventarioUI = FindAnyObjectByType<InventoryUI>();
        if (guia == null) guia = FindAnyObjectByType<GuiaController>();
        return Jugador != null && guia != null && Linterna != null;
    }

    IEnumerator Historia()
    {
        while (!Buscar()) yield return null;
        if (Director != null) Director.Habilitar(false);

        capitulos = new List<CapituloBase>
        {
            new CapPrologo(),
            new CapActoI(),
            new CapActoII(),
            new CapActoIII(),
        };

        foreach (CapituloBase c in capitulos)
        {
            if (c is CapPrologo && saltarPrologo) continue;
            Actual = c;
            Debug.Log("[Historia] " + c.Titulo);
            yield return c.Jugar(this);
        }
    }

    // ================================================================== Herramientas para los capítulos

    public void Objetivo(string texto)
    {
        if (!string.IsNullOrEmpty(texto)) DialogoUI.NuevoObjetivo(texto);
    }

    public float Distancia(Vector3 p)
    {
        Vector3 a = Jugador.position, b = p; a.y = b.y = 0;
        return Vector3.Distance(a, b);
    }

    /// <summary>Espera a que se cumpla algo. Mientras tanto el Vigía puede asomarse de lejos.</summary>
    public IEnumerator EsperarHasta(Func<bool> listo, bool vigiaObserva = false, Func<Vector3> haciaDonde = null)
    {
        float siguiente = Time.time + UnityEngine.Random.Range(25f, 40f);
        while (!listo())
        {
            if (vigiaObserva && Time.time > siguiente && HorrorGameState.PuedeJugar && !guia.Visible)
            {
                yield return Observar(haciaDonde != null ? haciaDonde() : Jugador.position + Jugador.forward * 25f);
                siguiente = Time.time + UnityEngine.Random.Range(30f, 50f);
            }
            yield return null;
        }
    }

    /// <summary>El Vigía aparece, espera a que te acerques, habla y se va cuando dejas de mirarlo.</summary>
    public IEnumerator EscenaVigia(Vector3 punto, GuiaController.Pose pose, string[] lineas, bool caminaHaciaTi = false)
    {
        guia.Aparecer(punto, Jugador.position, pose);
        if (caminaHaciaTi) guia.CaminarHacia(Jugador.position + (punto - Jugador.position).normalized * 3.5f);

        float espera = 0f, ultimoLlamado = 0f;
        while (Vector3.Distance(Jugador.position, guia.transform.position) > distanciaHablar)
        {
            espera += Time.deltaTime;
            if (espera - ultimoLlamado > 12f)
            {
                ultimoLlamado = espera;
                string[] llamados = { "Por aquí.", "No me hagas ir por ti.", "Frío, frío... más frío.", "Te estoy viendo. Ven." };
                DialogoUI.Susurrar(guia.nombre, llamados[UnityEngine.Random.Range(0, llamados.Length)]);
            }
            if (Vector3.Distance(Jugador.position, guia.transform.position) > 45f && !guia.JugadorMeVe())
                guia.Aparecer(Adelante(12f, 40f), Jugador.position, GuiaController.Pose.Idle);
            yield return null;
        }

        guia.Detener();
        yield return DialogoUI.Instancia.Conversar(guia, lineas);

        float t = 0f;
        while (t < 6f && guia.JugadorMeVe()) { t += Time.deltaTime; yield return null; }
        guia.Desaparecer();
    }

    IEnumerator Observar(Vector3 centro)
    {
        guia.Aparecer(JuntoAArbol(centro, 0f, 15f), Jugador.position,
            UnityEngine.Random.value < 0.5f ? GuiaController.Pose.Sentado : GuiaController.Pose.Idle);
        string[] comentarios = { "Sigue. Yo aquí nomás.", "Por ahí no.", "¿Oíste eso? Yo tampoco.", "Vas bien. Creo.", "No voltees." };
        DialogoUI.Susurrar(guia.nombre, comentarios[UnityEngine.Random.Range(0, comentarios.Length)]);

        float t = 0f, mirado = 0f;
        while (t < 14f)
        {
            t += Time.deltaTime;
            if (guia.JugadorMeVe()) mirado += Time.deltaTime;
            if (mirado > 2.5f || Vector3.Distance(Jugador.position, guia.transform.position) < 10f) break;
            yield return null;
        }
        guia.Desaparecer();
    }

    /// <summary>Un punto frente a la cámara a cierta distancia (con desvío lateral aleatorio).</summary>
    public Vector3 Adelante(float distancia, float desvioGrados)
    {
        Transform cam = Camera.main != null ? Camera.main.transform : Jugador;
        Vector3 f = cam.forward; f.y = 0;
        if (f.sqrMagnitude < 0.01f) f = Jugador.forward;
        f = Quaternion.Euler(0, UnityEngine.Random.Range(-desvioGrados, desvioGrados), 0) * f.normalized;
        return Mundo.EnSuelo(Jugador.position + f * distancia);
    }

    /// <summary>Un punto al pie de un árbol entre min y max metros del centro.</summary>
    public Vector3 JuntoAArbol(Vector3 centro, float min, float max)
    {
        List<Vector3> arboles = Mundo.ArbolesCerca(centro, min, max, 40);
        if (arboles.Count > 0)
        {
            Vector3 arbol = arboles[UnityEngine.Random.Range(0, arboles.Count)];
            Vector3 haciaJugador = Jugador.position - arbol; haciaJugador.y = 0;
            return Mundo.EnSuelo(arbol + haciaJugador.normalized * 0.8f);
        }
        Vector2 r = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(Mathf.Max(min, 4f), Mathf.Max(max, 6f));
        return Mundo.EnSuelo(centro + new Vector3(r.x, 0, r.y));
    }
}

/// <summary>Parpadeo suave de una luz (ánimas, monitores, lámparas).</summary>
public class Parpadeo : MonoBehaviour
{
    Light l; float baseI; float semilla;
    void Start() { l = GetComponent<Light>(); baseI = l != null ? l.intensity : 1f; semilla = UnityEngine.Random.value * 100f; }
    void Update() { if (l) l.intensity = baseI * (0.6f + 0.4f * Mathf.PerlinNoise(semilla, Time.time * 2f)); }
}

/// <summary>Parpadeo nervioso de encendido/apagado (linterna tirada, lámparas de túnel fallando, baliza).</summary>
public class ParpadeoFuerte : MonoBehaviour
{
    public float encendido = 0.35f, apagado = 0.25f;
    public bool regular = false;
    Light l; float baseI, siguiente; bool on = true;
    void Start() { l = GetComponent<Light>(); baseI = l != null ? l.intensity : 1f; }
    void Update()
    {
        if (l == null || Time.time < siguiente) return;
        on = !on;
        l.intensity = on ? baseI : 0f;
        float d = on ? encendido : apagado;
        siguiente = Time.time + (regular ? d : d * UnityEngine.Random.Range(0.2f, 1.8f));
    }
}
