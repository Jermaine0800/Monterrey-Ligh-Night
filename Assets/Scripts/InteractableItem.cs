using UnityEngine;

public abstract class InteractableItem : MonoBehaviour, IInteractable
{
    public ItemDefinition item;
    public bool esObjetoMano = false;
    protected bool yaFueRecogido = false;

    public virtual void Interact(PlayerInteraction jugador)
    {
        if (yaFueRecogido) return;

        InventoryUI ui = FindFirstObjectByType<InventoryUI>();
        PlayerInventory inventario = FindFirstObjectByType<PlayerInventory>();

        if (ui == null || inventario == null) return;

        if (!esObjetoMano && !ui.hasBackPack)
        {
            Debug.Log("Necesitas una mochila para recoger esto.");
            return;
        }

        if (inventario.Agregar(item))
        {
            yaFueRecogido = true;
            Debug.Log("Recogiste: " + item.nombre);
            OnPickedUp(jugador);
        }
        else
        {
            Debug.Log("¡Inventario lleno!");
        }
    }

    // Método virtual para que cada objeto hijo decida qué hacer al ser recogido (herencia)
    protected virtual void OnPickedUp(PlayerInteraction jugador)
    {
        Destroy(gameObject); // Por defecto se destruye del suelo
    }
}