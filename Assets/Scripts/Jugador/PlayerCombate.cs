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

    // ---- Arma equipada (viene del maletín). Sin arma = manos.
    public ItemDefinition ArmaActual { get; private set; }
    public bool TieneArma => ArmaActual != null && ArmaActual.usos > 0;
    public string NombreArma => ArmaActual != null ? ArmaActual.nombre : "";
    public int UsosArma => ArmaActual != null ? ArmaActual.usos : 0;
    float danioArma => ArmaActual != null ? ArmaActual.valor : danio;
    float alcanceArma => ArmaActual != null ? ArmaActual.alcance : alcance;
    GameObject visualArma;

    /// <summary>Equipa un arma del maletín (null = manos).</summary>
    public void Equipar(ItemDefinition arma)
    {
        if (visualArma != null) Destroy(visualArma);
        ArmaActual = arma;
        if (arma == null) return;
        GameObject visual = InventarioUtil.NuevoVisual(arma.nombre);
        visualArma = visual;
        if (visual != null)
        {
            Animator a = GetComponentInChildren<Animator>();
            Transform mano = a != null && a.isHuman ? a.GetBoneTransform(HumanBodyBones.LeftHand) : null;
            visual.transform.SetParent(mano != null ? mano : transform, true);
            visual.transform.localPosition = mano != null ? new Vector3(0, 0.05f, 0.04f) : new Vector3(-0.35f, 1f, 0.3f);
            visual.transform.localRotation = Quaternion.Euler(0, 0, 90f);
        }
        if (src != null) src.PlayOneShot(ProceduralAudio.ClicMenu(), 0.6f);
    }

    /// <summary>Compatibilidad: equipar un arma sin pasar por el maletín.</summary>
    public void EquiparArma(string nombre, float danio, float alcance, int usos, GameObject visual)
    {
        if (visual != null) { InventarioUtil.RegistrarVisual(nombre, visual); Destroy(visual); }
        Equipar(InventarioUtil.Crear(nombre, ItemDefinition.Tipo.Arma, nombre, "", danio, alcance, usos));
    }

    void Gastar()
    {
        if (ArmaActual == null || ArmaActual.usos <= 0) return;
        ArmaActual.usos--;
        if (ArmaActual.usos == 0)
        {
            DialogoUI.Susurrar("", NombreArma + " se rompió.", 2.5f);
            src.PlayOneShot(ProceduralAudio.Rechinido(77), 0.8f);
            PlayerInventory inv = GetComponent<PlayerInventory>() ?? InventarioUtil.Inventario;
            if (inv != null) inv.Quitar(ArmaActual);
            Equipar(null);
            CambiarArma(1);
        }
    }

    /// <summary>Pasa a la siguiente (o anterior) arma del maletín, incluyendo "manos".</summary>
    public void CambiarArma(int dir)
    {
        PlayerInventory inv = GetComponent<PlayerInventory>() ?? InventarioUtil.Inventario;
        System.Collections.Generic.List<ItemDefinition> armas = new System.Collections.Generic.List<ItemDefinition> { null };
        if (inv != null) foreach (ItemDefinition it in inv.objetos) if (it != null && it.tipo == ItemDefinition.Tipo.Arma && it.usos > 0) armas.Add(it);
        int k = Mathf.Max(0, armas.IndexOf(ArmaActual));
        k = (k + dir + armas.Count) % armas.Count;
        Equipar(armas[k]);
        DialogoUI.Susurrar("", armas[k] != null ? armas[k].nombre + "  (" + armas[k].usos + " golpes)" : "Manos", 1.5f);
    }

    /// <summary>Usa la curación más pequeña que tengas (▲ / H).</summary>
    void CurarRapido()
    {
        PlayerInventory inv = GetComponent<PlayerInventory>() ?? InventarioUtil.Inventario;
        SaludJugador s = GetComponent<SaludJugador>();
        if (inv == null || s == null) return;
        if (s.Vida01 >= 0.999f) { DialogoUI.Susurrar("", "Estoy bien por ahora.", 1.5f); return; }
        ItemDefinition mejor = null;
        foreach (ItemDefinition it in inv.objetos) if (it != null && it.tipo == ItemDefinition.Tipo.Curacion && (mejor == null || it.valor < mejor.valor)) mejor = it;
        if (mejor == null) { DialogoUI.Susurrar("", "No tengo nada para curarme.", 1.5f); return; }
        inv.Quitar(mejor);
        Curacion.Usar(gameObject, mejor.valor);
        DialogoUI.Susurrar("", mejor.nombre + ": +" + Mathf.RoundToInt(mejor.valor) + " de vida.", 2f);
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
        if (!HorrorGameState.PuedeJugar) return;
        PlayerEscondite e = GetComponent<PlayerEscondite>();
        if (e != null && e.Escondido) return;
        if (HorrorInput.ArmaSiguiente) CambiarArma(1);
        if (HorrorInput.ArmaAnterior) CambiarArma(-1);
        if (HorrorInput.CurarRapido) CurarRapido();
        if (Time.time < listo) return;

        if (HorrorInput.Golpe)
        {
            // Cada golpe cansa (más con armas pesadas). Sin aire no puedes golpear.
            PlayerMovement pmv = GetComponent<PlayerMovement>();
            float costo = !TieneArma ? 0.5f : (NombreArma.Contains("Hacha") || NombreArma.Contains("Pala") ? 1.1f : 0.8f);
            if (pmv != null && !pmv.Gastar(costo))
            {
                DialogoUI.Susurrar("", "...no puedo... necesito aire.", 1.5f);
                listo = Time.time + 0.8f;
                return;
            }
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
