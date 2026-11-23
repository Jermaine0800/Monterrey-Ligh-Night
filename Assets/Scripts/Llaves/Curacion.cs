using UnityEngine;

/// <summary>
/// Botiquín, vendas, agua. Se guarda en el maletín y se usa desde ahí (o con ▲ / H para curarte rápido).
/// Sin mochila se usa en el momento.
/// </summary>
public class Curacion : Recogible
{
    public float cantidad = 40f;

    string Icono => nombre.ToLower().Contains("venda") ? "Vendas" : nombre.ToLower().Contains("agua") ? "Agua" : "Botiquin";

    protected override bool PuedeTomar(PlayerInteraction jugador)
    {
        PlayerInventory inv = jugador.GetComponent<PlayerInventory>() ?? InventarioUtil.Inventario;
        if (InventarioUtil.TieneMochila && inv != null && inv.objetos.Count >= inv.capacidad)
        {
            DialogoUI.Susurrar("", "El maletín está lleno.", 2.5f);
            return false;
        }
        SaludJugador s = jugador.GetComponent<SaludJugador>();
        if (!InventarioUtil.TieneMochila && s != null && s.Vida01 >= 0.999f)
        {
            DialogoUI.Susurrar("", "Estoy bien. Si tuviera mochila lo guardaba.", 2.5f);
            return false;
        }
        return true;
    }

    protected override void AlTomar(PlayerInteraction jugador)
    {
        PlayerInventory inv = jugador.GetComponent<PlayerInventory>() ?? InventarioUtil.Inventario;
        if (InventarioUtil.TieneMochila && inv != null)
        {
            inv.Agregar(InventarioUtil.Crear(nombre, ItemDefinition.Tipo.Curacion, Icono,
                "Recupera " + Mathf.RoundToInt(cantidad) + " de vida. Úsalo desde el maletín o con [▲ / H].", cantidad));
            DialogoUI.Susurrar("", nombre + " guardado en el maletín  [Y]", 2.5f);
            AudioSource.PlayClipAtPoint(ProceduralAudio.ClicMenu(), transform.position, 0.8f);
            return;
        }
        Usar(jugador.gameObject, cantidad);
        DialogoUI.Susurrar("", nombre + ": +" + Mathf.RoundToInt(cantidad) + " de vida.", 2.5f);
    }

    public static void Usar(GameObject jugador, float cantidad)
    {
        SaludJugador s = jugador.GetComponent<SaludJugador>();
        if (s != null) { s.Curar(cantidad); s.CalmarPulso(); }
        AudioSource.PlayClipAtPoint(ProceduralAudio.Respiracion(5), jugador.transform.position, 0.5f);
    }
}
