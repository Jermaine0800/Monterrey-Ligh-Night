using System.Collections;
using UnityEngine;

/// <summary>Puerta que te lleva a otro punto (de afuera del túnel al andén y de regreso).</summary>
[RequireComponent(typeof(Collider))]
public class PuertaTeleport : MonoBehaviour, IInteractable
{
    public Transform destino;
    public string mensaje = "";

    bool usando;

    public void Interact(PlayerInteraction jugador)
    {
        if (usando || destino == null) return;
        PlayerMovement m = jugador.GetComponent<PlayerMovement>();
        if (m == null) return;
        StartCoroutine(Cruzar(m));
    }

    IEnumerator Cruzar(PlayerMovement m)
    {
        usando = true;
        AudioSource.PlayClipAtPoint(ProceduralAudio.Rechinido(Random.Range(0, 99)), transform.position, 1f);
        VHSOverlay.Glitch(0.9f, 0.7f);
        yield return new WaitForSeconds(0.3f);
        m.Teletransportar(destino.position, destino.rotation);
        if (!string.IsNullOrEmpty(mensaje)) DialogoUI.Susurrar("", mensaje, 3f);
        yield return new WaitForSeconds(1f);
        usando = false;
    }
}
