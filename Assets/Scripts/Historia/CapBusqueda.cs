using System.Collections;
using UnityEngine;

/// <summary>
/// LA BÚSQUEDA — las 5 llaves. Recorre los lugares en orden; cada uno es un <see cref="SitioLlave"/>
/// con su propio reto (polimorfismo: el capítulo no sabe si es un panteón o un túnel).
/// </summary>
public class CapBusqueda : CapituloBase
{
    public override string Titulo => "Las 5 llaves";

    public override IEnumerator Jugar(MisionManager h)
    {
        if (h.sitios == null || h.sitios.Length == 0)
        {
            Debug.LogWarning("[Historia] No hay lugares de llaves en la escena (menú Monterrey/11).");
            yield break;
        }

        foreach (SitioLlave sitio in h.sitios)
        {
            if (sitio == null || sitio.Completado) continue;
            Debug.Log("[Historia] Lugar: " + sitio.nombreLugar);
            yield return h.CinematicaLugar(sitio.transform, sitio.nombreLugar);
            yield return sitio.Jugar(h);
            yield return new WaitForSeconds(2f);
        }
    }
}
