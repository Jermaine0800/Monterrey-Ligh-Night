using System.Collections;
using UnityEngine;

/// <summary>
/// Trampa para oso escondida en el pasto. Si la pisas: CLACK, te quita vida y te deja cojeando unos segundos.
/// Con la linterna se alcanza a ver el brillo del metal.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TrampaOso : MonoBehaviour
{
    public float danio = 18f;
    public float segundosAtrapado = 2.5f;
    public Transform modelo;
    bool disparada;

    void OnTriggerEnter(Collider c)
    {
        if (disparada) return;
        PlayerMovement pm = c.GetComponentInParent<PlayerMovement>();
        if (pm == null) return;
        StartCoroutine(Cerrar(pm));
    }

    IEnumerator Cerrar(PlayerMovement pm)
    {
        disparada = true;
        AudioSource.PlayClipAtPoint(ProceduralAudio.GolpeGrave(), transform.position, 1f);
        AudioSource.PlayClipAtPoint(ProceduralAudio.Rechinido(66), transform.position, 1f);
        if (modelo != null) modelo.localScale = new Vector3(modelo.localScale.x * 0.6f, modelo.localScale.y * 1.6f, modelo.localScale.z);
        SaludJugador s = pm.GetComponent<SaludJugador>();
        if (s != null) s.RecibirDanio(danio, transform.position);
        VozProtagonista.Decir("herido", 5f);
        DialogoUI.Susurrar("", "¡Una trampa! ...me agarró el pie.", 3f);
        pm.enabled = false;
        yield return new WaitForSeconds(segundosAtrapado);
        pm.enabled = true;
    }
}
