using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Ambientación detallada (la llama el menú 12 al final; también se puede correr sola con el menú 14).
/// - Arregla al Vigía (salía en pose T): usa el modelo de Mixamo como cuerpo y todas las animaciones en Humanoid.
/// - Portal de las 5 llaves en la puerta del bucle.
/// - Cada lugar como mini escenario, con referencias:
///   · Campamento → Blair Witch (figuras de ramas colgando, montones de piedras), Until Dawn (campamento abandonado), trampas para oso.
///   · Panteón → Día de Muertos (ofrenda, cempasúchil, papel picado, veladoras), Silent Hill (niebla y faroles).
///   · Túnel → Metro 2033 / Outlast (letreros de obra, pintas, notas de trabajadores, luz roja de emergencia).
///   · Capilla → Fatal Frame / Resident Evil 4 (veladoras por cientos, santos sin cara, pintas en la pared).
///   · Mirador → descansos mexicanos (cruces con flores a la orilla del barranco).
/// - Letreros de sendero entre los lugares (Silent Hill) y notas del equipo perdido para leer.
/// </summary>
public static class MonterreyAmbientacionSetup
{
    const string Tex = "Assets/Escenarios/Texturas";
    const string Mats = "Assets/Escenarios/Materiales";
    const string Camp = "Assets/Escenarios/PSXForestCamp";
    const string Pant = "Assets/Escenarios/PSXGraveyard";
    const string Guia = "Assets/Animations/Animaciones Monterrey Incident";

    [MenuItem("Monterrey/14. Solo ambientación: detalles de lugares + portal + arreglar Vigía")]
    public static void Construir()
    {
        Random.InitState(9811);
        ConfigurarImportaciones();
        ArreglarVigia();

        MisionManager mm = Object.FindAnyObjectByType<MisionManager>();
        if (mm == null || mm.sitios == null) { Debug.LogError("[Monterrey] Corre primero el menú 12."); return; }

        foreach (Transform viejo in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            if (viejo != null && viejo.name == "Ambientacion") Object.DestroyImmediate(viejo.gameObject);

        foreach (SitioLlave s in mm.sitios)
        {
            if (s == null) continue;
            Transform a = new GameObject("Ambientacion").transform;
            a.SetParent(s.transform, false);
            switch (s)
            {
                case SitioCampamento c: Campamento(c.transform, a); break;
                case SitioPanteon p: Panteon(p.transform, a); break;
                case SitioTunel t: Tunel(t, a); break;
                case SitioIglesia i: Capilla(i.transform, a); break;
                case SitioCasa ca: Casa(ca.transform, a); break;
                case SitioMirador m: Mirador(m.transform, a); break;
            }
        }
        if (mm.puertaBucle != null) Portal(mm.puertaBucle);
        Senderos(mm);
        MonterreyNotasSetup.Colocar(mm);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[Monterrey] Ambientación lista: Vigía con animaciones, portal, ofrenda, veladoras, trampas, notas y letreros.");
    }

    // ================================================================== Importación

    static void ConfigurarImportaciones()
    {
        foreach (string g in AssetDatabase.FindAssets("t:Texture2D", new[] { Camp + "/Textures", Tex }))
        {
            TextureImporter ti = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(g)) as TextureImporter;
            if (ti == null) continue;
            bool alfa = ti.assetPath.Contains("Pinta") || ti.assetPath.Contains("Papel") || ti.assetPath.Contains("Cempa") ||
                        ti.assetPath.Contains("Flama") || ti.assetPath.Contains("Mano") || ti.assetPath.Contains("Portal") || ti.assetPath.Contains("Particula");
            ti.filterMode = ti.assetPath.Contains("Portal") || ti.assetPath.Contains("Particula") ? FilterMode.Bilinear : FilterMode.Point;
            ti.alphaIsTransparency = alfa;
            ti.mipmapEnabled = false;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.SaveAndReimport();
        }
        foreach (string g in AssetDatabase.FindAssets("t:Model", new[] { Camp + "/FBX" }))
        {
            ModelImporter mi = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(g)) as ModelImporter;
            if (mi == null) continue;
            mi.importCameras = false; mi.importLights = false; mi.animationType = ModelImporterAnimationType.None;
            mi.materialImportMode = ModelImporterMaterialImportMode.None;
            mi.SaveAndReimport();
        }
    }

    // ================================================================== El Vigía (pose T)

    /// <summary>
    /// El modelo "GuiaIncident_texture" no tiene el esqueleto de Mixamo, así que sus animaciones no le aplicaban
    /// (por eso la pose T). Solución: el cuerpo del Vigía ahora es el FBX de Mixamo "Breathing Idle" (mismo personaje,
    /// con esqueleto mixamorig) y todos los clips pasan a Humanoid, que se retargetean entre sí sin problema.
    /// </summary>
    public static void ArreglarVigia()
    {
        // Los FBX de la carpeta Guia/ son SOLO animación (sin malla). El que trae la malla con piel de Mixamo es "M_Incident saludar".
        string cuerpoRuta = Guia + "/M_Incident saludar.fbx";
        List<string> fbx = new List<string>();
        foreach (string g in AssetDatabase.FindAssets("t:Model", new[] { Guia }))
        {
            string p = AssetDatabase.GUIDToAssetPath(g);
            if (p.EndsWith("GuiaIncident_texture.fbx") || p.Contains("MtyIncident01") || p.Contains("Terror watchet")) continue;   // no son de Mixamo
            fbx.Add(p);
        }
        foreach (string p in fbx)
        {
            ModelImporter mi = AssetImporter.GetAtPath(p) as ModelImporter;
            if (mi == null) continue;
            mi.animationType = ModelImporterAnimationType.Human;
            mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            mi.importCameras = false; mi.importLights = false;
            ModelImporterClipAnimation[] clips = mi.clipAnimations.Length > 0 ? mi.clipAnimations : mi.defaultClipAnimations;
            string n = System.IO.Path.GetFileNameWithoutExtension(p).ToLower();
            bool bucle = n.Contains("idle") || n.Contains("talk") || n.Contains("walk") || n.Contains("caminar") || n.Contains("sitting");
            foreach (ModelImporterClipAnimation c in clips)
            {
                c.loopTime = bucle;
                c.lockRootRotation = true; c.lockRootHeightY = true; c.lockRootPositionXZ = true;
                c.keepOriginalOrientation = true; c.keepOriginalPositionY = true; c.keepOriginalPositionXZ = true;
            }
            mi.clipAnimations = clips;
            mi.SaveAndReimport();
        }

        GuiaController gc = Object.FindAnyObjectByType<GuiaController>();
        GameObject cuerpoPf = AssetDatabase.LoadAssetAtPath<GameObject>(cuerpoRuta);
        Avatar avatar = null;
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(cuerpoRuta)) if (o is Avatar av) avatar = av;
        if (gc == null || cuerpoPf == null) { Debug.LogWarning("[Monterrey] No encontré al Vigía o su FBX de Mixamo."); return; }
        if (avatar == null || !avatar.isHuman) Debug.LogWarning("[Monterrey] El avatar del Vigía no quedó Humanoid. Revisa el FBX Breathing Idle.");

        GameObject raiz = gc.gameObject;
        if (PrefabUtility.IsPartOfPrefabInstance(raiz)) PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(raiz), PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        // Altura y material del modelo viejo
        float altura = 1.9f;
        Material[] mats = null;
        // material original del Vigía (del modelo sin esqueleto)
        GameObject original = AssetDatabase.LoadAssetAtPath<GameObject>(Guia + "/GuiaIncident_texture.fbx");
        if (original != null) { Renderer ro = original.GetComponentInChildren<Renderer>(); if (ro != null) mats = ro.sharedMaterials; }
        Renderer[] viejos = raiz.GetComponentsInChildren<Renderer>(true);
        if (viejos.Length > 0)
        {
            Bounds b = viejos[0].bounds; foreach (Renderer r in viejos) b.Encapsulate(r.bounds);
            if (b.size.y > 0.5f && b.size.y < 4f) altura = b.size.y;
            if (mats == null) mats = viejos[0].sharedMaterials;
        }
        // Quitar todo lo viejo (malla en pose T, cámara/luces de vitrina, modelo anterior)
        for (int i = raiz.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(raiz.transform.GetChild(i).gameObject);
        foreach (Renderer r in raiz.GetComponents<Renderer>()) Object.DestroyImmediate(r);
        Animator viejoA = raiz.GetComponent<Animator>();
        if (viejoA != null) Object.DestroyImmediate(viejoA);

        GameObject cuerpo = (GameObject)PrefabUtility.InstantiatePrefab(cuerpoPf);
        PrefabUtility.UnpackPrefabInstance(cuerpo, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        cuerpo.name = "Cuerpo_Vigia";
        cuerpo.transform.SetParent(raiz.transform, false);
        cuerpo.transform.localPosition = Vector3.zero;
        cuerpo.transform.localRotation = Quaternion.identity;
        foreach (Transform t in cuerpo.GetComponentsInChildren<Transform>(true))
            if (t.name.StartsWith("PS2_") && t.GetComponent<SkinnedMeshRenderer>() == null && t.GetComponent<MeshRenderer>() == null) t.gameObject.SetActive(false);
        foreach (Camera c in cuerpo.GetComponentsInChildren<Camera>(true)) Object.DestroyImmediate(c.gameObject);
        foreach (Light l in cuerpo.GetComponentsInChildren<Light>(true)) Object.DestroyImmediate(l.gameObject);

        Renderer[] nuevos = cuerpo.GetComponentsInChildren<Renderer>(true);
        if (nuevos.Length > 0)
        {
            Bounds nb = nuevos[0].bounds; foreach (Renderer r in nuevos) nb.Encapsulate(r.bounds);
            if (nb.size.y > 0.01f) cuerpo.transform.localScale *= altura / nb.size.y;
            foreach (Renderer r in nuevos)
            {
                if (mats != null && mats.Length > 0) r.sharedMaterials = mats;
                if (r is SkinnedMeshRenderer smr) smr.updateWhenOffscreen = true;
            }
        }

        Animator a = cuerpo.GetComponent<Animator>();
        if (a == null) a = cuerpo.AddComponent<Animator>();
        a.avatar = avatar;
        a.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(Guia + "/Guia/VigiaTerror.controller");
        a.applyRootMotion = false;
        a.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        gc.animator = a;
        EditorUtility.SetDirty(gc);
        Debug.Log("[Monterrey] Vigía: cuerpo de Mixamo + animaciones Humanoid (" + fbx.Count + " FBX). Avatar humano: " + (avatar != null && avatar.isHuman));
    }

    // ================================================================== Ayudantes

    internal static Material Mat(string n, Texture2D t, Color c, bool recorte = false, bool unlit = false, bool dobleCara = false)
    {
        string ruta = Mats + "/" + n + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        Shader sh = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
        if (m == null) { m = new Material(sh); AssetDatabase.CreateAsset(m, ruta); } else m.shader = sh;
        m.SetTexture("_BaseMap", t);
        m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.05f);
        if (recorte) { m.SetFloat("_AlphaClip", 1f); m.SetFloat("_Cutoff", 0.35f); m.EnableKeyword("_ALPHATEST_ON"); }
        if (dobleCara || recorte) m.SetFloat("_Cull", 0f);
        EditorUtility.SetDirty(m);
        return m;
    }

    internal static Texture2D T(string n) => AssetDatabase.LoadAssetAtPath<Texture2D>(Tex + "/" + n + ".png");

    internal static GameObject Modelo(string pack, string nombre, Transform padre, Vector3 pos, float giro, bool collider = true, float escala = 1f)
    {
        GameObject pf = AssetDatabase.LoadAssetAtPath<GameObject>(pack + "/FBX/" + nombre + ".fbx");
        if (pf == null) { Debug.LogWarning("Falta " + nombre); return new GameObject(nombre + "_FALTA"); }
        GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(pf);
        PrefabUtility.UnpackPrefabInstance(g, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        g.name = nombre;
        g.transform.SetParent(padre, true);
        g.transform.position = Mundo.EnSuelo(pos, -0.02f);
        g.transform.rotation = Quaternion.Euler(0, giro, 0) * g.transform.rotation;
        g.transform.localScale *= escala;
        Material m = Mat((pack.Contains("Camp") ? "Camp_" : "Panteon_") + nombre, AssetDatabase.LoadAssetAtPath<Texture2D>(pack + "/Textures/" + nombre + "_Albedo.png"), Color.white);
        foreach (Renderer r in g.GetComponentsInChildren<Renderer>())
        {
            r.sharedMaterial = m;
            if (collider && r.GetComponent<Collider>() == null && r.GetComponent<MeshFilter>() != null)
                r.gameObject.AddComponent<MeshCollider>().sharedMesh = r.GetComponent<MeshFilter>().sharedMesh;
        }
        return g;
    }

    static Vector3 W(Transform s, float x, float z) => Mundo.EnSuelo(s.TransformPoint(new Vector3(x, 0, z)));

    internal static Transform Cubo(Transform p, string n, Vector3 posMundo, Vector3 esc, Quaternion rot, Material m, bool col = false)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
        g.name = n; g.transform.SetParent(p, true); g.transform.position = posMundo; g.transform.rotation = rot; g.transform.localScale = esc;
        g.GetComponent<Renderer>().sharedMaterial = m;
        if (!col) Object.DestroyImmediate(g.GetComponent<Collider>());
        return g.transform;
    }

    internal static Transform Cilindro(Transform p, string n, Vector3 posMundo, Vector3 esc, Material m)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        g.name = n; g.transform.SetParent(p, true); g.transform.position = posMundo; g.transform.localScale = esc;
        g.GetComponent<Renderer>().sharedMaterial = m;
        Object.DestroyImmediate(g.GetComponent<Collider>());
        return g.transform;
    }

    internal static Transform Plano(Transform p, string n, Vector3 posMundo, Quaternion rot, Vector2 tam, Material m)
    {
        GameObject g = GameObject.CreatePrimitive(PrimitiveType.Quad);
        g.name = n; Object.DestroyImmediate(g.GetComponent<Collider>());
        g.transform.SetParent(p, true); g.transform.position = posMundo; g.transform.rotation = rot;
        g.transform.localScale = new Vector3(tam.x, tam.y, 1);
        Renderer r = g.GetComponent<Renderer>(); r.sharedMaterial = m; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return g.transform;
    }

    internal static Light Luz(Transform p, Vector3 posMundo, Color c, float rango, float intens, bool parpadeo = true)
    {
        GameObject g = new GameObject("Luz"); g.transform.SetParent(p, true); g.transform.position = posMundo;
        Light l = g.AddComponent<Light>(); l.type = LightType.Point; l.color = c; l.range = rango; l.intensity = intens; l.shadows = LightShadows.None;
        if (parpadeo) g.AddComponent<Parpadeo>();
        return l;
    }

    /// <summary>Grupo de veladoras (velas blancas y rojas con flama), con UNA luz para todo el grupo.</summary>
    internal static void Veladoras(Transform p, Vector3 centro, int n, float radio, bool luz = true)
    {
        Material blanca = Mat("Cera", null, new Color(0.88f, 0.85f, 0.75f));
        Material roja = Mat("VeladoraRoja", null, new Color(0.55f, 0.06f, 0.05f));
        Material flama = Mat("Flama", T("Flama"), Color.white, true, true);
        for (int i = 0; i < n; i++)
        {
            Vector2 r = Random.insideUnitCircle * radio;
            Vector3 b = Mundo.EnSuelo(centro + new Vector3(r.x, 0, r.y));
            b.y = Mathf.Max(b.y, centro.y);
            float h = Random.Range(0.08f, 0.22f);
            Cilindro(p, "Veladora", b + Vector3.up * h * 0.5f, new Vector3(0.07f, h * 0.5f, 0.07f), Random.value < 0.4f ? roja : blanca);
            Vector3 f = b + Vector3.up * (h + 0.05f);
            Plano(p, "Flama", f, Quaternion.Euler(0, Random.Range(0, 180f), 0), new Vector2(0.06f, 0.11f), flama);
            Plano(p, "Flama", f, Quaternion.Euler(0, Random.Range(0, 180f) + 90f, 0), new Vector2(0.06f, 0.11f), flama);
        }
        if (luz) Luz(p, centro + Vector3.up * 0.5f, new Color(1f, 0.6f, 0.28f), 4.5f + radio * 2f, 1.6f);
    }

    internal static void Cempasuchil(Transform p, Vector3 centro, int n, float radio)
    {
        Material flor = Mat("Cempasuchil", T("Cempasuchil"), Color.white, true);
        for (int i = 0; i < n; i++)
        {
            Vector2 r = Random.insideUnitCircle * radio;
            Vector3 b = Mundo.EnSuelo(centro + new Vector3(r.x, 0, r.y), 0.02f);
            b.y = Mathf.Max(b.y, centro.y + 0.02f);
            Plano(p, "Petalos", b, Quaternion.Euler(90, Random.Range(0f, 360f), 0), Vector2.one * Random.Range(0.15f, 0.3f), flor);
        }
    }

    internal static void Pinta(Transform p, string tex, Vector3 pos, Quaternion rot, float ancho)
    {
        Texture2D t = T(tex);
        float alto = t != null ? ancho * t.height / t.width : ancho * 0.5f;
        Plano(p, "Pinta_" + tex, pos, rot, new Vector2(ancho, alto), Mat(tex, t, Color.white, true));
    }

    internal static NotaLeible Nota(Transform p, Vector3 pos, string titulo, string texto, string tex = "Nota")
    {
        Transform hoja = Plano(p, "Nota", pos + Vector3.up * 0.02f, Quaternion.Euler(90, Random.Range(0f, 360f), 0), new Vector2(0.25f, 0.31f), Mat(tex, T(tex), Color.white, false, false, true));
        GameObject g = new GameObject("NotaLeible"); g.transform.SetParent(p, true); g.transform.position = pos;
        BoxCollider bc = g.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = new Vector3(0.8f, 0.6f, 0.8f); bc.center = Vector3.up * 0.2f;
        NotaLeible n = g.AddComponent<NotaLeible>(); n.titulo = titulo; n.texto = texto;
        Luz(p, pos + Vector3.up * 0.4f, new Color(0.9f, 0.9f, 1f), 1.5f, 0.6f, false);
        return n;
    }

    /// <summary>Figura de ramas (Blair Witch) colgando de un árbol, se mece.</summary>
    static void FiguraRamas(Transform p, Vector3 pos, float escala)
    {
        Material rama = Mat("Rama", null, new Color(0.2f, 0.15f, 0.1f));
        Transform piv = new GameObject("FiguraRamas").transform; piv.SetParent(p, true); piv.position = pos + Vector3.up * 2.6f;
        float e = escala;
        Cubo(piv, "Cuerda", piv.position + Vector3.down * 0.2f * e, new Vector3(0.01f, 0.4f, 0.01f) * e, Quaternion.identity, rama);
        Cubo(piv, "Vertical", piv.position + Vector3.down * 0.75f * e, new Vector3(0.04f, 0.8f, 0.04f) * e, Quaternion.identity, rama);
        Cubo(piv, "Brazos", piv.position + Vector3.down * 0.55f * e, new Vector3(0.6f, 0.03f, 0.03f) * e, Quaternion.Euler(0, 0, 10), rama);
        Cubo(piv, "PiernaA", piv.position + new Vector3(-0.12f, -1.25f, 0) * e, new Vector3(0.03f, 0.45f, 0.03f) * e, Quaternion.Euler(0, 0, 18), rama);
        Cubo(piv, "PiernaB", piv.position + new Vector3(0.12f, -1.25f, 0) * e, new Vector3(0.03f, 0.45f, 0.03f) * e, Quaternion.Euler(0, 0, -18), rama);
        ObjetoEmbrujado o = piv.gameObject.AddComponent<ObjetoEmbrujado>();
        o.movimiento = ObjetoEmbrujado.Movimiento.Mecer; o.radio = 9f; o.soloSinVerlo = false; o.rearmar = 12f; o.direccion = Vector3.right;
    }

    static void MontonPiedras(Transform p, Vector3 pos)
    {
        Material piedra = Mat("Piedra", null, new Color(0.35f, 0.34f, 0.32f));
        for (int i = 0; i < 5; i++)
        {
            float s = 0.32f - i * 0.05f;
            Cubo(p, "Piedra", Mundo.EnSuelo(pos) + Vector3.up * (0.05f + i * 0.1f), new Vector3(s, 0.1f, s * 0.8f), Quaternion.Euler(0, Random.Range(0, 90f), Random.Range(-6f, 6f)), piedra);
        }
    }

    internal static void Trampa(Transform p, Vector3 pos)
    {
        GameObject m = Modelo(Camp, "Bear_Trap_01", p, pos, Random.Range(0f, 360f), false);
        GameObject g = new GameObject("TrampaOso"); g.transform.SetParent(p, true); g.transform.position = Mundo.EnSuelo(pos);
        BoxCollider bc = g.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = new Vector3(0.9f, 0.5f, 0.6f); bc.center = Vector3.up * 0.2f;
        TrampaOso t = g.AddComponent<TrampaOso>(); t.modelo = m.transform;
    }

    internal static void Pilar(Transform p, Vector3 pos, Color c)
    {
        // luz alta que se ve desde lejos entre la niebla (guía visual hacia el lugar)
        Light l = Luz(p, pos + Vector3.up * 9f, c, 22f, 1.4f, true);
        l.name = "LuzGuia";
    }

    // ================================================================== Lugares

    static void Campamento(Transform s, Transform a)
    {
        Pilar(a, s.position, new Color(1f, 0.55f, 0.25f));
        Modelo(Camp, "Campfire_01", a, W(s, 0, 0), s.eulerAngles.y, false);
        Modelo(Camp, "Tent_01", a, W(s, -6f, 3.5f), s.eulerAngles.y + 160f);
        Modelo(Camp, "Tent_01", a, W(s, 6.5f, 1.5f), s.eulerAngles.y - 120f);
        Modelo(Camp, "Picnic_Table_01", a, W(s, 3.5f, 5f), s.eulerAngles.y + 15f);
        Modelo(Camp, "Cooler_01", a, W(s, 4.6f, 4f), s.eulerAngles.y + 40f);
        Modelo(Camp, "Firewood_Pile_01", a, W(s, -2f, -2.5f), s.eulerAngles.y);
        Modelo(Camp, "Log_Seat_01", a, W(s, -1.6f, 1.2f), s.eulerAngles.y + 70f);
        Modelo(Camp, "Log_Seat_01", a, W(s, 1.4f, -1.4f), s.eulerAngles.y - 20f);
        Modelo(Camp, "Stump_Axe_01", a, W(s, -4f, -4.5f), s.eulerAngles.y + 30f);
        GameObject letrero = Modelo(Camp, "Sign_Trail_01", a, W(s, 0f, 9f), s.eulerAngles.y + 180f);
        GameObject lin = Modelo(Camp, "Lantern_01", a, W(s, 3.4f, 5f) + Vector3.up * 0.8f, 0f, false);
        lin.transform.position = W(s, 3.4f, 5f) + Vector3.up * 0.8f;
        Luz(a, lin.transform.position + Vector3.up * 0.2f, new Color(1f, 0.75f, 0.4f), 6f, 1.3f).gameObject.AddComponent<ParpadeoFuerte>().encendido = 3f;

        // Blair Witch: figuras de ramas colgando y montones de piedras
        foreach (Vector3 arbol in Mundo.ArbolesCerca(s.position, 7f, 20f, 6)) FiguraRamas(a, arbol + (s.position - arbol).normalized * 0.7f, Random.Range(0.8f, 1.2f));
        MontonPiedras(a, W(s, 2.5f, -5f)); MontonPiedras(a, W(s, -5.5f, 0.5f)); MontonPiedras(a, W(s, 5f, -3f));

        // Trampas escondidas en el pasto alrededor
        Trampa(a, W(s, -3f, 6f)); Trampa(a, W(s, 7f, -4f)); Trampa(a, W(s, -7f, -3f));

        Nota(a, W(s, 1.6f, 3.2f), "DIARIO DE MARIO — 27/11/98",
            "Llegamos a La Pastora a las 9. Rogelio dice que hay un túnel del metro que nunca terminaron, aquí, en el cerro.\n\n" +
            "Hay un señor en el bosque. No dice nada. Solo saluda. Lo vimos tres veces.\n\n" +
            "Ana dice que lo vio sonreír detrás de su casa de campaña. Yo no vi nada.\n\nSon las 11:47. Otra vez son las 11:47.");
        Pinta(a, "Pinta_NoVoltees", W(s, 0f, 9f) + Vector3.up * 1.2f + s.forward * 0.1f, Quaternion.LookRotation(-s.forward), 1.1f);
    }

    static void Panteon(Transform s, Transform a)
    {
        Pilar(a, s.position, new Color(1f, 0.5f, 0.15f));
        Material madera = Mat("Durmiente", null, new Color(0.25f, 0.17f, 0.1f));
        Material mantel = Mat("MantelMorado", null, new Color(0.3f, 0.12f, 0.35f));
        Material foto = Mat("Polaroid", T("Polaroid"), Color.white, false, false, true);
        Material papel = Mat("PapelPicado", T("PapelPicado"), Color.white, true);

        // Papel picado sobre la entrada (dos tiras)
        for (int i = 0; i < 2; i++)
        {
            Vector3 c = W(s, 0, 10.2f) + Vector3.up * (2.6f + i * 0.35f);
            for (int k = -2; k <= 2; k++)
                Plano(a, "PapelPicado", c + s.right * k * 1.05f + Vector3.down * Mathf.Abs(k) * 0.08f, Quaternion.LookRotation(s.forward), new Vector2(1f, 0.5f), papel);
        }

        // Ofrenda de tres niveles frente a la cripta (Día de Muertos)
        Vector3 o = W(s, 0, -5.5f);
        Quaternion r = s.rotation;
        Cubo(a, "Ofrenda1", o + Vector3.up * 0.25f + s.forward * 2.8f, new Vector3(2.4f, 0.5f, 1.2f), r, mantel, true);
        Cubo(a, "Ofrenda2", o + Vector3.up * 0.65f + s.forward * 2.6f, new Vector3(1.8f, 0.3f, 0.8f), r, mantel, true);
        Cubo(a, "Ofrenda3", o + Vector3.up * 0.95f + s.forward * 2.45f, new Vector3(1.2f, 0.3f, 0.5f), r, mantel, true);
        for (int i = -1; i <= 1; i++)
            Plano(a, "FotoOfrenda", o + Vector3.up * 1.3f + s.forward * 2.42f + s.right * i * 0.32f, Quaternion.LookRotation(-s.forward) * Quaternion.Euler(-8, 0, 0), new Vector2(0.22f, 0.26f), foto);
        Veladoras(a, o + s.forward * 2.8f + Vector3.up * 0.5f, 14, 1.0f);
        Cempasuchil(a, o + s.forward * 4f, 40, 1.6f);
        // Camino de pétalos desde la entrada hasta la ofrenda
        for (float z = 9f; z > -2f; z -= 0.7f) Cempasuchil(a, W(s, Random.Range(-0.3f, 0.3f), z), 2, 0.3f);
        // Arco de cempasúchil sobre la ofrenda
        Material florM = Mat("Cempasuchil", T("Cempasuchil"), Color.white, true);
        for (int i = 0; i <= 10; i++)
        {
            float ang = Mathf.PI * i / 10f;
            Vector3 p = o + s.forward * 2.4f + s.right * Mathf.Cos(ang) * 1.3f + Vector3.up * (1f + Mathf.Sin(ang) * 1.1f);
            Plano(a, "ArcoFlor", p, Quaternion.LookRotation(-s.forward), Vector2.one * 0.3f, florM);
        }

        // Veladoras en varias tumbas + cruces de madera chuecas
        for (int i = 0; i < 9; i++) Veladoras(a, W(s, Random.Range(-9f, 9f), Random.Range(-7f, 8f)), Random.Range(3, 7), 0.35f, i % 3 == 0);
        for (int i = 0; i < 6; i++)
        {
            Vector3 p = W(s, Random.Range(-10f, 10f), Random.Range(-8f, 8f));
            Quaternion rr = s.rotation * Quaternion.Euler(Random.Range(-14f, 14f), Random.Range(-20f, 20f), Random.Range(-14f, 14f));
            Cubo(a, "CruzMadera", p + Vector3.up * 0.55f, new Vector3(0.07f, 1.1f, 0.07f), rr, madera);
            Cubo(a, "CruzMaderaH", p + rr * new Vector3(0, 0.8f, 0), new Vector3(0.5f, 0.07f, 0.07f), rr, madera);
        }

        Nota(a, W(s, 1.5f, 6f), "FOTO RECORTADA",
            "Atrás de la foto, con pluma:\n\n\"Si ves que las velas se prenden solas, ya es tarde.\nPrende las tres de la calavera. La reja abre.\nNo mires dentro del ataúd más de lo necesario.\"", "Polaroid");
        Pinta(a, "Pinta_CuantasVeces", W(s, 0, -9.2f) + Vector3.up * 1.6f, Quaternion.LookRotation(s.forward), 1.6f);
    }

    static void Tunel(SitioTunel st, Transform a)
    {
        TunelLineaK t = st.tunel;
        if (t == null || t.interior == null) return;
        Transform I = t.interior;
        Transform ai = new GameObject("Ambientacion").transform; ai.SetParent(I, false);
        Pilar(a, t.muro != null ? t.muro.position : st.transform.position, new Color(0.7f, 0.9f, 1f));

        // Afuera: cinta de precaución y un letrero de obra
        Material amarillo = Mat("Precaucion", null, new Color(0.85f, 0.7f, 0.05f));
        Material negro = Mat("Plastico", null, new Color(0.08f, 0.08f, 0.08f));
        Transform tr = st.transform;
        for (int i = -1; i <= 1; i += 2)
            Cubo(a, "PostePrecaucion", W(tr, i * 3.5f, 2.2f) + Vector3.up * 0.5f, new Vector3(0.08f, 1f, 0.08f), tr.rotation, amarillo, true);
        Cubo(a, "CintaPrecaucion", W(tr, 0, 2.2f) + Vector3.up * 0.85f, new Vector3(7f, 0.08f, 0.01f), tr.rotation * Quaternion.Euler(0, 0, 2f), amarillo);

        // Adentro: pintas, manos, notas, luz roja de emergencia, señalización
        Pinta(ai, "Pinta_ElTeGuia", I.TransformPoint(new Vector3(-4.98f, 2.6f, -2f)), Quaternion.LookRotation(Vector3.left), 2.2f);
        Pinta(ai, "Mano", I.TransformPoint(new Vector3(-4.97f, 1.8f, 4f)), Quaternion.LookRotation(Vector3.left), 0.5f);
        Pinta(ai, "Mano", I.TransformPoint(new Vector3(-4.97f, 1.6f, 4.6f)), Quaternion.LookRotation(Vector3.left) * Quaternion.Euler(0, 0, 25), 0.5f);
        Pinta(ai, "Mano", I.TransformPoint(new Vector3(4.97f, 1.4f, 12f)), Quaternion.LookRotation(Vector3.right), 0.5f);
        Light roja = Luz(ai, I.TransformPoint(new Vector3(3f, 4.3f, 14f)), new Color(1f, 0.08f, 0.04f), 12f, 2.5f, false);
        ParpadeoFuerte pf = roja.gameObject.AddComponent<ParpadeoFuerte>(); pf.encendido = 0.6f; pf.apagado = 0.6f; pf.regular = true;
        for (int i = 0; i < 5; i++)
            Cubo(ai, "FranjaAnden", I.TransformPoint(new Vector3(-0.6f, 1.01f, -18f + i * 5.5f)), new Vector3(0.25f, 0.01f, 2.5f), Quaternion.identity, amarillo);
        Nota(ai, I.TransformPoint(new Vector3(-3.5f, 1.0f, -5f)), "BITÁCORA — CUADRILLA 3",
            "12/03/87: Las lámparas fallan en el km 1.2. El ingeniero dice que es la humedad.\n\n" +
            "13/03/87: Chuy dice que vio a alguien parado en las vías. Alto. La cabeza como un sol.\n\n" +
            "14/03/87: Nadie quiere bajar. Se escucha alguien saludando desde el fondo del túnel. Saludando. ¿Cómo se escucha un saludo?");
        Nota(ai, I.TransformPoint(new Vector3(-2.2f, 1.0f, 2.5f)), "PAPEL ARRUGADO", "Contamos 5 cerraduras en la reja del claro.\nCinco llaves. Una está aquí, en el casillero de Chuy.\nLas demás... el panteón, la capilla, el campamento y el mirador.");
    }

    static void Capilla(Transform s, Transform a)
    {
        Pilar(a, s.position + s.forward * 6f, new Color(0.8f, 0.85f, 1f));
        Material santo = Mat("Santo", null, new Color(0.72f, 0.7f, 0.65f));
        Material manto = Mat("MantoAzul", null, new Color(0.12f, 0.16f, 0.3f));
        Material flor = Mat("FlorBlanca", null, new Color(0.85f, 0.85f, 0.8f));
        SitioIglesia si = s.GetComponent<SitioIglesia>();
        float zFondo = si != null && si.llave != null ? s.InverseTransformPoint(si.llave.transform.position).z : -7f;

        // Cientos de veladoras alrededor del altar y a los lados
        Veladoras(a, s.TransformPoint(new Vector3(0, 0, zFondo + 1.2f)), 30, 1.3f);
        Veladoras(a, s.TransformPoint(new Vector3(-2.8f, 0, zFondo + 2.5f)), 18, 0.7f);
        Veladoras(a, s.TransformPoint(new Vector3(2.8f, 0, zFondo + 2.5f)), 18, 0.7f, false);

        // Santos sin cara (Fatal Frame) a los lados del altar, con flores
        for (int i = -1; i <= 1; i += 2)
        {
            Vector3 b = s.TransformPoint(new Vector3(i * 2.6f, 0, zFondo + 0.3f));
            Cubo(a, "Pedestal", b + Vector3.up * 0.4f, new Vector3(0.6f, 0.8f, 0.6f), s.rotation, santo, true);
            Transform cuerpo = Cilindro(a, "SantoCuerpo", b + Vector3.up * 1.35f, new Vector3(0.38f, 0.55f, 0.32f), manto);
            GameObject cab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Object.DestroyImmediate(cab.GetComponent<Collider>());
            cab.transform.SetParent(a, true); cab.transform.position = b + Vector3.up * 2.05f; cab.transform.localScale = Vector3.one * 0.26f;
            cab.GetComponent<Renderer>().sharedMaterial = santo;
            ObjetoEmbrujado o = cab.AddComponent<ObjetoEmbrujado>();
            o.movimiento = ObjetoEmbrujado.Movimiento.Girar; o.radio = 6f; o.soloSinVerlo = true;   // la cabeza voltea a verte
            for (int k = 0; k < 6; k++) Cubo(a, "Flor", b + Vector3.up * 0.85f + new Vector3(Random.Range(-0.2f, 0.2f), 0, Random.Range(-0.2f, 0.2f)), Vector3.one * 0.08f, Quaternion.identity, flor);
        }
        Pinta(a, "Pinta_Cinco", s.TransformPoint(new Vector3(0, 2.2f, zFondo - 0.9f)), Quaternion.LookRotation(s.forward), 1.2f);
        Nota(a, s.TransformPoint(new Vector3(1.2f, 0.05f, zFondo + 4f)), "ORACIÓN ESCRITA A MANO",
            "Virgencita, que ya se acabe la noche.\nLlevo cinco veces aquí. Cinco.\nTomo la llave del altar y las puertas se cierran. Algo entra corriendo.\nMe escondo en el confesionario y aguanto la respiración.\nSi no me escondo... vuelvo a despertar en el claro.");
        Cempasuchil(a, s.TransformPoint(new Vector3(0, 0, 4f)), 20, 1.2f);
    }

    static void Casa(Transform s, Transform a)
    {
        Pilar(a, s.position + s.forward * 9f, new Color(1f, 0.75f, 0.5f));
        float piso = 0.38f;
        // Pintas adentro (Pasillo y cuarto del fondo), veladoras en la sala, cempasúchil en el porche
        Pinta(a, "Pinta_NoVoltees", s.TransformPoint(new Vector3(-2.15f, piso + 1.7f, 4.7f)), s.rotation * Quaternion.Euler(0, 90f, 0), 1.1f);
        Pinta(a, "Pinta_CuantasVeces", s.TransformPoint(new Vector3(-5.3f, piso + 1.8f, -1.0f)), s.rotation * Quaternion.Euler(0, 180f, 0), 1.0f);
        Pinta(a, "Pinta_ElTeGuia", s.TransformPoint(new Vector3(6.68f, piso + 1.6f, 0.8f)), s.rotation * Quaternion.Euler(0, -90f, 0), 1.2f);
        Veladoras(a, s.TransformPoint(new Vector3(0.2f, piso, 1.2f)), 14, 0.6f);
        Veladoras(a, s.TransformPoint(new Vector3(-5.0f, piso, -3.2f)), 8, 0.35f, false);
        Cempasuchil(a, s.TransformPoint(new Vector3(0, 0, 8.5f)), 16, 1.2f);
        // Polaroids de la familia tiradas en el pasillo
        Material pol = Mat("Polaroid", T("Polaroid"), Color.white);
        for (int i = 0; i < 4; i++)
            Plano(a, "Polaroid", s.TransformPoint(new Vector3(Random.Range(-0.7f, 0.7f), piso + 0.02f, 3.8f + i * 0.6f)), s.rotation * Quaternion.Euler(90, Random.Range(0f, 360f), 0), new Vector2(0.12f, 0.14f), pol);
    }

    static void Mirador(Transform s, Transform a)
    {
        Material madera = Mat("CruzBlanca", null, new Color(0.8f, 0.8f, 0.78f));
        Material florM = Mat("FlorPlastico", null, new Color(0.7f, 0.15f, 0.3f));
        // "Descansos": cruces blancas con flores en la orilla del barranco
        for (int i = 0; i < 4; i++)
        {
            Vector3 p = s.TransformPoint(new Vector3(-2.4f + i * 1.6f, 0, 3f + Random.Range(-0.3f, 0.3f)));
            p = Mundo.EnSuelo(p);
            Quaternion rr = s.rotation * Quaternion.Euler(Random.Range(-6f, 6f), Random.Range(-15f, 15f), Random.Range(-6f, 6f));
            Cubo(a, "Descanso", p + Vector3.up * 0.45f, new Vector3(0.06f, 0.9f, 0.06f), rr, madera);
            Cubo(a, "DescansoH", p + rr * new Vector3(0, 0.65f, 0), new Vector3(0.38f, 0.06f, 0.06f), rr, madera);
            for (int k = 0; k < 5; k++) Cubo(a, "Flor", p + new Vector3(Random.Range(-0.25f, 0.25f), 0.08f, Random.Range(-0.25f, 0.25f)), Vector3.one * 0.08f, Quaternion.identity, florM);
            Veladoras(a, p + s.forward * -0.3f, 3, 0.15f, i == 1);
        }
        Nota(a, s.TransformPoint(new Vector3(0.8f, 0.2f, -0.8f)), "PLACA OXIDADA",
            "MIRADOR LA PASTORA\nEn memoria de los 7 trabajadores de la Línea K\ny de los excursionistas que no regresaron.\n\nAlguien rayó debajo:  \"la luz roja miente\".");
    }

    // ================================================================== Portal

    static void Portal(PuertaBucle p)
    {
        Transform viejo = p.transform.Find("Portal");
        if (viejo != null) Object.DestroyImmediate(viejo.gameObject);
        Transform portal = new GameObject("Portal").transform;
        portal.SetParent(p.transform, false);
        portal.localPosition = Vector3.zero;

        Material mRem = Mat("PortalRemolino", T("Portal"), new Color(0.7f, 0.85f, 1f, 1f), true, true);
        Transform rem = Plano(portal, "Remolino", p.transform.position + Vector3.up * 1.4f, p.transform.rotation, Vector2.one, mRem);
        Transform rem2 = Plano(portal, "Remolino2", p.transform.position + Vector3.up * 1.4f + p.transform.forward * 0.05f, p.transform.rotation * Quaternion.Euler(0, 180, 0), Vector2.one, mRem);

        // Partículas que giran hacia el centro
        GameObject pg = new GameObject("Particulas");
        pg.transform.SetParent(portal, false); pg.transform.localPosition = Vector3.up * 1.4f;
        ParticleSystem ps = pg.AddComponent<ParticleSystem>();
        var main = ps.main; main.startLifetime = 2.5f; main.startSpeed = 0f; main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.09f);
        main.startColor = new Color(0.75f, 0.85f, 1f, 0.9f); main.maxParticles = 400; main.simulationSpace = ParticleSystemSimulationSpace.Local;
        var em = ps.emission; em.rateOverTime = 120f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 1.6f; sh.radiusThickness = 0.2f;
        var vel = ps.velocityOverLifetime; vel.enabled = true; vel.orbitalZ = 1.6f; vel.radial = -0.5f;
        var col = ps.colorOverLifetime; col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(new Color(0.5f, 0.7f, 1f), 1) }, new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.2f), new GradientAlphaKey(0, 1) });
        col.color = g;
        ParticleSystemRenderer pr = pg.GetComponent<ParticleSystemRenderer>();
        Material mPart = AssetDatabase.LoadAssetAtPath<Material>(Mats + "/PortalParticula.mat");
        if (mPart == null) { mPart = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")); AssetDatabase.CreateAsset(mPart, Mats + "/PortalParticula.mat"); }
        mPart.SetTexture("_BaseMap", T("Particula"));
        mPart.SetFloat("_Surface", 1f); mPart.SetFloat("_Blend", 2f);   // transparente aditivo
        mPart.SetOverrideTag("RenderType", "Transparent");
        mPart.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha); mPart.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mPart.SetInt("_ZWrite", 0); mPart.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); mPart.renderQueue = 3000;
        EditorUtility.SetDirty(mPart);
        pr.sharedMaterial = mPart;
        pr.renderMode = ParticleSystemRenderMode.Billboard;

        Light luz = Luz(portal, p.transform.position + Vector3.up * 1.4f - p.transform.forward * 0.8f, new Color(0.65f, 0.8f, 1f), 14f, 4f, false);
        Light pilar = Luz(portal, p.transform.position + Vector3.up * 14f, new Color(0.6f, 0.75f, 1f), 40f, 6f, false);

        // Las 5 llaves orbitando
        Material oro = AssetDatabase.LoadAssetAtPath<Material>(Mats + "/LlaveOro.mat");
        Transform[] llaves = new Transform[5];
        for (int i = 0; i < 5; i++)
        {
            Transform k = new GameObject("LlaveOrbita_" + i).transform;
            k.SetParent(portal, false);
            Transform vas = GameObject.CreatePrimitive(PrimitiveType.Cube).transform; Object.DestroyImmediate(vas.GetComponent<Collider>());
            vas.SetParent(k, false); vas.localScale = new Vector3(0.04f, 0.3f, 0.03f); vas.GetComponent<Renderer>().sharedMaterial = oro;
            Transform aro = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform; Object.DestroyImmediate(aro.GetComponent<Collider>());
            aro.SetParent(k, false); aro.localPosition = Vector3.up * 0.2f; aro.localRotation = Quaternion.Euler(90, 0, 0); aro.localScale = new Vector3(0.14f, 0.02f, 0.14f);
            aro.GetComponent<Renderer>().sharedMaterial = oro;
            llaves[i] = k;
        }

        PortalBucle pb = portal.gameObject.AddComponent<PortalBucle>();
        pb.remolino = rem; pb.remolino2 = rem2; pb.luz = luz; pb.pilar = pilar; pb.llaves = llaves;
        p.portal = portal.gameObject;
        portal.gameObject.SetActive(false);
        EditorUtility.SetDirty(p);
    }

    // ================================================================== Senderos

    /// <summary>Letreros de madera (estilo Silent Hill) en el camino hacia cada lugar.</summary>
    static void Senderos(MisionManager mm)
    {
        Transform viejo = GameObject.Find("_Senderos") != null ? GameObject.Find("_Senderos").transform : null;
        if (viejo != null) Object.DestroyImmediate(viejo.gameObject);
        Transform raiz = new GameObject("_Senderos").transform;
        PlayerMovement pm = Object.FindAnyObjectByType<PlayerMovement>();
        if (pm == null) return;
        Vector3 inicio = pm.transform.position;
        foreach (SitioLlave s in mm.sitios)
        {
            if (s == null) continue;
            Vector3 d = s.transform.position - inicio; d.y = 0;
            float dist = d.magnitude; d.Normalize();
            for (float f = 0.3f; f < 0.95f; f += 0.3f)
            {
                Vector3 p = inicio + d * dist * f + Vector3.Cross(Vector3.up, d) * 2.2f;
                GameObject l = Modelo(Camp, "Sign_Trail_01", raiz, p, Quaternion.LookRotation(d).eulerAngles.y + 90f);
                Luz(raiz, Mundo.EnSuelo(p) + Vector3.up * 2.2f, new Color(0.9f, 0.85f, 0.7f), 5f, 0.7f, true);
            }
        }
    }
}
