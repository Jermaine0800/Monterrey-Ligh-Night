using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    public float gravity = -20f;
    public float velocidadGiro = 10f;

    public Transform camara; // Referencia a la cámara para obtener su orientación

    private CharacterController controller;
    private Animator animator;
    private float velocidadVertical;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        if (camara == null && Camera.main != null)
        {
            camara = Camera.main.transform; // Si no se asigna, usar la cámara principal
        }

    }


    void Update()
    {
        if (Gamepad.current == null || camara == null)
        {
            return;
        }

        // Detectar si estamos tocando el suelo
        if (controller.isGrounded && velocidadVertical < 0)
        {
            velocidadVertical = -2f;
        }

        Vector2 input = Gamepad.current.leftStick.ReadValue();
        animator.SetFloat("Speed", input.magnitude);


        // "Adelante" y "derecha" según la cámara, sin la inclinación arriba/abajo
        Vector3 adelante = camara.forward;
        Vector3 derecha = camara.right;
        adelante.y = 0;
        derecha.y = 0;
        adelante.Normalize();
        derecha.Normalize();
        Vector3 direccion = adelante * input.y + derecha * input.x;

        // Girar el cuerpo hacia donde camina
        if (direccion.sqrMagnitude > 0.01f)
        {
            Quaternion objetivo = Quaternion.LookRotation(direccion);
            transform.rotation = Quaternion.Slerp(transform.rotation, objetivo, velocidadGiro * Time.deltaTime);
        }

        velocidadVertical += gravity * Time.deltaTime;

        Vector3 movimientoFinal = direccion * speed;
        movimientoFinal.y = velocidadVertical;

        controller.Move(movimientoFinal * Time.deltaTime);
    }



}

// en caso de tener problemas con el movimiento, aquí hay una versión más simple que no considera la cámara ni la rotación del personaje:

/*
 public float speed = 5f;
 public float gravity = -20f;

 private CharacterController controller;
 private Animator animator;
 private float velocidadVertical;




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
 */
