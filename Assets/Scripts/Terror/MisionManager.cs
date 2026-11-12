using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Historia de "Monterrey Light Night": el jugador entra a la zona embrujada del Cerro de la Silla.
/// El Vigía aparece en cada capítulo, habla, deja un objetivo y se desvanece.
/// Entre capítulos te sigue de lejos: sentado junto a un árbol, mirándote.
/// </summary>
public class MisionManager : MonoBehaviour
{
    [Header("Referencias")]
    public GuiaController guia;
    public ItemDefinition llave;          // Item_Llave.asset
    public Transform casa;               // House_Prefab

    [Header("Ajustes")]
    public float distanciaHablar = 5.5f;
    public float distanciaLlave = 70f;
    public float distanciaLlegarCasa = 14f;
    public string escenaMenu = "MainMenu";

    Transform jugador;
    PlayerInventory inventario;
    InventoryUI inventarioUI;
    FlashlightController linterna;
    GameObject llaveEnMundo;
    Vector3 puntoLlave;

    class Capitulo
    {
        public string objetivo;
        public GuiaController.Pose pose;
        public bool caminaHaciaTi;
        public Func<Vector3> donde;
        public string[] lineas;
        public Func<bool> completo;
        public Action alTerminarDialogo;
    }

    List<Capitulo> capitulos;

    void Start()
    {
        StartCoroutine(Historia());
    }

    bool Buscar()
    {
        if (jugador == null) { var pm = FindFirstObjectByType<PlayerMovement>(); if (pm) jugador = pm.transform; }
        if (inventario == null) inventario = FindFirstObjectByType<PlayerInventory>();
        if (inventarioUI == null) inventarioUI = FindFirstObjectByType<InventoryUI>();
        if (linterna == null) linterna = FindFirstObjectByType<FlashlightController>();
        if (guia == null) guia = FindFirstObjectByType<GuiaController>();
        return jugador != null && guia != null;
    }

    // ------------------------------------------------------------------ Guion

    void ArmarCapitulos()
    {
        capitulos = new List<Capitulo>
        {
            new Capitulo {
                pose = GuiaController.Pose.Sentado,
                donde = () => JuntoAArbol(jugador.position, 7f, 16f),
                lineas = new[] {
                    "Sentado|Mira nada más. Sobreviviste al choque. Eso no le pasa a cualquiera.",
                    "Hablar|A tus papás, por ejemplo, no les pasó. Uy. ¿Muy pronto? Perdón. No es cierto, no me arrepiento.",
                    "Hablar|No te asustes. Bueno... sí, asústate. Es más divertido para mí.",
                    "Senalar|Soy el Vigía. Cuido este lado de la Silla. Tú nada más vas de paso. Eso espero.",
                    "Reir|Esa mochila no es tuya. Ahora sí. Ve por ella, yo te espero. Siempre espero.",
                },
                objetivo = "Recoge la mochila",
                completo = () => inventarioUI != null && inventarioUI.hasBackPack,
            },
            new Capitulo {
                pose = GuiaController.Pose.Saludar,
                caminaHaciaTi = true,
                donde = () => Adelante(14f, 35f),
                lineas = new[] {
                    "Saludar|¡Bravo! Sabes levantar cosas del suelo. Me impresionas. Casi.",
                    "Hablar|Las ánimas salen cuando se va la luz. Y aquí la luz se va muy seguido.",
                    "Senalar|Toma la linterna. Se prende con X. Sí, yo también veo tus botones.",
                },
                objetivo = "Toma la linterna  [RB]  y préndela  [X]",
                completo = () => linterna != null && linterna.TieneLinterna,
            },
            new Capitulo {
                pose = GuiaController.Pose.Intimidar,
                donde = () => Adelante(7f, 0f),
                lineas = new[] {
                    "Intimidar|¿Sentiste eso? Fue alguien pasando a través de ti. Qué grosero, ¿verdad?",
                    "Hablar|Allá arriba hay una cabaña. Está cerrada. Las cosas cerradas guardan cosas que no quieren salir.",
                    "Senalar|Busca la llave. Está donde la dejaron los que no regresaron.",
                    "Reir|Tranquilo. Casi todos regresan. En partes.",
                },
                objetivo = "Encuentra la llave  (sigue la luz dorada)",
                alTerminarDialogo = CrearLlave,
                completo = TieneLlave,
            },
            new Capitulo {
                pose = GuiaController.Pose.Sentado,
                donde = () => JuntoAArbol(jugador.position, 6f, 14f),
                lineas = new[] {
                    "Sentado|La encontraste. Ugh. Yo había apostado a que no.",
                    "Hablar|Ahora camina hacia la cabaña. No te salgas del camino. No hay camino, pero no te salgas.",
                    "Intimidar|Y si alguien te llama por tu nombre, no contestes. Yo nunca te dije tu nombre. ¿Cómo lo sabrían?",
                },
                objetivo = "Llega a la cabaña",
                completo = () => casa != null && Vector3.Distance(jugador.position, casa.position) < distanciaLlegarCasa,
            },
            new Capitulo {
                pose = GuiaController.Pose.Saludar,
                donde = () => casa != null ? casa.position + (jugador.position - casa.position).normalized * 6f : Adelante(6f, 0f),
                lineas = new[] {
                    "Saludar|Llegaste. Te dije que te iba a esperar.",
                    "Hablar|¿Sabes qué es lo bonito de las cabañas? Que tienen una sola puerta.",
                    "Reir|Ya no hay mochila que cargar ni linterna que prender. Solo tú, yo, y toda la noche.",
                    "Senalar|Adentro te espera alguien. Le dicen el Arquitecto. Él construyó todo esto... incluso lo que traes en la cabeza.",
                    "Intimidar|Bienvenido a la Silla. Siéntate. Te vas a quedar un buen rato.",
                },
                objetivo = "",
                completo = () => true,
            },
        };
    }

    // ------------------------------------------------------------------ Flujo

    IEnumerator Historia()
    {
        while (!Buscar()) yield return null;
        ArmarCapitulos();
        yield return new WaitForSeconds(3f);

        for (int i = 0; i < capitulos.Count; i++)
        {
            Capitulo cap = capitulos[i];

            // 1. Aparece
            Vector3 punto = cap.donde();
            guia.Aparecer(punto, jugador.position, cap.pose);
            if (cap.caminaHaciaTi)
            {
                Vector3 hacia = jugador.position + (punto - jugador.position).normalized * 3.5f;
                guia.CaminarHacia(hacia);
            }

            // 2. Espera a que estés cerca (si tardas, te llama)
            float espera = 0f, ultimoLlamado = 0f;
            while (Vector3.Distance(jugador.position, guia.transform.position) > distanciaHablar)
            {
                espera += Time.deltaTime;
                if (espera - ultimoLlamado > 12f)
                {
                    ultimoLlamado = espera;
                    string[] llamados = { "Por aquí.", "No me hagas ir por ti.", "Frío, frío... más frío.", "Te estoy viendo. Ven." };
                    DialogoUI.Susurrar(guia.nombre, llamados[UnityEngine.Random.Range(0, llamados.Length)]);
                }
                // Si te alejas mucho, reaparece más cerca (y tú no lo viste moverse)
                if (Vector3.Distance(jugador.position, guia.transform.position) > 45f && !guia.JugadorMeVe())
                    guia.Aparecer(Adelante(12f, 40f), jugador.position, GuiaController.Pose.Idle);
                yield return null;
            }

            // 3. Habla
            guia.Detener();
            yield return DialogoUI.Instancia.Conversar(guia, cap.lineas);
            cap.alTerminarDialogo?.Invoke();
            if (!string.IsNullOrEmpty(cap.objetivo)) DialogoUI.NuevoObjetivo(cap.objetivo);

            if (i == capitulos.Count - 1) break;

            // 4. Se va cuando dejas de mirarlo (o a los 6 s)
            float t = 0f;
            while (t < 6f && guia.JugadorMeVe()) { t += Time.deltaTime; yield return null; }
            guia.Desaparecer();

            // 5. Mientras cumples el objetivo te observa de lejos
            float siguienteObservacion = Time.time + UnityEngine.Random.Range(20f, 35f);
            while (!cap.completo())
            {
                if (Time.time > siguienteObservacion && HorrorGameState.PuedeJugar)
                {
                    yield return Observar();
                    siguienteObservacion = Time.time + UnityEngine.Random.Range(25f, 45f);
                }
                yield return null;
            }
            yield return new WaitForSeconds(1.5f);
        }

        yield return Final();
    }

    IEnumerator Observar()
    {
        Vector3 haciaObjetivo = DireccionObjetivo();
        Vector3 centro = jugador.position + haciaObjetivo * 28f;
        Vector3 p = JuntoAArbol(centro, 0f, 15f);
        guia.Aparecer(p, jugador.position, UnityEngine.Random.value < 0.5f ? GuiaController.Pose.Sentado : GuiaController.Pose.Idle);

        string[] comentarios = { "Sigue. Yo aquí nomás.", "Por ahí no.", "¿Oíste eso? Yo tampoco.", "Vas bien. Creo.", "No voltees." };
        DialogoUI.Susurrar(guia.nombre, comentarios[UnityEngine.Random.Range(0, comentarios.Length)]);

        float t = 0f, mirado = 0f;
        while (t < 14f)
        {
            t += Time.deltaTime;
            if (guia.JugadorMeVe()) mirado += Time.deltaTime;
            if (mirado > 2.5f || Vector3.Distance(jugador.position, guia.transform.position) < 10f) break;
            yield return null;
        }
        guia.Desaparecer();
    }

    IEnumerator Final()
    {
        HorrorGameState.modo = HorrorGameState.Modo.Final;
        VHSOverlay.Glitch(1f, 3f);
        yield return new WaitForSeconds(1.5f);

        Canvas c = HorrorUI.CrearCanvas("Final_Canvas", 600, transform);
        Image negro = HorrorUI.CrearImagen(c.transform, "Negro", new Color(0, 0, 0, 0));
        HorrorUI.Estirar(negro.rectTransform);
        Text fin = HorrorUI.CrearTexto(c.transform, "Fin", "F I N   D E   L A   C I N T A   0 7", 48, new Color(1, 1, 1, 0), TextAnchor.MiddleCenter);
        HorrorUI.Estirar(fin.rectTransform);
        Text sub = HorrorUI.CrearTexto(c.transform, "Sub", "no grabar encima", 26, new Color(1, 1, 1, 0), TextAnchor.MiddleCenter, FontStyle.Italic);
        HorrorUI.Colocar(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -80), new Vector2(800, 40));

        for (float t = 0; t < 2.5f; t += Time.deltaTime)
        {
            negro.color = new Color(0, 0, 0, t / 2.5f);
            yield return null;
        }
        for (float t = 0; t < 2f; t += Time.deltaTime)
        {
            fin.color = new Color(HorrorUI.Hueso.r, HorrorUI.Hueso.g, HorrorUI.Hueso.b, t / 2f);
            sub.color = new Color(HorrorUI.Sangre.r, HorrorUI.Sangre.g, HorrorUI.Sangre.b, t / 2f);
            yield return null;
        }
        yield return new WaitForSeconds(4f);
        PlayerPrefs.SetInt("partida_guardada", 0);
        if (Application.CanStreamedLevelBeLoaded(escenaMenu)) SceneManager.LoadScene(escenaMenu);
    }

    // ------------------------------------------------------------------ Llave

    void CrearLlave()
    {
        if (llaveEnMundo != null) return;
        Vector3 dir = casa != null ? (casa.position - jugador.position) : jugador.forward;
        dir.y = 0; dir.Normalize();
        dir = Quaternion.Euler(0, UnityEngine.Random.Range(-20f, 20f), 0) * dir;
        puntoLlave = jugador.position + dir * distanciaLlave;
        puntoLlave.y = GuiaController.AlturaSuelo(puntoLlave) + 0.15f;

        llaveEnMundo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        llaveEnMundo.name = "Llave_Pickup";
        llaveEnMundo.transform.position = puntoLlave;
        llaveEnMundo.transform.localScale = new Vector3(0.08f, 0.03f, 0.22f);
        Destroy(llaveEnMundo.GetComponent<BoxCollider>());
        SphereCollider sc = llaveEnMundo.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 6f; // escala pequeña: radio real ~0.5 m

        GameObject luz = new GameObject("BrilloLlave");
        luz.transform.SetParent(llaveEnMundo.transform, false);
        luz.transform.localPosition = new Vector3(0, 10f, 0);
        Light l = luz.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.75f, 0.3f);
        l.range = 5f;
        l.intensity = 2.5f;
        luz.AddComponent<Parpadeo>();

        ItemPickup pick = llaveEnMundo.AddComponent<ItemPickup>();
        pick.item = llave;
        pick.esObjetoMano = false;
    }

    bool TieneLlave()
    {
        if (inventario == null) return false;
        foreach (ItemDefinition it in inventario.objetos)
            if (it != null && (it == llave || (it.nombre != null && it.nombre.ToLower().Contains("llave")))) return true;
        return false;
    }

    // ------------------------------------------------------------------ Puntos en el mundo

    Vector3 DireccionObjetivo()
    {
        Vector3 d;
        if (llaveEnMundo != null) d = llaveEnMundo.transform.position - jugador.position;
        else if (casa != null) d = casa.position - jugador.position;
        else d = jugador.forward;
        d.y = 0;
        return d.sqrMagnitude > 0.01f ? d.normalized : jugador.forward;
    }

    /// <summary>Un punto frente a la cámara a cierta distancia (con desvío lateral aleatorio).</summary>
    Vector3 Adelante(float distancia, float desvioGrados)
    {
        Transform cam = Camera.main != null ? Camera.main.transform : jugador;
        Vector3 f = cam.forward; f.y = 0;
        if (f.sqrMagnitude < 0.01f) f = jugador.forward;
        f = Quaternion.Euler(0, UnityEngine.Random.Range(-desvioGrados, desvioGrados), 0) * f.normalized;
        Vector3 p = jugador.position + f * distancia;
        p.y = GuiaController.AlturaSuelo(p);
        return p;
    }

    /// <summary>Busca un árbol del terreno entre min y max metros del centro y regresa un punto al pie del tronco.</summary>
    Vector3 JuntoAArbol(Vector3 centro, float min, float max)
    {
        Terrain t = Terrain.activeTerrain;
        if (t != null && t.terrainData != null)
        {
            TreeInstance[] arboles = t.terrainData.treeInstances;
            Vector3 tam = t.terrainData.size;
            Vector3 origen = t.transform.position;
            List<Vector3> candidatos = new List<Vector3>();
            for (int i = 0; i < arboles.Length; i++)
            {
                Vector3 w = origen + Vector3.Scale(arboles[i].position, tam);
                float d = Vector3.Distance(new Vector3(w.x, 0, w.z), new Vector3(centro.x, 0, centro.z));
                if (d >= min && d <= max) candidatos.Add(w);
                if (candidatos.Count > 40) break;
            }
            if (candidatos.Count > 0)
            {
                Vector3 arbol = candidatos[UnityEngine.Random.Range(0, candidatos.Count)];
                Vector3 haciaJugador = jugador.position - arbol; haciaJugador.y = 0;
                Vector3 p = arbol + haciaJugador.normalized * 0.8f;
                p.y = GuiaController.AlturaSuelo(p);
                return p;
            }
        }
        // Sin árboles cerca: un punto al azar en ese rango
        Vector2 r = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(Mathf.Max(min, 4f), Mathf.Max(max, 6f));
        Vector3 q = centro + new Vector3(r.x, 0, r.y);
        q.y = GuiaController.AlturaSuelo(q);
        return q;
    }
}

/// <summary>Parpadeo suave de una luz (llave, ánimas).</summary>
public class Parpadeo : MonoBehaviour
{
    Light l; float baseI; float semilla;
    void Start() { l = GetComponent<Light>(); baseI = l != null ? l.intensity : 1f; semilla = UnityEngine.Random.value * 100f; }
    void Update() { if (l) l.intensity = baseI * (0.6f + 0.4f * Mathf.PerlinNoise(semilla, Time.time * 2f)); }
}
