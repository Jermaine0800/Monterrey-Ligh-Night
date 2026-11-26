using UnityEngine;

/// <summary>
/// Pala, tubo, crucifijo, hacha, machete...: un arma con usos limitados.
/// Se guarda en el maletín y se equipa sola si traes las manos vacías. Cambia de arma con ◀ ▶ / Q R.
/// </summary>
public class ArmaRecogible : Recogible
{
    public float danio = 60f;
    public float alcance = 1.6f;
    public int usos = 6;
    [Tooltip("Modelo que se pone en la mano (este mismo objeto si está vacío)")]
    public GameObject modeloMano;

    protected override bool PuedeTomar(PlayerInteraction jugador)
    {
        PlayerInventory inv = jugador.GetComponent<PlayerInventory>() ?? InventarioUtil.Inventario;
        if (InventarioUtil.TieneMochila && inv != null && inv.objetos.Count >= inv.capacidad)
        {
            DialogoUI.Susurrar("", "El maletín está lleno.", 2.5f);
            return false;
        }
        return true;
    }

    protected override void AlTomar(PlayerInteraction jugador)
    {
        PlayerCombate c = jugador.GetComponent<PlayerCombate>();
        InventarioUtil.RegistrarVisual(nombre, modeloMano != null ? modeloMano : gameObject);
        string icono = nombre.Contains("Pala") ? "Pala" : nombre.Contains("Crucifijo") ? "Crucifijo" : nombre.Contains("Hacha") ? "Hacha" : nombre.Contains("Machete") ? "Machete" : "Tubo";
        ItemDefinition it = InventarioUtil.Crear(nombre, ItemDefinition.Tipo.Arma, icono,
            "Daño " + Mathf.RoundToInt(danio) + "  ·  alcance " + alcance.ToString("0.0") + " m. Se rompe después de unos golpes.", danio, alcance, usos);

        PlayerInventory inv = jugador.GetComponent<PlayerInventory>() ?? InventarioUtil.Inventario;
        if (InventarioUtil.TieneMochila && inv != null) inv.Agregar(it);
        if (c != null && (!c.TieneArma || !InventarioUtil.TieneMochila)) c.Equipar(it);

        AudioSource.PlayClipAtPoint(ProceduralAudio.GolpeGrave(), transform.position, 0.4f);
        DialogoUI.Susurrar("", nombre + ". Con esto le puedo pegar en serio.  [RT]   ·   cambiar arma [◀ ▶ / Q R]", 4f);
    }
}
