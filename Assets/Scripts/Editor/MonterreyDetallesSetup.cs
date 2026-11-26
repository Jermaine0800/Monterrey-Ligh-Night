using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menú 12: TODO junto. Corre el menú 11 (lugares y llaves) y además:
/// - Sistemas: música adaptativa, terror psicológico, barra de vida, mapa redondo.
/// - En cada lugar: música propia (3D), curaciones, un arma, objetos que se mueven solos y más utilería
///   para que cada lugar sea un mini escenario.
/// </summary>
public static class MonterreyDetallesSetup
{
    const string Panteon = "Assets/Escenarios/PSXGraveyard";
    const string CarpetaMat = "Assets/Escenarios/Materiales";

    [MenuItem("Monterrey/12. TODO: lugares + vida + mapa + música + sustos (recomendado)")]
    public static void Construir()
    {
        MonterreyLlavesSetup.Construir();

        GameObject sistemas = GameObject.Find("_SistemasTerror");
        MisionManager mm = Object.FindAnyObjectByType<MisionManager>();
        if (sistemas == null || mm == null || mm.sitios == null) { Debug.LogError("[Monterrey] Falta _SistemasTerror o MisionManager."); return; }

        Sistema<MusicaAdaptativa>(sistemas, "MusicaAdaptativa");
        Sistema<PsicoDirector>(sistemas, "PsicoDirector");
        Sistema<HUDJugador>(sistemas, "HUDJugador");
        Sistema<MapaRadial>(sistemas, "MapaRadial");

        foreach (EnemigoBase e in Object.FindObjectsByType<EnemigoBase>(FindObjectsInactive.Include))
        {
            e.danioSusto = 25f;          // un susto te deja herido, no muerto
            e.enfriamientoSusto = 30f;
            EditorUtility.SetDirty(e);
        }

        foreach (SitioLlave s in mm.sitios)
        {
            if (s == null) continue;
            Transform d = new GameObject("Detalles").transform;
            d.SetParent(s.transform, false);
            switch (s)
            {
                case SitioCampamento c: Campamento(c, d); break;
                case SitioPanteon p: PanteonExtra(p, d); break;
                case SitioTunel t: Tunel(t); break;
                case SitioIglesia i: Capilla(i, d); break;
                case SitioCasa ca: Casa(ca, d); break;
                case SitioMirador m: Mirador(m, d); break;
            }
        }

        MonterreyAmbientacionSetup.Construir();
        MonterreyExpansionSetup.Construir();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[Monterrey] Detalles listos: música por lugar, curaciones, armas, objetos embrujados, mapa, vida y terror psicológico.");
    }

    static T Sistema<T>(GameObject sistemas, string nombre) where T : Component
    {
        T c = sistemas.GetComponentInChildren<T>();
        if (c != null) return c;
        GameObject g = new GameObject(nombre);
        g.transform.SetParent(sistemas.transform);
        return g.AddComponent<T>();
    }

    // ================================================================== Ayudantes

    static Material Mat(string n, Color c, bool unlit = false)
    {
        string ruta = CarpetaMat + "/" + n + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null) { m = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, ruta); }
        m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.1f);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Transform Caja(Transform p, string n, Vector3 local, Vector3 esc, Material m, bool col = true)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = n; g.transform.SetParent(p, false); g.transform.localPosition = local; g.transform.localScale = esc;
        g.GetComponent<Renderer>().sharedMaterial = m;
        if (!col) Object.DestroyImmediate(g.GetComponent<Collider>());
        return g.transform;
    }

    static Transform Cil(Transform p, string n, Vector3 local, Vector3 esc, Material m, bool col = false)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        g.name = n; g.transform.SetParent(p, false); g.transform.localPosition = local; g.transform.localScale = esc;
        g.GetComponent<Renderer>().sharedMaterial = m;
        if (!col) Object.DestroyImmediate(g.GetComponent<Collider>());
        return g.transform;
    }

    /// <summary>Un objeto "en el suelo" del sitio (pivote en la base, para que se caiga bien).</summary>
    static Transform Pivote(Transform padre, string n, Transform sitio, float x, float z, float giro = 0f)
    {
        Transform t = new GameObject(n).transform;
        t.SetParent(padre, true);
        t.position = Mundo.EnSuelo(sitio.TransformPoint(new Vector3(x, 0, z)));
        t.rotation = sitio.rotation * Quaternion.Euler(0, giro, 0);
        return t;
    }

    static void Brillo(Transform t, Color c)
    {
        GameObject g = new GameObject("Brillo");
        g.transform.SetParent(t, false);
        g.transform.localPosition = Vector3.up * 0.5f;
        Light l = g.AddComponent<Light>();
        l.type = LightType.Point; l.color = c; l.range = 2.2f; l.intensity = 0.8f; l.shadows = LightShadows.None;
    }

    internal static void Musica(Transform sitio, MusicaSitio.Tipo tipo, float vol, float dist)
    {
        GameObject g = new GameObject("Musica_" + tipo);
        g.transform.SetParent(sitio, false);
        g.transform.localPosition = Vector3.up * 2f;
        MusicaSitio m = g.AddComponent<MusicaSitio>();
        m.tipo = tipo; m.volumen = vol; m.distanciaMax = dist;
    }

    static ObjetoEmbrujado Embrujar(Transform t, ObjetoEmbrujado.Movimiento mov, float radio, bool sinVerlo = true, Vector3? dir = null)
    {
        ObjetoEmbrujado o = t.gameObject.AddComponent<ObjetoEmbrujado>();
        o.movimiento = mov; o.radio = radio; o.soloSinVerlo = sinVerlo;
        if (dir.HasValue) o.direccion = dir.Value;
        return o;
    }

    // ---- Recogibles

    internal static Curacion Botiquin(Transform padre, Transform sitio, float x, float z, string tipo = "Botiquín")
    {
        Transform t = Pivote(padre, "Curacion_" + tipo, sitio, x, z, Random.Range(0f, 360f));
        Material blanco = Mat("Botiquin", new Color(0.85f, 0.85f, 0.8f));
        Material rojo = Mat("CruzRoja", new Color(0.7f, 0.05f, 0.05f));
        float cantidad = 40f;
        if (tipo == "Botiquín")
        {
            Caja(t, "Caja", new Vector3(0, 0.12f, 0), new Vector3(0.45f, 0.24f, 0.3f), blanco, false);
            Caja(t, "CruzV", new Vector3(0, 0.12f, -0.155f), new Vector3(0.06f, 0.16f, 0.01f), rojo, false);
            Caja(t, "CruzH", new Vector3(0, 0.12f, -0.155f), new Vector3(0.16f, 0.06f, 0.01f), rojo, false);
            cantidad = 55f;
        }
        else if (tipo == "Vendas")
        {
            Cil(t, "Rollo1", new Vector3(0, 0.06f, 0), new Vector3(0.12f, 0.06f, 0.12f), blanco);
            Cil(t, "Rollo2", new Vector3(0.14f, 0.06f, 0.03f), new Vector3(0.12f, 0.06f, 0.12f), blanco);
            cantidad = 30f;
        }
        else // Agua
        {
            Cil(t, "Botella", new Vector3(0, 0.16f, 0), new Vector3(0.09f, 0.16f, 0.09f), Mat("AguaAzul", new Color(0.3f, 0.5f, 0.8f)));
            Cil(t, "Tapa", new Vector3(0, 0.34f, 0), new Vector3(0.05f, 0.02f, 0.05f), Mat("TapaBlanca", Color.white));
            cantidad = 20f;
        }
        Brillo(t, new Color(0.9f, 0.95f, 1f));
        BoxCollider bc = t.gameObject.AddComponent<BoxCollider>();
        bc.isTrigger = true; bc.size = new Vector3(0.7f, 0.6f, 0.7f); bc.center = new Vector3(0, 0.25f, 0);
        Curacion c = t.gameObject.AddComponent<Curacion>();
        c.nombre = tipo; c.cantidad = cantidad;
        return c;
    }

    internal static ArmaRecogible Arma(Transform padre, Transform sitio, float x, float z, string tipo)
    {
        Transform t = Pivote(padre, "Arma_" + tipo, sitio, x, z, Random.Range(0f, 360f));
        Transform modelo = new GameObject("Modelo").transform;
        modelo.SetParent(t, false);
        Material madera = Mat("MangoMadera", new Color(0.35f, 0.24f, 0.14f));
        Material fierro = Mat("MetalOxidado", new Color(0.25f, 0.18f, 0.14f));
        float danio = 55f, alcance = 1.6f; int usos = 6;
        if (tipo == "Pala")
        {
            Cil(modelo, "Mango", new Vector3(0, 0.55f, 0), new Vector3(0.04f, 0.55f, 0.04f), madera);
            Caja(modelo, "Hoja", new Vector3(0, -0.08f, 0), new Vector3(0.24f, 0.3f, 0.03f), fierro, false);
            danio = 60f; alcance = 1.75f; usos = 6;
            modelo.localRotation = Quaternion.Euler(0, 0, 70f); modelo.localPosition = new Vector3(0, 0.15f, 0);
        }
        else if (tipo == "Tubo")
        {
            Cil(modelo, "Tubo", new Vector3(0, 0.45f, 0), new Vector3(0.05f, 0.45f, 0.05f), fierro);
            danio = 45f; alcance = 1.6f; usos = 8;
            modelo.localRotation = Quaternion.Euler(0, 0, 85f); modelo.localPosition = new Vector3(0, 0.06f, 0);
        }
        else if (tipo == "Hacha")
        {
            Cil(modelo, "Mango", new Vector3(0, 0.4f, 0), new Vector3(0.04f, 0.4f, 0.04f), madera);
            Caja(modelo, "Cabeza", new Vector3(0.07f, 0.74f, 0), new Vector3(0.2f, 0.14f, 0.04f), fierro, false);
            danio = 80f; alcance = 1.6f; usos = 5;
            modelo.localRotation = Quaternion.Euler(0, 0, 80f); modelo.localPosition = new Vector3(0, 0.06f, 0);
        }
        else if (tipo == "Machete")
        {
            Cil(modelo, "Mango", new Vector3(0, 0.08f, 0), new Vector3(0.035f, 0.08f, 0.035f), Mat("MangoNegro", new Color(0.06f, 0.05f, 0.05f)));
            Caja(modelo, "Hoja", new Vector3(0, 0.4f, 0), new Vector3(0.06f, 0.5f, 0.012f), fierro, false);
            danio = 65f; alcance = 1.5f; usos = 9;
            modelo.localRotation = Quaternion.Euler(0, 0, 85f); modelo.localPosition = new Vector3(0, 0.04f, 0);
        }
        else if (tipo == "Varilla")
        {
            Cil(modelo, "Varilla", new Vector3(0, 0.55f, 0), new Vector3(0.025f, 0.55f, 0.025f), fierro);
            danio = 40f; alcance = 1.9f; usos = 10;
            modelo.localRotation = Quaternion.Euler(0, 0, 85f); modelo.localPosition = new Vector3(0, 0.04f, 0);
        }
        else // Crucifijo: pega poco pero muchos usos
        {
            Caja(modelo, "Palo", new Vector3(0, 0.25f, 0), new Vector3(0.05f, 0.5f, 0.04f), madera, false);
            Caja(modelo, "Travesaño", new Vector3(0, 0.35f, 0), new Vector3(0.26f, 0.05f, 0.04f), madera, false);
            danio = 40f; alcance = 1.4f; usos = 12;
            modelo.localRotation = Quaternion.Euler(0, 0, 80f); modelo.localPosition = new Vector3(0, 0.05f, 0);
        }
        Brillo(t, new Color(1f, 0.85f, 0.6f));
        BoxCollider bc = t.gameObject.AddComponent<BoxCollider>();
        bc.isTrigger = true; bc.size = new Vector3(1.2f, 0.6f, 1.2f); bc.center = new Vector3(0, 0.25f, 0);
        ArmaRecogible a = t.gameObject.AddComponent<ArmaRecogible>();
        a.nombre = tipo; a.danio = danio; a.alcance = alcance; a.usos = usos; a.modeloMano = modelo.gameObject;
        return a;
    }

    static GameObject ModeloPanteon(string n, Transform padre, Vector3 pos, float giro)
    {
        GameObject pf = AssetDatabase.LoadAssetAtPath<GameObject>(Panteon + "/FBX/" + n + ".fbx");
        if (pf == null) return new GameObject(n);
        GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(pf);
        PrefabUtility.UnpackPrefabInstance(g, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        g.transform.SetParent(padre, true);
        g.transform.position = Mundo.EnSuelo(pos, -0.03f);
        g.transform.rotation = Quaternion.Euler(0, giro, 0) * g.transform.rotation;
        Material m = AssetDatabase.LoadAssetAtPath<Material>(CarpetaMat + "/Panteon_" + n + ".mat");
        if (m != null) foreach (Renderer r in g.GetComponentsInChildren<Renderer>()) r.sharedMaterial = m;
        return g;
    }

    // ================================================================== Lugares

    static void Campamento(SitioCampamento s, Transform d)
    {
        Transform t = s.transform;
        Musica(t, MusicaSitio.Tipo.Fogata, 0.5f, 18f);
        Musica(t, MusicaSitio.Tipo.CajaMusical, 0.35f, 45f);

        Material lona = Mat("BolsaDormir", new Color(0.2f, 0.3f, 0.15f));
        Material roja = Mat("MochilaRoja", new Color(0.45f, 0.08f, 0.06f));
        Material sangre = Mat("ManchaOscura", new Color(0.12f, 0.01f, 0.01f));
        // Bolsas de dormir abiertas, mochilas tiradas, una mancha oscura, linterna de gas colgando
        Caja(d, "BolsaDormir1", t.InverseTransformPoint(Mundo.EnSuelo(t.TransformPoint(new Vector3(-1.5f, 0, 2.5f)))) + Vector3.up * 0.06f, new Vector3(0.8f, 0.12f, 1.9f), lona, false).localRotation = Quaternion.Euler(0, 25f, 0);
        Caja(d, "BolsaDormir2", t.InverseTransformPoint(Mundo.EnSuelo(t.TransformPoint(new Vector3(2.5f, 0, 2.8f)))) + Vector3.up * 0.06f, new Vector3(0.8f, 0.12f, 1.9f), lona, false).localRotation = Quaternion.Euler(0, -40f, 0);
        Caja(d, "Mancha", t.InverseTransformPoint(Mundo.EnSuelo(t.TransformPoint(new Vector3(2.8f, 0, -3.8f)))) + Vector3.up * 0.02f, new Vector3(1.4f, 0.01f, 0.9f), sangre, false);

        Transform mochila = Pivote(d, "MochilaTirada", t, -2.8f, -3.2f, 30f);
        Caja(mochila, "Cuerpo", new Vector3(0, 0.3f, 0), new Vector3(0.45f, 0.6f, 0.3f), roja);
        Embrujar(mochila, ObjetoEmbrujado.Movimiento.Caer, 7f);

        Transform silla = Pivote(d, "SillaPlegable", t, 1.2f, 2.2f, -20f);
        Material metal = Mat("MetalOxidado", new Color(0.25f, 0.18f, 0.14f));
        Caja(silla, "Asiento", new Vector3(0, 0.45f, 0), new Vector3(0.5f, 0.05f, 0.45f), lona);
        Caja(silla, "Respaldo", new Vector3(0, 0.75f, -0.22f), new Vector3(0.5f, 0.6f, 0.05f), lona);
        Caja(silla, "Pata1", new Vector3(-0.22f, 0.22f, 0), new Vector3(0.04f, 0.45f, 0.04f), metal, false);
        Caja(silla, "Pata2", new Vector3(0.22f, 0.22f, 0), new Vector3(0.04f, 0.45f, 0.04f), metal, false);
        Embrujar(silla, ObjetoEmbrujado.Movimiento.Caer, 8f, true, Vector3.back);

        // Linterna de gas colgando de un palo: se mece sola
        Transform palo = Pivote(d, "PaloLinterna", t, -1f, -1.2f);
        Cil(palo, "Palo", new Vector3(0, 1.1f, 0), new Vector3(0.06f, 1.1f, 0.06f), Mat("Durmiente", new Color(0.18f, 0.13f, 0.09f)), true);
        Transform gancho = new GameObject("Gancho").transform;
        gancho.SetParent(palo, false); gancho.localPosition = new Vector3(0.35f, 2.1f, 0);
        Caja(gancho, "Brazo", new Vector3(-0.17f, 0.02f, 0), new Vector3(0.38f, 0.04f, 0.04f), metal, false);
        Cil(gancho, "Farol", new Vector3(0, -0.3f, 0), new Vector3(0.14f, 0.13f, 0.14f), Mat("FarolVidrio", new Color(1f, 0.8f, 0.5f), true));
        GameObject lf = new GameObject("LuzFarol"); lf.transform.SetParent(gancho, false); lf.transform.localPosition = new Vector3(0, -0.3f, 0);
        Light l = lf.AddComponent<Light>(); l.type = LightType.Point; l.color = new Color(1f, 0.75f, 0.4f); l.range = 6f; l.intensity = 1.2f;
        lf.AddComponent<ParpadeoFuerte>().encendido = 2.5f;
        ObjetoEmbrujado o = Embrujar(gancho, ObjetoEmbrujado.Movimiento.Mecer, 10f, false, Vector3.forward);
        o.rearmar = 25f;

        Botiquin(d, t, -4.5f, 3.5f, "Botiquín");
        Botiquin(d, t, 4.8f, -1.5f, "Agua");
        Arma(d, t, 0.6f, 3.6f, "Tubo");
    }

    static void PanteonExtra(SitioPanteon s, Transform d)
    {
        Transform t = s.transform;
        Musica(t, MusicaSitio.Tipo.Campanillas, 0.6f, 55f);
        // Lápidas que se ladean solas, una pala que se cae
        for (int i = 0; i < 3; i++)
        {
            Transform p = Pivote(d, "LapidaInquieta_" + i, t, -6f + i * 6f, -6.5f + (i % 2) * 2f, 180f);
            GameObject m = ModeloPanteon("Gravestone_Rounded_01", p, p.position, p.eulerAngles.y);
            Embrujar(p, ObjetoEmbrujado.Movimiento.Ladear, 6f, true, Vector3.forward);
        }
        Transform pala = Pivote(d, "PalaApoyada", t, 4.5f, 3f, 90f);
        Cil(pala, "Mango", new Vector3(0, 0.8f, 0), new Vector3(0.04f, 0.6f, 0.04f), Mat("MangoMadera", new Color(0.35f, 0.24f, 0.14f)));
        pala.localRotation *= Quaternion.Euler(0, 0, 12f);
        Embrujar(pala, ObjetoEmbrujado.Movimiento.Caer, 6f);

        Botiquin(d, t, -8.5f, 6.5f, "Vendas");
        Botiquin(d, t, 8.5f, -6f, "Botiquín");
        Arma(d, t, 3f, 7.8f, "Pala");      // ¡la pala del panteón!
    }

    static void Tunel(SitioTunel s)
    {
        TunelLineaK tunel = s.tunel;
        if (tunel == null || tunel.interior == null) return;
        Transform I = tunel.interior;
        Transform d = new GameObject("Detalles").transform;
        d.SetParent(I, false);
        Musica(I, MusicaSitio.Tipo.Zumbido, 0.4f, 30f);

        Material madera = Mat("Durmiente", new Color(0.18f, 0.13f, 0.09f));
        Material metal = Mat("MetalOxidado", new Color(0.25f, 0.18f, 0.14f));
        // Silla de la caseta que se arrastra sola, carrito de herramientas que tiembla, casco tirado
        Transform silla = new GameObject("SillaCaseta").transform;
        silla.SetParent(d, false); silla.localPosition = new Vector3(-3.6f, 1.0f, -8.6f);
        Caja(silla, "Asiento", new Vector3(0, 0.45f, 0), new Vector3(0.45f, 0.06f, 0.45f), madera);
        Caja(silla, "Respaldo", new Vector3(-0.2f, 0.75f, 0), new Vector3(0.06f, 0.6f, 0.45f), madera, false);
        Embrujar(silla, ObjetoEmbrujado.Movimiento.Deslizar, 5f, true, Vector3.right);

        Transform carro = new GameObject("CarritoHerramientas").transform;
        carro.SetParent(d, false); carro.localPosition = new Vector3(2.15f, 0.2f, 5f);
        Caja(carro, "Caja", new Vector3(0, 0.3f, 0), new Vector3(1f, 0.6f, 1.4f), metal);
        Embrujar(carro, ObjetoEmbrujado.Movimiento.Deslizar, 9f, false, Vector3.back).rearmar = 0f;

        Transform casco = new GameObject("Casco").transform;
        casco.SetParent(d, false); casco.localPosition = new Vector3(-1.4f, 1.0f, -12f);
        GameObject esf = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Object.DestroyImmediate(esf.GetComponent<Collider>());
        esf.transform.SetParent(casco, false); esf.transform.localScale = new Vector3(0.32f, 0.2f, 0.36f); esf.transform.localPosition = Vector3.up * 0.1f;
        esf.GetComponent<Renderer>().sharedMaterial = Mat("CascoAmarillo", new Color(0.7f, 0.55f, 0.1f));
        Embrujar(casco, ObjetoEmbrujado.Movimiento.Temblar, 4f, false);

        // Curación y arma dentro de la estación
        Transform c1 = new GameObject("ref").transform; c1.SetParent(I, false); c1.localPosition = new Vector3(-3.2f, 1f, 3f);
        Botiquin(d, c1, 0, 0, "Botiquín").transform.position = I.TransformPoint(new Vector3(-3.2f, 1f, 3f));
        Arma(d, c1, 0, 0, "Tubo").transform.position = I.TransformPoint(new Vector3(-4f, 1f, -14f));
        Object.DestroyImmediate(c1.gameObject);
    }

    static void Capilla(SitioIglesia s, Transform d)
    {
        Transform t = s.transform;
        Musica(t, MusicaSitio.Tipo.Organo, 0.45f, 40f);
        Material madera = Mat("Durmiente", new Color(0.18f, 0.13f, 0.09f));
        Material cera = Mat("Cera", new Color(0.85f, 0.82f, 0.7f));
        // Una banca que se arrastra cuando no la ves, un cirio que se cae, un libro que tiembla en el atril
        Transform banca = new GameObject("BancaInquieta").transform;
        banca.SetParent(d, false); banca.localPosition = new Vector3(-1.2f, 0, 2.5f);
        Caja(banca, "Asiento", new Vector3(0, 0.45f, 0), new Vector3(1.8f, 0.1f, 0.5f), madera);
        Embrujar(banca, ObjetoEmbrujado.Movimiento.Deslizar, 6f, true, Vector3.right);

        Transform cirio = new GameObject("CirioSuelto").transform;
        cirio.SetParent(d, false); cirio.localPosition = new Vector3(1.8f, 0, -2f);
        Cil(cirio, "Cirio", new Vector3(0, 0.6f, 0), new Vector3(0.1f, 0.6f, 0.1f), cera, true);
        Embrujar(cirio, ObjetoEmbrujado.Movimiento.Caer, 4f, true, Vector3.left);

        Transform atril = new GameObject("Atril").transform;
        atril.SetParent(d, false); atril.localPosition = new Vector3(1.6f, 0, 0.5f);
        Caja(atril, "Pie", new Vector3(0, 0.55f, 0), new Vector3(0.12f, 1.1f, 0.12f), madera);
        Caja(atril, "Libro", new Vector3(0, 1.15f, 0), new Vector3(0.45f, 0.06f, 0.35f), Mat("LibroRojo", new Color(0.35f, 0.05f, 0.05f)), false).localRotation = Quaternion.Euler(-20f, 0, 0);
        Embrujar(atril, ObjetoEmbrujado.Movimiento.Temblar, 5f, false).rearmar = 20f;

        Transform cruz = t.Find("Cruz_V");
        if (cruz != null)
        {
            Transform piv = new GameObject("CruzQueGira").transform;
            piv.SetParent(t, false); piv.position = cruz.position;
            Transform h = t.Find("Cruz_H");
            cruz.SetParent(piv, true); if (h != null) h.SetParent(piv, true);
            Embrujar(piv, ObjetoEmbrujado.Movimiento.Ladear, 5f, true, Vector3.right).direccion = Vector3.right;
        }

        Botiquin(d, t, -2.5f, 5f, "Vendas").transform.SetParent(d, true);
        Arma(d, t, 2.4f, -3f, "Crucifijo");
        Botiquin(d, t, 3f, 9f, "Agua");
    }

    static void Casa(SitioCasa s, Transform d)
    {
        Transform t = s.transform;
        Musica(t, MusicaSitio.Tipo.CajaMusical, 0.4f, 30f);
        Material madera = Mat("Durmiente", new Color(0.18f, 0.13f, 0.09f));
        // Mecedora que se mece sola en el cuarto de la niña, un cuadro que se ladea, una silla que se arrastra
        Transform mec = new GameObject("Mecedora").transform;
        mec.SetParent(d, false); mec.localPosition = new Vector3(-3.4f, 0.38f, 2.3f);
        Caja(mec, "Asiento", new Vector3(0, 0.45f, 0), new Vector3(0.5f, 0.06f, 0.5f), madera);
        Caja(mec, "Respaldo", new Vector3(0, 0.85f, -0.24f), new Vector3(0.5f, 0.8f, 0.05f), madera);
        Caja(mec, "Patin", new Vector3(0, 0.03f, 0), new Vector3(0.5f, 0.05f, 0.8f), madera, false);
        Embrujar(mec, ObjetoEmbrujado.Movimiento.Mecer, 6f, false, Vector3.forward).rearmar = 6f;

        Transform cuadro = new GameObject("CuadroFamilia").transform;
        cuadro.SetParent(d, false); cuadro.localPosition = new Vector3(0f, 2.2f, 3.02f);
        Caja(cuadro, "Marco", Vector3.zero, new Vector3(0.7f, 0.5f, 0.04f), madera, false);
        Embrujar(cuadro, ObjetoEmbrujado.Movimiento.Ladear, 5f, true, Vector3.right);

        Transform silla = new GameObject("SillaQueSeArrastra").transform;
        silla.SetParent(d, false); silla.localPosition = new Vector3(0.6f, 0.38f, 0.6f);
        Caja(silla, "Asiento", new Vector3(0, 0.45f, 0), new Vector3(0.45f, 0.06f, 0.45f), madera);
        Caja(silla, "Respaldo", new Vector3(0, 0.8f, -0.2f), new Vector3(0.45f, 0.7f, 0.05f), madera);
        Embrujar(silla, ObjetoEmbrujado.Movimiento.Deslizar, 5f, true, Vector3.left);

        // Recogibles: adentro y en el patio
        Arma(d, t, 8.0f, 4.2f, "Hacha");                     // junto a la leña
        ArmaRecogible m = Arma(d, t, -2.6f, -3.6f, "Machete"); // en la cocina
        m.transform.position += Vector3.up * 0.38f;
        Curacion v = Botiquin(d, t, 3.4f, 2.3f, "Vendas"); v.transform.position += Vector3.up * 1.0f;   // sobre el buró
        Botiquin(d, t, -6f, 5f, "Agua");
    }

    static void Mirador(SitioMirador s, Transform d)
    {
        Transform t = s.transform;
        Musica(t, MusicaSitio.Tipo.Coro, 0.55f, 60f);
        Transform banca = t.Find("Banca");
        if (banca != null)
        {
            Transform piv = new GameObject("BancaQueRechina").transform;
            piv.SetParent(t, false); piv.position = banca.position;
            banca.SetParent(piv, true);
            Embrujar(piv, ObjetoEmbrujado.Movimiento.Temblar, 6f, false).rearmar = 15f;
        }
        // Binoculares de moneda que giran solos hacia ti
        Transform bin = Pivote(d, "Binoculares", t, 1.8f, 1.2f);
        Material metal = Mat("MetalVerde", new Color(0.18f, 0.24f, 0.2f));
        Cil(bin, "Poste", new Vector3(0, 0.6f, 0), new Vector3(0.1f, 0.6f, 0.1f), metal, true);
        Caja(bin, "Cabeza", new Vector3(0, 1.3f, 0.1f), new Vector3(0.35f, 0.25f, 0.45f), metal, false);
        Embrujar(bin, ObjetoEmbrujado.Movimiento.Girar, 7f, true);

        Botiquin(d, t, -1.5f, -1.8f, "Botiquín");
        Botiquin(d, t, 1.8f, -1.5f, "Agua");
        Arma(d, t, -2.2f, 0.8f, "Varilla");
    }
}
