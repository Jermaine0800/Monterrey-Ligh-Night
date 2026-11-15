using UnityEngine;

/// <summary>
/// Pasos sobre tierra/hojas sincronizados con la velocidad del CharacterController.
/// Va en el jugador (mismo objeto que PlayerMovement).
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class FootstepAudio : MonoBehaviour
{
    [Range(0, 1)] public float volumen = 0.55f;
    public float pasosPorMetro = 0.65f;

    CharacterController cc;
    AudioSource src;
    AudioClip[] pasos;
    float distancia;
    Vector3 ultimaPos;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        src = gameObject.AddComponent<AudioSource>();
        src.spatialBlend = 0.6f;
        src.playOnAwake = false;
        pasos = new AudioClip[6];
        for (int i = 0; i < pasos.Length; i++) pasos[i] = ProceduralAudio.Paso(100 + i);
        ultimaPos = transform.position;
    }

    void Update()
    {
        Vector3 delta = transform.position - ultimaPos;
        ultimaPos = transform.position;
        delta.y = 0;
        if (!cc.isGrounded || Time.deltaTime <= 0f) return;

        distancia += delta.magnitude;
        if (distancia * pasosPorMetro >= 1f)
        {
            distancia = 0f;
            src.pitch = Random.Range(0.85f, 1.15f);
            src.PlayOneShot(pasos[Random.Range(0, pasos.Length)], volumen * Random.Range(0.8f, 1f));
        }
    }
}
