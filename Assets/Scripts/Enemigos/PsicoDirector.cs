using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Terror psicológico: cosas pequeñas que hacen dudar al jugador de lo que vio u oyó.
/// Usa SOLO a los dos monstruos (La Sonriente y El Asomado) para las apariciones:
/// - Vistazo periférico: aparece en la orilla de la vista, entre los árboles; cuando volteas ya no está.
/// - Detrás de ti: si te quedas quieto mucho tiempo, aparece a tu espalda; si volteas rápido, alcanzas a verlo irse.
/// - Llamado lejano: alguien dice "¿hola?" desde el bosque.
/// - Tu nombre en la cinta: el contador de llaves marca "6/5" un instante.
/// - Silencio antes del golpe: la música se calla justo antes de un susto.
/// Nunca dos cosas seguidas: deja respirar para que el siguiente susto pegue más.
/// </summary>
public class PsicoDirector : MonoBehaviour
{
    public bool habilitado = true;
    public float esperaMin = 35f, esperaMax = 80f;

    Transform jugador;
    PlayerMovement mov;
    float siguiente, quietoDesde;
    Vector3 ultimaPos;
    AudioSource fuente;
    readonly List<EnemigoBase> monstruos = new List<EnemigoBase>();

    void Start()
    {
        siguiente = Time.time + 25f;   // poca calma: a los 25 s empieza lo raro
        esperaMin = Mathf.Min(esperaMin, 18f); esperaMax = Mathf.Min(esperaMax, 40f);
        monstruos.AddRange(FindObjectsByType<EnemigoBase>(FindObjectsInactive.Include));
        GameObject g = new GameObject("FuentePsico");
        g.transform.SetParent(transform);
        fuente = g.AddComponent<AudioSource>();
        fuente.spatialBlend = 1f; fuente.minDistance = 4f; fuente.maxDistance = 60f; fuente.rolloffMode = AudioRolloffMode.Linear;
    }

    void Update()
    {
        if (jugador == null) { mov = FindAnyObjectByType<PlayerMovement>(); if (mov) { jugador = mov.transform; ultimaPos = jugador.position; } return; }
        if (!habilitado || !HorrorGameState.PuedeJugar || DialogoUI.Activo) return;
        if (AcechoDirector.Instancia != null && (!AcechoDirector.Instancia.habilitado || AcechoDirector.Instancia.HayMonstruoActivo)) return;
        if (jugador.position.y < Terrain.activeTerrain.transform.position.y - 20f) return; // dentro del túnel

        if ((jugador.position - ultimaPos).sqrMagnitude > 0.04f) { ultimaPos = jugador.position; quietoDesde = Time.time; }

        if (Time.time - quietoDesde > 25f && Time.time > siguiente - 20f)
        {
            quietoDesde = Time.time;
            StartCoroutine(DetrasDeTi());
            siguiente = Time.time + Random.Range(esperaMin, esperaMax);
            return;
        }

        if (Time.time < siguiente) return;
        siguiente = Time.time + Random.Range(esperaMin, esperaMax);
        float r = Random.value;
        if (r < 0.45f) StartCoroutine(VistazoPeriferico());
        else if (r < 0.7f) Llamado();
        else if (r < 0.85f) StartCoroutine(ContadorFalso());
        else StartCoroutine(PasosQueSeDetienen());
    }

    EnemigoBase Libre()
    {
        List<EnemigoBase> libres = monstruos.FindAll(m => m != null && m.EstadoActual == EnemigoBase.Estado.Inactivo);
        return libres.Count > 0 ? libres[Random.Range(0, libres.Count)] : null;
    }

    IEnumerator VistazoPeriferico()
    {
        EnemigoBase m = Libre();
        Camera cam = Camera.main;
        if (m == null || cam == null) yield break;
        // un punto en la orilla de la vista, entre 14 y 24 m
        float lado = Random.value < 0.5f ? -1f : 1f;
        Vector3 dir = Quaternion.Euler(0, lado * Random.Range(32f, 42f), 0) * Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
        List<Vector3> arboles = Mundo.ArbolesCerca(jugador.position + dir * 19f, 0f, 7f, 10);
        Vector3 p = arboles.Count > 0 ? arboles[0] + Vector3.Cross(Vector3.up, dir) * 0.6f * lado : jugador.position + dir * 19f;
        yield return m.Vistazo(p, 2.5f);
    }

    IEnumerator DetrasDeTi()
    {
        EnemigoBase m = Libre();
        Camera cam = Camera.main;
        if (m == null || cam == null) yield break;
        if (MusicaAdaptativa.Instancia != null) MusicaAdaptativa.Instancia.Silencio(3f);
        Vector3 atras = -Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
        fuente.transform.position = jugador.position + atras * 3f;
        fuente.PlayOneShot(ProceduralAudio.Respiracion(41), 0.6f);
        yield return m.Vistazo(jugador.position + atras * 3.5f, 4f);
    }

    void Llamado()
    {
        Vector2 r = Random.insideUnitCircle.normalized * Random.Range(25f, 40f);
        fuente.transform.position = jugador.position + new Vector3(r.x, 1.5f, r.y);
        fuente.pitch = Random.Range(0.85f, 1.05f);
        fuente.PlayOneShot(SonidosTerror.Llamado(Random.Range(0, 999)), 0.9f);
    }

    IEnumerator PasosQueSeDetienen()
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;
        Vector3 atras = -Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
        for (int i = 0; i < 6; i++)
        {
            fuente.transform.position = jugador.position + atras * (7f - i * 0.6f);
            fuente.PlayOneShot(ProceduralAudio.Paso(800 + i), 1f);
            yield return new WaitForSeconds(0.5f - i * 0.03f);
        }
        // ...y se detienen justo detrás.
    }

    /// <summary>El contador de llaves marca una llave de más por un instante.</summary>
    IEnumerator ContadorFalso()
    {
        GameObject g = GameObject.Find("Llaves");
        Text t = g != null ? g.GetComponent<Text>() : null;
        if (t == null) yield break;
        string real = t.text;
        int n = LlavesBucle.Instancia != null ? LlavesBucle.Instancia.Llaves : 0;
        t.text = real.Replace(n + "/5", (n + 1) + "/5");
        t.color = HorrorUI.Sangre;
        VHSOverlay.Glitch(0.3f, 0.2f);
        yield return new WaitForSeconds(0.35f);
        t.text = real;
        t.color = HorrorUI.Hueso;
    }
}
