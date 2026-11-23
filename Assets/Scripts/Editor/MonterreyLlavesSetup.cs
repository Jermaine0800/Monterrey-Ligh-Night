using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menú 11: arma el juego de las 5 llaves del bucle sobre la escena abierta.
/// 1) Rehace los escenarios del guion (menú 10: linterna, túnel, mirador).
/// 2) Sube un poco la luz de la noche.
/// 3) Busca lugares planos y construye: CAMPAMENTO, PANTEÓN, CAPILLA (y usa el TÚNEL y el MIRADOR).
/// 4) Pone las 5 llaves, escondites en cada lugar y la PUERTA DEL BUCLE en el claro.
/// 5) Conecta todo con la historia (MisionManager).
/// Se puede correr varias veces: reemplaza lo que ya había creado (los árboles quitados no regresan).
/// </summary>
public static class MonterreyLlavesSetup
{
    const string RutaPanteon = "Assets/Escenarios/PSXGraveyard";
    const string RutaIglesia = "Assets/Escenarios/PSXChurch/PSX Abandoned Church.fbx";
    const string CarpetaMat = "Assets/Escenarios/Materiales";

    [MenuItem("Monterrey/11. Construir las 5 llaves (campamento, panteón, casa, túnel, mirador)")]
    public static void Construir()
    {
        MonterreyHistoriaSetup.Construir();

        PlayerMovement pm = Object.FindAnyObjectByType<PlayerMovement>();
        Terrain terreno = Terrain.activeTerrain;
        MisionManager mm = Object.FindAnyObjectByType<MisionManager>();
        TunelLineaK tunel = Object.FindAnyObjectByType<TunelLineaK>();
        GameObject miradorGO = GameObject.Find("Mirador");
        if (pm == null || terreno == null || mm == null || tunel == null || miradorGO == null)
        {
            EditorUtility.DisplayDialog("Monterrey", "Falta el jugador, el terreno, el MisionManager o los escenarios del menú 10.", "OK");
            return;
        }

        SubirLuz(pm);
        ConfigurarImportaciones();

        GameObject sistemas = GameObject.Find("_SistemasTerror");
        if (sistemas != null && sistemas.GetComponentInChildren<LlavesBucle>() == null)
        {
            GameObject g = new GameObject("LlavesBucle");
            g.transform.SetParent(sistemas.transform);
            g.AddComponent<LlavesBucle>();
        }

        GameObject raiz = GameObject.Find("_SitiosLlaves");
        if (raiz != null) Object.DestroyImmediate(raiz);
        raiz = new GameObject("_SitiosLlaves");

        Vector3 inicio = pm.transform.position;
        Random.InitState(1127);

        // ---- Elegir lugares planos, lejos entre sí y de lo que ya existe
        List<Vector3> ocupados = new List<Vector3> { inicio, tunel.transform.position, miradorGO.transform.position };
        Vector3 pCamp = BuscarPlano(terreno, inicio, 85f, 135f, 8f, ocupados, 70f);
        ocupados.Add(pCamp);
        Vector3 pPant = BuscarPlano(terreno, inicio, 110f, 190f, 14f, ocupados, 80f);
        ocupados.Add(pPant);
        Vector3 pIgle = BuscarPlano(terreno, inicio, 120f, 210f, 15f, ocupados, 80f);

        QuitarArboles(terreno, new[] { (pCamp, 16f), (pPant, 22f), (pIgle, 22f), (inicio, 8f) });

        // ---- Construir
        SitioCampamento camp = CrearCampamento(raiz.transform, pCamp, inicio);
        SitioPanteon pant = CrearPanteon(raiz.transform, pPant, inicio);
        SitioCasa igle = CrearCasa(raiz.transform, pIgle, inicio);
        SitioTunel stun = PrepararTunel(tunel, inicio);
        SitioMirador smir = PrepararMirador(miradorGO, inicio);
        PuertaBucle puerta = CrearPuertaBucle(raiz.transform, inicio, pm.transform.forward);

        mm.sitios = new SitioLlave[] { camp, pant, stun, igle, smir };
        mm.puertaBucle = puerta;
        EditorUtility.SetDirty(mm);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[Monterrey] 5 llaves listas. Campamento {D(inicio, pCamp)} m, Panteón {D(inicio, pPant)} m, Túnel {D(inicio, tunel.transform.position)} m, " +
                  $"Casa {D(inicio, pIgle)} m, Mirador {D(inicio, miradorGO.transform.position)} m.");
    }

    static int D(Vector3 a, Vector3 b) { a.y = b.y = 0; return Mathf.RoundToInt(Vector3.Distance(a, b)); }

    // ================================================================== Luz

    static void SubirLuz(PlayerMovement pm)
    {
        NightAmbience n = Object.FindAnyObjectByType<NightAmbience>();
        if (n != null)
        {
            n.luzLuna = 0.9f;
            n.ambienteNoche = new Color(0.22f, 0.23f, 0.3f);
            n.neblinaColor = new Color(0.07f, 0.075f, 0.095f);
            n.neblina = 0.022f;
            n.exposicion = 0.9f;
            n.intensidadFiltros = 0.35f;
            n.tramadoPSX = 0f;
            EditorUtility.SetDirty(n);
        }
        VHSOverlay vhs = Object.FindAnyObjectByType<VHSOverlay>();
        if (vhs != null) { vhs.grano = 0.01f; vhs.scanlinesAlpha = 0.19f; EditorUtility.SetDirty(vhs); }
        FlashlightController f = Object.FindAnyObjectByType<FlashlightController>();
        if (f != null) { f.intensidad = 9f; f.rango = 32f; EditorUtility.SetDirty(f); }
    }

    // ================================================================== Terreno

    static bool Dentro(Terrain t, Vector3 p, float m)
    {
        Vector3 l = p - t.transform.position;
        return l.x > m && l.z > m && l.x < t.terrainData.size.x - m && l.z < t.terrainData.size.z - m;
    }

    /// <summary>Qué tanto varía la altura en un círculo (para saber si es plano).</summary>
    static float Desnivel(Vector3 c, float r)
    {
        float min = float.MaxValue, max = float.MinValue;
        for (int i = 0; i < 12; i++)
            for (float k = 0.5f; k <= 1f; k += 0.5f)
            {
                Vector3 p = c + Quaternion.Euler(0, i * 30f, 0) * Vector3.forward * r * k;
                float h = Mundo.AlturaSuelo(p);
                min = Mathf.Min(min, h); max = Mathf.Max(max, h);
            }
        float hc = Mundo.AlturaSuelo(c);
        return Mathf.Max(max, hc) - Mathf.Min(min, hc);
    }

    static Vector3 BuscarPlano(Terrain t, Vector3 inicio, float rMin, float rMax, float radio, List<Vector3> evitar, float separacion)
    {
        Vector3 mejor = Vector3.zero; float mejorPuntaje = float.MaxValue;
        for (float r = rMin; r <= rMax; r += 6f)
            for (float a = 0; a < 360f; a += 6f)
            {
                Vector3 p = inicio + Quaternion.Euler(0, a, 0) * Vector3.forward * r;
                if (!Dentro(t, p, radio + 10f)) continue;
                bool lejos = true;
                foreach (Vector3 e in evitar) { Vector3 d = e - p; d.y = 0; if (d.magnitude < separacion) { lejos = false; break; } }
                if (!lejos) continue;
                float des = Desnivel(p, radio);
                float subida = Mathf.Abs(Mundo.AlturaSuelo(p) - inicio.y);
                float puntaje = des * 4f + subida * 0.08f + r * 0.01f;
                if (puntaje < mejorPuntaje) { mejorPuntaje = puntaje; mejor = p; }
            }
        if (mejor == Vector3.zero) mejor = inicio + Vector3.forward * rMin;
        return Mundo.EnSuelo(mejor);
    }

    static void QuitarArboles(Terrain t, (Vector3 c, float r)[] zonas)
    {
        TerrainData td = t.terrainData;
        Undo.RegisterCompleteObjectUndo(td, "Quitar árboles");
        List<TreeInstance> quedan = new List<TreeInstance>();
        int quitados = 0;
        foreach (TreeInstance ti in td.treeInstances)
        {
            Vector3 w = t.transform.position + Vector3.Scale(ti.position, td.size);
            bool fuera = true;
            foreach (var z in zonas) { Vector3 d = w - z.c; d.y = 0; if (d.magnitude < z.r) { fuera = false; break; } }
            if (fuera) quedan.Add(ti); else quitados++;
        }
        if (quitados > 0) { td.treeInstances = quedan.ToArray(); t.Flush(); EditorUtility.SetDirty(td); }
        Debug.Log("[Monterrey] Árboles quitados para los lugares: " + quitados);
    }

    // ================================================================== Importaciones y materiales

    static void ConfigurarImportaciones()
    {
        foreach (string g in AssetDatabase.FindAssets("t:Texture2D", new[] { RutaPanteon + "/Textures", "Assets/Escenarios/PSXChurch" }))
        {
            TextureImporter ti = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(g)) as TextureImporter;
            if (ti == null) continue;
            ti.filterMode = FilterMode.Point; ti.mipmapEnabled = false; ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.SaveAndReimport();
        }
        foreach (string g in AssetDatabase.FindAssets("t:Model", new[] { RutaPanteon + "/FBX", "Assets/Escenarios/PSXChurch" }))
        {
            ModelImporter mi = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(g)) as ModelImporter;
            if (mi == null) continue;
            mi.importCameras = false; mi.importLights = false; mi.animationType = ModelImporterAnimationType.None;
            // la capilla conserva sus materiales originales (sus nombres dicen cuál es la puerta)
            bool capilla = mi.assetPath.Contains("PSXChurch");
            mi.materialImportMode = capilla ? ModelImporterMaterialImportMode.ImportViaMaterialDescription : ModelImporterMaterialImportMode.None;
            mi.SaveAndReimport();
        }
    }

    static Material Mat(string nombre, Texture2D tex, Color color, bool dobleCara = false, string shader = "Universal Render Pipeline/Lit")
    {
        if (!AssetDatabase.IsValidFolder(CarpetaMat)) AssetDatabase.CreateFolder("Assets/Escenarios", "Materiales");
        string ruta = CarpetaMat + "/" + nombre + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null) { m = new Material(Shader.Find(shader)); AssetDatabase.CreateAsset(m, ruta); }
        else m.shader = Shader.Find(shader);
        m.SetTexture("_BaseMap", tex);
        m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.05f);
        if (dobleCara && m.HasProperty("_Cull")) m.SetFloat("_Cull", 0f);
        EditorUtility.SetDirty(m);
        return m;
    }

    /// <summary>Instancia un modelo del panteón con su textura, en una posición del mundo.</summary>
    static GameObject Modelo(string nombre, Transform padre, Vector3 pos, float giroY, bool collider = true, float escala = 1f)
    {
        GameObject pf = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPanteon + "/FBX/" + nombre + ".fbx");
        if (pf == null) { Debug.LogWarning("Falta " + nombre); return new GameObject(nombre + "_FALTA"); }
        GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(pf);
        PrefabUtility.UnpackPrefabInstance(g, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        g.name = nombre;
        g.transform.SetParent(padre, true);
        g.transform.position = Mundo.EnSuelo(pos, -0.03f);
        g.transform.rotation = Quaternion.Euler(0, giroY, 0) * g.transform.rotation;
        g.transform.localScale *= escala;
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaPanteon + "/Textures/" + nombre + "_Albedo.png");
        Material m = Mat("Panteon_" + nombre, tex, Color.white);
        foreach (Renderer r in g.GetComponentsInChildren<Renderer>())
        {
            r.sharedMaterial = m;
            if (collider && r.GetComponent<Collider>() == null)
            {
                MeshFilter mf = r.GetComponent<MeshFilter>();
                if (mf != null) { MeshCollider mc = r.gameObject.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; }
            }
        }
        return g;
    }

    // ================================================================== Piezas comunes

    static Transform Caja(Transform padre, string nombre, Vector3 local, Vector3 escala, Material mat, bool collider = true)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = nombre;
        g.transform.SetParent(padre, false);
        g.transform.localPosition = local;
        g.transform.localScale = escala;
        g.GetComponent<Renderer>().sharedMaterial = mat;
        if (!collider) Object.DestroyImmediate(g.GetComponent<Collider>());
        return g.transform;
    }

    static Light Luz(Transform padre, string nombre, Vector3 local, Color c, float rango, float intensidad, bool parpadea = true)
    {
        GameObject g = new GameObject(nombre);
        g.transform.SetParent(padre, false);
        g.transform.localPosition = local;
        Light l = g.AddComponent<Light>();
        l.type = LightType.Point; l.color = c; l.range = rango; l.intensity = intensidad; l.shadows = LightShadows.None;
        if (parpadea) g.AddComponent<Parpadeo>();
        return l;
    }

    static Transform PuntoVigia(Transform sitio, Vector3 inicio, float distancia)
    {
        Vector3 d = inicio - sitio.position; d.y = 0;
        Transform p = new GameObject("PuntoVigia").transform;
        p.SetParent(sitio, true);
        p.position = Mundo.EnSuelo(sitio.position + d.normalized * distancia + Vector3.Cross(Vector3.up, d.normalized) * 3f);
        return p;
    }

    /// <summary>Llave flotante hecha de piezas (aro + vástago + dientes) con brillo dorado.</summary>
    static LlaveBucle CrearLlave(Transform padre, string nombre, Vector3 posMundo, bool soloConLinterna, string frase)
    {
        Material oro = Mat("LlaveOro", null, new Color(0.95f, 0.78f, 0.38f), false, "Universal Render Pipeline/Unlit");
        GameObject g = new GameObject(nombre);
        g.transform.SetParent(padre, true);
        g.transform.position = posMundo;
        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Mathf.Deg2Rad;
            Transform seg = Caja(g.transform, "Aro", new Vector3(Mathf.Cos(a) * 0.055f, 0.1f + Mathf.Sin(a) * 0.055f, 0), new Vector3(0.035f, 0.035f, 0.02f), oro, false);
            seg.localRotation = Quaternion.Euler(0, 0, i * 45f);
        }
        Caja(g.transform, "Vastago", new Vector3(0, -0.04f, 0), new Vector3(0.025f, 0.22f, 0.02f), oro, false);
        Caja(g.transform, "Diente1", new Vector3(0.03f, -0.12f, 0), new Vector3(0.05f, 0.025f, 0.02f), oro, false);
        Caja(g.transform, "Diente2", new Vector3(0.025f, -0.07f, 0), new Vector3(0.04f, 0.02f, 0.02f), oro, false);
        Luz(g.transform, "Brillo", new Vector3(0, 0, -0.2f), new Color(1f, 0.85f, 0.5f), 5f, 2f, true);
        g.transform.localScale = Vector3.one * 1.6f;
        BoxCollider bc = g.AddComponent<BoxCollider>();
        bc.isTrigger = true; bc.size = new Vector3(0.35f, 0.45f, 0.35f);
        LlaveBucle l = g.AddComponent<LlaveBucle>();
        l.soloConLinterna = soloConLinterna;
        l.frase = frase;
        return l;
    }

    static Escondite CrearEscondite(Transform padre, string nombre, Vector3 posMundo, Vector3 tam, string etiqueta)
    {
        GameObject g = new GameObject(nombre);
        g.transform.SetParent(padre, true);
        g.transform.position = posMundo;
        BoxCollider bc = g.AddComponent<BoxCollider>();
        bc.isTrigger = true; bc.size = tam; bc.center = new Vector3(0, tam.y * 0.5f, 0);
        Transform dentro = new GameObject("PuntoDentro").transform;
        dentro.SetParent(g.transform, false);
        Escondite e = g.AddComponent<Escondite>();
        e.puntoDentro = dentro;
        e.nombre = etiqueta;
        return e;
    }

    static Transform NuevoSitio(Transform raiz, string nombre, Vector3 pos, Vector3 inicio)
    {
        Transform t = new GameObject(nombre).transform;
        t.SetParent(raiz);
        t.position = pos;
        Vector3 d = inicio - pos; d.y = 0;
        t.rotation = Quaternion.LookRotation(d.normalized);   // forward = hacia de donde vienes (la entrada)
        return t;
    }

    static Vector3 W(Transform t, float x, float z) => Mundo.EnSuelo(t.TransformPoint(new Vector3(x, 0, z)));

    // ================================================================== LLAVE 1: Campamento

    static SitioCampamento CrearCampamento(Transform raiz, Vector3 pos, Vector3 inicio)
    {
        Transform s = NuevoSitio(raiz, "Sitio1_Campamento", pos, inicio);
        Material lonaN = Mat("LonaNaranja", null, new Color(0.55f, 0.25f, 0.1f), true);
        Material lonaA = Mat("LonaAzul", null, new Color(0.12f, 0.18f, 0.3f), true);
        Material negro = Mat("Plastico", null, new Color(0.08f, 0.08f, 0.08f));
        Material tronco = Mat("Durmiente", null, new Color(0.18f, 0.13f, 0.09f));

        Transform[] tiendas = new Transform[3];
        float[][] lugares = { new[] { -4f, -1f, 20f }, new[] { 3.5f, -2.5f, -35f }, new[] { 0.5f, -6f, 80f } };
        for (int i = 0; i < 3; i++)
        {
            Transform tienda = new GameObject("Tienda_" + i).transform;
            tienda.SetParent(s, false);
            tienda.position = W(s, lugares[i][0], lugares[i][1]);
            tienda.rotation = s.rotation * Quaternion.Euler(0, lugares[i][2], 0);
            Material m = i == 1 ? lonaA : lonaN;
            bool caida = i == 2;
            Transform a = Caja(tienda, "Lona_A", new Vector3(-0.6f, 0.6f, 0), new Vector3(0.05f, 1.6f, 2.4f), m, false);
            a.localRotation = Quaternion.Euler(0, 0, caida ? -70f : -38f);
            Transform b = Caja(tienda, "Lona_B", new Vector3(0.6f, 0.6f, 0), new Vector3(0.05f, 1.6f, 2.4f), m, false);
            b.localRotation = Quaternion.Euler(0, 0, caida ? 75f : 38f);
            tiendas[i] = tienda;
        }
        // Una tienda sirve para esconderse
        CrearEscondite(tiendas[0], "Escondite_Tienda", tiendas[0].position, new Vector3(1.6f, 1.2f, 2.2f), "casa de campaña");

        // Fogata
        Transform fogata = new GameObject("Fogata").transform;
        fogata.SetParent(s, false);
        fogata.position = W(s, 0, 0);
        GameObject lena = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Low Poly Fire Particles/Models/FireWood.fbx");
        if (lena != null)
        {
            GameObject l = (GameObject)PrefabUtility.InstantiatePrefab(lena);
            PrefabUtility.UnpackPrefabInstance(l, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            l.transform.SetParent(fogata, false);
        }
        else for (int i = 0; i < 4; i++) Caja(fogata, "Leño", new Vector3(0, 0.08f, 0), new Vector3(0.12f, 0.12f, 0.9f), tronco, false).localRotation = Quaternion.Euler(0, i * 45f, 0);
        for (int i = 0; i < 8; i++)
            Caja(fogata, "Piedra", Quaternion.Euler(0, i * 45f, 0) * Vector3.forward * 0.6f + Vector3.up * 0.05f, Vector3.one * 0.18f, Mat("Piedra", null, new Color(0.3f, 0.3f, 0.3f)), false);
        Light fuego = Luz(fogata, "LuzFogata", new Vector3(0, 0.6f, 0), new Color(1f, 0.5f, 0.2f), 9f, 2.2f, true);
        Caja(s, "Tronco_Asiento", s.InverseTransformPoint(W(s, 1.8f, 1.5f)) + Vector3.up * 0.2f, new Vector3(1.6f, 0.35f, 0.35f), tronco);

        // Radio
        Transform radio = Caja(s, "Radio", s.InverseTransformPoint(W(s, 2f, 1.5f)) + Vector3.up * 0.5f, new Vector3(0.35f, 0.2f, 0.12f), negro, false);
        Caja(radio, "Antena", new Vector3(0.35f, 2.2f, 0), new Vector3(0.05f, 3f, 0.3f), negro, false);
        Luz(radio, "LedRadio", new Vector3(0, 0, -0.8f), new Color(1f, 0.1f, 0.05f), 1.2f, 1f, true);

        // Llave escondida: solo con la linterna
        LlaveBucle llave = CrearLlave(s, "Llave1_Campamento", W(s, 3.2f, -4.6f) + Vector3.up * 0.6f, false, "Llave 1 de 5. Estaba junto a una bota... sin dueño.");

        SitioCampamento sitio = s.gameObject.AddComponent<SitioCampamento>();
        sitio.nombreLugar = "Campamento";
        sitio.llave = llave;
        sitio.fogata = fuego;
        sitio.radio = radio;
        sitio.puntoVigia = PuntoVigia(s, inicio, 14f);
        sitio.radioLlegada = 10f;
        return sitio;
    }

    // ================================================================== LLAVE 2: Panteón

    static SitioPanteon CrearPanteon(Transform raiz, Vector3 pos, Vector3 inicio)
    {
        Transform s = NuevoSitio(raiz, "Sitio2_Panteon", pos, inicio);
        float ancho = 22f, largo = 20f;
        float giro = s.eulerAngles.y;

        // Barda de fierro con reja de entrada (frente = hacia el jugador, +Z local)
        for (float x = -ancho / 2 + 1; x <= ancho / 2 - 1; x += 2f)
        {
            if (Mathf.Abs(x) < 2f) continue;   // hueco de la reja
            Modelo("Fence_Iron_2m_01", s, W(s, x, largo / 2), giro + 180f);
            Modelo("Fence_Iron_2m_01", s, W(s, x, -largo / 2), giro);
        }
        for (float z = -largo / 2 + 1; z <= largo / 2 - 1; z += 2f)
        {
            Modelo("Fence_Iron_2m_01", s, W(s, -ancho / 2, z), giro + 90f);
            Modelo("Fence_Iron_2m_01", s, W(s, ancho / 2, z), giro - 90f);
        }
        Modelo("Fence_Gate_01", s, W(s, 0, largo / 2), giro + 180f, false);
        Modelo("Lamp_Post_01", s, W(s, -2.2f, largo / 2 + 0.8f), giro);
        Modelo("Lamp_Post_01", s, W(s, 2.2f, largo / 2 + 0.8f), giro);
        Luz(s, "LuzFarol_A", s.InverseTransformPoint(W(s, -2.2f, largo / 2 + 0.8f)) + Vector3.up * 2.5f, new Color(0.9f, 0.75f, 0.5f), 10f, 1.5f);
        Luz(s, "LuzFarol_B", s.InverseTransformPoint(W(s, 2.2f, largo / 2 + 0.8f)) + Vector3.up * 2.5f, new Color(0.9f, 0.75f, 0.5f), 10f, 1.2f);

        // Tumbas en filas (dejando pasillo al centro)
        List<VelaPanteon> velas = new List<VelaPanteon>();
        int[] marcadas = { 3, 9, 14 };
        int n = 0;
        for (int fila = 0; fila < 4; fila++)
            for (int col = 0; col < 6; col++)
            {
                float x = -8f + col * 3.2f; if (Mathf.Abs(x) < 1.8f) x += 1.6f * Mathf.Sign(x + 0.01f);
                float z = 6f - fila * 3.6f;
                string tipo = (n % 3 == 0) ? "Gravestone_Cross_01" : "Gravestone_Rounded_01";
                Modelo(tipo, s, W(s, x + Random.Range(-0.3f, 0.3f), z), giro + 180f + Random.Range(-12f, 12f));
                Modelo("Grave_Mound_01", s, W(s, x, z - 1.3f), giro + 180f, false);
                if (System.Array.IndexOf(marcadas, n) >= 0)
                {
                    Vector3 pv = W(s, x + 0.55f, z - 0.4f);
                    Modelo("Candles_Skull_01", s, pv, giro + 180f, false);
                    GameObject vela = new GameObject("Vela_Interactiva");
                    vela.transform.SetParent(s, true);
                    vela.transform.position = pv;
                    BoxCollider bc = vela.AddComponent<BoxCollider>();
                    bc.isTrigger = true; bc.size = new Vector3(0.9f, 0.9f, 0.9f); bc.center = new Vector3(0, 0.35f, 0);
                    VelaPanteon v = vela.AddComponent<VelaPanteon>();
                    v.luz = Luz(vela.transform, "Flama", new Vector3(0, 0.5f, 0), new Color(1f, 0.6f, 0.3f), 5f, 1.6f, true);
                    velas.Add(v);
                }
                n++;
            }

        // Árboles muertos
        for (int i = 0; i < 5; i++)
            Modelo("Tree_Dead_01", s, W(s, Random.Range(-ancho / 2 + 1.5f, ancho / 2 - 1.5f), Random.Range(-largo / 2 + 1.5f, largo / 2 - 1.5f)), Random.Range(0f, 360f), true, Random.Range(1.2f, 1.8f));
        Modelo("Shovel_Dirt_01", s, W(s, 5.5f, 7.5f), giro + 30f);

        // Cripta al fondo con una jaula de fierro enfrente (adentro el ataúd con la llave)
        Modelo("Crypt_01", s, W(s, 0, -largo / 2 + 2.2f), giro + 180f);
        Material fierro = Mat("MetalOxidado", null, new Color(0.25f, 0.18f, 0.14f));
        Transform jaula = new GameObject("Jaula").transform;
        jaula.SetParent(s, false);
        jaula.position = W(s, 0, -largo / 2 + 5.2f);
        jaula.localRotation = Quaternion.identity;
        for (int i = 0; i <= 6; i++)
        {
            float t = -1.5f + i * 0.5f;
            Caja(jaula, "Barrote", new Vector3(-1.5f, 1f, t), new Vector3(0.05f, 2f, 0.05f), fierro);
            Caja(jaula, "Barrote", new Vector3(1.5f, 1f, t), new Vector3(0.05f, 2f, 0.05f), fierro);
        }
        Caja(jaula, "Techo", new Vector3(0, 2f, 0), new Vector3(3.1f, 0.06f, 3.1f), fierro);
        Transform bisagra = new GameObject("BisagraReja").transform;
        bisagra.SetParent(jaula, false);
        bisagra.localPosition = new Vector3(-1.5f, 0, 1.5f);
        for (int i = 0; i <= 6; i++) Caja(bisagra, "BarroteReja", new Vector3(i * 0.5f, 1f, 0), new Vector3(0.05f, 2f, 0.05f), fierro);
        Caja(bisagra, "TravesañoReja", new Vector3(1.5f, 1.4f, 0), new Vector3(3f, 0.06f, 0.06f), fierro);
        PuertaBisagra reja = bisagra.gameObject.AddComponent<PuertaBisagra>();
        reja.anguloAbierta = -105f;
        GameObject ataud = Modelo("Coffin_01", jaula, jaula.position, giro + 90f, false);
        LlaveBucle llave = CrearLlave(s, "Llave2_Panteon", jaula.position + Vector3.up * 0.75f, false, "Llave 2 de 5. Estaba sobre el ataúd... tibia.");

        // Escondites: dos ataúdes abiertos entre las tumbas
        GameObject c1 = Modelo("Coffin_01", s, W(s, -9f, -3f), giro + 90f, false);
        CrearEscondite(s, "Escondite_Ataud1", c1.transform.position, new Vector3(1.2f, 0.8f, 2.2f), "ataúd");
        GameObject c2 = Modelo("Coffin_01", s, W(s, 9f, 2f), giro + 90f, false);
        CrearEscondite(s, "Escondite_Ataud2", c2.transform.position, new Vector3(1.2f, 0.8f, 2.2f), "ataúd");

        SitioPanteon sitio = s.gameObject.AddComponent<SitioPanteon>();
        sitio.nombreLugar = "Panteón";
        sitio.llave = llave;
        sitio.velas = velas.ToArray();
        sitio.rejaCripta = reja;
        sitio.puntoVigia = PuntoVigia(s, inicio, largo / 2 + 6f);
        sitio.radioLlegada = 13f;
        return sitio;
    }

    // ================================================================== LLAVE 4: Capilla

    static SitioIglesia CrearIglesia(Transform raiz, Vector3 pos, Vector3 inicio)
    {
        Transform s = NuevoSitio(raiz, "Sitio4_Capilla", pos, inicio);
        GameObject pf = AssetDatabase.LoadAssetAtPath<GameObject>(RutaIglesia);
        float escala = 1.5f;

        // Instancia temporal para leer la malla en el mundo
        GameObject tmp = (GameObject)Object.Instantiate(pf);
        MeshFilter mf = tmp.GetComponentInChildren<MeshFilter>();
        MeshRenderer mr = tmp.GetComponentInChildren<MeshRenderer>();
        Mesh orig = mf.sharedMesh;
        Matrix4x4 m2w = mf.transform.localToWorldMatrix;

        // Pasar los vértices a "metros, Y arriba" y separar la puerta (submalla con material "door")
        Vector3[] v = orig.vertices.Select(p => (Vector3)(m2w.MultiplyPoint3x4(p)) * escala).ToArray();
        Vector3[] nrm = orig.normals.Select(p => m2w.MultiplyVector(p).normalized).ToArray();
        int subPuerta = -1;
        Material[] matsOrig = mr.sharedMaterials;
        for (int i = 0; i < matsOrig.Length; i++) if (matsOrig[i] != null && matsOrig[i].name.ToLower().Contains("door")) subPuerta = i;
        Debug.Log("[Monterrey] Capilla: materiales = " + string.Join(", ", matsOrig.Select(x => x != null ? x.name : "null")) + " | puerta = submalla " + subPuerta);
        Object.DestroyImmediate(tmp);

        Mesh m = new Mesh { name = "CapillaSinPuerta" };
        m.vertices = v; m.normals = nrm; m.uv = orig.uv;
        List<int[]> subs = new List<int[]>();
        List<Material> mats = new List<Material>();
        Bounds puertaB = new Bounds(); bool hayPuerta = false;
        for (int i = 0; i < orig.subMeshCount; i++)
        {
            int[] tris = orig.GetTriangles(i);
            if (i == subPuerta)
            {
                foreach (int k in tris) { if (!hayPuerta) { puertaB = new Bounds(v[k], Vector3.zero); hayPuerta = true; } else puertaB.Encapsulate(v[k]); }
                continue;
            }
            subs.Add(tris);
            mats.Add(MatIglesia(i < matsOrig.Length ? matsOrig[i] : null, i));
        }
        m.subMeshCount = subs.Count;
        for (int i = 0; i < subs.Count; i++) m.SetTriangles(subs[i], i);
        m.RecalculateBounds();
        string rutaMalla = "Assets/Escenarios/PSXChurch/CapillaSinPuerta.asset";
        AssetDatabase.DeleteAsset(rutaMalla);
        AssetDatabase.CreateAsset(m, rutaMalla);

        Bounds b = m.bounds;
        Vector3 centroLocal = new Vector3(b.center.x, 0, b.center.z);
        Vector3 haciaPuerta = hayPuerta ? (puertaB.center - b.center) : Vector3.forward; haciaPuerta.y = 0;
        haciaPuerta.Normalize();

        // Raíz del edificio: la puerta mira hacia +Z del sitio (de donde vienes)
        Transform edificio = new GameObject("Capilla").transform;
        edificio.SetParent(s, false);
        edificio.localRotation = Quaternion.FromToRotation(haciaPuerta, Vector3.forward);
        edificio.position = s.position - edificio.rotation * centroLocal + Vector3.down * 0.1f;
        GameObject malla = new GameObject("Malla");
        malla.transform.SetParent(edificio, false);
        malla.AddComponent<MeshFilter>().sharedMesh = m;
        malla.AddComponent<MeshRenderer>().sharedMaterials = mats.ToArray();
        malla.AddComponent<MeshCollider>().sharedMesh = m;

        // Puerta propia (se azota): ocupa el hueco de la puerta original
        PuertaBisagra puerta = null;
        if (hayPuerta)
        {
            Vector3 c = edificio.TransformPoint(puertaB.center);
            Vector3 tamMundo = edificio.rotation * puertaB.size;
            float anchoP = Mathf.Max(Mathf.Abs(tamMundo.x), Mathf.Abs(tamMundo.z), 1.2f), altoP = Mathf.Max(puertaB.size.y, 2.2f);
            Transform bis = new GameObject("BisagraPuerta").transform;
            bis.SetParent(s, true);
            bis.rotation = s.rotation;
            bis.position = c - s.right * anchoP * 0.5f - Vector3.up * puertaB.size.y * 0.5f;
            Material mPuerta = MatIglesia(subPuerta >= 0 ? matsOrig[subPuerta] : null, 99);
            Caja(bis, "Hoja", new Vector3(anchoP * 0.5f, altoP * 0.5f, 0.05f), new Vector3(anchoP, altoP, 0.08f), mPuerta);
            puerta = bis.gameObject.AddComponent<PuertaBisagra>();
            puerta.anguloAbierta = -100f;
            puerta.abiertaAlInicio = true;
        }

        // Interior: bancas, altar, velas, confesionario
        float largo = b.size.z, anchoI = b.size.x;
        if (Mathf.Abs(haciaPuerta.x) > Mathf.Abs(haciaPuerta.z)) { largo = b.size.x; anchoI = b.size.z; }
        Material madera = Mat("Durmiente", null, new Color(0.2f, 0.14f, 0.09f));
        Material tela = Mat("ManteAltar", null, new Color(0.45f, 0.42f, 0.36f));
        float zFondo = -largo / 2f + 1.6f;
        for (int fila = 0; fila < 5; fila++)
            for (int lado = -1; lado <= 1; lado += 2)
            {
                if (fila == 3 && lado == 1) continue; // una banca tirada
                Transform banca = Caja(s, "Banca", new Vector3(lado * anchoI * 0.22f, 0.45f, largo / 2f - 4f - fila * 1.8f), new Vector3(anchoI * 0.3f, 0.1f, 0.5f), madera);
                Caja(banca, "Respaldo", new Vector3(0, 4f, -0.45f), new Vector3(1f, 8f, 0.2f), madera, false);
            }
        Caja(s, "BancaTirada", new Vector3(anchoI * 0.22f, 0.25f, largo / 2f - 4f - 3 * 1.8f), new Vector3(anchoI * 0.3f, 0.1f, 0.5f), madera).localRotation = Quaternion.Euler(80f, 15f, 0);
        Transform altar = Caja(s, "Altar", new Vector3(0, 0.55f, zFondo), new Vector3(2.4f, 1.1f, 1f), tela);
        List<Light> velas = new List<Light>();
        for (int i = -1; i <= 1; i += 2)
        {
            Caja(s, "Cirio", new Vector3(i * 0.9f, 1.35f, zFondo), new Vector3(0.08f, 0.5f, 0.08f), Mat("Cera", null, new Color(0.85f, 0.82f, 0.7f)), false);
            velas.Add(Luz(s, "LuzCirio", new Vector3(i * 0.9f, 1.75f, zFondo + 0.2f), new Color(1f, 0.65f, 0.3f), 7f, 1.6f, true));
        }
        Caja(s, "Cruz_V", new Vector3(0, 3.2f, zFondo - 0.6f), new Vector3(0.18f, 2.4f, 0.12f), madera, false);
        Caja(s, "Cruz_H", new Vector3(0, 3.7f, zFondo - 0.6f), new Vector3(1.2f, 0.18f, 0.12f), madera, false);
        LlaveBucle llave = CrearLlave(s, "Llave4_Capilla", s.TransformPoint(new Vector3(0, 1.35f, zFondo)), false, "Llave 4 de 5. El altar estaba frío como hielo.");

        // Confesionario = escondite
        Vector3 conf = new Vector3(-anchoI / 2f + 1f, 0, zFondo + 3f);
        Transform cajaC = Caja(s, "Confesionario", conf + new Vector3(-0.15f, 1.2f, 0), new Vector3(0.1f, 2.4f, 1.4f), madera);
        Caja(s, "Confesionario_Lado1", conf + new Vector3(0.3f, 1.2f, 0.7f), new Vector3(1f, 2.4f, 0.1f), madera);
        Caja(s, "Confesionario_Lado2", conf + new Vector3(0.3f, 1.2f, -0.7f), new Vector3(1f, 2.4f, 0.1f), madera);
        Caja(s, "Confesionario_Techo", conf + new Vector3(0.3f, 2.45f, 0), new Vector3(1.1f, 0.1f, 1.5f), madera, false);
        Caja(s, "Confesionario_Cortina", conf + new Vector3(0.78f, 1.4f, 0), new Vector3(0.03f, 1.9f, 1.3f), Mat("Cortina", null, new Color(0.25f, 0.05f, 0.05f), true), false);
        CrearEscondite(s, "Escondite_Confesionario", s.TransformPoint(conf + new Vector3(0.3f, 0, 0)), new Vector3(1.6f, 2f, 1.6f), "confesionario");

        // Campana sobre la entrada
        Transform campana = new GameObject("Campana").transform;
        campana.SetParent(s, false);
        campana.localPosition = new Vector3(0, Mathf.Min(b.size.y - 1.2f, 6.5f), largo / 2f - 1f);
        Material bronce = Mat("Bronce", null, new Color(0.35f, 0.27f, 0.15f));
        GameObject cuerpo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.DestroyImmediate(cuerpo.GetComponent<Collider>());
        cuerpo.transform.SetParent(campana, false);
        cuerpo.transform.localPosition = new Vector3(0, -0.45f, 0);
        cuerpo.transform.localScale = new Vector3(0.7f, 0.35f, 0.7f);
        cuerpo.GetComponent<Renderer>().sharedMaterial = bronce;
        CampanaIglesia ci = campana.gameObject.AddComponent<CampanaIglesia>();
        Luz(s, "LuzEntrada", new Vector3(0, 3f, largo / 2f + 1.5f), new Color(0.6f, 0.7f, 1f), 9f, 1.2f, true);

        Transform entrada = new GameObject("EntradaMonstruo").transform;
        entrada.SetParent(s, false);
        entrada.localPosition = new Vector3(0, 0, largo / 2f + 6f);

        SitioIglesia sitio = s.gameObject.AddComponent<SitioIglesia>();
        sitio.nombreLugar = "Capilla";
        sitio.llave = llave;
        sitio.puerta = puerta;
        sitio.campana = ci;
        sitio.velas = velas.ToArray();
        sitio.entradaMonstruo = entrada;
        sitio.puntoVigia = PuntoVigia(s, inicio, largo / 2f + 7f);
        sitio.radioLlegada = largo / 2f + 2f;
        return sitio;
    }

    // ================================================================== LLAVE 4: Casa abandonada (antes capilla)
    const string RutaCasa = "Assets/ALP_Assets/country house01/Prefabs/House_Prefab.prefab";

    /// <summary>
    /// Casa de campo abandonada con 5 cuartos + pasillo + patio. Coordenadas locales de la casa (entrada hacia +Z = hacia donde vienes):
    /// Pasillo (0, 4.7) · Sala (0, 1.4) · Cuarto niña (-4.3, 1.1) · Cocina/comedor (-0.7, -2.4) · Recámara grande (4.4, -0.6) · Cuarto del fondo (-5.3, -2.7).
    /// </summary>
    static SitioCasa CrearCasa(Transform raiz, Vector3 pos, Vector3 inicio)
    {
        Transform s = NuevoSitio(raiz, "Sitio4_Casa", pos, inicio);
        GameObject pf = AssetDatabase.LoadAssetAtPath<GameObject>(RutaCasa);
        GameObject casa = pf != null ? (GameObject)PrefabUtility.InstantiatePrefab(pf) : new GameObject("Casa_FALTA");
        if (pf != null) PrefabUtility.UnpackPrefabInstance(casa, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        casa.name = "CasaGarza";
        casa.transform.SetParent(s, false);
        casa.transform.localPosition = new Vector3(0, -0.08f, 0);
        casa.transform.localRotation = Quaternion.identity;
        float piso = 0.38f;   // altura del piso interior (local del sitio)

        // Escalones de la entrada (el piso de la casa está a ~40 cm)
        Material madera = Mat("Durmiente", null, new Color(0.2f, 0.14f, 0.09f));
        Material maderaClara = Mat("MaderaVieja", null, new Color(0.32f, 0.24f, 0.16f));
        Material tela = Mat("TelaVieja", null, new Color(0.35f, 0.3f, 0.26f));
        Caja(s, "Escalon1", new Vector3(0, 0.12f, 7.3f), new Vector3(2.2f, 0.24f, 0.5f), madera);
        Caja(s, "Escalon2", new Vector3(0, 0.26f, 6.85f), new Vector3(2.2f, 0.24f, 0.5f), madera);

        // Puerta principal: se azota al tomar la llave
        PuertaBisagra puerta = null;
        Transform pPuerta = casa.transform.Find("Doors/Door04_pr");
        if (pPuerta != null)
        {
            pPuerta.localRotation = Quaternion.Euler(0, 80f, 0);    // cerrada
            puerta = pPuerta.gameObject.AddComponent<PuertaBisagra>();
            puerta.anguloAbierta = -90f; puerta.abiertaAlInicio = true; puerta.duracion = 1.2f;
        }

        // ---- Recámara grande (4.4, -0.6): cama, ropero = escondite, buró
        Transform cama = Caja(s, "Cama", new Vector3(4.6f, piso + 0.45f, 1.4f), new Vector3(1.5f, 0.12f, 2.1f), tela);
        Caja(s, "CamaPata1", new Vector3(3.95f, piso + 0.2f, 0.45f), new Vector3(0.08f, 0.4f, 0.08f), madera, false);
        Caja(s, "CamaPata2", new Vector3(5.25f, piso + 0.2f, 0.45f), new Vector3(0.08f, 0.4f, 0.08f), madera, false);
        Caja(s, "CamaPata3", new Vector3(3.95f, piso + 0.2f, 2.35f), new Vector3(0.08f, 0.4f, 0.08f), madera, false);
        Caja(s, "CamaPata4", new Vector3(5.25f, piso + 0.2f, 2.35f), new Vector3(0.08f, 0.4f, 0.08f), madera, false);
        Caja(s, "Cabecera", new Vector3(4.6f, piso + 0.8f, 2.48f), new Vector3(1.6f, 1f, 0.08f), madera);
        Escondite bajoCama = CrearEscondite(s, "Escondite_BajoCama", s.TransformPoint(new Vector3(4.6f, piso, 1.4f)), new Vector3(1.8f, 1f, 2.4f), "debajo de la cama");
        bajoCama.puntoSalida = Punto(bajoCama.transform, s.TransformPoint(new Vector3(3.3f, piso, 1.4f)));

        // Ropero contra la pared derecha, con las puertas hacia el cuarto
        Vector3 ro = new Vector3(6.2f, piso, -2.6f);
        Caja(s, "Ropero_Fondo", ro + new Vector3(0.35f, 1.05f, 0), new Vector3(0.06f, 2.1f, 1.3f), madera);
        Caja(s, "Ropero_Lado1", ro + new Vector3(0, 1.05f, 0.65f), new Vector3(0.7f, 2.1f, 0.06f), madera);
        Caja(s, "Ropero_Lado2", ro + new Vector3(0, 1.05f, -0.65f), new Vector3(0.7f, 2.1f, 0.06f), madera);
        Caja(s, "Ropero_Techo", ro + new Vector3(0, 2.12f, 0), new Vector3(0.72f, 0.06f, 1.36f), madera, false);
        Caja(s, "Ropero_PuertaA", ro + new Vector3(-0.36f, 1.05f, 0.33f), new Vector3(0.04f, 2.0f, 0.62f), maderaClara, false);
        Caja(s, "Ropero_PuertaB", ro + new Vector3(-0.36f, 1.05f, -0.33f), new Vector3(0.04f, 2.0f, 0.62f), maderaClara, false);
        Escondite ropero = CrearEscondite(s, "Escondite_Ropero", s.TransformPoint(ro), new Vector3(1.4f, 2.1f, 1.5f), "el ropero");
        ropero.puntoSalida = Punto(ropero.transform, s.TransformPoint(ro + new Vector3(-1.2f, 0, 0)));
        Caja(s, "Buro", new Vector3(3.6f, piso + 0.3f, 2.3f), new Vector3(0.45f, 0.6f, 0.4f), madera);

        // ---- Comedor/cocina (-0.7, -2.4): la mesa puesta
        Caja(s, "Mesa", new Vector3(-0.8f, piso + 0.75f, -2.4f), new Vector3(2f, 0.08f, 1f), madera);
        for (int i = -1; i <= 1; i += 2)
            for (int j = -1; j <= 1; j += 2)
            {
                Caja(s, "MesaPata", new Vector3(-0.8f + i * 0.9f, piso + 0.37f, -2.4f + j * 0.42f), new Vector3(0.07f, 0.74f, 0.07f), madera, false);
                Caja(s, "Silla", new Vector3(-0.8f + i * 0.5f, piso + 0.45f, -2.4f + j * 0.85f), new Vector3(0.42f, 0.06f, 0.42f), madera);
                Caja(s, "Plato", new Vector3(-0.8f + i * 0.5f, piso + 0.8f, -2.4f + j * 0.28f), new Vector3(0.24f, 0.015f, 0.24f), Mat("Plato", null, new Color(0.8f, 0.78f, 0.72f)), false);
            }
        Caja(s, "Alacena", new Vector3(-3.4f, piso + 0.9f, -4.0f), new Vector3(1.4f, 1.8f, 0.45f), madera);

        // ---- Cuarto de la niña (-4.3, 1.1): cuna y mecedora
        Caja(s, "Cuna", new Vector3(-5.2f, piso + 0.5f, 2.2f), new Vector3(1.1f, 0.06f, 0.65f), maderaClara);
        for (int i = 0; i < 6; i++) Caja(s, "CunaBarrote", new Vector3(-5.7f + i * 0.2f, piso + 0.75f, 2.52f), new Vector3(0.03f, 0.5f, 0.03f), maderaClara, false);
        Caja(s, "Muneca", new Vector3(-5.1f, piso + 0.62f, 2.15f), new Vector3(0.14f, 0.22f, 0.1f), Mat("Muneca", null, new Color(0.75f, 0.62f, 0.55f)), false);

        // ---- Cuarto del fondo (-5.3, -2.7): la llave, sobre un tocador
        Caja(s, "Tocador", new Vector3(-5.4f, piso + 0.42f, -3.75f), new Vector3(1.1f, 0.84f, 0.45f), madera);
        Caja(s, "Espejo", new Vector3(-5.4f, piso + 1.35f, -3.95f), new Vector3(0.7f, 0.9f, 0.03f), Mat("Espejo", null, new Color(0.5f, 0.55f, 0.6f)), false);
        LlaveBucle llave = CrearLlave(s, "Llave4_Casa", s.TransformPoint(new Vector3(-5.4f, piso + 1.15f, -3.65f)), false, "Llave 4 de 5. Estaba frente al espejo. En el reflejo había alguien más.");

        // ---- Focos: luz tenue en cada cuarto (se van al tomar la llave)
        List<Light> focos = new List<Light>
        {
            Luz(s, "Foco_Pasillo", new Vector3(0, piso + 2.9f, 4.8f), new Color(1f, 0.8f, 0.55f), 5f, 0.9f, true),
            Luz(s, "Foco_Sala", new Vector3(0, piso + 2.9f, 1.4f), new Color(1f, 0.8f, 0.55f), 5f, 0.7f, true),
            Luz(s, "Foco_Comedor", new Vector3(-0.8f, piso + 2.6f, -2.4f), new Color(1f, 0.75f, 0.5f), 5f, 0.9f, true),
            Luz(s, "Foco_Recamara", new Vector3(4.4f, piso + 2.9f, -0.6f), new Color(1f, 0.8f, 0.55f), 6f, 0.6f, true),
            Luz(s, "Foco_Ninia", new Vector3(-4.3f, piso + 2.9f, 1.1f), new Color(0.7f, 0.8f, 1f), 5f, 0.5f, true),
            Luz(s, "Foco_Fondo", new Vector3(-5.3f, piso + 2.6f, -2.7f), new Color(1f, 0.5f, 0.35f), 4f, 0.8f, true),
        };
        Luz(s, "LuzPorche", new Vector3(0, 2.8f, 7.6f), new Color(0.6f, 0.7f, 1f), 9f, 1.2f, true);

        // ---- Patio: cerca rota, tendedero, leña, pozo
        Material cerca = Mat("CercaVieja", null, new Color(0.26f, 0.2f, 0.14f));
        for (int i = 0; i < 26; i++)
        {
            float a = i / 26f * Mathf.PI * 2f;
            if (Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, 0f)) < 18f) continue;   // hueco de la entrada (+Z)
            if (i % 7 == 3) continue;                                               // tablas caídas
            Vector3 lp = new Vector3(Mathf.Sin(a) * 13f, 0, Mathf.Cos(a) * 11f);
            Vector3 w = Mundo.EnSuelo(s.TransformPoint(lp));
            Transform poste = Caja(s, "Cerca", s.InverseTransformPoint(w) + Vector3.up * 0.55f, new Vector3(0.1f, 1.1f, 0.1f), cerca);
            poste.localRotation = Quaternion.Euler(Random.Range(-8f, 8f), 0, Random.Range(-8f, 8f));
        }
        Transform pozo = new GameObject("Pozo").transform; pozo.SetParent(s, false);
        pozo.position = W(s, -9f, -6f);
        for (int i = 0; i < 8; i++)
        {
            float a = i * 45f * Mathf.Deg2Rad;
            Caja(pozo, "Piedra", new Vector3(Mathf.Cos(a) * 0.7f, 0.35f, Mathf.Sin(a) * 0.7f), new Vector3(0.55f, 0.7f, 0.3f), Mat("Piedra", null, new Color(0.35f, 0.34f, 0.32f))).localRotation = Quaternion.Euler(0, -i * 45f + 90f, 0);
        }
        Transform lena = new GameObject("Lena").transform; lena.SetParent(s, false); lena.position = W(s, 8.5f, 5f);
        for (int i = 0; i < 9; i++) Caja(lena, "Tronco", new Vector3((i % 3) * 0.25f - 0.25f, 0.12f + (i / 3) * 0.22f, 0), new Vector3(0.22f, 0.22f, 1f), madera);
        Transform tendedero = new GameObject("Tendedero").transform; tendedero.SetParent(s, false); tendedero.position = W(s, -8f, 4f);
        Caja(tendedero, "Poste1", new Vector3(-2f, 1f, 0), new Vector3(0.08f, 2f, 0.08f), madera);
        Caja(tendedero, "Poste2", new Vector3(2f, 1f, 0), new Vector3(0.08f, 2f, 0.08f), madera);
        Caja(tendedero, "Cuerda", new Vector3(0, 1.85f, 0), new Vector3(4f, 0.02f, 0.02f), madera, false);
        Material sabana = Mat("Sabana", null, new Color(0.75f, 0.72f, 0.65f), true);
        Transform sab = Caja(tendedero, "Sabana", new Vector3(0.4f, 1.3f, 0), new Vector3(1.4f, 1.1f, 0.02f), sabana, false);
        ObjetoEmbrujado ob = sab.gameObject.AddComponent<ObjetoEmbrujado>();
        ob.movimiento = ObjetoEmbrujado.Movimiento.Mecer; ob.radio = 10f; ob.soloSinVerlo = false; ob.direccion = Vector3.forward; ob.rearmar = 8f;

        Transform entrada = new GameObject("EntradaMonstruo").transform;
        entrada.SetParent(s, false);
        entrada.localPosition = new Vector3(0, 0, 14f);

        SitioCasa sitio = s.gameObject.AddComponent<SitioCasa>();
        sitio.nombreLugar = "Casa";
        sitio.llave = llave;
        sitio.puerta = puerta;
        sitio.focos = focos.ToArray();
        sitio.entradaMonstruo = entrada;
        sitio.puntoVigia = PuntoVigia(s, inicio, 17f);
        sitio.radioLlegada = 11f;
        return sitio;
    }

    static Transform Punto(Transform padre, Vector3 mundo)
    {
        Transform t = new GameObject("PuntoSalida").transform;
        t.SetParent(padre, true);
        t.position = mundo;
        return t;
    }

    static Material MatIglesia(Material original, int i)
    {
        string n = original != null ? original.name.ToLower() : "";
        string tex = n.Contains("brick") ? "Brick (2)" : n.Contains("door") ? "Door (2)" : n.Contains("window") ? "Window (1)" : n.Contains("plank") ? "Wood Planks" : "Wood (2)";
        Texture2D t = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Escenarios/PSXChurch/" + tex + ".jpg");
        return Mat("Capilla_" + tex.Replace(" ", "").Replace("(", "").Replace(")", ""), t, new Color(0.8f, 0.78f, 0.75f), true);
    }

    // ================================================================== LLAVE 3 y 5: Túnel y Mirador (ya existen)

    static SitioTunel PrepararTunel(TunelLineaK tunel, Vector3 inicio)
    {
        Transform salida = null;
        foreach (Transform t in tunel.GetComponentsInChildren<Transform>()) if (t.name == "Puerta_Salida") salida = t;
        tunel.puertaSalida = salida;
        EditorUtility.SetDirty(tunel);

        LlaveBucle llave = null;
        if (tunel.casillero != null)
        {
            // flota frente al casillero; está oculta hasta que lo abres
            Vector3 dentro = tunel.casillero.transform.position + Vector3.right * 0.4f + Vector3.up * 0.1f;
            llave = CrearLlave(tunel.casillero.transform.parent, "Llave3_Tunel", dentro, false, "Llave 3 de 5. Tenía grabado: LÍNEA K.");
            tunel.casillero.llave = llave;
            EditorUtility.SetDirty(tunel.casillero);
        }
        // Escondite dentro del túnel: la caseta
        if (tunel.monitor != null)
            CrearEscondite(tunel.interior, "Escondite_Caseta", tunel.monitor.position + new Vector3(0.6f, -1.06f, 0.5f), new Vector3(1.2f, 2f, 1f), "caseta");

        SitioTunel s = tunel.GetComponent<SitioTunel>();
        if (s == null) s = tunel.gameObject.AddComponent<SitioTunel>();
        s.nombreLugar = "Muro de concreto";
        s.tunel = tunel;
        s.llave = llave;
        s.puntoVigia = PuntoVigia(tunel.transform, inicio, 14f);
        s.radioLlegada = 12f;
        return s;
    }

    static SitioMirador PrepararMirador(GameObject mir, Vector3 inicio)
    {
        SitioMirador s = mir.GetComponent<SitioMirador>();
        if (s == null) s = mir.AddComponent<SitioMirador>();
        Transform poste = mir.transform.Find("Poste_Baliza");
        Vector3 p = poste != null ? poste.position + Vector3.up * -0.3f + mir.transform.right * 0.35f : mir.transform.position + Vector3.up * 1.3f;
        s.llave = CrearLlave(mir.transform, "Llave5_Mirador", p, false, "Llave 5 de 5. Ya las tienes todas. ...¿y ahora?");
        s.nombreLugar = "Mirador";
        s.puntoVigia = null;
        s.radioLlegada = 6f;
        // Escondite: matorral junto a la banca
        GameObject arb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        arb.name = "Escondite_Matorral";
        arb.transform.SetParent(mir.transform, false);
        arb.transform.localPosition = new Vector3(2.8f, 0.6f, -2.5f);
        arb.transform.localScale = new Vector3(2.3f, 1.4f, 2.3f);
        arb.GetComponent<Collider>().isTrigger = true;
        arb.GetComponent<Renderer>().sharedMaterial = Mat("Maleza", null, new Color(0.08f, 0.13f, 0.06f));
        Escondite e = arb.AddComponent<Escondite>();
        e.nombre = "matorral";
        return s;
    }

    // ================================================================== La puerta del bucle

    static PuertaBucle CrearPuertaBucle(Transform raiz, Vector3 inicio, Vector3 adelante)
    {
        adelante.y = 0; adelante.Normalize();
        Vector3 lado = Vector3.Cross(Vector3.up, adelante);
        Vector3 pos = Mundo.EnSuelo(inicio - adelante * 7f + lado * 4f);
        Transform t = new GameObject("PuertaDelBucle").transform;
        t.SetParent(raiz);
        t.position = pos;
        t.rotation = Quaternion.LookRotation(-adelante);   // al cruzarla te alejas del claro

        Modelo("Fence_Gate_01", t, pos, t.eulerAngles.y + 180f, true);
        Material fierro = Mat("MetalOxidado", null, new Color(0.25f, 0.18f, 0.14f));
        Transform bis = new GameObject("Hoja").transform;
        bis.SetParent(t, false);
        bis.localPosition = new Vector3(-0.8f, 0, 0);
        for (int i = 0; i <= 8; i++) Caja(bis, "Barrote", new Vector3(i * 0.2f, 1f, 0), new Vector3(0.04f, 2f, 0.04f), fierro);
        Caja(bis, "Travesaño", new Vector3(0.8f, 1.5f, 0), new Vector3(1.6f, 0.06f, 0.06f), fierro);
        Caja(bis, "Travesaño2", new Vector3(0.8f, 0.4f, 0), new Vector3(1.6f, 0.06f, 0.06f), fierro);
        PuertaBisagra hoja = bis.gameObject.AddComponent<PuertaBisagra>();
        hoja.anguloAbierta = -110f;
        hoja.duracion = 3f;

        Light[] luces = new Light[5];
        Material cera = Mat("Cera", null, new Color(0.85f, 0.82f, 0.7f));
        for (int i = 0; i < 5; i++)
        {
            float x = -1.6f + i * 0.8f;
            Caja(t, "Vela_" + i, new Vector3(x, 0.15f, -0.9f), new Vector3(0.08f, 0.3f, 0.08f), cera, false);
            luces[i] = Luz(t, "LuzLlave_" + i, new Vector3(x, 0.4f, -0.9f), new Color(1f, 0.8f, 0.45f), 3f, 1.4f, true);
        }
        Light central = Luz(t, "LuzDelOtroLado", new Vector3(0, 1.5f, 2f), new Color(0.75f, 0.85f, 1f), 12f, 3f, true);

        PuertaBucle p = t.gameObject.AddComponent<PuertaBucle>();
        p.lucesLlaves = luces;
        p.hoja = hoja;
        p.luzCentral = central;
        return p;
    }
}
