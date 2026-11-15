using UnityEngine;

public class GuiaTrigger : MonoBehaviour
{
    [Header("Referencias del Guía")]
    public GameObject guiaBot;         // El objeto de tu guía en la escena
    public Animator guiaAnimator;      // El Animator del guía
    public float tiempoEnEscena = 6f;  // Segundos antes de desaparecer

    private bool yaSeActivo = false;

    private void OnTriggerEnter(Collider other)
    {
        // Detecta al jugador por su Tag
        if (other.CompareTag("Player") && !yaSeActivo)
        {
            yaSeActivo = true;
            guiaBot.SetActive(true); // Aparece entre los árboles

            if (guiaAnimator != null)
            {
                guiaAnimator.SetTrigger("Saludar"); // Dispara la animación de saludo
            }

            // Programa su desaparición tras cumplir su tiempo
            Invoke(nameof(Desaparecer), tiempoEnEscena);
        }
    }

    void Desaparecer()
    {
        guiaBot.SetActive(false); // Vuelve a ocultarse
    }
}               