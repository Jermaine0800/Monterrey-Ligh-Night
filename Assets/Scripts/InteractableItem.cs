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

        // Validación de seguridad para que no se congele si falta algún componente
        if (inventario == null)
        {
            Debug.LogError("¡Falta el componente PlayerInventory en la escena!");
            return;
        }

        // Si requiere mochila y la UI existe pero no la tiene
        if (!esObjetoMano && ui != null && !ui.hasBackPack)
        {
            Debug.Log("Necesitas una mochila para recoger esto.");
            return;
        }

        if (inventario.Agregar(item))
        {
            yaFueRecogido = true;
            Debug.Log("Recogiste: " + (item != null ? item.nombre : "Objeto"));
            OnPickedUp(jugador);
        }
        else
        {
            Debug.Log("¡Inventario lleno!");
        }
    }

    protected virtual void OnPickedUp(PlayerInteraction jugador)
    {
        Destroy(gameObject);
    }
}