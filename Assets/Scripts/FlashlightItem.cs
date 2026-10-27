using UnityEngine;

public class FlashlightItem : InteractableItem
{
    protected override void OnPickedUp(PlayerInteraction jugador)
    {
        // Buscamos el socket directamente en la jerarquía del X Bot de manera segura
        Transform socketMano = jugador.transform.Find("mixamorig:Hips/mixamorig:Spine/mixamorig:Spine1/mixamorig:Spine2/mixamorig:RightShoulder/mixamorig:RightArm/mixamorig:RightForeArm/mixamorig:RightHand/SocketMano");

        if (socketMano != null)
        {
            // Apagamos físicas y colisiones para que no caiga al suelo
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            // Lo mandamos a la mano
            transform.SetParent(socketMano);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one; // Evita que se deforme

            Debug.Log("Linterna equipada en la mano exitosamente.");
        }
        else
        {
            Debug.LogWarning("No se encontró el SocketMano. Revisa la ruta de los huesos.");
            Destroy(gameObject);
        }
    }
}