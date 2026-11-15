using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menú 10: arma el escenario del guion en la escena abierta.
/// - Arregla la cámara: apaga la cámara/luces "PS2_" que traía el modelo del guía.
/// - Linterna tirada y parpadeando junto al jugador (Acto I).
/// - Luz fría en el sendero donde saluda el Guía.
/// - Muro de concreto + marca + puerta EXPEDIENTE 08, y el andén subterráneo (Acto II).
/// - Mirador con baliza roja en un punto alto (Acto III).
/// - Fecha de la cinta: 27/11/1998.
/// Se puede correr varias veces: reemplaza lo que ya había creado.
/// </summary>
public static class MonterreyHistoriaSetup
{
    const string CarpetaTex = "Assets/Escenarios/Texturas";
    const string CarpetaMat = "Assets/Escenarios/Materiales";

    [MenuItem("Monterrey/10. Construir escenarios del guion (linterna, túnel, mirador)")]
    public static void Construir()
    {
        PlayerMovement pm = Object.FindAnyObjectByType<PlayerMovement>();
        Terrain terreno = Terrain.activeTerrain;
        if (pm == null || terreno == null) { EditorUtility.DisplayDialog("Monterrey", "Falta el jugador o el terreno en la escena.", "OK"); return; }

        ArreglarCamaras();
        ConfigurarTexturas();

        GameObject raiz = GameObject.Find("_EscenariosGuion");
        if (raiz != null) Object.DestroyImmediate(raiz);
        raiz = new GameObject("_EscenariosGuion");

        Vector3 inicio = pm.transform.position;
        Vector3 adelante = pm.transform.forward; adelante.y = 0; adelante.Normalize();

        Vector3 mirador = BuscarMirador(terreno, inicio);
        Vector3 haciaMirador = mirador - inicio; haciaMirador.y = 0; haciaMirador.Normalize();
        Vector3 tunelPos = BuscarLadera(terreno, inicio, haciaMirador, out Vector3 normalLadera);
        Vector3 sendero = Mundo.EnSuelo(Vector3.Lerp(inicio, tunelPos, 0.4f));

        Transform linterna = ColocarLinterna(inicio, adelante);
        Transform puntoSendero = CrearSendero(raiz.transform, sendero);
        TunelLineaK tunel = CrearTunel(raiz.transform, tunelPos, normalLadera, terreno);
        Transform mir = CrearMirador(raiz.transform, mirador, inicio);

        // Fecha y etiqueta de la cinta
        VHSOverlay vhs = Object.FindAnyObjectByType<VHSOverlay>();
        if (vhs != null)
        {
            vhs.fechaInicial = "1998-11-27 23:47:00";
            vhs.etiquetaCinta = "CINTA RECUPERADA // SECTOR LA PASTORA";
            EditorUtility.SetDirty(vhs);
        }

        // Conectar con la historia
        MisionManager mm = Object.FindAnyObjectByType<MisionManager>();
        if (mm != null)
        {
            mm.linternaEnSuelo = linterna;
            mm.puntoSendero = puntoSendero;
            mm.tunel = tunel;
            mm.mirador = mir;
            EditorUtility.SetDirty(mm);
        }
        AcechoDirector dir = Object.FindAnyObjectByType<AcechoDirector>();
        if (dir != null) { dir.habilitado = false; EditorUtility.SetDirty(dir); }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[Monterrey] Escenarios listos. Sendero {sendero}, túnel {tunelPos}, mirador {mirador} " +
                  $"(a {Vector3.Distance(inicio, mirador):0} m del inicio).");
    }

    // ================================================================== Cámara

    static void ArreglarCamaras()
    {
        foreach (Camera c in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include))
        {
            if (c.GetComponentInParent<CameraController>() != null || c.targetTexture != null) continue;
            c.enabled = false;
            if (c.CompareTag("MainCamera")) c.tag = "Untagged";
            AudioListener a = c.GetComponent<AudioListener>();
            if (a != null) a.enabled = false;
            EditorUtility.SetDirty(c);
            Debug.Log("[Monterrey] Cámara extra apagada: " + c.name);
        }
        foreach (Light l in Object.FindObjectsByType<Light>(FindObjectsInactive.Include))
            if (l.name.StartsWith("PS2_")) { l.enabled = false; EditorUtility.SetDirty(l); Debug.Log("[Monterrey] Luz de vitrina apagada: " + l.name); }

        CameraController cc = Object.FindAnyObjectByType<CameraController>();
        if (cc != null)
        {
            Camera mia = cc.GetComponentInChildren<Camera>();
            if (mia != null) { mia.tag = "MainCamera"; mia.enabled = true; EditorUtility.SetDirty(mia); }
        }
    }

    // ================================================================== Búsqueda de lugares en el terreno

    static float Pendiente(Terrain t, Vector3 p)
    {
        Vector3 l = p - t.transform.position;
        return t.terrainData.GetSteepness(l.x / t.terrainData.size.x, l.z / t.terrainData.size.z);
    }

    static Vector3 Normal(Terrain t, Vector3 p)
    {
        Vector3 l = p - t.transform.position;
        return t.terrainData.GetInterpolatedNormal(l.x / t.terrainData.size.x, l.z / t.terrainData.size.z);
    }

    static bool DentroDelTerreno(Terrain t, Vector3 p, float margen = 15f)
    {
        Vector3 l = p - t.transform.position;
        return l.x > margen && l.z > margen && l.x < t.terrainData.size.x - margen && l.z < t.terrainData.size.z - margen;
    }

    /// <summary>Un punto alto, caminable (pendiente suave) y no demasiado lejos.</summary>
    static Vector3 BuscarMirador(Terrain t, Vector3 inicio)
    {
        Vector3 mejor = Mundo.EnSuelo(inicio + Vector3.forward * 120f);
        float mejorPuntaje = float.MinValue;
        for (float r = 90f; r <= 200f; r += 10f)
            for (float a = 0; a < 360f; a += 7.5f)
            {
                Vector3 p = inicio + Quaternion.Euler(0, a, 0) * Vector3.forward * r;
                if (!DentroDelTerreno(t, p)) continue;
                p = Mundo.EnSuelo(p);
                if (Pendiente(t, p) > 22f) continue;
                float puntaje = (p.y - inicio.y) - r * 0.08f;
                if (puntaje > mejorPuntaje) { mejorPuntaje = puntaje; mejor = p; }
            }
        return mejor;
    }

    /// <summary>Una ladera entre 40 y 75 m, de preferencia hacia el mirador, para meter el muro del túnel.</summary>
    static Vector3 BuscarLadera(Terrain t, Vector3 inicio, Vector3 haciaMirador, out Vector3 normal)
    {
        Vector3 mejor = Mundo.EnSuelo(inicio + haciaMirador * 55f);
        normal = -haciaMirador;
        float mejorPuntaje = float.MinValue;
        for (float r = 40f; r <= 75f; r += 5f)
            for (float a = -80f; a <= 80f; a += 8f)
            {
                Vector3 p = inicio + Quaternion.Euler(0, a, 0) * haciaMirador * r;
                if (!DentroDelTerreno(t, p)) continue;
                p = Mundo.EnSuelo(p);
                float s = Pendiente(t, p);
                if (s < 15f || s > 42f) continue;
                Vector3 n = Normal(t, p); n.y = 0;
                // que la cara del muro mire más o menos hacia donde viene el jugador
                float mira = Vector3.Dot(n.normalized, (inicio - p).normalized);
                float puntaje = mira * 2f - Mathf.Abs(a) / 80f - Mathf.Abs(s - 28f) / 20f;
                if (puntaje > mejorPuntaje) { mejorPuntaje = puntaje; mejor = p; normal = n.normalized; }
            }
        if (normal.sqrMagnitude < 0.01f) normal = -haciaMirador;
        return mejor;
    }

    // ================================================================== Acto I

    static Transform ColocarLinterna(Vector3 inicio, Vector3 adelante)
    {
        GameObject f = GameObject.Find("Flashlight");
        if (f == null) { Debug.LogWarning("[Monterrey] No encontré el objeto 'Flashlight'."); return null; }
        Undo.RecordObject(f.transform, "Linterna");
        Vector3 p = Mundo.EnSuelo(inicio + adelante * 1.8f + Vector3.Cross(Vector3.up, adelante) * 0.6f, 0.06f);
        f.transform.position = p;
        f.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 90f);

        Transform viejo = f.transform.Find("LuzParpadeante");
        if (viejo != null) Object.DestroyImmediate(viejo.gameObject);
        GameObject luz = new GameObject("LuzParpadeante");
        luz.transform.SetParent(f.transform, false);
        luz.transform.position = p + Vector3.up * 0.25f;
        Light l = luz.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.9f, 0.7f);
        l.range = 4.5f;
        l.intensity = 2.2f;
        ParpadeoFuerte pf = luz.AddComponent<ParpadeoFuerte>();
        pf.encendido = 0.3f; pf.apagado = 0.35f;
        return f.transform;
    }

    static Transform CrearSendero(Transform raiz, Vector3 p)
    {
        GameObject g = new GameObject("Sendero_LuzFria");
        g.transform.SetParent(raiz);
        g.transform.position = p + Vector3.up * 1.6f;
        Light l = g.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(0.6f, 0.75f, 1f);
        l.range = 9f;
        l.intensity = 1.6f;
        g.AddComponent<Parpadeo>();
        return g.transform;
    }

    // ================================================================== Acto II: Túnel Línea K

    static TunelLineaK CrearTunel(Transform raiz, Vector3 pos, Vector3 normal, Terrain terreno)
    {
        Material concreto = Mat("Concreto", Tex("Concreto"), new Color(0.75f, 0.74f, 0.7f), new Vector2(2, 1));
        Material puertaMat = Mat("PuertaExpediente08", Tex("PuertaExpediente08"), Color.white, Vector2.one);
        Material marcaMat = Mat("MarcaWatcher", Tex("MarcaWatcher"), Color.white, Vector2.one, true);
        Material oscuro = Mat("ConcretoOscuro", Tex("Concreto"), new Color(0.32f, 0.32f, 0.3f), new Vector2(6, 2));
        Material metal = Mat("MetalOxidado", null, new Color(0.25f, 0.18f, 0.14f), Vector2.one);
        Material madera = Mat("Durmiente", null, new Color(0.18f, 0.13f, 0.09f), Vector2.one);
        Material negro = Mat("Silueta", null, new Color(0.01f, 0.01f, 0.01f), Vector2.one);
        Material arbusto = Mat("Maleza", null, new Color(0.08f, 0.13f, 0.06f), Vector2.one);

        GameObject t = new GameObject("TunelLineaK");
        t.transform.SetParent(raiz);
        t.transform.position = pos;
        t.transform.rotation = Quaternion.LookRotation(normal); // forward = cara del muro (hacia afuera)
        TunelLineaK tunel = t.AddComponent<TunelLineaK>();

        // ---- Exterior: muro, marca, puerta, maleza
        Transform muro = Caja(t.transform, "Muro", new Vector3(0, 1.4f, -0.4f), new Vector3(7f, 5f, 1.6f), concreto);
        tunel.muro = muro;

        GameObject marca = GameObject.CreatePrimitive(PrimitiveType.Quad);
        marca.name = "Marca_Pintada";
        Object.DestroyImmediate(marca.GetComponent<Collider>());
        marca.transform.SetParent(t.transform, false);
        marca.transform.localPosition = new Vector3(-1.6f, 1.7f, 0.42f);
        marca.transform.localRotation = Quaternion.Euler(0, 180f, 0);
        marca.transform.localScale = Vector3.one * 1.9f;
        marca.GetComponent<Renderer>().sharedMaterial = marcaMat;
        tunel.marca = marca.transform;

        Transform puerta = Caja(t.transform, "Puerta_Expediente08", new Vector3(1.5f, 1.15f, 0.48f), new Vector3(1.3f, 2.3f, 0.12f), puertaMat);
        tunel.puertaExterior = puerta.gameObject;
        Transform afuera = Punto(t.transform, "PuntoAfuera", new Vector3(1.5f, 0f, 2.4f), 0f);
        afuera.position = Mundo.EnSuelo(afuera.position, 0.1f);

        for (int i = 0; i < 7; i++)
        {
            Vector3 lp = new Vector3(Random.Range(-4.5f, 4.5f), 0, Random.Range(0.6f, 2.5f));
            if (Mathf.Abs(lp.x - 1.5f) < 1.2f) lp.x += 2.4f * Mathf.Sign(lp.x - 1.5f + 0.01f);
            GameObject m = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            m.name = "Maleza";
            Object.DestroyImmediate(m.GetComponent<Collider>());
            m.transform.SetParent(t.transform, false);
            m.transform.localPosition = lp;
            m.transform.position = Mundo.EnSuelo(m.transform.position, 0.2f);
            m.transform.localScale = new Vector3(Random.Range(0.9f, 1.8f), Random.Range(0.6f, 1.1f), Random.Range(0.9f, 1.8f));
            m.GetComponent<Renderer>().sharedMaterial = arbusto;
        }

        // ---- Interior: andén bajo tierra
        float fondo = terreno.transform.position.y - 60f;
        GameObject ini = new GameObject("Interior_Anden");
        ini.transform.SetParent(t.transform, true);
        ini.transform.position = new Vector3(pos.x, fondo, pos.z);
        ini.transform.rotation = Quaternion.identity;
        Transform I = ini.transform;
        tunel.interior = I;

        // Piso, techo, paredes (40 x 10 x 5)
        Caja(I, "Piso", new Vector3(0, -0.25f, 0), new Vector3(10, 0.5f, 40), oscuro);
        Caja(I, "Techo", new Vector3(0, 5.25f, 0), new Vector3(10, 0.5f, 40), oscuro);
        Caja(I, "Pared_Izq", new Vector3(-5.25f, 2.5f, 0), new Vector3(0.5f, 5, 40), concreto);
        Caja(I, "Pared_Der", new Vector3(5.25f, 2.5f, 0), new Vector3(0.5f, 5, 40), concreto);
        Caja(I, "Pared_Inicio", new Vector3(0, 2.5f, -20.25f), new Vector3(10, 5, 0.5f), concreto);
        Caja(I, "Pared_Fondo", new Vector3(0, 2.5f, 20.25f), new Vector3(10, 5, 0.5f), oscuro);

        // Andén (sin terminar: se corta en z = 6)
        Caja(I, "Anden", new Vector3(-2.75f, 0.5f, -7f), new Vector3(4.5f, 1f, 26f), concreto);
        for (int i = 0; i < 6; i++)
            Caja(I, "Varilla", new Vector3(-2.75f + Random.Range(-1.8f, 1.8f), 1.3f, 6.2f), new Vector3(0.04f, 0.6f, 0.04f), metal)
                .localRotation = Quaternion.Euler(Random.Range(-25f, 25f), 0, Random.Range(-25f, 25f));
        Caja(I, "Costales", new Vector3(-4f, 1.25f, 4f), new Vector3(1f, 0.5f, 0.7f), Mat("Costal", null, new Color(0.45f, 0.42f, 0.36f), Vector2.one));

        // Vías
        Caja(I, "Riel_A", new Vector3(1.4f, 0.08f, 0), new Vector3(0.08f, 0.16f, 40), metal);
        Caja(I, "Riel_B", new Vector3(2.9f, 0.08f, 0), new Vector3(0.08f, 0.16f, 40), metal);
        for (float z = -19.5f; z < 20f; z += 1.1f)
        {
            Transform d = Caja(I, "Durmiente", new Vector3(2.15f, 0.03f, z), new Vector3(2.4f, 0.06f, 0.25f), madera);
            Object.DestroyImmediate(d.GetComponent<Collider>());
        }

        // Caseta del vigilante (z -9.4 a -6.6, puerta al centro hacia las vías)
        Caja(I, "Caseta_Lado1", new Vector3(-3.8f, 2.2f, -9.4f), new Vector3(2.4f, 2.4f, 0.12f), concreto);
        Caja(I, "Caseta_Lado2", new Vector3(-3.8f, 2.2f, -6.6f), new Vector3(2.4f, 2.4f, 0.12f), concreto);
        Caja(I, "Caseta_Frente1", new Vector3(-2.6f, 2.2f, -8.95f), new Vector3(0.12f, 2.4f, 0.9f), concreto);
        Caja(I, "Caseta_Frente2", new Vector3(-2.6f, 2.2f, -7.05f), new Vector3(0.12f, 2.4f, 0.9f), concreto);
        Caja(I, "Caseta_Dintel", new Vector3(-2.6f, 3.2f, -8f), new Vector3(0.12f, 0.4f, 1.0f), concreto);
        Caja(I, "Caseta_Techo", new Vector3(-3.8f, 3.45f, -8f), new Vector3(2.5f, 0.1f, 2.9f), concreto);
        Caja(I, "Escritorio", new Vector3(-4.4f, 1.4f, -8f), new Vector3(0.9f, 0.8f, 1.6f), madera);
        Transform tv = Caja(I, "Monitor_Caja", new Vector3(-4.45f, 2.05f, -8f), new Vector3(0.55f, 0.48f, 0.55f), Mat("Plastico", null, new Color(0.2f, 0.2f, 0.19f), Vector2.one));
        GameObject pantalla = GameObject.CreatePrimitive(PrimitiveType.Quad);
        pantalla.name = "Monitor_Pantalla";
        Object.DestroyImmediate(pantalla.GetComponent<Collider>());
        pantalla.transform.SetParent(I, false);
        pantalla.transform.localPosition = new Vector3(-4.16f, 2.06f, -8f);
        pantalla.transform.localRotation = Quaternion.Euler(0, -90f, 0);
        pantalla.transform.localScale = new Vector3(0.44f, 0.34f, 1f);
        pantalla.GetComponent<Renderer>().sharedMaterial = Mat("PantallaCRT", null, Color.white, Vector2.one, false, "Universal Render Pipeline/Unlit");
        pantalla.AddComponent<MonitorCRT>();
        tunel.monitor = pantalla.transform;
        Luz(I, "Luz_Monitor", new Vector3(-3.7f, 2.1f, -8f), new Color(0.6f, 1f, 0.7f), 3f, 1.2f, true);

        // Casillero
        Transform cas = Caja(I, "Casillero", new Vector3(-4.7f, 1.95f, -2f), new Vector3(0.5f, 1.9f, 0.7f), metal);
        Transform bisagra = new GameObject("Bisagra").transform;
        bisagra.SetParent(I, false);
        bisagra.localPosition = new Vector3(-4.43f, 1.95f, -2.35f);
        Transform puertaCas = Caja(bisagra, "PuertaCasillero", new Vector3(0, 0, 0.35f), new Vector3(0.04f, 1.85f, 0.68f), Mat("MetalVerde", null, new Color(0.18f, 0.24f, 0.2f), Vector2.one));
        Object.DestroyImmediate(puertaCas.GetComponent<Collider>());
        Casillero casillero = cas.gameObject.AddComponent<Casillero>();
        casillero.puerta = bisagra;
        tunel.casillero = casillero;

        // Luces fluorescentes que fallan
        List<Light> luces = new List<Light>();
        for (int i = 0; i < 4; i++)
            luces.Add(Luz(I, "Fluorescente_" + i, new Vector3(-1f, 4.6f, -15f + i * 10f), new Color(0.75f, 0.95f, 0.8f), 11f, 1.8f, i == 2));
        tunel.luces = luces.ToArray();

        // La silueta de cabeza radial (al fondo de las vías)
        GameObject sil = new GameObject("Silueta_Vigia");
        sil.transform.SetParent(I, false);
        sil.transform.localPosition = new Vector3(2.15f, 0f, 17f);
        Transform cuerpo = Caja(sil.transform, "Cuerpo", new Vector3(0, 1.3f, 0), new Vector3(0.45f, 2.6f, 0.3f), negro);
        Object.DestroyImmediate(cuerpo.GetComponent<Collider>());
        for (int k = 0; k < 12; k++)
        {
            Transform rayo = Caja(sil.transform, "Rayo", new Vector3(0, 2.95f, 0), new Vector3(0.05f, 0.75f, 0.05f), negro);
            Object.DestroyImmediate(rayo.GetComponent<Collider>());
            rayo.localRotation = Quaternion.Euler(0, 0, k * 30f);
            rayo.localPosition += rayo.up * 0.38f;
        }
        Light roja = Luz(sil.transform, "LuzRoja", new Vector3(0, 3.5f, -1.5f), new Color(0.9f, 0.1f, 0.05f), 8f, 2.5f, false);
        roja.enabled = false;
        tunel.silueta = sil;

        // Puertas
        Transform adentro = Punto(I, "PuntoAdentro", new Vector3(-3f, 1.05f, -17.5f), 0f);
        Transform salida = Caja(I, "Puerta_Salida", new Vector3(-3f, 2.15f, -19.95f), new Vector3(1.3f, 2.3f, 0.12f), puertaMat);
        PuertaTeleport pIn = puerta.gameObject.AddComponent<PuertaTeleport>();
        pIn.destino = adentro;
        pIn.mensaje = "Huele a humedad y a fierro.";
        PuertaTeleport pOut = salida.gameObject.AddComponent<PuertaTeleport>();
        pOut.destino = afuera;

        return tunel;
    }

    // ================================================================== Acto III: Mirador

    static Transform CrearMirador(Transform raiz, Vector3 p, Vector3 inicio)
    {
        Material concreto = Mat("Concreto", Tex("Concreto"), new Color(0.75f, 0.74f, 0.7f), new Vector2(2, 1));
        Material metal = Mat("MetalOxidado", null, new Color(0.25f, 0.18f, 0.14f), Vector2.one);
        Material rojo = Mat("BalizaRoja", null, new Color(1f, 0.1f, 0.05f), Vector2.one, false, "Universal Render Pipeline/Unlit");

        GameObject m = new GameObject("Mirador");
        m.transform.SetParent(raiz);
        m.transform.position = p;
        Vector3 d = p - inicio; d.y = 0;
        m.transform.rotation = Quaternion.LookRotation(d.normalized);   // forward = hacia el vacío

        Caja(m.transform, "Plataforma", new Vector3(0, 0.05f, 0), new Vector3(5f, 0.3f, 4f), concreto);
        for (int i = -2; i <= 2; i++)
            Caja(m.transform, "Poste_Barandal", new Vector3(i * 1.2f, 0.6f, 1.9f), new Vector3(0.07f, 1.1f, 0.07f), metal);
        Caja(m.transform, "Barandal", new Vector3(0, 1.1f, 1.9f), new Vector3(5f, 0.06f, 0.06f), metal);
        Caja(m.transform, "Barandal_Roto", new Vector3(-1.6f, 0.7f, 1.95f), new Vector3(1.6f, 0.05f, 0.05f), metal).localRotation = Quaternion.Euler(0, 0, -18f);
        Caja(m.transform, "Banca", new Vector3(1.4f, 0.45f, -1.2f), new Vector3(1.6f, 0.1f, 0.45f), metal);

        Caja(m.transform, "Poste_Baliza", new Vector3(-2.2f, 1.6f, -1.6f), new Vector3(0.1f, 3.2f, 0.1f), metal);
        GameObject foco = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        foco.name = "Baliza";
        Object.DestroyImmediate(foco.GetComponent<Collider>());
        foco.transform.SetParent(m.transform, false);
        foco.transform.localPosition = new Vector3(-2.2f, 3.3f, -1.6f);
        foco.transform.localScale = Vector3.one * 0.22f;
        foco.GetComponent<Renderer>().sharedMaterial = rojo;
        Light l = Luz(m.transform, "LuzBaliza", new Vector3(-2.2f, 3.4f, -1.6f), new Color(1f, 0.08f, 0.04f), 16f, 4f, false);
        ParpadeoFuerte pf = l.gameObject.AddComponent<ParpadeoFuerte>();
        pf.encendido = 0.5f; pf.apagado = 0.7f; pf.regular = true;
        return m.transform;
    }

    // ================================================================== Ayudantes

    static Transform Caja(Transform padre, string nombre, Vector3 local, Vector3 escala, Material mat)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = nombre;
        g.transform.SetParent(padre, false);
        g.transform.localPosition = local;
        g.transform.localScale = escala;
        g.GetComponent<Renderer>().sharedMaterial = mat;
        return g.transform;
    }

    static Transform Punto(Transform padre, string nombre, Vector3 local, float giroY)
    {
        Transform t = new GameObject(nombre).transform;
        t.SetParent(padre, false);
        t.localPosition = local;
        t.localRotation = Quaternion.Euler(0, giroY, 0);
        return t;
    }

    static Light Luz(Transform padre, string nombre, Vector3 local, Color color, float rango, float intensidad, bool fallando)
    {
        GameObject g = new GameObject(nombre);
        g.transform.SetParent(padre, false);
        g.transform.localPosition = local;
        Light l = g.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.range = rango;
        l.intensity = intensidad;
        l.shadows = LightShadows.None;
        if (fallando) { ParpadeoFuerte p = g.AddComponent<ParpadeoFuerte>(); p.encendido = 1.6f; p.apagado = 0.12f; }
        else g.AddComponent<Parpadeo>();
        return l;
    }

    static void ConfigurarTexturas()
    {
        foreach (string n in new[] { "Concreto", "MarcaWatcher", "PuertaExpediente08" })
        {
            TextureImporter t = AssetImporter.GetAtPath(CarpetaTex + "/" + n + ".png") as TextureImporter;
            if (t == null) continue;
            t.filterMode = FilterMode.Point;
            t.alphaIsTransparency = n == "MarcaWatcher";
            t.wrapMode = n == "Concreto" ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            t.SaveAndReimport();
        }
    }

    static Texture2D Tex(string n) => AssetDatabase.LoadAssetAtPath<Texture2D>(CarpetaTex + "/" + n + ".png");

    static Material Mat(string nombre, Texture2D tex, Color color, Vector2 tiling, bool recorte = false, string shader = "Universal Render Pipeline/Lit")
    {
        if (!AssetDatabase.IsValidFolder("Assets/Escenarios")) AssetDatabase.CreateFolder("Assets", "Escenarios");
        if (!AssetDatabase.IsValidFolder(CarpetaMat)) AssetDatabase.CreateFolder("Assets/Escenarios", "Materiales");
        string ruta = CarpetaMat + "/" + nombre + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null) { m = new Material(Shader.Find(shader)); AssetDatabase.CreateAsset(m, ruta); }
        else m.shader = Shader.Find(shader);
        if (tex != null) { m.SetTexture("_BaseMap", tex); m.SetTextureScale("_BaseMap", tiling); }
        m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.05f);
        if (recorte)
        {
            m.SetFloat("_AlphaClip", 1f);
            m.SetFloat("_Cutoff", 0.4f);
            m.EnableKeyword("_ALPHATEST_ON");
            m.SetFloat("_Cull", 0f);
        }
        EditorUtility.SetDirty(m);
        return m;
    }
}
