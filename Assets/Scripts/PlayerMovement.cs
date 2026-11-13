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
    public float aguanteMaximo = 6f;     // segundos corriendo
    public float recuperacion = 1.5f;    // por segundo

    [Header("Altura del cuerpo")]
    public float alturaDePie = 1.8f;
    public float alturaAgachado = 1.1f;

    public Transform camara; // Referencia a la cámara para obtener su orientación

    public bool Corriendo { get; private set; }
    public bool Agachado { get; private set; }
    public float Aguante01 => aguante / aguanteMaximo;
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
        animator = GetComponent<Animator>();
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
        if (animator == null) animator = GetComponent<Animator>();
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
        if (quiereCorrer && aguante > 0.1f)
        {
            Corriendo = true;
            aguante = Mathf.Max(0f, aguante - Time.deltaTime);
            if (aguante <= 0f) correrAlternado = false;
        }
        else
        {
            Corriendo = false;
            aguante = Mathf.Min(aguanteMaximo, aguante + recuperacion * Time.deltaTime);
        }

        float velocidad = Agachado ? velocidadAgachado : (Corriendo ? velocidadCorrer : velocidadCaminar);

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
    }

    void Animar(float speed)
    {
        if (animator == null) return;
        if (tieneSpeed) animator.SetFloat("Speed", speed, 0.1f, Time.deltaTime);
        if (tieneAgachado) animator.SetBool("Agachado", Agachado);
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
