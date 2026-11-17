using System.Collections;
using UnityEngine;

/// <summary>
/// Golpe del jugador (RT / clic). Le pega a cualquier cosa que sea IDaniable enfrente:
/// monstruos, objetos rompibles... Polimorfismo: no le importa QUÉ es, solo que se pueda dañar.
/// Parámetro del Animator (si existe): trigger "Golpe".
/// </summary>
public class PlayerCombate : MonoBehaviour
{
    public float danio = 34f;
    public float alcance = 1.3f;
    public float radio = 0.7f;
    public float enfriamiento = 0.75f;
    [Tooltip("Momento del golpe dentro de la animación (segundos)")]
    public float retrasoImpacto = 0.25f;

    // ---- Arma (pala, tubo...) con usos limitados
    public bool TieneArma => usosArma > 0;
    public string NombreArma { get; private set; } = "";
    public int UsosArma => usosArma;
    int usosArma;
    float danioArma, alcanceArma;
    GameObject visualArma;

    /// <summary>Equipa un arma: pega más fuerte y más lejos hasta que se rompe.</summary>
    public void EquiparArma(string nombre, float danio, float alcance, int usos, GameObject visual)
    {
        if (visualArma != null) Destroy(visualArma);
        NombreArma = nombre; danioArma = danio; alcanceArma = alcance; usosArma = usos;
        visualArma = visual;
        if (visual != null)
        {
            Animator a = GetComponentInChildren<Animator>();
            Transform mano = a != null && a.isHuman ? a.GetBoneTransform(HumanBodyBones.LeftHand) : null;
            visual.transform.SetParent(mano != null ? mano : transform, false);
            visual.transform.localPosition = mano != null ? new Vector3(0, 0.05f, 0.04f) : new Vector3(-0.35f, 1f, 0.3f);
            visual.transform.localRotation = Quaternion.Euler(0, 0, 90f);
        }
    }

    void Gastar()
    {
        if (usosArma <= 0) return;
        usosArma--;
        if (usosArma == 0)
        {
            DialogoUI.Susurrar("", "La " + NombreArma.ToLower() + " se rompió.", 2.5f);
            src.PlayOneShot(ProceduralAudio.Rechinido(77), 0.8f);
            if (visualArma != null) Destroy(visualArma);
        }
    }

    Animator animator;
    PlayerMovement movimiento;
    AudioSource src;
    AudioClip zumbido, impacto;
    float listo;
    bool tieneTrigger;

    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        movimiento = GetComponent<PlayerMovement>();
        src = gameObject.AddComponent<AudioSource>();
        src.spatialBlend = 0.5f;
        zumbido = ProceduralAudio.Susurro(301);
        impacto = ProceduralAudio.GolpeGrave();
        RevisarParametros();
    }

    public void RevisarParametros()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        tieneTrigger = false;
        if (animator == null || animator.runtimeAnimatorController == null) return;
        foreach (AnimatorControllerParameter p in animator.parameters)
            if (p.name == "Golpe" && p.type == AnimatorControllerParameterType.Trigger) tieneTrigger = true;
    }

    void Update()
    {
        if (!HorrorGameState.PuedeJugar || Time.time < listo) return;
        PlayerEscondite e = GetComponent<PlayerEscondite>();
        if (e != null && e.Escondido) return;

        if (HorrorInput.Golpe)
        {
            listo = Time.time + enfriamiento;
            if (tieneTrigger) animator.SetTrigger("Golpe");
            src.pitch = Random.Range(0.9f, 1.2f);
            src.PlayOneShot(zumbido, 0.3f);
            StartCoroutine(Impacto());
        }
    }

    IEnumerator Impacto()
    {
        yield return new WaitForSeconds(retrasoImpacto);
        float a = TieneArma ? alcanceArma : alcance;
        Vector3 centro = transform.position + Vector3.up * 1.2f + transform.forward * a;
        bool pego = false;
        foreach (Collider c in Physics.OverlapSphere(centro, radio * (TieneArma ? 1.3f : 1f), ~0, QueryTriggerInteraction.Collide))
        {
            if (c.transform.IsChildOf(transform)) continue;
            IDaniable d = c.GetComponentInParent<IDaniable>();
            if (d != null && d.EstaVivo)
            {
                bool conArma = TieneArma;
                d.RecibirDanio(conArma ? danioArma : danio, transform.position);
                if (conArma) Gastar();
                pego = true;
                break;
            }
        }
        if (pego)
        {
            src.PlayOneShot(impacto, 1f);
            HorrorInput.Vibrar(0.7f, 0.9f, 0.15f, this);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.2f + transform.forward * alcance, radio);
    }
}
