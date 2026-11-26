using UnityEngine;

/// <summary>Parpadeo nervioso de encendido/apagado (linterna tirada, lámparas de túnel fallando, baliza).</summary>
public class ParpadeoFuerte : MonoBehaviour
{
    public float encendido = 0.35f, apagado = 0.25f;
    public bool regular = false;
    Light l; float baseI, siguiente; bool on = true;
    void Start() { l = GetComponent<Light>(); baseI = l != null ? l.intensity : 1f; }
    void Update()
    {
        if (l == null || Time.time < siguiente) return;
        on = !on;
        l.intensity = on ? baseI : 0f;
        float d = on ? encendido : apagado;
        siguiente = Time.time + (regular ? d : d * Random.Range(0.2f, 1.8f));
    }
}
