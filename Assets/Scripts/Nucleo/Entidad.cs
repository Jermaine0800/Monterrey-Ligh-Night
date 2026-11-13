using System;
using UnityEngine;

/// <summary>
/// Cualquier cosa que puede recibir daño: el jugador, los monstruos, objetos rompibles.
/// </summary>
public interface IDaniable
{
    bool EstaVivo { get; }
    void RecibirDanio(float cantidad, Vector3 origen);
}

/// <summary>Algo que vuelve a su estado inicial cuando el jugador reaparece.</summary>
public interface IReiniciable
{
    void Reiniciar();
}

/// <summary>
/// Clase base de todo ser con vida en el juego (jugador y enemigos).
/// Las clases hijas deciden qué pasa al recibir daño y al morir (polimorfismo).
/// </summary>
public abstract class Entidad : MonoBehaviour, IDaniable
{
    [Header("Vida")]
    [SerializeField] protected float vidaMaxima = 100f;
    [SerializeField] protected float invulnerableTrasGolpe = 0.4f;

    public float Vida { get; protected set; }
    public float VidaMaxima => vidaMaxima;
    public float Vida01 => vidaMaxima > 0 ? Vida / vidaMaxima : 0f;
    public bool EstaVivo => Vida > 0f;

    /// <summary>(vidaActual, vidaMaxima)</summary>
    public event Action<float, float> AlCambiarVida;
    public event Action AlMorir;

    float finInvulnerable;

    protected virtual void Awake()
    {
        Vida = vidaMaxima;
    }

    public void RecibirDanio(float cantidad, Vector3 origen)
    {
        if (!EstaVivo || cantidad <= 0f || Time.time < finInvulnerable) return;
        finInvulnerable = Time.time + invulnerableTrasGolpe;

        Vida = Mathf.Max(0f, Vida - ModificarDanio(cantidad));
        AlCambiarVida?.Invoke(Vida, vidaMaxima);
        AlRecibirDanio(cantidad, origen);

        if (Vida <= 0f)
        {
            AlMorir?.Invoke();
            Morir();
        }
    }

    public void Curar(float cantidad)
    {
        if (!EstaVivo) return;
        Vida = Mathf.Min(vidaMaxima, Vida + cantidad);
        AlCambiarVida?.Invoke(Vida, vidaMaxima);
    }

    protected void Revivir()
    {
        Vida = vidaMaxima;
        AlCambiarVida?.Invoke(Vida, vidaMaxima);
    }

    /// <summary>Permite a las hijas reducir o aumentar el daño (ej. el jugador escondido).</summary>
    protected virtual float ModificarDanio(float cantidad) => cantidad;

    /// <summary>Reacción al golpe (sonido, retroceso, aturdimiento...).</summary>
    protected virtual void AlRecibirDanio(float cantidad, Vector3 origen) { }

    protected abstract void Morir();
}
