#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using UnityEngine;

/// <summary>
/// SOLO EN EL EDITOR. Graba tomas VERTICALES (720x1280, 30 cuadros por segundo exactos) para los tráilers de TikTok.
/// La cámara del juego se dirige sola por cada toma (bosque, Vigía, los 5 lugares, túnel, monstruos, portal)
/// y cada cuadro se guarda como JPG en  D:\Proyectos\Trailer_tomas\<toma>\00000.jpg
/// Se activa con el menú Monterrey/23 (crea Temp/tomas.flag y le da Play).
/// </summary>
public class TomasTrailer : MonoBehaviour
{
    const int ANCHO = 720, ALTO = 1280, FPS = 30;
    public static string Carpeta => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Trailer_tomas"));
    static string Flag => Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/tomas.flag"));

    Camera cam;
    RenderTexture rt;
    Texture2D tex;
    Light foco;
    PlayerMovement pm;
    MisionManager mm;
    Vector3 inicio, frenteInicio;
    string log = "";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Iniciar()
    {
        if (!File.Exists(Flag)) return;
        if (FindAnyObjectByType<TomasTrailer>() != null) return;
        File.Delete(Flag);
        MisionManager m = FindAnyObjectByType<MisionManager>();
        if (m != null) m.saltarPrologo = true;
        GameObject g = new GameObject("TomasTrailer");
        DontDestroyOnLoad(g);
        g.AddComponent<TomasTrailer>();
    }

    IEnumerator Start()
    {
        Directory.CreateDirectory(Carpeta);
        yield return new WaitForSecondsRealtime(4f);
        pm = FindAnyObjectByType<PlayerMovement>();
        mm = FindAnyObjectByType<MisionManager>();
        GuiaController guia = FindAnyObjectByType<GuiaController>();
        if (mm != null) { mm.StopAllCoroutines(); mm.enabled = false; }
        if (guia != null) guia.Desaparecer();
        HorrorGameState.modo = HorrorGameState.Modo.Jugando;
        if (AcechoDirector.Instancia != null) AcechoDirector.Instancia.Habilitar(false);
        PsicoDirector psico = FindAnyObjectByType<PsicoDirector>(); if (psico != null) psico.habilitado = false;
        foreach (EnemigoBase e in FindObjectsByType<EnemigoBase>(FindObjectsInactive.Include, FindObjectsSortMode.None)) e.Reiniciar();

        inicio = pm.transform.position;
        frenteInicio = pm.transform.forward; frenteInicio.y = 0; frenteInicio.Normalize();

        // Cámara del juego → textura vertical; sin control del jugador; cuerpo del jugador oculto
        cam = Camera.main;
        CameraController cc = cam.GetComponent<CameraController>(); if (cc != null) cc.enabled = false;
        pm.enabled = false;
        CharacterController chc = pm.GetComponent<CharacterController>(); if (chc != null) chc.enabled = false;
        foreach (Renderer r in pm.GetComponentsInChildren<Renderer>()) r.enabled = false;
        FlashlightController fl = pm.GetComponent<FlashlightController>();
        if (fl != null) foreach (Light l in fl.GetComponentsInChildren<Light>()) l.enabled = false;
        rt = new RenderTexture(ANCHO, ALTO, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.fieldOfView = 68f;
        tex = new Texture2D(ANCHO, ALTO, TextureFormat.RGB24, false);
        // "linterna" de la cámara (como la del juego)
        foco = new GameObject("FocoTrailer").AddComponent<Light>();
        foco.type = LightType.Spot; foco.spotAngle = 52f; foco.innerSpotAngle = 22f; foco.range = 22f;
        foco.intensity = 2.6f; foco.color = new Color(1f, 0.93f, 0.8f); foco.shadows = LightShadows.Soft;
        Time.captureFramerate = FPS;

        // ------------------------------------------------------------ TOMAS
        // 1. Bosque: caminando en la oscuridad
        Vector3 a = Suelo(inicio, 1.6f), b = Suelo(inicio + frenteInicio * 7f, 1.6f);
        yield return Toma("01_bosque", 150, t => Mano(Vector3.Lerp(a, b, t), t), t => Vector3.Lerp(a, b, t) + frenteInicio * 10f + Vector3.up * 0.1f);

        // 2. El Vigía señalando a lo lejos
        if (guia != null)
        {
            Vector3 pv = Mundo.EnSuelo(inicio + frenteInicio * 11f);
            guia.Aparecer(pv, inicio, GuiaController.Pose.Senalar);
            Vector3 c0 = Suelo(inicio, 1.55f), c1 = Suelo(inicio + frenteInicio * 4.5f, 1.55f);
            yield return Toma("02_vigia", 120, t => Mano(Vector3.Lerp(c0, c1, Suave(t)), t), t => pv + Vector3.up * 1.6f);
            guia.Animar(GuiaController.Pose.Reir, 0.05f);
            yield return Toma("03_vigia_rie", 75, t => Mano(Vector3.Lerp(c1, c1 + frenteInicio * 2.5f, t), t), t => pv + Vector3.up * 1.7f);
            guia.Desaparecer();
        }

        // 3. Los cinco lugares (acercamiento lento con giro)
        if (mm != null && mm.sitios != null)
            for (int i = 0; i < mm.sitios.Length; i++)
            {
                SitioLlave s = mm.sitios[i];
                if (s == null) continue;
                Vector3 centro = s.transform.position;
                Vector3 hacia = inicio - centro; hacia.y = 0; hacia.Normalize();
                Vector3 lado = Vector3.Cross(Vector3.up, hacia);
                float d = s is SitioIglesia ? 15f : s is SitioMirador ? 9f : 11f;
                Vector3 p0 = centro + hacia * d + lado * 3f, p1 = centro + hacia * d * 0.55f - lado * 1.5f;
                yield return Toma("1" + i + "_lugar", 105, t => Suelo(Vector3.Lerp(p0, p1, Suave(t)), 1.7f), t => centro + Vector3.up * 1.6f);
            }

        // 4. Túnel de la Línea K y La Sonriente al fondo
        LaSonriente son = FindAnyObjectByType<LaSonriente>(FindObjectsInactive.Include);
        ElAsomado aso = FindAnyObjectByType<ElAsomado>(FindObjectsInactive.Include);
        if (mm != null && mm.tunel != null && mm.tunel.interior != null)
        {
            Transform I = mm.tunel.interior;
            Vector3 t0 = I.TransformPoint(new Vector3(-3f, 1.6f, -16f)), t1 = I.TransformPoint(new Vector3(-3f, 1.6f, -7f));
            Vector3 fondo = I.TransformPoint(new Vector3(-3f, 0f, 4f));
            if (son != null) Poner(son, fondo, t1, "Idle");
            yield return Toma("20_tunel", 165, t => Mano(Vector3.Lerp(t0, t1, t), t), t => fondo + Vector3.up * 1.4f, Parpadeo(0.55f, 0.75f));
            if (son != null)
            {
                Vector3 cerca = I.TransformPoint(new Vector3(-3f, 0f, -2.5f));
                Poner(son, cerca, t1, "Idle");
                yield return Toma("21_tunel_sonriente", 60, t => Mano(t1, t * 0.3f), t => cerca + Vector3.up * 1.5f, Parpadeo(0f, 0.25f));
            }
        }

        // 5. Linterna falla... y al regresar La Sonriente está enfrente
        if (son != null)
        {
            Vector3 fr = Quaternion.Euler(0, 140f, 0) * frenteInicio;
            Vector3 cp = Suelo(inicio + fr * 3f, 1.6f);
            Vector3 vacio = cp + fr * 12f;
            Vector3 ps = Mundo.EnSuelo(cp + fr * 2.4f);
            Ocultar(son);
            yield return Toma("30_linterna", 150, t => Mano(cp, t), t => vacio, (f, t) =>
            {
                foco.enabled = !(t > 0.4f && t < 0.62f && (f % 4 < 2)) && !(t >= 0.62f && t < 0.7f);
                if (f == (int)(150 * 0.66f)) Poner(son, ps, cp, "Idle");
                if (t > 0.72f) cam.transform.LookAt(Vector3.Lerp(vacio, ps + Vector3.up * 1.55f, Mathf.Clamp01((t - 0.72f) * 8f)));
            });
            foco.enabled = true;
            // primer plano de su cara y ataque
            yield return Toma("31_sonriente_cara", 75, t => Vector3.Lerp(cp, ps + Vector3.up * 1.55f + (cp - ps).normalized * 1.1f, Suave(t)) + Vector3.up * (0.05f * Mathf.Sin(t * 20f)), t => ps + Vector3.up * 1.55f,
                (f, t) => { if (f == 45) Animar(son, "Attack"); });
            Ocultar(son);
        }

        // 6. El Asomado detrás de un árbol... y corre
        if (aso != null)
        {
            Vector3 fr = Quaternion.Euler(0, -70f, 0) * frenteInicio;
            Vector3 cp = Suelo(inicio + fr * 2f, 1.6f);
            Vector3 arbol = cp + fr * 13f;
            var cerca = Mundo.ArbolesCerca(cp + fr * 12f, 0f, 6f, 10);
            if (cerca.Count > 0) arbol = cerca[0];
            Vector3 dir = (cp - arbol); dir.y = 0; dir.Normalize();
            Vector3 lado = Vector3.Cross(Vector3.up, dir);
            Vector3 asomo = Mundo.EnSuelo(arbol + lado * 0.7f);
            Poner(aso, asomo, cp, "Idle");
            yield return Toma("40_asomado", 90, t => Mano(cp, t), t => Vector3.Lerp(arbol + Vector3.up * 1.3f, asomo + Vector3.up * 1.6f, Suave(t)));
            Animar(aso, "Run");
            Vector3 desde = asomo;
            yield return Toma("41_asomado_corre", 55, t => Mano(cp, 1f + t), t => aso.transform.position + Vector3.up * 1.6f, (f, t) =>
            {
                aso.transform.position = Mundo.EnSuelo(Vector3.Lerp(desde, cp - dir * 0.9f, Mathf.Clamp01(t * 1.15f)));
                aso.transform.rotation = Quaternion.LookRotation(dir * -1f);
            });
            Ocultar(aso);
        }

        // 7. Monstruo de cuerpo completo bajo la linterna (para el cierre)
        if (aso != null)
        {
            Vector3 cp = Suelo(inicio - frenteInicio * 3f, 1.4f);
            Vector3 pa = Mundo.EnSuelo(cp - frenteInicio * 4.5f);
            Poner(aso, pa, cp, "Walk");
            yield return Toma("42_asomado_camina", 90, t => Vector3.Lerp(cp, cp - frenteInicio * 0.8f, t), t => pa + Vector3.up * 1.4f, (f, t) =>
            {
                aso.transform.position = Mundo.EnSuelo(Vector3.Lerp(pa, pa + frenteInicio * 1.6f, t));
            });
            Ocultar(aso);
        }

        // 8. El portal del bucle se abre
        if (mm != null && mm.puertaBucle != null)
        {
            Transform pb = mm.puertaBucle.transform;
            Vector3 p0 = Suelo(pb.position - pb.forward * 9f, 1.6f), p1 = Suelo(pb.position - pb.forward * 4f, 1.6f);
            mm.puertaBucle.Abrir();
            foco.intensity = 0.8f;
            yield return Toma("50_portal", 180, t => Mano(Vector3.Lerp(p0, p1, Suave(t)), t), t => pb.position + Vector3.up * 1.5f);
        }

        Time.captureFramerate = 0;
        cam.targetTexture = null;
        File.WriteAllText(Path.Combine(Carpeta, "listo.txt"), "Tomas grabadas:\n" + log);
        Debug.Log("[TomasTrailer] Listo → " + Carpeta);
        UnityEditor.EditorApplication.isPlaying = false;
    }

    // ------------------------------------------------------------------ Grabación

    IEnumerator Toma(string nombre, int cuadros, Func<float, Vector3> pos, Func<float, Vector3> mirar, Action<int, float> extra = null)
    {
        string soloF = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/tomas_solo.txt"));
        if (File.Exists(soloF) && !File.ReadAllText(soloF).Contains(nombre)) { log += nombre + ": (se dejó la anterior)\n"; yield break; }
        string dir = Path.Combine(Carpeta, nombre);
        Directory.CreateDirectory(dir);
        foreach (string f in Directory.GetFiles(dir, "*.jpg")) File.Delete(f);
        // dos cuadros de calentamiento para que cargue todo en el lugar nuevo
        for (int w = 0; w < 3; w++) { Colocar(pos(0f), mirar(0f)); yield return null; }
        for (int f = 0; f < cuadros; f++)
        {
            float t = cuadros > 1 ? f / (float)(cuadros - 1) : 0f;
            Colocar(pos(t), mirar(t));
            extra?.Invoke(f, t);
            yield return new WaitForEndOfFrame();
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, ANCHO, ALTO), 0, 0, false);
            tex.Apply(false);
            RenderTexture.active = prev;
            File.WriteAllBytes(Path.Combine(dir, f.ToString("00000") + ".jpg"), tex.EncodeToJPG(93));
        }
        log += nombre + ": " + cuadros + " cuadros\n";
    }

    void Colocar(Vector3 p, Vector3 objetivo)
    {
        cam.transform.position = p;
        Vector3 d = objetivo - p;
        if (d.sqrMagnitude > 0.0001f) cam.transform.rotation = Quaternion.LookRotation(d);
        foco.transform.SetPositionAndRotation(p + cam.transform.right * 0.25f - cam.transform.up * 0.2f, cam.transform.rotation);
        // el jugador va con la cámara para que el mundo (sonidos, niebla, objetos cercanos) esté donde debe
        pm.transform.position = p - Vector3.up * 1.55f;   // a los pies de la cámara (así funcionan las zonas como el túnel)
    }

    // ------------------------------------------------------------------ Ayudas

    static Vector3 Suelo(Vector3 p, float altura) => Mundo.EnSuelo(p) + Vector3.up * altura;
    static float Suave(float t) => t * t * (3f - 2f * t);

    // movimiento de cámara en mano (como la videocámara del juego)
    static Vector3 Mano(Vector3 p, float t)
    {
        float s = t * 9f;
        return p + new Vector3(Mathf.Sin(s * 1.3f) * 0.025f, Mathf.Sin(s * 2.6f) * 0.03f + Mathf.PerlinNoise(s, 0.3f) * 0.02f, 0f);
    }

    Action<int, float> Parpadeo(float desde, float hasta) => (f, t) =>
    {
        foco.enabled = !(t > desde && t < hasta && UnityEngine.Random.value < 0.35f);
    };

    static readonly MethodInfo mostrar = typeof(EnemigoBase).GetMethod("Mostrar", BindingFlags.Instance | BindingFlags.NonPublic);

    void Poner(EnemigoBase e, Vector3 donde, Vector3 mirarA, string anim)
    {
        e.gameObject.SetActive(true);
        e.enabled = false;
        e.transform.position = Mundo.EnSuelo(donde);
        Vector3 d = mirarA - e.transform.position; d.y = 0;
        if (d.sqrMagnitude > 0.001f) e.transform.rotation = Quaternion.LookRotation(d);
        mostrar?.Invoke(e, new object[] { true });
        Animar(e, anim);
    }

    static void Animar(EnemigoBase e, string estado)
    {
        Animator a = e.GetComponentInChildren<Animator>();
        if (a != null && a.HasState(0, Animator.StringToHash(estado))) a.Play(estado, 0, 0f);
    }

    static void Ocultar(EnemigoBase e)
    {
        mostrar?.Invoke(e, new object[] { false });
        e.enabled = true;
        e.Reiniciar();
    }
}
#endif
