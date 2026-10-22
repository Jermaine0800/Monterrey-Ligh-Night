using UnityEngine;

public class ItemPickup : MonoBehaviour, IInteractable
{
    public ItemDefinition item;
    public bool esObjetoMano = false; // Marca true si es la lintera oficial
    public void Interact(PlayerInteraction jugador)
    {
        InventoryUI ui = FindFirstObjectByType<InventoryUI>();
        PlayerInventory inventario = FindFirstObjectByType<PlayerInventory>();

        if (ui == null || inventario == null)
        {
            Debug.LogWarning("Falta InventoryUI o PlayerInventory en la escena.");
            return;
        }

        if (!ui.hasBackPack)
        {
            Debug.Log("Necesitas una mochila para recoger esto.");
            return;
        }

        if (inventario.Agregar(item))
        {
            Debug.Log("Recogiste: " + item.nombre);
            Destroy(gameObject);
        }
        else
        {
            Debug.Log("Inventario lleno.");
        }
    }
}