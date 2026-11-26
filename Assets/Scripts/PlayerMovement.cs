using UnityEngine;

/// <summary>
/// Movimiento del jugador: caminar, correr (con aguante), agacharse y gravedad.
/// Funciona con control de Xbox o teclado. Expone el "ruido" que hace el jugador
/// para que los monstruos lo puedan escuchar.
/// Parámetros del Animator que usa (si existen): Speed (0 quieto, 0.5 camina, 1 corre), Agachado (bool).
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Velocidades")]
    public float velocidadCaminar = 2.4f;
    public float velocidadCorrer = 5.2f;
    public float velocidadAgachado = 1.2f;
    public float gravity = -20f;
    public float velocidadGiro = 10f;

    [Header("Aguante para correr")]
    public float aguanteMaximo = 16f;    // segundos corriendo
    public float recuperacion = 1.5f;    // por segundo

    [Header("Altura del cuerpo")]
    public float alturaDePie = 1.8f;
    public float alturaAgachado = 1.1f;

    public Transform camara; // Referencia a la cámara para obtener su orientación

    public bool Corriendo { get; private set; }
    public bool Agachado { get; private set; }
    public float Aguante01 => aguante / aguanteMaximo;
    /// <summary>Agotado: te quedaste sin aire. No puedes correr hasta recuperar el 35% y caminas un poco más lento.</summary>
    public bool Agotado { get; private set; }
    /// <summary>Tope de condición: si estás herido te cansas antes (máximo entre 60% y 100% según la vida).</summary>
    public float Tope01 { get; private set; } = 1f;

    /// <summary>Gasta condición (golpes, forcejeos). Devuelve false si no te alcanza.</summary>
    public bool Gastar(float segundos)
    {
        if (aguante < segundos * 0.5f) { Agotado = true; return false; }
        aguante = Mathf.Max(0f, aguante - segundos);
        if (aguante <= 0.05f) Agotado = true;
        ultimoGasto = Time.time;
        return true;
    }
    float ultimoGasto = -10f;
    AudioSource jadeo;
    /// <summary>0 = silencio, 0.3 agachado, 0.6 caminando, 1 corriendo.</summary>
    public float NivelRuido { get; private set; }
    /// <summary>En primera persona el cuerpo gira con la cámara.</summary>
    public bool PrimeraPersona { get; set; }

    CharacterController controller;
    Animator animator;
    float velocidadVertical;
    float aguante;
    bool correrAlternado;
    bool tieneSpeed, tieneAgachado;
    float centroOriginalY;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        aguanteMaximo = Mathf.Max(aguanteMaximo, 16f); recuperacion = Mathf.Max(recuperacion, 2.4f);   // (la escena guardaba 6 s)
        aguante = aguanteMaximo;
        centroOriginalY = controller.center.y;
        alturaDePie = controller.height;
        alturaAgachado = Mathf.Min(alturaAgachado, alturaDePie * 0.65f);

        if (camara == null && Camera.main != null) camara = Camera.main.transform;
        RevisarParametros();
    }

    /// <summary>Se llama al cambiar el Animator Controller (por ejemplo al cambiar de personaje).</summary>
    public void RevisarParametros()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        tieneSpeed = tieneAgachado = false;
        if (animator == null || animator.runtimeAnimatorController == null) return;
        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.name == "Speed") tieneSpeed = true;
            if (p.name == "Agachado") tieneAgachado = true;
        }
    }

    void Update()
    {
        if (camara == null) { if (Camera.main != null) camara = Camera.main.transform; else return; }

        if (!HorrorGameState.PuedeJugar)
        {
            Animar(0f);
            NivelRuido = 0f;
            return;
        }

        if (controller.isGrounded && velocidadVertical < 0) velocidadVertical = -2f;

        // ---- Agacharse (B) ----
        if (HorrorInput.Agacharse) CambiarAgachado(!Agachado);

        // ---- Movimiento ----
        Vector2 input = HorrorInput.Movimiento;
        Vector3 adelante = camara.forward; adelante.y = 0; adelante.Normalize();
        Vector3 derecha = camara.right;   derecha.y = 0;  derecha.Normalize();
        Vector3 direccion = adelante * input.y + derecha * input.x;
        bool moviendose = input.sqrMagnitude > 0.04f;

        // ---- Correr (LB mantenido o L3 alterna) ----
        if (HorrorInput.CorrerAlternar) correrAlternado = !correrAlternado;
        if (!moviendose) correrAlternado = false;
        bool quiereCorrer = (HorrorInput.CorrerMantenido || correrAlternado) && moviendose && !Agachado;
        // ---- Condición física ----
        SaludJugador sj = SaludJugador.Instancia;
        Tope01 = sj != null ? Mathf.Lerp(0.6f, 1f, sj.Vida01) : 1f;
        float tope = aguanteMaximo * Tope01;
        if (quiereCorrer && !Agotado && aguante > 0.1f)
        {
            Corriendo = true;
            aguante = Mathf.Max(0f, aguante - Time.deltaTime);
            ultimoGasto = Time.time;
            if (aguante <= 0f) { correrAlternado = false; Agotado = true; }
        }
        else
        {
            Corriendo = false;
            // Se recupera después de 1 s sin esfuerzo: más rápido quieto o agachado, más lento caminando
            if (Time.time - ultimoGasto > 1f)
            {
                float k = !moviendose ? 1.6f : (Agachado ? 1.3f : 0.8f);
                aguante = Mathf.Min(tope, aguante + recuperacion * k * Time.deltaTime);
            }
        }
        aguante = Mathf.Min(aguante, tope);
        if (Agotado && aguante >= aguanteMaximo * 0.35f) Agotado = false;
        Jadear();

        float velocidad = Agachado ? velocidadAgachado : (Corriendo ? velocidadCorrer : velocidadCaminar * (Agotado ? 0.82f : 1f));

        // ---- Girar el cuerpo ----
        if (PrimeraPersona)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(adelante), 20f * Time.deltaTime);
        }
        else if (direccion.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direccion), velocidadGiro * Time.deltaTime);
        }

        velocidadVertical += gravity * Time.deltaTime;
        Vector3 movimientoFinal = direccion * velocidad;
        movimientoFinal.y = velocidadVertical;
        controller.Move(movimientoFinal * Time.deltaTime);

        // ---- Animación y ruido ----
        float magnitud = Mathf.Clamp01(input.magnitude);
        float speedAnim = Corriendo ? 1f : magnitud * 0.5f;
        Animar(speedAnim);
        NivelRuido = !moviendose ? 0f : (Agachado ? 0.25f : (Corriendo ? 1f : 0.6f));
        if (Agotado) NivelRuido = Mathf.Max(NivelRuido, 0.4f);   // jadear se oye
    }

    void Animar(float speed)
    {
        if (animator == null) return;
        if (tieneSpeed) animator.SetFloat("Speed", speed, 0.1f, Time.deltaTime);
        if (tieneAgachado) animator.SetBool("Agachado", Agachado);
    }

    /// <summary>Respiración: se oye más fuerte entre más cansado estés (y te delata un poco).</summary>
    void Jadear()
    {
        if (jadeo == null)
        {
            jadeo = gameObject.AddComponent<AudioSource>();
            jadeo.clip = ProceduralAudio.Respiracion(9);
            jadeo.loop = true; jadeo.volume = 0f; jadeo.spatialBlend = 0f;
            jadeo.Play();
        }
        float cansancio = 1f - Aguante01;
        float objetivo = Agotado ? 0.55f : Mathf.Clamp01((cansancio - 0.45f) * 1.4f) * 0.35f;
        jadeo.volume = Mathf.MoveTowards(jadeo.volume, objetivo, Time.deltaTime * 0.6f);
        jadeo.pitch = Agotado ? 1.25f : 1f;
    }

    public void CambiarAgachado(bool valor)
    {
        if (valor == Agachado) return;
        // No levantarse si hay algo encima (un techo, una mesa...)
        if (!valor && Physics.Raycast(transform.position + Vector3.up * (alturaAgachado - 0.05f), Vector3.up,
                                      alturaDePie - alturaAgachado + 0.1f, ~0, QueryTriggerInteraction.Ignore))
            return;

        Agachado = valor;
        float h = Agachado ? alturaAgachado : alturaDePie;
        float factor = h / Mathf.Max(0.01f, alturaDePie);
        controller.height = h;
        controller.center = new Vector3(controller.center.x, centroOriginalY * factor, controller.center.z);
    }

    /// <summary>Teletransporta al jugador sin pelearse con el CharacterController.</summary>
    public void Teletransportar(Vector3 posicion, Quaternion rotacion)
    {
        controller.enabled = false;
        transform.SetPositionAndRotation(posicion, rotacion);
        controller.enabled = true;
        velocidadVertical = 0f;
    }
}
