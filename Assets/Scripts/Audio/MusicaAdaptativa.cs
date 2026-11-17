using UnityEngine;

/// <summary>
/// Música que crece poco a poco según lo que pasa (nunca de golpe):
/// - Capa base (cuerdas disonantes): sube con cada llave que consigues.
/// - Infrasonido: sube cuando un monstruo está activo (aunque no lo veas).
/// - Tono de Shepard: entra cuando el monstruo está cerca (la tensión "sube" sin parar).
/// - Silencio: justo antes de un susto, todo se calla un par de segundos (lo más inquietante es el silencio).
/// La música propia de cada lugar la ponen los <see cref="MusicaSitio"/> (3D, se oye al acercarte).
/// </summary>
public class MusicaAdaptativa : MonoBehaviour
{
    public static MusicaAdaptativa Instancia { get; private set; }

    [Range(0, 1)] public float volumenGeneral = 0.8f;

    AudioSource base_, infra, shepard;
    float silencioHasta;
    Transform jugador;

    void Awake() { Instancia = this; }

    void Start()
    {
        base_ = Capa(SonidosTerror.CuerdasDisonantes());
        infra = Capa(SonidosTerror.Infrasonido());
        shepard = Capa(SonidosTerror.Shepard());
    }

    AudioSource Capa(AudioClip c)
    {
        AudioSource s = gameObject.AddComponent<AudioSource>();
        s.clip = c; s.loop = true; s.volume = 0f; s.spatialBlend = 0f; s.Play();
        return s;
    }

    /// <summary>Calla la música unos segundos (antes de un susto).</summary>
    public void Silencio(float segundos) { silencioHasta = Time.time + segundos; }

    void Update()
    {
        if (jugador == null) { PlayerMovement pm = FindAnyObjectByType<PlayerMovement>(); if (pm) jugador = pm.transform; }
        bool jugando = HorrorGameState.PuedeJugar || HorrorGameState.modo == HorrorGameState.Modo.Dialogo;

        int llaves = LlavesBucle.Instancia != null ? LlavesBucle.Instancia.Llaves : 0;
        float objBase = jugando ? 0.04f + llaves * 0.035f : 0f;

        float cerca = 999f; bool activo = false;
        AcechoDirector dir = AcechoDirector.Instancia;
        if (dir != null && jugador != null)
            foreach (EnemigoBase e in FindObjectsByType<EnemigoBase>(FindObjectsInactive.Exclude))
                if (e.Activo) { activo = true; cerca = Mathf.Min(cerca, Vector3.Distance(e.transform.position, jugador.position)); }

        float objInfra = activo ? 0.35f : 0.05f;
        float objShep = activo ? Mathf.InverseLerp(30f, 4f, cerca) * 0.25f : 0f;

        if (Time.time < silencioHasta) objBase = objInfra = objShep = 0f;

        float v = volumenGeneral, dt = Time.deltaTime;
        base_.volume = Mathf.MoveTowards(base_.volume, objBase * v, dt * 0.03f);   // muy gradual
        infra.volume = Mathf.MoveTowards(infra.volume, objInfra * v, dt * (Time.time < silencioHasta ? 2f : 0.08f));
        shepard.volume = Mathf.MoveTowards(shepard.volume, objShep * v, dt * (Time.time < silencioHasta ? 2f : 0.15f));
    }
}
