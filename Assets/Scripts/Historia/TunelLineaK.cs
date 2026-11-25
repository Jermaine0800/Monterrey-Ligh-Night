using System.Collections;
using UnityEngine;

/// <summary>
/// El Túnel Línea K: un muro de concreto con una marca pintada. Si la alumbras con la linterna,
/// la cinta se "traba" y aparece la puerta oxidada del EXPEDIENTE 08.
/// El interior (andén, caseta, casillero) está construido bajo tierra; las puertas te llevan de un lado a otro.
/// </summary>
public class TunelLineaK : MonoBehaviour
{
    [Header("Exterior")]
    public Transform muro;
    public Transform marca;
    public GameObject puertaExterior;

    [Header("Interior")]
    public Transform interior;
    public Transform monitor;
    public Casillero casillero;
    public Transform puertaSalida;
    public Light[] luces;
    public GameObject silueta;          // la figura de cabeza radial al fondo de las vías

    [Header("Ajustes")]
    public float segundosAlumbrando = 0.8f;
    public float distanciaMaxima = 16f;

    public bool Revelado { get; private set; }
    public bool JugadorDentro => jugador != null && interior != null && jugador.position.y < interior.position.y + 15f;

    Transform jugador;
    FlashlightController linterna;
    float alumbrado;
    AudioSource fuente;

    void Start()
    {
        if (puertaExterior != null) puertaExterior.SetActive(false);
        if (silueta != null) silueta.SetActive(false);
        fuente = gameObject.AddComponent<AudioSource>();
        fuente.spatialBlend = 1f; fuente.minDistance = 3f; fuente.maxDistance = 30f;
        CrearRampas();
        // un poco más de luz en el andén (antes casi no se veía)
        if (luces != null) foreach (Light l in luces) if (l != null) { l.intensity *= 1.5f; l.range *= 1.25f; }
    }

    /// <summary>
    /// Rampas para subir de las vías al andén (el andén mide 1 m: si te caías a las vías ya no podías subir).
    /// Dos a los lados del andén y una en la punta donde se corta la obra.
    /// </summary>
    void CrearRampas()
    {
        if (interior == null || interior.Find("Rampa_Anden_0") != null) return;
        Transform anden = interior.Find("Anden");
        Material mat = anden != null && anden.GetComponent<Renderer>() != null ? anden.GetComponent<Renderer>().sharedMaterial : null;
        float largo = Mathf.Sqrt(1.6f * 1.6f + 1f);      // baja 1 m en 1.6 m (≈32°)
        float[] zs = { -14f, 1.5f };
        for (int i = 0; i < zs.Length; i++)
            Rampa("Rampa_Anden_" + i, new Vector3(0.247f, 0.415f, zs[i]), Quaternion.Euler(0f, 0f, -32f), new Vector3(largo + 0.15f, 0.2f, 2.2f), mat);
        Rampa("Rampa_Anden_Punta", new Vector3(-2.75f, 0.415f, 6.75f), Quaternion.Euler(32f, 0f, 0f), new Vector3(4.4f, 0.2f, largo + 0.15f), mat);
    }

    void Rampa(string nombre, Vector3 local, Quaternion giro, Vector3 tam, Material mat)
    {
        GameObject r = GameObject.CreatePrimitive(PrimitiveType.Cube);
        r.name = nombre;
        r.transform.SetParent(interior, false);
        r.transform.localPosition = local;
        r.transform.localRotation = giro;
        r.transform.localScale = tam;
        if (mat != null) r.GetComponent<Renderer>().sharedMaterial = mat;
        r.isStatic = false;
        // franja amarilla de precaución en la orilla
        GameObject f = GameObject.CreatePrimitive(PrimitiveType.Cube);
        f.name = nombre + "_Franja";
        Destroy(f.GetComponent<Collider>());
        f.transform.SetParent(r.transform, false);
        f.transform.localPosition = new Vector3(0f, 0.51f, 0f);
        f.transform.localScale = new Vector3(1f, 0.02f, 0.12f);
        Renderer fr = f.GetComponent<Renderer>();
        Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh != null) { Material m = new Material(sh); m.SetColor("_BaseColor", new Color(0.85f, 0.75f, 0.1f)); fr.sharedMaterial = m; }
    }

    void Update()
    {
        if (jugador == null)
        {
            PlayerMovement pm = FindAnyObjectByType<PlayerMovement>();
            if (pm == null) return;
            jugador = pm.transform;
            linterna = pm.GetComponent<FlashlightController>();
            if (linterna == null) linterna = FindAnyObjectByType<FlashlightController>();
        }
        if (Revelado || marca == null || linterna == null || !linterna.Encendida) return;

        Camera cam = Camera.main;
        if (cam == null) return;
        Vector3 a = marca.position - cam.transform.position;
        bool apuntando = a.magnitude < distanciaMaxima && Vector3.Angle(cam.transform.forward, a) < 12f;
        alumbrado = apuntando ? alumbrado + Time.deltaTime : Mathf.Max(0f, alumbrado - Time.deltaTime);
        if (alumbrado > segundosAlumbrando) StartCoroutine(Revelar());
    }

    IEnumerator Revelar()
    {
        Revelado = true;
        fuente.transform.position = marca.position;
        fuente.PlayOneShot(ProceduralAudio.Estatica(41), 1f);
        VHSOverlay.Glitch(1f, 1.1f);
        HorrorInput.Vibrar(0.4f, 0.8f, 0.5f, this);
        yield return new WaitForSeconds(0.35f);
        if (puertaExterior != null) puertaExterior.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        fuente.PlayOneShot(ProceduralAudio.Rechinido(8), 0.8f);
    }

    /// <summary>Se van las luces. Algo está parado en las vías. Vuelven las luces: ya no está.</summary>
    public IEnumerator Apagon()
    {
        float[] intensidades = new float[luces.Length];
        for (int i = 0; i < luces.Length; i++) if (luces[i] != null) intensidades[i] = luces[i].intensity;

        // Fallan
        for (int k = 0; k < 6; k++)
        {
            foreach (Light l in luces) if (l != null) l.enabled = k % 2 == 1;
            yield return new WaitForSeconds(Random.Range(0.05f, 0.2f));
        }
        foreach (Light l in luces) if (l != null) l.enabled = false;

        // Zumbido grave y silueta al fondo, con una sola luz roja
        AudioSource s = fuente;
        if (silueta != null)
        {
            silueta.SetActive(true);
            s.transform.position = silueta.transform.position;
            Light roja = silueta.GetComponentInChildren<Light>();
            if (roja != null) roja.enabled = true;
        }
        s.PlayOneShot(ProceduralAudio.Drone(66), 1f);
        VHSOverlay.Glitch(0.5f, 2.5f);
        yield return new WaitForSeconds(2.8f);

        // Pasos sobre la grava... acercándose
        for (int i = 0; i < 4; i++)
        {
            if (silueta != null)
            {
                Vector3 d = jugador.position - silueta.transform.position; d.y = 0;
                silueta.transform.position += d.normalized * 2.5f;
            }
            s.PlayOneShot(ProceduralAudio.Paso(700 + i), 1f);
            yield return new WaitForSeconds(0.5f);
        }

        // Vuelve la luz: no hay nada
        VHSOverlay.Glitch(1f, 0.6f);
        if (silueta != null) silueta.SetActive(false);
        for (int i = 0; i < luces.Length; i++) if (luces[i] != null) { luces[i].enabled = true; luces[i].intensity = intensidades[i] * 0.6f; }
        HorrorInput.Vibrar(0.7f, 0.5f, 0.4f, this);
    }
}
