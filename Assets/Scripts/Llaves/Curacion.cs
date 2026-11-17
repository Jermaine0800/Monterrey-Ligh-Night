using UnityEngine;

/// <summary>Botiquín, vendas, agua: te cura.</summary>
public class Curacion : Recogible
{
    public float cantidad = 40f;

    protected override bool PuedeTomar(PlayerInteraction jugador)
    {
        SaludJugador s = jugador.GetComponent<SaludJugador>();
        if (s != null && s.Vida01 >= 0.999f) { DialogoUI.Susurrar("", "Estoy bien... por ahora. Mejor lo guardo para después.", 2.5f); return false; }
        return true;
    }

    protected override void AlTomar(PlayerInteraction jugador)
    {
        SaludJugador s = jugador.GetComponent<SaludJugador>();
        if (s != null) { s.Curar(cantidad); s.CalmarPulso(); }
        AudioSource.PlayClipAtPoint(ProceduralAudio.Respiracion(5), transform.position, 0.5f);
        DialogoUI.Susurrar("", nombre + ": +" + Mathf.RoundToInt(cantidad) + " de vida.", 2.5f);
    }
}
