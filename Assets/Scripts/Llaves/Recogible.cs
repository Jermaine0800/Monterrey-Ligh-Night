using System.Collections;
using UnityEngine;

/// <summary>
/// Algo que se recoge con RB. Base común (gira despacio, brilla, se recoge con animación);
/// cada tipo decide qué hace al recogerse (polimorfismo): curarte, darte un arma...
/// </summary>
[RequireComponent(typeof(Collider))]
public abstract class Recogible : MonoBehaviour, IInteractable
{
    public string nombre = "objeto";
    public bool girar = false;
    bool tomado;

    void Update()
    {
        if (girar && !tomado) transform.Rotate(0, 35f * Time.deltaTime, 0, Space.World);
    }

    public void Interact(PlayerInteraction jugador)
    {
        if (tomado) return;
        if (!PuedeTomar(jugador)) return;
        tomado = true;
        GetComponent<Collider>().enabled = false;
        AlTomar(jugador);
        StartCoroutine(Desaparecer());
    }

    protected virtual bool PuedeTomar(PlayerInteraction jugador) => true;
    protected abstract void AlTomar(PlayerInteraction jugador);

    IEnumerator Desaparecer()
    {
        Vector3 e = transform.localScale;
        for (float t = 0; t < 0.25f; t += Time.deltaTime) { transform.localScale = e * (1f - t / 0.25f); yield return null; }
        gameObject.SetActive(false);
    }
}
