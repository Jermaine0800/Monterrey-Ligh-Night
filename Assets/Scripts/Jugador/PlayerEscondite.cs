using System.Collections;
using UnityEngine;

/// <summary>
/// Estado "escondido" del jugador. Entra con RB en un Escondite y sale con RB o B.
/// La cámara pasa a primera persona para asomarse, el personaje se agacha
/// y se escucha su respiración contenida.
/// </summary>
public class PlayerEscondite : MonoBehaviour
{
    public bool Escondido { get; private set; }
    public Escondite Actual { get; private set; }
    /// <summary>¿Algún monstruo lo vio entrar? (los enemigos lo usan para revisar el escondite)</summary>
    public bool LoVieronEntrar { get; set; }
    /// <summary>Momento en que salió por última vez (para no volver a entrar con el mismo botón).</summary>
    public float UltimaSalida { get; private set; } = -10f;

    PlayerMovement movimiento;
    CameraController cam;
    AudioSource respiracion;
    float puedeSalir;
    bool estabaEnPrimera;

    void Awake()
    {
        movimiento = GetComponent<PlayerMovement>();
        respiracion = gameObject.AddComponent<AudioSource>();
        respiracion.clip = ProceduralAudio.Viento(55);
        respiracion.loop = true;
        respiracion.volume = 0f;
        respiracion.pitch = 2.2f; // el viento acelerado suena como respiración
    }

    public void Entrar(Escondite e)
    {
        if (Escondido || e == null || e.Ocupado) return;
        StartCoroutine(RutinaEntrar(e));
    }

    IEnumerator RutinaEntrar(Escondite e)
    {
        Actual = e;
        e.Marcar(true);
        Escondido = true;
        LoVieronEntrar = false;
        puedeSalir = Time.time + 0.6f;
        VHSOverlay.Glitch(0.25f, 0.2f);

        if (movimiento != null)
        {
            movimiento.CambiarAgachado(true);
            Vector3 mirar = e.PosicionSalida - e.PosicionDentro; mirar.y = 0;
            Quaternion rot = mirar.sqrMagnitude > 0.01f ? Quaternion.LookRotation(mirar) : transform.rotation;
            movimiento.Teletransportar(e.PosicionDentro, rot);
            movimiento.enabled = false;
        }

        cam = FindFirstObjectByType<CameraController>();
        if (cam != null)
        {
            estabaEnPrimera = cam.EnPrimeraPersona;
            cam.ForzarPrimeraPersona(true);
        }
        AnimarEscondido(true);
        respiracion.Play();
        DialogoUI.Susurrar("", "Escondido en " + e.nombre + "   [RB] salir", 3f);
        yield break;
    }

    void Update()
    {
        if (!Escondido) return;
        respiracion.volume = Mathf.MoveTowards(respiracion.volume, 0.25f, Time.deltaTime * 0.3f);
        if (HorrorGameState.PuedeJugar && Time.time > puedeSalir && (HorrorInput.RB || HorrorInput.Agacharse)) Salir();
    }

    void AnimarEscondido(bool v)
    {
        Animator a = GetComponent<Animator>();
        if (a == null || a.runtimeAnimatorController == null) return;
        foreach (AnimatorControllerParameter p in a.parameters)
        {
            if (p.name == "Escondido") a.SetBool("Escondido", v);
            if (p.name == "Agachado") a.SetBool("Agachado", v || (movimiento != null && movimiento.Agachado));
            if (p.name == "Speed") a.SetFloat("Speed", 0f);
        }
    }

    public void Salir()
    {
        if (!Escondido) return;
        Escondido = false;
        UltimaSalida = Time.time;
        AnimarEscondido(false);
        respiracion.volume = 0f;
        respiracion.Stop();
        if (Actual != null)
        {
            Actual.Marcar(false);
            if (movimiento != null)
            {
                movimiento.enabled = true;
                movimiento.Teletransportar(Actual.PosicionSalida, transform.rotation);
            }
        }
        if (cam != null && !estabaEnPrimera) cam.ForzarPrimeraPersona(false);
        Actual = null;
    }
}
