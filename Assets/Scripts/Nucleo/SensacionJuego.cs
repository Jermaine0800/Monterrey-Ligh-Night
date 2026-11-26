using System.Collections;
using UnityEngine;

/// <summary>
/// "Game feel" (lo que hace que un juego se sienta vivo, al estilo de los devlogs de itch.io y la charla
/// "The art of screenshake"): sacudida de cámara por trauma, pausa de impacto (hit-stop) al pegar
/// y sacudida cuando te lastiman o algo te asusta.
/// </summary>
[DefaultExecutionOrder(10000)]   // después de que la cámara se acomoda
public class SensacionJuego : MonoBehaviour
{
    static SensacionJuego inst;
    float trauma;
    float semilla;
    float vidaAnterior = -1f;
    bool enPausa;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear()
    {
        if (inst != null) return;
        GameObject g = new GameObject("SensacionJuego");
        DontDestroyOnLoad(g);
        inst = g.AddComponent<SensacionJuego>();
        inst.semilla = Random.value * 100f;
    }

    /// <summary>Suma "trauma" (0..1). La sacudida es trauma², así los golpes chicos casi no se notan y los grandes sí.</summary>
    public static void Sacudir(float cantidad)
    {
        if (inst == null) Crear();
        inst.trauma = Mathf.Clamp01(inst.trauma + cantidad);
    }

    /// <summary>Congela el tiempo un instante (se siente el golpe).</summary>
    public static void Pausa(float segundos)
    {
        if (inst == null) Crear();
        if (!inst.enPausa && Time.timeScale > 0.5f) inst.StartCoroutine(inst.RutinaPausa(segundos));
    }

    IEnumerator RutinaPausa(float s)
    {
        enPausa = true;
        float previo = Time.timeScale;
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(s);
        if (Time.timeScale < 0.1f) Time.timeScale = previo;
        enPausa = false;
    }

    void LateUpdate()
    {
        // Te lastimaron: sacudida fuerte
        SaludJugador s = SaludJugador.Instancia;
        if (s != null)
        {
            if (vidaAnterior >= 0f && s.Vida < vidaAnterior - 0.5f) Sacudir(Mathf.Clamp01((vidaAnterior - s.Vida) / 40f) + 0.35f);
            vidaAnterior = s.Vida;
        }

        if (trauma <= 0f) return;
        Camera cam = Camera.main;
        if (cam == null) { trauma = 0f; return; }
        float k = trauma * trauma;
        float t = Time.unscaledTime * 22f;
        Vector3 pos = new Vector3(Mathf.PerlinNoise(semilla, t) - 0.5f, Mathf.PerlinNoise(semilla + 1, t) - 0.5f, 0f) * 0.22f * k;
        float giro = (Mathf.PerlinNoise(semilla + 2, t) - 0.5f) * 7f * k;
        cam.transform.position += cam.transform.rotation * pos;
        cam.transform.rotation *= Quaternion.Euler(0f, 0f, giro);
        trauma = Mathf.Max(0f, trauma - Time.unscaledDeltaTime * 1.3f);
    }
}
