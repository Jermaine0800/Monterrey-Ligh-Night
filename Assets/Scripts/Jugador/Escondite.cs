using UnityEngine;

/// <summary>
/// Lugar donde el jugador se puede esconder (arbustos, troncos huecos, casilleros, bajo una cama).
/// Se usa con RB como cualquier otra cosa interactuable.
/// Mientras estás escondido los monstruos no te ven ni te pueden lastimar,
/// pero si te vieron entrar, lo saben...
/// </summary>
[RequireComponent(typeof(Collider))]
public class Escondite : MonoBehaviour, IInteractable
{
    [Tooltip("Dónde se coloca el jugador al esconderse. Si está vacío, usa el centro de este objeto.")]
    public Transform puntoDentro;
    [Tooltip("Dónde sale el jugador. Si está vacío, sale frente al escondite.")]
    public Transform puntoSalida;
    public string nombre = "arbusto";

    [Tooltip("Altura de los ojos escondido (sobre el suelo). -1 = automática según el tipo")]
    public float alturaOjos = -1f;

    public bool Ocupado { get; private set; }

    /// <summary>¿Es un mueble con rendijas (casillero, ropero, armario)? Cambia la vista desde dentro.</summary>
    public bool EsMueble
    {
        get { string n = nombre.ToLowerInvariant(); return n.Contains("casillero") || n.Contains("ropero") || n.Contains("armario") || n.Contains("closet") || n.Contains("locker"); }
    }

    public float AlturaOjos
    {
        get
        {
            if (alturaOjos > 0f) return alturaOjos;
            string n = nombre.ToLowerInvariant();
            if (EsMueble) return 1.5f;
            if (n.Contains("cama") || n.Contains("mesa")) return 0.35f;
            if (n.Contains("tronco") || n.Contains("tumba") || n.Contains("cripta")) return 0.6f;
            return 0.7f;   // arbustos y demás
        }
    }

    public Vector3 PosicionDentro => puntoDentro != null ? puntoDentro.position : transform.position;
    public Vector3 PosicionSalida => puntoSalida != null ? puntoSalida.position : transform.position + transform.forward * 1.5f;

    public void Interact(PlayerInteraction jugador)
    {
        PlayerEscondite e = jugador.GetComponent<PlayerEscondite>();
        if (e == null) e = jugador.gameObject.AddComponent<PlayerEscondite>();
        if (!e.Escondido) e.Entrar(this);
    }

    public void Marcar(bool ocupado) => Ocupado = ocupado;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }
}
