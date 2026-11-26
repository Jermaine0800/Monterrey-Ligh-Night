using UnityEngine;

public class FlashlightItem : InteractableItem
{
    protected override void OnPickedUp(PlayerInteraction jugador)
    {
        // Búsqueda inteligente del SocketMano por nombre en todo el esqueleto del personaje
        Transform socketMano = null;
        foreach (Transform hijo in jugador.GetComponentsInChildren<Transform>())
        {
            if (hijo.name == "SocketMano")
            {
                socketMano = hijo;
                break;
            }
        }

        if (socketMano != null)
        {
            // Desactivamos físicas y colisiones para que no caiga al suelo
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            // La anclamos a la mano del personaje
            transform.SetParent(socketMano);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            Debug.Log("Linterna equipada en la mano correctamente.");
        }
        else
        {
            Debug.LogWarning("No se encontró ningún objeto llamado 'SocketMano' dentro del jugador.");
            Destroy(gameObject);
        }
    }
}