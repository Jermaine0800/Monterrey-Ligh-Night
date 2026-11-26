using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menú 18 (y al final del 12): lugares más grandes y explorables.
///  · CAMPAMENTO: arroyo con puente, cueva con grieta para esconderse, campamento abandonado del otro equipo, sendero con letreros.
///  · PANTEÓN: mausoleo con interior (nichos), caseta del sepulturero (con casillero para esconderse), fosa común.
///  · ESTACIÓN DE GUARDABOSQUES (lugar nuevo, opcional): cabaña con cuartos, torre de vigilancia con escaleras,
///    bodega, provisiones y un arma. Sirve de refugio a mitad del mapa.
/// Regla de diseño (Resident Evil / Silent Hill / Outlast): cada zona tiene algo que encontrar (curación, arma o pista),
/// un lugar para esconderse y una luz que la anuncia desde lejos; los caminos se leen por luces y letreros.
/// </summary>
public static class MonterreyExpansionSetup
{
    const string Camp = "Assets/Escenarios/PSXForestCamp";
    const string Pant = "Assets/Escenarios/PSXGraveyard";
    const string CarpetaMat = "Assets/Escenarios/Materiales";

    [MenuItem("Monterrey/18. Ampliar campamento y panteón + estación de guardabosques")]
    public static void Construir()
    {
        MisionManager mm = Object.FindAnyObjectByType<MisionManager>();
        PlayerMovement pm = Object.FindAnyObjectByType<PlayerMovement>();
        Terrain ter = Terrain.activeTerrain;
        if (mm == null || mm.sitios == null || pm == null || ter == null) { Debug.LogError("[Monterrey] Corre primero el menú 12."); return; }
        Random.InitState(4471);

        foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            if (t != null && (t.name == "Expansion" || t.name == "_EstacionGuardabosques")) Object.DestroyImmediate(t.gameObject);

        var zonas = new List<(Vector3, float)>();
        foreach (SitioLlave s in mm.sitios)
        {
            if (s is SitioCampamento c) Campamento(c.transform, zonas);
            if (s is SitioPanteon p) Panteon(p.transform, zonas);
        }
        Estacion(mm, pm.transform.position, ter, zonas);
        MonterreyLlavesSetup.QuitarArboles(ter, zonas.ToArray());
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[Monterrey] Expansión lista: arroyo, cueva, campamento abandonado, mausoleo, caseta del sepulturero, fosa común y estación de guardabosques.");
    }

    // ================================================================== Ayudantes

    static Material Mat(string n, Color c)
    {
        string ruta = CarpetaMat + "/" + n + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, ruta); }
        m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.08f);
        EditorUtility.SetDirty(m);
        return m;
    }

    /// <summary>Zona: un pivote dentro del sitio, en (x, z) locales, girado como el sitio + giro.</summary>
    static Transform Zona(Transform padre, string nombre, Transform sitio, float x, float z, float giro)
    {
        Transform t = new GameObject(nombre).transform;
        t.SetParent(padre, true);
        t.position = Mundo.EnSuelo(sitio.TransformPoint(new Vector3(x, 0, z)));
        t.rotation = sitio.rotation * Quaternion.Euler(0, giro, 0);
        return t;
    }

    /// <summary>Bloque sobre el suelo en coordenadas locales de la zona (y = altura sobre el suelo en ese punto).</summary>
    static Transform B(Transform z, string n, float x, float y, float zz, Vector3 tam, Material m, bool col = true, float giro = 0f, bool sueloLocal = true)
    {
        Vector3 w = z.TransformPoint(new Vector3(x, 0, zz));
        float suelo = sueloLocal ? Mundo.AlturaSuelo(w) : z.position.y;
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = n; g.transform.SetParent(z, true);
        g.transform.position = new Vector3(w.x, suelo + y, w.z);
        g.transform.rotation = z.rotation * Quaternion.Euler(0, giro, 0);
        g.transform.localScale = tam;
        g.GetComponent<Renderer>().sharedMaterial = m;
        if (!col) Object.DestroyImmediate(g.GetComponent<Collider>());
        return g.transform;
    }

    /// <summary>
    /// Cuarto con piso nivelado, 4 muros y un hueco de puerta en el muro de enfrente (+Z local).
    /// Devuelve la altura del piso (mundo).
    /// </summary>
    static float Cuarto(Transform z, string n, float cx, float cz, float ancho, float fondo, float alto, float puerta, Material muro, Material piso, bool techo = true, float puertaX = 0f, float hFijo = -9999f)
    {
        Vector3 centro = z.TransformPoint(new Vector3(cx, 0, cz));
        float h0 = hFijo > -9000f ? hFijo : Mathf.Max(Mundo.AlturaSuelo(centro),
                             Mundo.AlturaSuelo(z.TransformPoint(new Vector3(cx + ancho / 2, 0, cz + fondo / 2))),
                             Mundo.AlturaSuelo(z.TransformPoint(new Vector3(cx - ancho / 2, 0, cz - fondo / 2)))) + 0.05f;
        float g = 0.18f;
        Transform r = new GameObject(n).transform; r.SetParent(z, false);
        // Cimiento: llena el desnivel del terreno
        Bx(r, z, "Piso", cx, h0 - 0.6f, cz, new Vector3(ancho, 1.2f, fondo), piso);
        Bx(r, z, "MuroFondo", cx, h0 + alto / 2, cz - fondo / 2, new Vector3(ancho, alto, g), muro);
        Bx(r, z, "MuroIzq", cx - ancho / 2, h0 + alto / 2, cz, new Vector3(g, alto, fondo), muro);
        Bx(r, z, "MuroDer", cx + ancho / 2, h0 + alto / 2, cz, new Vector3(g, alto, fondo), muro);
        float izq = (ancho / 2 + puertaX) - puerta / 2, der = (ancho / 2 - puertaX) - puerta / 2;
        if (izq > 0.05f) Bx(r, z, "FrenteIzq", cx - ancho / 2 + izq / 2, h0 + alto / 2, cz + fondo / 2, new Vector3(izq, alto, g), muro);
        if (der > 0.05f) Bx(r, z, "FrenteDer", cx + ancho / 2 - der / 2, h0 + alto / 2, cz + fondo / 2, new Vector3(der, alto, g), muro);
        if (puerta > 0.05f) Bx(r, z, "Dintel", cx + puertaX, h0 + alto - 0.3f, cz + fondo / 2, new Vector3(puerta, 0.6f, g), muro);
        if (techo) Bx(r, z, "Techo", cx, h0 + alto + 0.1f, cz, new Vector3(ancho + 0.5f, 0.2f, fondo + 0.5f), muro);
        return h0;
    }

    /// <summary>Bloque a una altura de mundo fija (para cuartos con piso nivelado).</summary>
    static Transform Bx(Transform padre, Transform z, string n, float x, float yMundo, float zz, Vector3 tam, Material m, bool col = true, float giro = 0f)
    {
        Vector3 w = z.TransformPoint(new Vector3(x, 0, zz));
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = n; g.transform.SetParent(padre, true);
        g.transform.position = new Vector3(w.x, yMundo, w.z);
        g.transform.rotation = z.rotation * Quaternion.Euler(0, giro, 0);
        g.transform.localScale = tam;
        g.GetComponent<Renderer>().sharedMaterial = m;
        if (!col) Object.DestroyImmediate(g.GetComponent<Collider>());
        return g.transform;
    }

    static Light Luz(Transform p, Vector3 w, Color c, float rango, float i, bool parpadea = true)
    {
        Light l = MonterreyAmbientacionSetup.Luz(p, w, c, rango, i, parpadea);
        return l;
    }

    static Escondite Esc(Transform padre, Vector3 dentro, Vector3 salida, Vector3 tam, string nombre, float ojos = -1f)
    {
        Escondite e = MonterreyLlavesSetup.CrearEscondite(padre, "Escondite_" + nombre, dentro, tam, nombre);
        Transform s = new GameObject("PuntoSalida").transform; s.SetParent(e.transform, true); s.position = salida;
        e.puntoSalida = s;
        e.alturaOjos = ojos;
        return e;
    }

    static GameObject M(string pack, string nombre, Transform p, Vector3 w, float giro, bool col = true, float esc = 1f)
        => MonterreyAmbientacionSetup.Modelo(pack, nombre, p, w, giro, col, esc);

    static void Letrero(Transform p, Vector3 w, float giro, string texto)
    {
        GameObject g = M(Camp, "Sign_Trail_01", p, w, giro, true);
        g.name = "Letrero_" + texto;
    }

    // ================================================================== CAMPAMENTO

    static void Campamento(Transform s, List<(Vector3, float)> zonas)
    {
        Transform e = new GameObject("Expansion").transform; e.SetParent(s, false);
        Material roca = Mat("RocaCueva", new Color(0.3f, 0.29f, 0.27f));
        Material agua = Mat("AguaNegra", new Color(0.04f, 0.06f, 0.07f));
        Material madera = Mat("Durmiente", new Color(0.2f, 0.14f, 0.09f));
        Material lona = Mat("LonaVerde", new Color(0.18f, 0.25f, 0.14f));

        // --- Arroyo detrás del campamento, con puente de tablas
        Transform ar = Zona(e, "Arroyo", s, 0, -15f, 0);
        for (float x = -18f; x <= 18f; x += 2f)
        {
            float off = Mathf.Sin(x * 0.25f) * 1.2f;
            Transform w = B(ar, "Agua", x, -0.06f, off, new Vector3(2.2f, 0.1f, 2.4f), agua, false);
            if (Mathf.Abs(x) > 1.5f && Random.value < 0.7f)
                B(ar, "Piedra", x + Random.Range(-0.6f, 0.6f), 0.1f, off + (Random.value < 0.5f ? 1.5f : -1.5f), Vector3.one * Random.Range(0.4f, 0.9f), roca, true, Random.Range(0, 90f));
        }
        for (int i = -3; i <= 3; i++) B(ar, "Tabla", i * 0.32f, 0.12f, 0, new Vector3(0.28f, 0.06f, 3.6f), madera, true, Random.Range(-3f, 3f));
        B(ar, "Baranda", -1.2f, 0.65f, 0, new Vector3(0.06f, 0.06f, 3.6f), madera, false);
        Luz(ar, ar.TransformPoint(new Vector3(0, 1.5f, 0)), new Color(0.6f, 0.75f, 1f), 7f, 0.8f);
        zonas.Add((ar.position, 19f));

        // --- Cueva al otro lado del arroyo: rocas en U con techo, adentro una grieta para esconderse
        Transform cu = Zona(e, "Cueva", s, 10f, -26f, 200f);   // la boca mira hacia el arroyo
        for (int i = 0; i < 7; i++) B(cu, "ParedIzq", -2.2f + Random.Range(-0.2f, 0.2f), 1.2f, -i * 0.9f, new Vector3(1.4f, 2.6f, 1.2f), roca, true, Random.Range(-15f, 15f));
        for (int i = 0; i < 7; i++) B(cu, "ParedDer", 2.2f + Random.Range(-0.2f, 0.2f), 1.2f, -i * 0.9f, new Vector3(1.4f, 2.6f, 1.2f), roca, true, Random.Range(-15f, 15f));
        B(cu, "Fondo", 0, 1.2f, -6.6f, new Vector3(5.6f, 2.6f, 1.4f), roca);
        B(cu, "Techo", 0, 2.7f, -3f, new Vector3(6f, 0.8f, 7.6f), roca);
        B(cu, "RocaBoca", -2.6f, 1.6f, 0.8f, new Vector3(1.6f, 3.2f, 1.4f), roca, true, 20f);
        B(cu, "RocaBoca2", 2.6f, 1.4f, 0.8f, new Vector3(1.6f, 2.8f, 1.4f), roca, true, -25f);
        for (int i = 0; i < 6; i++) B(cu, "Hueso", Random.Range(-1f, 1f), 0.05f, Random.Range(-5.5f, -3f), new Vector3(0.05f, 0.05f, 0.35f), Mat("Hueso", new Color(0.8f, 0.78f, 0.7f)), false, Random.Range(0, 180f));
        Luz(cu, cu.TransformPoint(new Vector3(0, 1.4f, -5f)), new Color(1f, 0.5f, 0.25f), 6f, 1.1f);
        MonterreyAmbientacionSetup.Veladoras(cu, Mundo.EnSuelo(cu.TransformPoint(new Vector3(0.8f, 0, -5.6f))), 6, 0.3f, false);
        Esc(cu, Mundo.EnSuelo(cu.TransformPoint(new Vector3(-1.3f, 0, -5.4f))), Mundo.EnSuelo(cu.TransformPoint(new Vector3(0, 0, -4f))), new Vector3(1.2f, 1.6f, 1.2f), "la grieta de la cueva", 0.9f);
        MonterreyDetallesSetup.Botiquin(cu, cu, 1.2f, -4.2f, "Botiquín");
        Vector3 pp = cu.TransformPoint(new Vector3(0, 0, -5.85f)); pp.y = Mundo.AlturaSuelo(pp) + 1.4f;
        MonterreyAmbientacionSetup.Pinta(cu, "Pinta_CuantasVeces", pp, cu.rotation, 1.3f);
        zonas.Add((cu.position, 9f));

        // --- Campamento abandonado del otro equipo (a un lado): casa de campaña para esconderse, hielera, trampa
        Transform ab = Zona(e, "CampamentoAbandonado", s, -17f, -6f, 60f);
        M(Camp, "Tent_01", ab, ab.TransformPoint(new Vector3(0, 0, -1.5f)), ab.eulerAngles.y + 180f);
        M(Camp, "Tent_01", ab, ab.TransformPoint(new Vector3(3.5f, 0, 1f)), ab.eulerAngles.y + 120f);
        M(Camp, "Cooler_01", ab, ab.TransformPoint(new Vector3(1.5f, 0, 2f)), Random.Range(0, 360f));
        M(Camp, "Firewood_Pile_01", ab, ab.TransformPoint(new Vector3(-2f, 0, 2f)), Random.Range(0, 360f));
        M(Camp, "Log_Seat_01", ab, ab.TransformPoint(new Vector3(0.5f, 0, 3.5f)), Random.Range(0, 360f));
        M(Camp, "Lantern_01", ab, ab.TransformPoint(new Vector3(1.2f, 0, 3.3f)), 0, false);
        Luz(ab, ab.TransformPoint(new Vector3(1.2f, 0.6f, 3.3f)) + Vector3.up * (Mundo.AlturaSuelo(ab.position) - ab.position.y), new Color(1f, 0.7f, 0.35f), 6f, 1.2f);
        Esc(ab, Mundo.EnSuelo(ab.TransformPoint(new Vector3(0, 0, -1.5f))), Mundo.EnSuelo(ab.TransformPoint(new Vector3(0, 0, 1f))), new Vector3(2f, 1.4f, 2.2f), "la casa de campaña", 0.6f);
        MonterreyAmbientacionSetup.Trampa(ab, ab.TransformPoint(new Vector3(-1.5f, 0, 4.5f)));
        MonterreyDetallesSetup.Botiquin(ab, ab, 3f, -0.5f, "Agua");
        Letrero(ab, ab.TransformPoint(new Vector3(-3f, 0, 5f)), ab.eulerAngles.y, "Abandonado");
        zonas.Add((ab.position, 8f));

        // --- Sendero con letreros hacia la cueva y el arroyo
        Letrero(e, Mundo.EnSuelo(s.TransformPoint(new Vector3(4f, 0, -9f))), s.eulerAngles.y + 160f, "Arroyo");
        Letrero(e, Mundo.EnSuelo(s.TransformPoint(new Vector3(9f, 0, -19f))), s.eulerAngles.y + 200f, "Cueva");
        MonterreyAmbientacionSetup.Trampa(e, s.TransformPoint(new Vector3(6f, 0, -21f)));
    }

    // ================================================================== PANTEÓN

    static void Panteon(Transform s, List<(Vector3, float)> zonas)
    {
        Transform e = new GameObject("Expansion").transform; e.SetParent(s, false);
        Material piedra = Mat("PiedraMausoleo", new Color(0.42f, 0.41f, 0.38f));
        Material pisoP = Mat("PisoMausoleo", new Color(0.25f, 0.24f, 0.22f));
        Material madera = Mat("TablaCaseta", new Color(0.24f, 0.17f, 0.11f));
        Material metal = Mat("MetalOxidado", new Color(0.25f, 0.18f, 0.14f));

        // --- Mausoleo de la familia Treviño: un cuarto de piedra con nichos y un ataúd abierto
        Transform ma = Zona(e, "Mausoleo", s, -19f, -6f, 90f);   // puerta hacia el centro del panteón
        float h = Cuarto(ma, "Cuarto", 0, 0, 5f, 6f, 3.2f, 1.4f, piedra, pisoP);
        for (int lado = -1; lado <= 1; lado += 2)
            for (int fila = 0; fila < 3; fila++)
                for (int col = 0; col < 2; col++)
                    Bx(ma, ma, "Nicho", lado * 2.25f, h + 0.5f + fila * 0.9f, -1.6f + col * 1.6f, new Vector3(0.35f, 0.7f, 1.3f), pisoP, false);
        Transform ataud = M(Pant, "Coffin_01", ma, ma.TransformPoint(new Vector3(0, 0, -1.6f)), ma.eulerAngles.y).transform;
        ataud.position = new Vector3(ataud.position.x, h + 0.02f, ataud.position.z);
        Transform velas = M(Pant, "Candles_Skull_01", ma, ma.TransformPoint(new Vector3(1.2f, 0, -2.5f)), 0, false).transform;
        velas.position = new Vector3(velas.position.x, h, velas.position.z);
        Luz(ma, new Vector3(ma.position.x, h + 2.4f, ma.position.z), new Color(1f, 0.55f, 0.3f), 6f, 1f);
        Luz(ma, ma.TransformPoint(new Vector3(0, 0, 4f)) + Vector3.up * (h - ma.position.y + 3f), new Color(0.7f, 0.8f, 1f), 9f, 1.2f);
        // Esconderse: detrás del ataúd, al fondo (bajito, a oscuras)
        Esc(ma, ma.TransformPoint(new Vector3(-1.2f, 0, -2.6f)) + Vector3.up * (h - ma.position.y), ma.TransformPoint(new Vector3(0, 0, 0.5f)) + Vector3.up * (h - ma.position.y), new Vector3(1.2f, 1.4f, 1f), "detrás del ataúd", 0.7f);
        Curacion v = MonterreyDetallesSetup.Botiquin(ma, ma, -1.5f, 1.8f, "Vendas"); v.transform.position = new Vector3(v.transform.position.x, h, v.transform.position.z);
        zonas.Add((ma.position, 7f));

        // --- Caseta del sepulturero: herramientas, casillero de metal para esconderse, un tubo
        Transform ca = Zona(e, "CasetaSepulturero", s, 18f, -9f, -90f);
        float hc = Cuarto(ca, "Cuarto", 0, 0, 4.2f, 4.2f, 2.6f, 1.2f, madera, madera, true, -0.8f);
        Bx(ca, ca, "Mesa", 0.8f, hc + 0.75f, -1.2f, new Vector3(1.6f, 0.08f, 0.8f), madera);
        Bx(ca, ca, "Repisa", -1.85f, hc + 1.4f, -0.5f, new Vector3(0.35f, 0.05f, 2f), madera, false);
        // casillero
        Bx(ca, ca, "Casillero_Fondo", 1.75f, hc + 1f, 1.2f, new Vector3(0.05f, 2f, 0.9f), metal);
        Bx(ca, ca, "Casillero_L1", 1.45f, hc + 1f, 1.62f, new Vector3(0.6f, 2f, 0.05f), metal);
        Bx(ca, ca, "Casillero_L2", 1.45f, hc + 1f, 0.78f, new Vector3(0.6f, 2f, 0.05f), metal);
        Bx(ca, ca, "Casillero_Puerta", 1.14f, hc + 1f, 1.2f, new Vector3(0.04f, 1.9f, 0.82f), metal, false);
        Esc(ca, ca.TransformPoint(new Vector3(1.45f, 0, 1.2f)) + Vector3.up * (hc - ca.position.y), ca.TransformPoint(new Vector3(0.2f, 0, 1.2f)) + Vector3.up * (hc - ca.position.y), new Vector3(1f, 2f, 1f), "el casillero", 1.5f);
        Luz(ca, new Vector3(ca.position.x, hc + 2.2f, ca.position.z), new Color(1f, 0.75f, 0.45f), 5f, 0.9f);
        ArmaRecogible tubo = MonterreyDetallesSetup.Arma(ca, ca, 0.8f, -1.2f, "Tubo"); tubo.transform.position = new Vector3(tubo.transform.position.x, hc + 0.8f, tubo.transform.position.z);
        M(Pant, "Lamp_Post_01", ca, ca.TransformPoint(new Vector3(-1.8f, 0, 3.2f)), 0);
        zonas.Add((ca.position, 6f));

        // --- Fosa común: montículos, árboles secos, una pala clavada y una trampa
        Transform fo = Zona(e, "FosaComun", s, 0f, -24f, 0);
        for (int i = 0; i < 9; i++)
            M(Pant, "Grave_Mound_01", fo, fo.TransformPoint(new Vector3((i % 3 - 1) * 2.2f + Random.Range(-0.3f, 0.3f), 0, (i / 3 - 1) * 2.6f)), Random.Range(-10f, 10f) + fo.eulerAngles.y);
        M(Pant, "Tree_Dead_01", fo, fo.TransformPoint(new Vector3(-5f, 0, 1f)), Random.Range(0, 360f));
        M(Pant, "Tree_Dead_01", fo, fo.TransformPoint(new Vector3(5.5f, 0, -2f)), Random.Range(0, 360f));
        M(Pant, "Shovel_Dirt_01", fo, fo.TransformPoint(new Vector3(3f, 0, 3.2f)), Random.Range(0, 360f), false);
        MonterreyAmbientacionSetup.Trampa(fo, fo.TransformPoint(new Vector3(-2f, 0, 4.2f)));
        MonterreyAmbientacionSetup.Cempasuchil(fo, fo.position, 25, 3.5f);
        MonterreyAmbientacionSetup.Veladoras(fo, fo.TransformPoint(new Vector3(0, 0, 4f)), 10, 0.5f);
        MonterreyDetallesSetup.Botiquin(fo, fo, 1f, -4f, "Agua");
        zonas.Add((fo.position, 10f));
    }

    // ================================================================== ESTACIÓN DE GUARDABOSQUES (nueva)

    static void Estacion(MisionManager mm, Vector3 inicio, Terrain ter, List<(Vector3, float)> zonas)
    {
        List<Vector3> evitar = new List<Vector3> { inicio };
        foreach (SitioLlave s in mm.sitios) if (s != null) evitar.Add(s.transform.position);
        if (mm.puertaBucle != null) evitar.Add(mm.puertaBucle.transform.position);
        Vector3 p = MonterreyLlavesSetup.BuscarPlano(ter, inicio, 60f, 140f, 12f, evitar, 55f);

        Transform raiz = new GameObject("_EstacionGuardabosques").transform;
        raiz.position = p;
        Vector3 d = inicio - p; d.y = 0;
        raiz.rotation = Quaternion.LookRotation(d.normalized);   // la entrada mira hacia el claro

        Material tablas = Mat("TablasCabana", new Color(0.33f, 0.22f, 0.13f));
        Material piso = Mat("PisoCabana", new Color(0.22f, 0.15f, 0.09f));
        Material verde = Mat("VerdeForestal", new Color(0.16f, 0.24f, 0.16f));
        Material metal = Mat("MetalVerde", new Color(0.18f, 0.24f, 0.2f));

        // Cabaña: 2 cuartos (oficina + dormitorio) unidos por una puerta interior
        float h = Cuarto(raiz, "Oficina", -2.5f, 0, 5f, 5f, 2.8f, 1.3f, tablas, piso);
        float h2 = Cuarto(raiz, "Dormitorio", 2.5f, -0.5f, 5f, 4f, 2.8f, 0f, tablas, piso, true, 0f, h);
        // Puerta interior: hueco en el muro compartido
        Transform muroD = raiz.Find("Dormitorio/MuroIzq"); if (muroD != null) Object.DestroyImmediate(muroD.gameObject);
        Transform muroO = raiz.Find("Oficina/MuroDer"); if (muroO != null) Object.DestroyImmediate(muroO.gameObject);
        Bx(raiz, raiz, "MuroMedioA", 0, h + 1.4f, 1.6f, new Vector3(0.18f, 2.8f, 1.8f), tablas);
        Bx(raiz, raiz, "MuroMedioB", 0, h + 1.4f, -1.9f, new Vector3(0.18f, 2.8f, 1.2f), tablas);
        Bx(raiz, raiz, "MuroMedioDintel", 0, h + 2.5f, -0.3f, new Vector3(0.18f, 0.6f, 2f), tablas);
        Bx(raiz, raiz, "Escalon", -2.5f, h - 0.15f, 2.9f, new Vector3(1.6f, 0.3f, 0.6f), piso);
        // Oficina: escritorio, radio, mapa, archivero
        Bx(raiz, raiz, "Escritorio", -3.5f, h + 0.75f, -1.6f, new Vector3(1.8f, 0.08f, 0.8f), tablas);
        Bx(raiz, raiz, "Radio", -3.9f, h + 0.9f, -1.7f, new Vector3(0.4f, 0.22f, 0.25f), metal);
        Bx(raiz, raiz, "Archivero", -4.6f, h + 0.65f, 1.5f, new Vector3(0.5f, 1.3f, 0.6f), metal);
        Bx(raiz, raiz, "MapaPared", -2.5f, h + 1.6f, -2.42f, new Vector3(1.6f, 1f, 0.02f), Mat("MapaPapel", new Color(0.7f, 0.65f, 0.5f)), false);
        // Dormitorio: litera + ropero (escondite)
        Bx(raiz, raiz, "Litera1", 3.8f, h2 + 0.45f, -1.3f, new Vector3(1.2f, 0.12f, 2f), verde);
        Bx(raiz, raiz, "Litera2", 3.8f, h2 + 1.5f, -1.3f, new Vector3(1.2f, 0.12f, 2f), verde);
        Bx(raiz, raiz, "LiteraPoste", 3.2f, h2 + 1f, -2.3f, new Vector3(0.08f, 2f, 0.08f), tablas, false);
        Bx(raiz, raiz, "Ropero_Fondo", 1.2f, h2 + 1f, -2.4f, new Vector3(1.2f, 2f, 0.06f), tablas);
        Bx(raiz, raiz, "Ropero_L1", 0.62f, h2 + 1f, -2.05f, new Vector3(0.06f, 2f, 0.7f), tablas);
        Bx(raiz, raiz, "Ropero_L2", 1.78f, h2 + 1f, -2.05f, new Vector3(0.06f, 2f, 0.7f), tablas);
        Bx(raiz, raiz, "Ropero_Puerta", 1.2f, h2 + 1f, -1.72f, new Vector3(1.1f, 1.9f, 0.04f), piso, false);
        Esc(raiz, raiz.TransformPoint(new Vector3(1.2f, 0, -2.05f)) + Vector3.up * (h2 - raiz.position.y), raiz.TransformPoint(new Vector3(1.2f, 0, -0.6f)) + Vector3.up * (h2 - raiz.position.y), new Vector3(1.2f, 2f, 1f), "el ropero", 1.5f);
        Bx(raiz, raiz, "Ventana", 5.0f, h2 + 1.5f, -0.5f, new Vector3(0.2f, 0.8f, 1.2f), Mat("VidrioSucio", new Color(0.3f, 0.35f, 0.35f)), false);
        Luz(raiz, raiz.TransformPoint(new Vector3(-2.5f, 0, 0)) + Vector3.up * (h - raiz.position.y + 2.4f), new Color(1f, 0.8f, 0.55f), 6f, 1f);
        Luz(raiz, raiz.TransformPoint(new Vector3(2.5f, 0, -0.5f)) + Vector3.up * (h2 - raiz.position.y + 2.4f), new Color(0.6f, 0.7f, 1f), 5f, 0.6f);

        // Provisiones y arma
        Curacion b1 = MonterreyDetallesSetup.Botiquin(raiz, raiz, -3.2f, -1.6f, "Botiquín"); b1.transform.position = new Vector3(b1.transform.position.x, h + 0.8f, b1.transform.position.z);
        Curacion b2 = MonterreyDetallesSetup.Botiquin(raiz, raiz, 3.8f, -0.8f, "Agua"); b2.transform.position = new Vector3(b2.transform.position.x, h2 + 0.52f, b2.transform.position.z);
        ArmaRecogible hacha = MonterreyDetallesSetup.Arma(raiz, raiz, -4.3f, 0.2f, "Hacha"); hacha.transform.position = new Vector3(hacha.transform.position.x, h + 0.02f, hacha.transform.position.z);

        // Nota de lore (opcional, no es de llave)
        MonterreyAmbientacionSetup.Nota(raiz, raiz.TransformPoint(new Vector3(-3.3f, 0, -1.4f)) + Vector3.up * (h + 0.8f - raiz.position.y), "BITÁCORA DEL GUARDABOSQUES",
            "27 nov 1998.\nOtra vez los excursionistas. Otra vez la misma fecha en el radio.\n" +
            "Desde la torre se ven las cinco luces: el campamento, el panteón, la casa de los Garza, el muro del túnel y la baliza del mirador.\n" +
            "Si alguien lee esto: sube a la torre, ubícalas, y no camines en línea recta. Lo que sigue a la gente camina en línea recta.");

        // Torre de vigilancia con escalera (se puede subir) y plataforma
        Transform to = new GameObject("Torre").transform; to.SetParent(raiz, false); to.localPosition = new Vector3(-9f, 0, -3f);
        float ht = Mundo.AlturaSuelo(to.position);
        for (int i = -1; i <= 1; i += 2) for (int j = -1; j <= 1; j += 2)
            Bx(to, raiz, "Pata", -9f + i * 1.4f, ht + 3f, -3f + j * 1.4f, new Vector3(0.22f, 6f, 0.22f), tablas);
        Bx(to, raiz, "Plataforma", -9f, ht + 6f, -3f, new Vector3(3.4f, 0.2f, 3.4f), piso);
        Bx(to, raiz, "BarandaA", -9f, ht + 6.55f, -4.65f, new Vector3(3.4f, 0.9f, 0.08f), tablas);
        Bx(to, raiz, "BarandaB", -10.65f, ht + 6.55f, -3f, new Vector3(0.08f, 0.9f, 3.4f), tablas);
        Bx(to, raiz, "BarandaC", -7.35f, ht + 6.55f, -3f, new Vector3(0.08f, 0.9f, 3.4f), tablas);
        Bx(to, raiz, "Techito", -9f, ht + 8.3f, -3f, new Vector3(3.8f, 0.15f, 3.8f), verde, false);
        // Escalera en rampa (24 escalones de 25 cm) que sube por el frente
        for (int k = 0; k < 24; k++)
            Bx(to, raiz, "Escalon", -9f, ht + 0.12f + k * 0.25f, -1.3f + 6.4f - k * 0.27f, new Vector3(1.1f, 0.25f, 0.3f), piso);
        Luz(to, new Vector3(to.position.x, ht + 7.6f, to.position.z), new Color(1f, 0.35f, 0.25f), 14f, 1.6f);   // se ve desde lejos

        // Bodega con generador (rincón para esconderse entre tambos)
                float hb = Cuarto(raiz, "Bodega", 8f, -4f, 3.5f, 3.5f, 2.4f, 1.2f, Mat("LaminaOxidada", new Color(0.35f, 0.25f, 0.18f)), piso);
        for (int k = 0; k < 4; k++)
        {
            GameObject tambo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tambo.name = "Tambo"; tambo.transform.SetParent(raiz, true);
            tambo.transform.position = raiz.TransformPoint(new Vector3(7f + (k % 2) * 0.7f, 0, -5.2f + (k / 2) * 0.7f)) + Vector3.up * (hb - raiz.position.y + 0.45f);
            tambo.transform.localScale = new Vector3(0.6f, 0.45f, 0.6f);
            tambo.GetComponent<Renderer>().sharedMaterial = Mat("TamboAzul", new Color(0.12f, 0.2f, 0.35f));
        }
        Esc(raiz, raiz.TransformPoint(new Vector3(9.2f, 0, -5.2f)) + Vector3.up * (hb - raiz.position.y), raiz.TransformPoint(new Vector3(8f, 0, -3f)) + Vector3.up * (hb - raiz.position.y), new Vector3(1f, 1.4f, 1f), "detrás de los tambos", 0.8f);
        Luz(raiz, raiz.TransformPoint(new Vector3(8f, 0, -4f)) + Vector3.up * (hb - raiz.position.y + 2f), new Color(1f, 0.6f, 0.3f), 4f, 0.8f);

        // Letrero y luz guía
        Letrero(raiz, Mundo.EnSuelo(raiz.TransformPoint(new Vector3(0, 0, 7f))), raiz.eulerAngles.y + 180f, "Guardabosques");
        MonterreyAmbientacionSetup.Trampa(raiz, raiz.TransformPoint(new Vector3(4f, 0, 5f)));
        zonas.Add((p, 18f));
        Debug.Log("[Monterrey] Estación de guardabosques en " + p + " (" + Mathf.RoundToInt(Vector3.Distance(new Vector3(p.x, 0, p.z), new Vector3(inicio.x, 0, inicio.z))) + " m del claro).");
    }
}
