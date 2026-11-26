using UnityEngine;

/// <summary>Parpadeo suave de una luz (ánimas, monitores, lámparas).</summary>
public class Parpadeo : MonoBehaviour
{
    Light l; float baseI; float semilla;
    void Start() { l = GetComponent<Light>(); baseI = l != null ? l.intensity : 1f; semilla = Random.value * 100f; }
    void Update() { if (l) l.intensity = baseI * (0.6f + 0.4f * Mathf.PerlinNoise(semilla, Time.time * 2f)); }
}
