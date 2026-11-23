using UnityEngine;

/// <summary>
/// Una carta, foto o página que se lee con [A / E]. En cada lugar hay DOS juntas:
/// una con pistas de ayer y otra de alguien que ya estuvo en el bucle (instrucciones para la llave).
/// Al leerla se guarda en el maletín para releerla.
/// </summary>
[RequireComponent(typeof(Collider))]
public class NotaLeible : MonoBehaviour, IInteractable
{
    public string titulo = "NOTA";
    [TextArea(4, 12)] public string texto = "";

    public bool Leida { get; private set; }
    public event System.Action<NotaLeible> AlLeer;

    public void Interact(PlayerInteraction jugador)
    {
        if (DocumentoUI.Abierto) return;
        StartCoroutine(DocumentoUI.Mostrar(titulo, texto));
        if (Leida) return;
        Leida = true;
        InventarioUtil.GuardarNota(titulo, texto);   // casilla "Notas (n)" del maletín
        if (InventarioUtil.TieneMochila) DialogoUI.Susurrar("", "Nota guardada en el maletín  [Y]", 2.5f);
        Light l = GetComponentInChildren<Light>();
        if (l != null) l.enabled = false;
        AlLeer?.Invoke(this);
    }
}
