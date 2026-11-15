using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    public float alcance = 1.5f;
    public Transform socketEspalda;

    void Update()
    {
        if (!HorrorGameState.PuedeJugar) return; // no interactuar con inventario/pausa abiertos
        PlayerEscondite esc = GetComponent<PlayerEscondite>();
        if (esc != null && (esc.Escondido || Time.time - esc.UltimaSalida < 0.5f)) return;

        // Cambiado a Gamepad.current.rightShoulder (Botón RB)
        if (HorrorInput.RB)
        {
            Intentar();
        }
    }

    void Intentar()
    {
        Vector3 centro = transform.position + Vector3.up * 0.9f;
        Collider[] cercanos = Physics.OverlapSphere(centro, alcance, ~0, QueryTriggerInteraction.Collide);

        foreach (Collider c in cercanos)
        {
            IInteractable objeto = c.GetComponentInParent<IInteractable>();
            if (objeto != null)
            {
                objeto.Interact(this);
                return;
            }
        }
    }
}