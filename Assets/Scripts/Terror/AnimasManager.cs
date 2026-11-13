using UnityEngine;

/// <summary>
/// Ánimas: luces pálidas que flotan entre los árboles a lo lejos.
/// Si te acercas se apagan y reaparecen en otro lado. A veces susurran.
/// </summary>
public class AnimasManager : MonoBehaviour
{
    public int cantidad = 9;
    public float distanciaMin = 18f;
    public float distanciaMax = 55f;
    public float distanciaDesvanecer = 11f;
    public Color color = new Color(0.75f, 0.85f, 1f);

    class Anima
    {
        public Transform t;
        public Light luz;
        public AudioSource voz;
        public float fase, intensidad, apagando;
        public Vector3 deriva;
    }

    Anima[] animas;
    Transform jugador;
    AudioClip[] susurros;

    void Start()
    {
        susurros = new[] { ProceduralAudio.Susurro(11), ProceduralAudio.Susurro(12), ProceduralAudio.Susurro(13) };
        animas = new Anima[cantidad];
        for (int i = 0; i < cantidad; i++)
        {
            GameObject go = new GameObject("Anima_" + i);
            go.transform.SetParent(transform);
            Light l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.range = 4f;
            l.shadows = LightShadows.None;
            AudioSource a = go.AddComponent<AudioSource>();
            a.spatialBlend = 1f; a.minDistance = 4f; a.maxDistance = 35f;
            a.rolloffMode = AudioRolloffMode.Linear;
            animas[i] = new Anima { t = go.transform, luz = l, voz = a, fase = Random.value * 10f };
            go.SetActive(false);
        }
    }

    void Update()
    {
        if (jugador == null)
        {
            var pm = FindFirstObjectByType<PlayerMovement>();
            if (pm == null) return;
            jugador = pm.transform;
            foreach (var a in animas) Reubicar(a);
        }

        foreach (Anima a in animas)
        {
            float d = Vector3.Distance(a.t.position, jugador.position);
            if (d > distanciaMax * 1.4f) { Reubicar(a); continue; }

            // Se apaga si te acercas
            if (d < distanciaDesvanecer) a.apagando = Mathf.Min(1f, a.apagando + Time.deltaTime * 1.5f);
            if (a.apagando >= 1f) { Reubicar(a); continue; }

            a.fase += Time.deltaTime;
            Vector3 p = a.t.position + a.deriva * Time.deltaTime;
            p.y = GuiaController.AlturaSuelo(p) + 1.2f + Mathf.Sin(a.fase * 0.9f) * 0.4f;
            a.t.position = p;

            float pulso = 0.55f + 0.45f * Mathf.PerlinNoise(a.fase * 1.3f, 0.5f);
            a.luz.intensity = a.intensidad * pulso * (1f - a.apagando);

            if (!a.voz.isPlaying && Random.value < 0.0008f)
                a.voz.PlayOneShot(susurros[Random.Range(0, susurros.Length)], 0.5f);
        }
    }

    void Reubicar(Anima a)
    {
        Vector2 r = Random.insideUnitCircle.normalized * Random.Range(distanciaMin, distanciaMax);
        Vector3 p = jugador.position + new Vector3(r.x, 0, r.y);
        p.y = GuiaController.AlturaSuelo(p) + 1.2f;
        a.t.position = p;
        a.apagando = 0f;
        a.intensidad = Random.Range(0.8f, 2f);
        Vector2 dv = Random.insideUnitCircle * 0.6f;
        a.deriva = new Vector3(dv.x, 0, dv.y);
        a.t.gameObject.SetActive(true);
    }
}
