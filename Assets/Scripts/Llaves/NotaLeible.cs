using UnityEngine;

/// <summary>Una nota, foto o página que se lee con RB (cuenta la historia del equipo que se perdió).</summary>
[RequireComponent(typeof(Collider))]
public class NotaLeible : MonoBehaviour, IInteractable
{
    public string titulo = "NOTA";
    [TextArea(4, 12)] public string texto = "";

    public void Interact(PlayerInteraction jugador)
    {
        if (DocumentoUI.Abierto) return;
        StartCoroutine(DocumentoUI.Mostrar(titulo, texto));
    }
}
