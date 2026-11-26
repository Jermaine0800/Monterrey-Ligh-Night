using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// LÍNEA K por dentro: un vagón de metro abandonado junto al andén (la llave 3 está adentro),
/// dos cajas de fusibles para prender las luces del túnel, y la pelea contra La Sonriente:
/// al entrar al vagón todo se cierra y no puedes salir hasta lastimarla lo suficiente.
/// Con la luz prendida le haces mucho más daño (0 cajas = 35%, 1 caja = 100%, 2 cajas = 200%).
/// </summary>
public class MetroTunel : MonoBehaviour
{
    public static MetroTunel Instancia { get; private set; }
    public static bool PeleaActiva => Instancia != null && Instancia.pelea;

    TunelLineaK tunel;
    Transform I;
    CajaFusibles[] cajas;
    Light[] lucesVagon;
    bool pelea, vencida;
    Vector3 puntoLlave;

    // ------------------------------------------------------------------ jefe
    LaSonriente jefe;
    Transform jefeT;
    Animator jefeAnim;
    float danioHecho;
    const float DanioNecesario = 240f;
    float siguienteAtaque, aturdida;
    Canvas barraCanvas; Image barraRelleno; Text barraTexto;

    public static MetroTunel Crear(TunelLineaK t)
    {
        MetroTunel m = t.gameObject.AddComponent<MetroTunel>();
        m.tunel = t; m.I = t.interior;
        Instancia = m;
        m.Construir();
        return m;
    }

    public CajaFusibles[] Cajas => cajas;
    public bool Vencida => vencida;
    public float DanioHecho => danioHecho;

    public int CajasPrendidas { get { int n = 0; if (cajas != null) foreach (CajaFusibles c in cajas) if (c != null && c.Prendida) n++; return n; } }
    float Multiplicador => CajasPrendidas == 0 ? 0.35f : (CajasPrendidas == 1 ? 1f : 2f);

    // ================================================================== Construcción
    static Material Mat(Color c, bool brilla = false)
    {
        Shader sh = Shader.Find(brilla ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/Unlit");
        Material m = new Material(sh);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        m.color = c; return m;
    }

    Transform Caja(string n, Vector3 local, Vector3 tam, Material m, bool choque = true)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = n; g.transform.SetParent(I, false);
        g.transform.localPosition = local; g.transform.localScale = tam;
        g.GetComponent<Renderer>().sharedMaterial = m;
        if (!choque) Destroy(g.GetComponent<Collider>());
        return g.transform;
    }

    void Construir()
    {
        if (I == null) return;
        Material naranja = Mat(new Color(0.75f, 0.38f, 0.12f)), blanco = Mat(new Color(0.82f, 0.8f, 0.72f));
        Material vidrio = Mat(new Color(0.08f, 0.1f, 0.12f)), piso = Mat(new Color(0.22f, 0.2f, 0.18f)), asiento = Mat(new Color(0.15f, 0.25f, 0.4f));
        float x0 = -0.4f, x1 = 2.6f, z0 = 4f, z1 = 18f, yP = 1f, yT = 3.3f;
        float cx = (x0 + x1) / 2f, cz = (z0 + z1) / 2f, h = yT - yP;
        Caja("Vagon_Piso", new Vector3(cx, yP - 0.075f, cz), new Vector3(x1 - x0, 0.15f, z1 - z0), piso);
        Caja("Vagon_Bajo", new Vector3(cx, (yP - 0.15f) / 2f + 0.15f, cz), new Vector3(x1 - x0 - 0.2f, yP - 0.3f, z1 - z0 - 0.4f), naranja, false);
        Caja("Vagon_Techo", new Vector3(cx, yT + 0.06f, cz), new Vector3(x1 - x0 + 0.1f, 0.12f, z1 - z0 + 0.1f), naranja);
        Caja("Vagon_Der", new Vector3(x1, yP + h / 2f, cz), new Vector3(0.1f, h, z1 - z0), naranja);
        Caja("Vagon_Frente", new Vector3(cx, yP + h / 2f, z0), new Vector3(x1 - x0, h, 0.1f), naranja);
        Caja("Vagon_Fondo", new Vector3(cx, yP + h / 2f, z1), new Vector3(x1 - x0, h, 0.1f), naranja);
        // Lado del andén con una puerta abierta (z 4.3 a 5.8)
        Caja("Vagon_Izq_A", new Vector3(x0, yP + h / 2f, 4.15f), new Vector3(0.1f, h, 0.3f), naranja);
        Caja("Vagon_Izq_B", new Vector3(x0, yP + h / 2f, (5.8f + z1) / 2f), new Vector3(0.1f, h, z1 - 5.8f), naranja);
        Caja("Vagon_Dintel", new Vector3(x0, yT - 0.25f, 5.05f), new Vector3(0.1f, 0.5f, 1.5f), naranja);
        // Franja blanca y ventanas
        Caja("Franja_Izq", new Vector3(x0 - 0.06f, yP + 0.6f, (5.8f + z1) / 2f), new Vector3(0.02f, 0.18f, z1 - 5.8f), blanco, false);
        Caja("Franja_Der", new Vector3(x1 + 0.06f, yP + 0.6f, cz), new Vector3(0.02f, 0.18f, z1 - z0), blanco, false);
        for (float z = 7f; z < z1 - 1f; z += 2.2f)
        {
            Caja("Ventana_I", new Vector3(x0 - 0.06f, yP + 1.45f, z), new Vector3(0.02f, 0.7f, 1.4f), vidrio, false);
            Caja("Ventana_D", new Vector3(x1 + 0.06f, yP + 1.45f, z), new Vector3(0.02f, 0.7f, 1.4f), vidrio, false);
        }
        // Asientos a lo largo
        for (float z = 7f; z < z1 - 1f; z += 2.2f)
        {
            Caja("Asiento", new Vector3(x1 - 0.35f, yP + 0.25f, z), new Vector3(0.5f, 0.5f, 1.8f), asiento);
            Caja("Respaldo", new Vector3(x1 - 0.12f, yP + 0.7f, z), new Vector3(0.1f, 0.6f, 1.8f), asiento, false);
        }
        // Tubos para agarrarse
        for (float z = 6.5f; z < z1; z += 3f)
        {
            Transform tubo = Caja("Tubo", new Vector3(cx - 0.2f, yP + h / 2f, z), new Vector3(0.05f, h, 0.05f), blanco, false);
        }
        // Luces del vagón (apagadas hasta la segunda caja)
        lucesVagon = new Light[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject l = new GameObject("LuzVagon_" + i); l.transform.SetParent(I, false);
            l.transform.localPosition = new Vector3(cx, yT - 0.25f, 7f + i * 4.5f);
            lucesVagon[i] = l.AddComponent<Light>(); lucesVagon[i].type = LightType.Point; lucesVagon[i].range = 6f;
            lucesVagon[i].intensity = 1.8f; lucesVagon[i].color = new Color(0.85f, 1f, 0.9f); lucesVagon[i].enabled = false;
        }
        // Letrero
        Caja("Letrero_LineaK", new Vector3(x0 - 0.07f, yT - 0.35f, 9f), new Vector3(0.02f, 0.3f, 2.2f), Mat(new Color(0.9f, 0.85f, 0.1f), true), false);

        // La llave 3 ahora está dentro del vagón (sobre un asiento), y no aparece hasta vencer a La Sonriente
        puntoLlave = I.TransformPoint(new Vector3(x1 - 0.35f, yP + 0.65f, 13.6f));
        LlaveBucle ll = LlaveTunel();
        if (ll != null && !ll.Tomada)
        {
            ll.transform.position = puntoLlave;
            ll.soloConLinterna = false;
            ll.Disponible = false;
            if (tunel.casillero != null) tunel.casillero.llave = null;
        }

        // Cajas de fusibles: una en el andén y otra del lado de las vías
        cajas = new CajaFusibles[2];
        cajas[0] = CajaFusibles.Crear(I, new Vector3(-4.9f, 2.4f, -12f), Quaternion.Euler(0, 90f, 0), this, 0);
        cajas[1] = CajaFusibles.Crear(I, new Vector3(4.9f, 1.4f, 10f), Quaternion.Euler(0, -90f, 0), this, 1);
    }

    LlaveBucle LlaveTunel()
    {
        SitioTunel s = tunel.GetComponent<SitioTunel>();
        if (s != null && s.llave != null) return s.llave;
        return tunel.casillero != null ? tunel.casillero.llave : null;
    }

    // ================================================================== Luces
    public void CajaPrendida(int indice)
    {
        int n = CajasPrendidas;
        if (tunel.luces != null)
            for (int i = 0; i < tunel.luces.Length; i++)
            {
                Light l = tunel.luces[i]; if (l == null) continue;
                bool toca = n >= 2 || (i % 2 == indice % 2);
                if (toca) { l.enabled = true; l.color = new Color(0.8f, 1f, 0.85f); l.intensity = 3.2f; l.range = 16f; }
            }
        if (n >= 2) foreach (Light l in lucesVagon) l.enabled = true;
        VHSOverlay.Glitch(0.5f, 0.4f);
        SensacionJuego.Sacudir(0.25f);
        string msg = n >= 2 ? "¡Todas las luces! Ahora sí le duele." : "Una caja lista. Falta la otra.";
        DialogoUI.Susurrar("", msg, 2.5f);
        if (pelea) DialogoUI.NuevoObjetivo(Objetivo());
    }

    string Objetivo() => HorrorInput.Traducir("Lastima a La Sonriente — prende las luces (cajas de fusibles, mantén [A]) para hacerle más daño   " + CajasPrendidas + "/2");

    // ================================================================== Pelea
    void Update()
    {
        if (I == null || vencida) return;
        Transform j = SaludJugador.Instancia != null ? SaludJugador.Instancia.transform : null;
        if (j == null) return;
        if (!pelea)
        {
            Vector3 l = I.InverseTransformPoint(j.position);
            bool enVagon = l.x > -0.4f && l.x < 2.6f && l.z > 4f && l.z < 18f && l.y > 0.7f;
            LlaveBucle ll = LlaveTunel();
            if (enVagon && HorrorGameState.PuedeJugar && (ll == null || !ll.Tomada)) StartCoroutine(EmpezarPelea(j));
            return;
        }
        if (SaludJugador.Instancia.Vida <= 0f) { TerminarPelea(false); return; }
        MoverJefe(j);
    }

    IEnumerator EmpezarPelea(Transform j)
    {
        pelea = true;
        jefe = FindAnyObjectByType<LaSonriente>(FindObjectsInactive.Include);
        if (jefe == null) { pelea = false; vencida = true; Liberar(); yield break; }
        if (AcechoDirector.Instancia != null) AcechoDirector.Instancia.Habilitar(false);
        jefe.Reiniciar();
        // Se cierra todo: apagón rojo
        if (tunel.luces != null) foreach (Light l in tunel.luces) if (l != null) { l.color = new Color(0.8f, 0.1f, 0.08f); l.intensity = 0.8f; }
        foreach (CajaFusibles c in cajas) c.Reiniciar();
        foreach (Light l in lucesVagon) l.enabled = false;
        VHSOverlay.Glitch(1f, 1.2f);
        SensacionJuego.Sacudir(0.6f);
        AudioSource.PlayClipAtPoint(ProceduralAudio.GolpeGrave(), j.position, 1f);
        DialogoUI.Susurrar("", "¡Se cerró todo! ...no estoy solo aquí abajo.", 3f);
        yield return new WaitForSeconds(1.8f);

        // Aparece al fondo del andén
        jefeT = jefe.transform;
        jefe.gameObject.SetActive(true);
        jefe.enabled = false;
        Mostrar(true);
        foreach (Collider c in jefe.GetComponentsInChildren<Collider>()) c.enabled = false;
        GameObject hb = new GameObject("Golpeable_Jefe");
        hb.transform.SetParent(jefeT, false); hb.transform.localPosition = Vector3.up * 1f;
        CapsuleCollider cc = hb.AddComponent<CapsuleCollider>(); cc.isTrigger = true; cc.height = 2f; cc.radius = 0.45f;
        hb.AddComponent<GolpeableJefe>().metro = this;
        jefeAnim = jefe.GetComponentInChildren<Animator>();
        jefeT.position = I.TransformPoint(new Vector3(-2.75f, 1f, -17f));
        danioHecho = 0f; siguienteAtaque = Time.time + 3f; aturdida = 0f;
        Barra(true);
        DialogoUI.NuevoObjetivo(Objetivo());
        Animar("Walk");
    }

    void Mostrar(bool v)
    {
        typeof(EnemigoBase).GetMethod("Mostrar", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.Invoke(jefe, new object[] { v });
    }

    void Animar(string e)
    {
        if (jefeAnim != null && jefeAnim.HasState(0, Animator.StringToHash(e))) jefeAnim.CrossFadeInFixedTime(e, 0.15f);
    }

    void MoverJefe(Transform j)
    {
        if (jefeT == null) return;
        if (aturdida > 0f) { aturdida -= Time.deltaTime; return; }
        Vector3 meta = j.position;
        Vector3 d = meta - jefeT.position; float dist = new Vector2(d.x, d.z).magnitude;
        Vector3 plano = new Vector3(d.x, 0, d.z);
        if (plano.sqrMagnitude > 0.01f) jefeT.rotation = Quaternion.Slerp(jefeT.rotation, Quaternion.LookRotation(plano), 8f * Time.deltaTime);

        if (dist < 1.5f)
        {
            if (Time.time > siguienteAtaque)
            {
                siguienteAtaque = Time.time + 2.2f;
                Animar("Attack");
                SaludJugador.Instancia.RecibirDanio(CajasPrendidas >= 2 ? 10f : 14f, jefeT.position);
                VHSOverlay.Glitch(0.8f, 0.5f);
                HorrorInput.Vibrar(0.9f, 0.9f, 0.4f, this);
                StartCoroutine(Reaparecer(0.7f));
            }
            return;
        }
        bool corre = dist > 7f;
        float vel = corre ? 5.2f : 2.6f;
        if (jefeAnim != null) Animar(corre ? "Run" : "Walk");
        float y = Mathf.MoveTowards(jefeT.position.y, j.position.y, Time.deltaTime * 2f);
        Vector3 sig = Vector3.MoveTowards(jefeT.position, new Vector3(meta.x, y, meta.z), vel * Time.deltaTime);
        jefeT.position = sig;
    }

    // Después de atacar se desvanece y sale por otro lado (no te deja respirar)
    IEnumerator Reaparecer(float espera)
    {
        aturdida = espera + 0.6f;
        yield return new WaitForSeconds(espera);
        if (!pelea) yield break;
        VHSOverlay.Glitch(0.5f, 0.3f);
        Vector3[] locales = { new Vector3(2.15f, 0f, -18f), new Vector3(-2.75f, 1f, -18.5f), new Vector3(2.15f, 0f, 19f), new Vector3(-3.5f, 1f, 3f) };
        Transform j = SaludJugador.Instancia.transform;
        Vector3 mejor = jefeT.position; float lejos = 0f;
        foreach (Vector3 l in locales) { Vector3 w = I.TransformPoint(l); float dd = Vector3.Distance(w, j.position); if (dd > lejos && dd < 26f) { lejos = dd; mejor = w; } }
        jefeT.position = mejor;
    }

    public void RecibirGolpe(float danio, Vector3 origen)
    {
        if (!pelea) return;
        float real = danio * Multiplicador;
        danioHecho += real;
        aturdida = 0.7f;
        Vector3 empuje = jefeT.position - origen; empuje.y = 0;
        jefeT.position += empuje.normalized * 0.8f;
        VHSOverlay.Glitch(0.35f, 0.2f);
        SensacionJuego.Pausa(0.06f);
        AudioSource.PlayClipAtPoint(ProceduralAudio.Grito(Random.Range(1, 99)), jefeT.position, CajasPrendidas >= 1 ? 1f : 0.5f);
        if (CajasPrendidas == 0 && Random.value < 0.5f) DialogoUI.Susurrar("", "...casi no le hace nada en la oscuridad. ¡Las luces!", 2f);
        Barra(true);
        if (danioHecho >= DanioNecesario) TerminarPelea(true);
    }

    void TerminarPelea(bool gano)
    {
        if (!pelea) return;
        pelea = false;
        Barra(false);
        if (jefe != null)
        {
            Transform hb = jefeT != null ? jefeT.Find("Golpeable_Jefe") : null;
            if (hb != null) Destroy(hb.gameObject);
            Mostrar(false);
            jefe.enabled = true;
            jefe.Reiniciar();
        }
        if (gano)
        {
            vencida = true;
            StartCoroutine(Victoria());
        }
        // Si perdiste, al reaparecer puedes volver a entrar al vagón y empieza otra vez
    }

    IEnumerator Victoria()
    {
        VHSOverlay.Glitch(1f, 1.2f);
        SensacionJuego.Sacudir(0.8f);
        DialogoUI.Susurrar("", "...se fue gritando. Las puertas... se abrieron.", 3.5f);
        Liberar();
        yield return new WaitForSeconds(1.5f);
        LlaveBucle ll = LlaveTunel();
        if (ll != null && !ll.Tomada)
        {
            ll.transform.position = puntoLlave;
            ll.Disponible = true;
            GameObject b = new GameObject("BrilloLlave3"); b.transform.position = puntoLlave + Vector3.up * 0.4f;
            Light l = b.AddComponent<Light>(); l.type = LightType.Point; l.range = 3f; l.intensity = 2.2f; l.color = new Color(1f, 0.85f, 0.4f);
            Destroy(b, 30f);
            if (LlavesBucle.Instancia != null) { LlavesBucle.Instancia.destino = ll.transform; LlavesBucle.Instancia.nombreDestino = "LLAVE 3"; }
            DialogoUI.NuevoObjetivo(HorrorInput.Traducir("Toma la llave del vagón  [A]"));
        }
    }

    void Liberar()
    {
        if (tunel.luces != null) foreach (Light l in tunel.luces) if (l != null) { l.enabled = true; l.color = new Color(0.75f, 0.95f, 0.8f); }
    }

    // ================================================================== Barra de vida del jefe
    void Barra(bool v)
    {
        if (barraCanvas == null)
        {
            if (!v) return;
            barraCanvas = HorrorUI.CrearCanvas("Jefe_Canvas", 130, null);
            barraTexto = HorrorUI.CrearTexto(barraCanvas.transform, "Nombre", "LA SONRIENTE", 26, new Color(0.9f, 0.25f, 0.2f), TextAnchor.MiddleCenter, FontStyle.Bold);
            HorrorUI.Colocar(barraTexto.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -95), new Vector2(700, 34));
            Image f = HorrorUI.CrearImagen(barraCanvas.transform, "Fondo", new Color(0, 0, 0, 0.7f));
            HorrorUI.Colocar(f.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -125), new Vector2(620, 16));
            barraRelleno = HorrorUI.CrearImagen(f.transform, "Vida", new Color(0.75f, 0.1f, 0.08f));
            barraRelleno.rectTransform.anchorMin = Vector2.zero; barraRelleno.rectTransform.anchorMax = new Vector2(0, 1);
            barraRelleno.rectTransform.pivot = new Vector2(0, 0.5f); barraRelleno.rectTransform.anchoredPosition = Vector2.zero;
        }
        barraCanvas.gameObject.SetActive(v);
        if (barraRelleno != null) barraRelleno.rectTransform.sizeDelta = new Vector2(620f * Mathf.Clamp01(1f - danioHecho / DanioNecesario), 0);
    }
}

/// <summary>Lo que recibe los golpes del jugador durante la pelea en el metro.</summary>
public class GolpeableJefe : MonoBehaviour, IDaniable
{
    public MetroTunel metro;
    public bool EstaVivo => MetroTunel.PeleaActiva;
    public void RecibirDanio(float cantidad, Vector3 origen) { if (metro != null) metro.RecibirGolpe(cantidad, origen); }
}

/// <summary>Caja de fusibles: abrir → cambiar el fusible → bajar la palanca (sin moverte). Prende parte de las luces.</summary>
public class CajaFusibles : MonoBehaviour, IInteractable
{
    public bool Prendida { get; private set; }
    public bool Trabajando { get; private set; }
    MetroTunel metro; int indice;
    Transform puerta, palanca; Light foco;

    static readonly string[] Pasos = { "Abriendo la caja de fusibles...", "Cambiando el fusible quemado...", "Bajando la palanca... no te muevas" };
    static readonly float[] Tiempos = { 0.8f, 1.4f, 1.2f };

    public static CajaFusibles Crear(Transform interior, Vector3 local, Quaternion giro, MetroTunel m, int i)
    {
        GameObject g = new GameObject("CajaFusibles_" + i);
        g.transform.SetParent(interior, false);
        g.transform.localPosition = local; g.transform.localRotation = giro;
        CajaFusibles c = g.AddComponent<CajaFusibles>();
        c.metro = m; c.indice = i; c.Construir();
        return c;
    }

    void Construir()
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit"); if (sh == null) sh = Shader.Find("Universal Render Pipeline/Unlit");
        Material gris = new Material(sh); gris.SetColor("_BaseColor", new Color(0.35f, 0.37f, 0.33f));
        Material amarillo = new Material(sh); amarillo.SetColor("_BaseColor", new Color(0.85f, 0.7f, 0.1f));
        GameObject caja = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(caja.GetComponent<Collider>());
        caja.transform.SetParent(transform, false); caja.transform.localScale = new Vector3(0.7f, 0.9f, 0.25f); caja.GetComponent<Renderer>().sharedMaterial = gris;
        puerta = new GameObject("Bisagra").transform; puerta.SetParent(transform, false); puerta.localPosition = new Vector3(-0.35f, 0, 0.13f);
        GameObject p = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(p.GetComponent<Collider>());
        p.transform.SetParent(puerta, false); p.transform.localPosition = new Vector3(0.35f, 0, 0); p.transform.localScale = new Vector3(0.7f, 0.9f, 0.03f);
        p.GetComponent<Renderer>().sharedMaterial = amarillo;
        palanca = new GameObject("Palanca").transform; palanca.SetParent(transform, false); palanca.localPosition = new Vector3(0.45f, 0.1f, 0.05f);
        GameObject pa = GameObject.CreatePrimitive(PrimitiveType.Cube); Destroy(pa.GetComponent<Collider>());
        pa.transform.SetParent(palanca, false); pa.transform.localPosition = new Vector3(0, 0.18f, 0); pa.transform.localScale = new Vector3(0.06f, 0.36f, 0.06f);
        pa.GetComponent<Renderer>().sharedMaterial = gris;
        GameObject fl = new GameObject("Foco"); fl.transform.SetParent(transform, false); fl.transform.localPosition = new Vector3(0, 0.6f, 0.2f);
        foco = fl.AddComponent<Light>(); foco.type = LightType.Point; foco.range = 3f; foco.intensity = 1.2f; foco.color = new Color(1f, 0.15f, 0.1f);
        BoxCollider bc = gameObject.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = new Vector3(1.4f, 1.6f, 1.4f); bc.center = new Vector3(0, 0, 0.5f);
    }

    public void Reiniciar()
    {
        Prendida = false;
        if (palanca != null) palanca.localRotation = Quaternion.identity;
        if (puerta != null) puerta.localRotation = Quaternion.identity;
        if (foco != null) foco.color = new Color(1f, 0.15f, 0.1f);
    }

    public void Interact(PlayerInteraction jugador)
    {
        if (Prendida || Trabajando || AccionMantenida.Ocupado) return;
        Trabajando = true;
        StartCoroutine(Animar());
        StartCoroutine(AccionMantenida.Ejecutar(jugador.transform, Pasos, Tiempos, 2, ok =>
        {
            Trabajando = false;
            if (ok) PrenderYa();
            else { puerta.localRotation = Quaternion.identity; DialogoUI.Susurrar("", "Me dio toque... otra vez.", 2f); }
        }));
    }

    public void PrenderYa()
    {
        if (Prendida) return;
        Prendida = true;
        palanca.localRotation = Quaternion.Euler(0, 0, -150f);
        foco.color = new Color(0.3f, 1f, 0.35f);
        AudioSource.PlayClipAtPoint(ProceduralAudio.GolpeGrave(), transform.position, 0.8f);
        metro.CajaPrendida(indice);
    }

    IEnumerator Animar()
    {
        for (float t = 0; t < 0.5f && Trabajando; t += Time.deltaTime) { puerta.localRotation = Quaternion.Euler(0, -Mathf.Lerp(0, 110f, t / 0.5f), 0); yield return null; }
        while (Trabajando) { foco.intensity = Random.value < 0.3f ? 3f : 1f; yield return new WaitForSeconds(0.07f); }
        foco.intensity = 1.5f;
    }
}
