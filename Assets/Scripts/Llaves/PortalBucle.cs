using System.Collections;
using UnityEngine;

/// <summary>
/// El portal que se abre en la puerta del bucle al juntar las 5 llaves:
/// un remolino de luz fría que crece, partículas que lo rodean, las 5 llaves girando alrededor,
/// un pilar de luz que se ve desde lejos y un zumbido que sube sin parar (tono de Shepard).
/// </summary>
public class PortalBucle : MonoBehaviour
{
    public Transform remolino;      // quad con la textura del remolino
    public Transform remolino2;     // segunda capa, gira al revés
    public Light luz;
    public Light pilar;             // luz alta para que se vea desde lejos
    public Transform[] llaves;      // las 5 llaves que orbitan

    float inicio;
    AudioSource zumbido, viento;

    void OnEnable()
    {
        inicio = Time.time;
        if (remolino != null) remolino.localScale = Vector3.zero;
        if (remolino2 != null) remolino2.localScale = Vector3.zero;
        StartCoroutine(Abrir());
    }

    IEnumerator Abrir()
    {
        zumbido = gameObject.AddComponent<AudioSource>();
        zumbido.clip = SonidosTerror.Shepard(); zumbido.loop = true; zumbido.spatialBlend = 1f;
        zumbido.minDistance = 3f; zumbido.maxDistance = 60f; zumbido.volume = 0f; zumbido.Play();
        viento = gameObject.AddComponent<AudioSource>();
        viento.clip = ProceduralAudio.Viento(9); viento.loop = true; viento.spatialBlend = 1f;
        viento.minDistance = 2f; viento.maxDistance = 30f; viento.volume = 0f; viento.Play();

        AudioSource.PlayClipAtPoint(ProceduralAudio.GolpeGrave(), transform.position, 1f);
        VHSOverlay.Glitch(1f, 1.5f);
        HorrorInput.Vibrar(0.6f, 0.6f, 1.2f, this);

        for (float t = 0; t < 3.5f; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / 3.5f);
            if (remolino != null) remolino.localScale = Vector3.one * 2.4f * k;
            if (remolino2 != null) remolino2.localScale = Vector3.one * 2.0f * k;
            zumbido.volume = k * 0.5f; viento.volume = k * 0.6f;
            yield return null;
        }
    }

    void Update()
    {
        float t = Time.time - inicio;
        if (remolino != null) remolino.Rotate(0, 0, -70f * Time.deltaTime, Space.Self);
        if (remolino2 != null) remolino2.Rotate(0, 0, 110f * Time.deltaTime, Space.Self);
        if (luz != null) luz.intensity = 3.5f + Mathf.Sin(t * 3f) * 1.2f + Random.Range(-0.3f, 0.3f);
        if (pilar != null) pilar.intensity = 6f + Mathf.Sin(t * 1.3f) * 2f;
        if (llaves != null)
            for (int i = 0; i < llaves.Length; i++)
            {
                if (llaves[i] == null) continue;
                float a = t * 0.9f + i * Mathf.PI * 2f / llaves.Length;
                llaves[i].localPosition = new Vector3(Mathf.Cos(a) * 1.7f, 1.4f + Mathf.Sin(t * 2f + i) * 0.25f, Mathf.Sin(a) * 0.4f);
                llaves[i].Rotate(0, 120f * Time.deltaTime, 0, Space.World);
            }
    }
}
