using System.Collections;
using UnityEngine;

/// <summary>
/// Una de las 5 llaves. Flota girando con un brillo frío. Al tomarla [RB] vuela hacia la cámara.
/// Puede estar "escondida": solo se ve cuando la alumbras con la linterna de cerca.
/// </summary>
[RequireComponent(typeof(Collider))]
public class LlaveBucle : MonoBehaviour, IInteractable
{
    [Tooltip("Solo se ve cuando la linterna le pega de cerca")]
    public bool soloConLinterna = false;
    public string frase = "";

    public bool Tomada { get; private set; }
    public bool Disponible { get; set; } = true;

    Vector3 origen;
    Renderer[] renderers;
    Light brillo;
    FlashlightController linterna;

    void Start()
    {
        origen = transform.position;
        renderers = GetComponentsInChildren<Renderer>();
        brillo = GetComponentInChildren<Light>();
        Mostrar(!soloConLinterna && Disponible);
    }

    void Mostrar(bool v)
    {
        foreach (Renderer r in renderers) r.enabled = v;
        if (brillo != null) brillo.enabled = v;
    }

    void Update()
    {
        if (Tomada) return;
        transform.position = origen + Vector3.up * (Mathf.Sin(Time.time * 2f) * 0.07f);
        transform.Rotate(0f, 70f * Time.deltaTime, 0f, Space.World);

        if (!Disponible) { Mostrar(false); return; }
        if (!soloConLinterna) { Mostrar(true); if (brillo != null) brillo.intensity = 1.6f + Mathf.Sin(Time.time * 4f) * 0.6f; return; }

        if (linterna == null) linterna = FindAnyObjectByType<FlashlightController>();
        Camera cam = Camera.main;
        bool alumbrada = false;
        if (linterna != null && linterna.Encendida && cam != null)
        {
            Vector3 a = transform.position - cam.transform.position;
            alumbrada = a.magnitude < 9f && Vector3.Angle(cam.transform.forward, a) < 20f;
        }
        Mostrar(alumbrada);
    }

    public void Interact(PlayerInteraction jugador)
    {
        if (Tomada || !Disponible) return;
        if (soloConLinterna && (renderers.Length == 0 || !renderers[0].enabled))
        {
            DialogoUI.Susurrar("", "Hay algo aquí... pero no lo veo. Necesito luz.", 2.5f);
            return;
        }
        StartCoroutine(Tomar());
    }

    IEnumerator Tomar()
    {
        Tomada = true;
        if (AcechoDirector.Instancia != null) AcechoDirector.Instancia.SustoPorLlave();
        SensacionJuego.Sacudir(0.35f);
        Mostrar(true);
        GetComponent<Collider>().enabled = false;
        AudioSource.PlayClipAtPoint(Campanita(), transform.position, 1f);
        VHSOverlay.Glitch(0.5f, 0.4f);
        HorrorInput.Vibrar(0.3f, 0.6f, 0.3f, this);

        Transform cam = Camera.main != null ? Camera.main.transform : transform;
        Vector3 desde = transform.position;
        Vector3 escala = transform.localScale;
        for (float t = 0; t < 0.6f; t += Time.deltaTime)
        {
            float k = t / 0.6f;
            transform.position = Vector3.Lerp(desde, cam.position + cam.forward * 0.5f - cam.up * 0.15f, k * k);
            transform.localScale = escala * (1f - k * 0.8f);
            transform.Rotate(0f, 600f * Time.deltaTime, 0f, Space.World);
            yield return null;
        }

        if (LlavesBucle.Instancia != null) LlavesBucle.Instancia.Sumar();
        int n = LlavesBucle.Instancia != null ? LlavesBucle.Instancia.Llaves : 0;
        InventarioUtil.ActualizarLlaves(n, LlavesBucle.Total, frase);   // una sola casilla: "Llaves del bucle n/5"
        DialogoUI.Susurrar("", (string.IsNullOrEmpty(frase) ? "Llave " + n + " de " + LlavesBucle.Total + "." : frase) + "   (guardada en el maletín)", 3.5f);
        gameObject.SetActive(false);
    }

    static AudioClip campanita;
    static AudioClip Campanita()
    {
        if (campanita != null) return campanita;
        int hz = 44100, n = (int)(hz * 1.6f);
        float[] d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / hz, env = Mathf.Exp(-t * 3f);
            d[i] = env * 0.35f * (Mathf.Sin(2 * Mathf.PI * 1318f * t) + 0.5f * Mathf.Sin(2 * Mathf.PI * 1975f * t) + 0.25f * Mathf.Sin(2 * Mathf.PI * 2637f * t * 1.003f));
        }
        campanita = AudioClip.Create("Campanita", n, 1, hz, false);
        campanita.SetData(d, 0);
        return campanita;
    }
}
