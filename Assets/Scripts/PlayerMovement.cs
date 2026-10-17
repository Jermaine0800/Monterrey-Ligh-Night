using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    public float gravity = -20f;

    private CharacterController controller;
    private Animator animator;
    private float velocidadVertical;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (Gamepad.current == null)
        {
            return;
        }

        // Detectar si estamos tocando el suelo
        if (controller.isGrounded && velocidadVertical < 0)
        {
            velocidadVertical = -2f;
        }

        // Joystick izquierdo
        Vector2 movimiento = Gamepad.current.leftStick.ReadValue();
         animator.SetFloat("Speed", movimiento.magnitude);

        Vector3 direccion = new Vector3(
            movimiento.x,
            0,
            movimiento.y
        );
            
        // Gravedad
        velocidadVertical += gravity * Time.deltaTime;

        // Juntar movimiento horizontal + gravedad
        direccion.y = velocidadVertical;

        // Mover personaje
        Vector3 movimientoFinal = direccion;
        movimientoFinal.x *= speed;
        movimientoFinal.z *= speed;

        controller.Move(movimientoFinal * Time.deltaTime);

    }
}