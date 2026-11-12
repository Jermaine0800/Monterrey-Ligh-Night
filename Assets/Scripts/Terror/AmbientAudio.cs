using System.Collections;
using UnityEngine;

/// <summary>
/// Paisaje sonoro del bosque de noche: viento, drone grave, estática leve
/// y sonidos aleatorios en 3D alrededor del jugador (rechinidos, toquidos, susurros).
/// Todo generado por código.
/// </summary>
public class AmbientAudio : MonoBehaviour
{
    [Header("Volúmenes")]
    [Range(0, 1)] public float volViento = 0.45f;
    [Range(0, 1)] public float volDrone = 0.25f;
    [Range(0, 1)] public float volEstatica = 0.03f;
    [Range(0, 1)] public float volEventos = 0.8f;

    [Header("Eventos aleatorios")]
    public float esperaMin = 18f;
    public float esperaMax = 45f;
    public float distanciaMin = 12f;
    public float distanciaMax = 30f;

    Transform jugador;
    AudioClip[] eventos;
    AudioSource srcEvento;

    void Start()
    {
        Loop(ProceduralAudio.Viento(), volViento);
        Loop(ProceduralAudio.Drone(), volDrone);
        Loop(ProceduralAudio.Estatica(), volEstatica);

        eventos = new AudioClip[]
        {
            ProceduralAudio.Rechinido(11), ProceduralAudio.Rechinido(23),
            ProceduralAudio.Toquidos(5),   ProceduralAudio.Toquidos(8),
            ProceduralAudio.Susurro(3),    ProceduralAudio.Susurro(6),
        };

        GameObject e = new GameObject("FuenteEvento3D");
        e.transform.SetParent(transform);
        srcEvento = e.AddComponent<AudioSource>();
        srcEvento.spatialBlend = 1f;
        srcEvento.rolloffMode = AudioRolloffMode.Linear;
        srcEvento.minDistance = 5f;
        srcEvento.maxDistance = 60f;
        srcEvento.dopplerLevel = 0f;

        StartCoroutine(Eventos());
    }

    void Loop(AudioClip c, float vol)
    {
        AudioSource s = gameObject.AddComponent<AudioSource>();
        s.clip = c; s.loop = true; s.volume = vol; s.spatialBlend = 0f;
        s.time = Random.Range(0f, c.length * 0.9f);
        s.Play();
    }

    IEnumerator Eventos()
    {
        yield return new WaitForSeconds(8f);
        while (true)
        {
            if (jugador == null)
            {
                GameObject p = GameObject.FindGameObjectWithTag("Player");
                if (p == null) { var pm = FindFirstObjectByType<PlayerMovement>(); if (pm) p = pm.gameObject; }
                if (p != null) jugador = p.transform;
            }

            if (jugador != null && HorrorGameState.PuedeJugar)
            {
                Vector2 dir = Random.insideUnitCircle.normalized * Random.Range(distanciaMin, distanciaMax);
                srcEvento.transform.position = jugador.position + new Vector3(dir.x, 1.5f, dir.y);
                srcEvento.pitch = Random.Range(0.8f, 1.1f);
                srcEvento.PlayOneShot(eventos[Random.Range(0, eventos.Length)], volEventos);

                // Un toque de interferencia en la cinta de vez en cuando
                if (Random.value < 0.35f) VHSOverlay.Glitch(0.35f, 0.25f);
            }
            yield return new WaitForSeconds(Random.Range(esperaMin, esperaMax));
        }
    }
}
