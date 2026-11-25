using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Vela de tumba (panteón). Prenderla toma tres pasos y hay que MANTENER el botón de acción:
///   1) raspar el cerillo,  2) cubrir la flama del viento sin moverte,  3) prender la mecha.
/// Si sueltas el botón o te mueves, el viento apaga el cerillo y empiezas otra vez.
/// Ya prendida se ve la flama (tiembla) y alumbra alrededor.
/// </summary>
[RequireComponent(typeof(Collider))]
public class VelaPanteon : MonoBehaviour, IInteractable
{
    public Light luz;
    public bool Prendida { get; private set; }
    public bool Encendiendo { get; private set; }

    static readonly string[] Pasos = { "Raspando el cerillo...", "Cubre la flama del viento... no te muevas", "Prendiendo la mecha..." };
    static readonly float[] Tiempos = { 1.0f, 1.4f, 0.9f };

    Transform[] flamas;
    float baseIntensidad = 2.6f;
    float semilla;

    // barra de progreso compartida
    static Canvas canvas;
    static Text texto;
    static Image relleno;

    void Start()
    {
        semilla = Random.Range(0f, 100f);
        if (luz != null)
        {
            luz.enabled = false;
            luz.range = Mathf.Max(luz.range, 7f);
            luz.color = new Color(1f, 0.62f, 0.28f);
            luz.shadows = LightShadows.None;
        }
    }

    public void Interact(PlayerInteraction jugador)
    {
        if (Prendida || Encendiendo) return;
        StartCoroutine(Encender(jugador));
    }

    IEnumerator Encender(PlayerInteraction jugador)
    {
        Encendiendo = true;
        Barra(true);
        Transform j = jugador.transform;
        for (int paso = 0; paso < Pasos.Length; paso++)
        {
            Vector3 donde = j.position;
            float t = 0f;
            while (t < Tiempos[paso])
            {
                bool movio = (j.position - donde).sqrMagnitude > 0.04f;
                if (!HorrorInput.InteractuarMantenido || !HorrorGameState.PuedeJugar || (paso == 1 && movio))
                {
                    Barra(false);
                    Encendiendo = false;
                    AudioSource.PlayClipAtPoint(ProceduralAudio.Susurro(Random.Range(0, 200)), transform.position, 0.35f);
                    DialogoUI.Susurrar("", paso == 0 ? "Se me cayó el cerillo... otra vez." : "El viento apagó el cerillo. Hay que volver a empezar.", 2.5f);
                    yield break;
                }
                t += Time.deltaTime;
                MostrarProgreso(Pasos[paso], (paso + t / Tiempos[paso]) / Pasos.Length);
                // el cerillo tiembla en la luz
                if (luz != null && paso > 0) { luz.enabled = true; luz.intensity = Random.Range(0.3f, 0.9f); }
                yield return null;
            }
            if (paso == 0) HorrorInput.Vibrar(0.15f, 0.3f, 0.12f, this);
        }
        Barra(false);
        Encendiendo = false;
        Prender();
    }

    void Prender()
    {
        Prendida = true;
        if (luz != null) { luz.enabled = true; baseIntensidad = Mathf.Max(2.6f, luz.intensity * 1.6f); }
        CrearFlamas();
        AudioSource.PlayClipAtPoint(ProceduralAudio.Susurro(Random.Range(0, 200)), transform.position, 0.6f);
        VHSOverlay.Glitch(0.25f, 0.2f);
        string[] frases = { "La flama se puso azul un segundo.", "Alguien suspiró detrás de la lápida.", "La vela está tibia... como si alguien la acabara de apagar." };
        DialogoUI.Susurrar("", frases[Random.Range(0, frases.Length)], 3f);
    }

    void CrearFlamas()
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        Material mat = new Material(sh);
        Color c = new Color(1f, 0.72f, 0.25f);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        mat.color = c;
        Vector3 centro = luz != null ? luz.transform.position - Vector3.up * 0.1f : transform.position + Vector3.up * 0.4f;
        flamas = new Transform[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject f = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            f.name = "Flama_" + i;
            Destroy(f.GetComponent<Collider>());
            f.transform.SetParent(transform, true);
            f.transform.position = centro + new Vector3((i - 1) * 0.07f, (i == 1 ? 0.04f : 0f), (i % 2) * 0.05f);
            f.transform.localScale = new Vector3(0.035f, 0.08f, 0.035f);
            Renderer r = f.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            flamas[i] = f.transform;
        }
    }

    void Update()
    {
        if (!Prendida) return;
        float t = Time.time * 9f + semilla;
        if (luz != null) luz.intensity = baseIntensidad * (0.8f + 0.2f * Mathf.PerlinNoise(t, 0.3f));
        if (flamas != null)
            for (int i = 0; i < flamas.Length; i++)
                if (flamas[i] != null)
                    flamas[i].localScale = new Vector3(0.035f, 0.07f + 0.03f * Mathf.PerlinNoise(t + i * 3f, 1.7f), 0.035f);
    }

    // ------------------------------------------------------------------ barra en pantalla

    static void Barra(bool visible)
    {
        if (canvas == null)
        {
            if (!visible) return;
            canvas = HorrorUI.CrearCanvas("Vela_Canvas", 140, null);
            texto = HorrorUI.CrearTexto(canvas.transform, "Paso", "", 26, HorrorUI.Hueso, TextAnchor.MiddleCenter);
            HorrorUI.Colocar(texto.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -150), new Vector2(900, 40));
            Image fondo = HorrorUI.CrearImagen(canvas.transform, "Fondo", new Color(0f, 0f, 0f, 0.6f));
            HorrorUI.Colocar(fondo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -190), new Vector2(360, 14));
            relleno = HorrorUI.CrearImagen(fondo.transform, "Relleno", new Color(1f, 0.65f, 0.25f, 0.9f));
            relleno.rectTransform.anchorMin = new Vector2(0, 0); relleno.rectTransform.anchorMax = new Vector2(0, 1);
            relleno.rectTransform.pivot = new Vector2(0, 0.5f);
            relleno.rectTransform.anchoredPosition = Vector2.zero; relleno.rectTransform.sizeDelta = new Vector2(0, 0);
        }
        canvas.gameObject.SetActive(visible);
    }

    static void MostrarProgreso(string paso, float p01)
    {
        if (texto == null) return;
        texto.text = HorrorInput.Traducir(paso + "   (mantén [A])");
        relleno.rectTransform.sizeDelta = new Vector2(360f * Mathf.Clamp01(p01), 0);
    }
}
