using UnityEngine;

/// <summary>
/// LA SONRIENTE — figura de negro, cara pálida y una sonrisa que no le cabe en la cara.
/// Solo se mueve cuando NO la estás viendo: cada vez que volteas, está más cerca.
/// Mientras la ves se queda quieta, con la cabeza temblando.
/// La linterna en la cara la espanta. Si llega hasta ti: susto.
/// </summary>
public class LaSonriente : EnemigoBase
{
    [Header("Acecho")]
    public float distanciaInicial = 28f;
    public float pasoMin = 3f, pasoMax = 6f;
    public float cadaCuantoAvanza = 1.1f;
    public float segundosLinternaParaEspantar = 1.4f;
    public float tiempoMaximoAcechando = 70f;

    [Tooltip("Hueso o hijo de la cabeza (para el temblor). Se busca solo si está vacío.")]
    public Transform cabeza;

    float siguientePaso, tiempoAlumbrada, inicio, tiempoEscondido;
    Quaternion rotCabezaBase;

    protected override void Start()
    {
        base.Start();
        if (cabeza == null)
            foreach (Transform t in GetComponentsInChildren<Transform>())
                if (t.name.ToLower().Contains("head")) { cabeza = t; break; }
        if (cabeza != null) rotCabezaBase = cabeza.localRotation;
    }

    protected override void AlActivar()
    {
        Teletransportar(Mundo.PuntoFueraDeVista(jugador.position, distanciaInicial * 0.8f, distanciaInicial));
        Animar("Idle");
        inicio = Time.time;
        tiempoAlumbrada = tiempoEscondido = 0f;
        siguientePaso = Time.time + cadaCuantoAvanza;
    }

    protected override void Tick()
    {
        // Te escondiste: se cansa de buscarte
        if (JugadorEscondido)
        {
            tiempoEscondido += Time.deltaTime;
            if (tiempoEscondido > 4f) { Susurrar(0.4f); Retirarse(); }
            return;
        }
        tiempoEscondido = 0f;

        if (Time.time - inicio > tiempoMaximoAcechando) { Retirarse(); return; }

        // La linterna en la cara la espanta
        if (MeAlumbra())
        {
            tiempoAlumbrada += Time.deltaTime;
            if (tiempoAlumbrada > segundosLinternaParaEspantar)
            {
                voz.PlayOneShot(grito, 0.25f);
                Retirarse();
                return;
            }
        }
        else tiempoAlumbrada = Mathf.Max(0f, tiempoAlumbrada - Time.deltaTime);

        if (MeVe())
        {
            // Quieta, mirándote. Si está muy cerca y la estás viendo: susto.
            if (anim != null) anim.speed = 0f;
            if (DistanciaJugador < distanciaSusto + 1.2f) { if (anim != null) anim.speed = 1f; Asustar(); }
            return;
        }

        if (anim != null) anim.speed = 1f;

        // No la ves: avanza a saltos
        if (Time.time >= siguientePaso)
        {
            siguientePaso = Time.time + cadaCuantoAvanza * Random.Range(0.7f, 1.3f);
            float d = DistanciaJugador;
            if (d <= distanciaSusto + 1f) { Asustar(); return; }

            Vector3 haciaMi = (transform.position - jugador.position); haciaMi.y = 0;
            float nueva = Mathf.Max(distanciaSusto, d - Random.Range(pasoMin, pasoMax));
            Vector3 lateral = Vector3.Cross(Vector3.up, haciaMi.normalized) * Random.Range(-1.5f, 1.5f);
            Vector3 p = jugador.position + haciaMi.normalized * nueva + lateral;
            if (!Mundo.Visible(p + Vector3.up * 1.4f, 0.05f, false))
            {
                Teletransportar(p);
                if (Random.value < 0.25f) Susurrar(0.5f);
            }
        }
    }

    void LateUpdate()
    {
        // Temblor de cabeza (como cinta trabada)
        if (cabeza == null || EstadoActual != Estado.Acechando) return;
        if (Random.value < 0.15f)
            cabeza.localRotation = rotCabezaBase * Quaternion.Euler(Random.Range(-14f, 14f), Random.Range(-20f, 20f), Random.Range(-25f, 25f));
    }
}
