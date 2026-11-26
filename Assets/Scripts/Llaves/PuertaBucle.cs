using UnityEngine;

/// <summary>
/// La puerta del bucle, en el claro donde despertaste: una reja de panteón parada sola en medio del bosque.
/// Tiene 5 luces, una por llave. Con las 5, la reja se abre.
/// </summary>
public class PuertaBucle : MonoBehaviour
{
    public Light[] lucesLlaves;
    public PuertaBisagra hoja;
    public Light luzCentral;
    [Tooltip("El portal que aparece al juntar las 5 llaves (inactivo al inicio)")]
    public GameObject portal;

    public bool Abierta => hoja == null || hoja.Abierta;

    void Start()
    {
        Actualizar(LlavesBucle.Instancia != null ? LlavesBucle.Instancia.Llaves : 0);
        if (LlavesBucle.Instancia != null) LlavesBucle.Instancia.AlConseguirLlave += Actualizar;
        if (luzCentral != null) luzCentral.enabled = false;
        if (portal != null) portal.SetActive(false);
    }

    void Actualizar(int n)
    {
        if (lucesLlaves == null) return;
        for (int i = 0; i < lucesLlaves.Length; i++) if (lucesLlaves[i] != null) lucesLlaves[i].enabled = i < n;
        if (n >= LlavesBucle.Total) Abrir();
    }

    public void Abrir()
    {
        if (hoja != null) hoja.Abrir();
        if (portal != null && !portal.activeSelf) portal.SetActive(true);
        if (luzCentral != null) luzCentral.enabled = true;
        VHSOverlay.Glitch(0.8f, 1f);
    }
}
