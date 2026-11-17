using System.Collections;
using UnityEngine;

/// <summary>
/// LLAVE 4 — LA CAPILLA abandonada. La llave está en el altar. Al tomarla las puertas se azotan,
/// la campana suena sola, se apagan las velas y algo entra corriendo.
/// Escóndete en el confesionario hasta que se vaya.
/// </summary>
public class SitioIglesia : SitioLlave
{
    public PuertaBisagra puerta;
    public CampanaIglesia campana;
    public Light[] velas;
    public Transform entradaMonstruo;

    public override string[] LineasPartida => new[]
    {
        "Senalar|La capilla. Ahí la gente venía a pedir que la noche se acabara. No funcionó.",
        "Hablar|La cuarta llave está en el altar. Tómala y sal. Rápido. O no. Tú sabrás.",
    };

    public override string[] LineasLlegada => new[]
    {
        "Sentado|¿Ves el confesionario? Recuérdalo. Lo vas a necesitar en... unos tres minutos.",
    };

    public override string ObjetivoLlegar => "Llave 4: la capilla abandonada";

    protected override IEnumerator Reto(MisionManager h)
    {
        h.Objetivo("Toma la llave del altar  [RB]");
        yield break;
    }

    protected override IEnumerator AlTomarLlave(MisionManager h)
    {
        yield return new WaitForSeconds(0.6f);
        if (puerta != null) puerta.Cerrar(true);
        if (campana != null) campana.Tocar(4);
        if (velas != null) foreach (Light l in velas) if (l != null) l.enabled = false;
        VHSOverlay.Glitch(1f, 1f);
        HorrorInput.Vibrar(0.8f, 0.4f, 0.6f, this);
        DialogoUI.Susurrar("", "¡La puerta! ...algo viene. ESCÓNDETE.", 4f);
        h.Objetivo("Escóndete  [RB en el confesionario]");

        yield return new WaitForSeconds(3f);
        Vector3 desde = entradaMonstruo != null ? entradaMonstruo.position : transform.position;
        SoltarMonstruo(desde);

        // Espera a que el monstruo se vaya (porque te escondiste, porque te asustó o porque lo golpeaste)
        float t = 0f;
        while (t < 30f && AcechoDirector.Instancia != null && AcechoDirector.Instancia.HayMonstruoActivo) { t += Time.deltaTime; yield return null; }
        yield return new WaitForSeconds(1.5f);
        if (puerta != null) puerta.Abrir();
        if (velas != null) foreach (Light l in velas) if (l != null) l.enabled = true;
        DialogoUI.Susurrar("", "La puerta se abrió otra vez. Como si nada.", 3f);
    }
}
