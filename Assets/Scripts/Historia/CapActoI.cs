using System.Collections;
using UnityEngine;

/// <summary>
/// ACTO I — EL CLARO. Despiertas junto a una linterna que parpadea. El Vigía se presenta,
/// te manda por la mochila y en el sendero "el Guía" te saluda rígido entre los árboles
/// y te señala la supuesta salida.
/// </summary>
public class CapActoI : CapituloBase
{
    public override string Titulo => "Acto I: el claro";

    public override IEnumerator Jugar(MisionManager h)
    {
        // 1) La linterna en el suelo
        h.Objetivo("Toma la linterna  [RB]");
        yield return h.EsperarHasta(() => h.Linterna.TieneLinterna);
        if (h.linternaEnSuelo != null)
        {
            Transform luz = h.linternaEnSuelo.Find("LuzParpadeante");
            if (luz != null) Object.Destroy(luz.gameObject);
        }
        h.Objetivo("Préndela  [X]");
        float t = 0f;
        while (!h.Linterna.Encendida && t < 12f) { t += Time.deltaTime; yield return null; }

        // 2) El Vigía se presenta
        yield return new WaitForSeconds(2f);
        yield return h.EscenaVigia(h.JuntoAArbol(h.Jugador.position, 7f, 16f), GuiaController.Pose.Sentado, new[]
        {
            "Sentado|Vaya, vaya. Despertaste. Tus amigos no tuvieron tanta suerte. Bueno, no sé. No los he contado.",
            "Hablar|27 de noviembre de 1998. Sector La Pastora. Bonita fecha para perderse, ¿no crees?",
            "Senalar|Soy el Vigía. Yo cuido este lado de la Silla. Tú nada más vas de paso. Eso espero.",
            "Hablar|Esa mochila de por ahí era de uno de ellos. Ya no la va a necesitar. Tómala.",
            "Reir|Y si alguien te saluda entre los árboles... no le regreses el saludo. Es de mala educación. Para ti.",
        });

        // 3) La mochila (si existe en la escena)
        if (h.InventarioUI != null && !h.InventarioUI.hasBackPack && Object.FindAnyObjectByType<BackpackPickup>() != null)
        {
            h.Objetivo("Recoge la mochila  [RB]");
            yield return h.EsperarHasta(() => h.InventarioUI.hasBackPack, true, () => h.puntoSendero != null ? h.puntoSendero.position : h.Jugador.position);
        }

        // 4) El sendero
        if (h.puntoSendero != null)
        {
            h.Objetivo("Sigue el sendero  (la luz fría entre los árboles)");
            yield return h.EsperarHasta(() => h.Distancia(h.puntoSendero.position) < 7f);
        }

        // 5) El saludo rígido
        yield return Saludo(h);

        // A partir de aquí, algo te sigue
        if (h.Director != null) h.Director.Habilitar(true);
        h.Objetivo("¿La salida? Hay algo de concreto entre la maleza...");
    }

    IEnumerator Saludo(MisionManager h)
    {
        GuiaController g = h.guia;
        Vector3 frente = h.Adelante(14f, 10f);
        Vector3 p = h.JuntoAArbol(frente, 0f, 6f);
        g.Aparecer(p, h.Jugador.position, GuiaController.Pose.Saludar);
        float velocidad = g.animator != null ? g.animator.speed : 1f;
        if (g.animator != null) g.animator.speed = 0.55f;

        AudioSource s = g.GetComponent<AudioSource>();
        AudioClip drone = ProceduralAudio.Drone(31);
        if (s != null) s.PlayOneShot(drone, 0.5f);

        // 6 segundos saludando. Rígido. Sin decir nada.
        for (float t = 0; t < 6f; t += 2.2f)
        {
            g.Animar(GuiaController.Pose.Saludar, 0.02f);
            VHSOverlay.Glitch(0.15f, 0.1f);
            yield return new WaitForSeconds(2.2f);
        }

        // Señala hacia el acantilado (el mirador) y se deshace en estática
        Vector3 haciaMirador = (h.mirador != null ? h.mirador.position : g.transform.position + g.transform.forward * 50f) - g.transform.position;
        haciaMirador.y = 0;
        g.congelarGiro = true;
        g.transform.rotation = Quaternion.LookRotation(haciaMirador);
        g.Animar(GuiaController.Pose.Senalar, 0.05f);
        DialogoUI.Susurrar("", "Por aquí es la salida...", 4f);
        yield return new WaitForSeconds(1.6f);
        VHSOverlay.Glitch(0.9f, 0.8f);
        g.Desaparecer();
        g.congelarGiro = false;
        if (g.animator != null) g.animator.speed = velocidad;
    }
}
