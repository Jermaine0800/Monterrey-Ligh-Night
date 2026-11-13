using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menú 9: configura los monstruos (La Sonriente y El Asomado), el director de acecho
/// y reparte escondites (arbustos) cerca de los árboles alrededor del jugador.
/// Se puede correr varias veces: reemplaza lo que ya había creado.
/// </summary>
public static class MonterreyMonstruosSetup
{
    const string Carpeta = "Assets/Characters/Monstruos";
    const string RutaSonriente = Carpeta + "/LowbitLab/hooded_figure.fbx";
    const string RutaAsomado = Carpeta + "/DesertCryptids/DC_Mimic.fbx";
    const string RutaCara = Carpeta + "/CaraSonriente.png";

    [MenuItem("Monterrey/9. Configurar monstruos y escondites")]
    public static void Configurar()
    {
        ConfigurarModelo(RutaSonriente);
        ConfigurarModelo(RutaAsomado);
        ConfigurarTextura(Carpeta + "/LowbitLab/hooded_figure_atlas.png");
        ConfigurarTextura(Carpeta + "/DesertCryptids/DC_Mimic_baked.png");
        ConfigurarTextura(RutaCara, true);
        AssetDatabase.Refresh();

        GameObject sistemas = GameObject.Find("_SistemasTerror");
        if (sistemas == null) sistemas = new GameObject("_SistemasTerror");

        Transform viejo = sistemas.transform.Find("Monstruos");
        if (viejo != null) Object.DestroyImmediate(viejo.gameObject);
        GameObject grupo = new GameObject("Monstruos");
        grupo.transform.SetParent(sistemas.transform);

        // --- La Sonriente
        GameObject sonr = CrearMonstruo<LaSonriente>("La_Sonriente", RutaSonriente,
            Carpeta + "/LowbitLab/hooded_figure_atlas.png", new Color(0.22f, 0.22f, 0.24f), 1.95f, grupo.transform);
        sonr.GetComponent<LaSonriente>().nombre = "La Sonriente";
        PonerCara(sonr);

        // --- El Asomado
        GameObject asom = CrearMonstruo<ElAsomado>("El_Asomado", RutaAsomado,
            Carpeta + "/DesertCryptids/DC_Mimic_baked.png", new Color(0.85f, 0.82f, 0.78f), 2.35f, grupo.transform);
        asom.GetComponent<ElAsomado>().nombre = "El Asomado";

        // --- Director
        AcechoDirector dir = sistemas.GetComponentInChildren<AcechoDirector>();
        if (dir == null)
        {
            GameObject g = new GameObject("AcechoDirector");
            g.transform.SetParent(sistemas.transform);
            dir = g.AddComponent<AcechoDirector>();
        }
        dir.habilitado = true;

        int n = CrearEscondites(sistemas.transform);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[Monterrey] Monstruos listos: La Sonriente, El Asomado + AcechoDirector. Escondites: " + n);
    }

    // ---------------------------------------------------------------- Importación

    static void ConfigurarModelo(string ruta)
    {
        ModelImporter imp = AssetImporter.GetAtPath(ruta) as ModelImporter;
        if (imp == null) { Debug.LogWarning("No encontré " + ruta); return; }
        imp.animationType = ModelImporterAnimationType.Generic;
        imp.materialImportMode = ModelImporterMaterialImportMode.None;
        ModelImporterClipAnimation[] clips = imp.clipAnimations.Length > 0 ? imp.clipAnimations : imp.defaultClipAnimations;
        foreach (ModelImporterClipAnimation c in clips)
        {
            string n = c.name.ToLower();
            c.loopTime = n.Contains("idle") || n.Contains("walk") || n.Contains("run");
        }
        imp.clipAnimations = clips;
        imp.SaveAndReimport();
    }

    static void ConfigurarTextura(string ruta, bool alfa = false)
    {
        TextureImporter t = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (t == null) return;
        t.filterMode = FilterMode.Point;
        t.mipmapEnabled = false;
        t.alphaIsTransparency = alfa;
        t.textureCompression = TextureImporterCompression.Uncompressed;
        t.SaveAndReimport();
    }

    // ---------------------------------------------------------------- Monstruo

    static GameObject CrearMonstruo<T>(string nombre, string rutaModelo, string rutaTex, Color tinte, float altura, Transform padre) where T : EnemigoBase
    {
        GameObject raiz = new GameObject(nombre);
        raiz.transform.SetParent(padre);
        raiz.transform.position = new Vector3(0, -500, 0);

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(rutaModelo);
        GameObject modelo = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        PrefabUtility.UnpackPrefabInstance(modelo, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        modelo.name = "Modelo";
        modelo.transform.SetParent(raiz.transform, false);
        modelo.transform.localPosition = Vector3.zero;

        // Escalar a la altura deseada
        Bounds b = Limites(modelo);
        if (b.size.y > 0.01f)
        {
            float f = altura / b.size.y;
            modelo.transform.localScale *= f;
            b = Limites(modelo);
            modelo.transform.position += Vector3.up * (raiz.transform.position.y - b.min.y);
        }

        // Material PSX
        Material mat = CrearMaterial(nombre, AssetDatabase.LoadAssetAtPath<Texture2D>(rutaTex), tinte);
        foreach (Renderer r in modelo.GetComponentsInChildren<Renderer>())
        {
            Material[] ms = r.sharedMaterials;
            for (int i = 0; i < ms.Length; i++) ms[i] = mat;
            r.sharedMaterials = ms;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            if (r is SkinnedMeshRenderer s) s.updateWhenOffscreen = true;
        }

        // Animator
        Animator a = modelo.GetComponent<Animator>();
        if (a == null) a = modelo.AddComponent<Animator>();
        a.runtimeAnimatorController = CrearControlador(nombre, rutaModelo);
        a.applyRootMotion = false;
        a.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        // Collider para que le puedas pegar
        CapsuleCollider col = raiz.AddComponent<CapsuleCollider>();
        col.isTrigger = true;
        col.height = altura;
        col.radius = 0.45f;
        col.center = new Vector3(0, altura * 0.5f, 0);

        raiz.AddComponent<T>();
        return raiz;
    }

    static Bounds Limites(GameObject g)
    {
        Renderer[] rs = g.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(g.transform.position, Vector3.zero);
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    static Material CrearMaterial(string nombre, Texture2D tex, Color color, string shader = "Universal Render Pipeline/Lit")
    {
        string carpeta = Carpeta + "/Materiales";
        if (!AssetDatabase.IsValidFolder(carpeta)) AssetDatabase.CreateFolder(Carpeta, "Materiales");
        string ruta = carpeta + "/" + nombre + ".mat";
        Material m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (m == null)
        {
            m = new Material(Shader.Find(shader));
            AssetDatabase.CreateAsset(m, ruta);
        }
        else m.shader = Shader.Find(shader);
        if (tex != null) m.SetTexture("_BaseMap", tex);
        m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.05f);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(m);
        return m;
    }

    // ---------------------------------------------------------------- Animator

    static AnimatorController CrearControlador(string nombre, string rutaModelo)
    {
        string ruta = Carpeta + "/" + nombre + ".controller";
        if (File.Exists(ruta)) AssetDatabase.DeleteAsset(ruta);
        AnimatorController c = AnimatorController.CreateAnimatorControllerAtPath(ruta);
        AnimatorStateMachine sm = c.layers[0].stateMachine;

        List<AnimationClip> clips = new List<AnimationClip>();
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(rutaModelo))
            if (o is AnimationClip ac && !ac.name.StartsWith("__preview__")) clips.Add(ac);

        AnimationClip Buscar(params string[] claves)
        {
            foreach (string k in claves)
                foreach (AnimationClip ac in clips)
                    if (ac.name.ToLower().Contains(k)) return ac;
            return null;
        }

        AnimationClip idle = Buscar("idle") ?? (clips.Count > 0 ? clips[0] : null);
        AnimationClip walk = Buscar("walk") ?? idle;
        AnimationClip run = Buscar("run") ?? walk;
        AnimationClip attack = Buscar("attack") ?? idle;
        AnimationClip death = Buscar("death", "die") ?? attack;

        AnimatorState sIdle = sm.AddState("Idle"); sIdle.motion = idle;
        sm.AddState("Walk").motion = walk;
        AnimatorState sRun = sm.AddState("Run"); sRun.motion = run; sRun.speed = run == walk ? 1.8f : 1f;
        sm.AddState("Attack").motion = attack;
        sm.AddState("Death").motion = death;
        sm.defaultState = sIdle;

        Debug.Log("[Monterrey] " + nombre + " clips: " + string.Join(", ", clips.ConvertAll(x => x.name)));
        return c;
    }

    // ---------------------------------------------------------------- Cara de La Sonriente

    static void PonerCara(GameObject raiz)
    {
        Transform cabeza = null;
        foreach (Transform t in raiz.GetComponentsInChildren<Transform>())
            if (t.name.ToLower().Contains("head")) { cabeza = t; break; }

        Bounds b = Limites(raiz.transform.Find("Modelo").gameObject);
        Vector3 pos = cabeza != null ? cabeza.position : new Vector3(b.center.x, b.max.y - 0.17f, b.center.z);

        GameObject cara = GameObject.CreatePrimitive(PrimitiveType.Quad);
        cara.name = "Cara_Sonriente";
        Object.DestroyImmediate(cara.GetComponent<Collider>());
        cara.transform.SetParent(cabeza != null ? cabeza : raiz.transform, true);
        cara.transform.position = pos + raiz.transform.forward * 0.13f + Vector3.up * 0.02f;
        cara.transform.rotation = Quaternion.LookRotation(-raiz.transform.forward, Vector3.up);
        float esc = 0.34f / Mathf.Max(0.0001f, cara.transform.lossyScale.x / cara.transform.localScale.x);
        cara.transform.localScale = Vector3.one * esc;

        Material m = CrearMaterial("CaraSonriente", AssetDatabase.LoadAssetAtPath<Texture2D>(RutaCara),
            new Color(0.72f, 0.72f, 0.7f), "Universal Render Pipeline/Unlit");
        m.SetFloat("_AlphaClip", 1f);
        m.SetFloat("_Cutoff", 0.5f);
        m.EnableKeyword("_ALPHATEST_ON");
        m.SetFloat("_Cull", 0f);
        EditorUtility.SetDirty(m);
        Renderer r = cara.GetComponent<Renderer>();
        r.sharedMaterial = m;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    // ---------------------------------------------------------------- Escondites

    static int CrearEscondites(Transform sistemas)
    {
        Transform viejo = sistemas.Find("Escondites");
        if (viejo != null) Object.DestroyImmediate(viejo.gameObject);
        GameObject grupo = new GameObject("Escondites");
        grupo.transform.SetParent(sistemas);

        PlayerMovement j = Object.FindFirstObjectByType<PlayerMovement>();
        Vector3 centro = j != null ? j.transform.position : Vector3.zero;

        Material mat = CrearMaterial("Arbusto", null, new Color(0.07f, 0.12f, 0.06f));
        Random.InitState(1998);

        List<Vector3> arboles = Mundo.ArbolesCerca(centro, 6f, 160f, 4000);
        List<Vector3> puestos = new List<Vector3>();
        int creados = 0;
        for (int intento = 0; intento < 400 && creados < 18 && arboles.Count > 0; intento++)
        {
            Vector3 a = arboles[Random.Range(0, arboles.Count)];
            bool lejos = true;
            foreach (Vector3 p in puestos) if ((p - a).sqrMagnitude < 18f * 18f) { lejos = false; break; }
            if (!lejos) continue;
            puestos.Add(a);

            Vector2 o = Random.insideUnitCircle.normalized * 1.6f;
            Vector3 pos = Mundo.EnSuelo(a + new Vector3(o.x, 0, o.y));

            GameObject arb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            arb.name = "Escondite_Arbusto_" + (creados + 1);
            arb.transform.SetParent(grupo.transform);
            arb.transform.position = pos + Vector3.up * 0.55f;
            arb.transform.localScale = new Vector3(Random.Range(2f, 2.6f), Random.Range(1.3f, 1.6f), Random.Range(2f, 2.6f));
            arb.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            arb.GetComponent<Collider>().isTrigger = true;
            Renderer r = arb.GetComponent<Renderer>();
            r.sharedMaterial = mat;

            GameObject dentro = new GameObject("PuntoDentro");
            dentro.transform.SetParent(arb.transform, false);
            dentro.transform.position = pos;

            Escondite e = arb.AddComponent<Escondite>();
            e.puntoDentro = dentro.transform;
            e.nombre = "arbusto";
            creados++;
        }
        return creados;
    }
}
