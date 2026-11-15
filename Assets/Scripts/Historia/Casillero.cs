using UnityEngine;

/// <summary>Casillero del andén: batería de alto rendimiento + el documento clasificado.</summary>
[RequireComponent(typeof(Collider))]
public class Casillero : MonoBehaviour, IInteractable
{
    public Transform puerta;   // se abre girando
    public bool Abierto { get; private set; }

    public const string TituloDocumento = "EXPEDIENTE 08  —  CLASIFICADO";
    public const string CuerpoDocumento =
        "SISTEMA DE TRANSPORTE COLECTIVO METRORREY\n" +
        "PROYECTO LÍNEA K  (tramo La Pastora – Cerro de la Silla)  —  OBRA SUSPENDIDA\n" +
        "Monterrey, N.L., 14 de marzo de 1987\n\n" +
        "Durante la excavación del túnel, la cuadrilla 3 reportó fallas en radios y lámparas, " +
        "un zumbido grave constante y una figura alta parada sobre las vías, con \"la cabeza como un sol negro\".\n\n" +
        "Mediciones: campo electromagnético 40 veces por encima de lo normal. " +
        "Tejido orgánico de origen desconocido dentro de la roca.\n\n" +
        "Conclusión: la anomalía es electromagnética y biológica a la vez. Estaba dormida. " +
        "Las excavaciones la despertaron. Imita formas humanas para guiar a las personas cuesta arriba.\n\n" +
        "Designación interna:  VIGÍA.\n\n" +
        "Se ordena sellar los accesos y borrar la línea de los planos.\n" +
        "7 trabajadores no localizados.\n\n" +
        "NO BAJEN A LAS VÍAS.";

    public void Interact(PlayerInteraction jugador)
    {
        if (Abierto) return;
        Abierto = true;
        if (puerta != null) puerta.localRotation *= Quaternion.Euler(0, -105f, 0);
        AudioSource.PlayClipAtPoint(ProceduralAudio.Rechinido(12), transform.position, 1f);

        FlashlightController l = jugador.GetComponent<FlashlightController>();
        if (l == null) l = FindAnyObjectByType<FlashlightController>();
        if (l != null)
        {
            l.Recargar(1f);
            l.duracionBateria *= 1.6f;
        }
        DialogoUI.Susurrar("", "Batería de alto rendimiento. La linterna aguantará más.", 3.5f);
        StartCoroutine(DocumentoUI.Mostrar(TituloDocumento, CuerpoDocumento, 1.5f));
    }
}
