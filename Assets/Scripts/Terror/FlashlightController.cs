using UnityEngine;

/// <summary>
/// Linterna con batería. Va en el jugador. Busca automáticamente la luz de la linterna
/// cuando tienes algo en la mano (SocketMano) y crea una luz al pecho. X = prender/apagar.
/// </summary>
public class FlashlightController : MonoBehaviour
{
    [Header("Batería")]
    public float duracionBateria = 600f;   // segundos encendida de 100% a 0%
    [Range(0, 1)] public float bateria = 1f;

    [Header("Luz")]
    public float intensidad = 8.5f;
    public float rango = 32f;
    public float anguloSpot = 64f;
    public Color color = new Color(1f, 0.93f, 0.8f);

    public bool TieneLinterna => luz != null;
    public float Bateria01 => bateria;
    public bool Encendida => luz != null && encendida;

    Light luz;
    bool encendida;
    AudioSource src;
    AudioClip clic;
    float buscarCada;

    void Start()
    {
        src = gameObject.AddComponent<AudioSource>();
        src.playOnAwake = false;
        clic = ProceduralAudio.ClicMenu();
        // Un poquito de luz alrededor del jugador (como los ojos que se acostumbran a la oscuridad)
        GameObject ojos = new GameObject("LuzOjosAcostumbrados");
        ojos.transform.SetParent(transform, false);
        ojos.transform.localPosition = new Vector3(0f, 1.6f, 0.3f);
        Light l = ojos.AddComponent<Light>();
        l.type = LightType.Point; l.range = 9f; l.intensity = 0.9f; l.shadows = LightShadows.None;
        l.color = new Color(0.62f, 0.68f, 0.85f);
    }

    void Update()
    {
        if (luz == null)
        {
            buscarCada -= Time.deltaTime;
            if (buscarCada <= 0f) { buscarCada = 0.5f; Buscar(); }
            return;
        }

        if (HorrorGameState.PuedeJugar && HorrorInput.Linterna)
        {
            encendida = !encendida;
            src.PlayOneShot(clic, 0.7f);
        }

        if (encendida)
        {
            bateria = Mathf.Max(0f, bateria - Time.deltaTime / duracionBateria);
            // sin batería no se apaga del todo: queda una luz débil y temblorosa
        }

        // Parpadeo cuando la batería está baja (o durante glitches fuertes)
        float factor = 1f;
        if (bateria < 0.2f) factor = Random.value < 0.06f ? Random.Range(0.05f, 0.5f) : 1f;
        factor *= Mathf.Lerp(0.55f, 1f, bateria / 0.3f);
        if (bateria <= 0f) factor = 0.3f * (Random.value < 0.08f ? Random.Range(0.3f, 1f) : 1f);

        luz.enabled = encendida;
        luz.intensity = intensidad * factor;
    }

    void Buscar()
    {
        foreach (Transform t in GetComponentsInChildren<Transform>())
        {
            if (t.name != "SocketMano" || t.GetComponentInChildren<FlashlightItem>() == null) continue;

            // Apagamos las luces que traiga el modelo (su orientación en la mano no es confiable)
            foreach (Light l in t.GetComponentsInChildren<Light>(true)) l.enabled = false;

            // Luz al pecho, estilo Silent Hill: siempre apunta hacia donde mira el personaje
            GameObject go = new GameObject("LuzLinterna");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0.15f, 1.35f, 0.25f);
            go.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
            luz = go.AddComponent<Light>();
            luz.type = LightType.Spot;
            luz.range = rango;
            luz.spotAngle = anguloSpot;
            luz.innerSpotAngle = anguloSpot * 0.45f;
            luz.color = color;
            luz.shadows = LightShadows.Soft;
            encendida = true;   // al recogerla se prende
            VHSOverlay.Glitch(0.3f, 0.2f);
            return;
        }
    }

    public void Recargar(float cantidad01)
    {
        bateria = Mathf.Clamp01(bateria + cantidad01);
    }
}
