using UnityEngine;

/// <summary>Pala, tubo, crucifijo...: un arma con pocos usos que pega más fuerte.</summary>
public class ArmaRecogible : Recogible
{
    public float danio = 60f;
    public float alcance = 1.6f;
    public int usos = 6;
    [Tooltip("Modelo que se pone en la mano (este mismo objeto si está vacío)")]
    public GameObject modeloMano;

    protected override void AlTomar(PlayerInteraction jugador)
    {
        PlayerCombate c = jugador.GetComponent<PlayerCombate>();
        if (c == null) return;
        GameObject visual = Instantiate(modeloMano != null ? modeloMano : gameObject);
        foreach (Collider col in visual.GetComponentsInChildren<Collider>()) Destroy(col);
        foreach (MonoBehaviour mb in visual.GetComponentsInChildren<MonoBehaviour>()) Destroy(mb);
        foreach (Light l in visual.GetComponentsInChildren<Light>()) Destroy(l.gameObject);
        visual.SetActive(true);
        c.EquiparArma(nombre, danio, alcance, usos, visual);
        AudioSource.PlayClipAtPoint(ProceduralAudio.GolpeGrave(), transform.position, 0.4f);
        DialogoUI.Susurrar("", nombre + ". Con esto por lo menos le puedo pegar en serio.  [RT]", 3.5f);
    }
}
