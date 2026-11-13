using UnityEngine;

public class ItemPickup : MonoBehaviour, IInteractable
{
    public ItemDefinition item;
    public bool esObjetoMano = false; // True si es la linterna

    private bool yaFueRecogido = false;

    public void Interact(PlayerInteraction jugador)
    {
        if (yaFueRecogido) return; // Evita que se vuelva a interactuar

        InventoryUI ui = FindFirstObjectByType<InventoryUI>();
        PlayerInventory inventario = FindFirstObjectByType<PlayerInventory>();

        if (ui == null || inventario == null) return;

        // Validar si requiere mochila y no es objeto de mano
        if (!esObjetoMano && !ui.hasBackPack)
        {
            Debug.Log("Necesitas una mochila para recoger esto.");
            return;
        }

        // Intentar agregar al inventario
        if (inventario.Agregar(item))
        {
            yaFueRecogido = true;
            Debug.Log("Recogiste: " + item.nombre);

            if (esObjetoMano)
            {
                // Buscar el SocketMano en el personaje
                Transform socketMano = jugador.transform.Find("mixamorig:Hips/mixamorig:Spine/mixamorig:Spine1/mixamorig:Spine2/mixamorig:RightShoulder/mixamorig:RightArm/mixamorig:RightForeArm/mixamorig:RightHand/SocketMano");

                if (socketMano != null)
                {
                    // Desactivar físicas y colisiones del suelo
                    Collider col = GetComponent<Collider>();
                    if (col != null) col.enabled = false;

                    Rigidbody rb = GetComponent<Rigidbody>();
                    if (rb != null) rb.isKinematic = true;

                    // Emparentar a la mano
                    transform.SetParent(socketMano);
                    transform.localPosition = Vector3.zero;
                    transform.localRotation = Quaternion.identity;
                }
                else
                {
                    Debug.LogWarning("No se encontró el SocketMano en el jugador.");
                    Destroy(gameObject);
                }
            }
            else
            {
                // Si es un ítem normal, desaparece del suelo
                Destroy(gameObject);
            }
        }
        else
        {
            Debug.Log("¡Inventario lleno!");
        }
    }
}