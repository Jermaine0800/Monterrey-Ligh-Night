using System.Collections;
using UnityEngine;

/// <summary>
/// LLAVE 4 — LA CASA DE LOS GARZA: una casa abandonada con cuartos, pasillo y patio.
/// La llave está en el cuarto del fondo. Al tomarla la puerta de enfrente se azota,
/// se va la luz y algo entra. Escóndete en el ropero (o bajo la cama) hasta que se vaya.
/// </summary>
public class SitioCasa : SitioLlave
{
    public PuertaBisagra puerta;
    public Light[] focos;
    public Transform entradaMonstruo;

    public override string[] LineasPartida => new[]
    {
        "Senalar|La casa de los Garza. La familia se fue en el 98... o eso dicen. Dejaron la mesa puesta.",
        "Hablar|La cuarta llave está adentro, en algún cuarto. Revisa todo. Y fíjate dónde te puedes meter.",
    };

    public override string[] LineasLlegada => new[]
    {
        "Sentado|¿Ves el ropero del cuarto grande? Recuérdalo. Lo vas a necesitar.",
    };

    public override string VozAlLlegar => "capilla";

    public override string ObjetivoLlegar => "Llave 4: la casa abandonada";

    protected override IEnumerator Reto(MisionManager h)
    {
        h.Objetivo("Busca la llave dentro de la casa (revisa los cuartos)");
        yield break;
    }

    protected override IEnumerator AlTomarLlave(MisionManager h)
    {
        yield return new WaitForSeconds(0.6f);
        if (puerta != null) puerta.Cerrar(true);
        if (focos != null) foreach (Light l in focos) if (l != null) l.enabled = false;
        VHSOverlay.Glitch(1f, 1f);
        HorrorInput.Vibrar(0.8f, 0.4f, 0.6f, this);
        DialogoUI.Susurrar("", "¡La puerta! ...algo está adentro. ESCÓNDETE.", 4f);
        h.Objetivo("Escóndete  [A / E en el ropero o bajo la cama]");

        yield return new WaitForSeconds(3f);
        if (puerta != null) puerta.Abrir();
        Vector3 desde = entradaMonstruo != null ? entradaMonstruo.position : transform.position;
        SoltarMonstruo(desde);

        float t = 0f;
        while (t < 30f && AcechoDirector.Instancia != null && AcechoDirector.Instancia.HayMonstruoActivo) { t += Time.deltaTime; yield return null; }
        yield return new WaitForSeconds(1.5f);
        if (focos != null) foreach (Light l in focos) if (l != null) l.enabled = true;
        DialogoUI.Susurrar("", "Se fue. ...creo.", 3f);
    }
}
