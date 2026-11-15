using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Director del acecho: hace que SIENTAS que algo te sigue aunque no lo veas.
/// Va subiendo la tensión con pasos detrás de ti, ramas que truenan y respiraciones;
/// cuando la tensión llega al límite, suelta a uno de los monstruos.
/// La Historia lo habilita o lo apaga (no hay monstruos durante diálogos ni antes de tener la linterna).
/// </summary>
public class AcechoDirector : MonoBehaviour
{
    public static AcechoDirector Instancia { get; private set; }

    [Tooltip("Lo prende la historia cuando el jugador ya tiene la linterna")]
    public bool habilitado = false;

    [Header("Tensión")]
    public float tensionPorSegundo = 1f;
    public float tensionParaSoltarMonstruo = 60f;
    public float esperaTrasMonstruo = 35f;

    [Header("Señales")]
    public float cadaCuantoSenal = 9f;

    readonly List<EnemigoBase> enemigos = new List<EnemigoBase>();
    EnemigoBase actual;
    float tension, enfriamiento, siguienteSenal;
    Transform jugador;
    PlayerMovement mov;
    AudioSource fuente;
    AudioClip[] pasos, ramas;
    AudioClip respiracion;

    void Awake() { Instancia = this; }

    void Start()
    {
        foreach (EnemigoBase e in FindObjectsByType<EnemigoBase>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            enemigos.Add(e);
            e.AlTerminar += AlTerminarEnemigo;
        }

        GameObject g = new GameObject("FuentePresencia");
        g.transform.SetParent(transform);
        fuente = g.AddComponent<AudioSource>();
        fuente.spatialBlend = 1f; fuente.minDistance = 2f; fuente.maxDistance = 25f;
        fuente.rolloffMode = AudioRolloffMode.Linear;

        pasos = new AudioClip[4];
        for (int i = 0; i < pasos.Length; i++) pasos[i] = ProceduralAudio.Paso(500 + i);
        ramas = new[] { ProceduralAudio.Rama(1), ProceduralAudio.Rama(2), ProceduralAudio.Rama(3) };
        respiracion = ProceduralAudio.Respiracion(9);
    }

    public void Habilitar(bool v)
    {
        habilitado = v;
        if (!v && actual != null) { actual.Reiniciar(); actual = null; }
    }

    void Update()
    {
        if (jugador == null)
        {
            mov = FindFirstObjectByType<PlayerMovement>();
            if (mov == null) return;
            jugador = mov.transform;
        }
        if (!habilitado || !HorrorGameState.PuedeJugar || DialogoUI.Activo) return;
        if (actual != null) return;

        if (enfriamiento > 0f) { enfriamiento -= Time.deltaTime; return; }

        // Más tensión si estás a oscuras o corriendo (haces ruido)
        FlashlightController l = jugador.GetComponent<FlashlightController>();
        float factor = 1f;
        if (l == null || !l.Encendida) factor += 0.6f;
        factor += mov.NivelRuido * 0.5f;
        tension += tensionPorSegundo * factor * Time.deltaTime;

        if (Time.time > siguienteSenal && tension > tensionParaSoltarMonstruo * 0.25f)
        {
            siguienteSenal = Time.time + cadaCuantoSenal * Random.Range(0.6f, 1.4f);
            StartCoroutine(Senal());
        }

        if (tension >= tensionParaSoltarMonstruo) SoltarMonstruo();
    }

    /// <summary>Algo detrás de ti: pasos que imitan los tuyos, una rama, una respiración.</summary>
    IEnumerator Senal()
    {
        Transform cam = Camera.main != null ? Camera.main.transform : jugador;
        Vector3 atras = -cam.forward; atras.y = 0; atras.Normalize();
        Vector3 lado = Vector3.Cross(Vector3.up, atras) * Random.Range(-3f, 3f);
        float r = Random.value;

        if (r < 0.45f)
        {
            // Pasos que se detienen cuando tú te detienes
            for (int i = 0; i < Random.Range(3, 6); i++)
            {
                fuente.transform.position = jugador.position + atras * Random.Range(6f, 10f) + lado;
                fuente.pitch = Random.Range(0.8f, 1f);
                fuente.PlayOneShot(pasos[Random.Range(0, pasos.Length)], 0.8f);
                yield return new WaitForSeconds(0.55f);
                if (mov.NivelRuido < 0.1f) break;
            }
        }
        else if (r < 0.8f)
        {
            fuente.transform.position = jugador.position + atras * Random.Range(8f, 16f) + lado * 2f;
            fuente.PlayOneShot(ramas[Random.Range(0, ramas.Length)], 1f);
        }
        else
        {
            fuente.transform.position = jugador.position + atras * 2.5f;
            fuente.PlayOneShot(respiracion, 0.45f);
            VHSOverlay.Glitch(0.2f, 0.4f);
        }
    }

    void SoltarMonstruo()
    {
        List<EnemigoBase> libres = enemigos.FindAll(e => e != null && e.EstadoActual == EnemigoBase.Estado.Inactivo);
        if (libres.Count == 0) { tension = tensionParaSoltarMonstruo * 0.5f; return; }
        actual = libres[Random.Range(0, libres.Count)];
        actual.Activar();
        tension = 0f;
    }

    void AlTerminarEnemigo(EnemigoBase e)
    {
        if (e != actual) return;
        actual = null;
        enfriamiento = esperaTrasMonstruo;
    }

    /// <summary>Para la historia: forzar una aparición ahora.</summary>
    public void ForzarAparicion() => tension = tensionParaSoltarMonstruo;
}
