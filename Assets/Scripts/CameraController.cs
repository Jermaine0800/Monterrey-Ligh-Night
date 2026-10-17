using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public Transform objetivo;          // a quién sigue la cámara (el X Bot)
    public float altura = 1.6f;         // qué tan arriba del piso está el punto de giro
    public float sensibilidad = 120f;   // qué tan rápido gira con el stick
    public float minVertical = -30f;    // límite para mirar hacia arriba
    public float maxVertical = 60f;     // límite para mirar hacia abajo

    private float giroHorizontal;       // ángulo izquierda/derecha
    private float giroVertical = 15f;   // ángulo arriba/abajo

    void Start()
    {
        if (objetivo != null)
        {
            giroHorizontal = objetivo.eulerAngles.y;
        }
    }

    void LateUpdate()
    {
        if (objetivo == null) return;

        // Ponerse donde está el personaje, un poco arriba
        transform.position = objetivo.position + Vector3.up * altura;

        // Leer el stick derecho (solo si hay control conectado)
        if (Gamepad.current != null)
        {
            Vector2 mirada = Gamepad.current.rightStick.ReadValue();
            giroHorizontal += mirada.x * sensibilidad * Time.deltaTime;
            giroVertical -= mirada.y * sensibilidad * Time.deltaTime;
            giroVertical = Mathf.Clamp(giroVertical, minVertical, maxVertical);
        }

        // 3. Aplicar el giro
        transform.rotation = Quaternion.Euler(giroVertical, giroHorizontal, 0f);
    }
}

    /*/
    void Update()
    {
        if (Gamepad.current == null)
        {
            Debug.Log("No hay controlador conectado");
            return;
        }

        Vector2 mirada = Gamepad.current.rightStick.ReadValue();

        Debug.Log(mirada);
    }

    */


