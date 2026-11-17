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

        // El más cercano a lo que estás viendo (las llaves tienen prioridad)
        Vector3 frente = centro + transform.forward * 0.6f;
        IInteractable mejor = null;
        float mejorPuntaje = float.MaxValue;
        foreach (Collider c in cercanos)
        {
            IInteractable objeto = c.GetComponentInParent<IInteractable>();
            if (objeto == null) continue;
            float puntaje = Vector3.Distance(frente, c.ClosestPoint(frente));
            if (objeto is LlaveBucle) puntaje -= 1f;
            if (objeto is Casillero cas && cas.Abierto) puntaje += 2f;
            if (puntaje < mejorPuntaje) { mejorPuntaje = puntaje; mejor = objeto; }
        }
        if (mejor != null) mejor.Interact(this);
    }
}