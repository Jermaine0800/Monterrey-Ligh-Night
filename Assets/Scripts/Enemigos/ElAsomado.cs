using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// EL ASOMADO — una cosa pálida de brazos larguísimos que se asoma detrás de los troncos.
/// Si lo ves, se esconde detrás del árbol y aparece en otro, cada vez más cerca.
/// Si lo ignoras demasiado tiempo... corre hacia ti.
/// </summary>
public class ElAsomado : EnemigoBase
{
    [Header("Asomarse")]
    public float distanciaArbolMin = 10f, distanciaArbolMax = 26f;
    public float segundosVistoParaEsconderse = 0.9f;
    public int asomadasAntesDeCorrer = 4;
    public float segundosIgnoradoParaCorrer = 7f;

    [Header("Carrera")]
    public float velocidadCarrera = 7.5f;

    [Tooltip("Hijo con el modelo (para inclinarlo como si se asomara)")]
    public Transform modelo;

    enum Fase { Asomado, Corriendo }
    Fase fase;
    int asomadas;
    float visto, ignorado, tiempoEscondido;
    Vector3 arbolActual;

    protected override void Start()
    {
        base.Start();
        if (modelo == null && transform.childCount > 0) modelo = transform.GetChild(0);
    }

    protected override void AlActivar()
    {
        asomadas = 0;
        fase = Fase.Asomado;
        if (!AsomarseEnArbol(distanciaArbolMin, distanciaArbolMax)) Retirarse(false);
    }

    bool AsomarseEnArbol(float min, float max)
    {
        List<Vector3> arboles = Mundo.ArbolesCerca(jugador.position, min, max, 80);
        if (arboles.Count == 0) return false;

        // Prefiere árboles frente a la cámara (para que lo veas asomado)
        Transform cam = Camera.main != null ? Camera.main.transform : jugador;
        arboles.Sort((a, b) => Vector3.Angle(cam.forward, a - cam.position).CompareTo(Vector3.Angle(cam.forward, b - cam.position)));
        arbolActual = arboles[Random.Range(0, Mathf.Min(6, arboles.Count))];

        Vector3 haciaJugador = jugador.position - arbolActual; haciaJugador.y = 0; haciaJugador.Normalize();
        Vector3 lado = Vector3.Cross(Vector3.up, haciaJugador) * (Random.value < 0.5f ? 1f : -1f);
        Vector3 p = arbolActual - haciaJugador * 0.45f + lado * 0.45f;

        Teletransportar(p);
        if (modelo != null) modelo.localRotation = Quaternion.Euler(0, 0, Vector3.Dot(lado, transform.right) > 0 ? -22f : 22f);
        Animar("Idle");
        visto = ignorado = 0f;
        asomadas++;
        return true;
    }

    protected override void Tick()
    {
        if (JugadorEscondido)
        {
            tiempoEscondido += Time.deltaTime;
            if (tiempoEscondido > 3f) Retirarse();
            return;
        }
        tiempoEscondido = 0f;

        if (fase == Fase.Corriendo)
        {
            MoverHacia(jugador.position, velocidadCarrera);
            if (DistanciaJugador < distanciaSusto + 0.5f) Asustar();
            return;
        }

        MirarJugador(0.1f);

        if (MeVe())
        {
            visto += Time.deltaTime;
            ignorado = 0f;
            if (visto > segundosVistoParaEsconderse)
            {
                // Se esconde detrás del tronco... y sale en otro, más cerca
                VHSOverlay.Glitch(0.25f, 0.15f);
                if (asomadas >= asomadasAntesDeCorrer) { EmpezarCarrera(); return; }
                float max = Mathf.Max(distanciaArbolMin, DistanciaJugador - 4f);
                Mostrar(false);
                if (!AsomarseEnArbol(Mathf.Max(5f, max - 8f), max)) Retirarse();
                else if (Random.value < 0.5f) Susurrar(0.5f);
            }
        }
        else
        {
            ignorado += Time.deltaTime;
            if (ignorado > segundosIgnoradoParaCorrer && DistanciaJugador < 30f) EmpezarCarrera();
        }
    }

    void EmpezarCarrera()
    {
        fase = Fase.Corriendo;
        if (modelo != null) modelo.localRotation = Quaternion.identity;
        Animar("Run");
        voz.PlayOneShot(grito, 0.4f);
    }

    public override void Reiniciar()
    {
        base.Reiniciar();
        if (modelo != null) modelo.localRotation = Quaternion.identity;
    }
}
