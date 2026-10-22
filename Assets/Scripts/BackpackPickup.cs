using UnityEngine;

public class BackpackPickup : MonoBehaviour, IInteractable
{
    public void Interact(PlayerInteraction jugador)
    {
        InventoryUI inventory = FindFirstObjectByType<InventoryUI>();

        if (inventory == null)
        {
            Debug.LogWarning("No encontré el InventoryUI en la escena.");
            return;
        }

        inventory.hasBackPack = true;
        Debug.Log("¡Mochila recogida! Ahora puedes abrir el inventario.");

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (jugador.socketEspalda != null)
        {
            transform.SetParent(jugador.socketEspalda);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}