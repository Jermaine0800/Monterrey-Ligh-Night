using System.Collections;
using UnityEngine;

/// <summary>
/// La fogata del campamento: empieza apagada (solo brasas). Para prenderla hay que mantener el botón de acción:
/// juntar ramas → raspar el cerillo → soplar la brasa sin moverte. Al prenderse, el fuego deja ver
/// algo que brilla entre las cenizas: la llave 1. Y algo en el bosque se da cuenta.
/// </summary>
[RequireComponent(typeof(SphereCollider))]
public class FogataCampamento : MonoBehaviour, IInteractable
{
    public Light luz;
    public SitioCampamento sitio;
    public bool Prendida { get; private set; }
    public bool Encendiendo { get; private set; }

    static readonly string[] Pasos = { "Juntando ramas secas...", "Raspando el cerillo...", "Soplando la brasa... no te muevas" };
    static readonly float[] Tiempos = { 1.2f, 0.9f, 1.6f };

    Transform[] flamas;
    float baseIntensidad = 3.2f, semilla;

    void Start()
    {
        semilla = Random.Range(0f, 50f);
        SphereCollider c = GetComponent<SphereCollider>();
        c.isTrigger = true; c.radius = 1.4f; c.center = Vector3.zero;
        if (luz != null) { luz.intensity = 0.15f; luz.range = Mathf.Max(luz.range, 12f); }
        // La llave 1 no aparece hasta que prendes la fogata
        if (sitio != null && sitio.llave != null && !sitio.llave.Tomada) sitio.llave.Disponible = false;
    }

    public void Interact(PlayerInteraction jugador)
    {
        if (Prendida || Encendiendo || AccionMantenida.Ocupado) return;
        Encendiendo = true;
        StartCoroutine(AccionMantenida.Ejecutar(jugador.transform, Pasos, Tiempos, 2, ok =>
        {
            Encendiendo = false;
            if (ok) StartCoroutine(Prender());
            else DialogoUI.Susurrar("", "Se apagó. El viento sopla raro aquí... otra vez.", 2.5f);
        }));
    }

    IEnumerator Prender()
    {
        Prendida = true;
        CrearFlamas();
        VHSOverlay.Glitch(0.4f, 0.3f);
        for (float t = 0; t < 1.2f; t += Time.deltaTime) { if (luz != null) luz.intensity = Mathf.Lerp(0.15f, baseIntensidad, t / 1.2f); yield return null; }
        DialogoUI.Susurrar("", "El fuego prendió... ¿qué es eso que brilla entre las cenizas?", 3.5f);

        // La llave aparece junto al fuego, con un destello
        if (sitio != null && sitio.llave != null && !sitio.llave.Tomada)
        {
            LlaveBucle ll = sitio.llave;
            Vector3 lado = transform.right * 0.9f + transform.forward * 0.5f;
            ll.transform.position = Mundo.EnSuelo(transform.position + lado, 0.12f);
            ll.soloConLinterna = false;
            ll.Disponible = true;
            GameObject brillo = new GameObject("BrilloLlave");
            brillo.transform.position = ll.transform.position + Vector3.up * 0.4f;
            Light b = brillo.AddComponent<Light>(); b.type = LightType.Point; b.range = 3f; b.intensity = 2f; b.color = new Color(1f, 0.85f, 0.4f);
            Destroy(brillo, 20f);
            MisionManager mm = FindAnyObjectByType<MisionManager>();
            if (mm != null && Camera.main != null)
            {
                Vector3 cam = Camera.main.transform.position;
                yield return mm.Toma(cam, Vector3.Lerp(cam, ll.transform.position + Vector3.up * 1.2f, 0.6f), ll.transform.position, 2.4f, "LLAVE 1 / 5", "algo brilla entre las cenizas");
            }
        }
        // Algo en el bosque vio la luz
        yield return new WaitForSeconds(4f);
        DialogoUI.Susurrar("", "...algo se mueve entre los árboles. La luz lo atrajo.", 3f);
        if (AcechoDirector.Instancia != null) AcechoDirector.Instancia.Habilitar(true);
    }

    void CrearFlamas()
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        Material a = new Material(sh), b = new Material(sh);
        Color ca = new Color(1f, 0.45f, 0.1f), cb = new Color(1f, 0.85f, 0.3f);
        if (a.HasProperty("_BaseColor")) { a.SetColor("_BaseColor", ca); b.SetColor("_BaseColor", cb); }
        a.color = ca; b.color = cb;
        flamas = new Transform[5];
        for (int i = 0; i < flamas.Length; i++)
        {
            GameObject f = GameObject.CreatePrimitive(i % 2 == 0 ? PrimitiveType.Sphere : PrimitiveType.Capsule);
            f.name = "Fuego_" + i;
            Destroy(f.GetComponent<Collider>());
            f.transform.SetParent(transform, true);
            float ang = i * 72f * Mathf.Deg2Rad;
            f.transform.position = Mundo.EnSuelo(transform.position) + new Vector3(Mathf.Cos(ang) * 0.18f, 0.3f, Mathf.Sin(ang) * 0.18f);
            f.GetComponent<Renderer>().sharedMaterial = i % 2 == 0 ? a : b;
            f.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            flamas[i] = f.transform;
        }
    }

    void Update()
    {
        if (!Prendida) return;
        float t = Time.time * 8f + semilla;
        if (luz != null) luz.intensity = baseIntensidad * (0.75f + 0.25f * Mathf.PerlinNoise(t, 0.5f));
        if (flamas != null)
            for (int i = 0; i < flamas.Length; i++)
            {
                float k = Mathf.PerlinNoise(t + i * 2.3f, 1.1f);
                flamas[i].localScale = new Vector3(0.16f, 0.25f + 0.25f * k, 0.16f);
            }
    }
}
