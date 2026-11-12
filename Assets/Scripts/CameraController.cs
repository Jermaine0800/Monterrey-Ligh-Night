using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public Transform objetivo;          // a quién sigue la cámara (el X Bot)
    public float altura = 1.6f;         // qué tan arriba del piso está el punto de giro
    public float distancia = 2.5f;      // qué tan atrás del personaje se aleja la cámara
    public float sensibilidad = 120f;   // qué tan rápido gira con el stick
    public float minVertical = -30f;    // límite para mirar hacia arriba
    public float maxVertical = 60f;     // límite para mirar hacia abajo

    private float giroHorizontal;
    private float giroVertical = 15f;
    private Transform enfoque; // durante diálogos la cámara voltea hacia el Vigía

    public void Enfocar(Transform t) { enfoque = t; }

    void Start()
    {
        if (objetivo != null)
        {
            giroHorizontal = objetivo.eulerAngles.y;
        }
        sensibilidad = PlayerPrefs.GetFloat("sensibilidad", sensibilidad); // desde Opciones del menú
        // Ocultar y bloquear el cursor opcionalmente si es necesario
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (objetivo == null) return;

        // 1. Leer el stick derecho del Gamepad para rotar los ángulos
        if (Gamepad.current != null && HorrorGameState.PuedeJugar)
        {
            Vector2 mirada = Gamepad.current.rightStick.ReadValue();
            giroHorizontal += mirada.x * sensibilidad * Time.deltaTime;
            giroVertical -= mirada.y * sensibilidad * Time.deltaTime;
            giroVertical = Mathf.Clamp(giroVertical, minVertical, maxVertical);
        }

        // 1b. Durante un diálogo la cámara gira sola hacia quien habla
        if (enfoque != null)
        {
            Vector3 d = enfoque.position - objetivo.position; d.y = 0;
            if (d.sqrMagnitude > 0.01f)
            {
                float angulo = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
                giroHorizontal = Mathf.LerpAngle(giroHorizontal, angulo, 2.5f * Time.unscaledDeltaTime);
                giroVertical = Mathf.Lerp(giroVertical, 6f, 2f * Time.unscaledDeltaTime);
            }
        }

        // 2. Calcular la rotación basada en los ángulos del stick
        Quaternion rotacion = Quaternion.Euler(giroVertical, giroHorizontal, 0f);
        transform.rotation = rotacion;

        // 3. Posicionar el pivote en el personaje más la altura
        Vector3 posicionObjetivo = objetivo.position + Vector3.up * altura;

        // 4. Calcular la posición final restando la distancia hacia atrás según hacia dónde mira
        transform.position = posicionObjetivo - (rotacion * Vector3.forward * distancia);
    }
}