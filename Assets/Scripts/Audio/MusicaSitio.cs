using UnityEngine;

/// <summary>
/// Música propia de un lugar (3D): se empieza a oír lejos y sube poco a poco mientras te acercas.
/// Campamento: fogata + caja musical. Panteón: campanillas. Túnel: zumbido eléctrico.
/// Capilla: órgano. Mirador: coro.
/// </summary>
public class MusicaSitio : MonoBehaviour
{
    public enum Tipo { CajaMusical, Campanillas, Zumbido, Organo, Coro, Fogata }
    public Tipo tipo;
    public float volumen = 0.7f;
    public float distanciaMax = 70f;
    public float distanciaMin = 6f;

    void Start()
    {
        AudioSource s = gameObject.AddComponent<AudioSource>();
        switch (tipo)
        {
            case Tipo.CajaMusical: s.clip = SonidosTerror.CajaMusical(); break;
            case Tipo.Campanillas: s.clip = SonidosTerror.Campanillas(); break;
            case Tipo.Zumbido: s.clip = SonidosTerror.ZumbidoElectrico(); break;
            case Tipo.Organo: s.clip = SonidosTerror.Organo(); break;
            case Tipo.Coro: s.clip = SonidosTerror.Coro(); break;
            case Tipo.Fogata: s.clip = SonidosTerror.Fogata(); break;
        }
        s.loop = true;
        s.volume = volumen;
        s.spatialBlend = 1f;
        s.rolloffMode = AudioRolloffMode.Linear;
        s.minDistance = distanciaMin;
        s.maxDistance = distanciaMax;
        s.dopplerLevel = 0f;
        s.time = Random.Range(0f, s.clip.length * 0.9f);
        s.Play();
    }
}
