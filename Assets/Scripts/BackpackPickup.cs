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
        foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>()) { rb.isKinematic = true; rb.detectCollisions = false; }
        foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = false;
        DialogoUI.Susurrar("", "Mochila. Ahora puedo guardar cosas.   Maletín [Y]", 4f);
        // Lo que ya traías antes de la mochila también se guarda
        PlayerInventory inv = jugador.GetComponent<PlayerInventory>() ?? InventarioUtil.Inventario;
        PlayerCombate pc = jugador.GetComponent<PlayerCombate>();
        if (inv != null && pc != null && pc.ArmaActual != null && !inv.Contiene(pc.ArmaActual)) inv.Agregar(pc.ArmaActual);
        InventarioUtil.CasillaNotas();
        Debug.Log("¡Mochila recogida! Ahora puedes abrir el inventario.");

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (jugador.socketEspalda != null)
        {
            ColocarEnEspalda(jugador);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// La mochila se queda derecha en la espalda sin importar cómo venía el modelo:
    /// se alinea con el torso del jugador (no con el hueso, que en Mixamo viene girado) y se pega detrás.
    /// </summary>
    void ColocarEnEspalda(PlayerInteraction jugador)
    {
        Transform raiz = jugador.transform;
        Transform hueso = jugador.socketEspalda;
        transform.SetParent(null, true);

        // Tamaño y centro del modelo en su orientación "de pie"
        transform.rotation = raiz.rotation * Quaternion.Euler(orientacionModelo);
        Bounds b = Limites();
        Vector3 desfaseCentro = transform.position - b.center;

        // Espalda: un poco por debajo de los hombros, detrás del torso
        float alto = 1.25f;
        Animator an = jugador.GetComponentInChildren<Animator>();
        if (an != null && an.isHuman)
        {
            Transform pecho = an.GetBoneTransform(HumanBodyBones.UpperChest) ?? an.GetBoneTransform(HumanBodyBones.Chest) ?? an.GetBoneTransform(HumanBodyBones.Spine);
            if (pecho != null) { hueso = pecho; alto = pecho.position.y - raiz.position.y; }
        }
        Vector3 centroEspalda = raiz.position + Vector3.up * (alto - 0.05f) - raiz.forward * (0.16f + b.extents.z * 0.5f);
        transform.position = centroEspalda + desfaseCentro;

        // Se pega al pecho para que siga la animación, pero sin girar con el hueso
        transform.SetParent(hueso != null ? hueso : raiz, true);
        SeguirTorso st = gameObject.AddComponent<SeguirTorso>();
        st.raiz = raiz;
        st.rotacionLocal = Quaternion.Inverse(raiz.rotation) * transform.rotation;
    }

    [Tooltip("Giro extra si el modelo de la mochila viene acostado o de lado")]
    public Vector3 orientacionModelo = Vector3.zero;

    Bounds Limites()
    {
        Renderer[] rs = GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(transform.position, Vector3.one * 0.4f);
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs) b.Encapsulate(r.bounds);
        return b;
    }
}
