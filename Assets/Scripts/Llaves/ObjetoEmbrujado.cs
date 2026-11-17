using System.Collections;
using UnityEngine;

/// <summary>
/// Algo que se mueve solo cuando te acercas: una silla que se cae, una lámpara que se mece,
/// una banca que se arrastra, una lápida que se ladea, una puerta que rechina...
/// Por defecto se activa cuando NO lo estás viendo: oyes el ruido, volteas y ya se movió.
/// </summary>
public class ObjetoEmbrujado : MonoBehaviour
{
    public enum Movimiento { Caer, Mecer, Deslizar, Girar, Temblar, Ladear }
    public Movimiento movimiento = Movimiento.Caer;
    public float radio = 9f;
    [Tooltip("Solo se mueve si el jugador NO lo está viendo")]
    public bool soloSinVerlo = true;
    [Tooltip("Segundos para volver a poder moverse (0 = una sola vez)")]
    public float rearmar = 0f;
    public Vector3 direccion = Vector3.forward;

    Transform jugador;
    bool listo = true;
    Vector3 pos0; Quaternion rot0;

    void Start() { pos0 = transform.localPosition; rot0 = transform.localRotation; }

    void Update()
    {
        if (!listo || !HorrorGameState.PuedeJugar) return;
        if (jugador == null) { PlayerMovement pm = FindAnyObjectByType<PlayerMovement>(); if (pm) jugador = pm.transform; return; }
        if (Vector3.Distance(jugador.position, transform.position) > radio) return;
        bool loVe = Mundo.Visible(transform.position + Vector3.up * 0.3f, 0.1f, false);
        if (soloSinVerlo == loVe) return;
        StartCoroutine(Mover());
    }

    IEnumerator Mover()
    {
        listo = false;
        AudioClip sonido = movimiento == Movimiento.Caer || movimiento == Movimiento.Ladear ? ProceduralAudio.GolpeGrave() : ProceduralAudio.Rechinido(Random.Range(0, 99));
        AudioSource.PlayClipAtPoint(sonido, transform.position, 1f);
        Vector3 dir = transform.parent != null ? transform.parent.InverseTransformDirection(transform.TransformDirection(direccion.normalized)) : direccion.normalized;

        switch (movimiento)
        {
            case Movimiento.Caer:
                yield return Rotar(Quaternion.AngleAxis(88f, Vector3.Cross(Vector3.up, dir)) * rot0, 0.35f, true);
                break;
            case Movimiento.Ladear:
                yield return Rotar(Quaternion.AngleAxis(18f, Vector3.Cross(Vector3.up, dir)) * rot0, 0.6f, false);
                break;
            case Movimiento.Deslizar:
                for (float t = 0; t < 0.8f; t += Time.deltaTime) { transform.localPosition = Vector3.Lerp(pos0, pos0 + dir * 1.2f, Mathf.SmoothStep(0, 1, t / 0.8f)); yield return null; }
                break;
            case Movimiento.Girar:
                yield return Rotar(rot0 * Quaternion.Euler(0, 140f, 0), 1.2f, false);
                break;
            case Movimiento.Mecer:
                for (float t = 0; t < 6f; t += Time.deltaTime)
                {
                    transform.localRotation = rot0 * Quaternion.AngleAxis(Mathf.Sin(t * 3f) * 25f * (1f - t / 6f), dir);
                    yield return null;
                }
                transform.localRotation = rot0;
                break;
            case Movimiento.Temblar:
                for (float t = 0; t < 1.5f; t += Time.deltaTime)
                {
                    transform.localPosition = pos0 + Random.insideUnitSphere * 0.04f;
                    yield return null;
                }
                transform.localPosition = pos0;
                break;
        }

        if (rearmar > 0f)
        {
            yield return new WaitForSeconds(rearmar);
            transform.localPosition = pos0; transform.localRotation = rot0;
            listo = true;
        }
    }

    IEnumerator Rotar(Quaternion hasta, float segundos, bool acelerar)
    {
        Quaternion desde = transform.localRotation;
        for (float t = 0; t < segundos; t += Time.deltaTime)
        {
            float k = t / segundos;
            transform.localRotation = Quaternion.Slerp(desde, hasta, acelerar ? k * k : Mathf.SmoothStep(0, 1, k));
            yield return null;
        }
        transform.localRotation = hasta;
        if (acelerar) AudioSource.PlayClipAtPoint(ProceduralAudio.GolpeGrave(), transform.position, 0.8f);
    }
}
