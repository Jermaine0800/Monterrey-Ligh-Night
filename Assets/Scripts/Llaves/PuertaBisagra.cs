using System.Collections;
using UnityEngine;

/// <summary>Puerta o reja que gira sobre su bisagra (este objeto es la bisagra). Abre/cierra con animación.</summary>
public class PuertaBisagra : MonoBehaviour
{
    public float anguloAbierta = -100f;
    public float duracion = 1.4f;
    public bool abiertaAlInicio = false;
    public bool Abierta { get; private set; }

    Quaternion cerrada;
    Coroutine mov;

    void Awake()
    {
        cerrada = transform.localRotation;
        if (abiertaAlInicio) { transform.localRotation = cerrada * Quaternion.Euler(0, anguloAbierta, 0); Abierta = true; }
    }

    public void Abrir() => Mover(true, false);
    public void Cerrar(bool azotar = false) => Mover(false, azotar);

    void Mover(bool abrir, bool azotar)
    {
        if (Abierta == abrir) return;
        Abierta = abrir;
        if (mov != null) StopCoroutine(mov);
        mov = StartCoroutine(Rutina(abrir, azotar));
    }

    IEnumerator Rutina(bool abrir, bool azotar)
    {
        AudioSource.PlayClipAtPoint(azotar ? ProceduralAudio.GolpeGrave() : ProceduralAudio.Rechinido(Random.Range(0, 50)), transform.position, 1f);
        Quaternion desde = transform.localRotation;
        Quaternion hasta = abrir ? cerrada * Quaternion.Euler(0, anguloAbierta, 0) : cerrada;
        float d = azotar ? 0.18f : duracion;
        for (float t = 0; t < d; t += Time.deltaTime)
        {
            float k = t / d;
            k = azotar ? k * k : Mathf.SmoothStep(0f, 1f, k);
            transform.localRotation = Quaternion.Slerp(desde, hasta, k);
            yield return null;
        }
        transform.localRotation = hasta;
        if (azotar) { VHSOverlay.Glitch(0.6f, 0.3f); HorrorInput.Vibrar(0.9f, 0.3f, 0.3f, this); }
    }
}
