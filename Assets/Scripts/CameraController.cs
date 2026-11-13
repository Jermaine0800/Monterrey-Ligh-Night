using UnityEngine;

/// <summary>
/// Cámara en tercera o primera persona (R3 / V para cambiar).
/// - Tercera: orbita detrás del personaje y no atraviesa paredes ni terreno.
/// - Primera: a la altura de los ojos; el cuerpo se ve solo como sombra.
/// Durante diálogos la cámara voltea sola hacia quien habla.
/// </summary>
public class CameraController : MonoBehaviour
{
    public Transform objetivo;          // el personaje
    public float altura = 1.6f;         // punto de giro en tercera persona
    public float distancia = 2.5f;      // qué tan atrás se aleja la cámara
    public float sensibilidad = 120f;   // qué tan rápido gira con el stick
    public float minVertical = -30f;
    public float maxVertical = 60f;

    [Header("Primera persona")]
    public float alturaOjos = 1.62f;
    public float alturaOjosAgachado = 1.0f;
    public float adelanteOjos = 0.12f;

    [Header("Colisión de cámara (tercera persona)")]
    public float radioColision = 0.25f;
    public LayerMask capasColision = ~0;

    public bool EnPrimeraPersona => primeraPersona;

    float giroHorizontal;
    float giroVertical = 15f;
    float distanciaActual;
    float alturaOjosActual;
    bool primeraPersona, forzada;
    Transform enfoque;
    PlayerMovement movimiento;
    Renderer[] cuerpo;

    public void Enfocar(Transform t) { enfoque = t; }

    void Start()
    {
        if (objetivo != null)
        {
            giroHorizontal = objetivo.eulerAngles.y;
            movimiento = objetivo.GetComponent<PlayerMovement>();
        }
        // La cámara hija tenía un desplazamiento propio; lo sumamos aquí para que este script la controle por completo
        Camera hija = GetComponentInChildren<Camera>();
        if (hija != null && hija.transform != transform)
        {
            distancia += Mathf.Max(0f, -hija.transform.localPosition.z);
            altura += hija.transform.localPosition.y;
            hija.transform.localPosition = Vector3.zero;
            hija.transform.localRotation = Quaternion.identity;
            hija.nearClipPlane = 0.05f;
        }
        sensibilidad = PlayerPrefs.GetFloat("sensibilidad", sensibilidad); // desde Opciones del menú
        primeraPersona = PlayerPrefs.GetInt("primera_persona", 0) == 1;
        distanciaActual = distancia;
        alturaOjosActual = alturaOjos;
        AplicarModo();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>Por ejemplo al esconderse: forzar primera persona aunque el jugador use tercera.</summary>
    public void ForzarPrimeraPersona(bool valor)
    {
        forzada = valor;
        primeraPersona = valor || PlayerPrefs.GetInt("primera_persona", 0) == 1;
        AplicarModo();
    }

    /// <summary>Si se cambia el modelo del jugador, hay que volver a buscar sus renderers.</summary>
    public void RefrescarCuerpo()
    {
        cuerpo = null;
        AplicarModo();
    }

    void AplicarModo()
    {
        if (objetivo == null) return;
        if (movimiento == null) movimiento = objetivo.GetComponent<PlayerMovement>();
        if (movimiento != null) movimiento.PrimeraPersona = primeraPersona;
        if (cuerpo == null) cuerpo = objetivo.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in cuerpo)
        {
            if (r == null || r is ParticleSystemRenderer || r.GetComponentInParent<Light>() != null) continue;
            // En primera persona el cuerpo solo proyecta sombra (no tapa la vista)
            if (r is SkinnedMeshRenderer)
                r.shadowCastingMode = primeraPersona ? UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly
                                                     : UnityEngine.Rendering.ShadowCastingMode.On;
        }
    }

    void LateUpdate()
    {
        if (objetivo == null) return;

        // Cambiar de cámara (R3)
        if (!forzada && HorrorGameState.PuedeJugar && HorrorInput.CambiarCamara)
        {
            primeraPersona = !primeraPersona;
            PlayerPrefs.SetInt("primera_persona", primeraPersona ? 1 : 0);
            AplicarModo();
            VHSOverlay.Glitch(0.2f, 0.1f);
        }

        // 1. Stick derecho
        if (HorrorGameState.PuedeJugar)
        {
            Vector2 mirada = HorrorInput.Mirar;
            giroHorizontal += mirada.x * sensibilidad * Time.deltaTime;
            giroVertical -= mirada.y * sensibilidad * Time.deltaTime;
            float min = primeraPersona ? -75f : minVertical;
            float max = primeraPersona ? 75f : maxVertical;
            giroVertical = Mathf.Clamp(giroVertical, min, max);
        }

        // 1b. Durante un diálogo la cámara gira sola hacia quien habla
        if (enfoque != null)
        {
            Vector3 d = enfoque.position - objetivo.position; d.y = 0;
            if (d.sqrMagnitude > 0.01f)
            {
                float angulo = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
                giroHorizontal = Mathf.LerpAngle(giroHorizontal, angulo, 2.5f * Time.unscaledDeltaTime);
                giroVertical = Mathf.Lerp(giroVertical, primeraPersona ? -5f : 6f, 2f * Time.unscaledDeltaTime);
            }
        }

        Quaternion rotacion = Quaternion.Euler(giroVertical, giroHorizontal, 0f);
        transform.rotation = rotacion;

        if (primeraPersona)
        {
            bool agachado = movimiento != null && movimiento.Agachado;
            alturaOjosActual = Mathf.Lerp(alturaOjosActual, agachado ? alturaOjosAgachado : alturaOjos, 8f * Time.deltaTime);
            Vector3 frente = objetivo.forward; frente.y = 0;
            transform.position = objetivo.position + Vector3.up * alturaOjosActual + frente.normalized * adelanteOjos;
        }
        else
        {
            // 3. Pivote y distancia, sin atravesar terreno/paredes
            Vector3 pivote = objetivo.position + Vector3.up * altura;
            Vector3 atras = rotacion * Vector3.back;
            float deseada = distancia;
            if (Physics.SphereCast(pivote, radioColision, atras, out RaycastHit hit, distancia, capasColision, QueryTriggerInteraction.Ignore)
                && !hit.transform.IsChildOf(objetivo))
                deseada = Mathf.Max(0.3f, hit.distance - 0.05f);
            distanciaActual = Mathf.Lerp(distanciaActual, deseada, (deseada < distanciaActual ? 20f : 4f) * Time.deltaTime);
            transform.position = pivote + atras * distanciaActual;
        }
    }
}
