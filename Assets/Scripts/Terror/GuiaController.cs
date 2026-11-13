using System.Collections;
using UnityEngine;

/// <summary>
/// EL VIGÍA — el guía del Cerro de la Silla.
/// Personalidad: amable en la superficie, pasivo-agresivo, sabe demasiado de ti.
/// Aparece y desaparece con interferencia, te mira todo el tiempo, se sienta junto a
/// los árboles, camina hacia ti, saluda, se ríe, señala y te intimida si te acercas de más.
/// Las animaciones se llaman por el NOMBRE del estado en su Animator:
/// Idle, Sentado, Caminar, Saludar, Intimidar, Hablar, Senalar, Reir.
/// </summary>
public class GuiaController : MonoBehaviour
{
    public enum Pose { Idle, Sentado, Caminar, Saludar, Intimidar, Hablar, Senalar, Reir }

    public string nombre = "EL VIGÍA";
    public Animator animator;

    [Header("Movimiento")]
    public float velocidadCaminar = 1.3f;
    public float velocidadGiro = 4f;

    [Header("Intimidación")]
    public float distanciaIntimidar = 2.3f;
    public float segundosParaIntimidar = 1.2f;
    public float enfriamientoIntimidar = 18f;

    [Tooltip("Frases cuando te le acercas demasiado")]
    [TextArea]
    public string[] frasesIntimidar =
    {
        "Espacio personal. Lo inventaron por algo.",
        "Si te acercas más, voy a ver lo que piensas. Y no me gusta lo que piensas.",
        "Qué valiente. Los valientes duran poco aquí. Sigue así.",
        "Uno, dos, tres... sigue contando. Yo ya terminé.",
        "Tienes algo en la cara. Ah, no. Es miedo.",
    };

    public bool Visible { get; private set; }
    [HideInInspector] public bool congelarGiro;   // la historia lo usa para que señale sin voltear hacia ti
    public Pose PoseActual { get; private set; }

    Transform jugador;
    Renderer[] renderers;
    Vector3? destino;
    float tiempoCerca, ultimoIntimidar = -999f;
    Coroutine volverIdle;
    AudioSource src;
    AudioClip estatica, golpe, risa;

    void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null) animator.applyRootMotion = false;
        renderers = GetComponentsInChildren<Renderer>(true);

        src = gameObject.AddComponent<AudioSource>();
        src.spatialBlend = 1f;
        src.minDistance = 3f;
        src.maxDistance = 45f;
        src.rolloffMode = AudioRolloffMode.Linear;
        estatica = ProceduralAudio.Estatica(77);
        golpe = ProceduralAudio.GolpeGrave();
        risa = ProceduralAudio.Susurro(99);
    }

    void Start()
    {
        // Arranca escondido: MisionManager decide cuándo aparece
        if (FindFirstObjectByType<MisionManager>() != null) SetVisible(false);
    }

    // ------------------------------------------------------------------ Aparecer / desaparecer

    public void Aparecer(Vector3 posicion, Vector3 mirarHacia, Pose pose)
    {
        destino = null;
        posicion.y = AlturaSuelo(posicion);
        transform.position = posicion;
        Vector3 d = mirarHacia - posicion; d.y = 0;
        if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(d);
        SetVisible(true);
        Animar(pose, 0f);
        src.PlayOneShot(estatica, 0.6f);
        VHSOverlay.Glitch(0.55f, 0.3f);
    }

    public void Desaparecer()
    {
        if (!Visible) return;
        src.PlayOneShot(estatica, 0.5f);
        VHSOverlay.Glitch(0.45f, 0.25f);
        destino = null;
        SetVisible(false);
    }

    void SetVisible(bool v)
    {
        Visible = v;
        foreach (Renderer r in renderers) if (r != null) r.enabled = v;
        if (animator != null) animator.enabled = v;
    }

    // ------------------------------------------------------------------ Animación

    public void Animar(Pose pose, float fundido = 0.25f)
    {
        PoseActual = pose;
        if (volverIdle != null) { StopCoroutine(volverIdle); volverIdle = null; }
        if (animator == null || !animator.enabled) return;

        int h = Animator.StringToHash(pose.ToString());
        if (animator.HasState(0, h)) animator.CrossFadeInFixedTime(h, fundido);
        else if (pose != Pose.Idle && animator.HasState(0, Animator.StringToHash("Idle")))
            animator.CrossFadeInFixedTime("Idle", fundido);

        // Las animaciones de una sola vez regresan a Idle solas
        if (pose == Pose.Saludar || pose == Pose.Intimidar || pose == Pose.Senalar || pose == Pose.Reir)
            volverIdle = StartCoroutine(VolverAIdle(2.6f));
    }

    public void AnimarPorNombre(string nombrePose)
    {
        if (System.Enum.TryParse(nombrePose, true, out Pose p)) Animar(p);
        else Animar(Pose.Hablar);
        if (p == Pose.Reir) src.PlayOneShot(risa, 0.5f);
    }

    IEnumerator VolverAIdle(float s)
    {
        yield return new WaitForSeconds(s);
        volverIdle = null;
        if (PoseActual != Pose.Sentado && PoseActual != Pose.Caminar) Animar(DialogoUI.Activo ? Pose.Hablar : Pose.Idle);
    }

    public void CaminarHacia(Vector3 punto)
    {
        destino = punto;
        Animar(Pose.Caminar);
    }

    public bool Caminando => destino.HasValue;

    public void Detener()
    {
        destino = null;
        if (PoseActual == Pose.Caminar) Animar(Pose.Idle);
    }

    // ------------------------------------------------------------------ Update

    void Update()
    {
        if (!Visible) return;
        if (jugador == null)
        {
            var pm = FindFirstObjectByType<PlayerMovement>();
            if (pm != null) jugador = pm.transform;
            if (jugador == null) return;
        }

        if (destino.HasValue)
        {
            Vector3 p = transform.position;
            Vector3 d = destino.Value - p; d.y = 0;
            if (d.magnitude < 0.3f)
            {
                destino = null;
                Animar(Pose.Idle);
            }
            else
            {
                Vector3 paso = d.normalized * velocidadCaminar * Time.deltaTime;
                p += paso;
                p.y = AlturaSuelo(p);
                transform.position = p;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), velocidadGiro * Time.deltaTime);
            }
        }
        else if (PoseActual != Pose.Sentado && !congelarGiro)
        {
            // Siempre te está mirando
            Vector3 d = jugador.position - transform.position; d.y = 0;
            if (d.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), velocidadGiro * Time.deltaTime);
        }

        // Te acercas demasiado → intimida
        float dist = Vector3.Distance(jugador.position, transform.position);
        if (HorrorGameState.PuedeJugar && !DialogoUI.Activo && dist < distanciaIntimidar)
        {
            tiempoCerca += Time.deltaTime;
            if (tiempoCerca > segundosParaIntimidar && Time.time - ultimoIntimidar > enfriamientoIntimidar) Intimidar();
        }
        else tiempoCerca = 0f;
    }

    public void Intimidar()
    {
        ultimoIntimidar = Time.time;
        tiempoCerca = 0f;
        destino = null;
        Animar(Pose.Intimidar, 0.1f);
        src.PlayOneShot(golpe, 1f);
        VHSOverlay.Glitch(0.9f, 0.6f);
        HorrorInput.Vibrar(0.8f, 0.6f, 0.4f, this);
        if (frasesIntimidar != null && frasesIntimidar.Length > 0)
            DialogoUI.Susurrar(nombre, frasesIntimidar[Random.Range(0, frasesIntimidar.Length)], 4f);
    }

    /// <summary>¿El jugador lo está viendo con la cámara?</summary>
    public bool JugadorMeVe()
    {
        Camera c = Camera.main;
        if (c == null || !Visible) return false;
        Vector3 v = c.WorldToViewportPoint(transform.position + Vector3.up * 1.2f);
        return v.z > 0 && v.x > 0.15f && v.x < 0.85f && v.y > 0.1f && v.y < 0.9f;
    }

    public static float AlturaSuelo(Vector3 p)
    {
        Terrain t = Terrain.activeTerrain;
        if (t == null) return p.y;
        return t.SampleHeight(p) + t.transform.position.y;
    }
}
