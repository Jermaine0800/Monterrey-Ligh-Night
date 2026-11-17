using UnityEngine;

/// <summary>Vela de tumba (panteón): se prende con RB. Su luz tiembla.</summary>
[RequireComponent(typeof(Collider))]
public class VelaPanteon : MonoBehaviour, IInteractable
{
    public Light luz;
    public bool Prendida { get; private set; }

    void Start() { if (luz != null) luz.enabled = false; }

    public void Interact(PlayerInteraction jugador)
    {
        if (Prendida) return;
        Prendida = true;
        if (luz != null) luz.enabled = true;
        AudioSource.PlayClipAtPoint(ProceduralAudio.Susurro(Random.Range(0, 200)), transform.position, 0.6f);
        VHSOverlay.Glitch(0.25f, 0.2f);
        string[] frases = { "La flama se puso azul un segundo.", "Alguien suspiró detrás de la lápida.", "La vela está tibia... como si alguien la acabara de apagar." };
        DialogoUI.Susurrar("", frases[Random.Range(0, frases.Length)], 3f);
    }
}
