using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Vida del jugador. Viñeta roja al recibir daño, latido con vida baja y
/// "SEÑAL PERDIDA" al morir; luego reapareces en el último punto de control.
/// </summary>
public class SaludJugador : Entidad
{
    [Header("Regeneración")]
    public float regeneracionPorSegundo = 2f;
    public float esperaParaRegenerar = 6f;

    public static SaludJugador Instancia { get; private set; }

    Vector3 puntoControl;
    Quaternion rotacionControl;
    float ultimoDanio;
    RawImage vinetaRoja;
    Text textoMuerte;
    Image negro;
    AudioSource src, latido;
    AudioClip golpe;

    protected override void Awake()
    {
        base.Awake();
        Instancia = this;
    }

    void Start()
    {
        puntoControl = transform.position;
        rotacionControl = transform.rotation;

        Canvas c = HorrorUI.CrearCanvas("Salud_Canvas", 450);
        vinetaRoja = HorrorUI.CrearRaw(c.transform, "VinetaRoja", HorrorUI.Vineta, new Color(0.6f, 0f, 0f, 0f));
        HorrorUI.Estirar(vinetaRoja.rectTransform);
        negro = HorrorUI.CrearImagen(c.transform, "Negro", new Color(0, 0, 0, 0));
        HorrorUI.Estirar(negro.rectTransform);
        textoMuerte = HorrorUI.CrearTexto(c.transform, "Muerte", "", 46, HorrorUI.Hueso, TextAnchor.MiddleCenter);
        HorrorUI.Estirar(textoMuerte.rectTransform);

        src = gameObject.AddComponent<AudioSource>();
        golpe = ProceduralAudio.GolpeGrave();
        latido = gameObject.AddComponent<AudioSource>();
        latido.clip = ProceduralAudio.Latido();
        latido.loop = true; latido.volume = 0f; latido.Play();
    }

    void Update()
    {
        if (!EstaVivo) return;

        // Viñeta roja según la vida que falta
        float falta = 1f - Vida01;
        Color c = vinetaRoja.color;
        c.a = Mathf.MoveTowards(c.a, falta * 0.85f, Time.deltaTime * 1.5f);
        vinetaRoja.color = c;
        latido.volume = Mathf.Clamp01((falta - 0.4f) * 1.6f);
        latido.pitch = 1f + falta * 0.5f;

        if (Time.time - ultimoDanio > esperaParaRegenerar && Vida < vidaMaxima)
            Curar(regeneracionPorSegundo * Time.deltaTime);
    }

    /// <summary>La Historia llama esto al pasar por un punto importante.</summary>
    public void GuardarPuntoControl()
    {
        puntoControl = transform.position;
        rotacionControl = transform.rotation;
    }

    /// <summary>Quita la viñeta de daño un rato (Easter Egg de la Enfermera).</summary>
    public void CalmarPulso()
    {
        Curar(vidaMaxima);
        if (vinetaRoja != null) vinetaRoja.color = new Color(0.6f, 0, 0, 0);
    }

    protected override float ModificarDanio(float cantidad)
    {
        // Escondido no te pueden lastimar
        PlayerEscondite e = GetComponent<PlayerEscondite>();
        return e != null && e.Escondido ? 0f : cantidad;
    }

    protected override void AlRecibirDanio(float cantidad, Vector3 origen)
    {
        ultimoDanio = Time.time;
        src.PlayOneShot(golpe, 0.9f);
        VHSOverlay.Glitch(0.7f, 0.35f);
        HorrorInput.Vibrar(0.9f, 0.7f, 0.35f, this);
        Color c = vinetaRoja.color; c.a = 1f; vinetaRoja.color = c;
    }

    protected override void Morir()
    {
        StartCoroutine(SecuenciaMuerte());
    }

    IEnumerator SecuenciaMuerte()
    {
        HorrorGameState.modo = HorrorGameState.Modo.Final;
        VHSOverlay.Glitch(1f, 2.5f);
        for (float t = 0; t < 1.2f; t += Time.deltaTime)
        {
            negro.color = new Color(0, 0, 0, t / 1.2f);
            yield return null;
        }
        textoMuerte.text = "S E Ñ A L   P E R D I D A\n\n<size=24>rebobinando cinta...</size>";
        textoMuerte.supportRichText = true;
        yield return new WaitForSeconds(3f);

        // Reaparecer
        CharacterController cc = GetComponent<CharacterController>();
        if (cc) cc.enabled = false;
        transform.SetPositionAndRotation(puntoControl, rotacionControl);
        if (cc) cc.enabled = true;
        Revivir();
        foreach (MonoBehaviour m in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            if (m is IReiniciable r) r.Reiniciar();

        textoMuerte.text = "";
        for (float t = 0; t < 1f; t += Time.deltaTime)
        {
            negro.color = new Color(0, 0, 0, 1f - t);
            yield return null;
        }
        negro.color = new Color(0, 0, 0, 0);
        HorrorGameState.modo = HorrorGameState.Modo.Jugando;
    }
}
