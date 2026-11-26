using UnityEngine;

/// <summary>
/// Al cargar la escena: los objetos que se pueden tomar o leer (notas, armas, curaciones, linterna, mochila)
/// que quedaron flotando en el aire se bajan a la superficie que tengan debajo (suelo, mesa, cama...).
/// No mueve nada que ya esté apoyado ni lo que esté a más de 3 m de la superficie (eso es a propósito).
/// </summary>
public static class AsentarObjetos
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Iniciar()
    {
        int n = 0;
        foreach (MonoBehaviour mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (!(mb is NotaLeible || mb is Recogible || mb is FlashlightItem || mb is BackpackPickup)) continue;
            if (Asentar(mb.transform)) n++;
        }
        if (n > 0) Debug.Log("[Física] Objetos asentados sobre su superficie: " + n);

        // Las casas de campaña tenían el choque del modelo completo (con la entrada "cerrada"): no se podía entrar.
        int tiendas = 0;
        foreach (MeshCollider mc in Object.FindObjectsByType<MeshCollider>(FindObjectsInactive.Exclude))
            if (mc.gameObject.name.StartsWith("Tent") || (mc.transform.parent != null && mc.transform.parent.name.StartsWith("Tent")))
            { Object.Destroy(mc); tiendas++; }
        if (tiendas > 0) Debug.Log("[Física] Casas de campaña abiertas: " + tiendas);
    }

    static bool Asentar(Transform t)
    {
        Renderer[] rs = t.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return false;
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs) b.Encapsulate(r.bounds);

        Collider[] propios = t.GetComponentsInChildren<Collider>();
        Vector3 desde = new Vector3(b.center.x, b.min.y + 0.05f, b.center.z);
        RaycastHit[] golpes = Physics.RaycastAll(desde, Vector3.down, 3f, ~0, QueryTriggerInteraction.Ignore);
        float mejor = float.MaxValue; RaycastHit h = default;
        foreach (RaycastHit g in golpes)
        {
            bool esPropio = false;
            foreach (Collider c in propios) if (g.collider == c) { esPropio = true; break; }
            if (esPropio || g.collider.GetComponentInParent<CharacterController>() != null) continue;
            if (g.distance < mejor) { mejor = g.distance; h = g; }
        }
        if (mejor == float.MaxValue) return false;
        float hueco = (b.min.y) - h.point.y;
        if (hueco < 0.08f) return false;              // ya está apoyado
        if (hueco > 1.0f) return false;               // colgada en pared o tablero: se queda donde está
        t.position -= Vector3.up * (hueco - 0.01f);
        return true;
    }
}
