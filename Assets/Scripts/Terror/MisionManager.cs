using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("Las 5 llaves (lo llena el menú Monterrey/11)")]
    public SitioLlave[] sitios;
    public PuertaBucle puertaBucle;

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
            new CapBusqueda(),
            new CapFinal(),
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
    public IEnumerator EscenaVigia(Vector3 punto, GuiaController.Pose pose, string[] lineas, bool caminaHaciaTi = false, float distancia = -1f)
    {
        if (distancia <= 0f) distancia = distanciaHablar;
        guia.Aparecer(punto, Jugador.position, pose);
        if (caminaHaciaTi) guia.CaminarHacia(Jugador.position + (punto - Jugador.position).normalized * 3.5f);

        float espera = 0f, ultimoLlamado = 0f;
        while (Vector3.Distance(Jugador.position, guia.transform.position) > distancia)
        {
            espera += Time.deltaTime;
            if (espera - ultimoLlamado > 12f)
            {
                ultimoLlamado = espera;
                string[] llamados = { "Por aquí.", "No me hagas ir por ti.", "Frío, frío... más frío.", "Te estoy viendo. Ven." };
                DialogoUI.Susurrar(guia.nombre, llamados[UnityEngine.Random.Range(0, llamados.Length)]);
            }
            if (Vector3.Distance(Jugador.position, guia.transform.position) > Mathf.Max(45f, distancia + 10f) && !guia.JugadorMeVe())
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

    // ================================================================== Cinemáticas (en el motor)

    /// <summary>Barras negras de cine + texto. Devuelve el canvas para destruirlo al final.</summary>
    Canvas BarrasCine(string titulo, string subtitulo)
    {
        Canvas c = HorrorUI.CrearCanvas("Cinematica_Canvas", 640);
        for (int i = 0; i < 2; i++)
        {
            Image b = HorrorUI.CrearImagen(c.transform, "Barra", Color.black);
            b.rectTransform.anchorMin = new Vector2(0, i == 0 ? 0 : 0.88f);
            b.rectTransform.anchorMax = new Vector2(1, i == 0 ? 0.12f : 1);
            b.rectTransform.offsetMin = b.rectTransform.offsetMax = Vector2.zero;
        }
        Text t = HorrorUI.CrearTexto(c.transform, "Titulo", titulo, 40, HorrorUI.Hueso, TextAnchor.MiddleCenter);
        t.rectTransform.anchorMin = new Vector2(0, 0.02f); t.rectTransform.anchorMax = new Vector2(1, 0.1f);
        t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
        Text s = HorrorUI.CrearTexto(c.transform, "Sub", subtitulo, 22, HorrorUI.HuesoTenue, TextAnchor.MiddleCenter);
        s.rectTransform.anchorMin = new Vector2(0, 0.9f); s.rectTransform.anchorMax = new Vector2(1, 0.97f);
        s.rectTransform.offsetMin = s.rectTransform.offsetMax = Vector2.zero;
        return c;
    }

    /// <summary>Mueve la cámara de A a B mirando a un punto (el jugador no se puede mover mientras).</summary>
    public IEnumerator Toma(Vector3 desde, Vector3 hasta, Vector3 mirar, float segundos, string titulo, string sub)
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;
        CameraController cc = FindAnyObjectByType<CameraController>();
        HorrorGameState.Modo previo = HorrorGameState.modo;
        HorrorGameState.modo = HorrorGameState.Modo.Dialogo;
        if (cc != null) cc.enabled = false;
        Vector3 p0 = cam.transform.position; Quaternion r0 = cam.transform.rotation;
        Canvas c = BarrasCine(titulo, sub);
        VHSOverlay.Glitch(0.8f, 0.4f);
        for (float t = 0; t < segundos; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / segundos);
            cam.transform.position = Vector3.Lerp(desde, hasta, k);
            cam.transform.rotation = Quaternion.LookRotation(mirar - cam.transform.position);
            if (HorrorInput.Aceptar && t > 0.8f) break;     // se puede saltar
            yield return null;
        }
        VHSOverlay.Glitch(0.8f, 0.4f);
        Destroy(c.gameObject);
        cam.transform.SetPositionAndRotation(p0, r0);
        if (cc != null) cc.enabled = true;
        HorrorGameState.modo = previo == HorrorGameState.Modo.Dialogo ? HorrorGameState.Modo.Jugando : previo;
    }

    /// <summary>Al empezar: la cámara baja desde el cerro, entre los árboles, hasta donde despiertas.</summary>
    public IEnumerator CinematicaIntro()
    {
        Vector3 objetivo = Jugador.position + Vector3.up * 1f;
        Vector3 dir = mirador != null ? (mirador.position - Jugador.position) : Jugador.forward * 100f; dir.y = 0; dir.Normalize();
        Vector3 desde = Jugador.position + dir * 70f + Vector3.up * 45f;
        Vector3 hasta = Jugador.position + dir * 5f + Vector3.up * 2.2f + Vector3.Cross(Vector3.up, dir) * 2f;
        yield return Toma(desde, hasta, objetivo, 9f, "M O N T E R R E Y   L I G H T   N I G H T", "Cerro de la Silla  ·  Sector La Pastora  ·  27/11/1998");
    }

    /// <summary>
    /// Después de la cinta: despiertas noqueado en el piso del claro. Parpadeas, ves el cielo,
    /// y te levantas despacio. (El modelo se acuesta girándolo; no hace falta animación extra.)
    /// </summary>
    public IEnumerator Despertar()
    {
        Camera cam = Camera.main;
        if (cam == null || Jugador == null) yield break;
        CameraController cc = FindAnyObjectByType<CameraController>();
        HorrorGameState.modo = HorrorGameState.Modo.Dialogo;
        if (cc != null) cc.enabled = false;
        Animator an = Jugador.GetComponentInChildren<Animator>();
        Transform modelo = an != null && an.transform != Jugador ? an.transform : null;
        Quaternion r0 = modelo != null ? modelo.localRotation : Quaternion.identity;
        Vector3 p0 = modelo != null ? modelo.localPosition : Vector3.zero;
        Vector3 pc0 = cam.transform.position; Quaternion rc0 = cam.transform.rotation;

        // Acostado boca arriba: la cabeza queda hacia atrás
        if (modelo != null) { modelo.localRotation = Quaternion.Euler(-90f, 0f, 0f) * r0; modelo.localPosition = p0 + Vector3.up * 0.15f; }
        Vector3 cabeza = Jugador.position - Jugador.forward * 1.55f + Vector3.up * 0.25f;

        // Párpados
        Canvas c = HorrorUI.CrearCanvas("Despertar_Canvas", 440);
        Image ojos = HorrorUI.CrearImagen(c.transform, "Parpados", Color.black);
        HorrorUI.Estirar(ojos.rectTransform);
        Text t = HorrorUI.CrearTexto(c.transform, "Texto", "", 30, HorrorUI.Hueso, TextAnchor.LowerCenter);
        HorrorUI.Colocar(t.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 120), new Vector2(1400, 80));
        t.supportRichText = true;

        // 1) Mirando el cielo entre los árboles, desde el piso
        Vector3 arriba = cabeza + Vector3.up * 0.15f;
        float[] alfas = { 1f, 0.55f, 1f, 0.35f, 0.9f, 0.15f, 0f };
        float[] tiempos = { 1.2f, 0.8f, 0.35f, 0.9f, 0.3f, 1.2f, 0.8f };
        VHSOverlay.Glitch(0.6f, 0.8f);
        for (int i = 0; i < alfas.Length; i++)
        {
            float a0 = ojos.color.a;
            for (float k = 0; k < tiempos[i]; k += Time.deltaTime)
            {
                float u = k / tiempos[i];
                ojos.color = new Color(0, 0, 0, Mathf.Lerp(a0, alfas[i], u));
                cam.transform.position = arriba;
                float bamboleo = Mathf.Sin(Time.time * 0.7f) * 6f;
                cam.transform.rotation = Quaternion.LookRotation(Vector3.up * 3f + Jugador.forward + Jugador.right * Mathf.Sin(Time.time * 0.4f) * 0.4f, Jugador.forward) * Quaternion.Euler(0, 0, bamboleo);
                yield return null;
            }
            if (i == 2) t.text = "<i>...¿qué... pasó?</i>";
            if (i == 4) t.text = "<i>Mario... Chuy...</i>";
        }
        t.supportRichText = true;
        t.text = "";

        // 2) Te levantas: el cuerpo se endereza y la cámara pasa detrás del hombro
        Vector3 detras = Jugador.position - Jugador.forward * 2.6f + Vector3.up * 1.75f + Jugador.right * 0.6f;
        float dur = 2.6f;
        for (float k = 0; k < dur; k += Time.deltaTime)
        {
            float u = Mathf.SmoothStep(0f, 1f, k / dur);
            if (modelo != null)
            {
                modelo.localRotation = Quaternion.Slerp(Quaternion.Euler(-90f, 0f, 0f) * r0, r0, u);
                modelo.localPosition = Vector3.Lerp(p0 + Vector3.up * 0.15f, p0, u);
            }
            Vector3 pos = Vector3.Lerp(arriba + Vector3.up * 0.3f, detras, u);
            Vector3 mirar = Vector3.Lerp(cabeza + Vector3.up * 4f, Jugador.position + Vector3.up * 1.2f + Jugador.forward * 3f, u);
            cam.transform.position = pos;
            cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, Quaternion.LookRotation(mirar - pos), 6f * Time.deltaTime);
            yield return null;
        }
        if (modelo != null) { modelo.localRotation = r0; modelo.localPosition = p0; }
        Destroy(c.gameObject);
        cam.transform.SetPositionAndRotation(pc0, rc0);
        if (cc != null) cc.enabled = true;
        HorrorGameState.modo = HorrorGameState.Modo.Jugando;
    }

    /// <summary>Antes de cada lugar: un vistazo de hacia dónde hay que ir.</summary>
    public IEnumerator CinematicaLugar(Transform lugar, string nombre)
    {
        if (lugar == null) yield break;
        Vector3 c = lugar.position + Vector3.up * 1.5f;
        Vector3 dir = lugar.position - Jugador.position; dir.y = 0; dir.Normalize();
        Vector3 lado = Vector3.Cross(Vector3.up, dir);
        int n = LlavesBucle.Instancia != null ? LlavesBucle.Instancia.Llaves + 1 : 1;
        yield return Toma(c - dir * 26f + lado * 10f + Vector3.up * 14f, c - dir * 14f - lado * 6f + Vector3.up * 5f, c, 4.5f,
            nombre.ToUpper(), "LLAVE " + n + " DE " + LlavesBucle.Total);
    }
}
