using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Entrada unificada para menús: control de Xbox (y cualquier gamepad) + teclado de respaldo.
/// Detecta "pulsaciones" del stick izquierdo para navegar como con la cruceta.
/// </summary>
public static class HorrorInput
{
    const float Umbral = 0.6f;
    static int ultimoFrame = -1;
    static Vector2Int dirAnterior, dirActual;

    static void Actualizar()
    {
        if (ultimoFrame == Time.frameCount) return;
        ultimoFrame = Time.frameCount;
        dirAnterior = dirActual;
        dirActual = Vector2Int.zero;
        var gp = Gamepad.current;
        if (gp != null)
        {
            Vector2 s = gp.leftStick.ReadValue();
            if (s.y > Umbral) dirActual.y = 1; else if (s.y < -Umbral) dirActual.y = -1;
            if (s.x > Umbral) dirActual.x = 1; else if (s.x < -Umbral) dirActual.x = -1;
        }
    }

    static bool Stick(int x, int y)
    {
        Actualizar();
        bool ahora = (x != 0 && dirActual.x == x) || (y != 0 && dirActual.y == y);
        bool antes = (x != 0 && dirAnterior.x == x) || (y != 0 && dirAnterior.y == y);
        return ahora && !antes;
    }

    static bool Tecla(Key a, Key b)
    {
        var kb = Keyboard.current;
        return kb != null && (kb[a].wasPressedThisFrame || kb[b].wasPressedThisFrame);
    }

    public static bool Arriba    => Stick(0, 1)  || (Gamepad.current != null && Gamepad.current.dpad.up.wasPressedThisFrame)    || Tecla(Key.UpArrow, Key.W);
    public static bool Abajo     => Stick(0, -1) || (Gamepad.current != null && Gamepad.current.dpad.down.wasPressedThisFrame)  || Tecla(Key.DownArrow, Key.S);
    public static bool Izquierda => Stick(-1, 0) || (Gamepad.current != null && Gamepad.current.dpad.left.wasPressedThisFrame)  || Tecla(Key.LeftArrow, Key.A);
    public static bool Derecha   => Stick(1, 0)  || (Gamepad.current != null && Gamepad.current.dpad.right.wasPressedThisFrame) || Tecla(Key.RightArrow, Key.D);

    /// <summary>A en Xbox / Enter.</summary>
    public static bool Aceptar  => (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) || Tecla(Key.Enter, Key.Space);
    /// <summary>B en Xbox / Escape.</summary>
    public static bool Cancelar => (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame) || Tecla(Key.Escape, Key.Backspace);
    /// <summary>Start (Menu) en Xbox / Escape.</summary>
    public static bool Start    => (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame) || Tecla(Key.Escape, Key.P);
    /// <summary>Y en Xbox / Tab.</summary>
    public static bool Inventario => (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame) || Tecla(Key.Tab, Key.I);
    /// <summary>X en Xbox / F.</summary>
    public static bool Linterna => (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame) || Tecla(Key.F, Key.L);
    public static bool LB => (Gamepad.current != null && Gamepad.current.leftShoulder.wasPressedThisFrame) || Tecla(Key.Q, Key.PageUp);
    public static bool RB => (Gamepad.current != null && Gamepad.current.rightShoulder.wasPressedThisFrame) || Tecla(Key.E, Key.PageDown);

    // ---------------- Jugador ----------------

    /// <summary>Stick izquierdo o WASD/flechas.</summary>
    public static Vector2 Movimiento
    {
        get
        {
            Vector2 v = Gamepad.current != null ? Gamepad.current.leftStick.ReadValue() : Vector2.zero;
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v.y += 1;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v.y -= 1;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) v.x += 1;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) v.x -= 1;
            }
            return Vector2.ClampMagnitude(v, 1f);
        }
    }

    /// <summary>Stick derecho (o mouse).</summary>
    public static Vector2 Mirar
    {
        get
        {
            Vector2 v = Gamepad.current != null ? Gamepad.current.rightStick.ReadValue() : Vector2.zero;
            if (Mouse.current != null) v += Mouse.current.delta.ReadValue() * 0.05f;
            return v;
        }
    }

    /// <summary>Correr: mantener LB o Shift. L3 lo alterna.</summary>
    public static bool CorrerMantenido => (Gamepad.current != null && Gamepad.current.leftShoulder.isPressed) ||
                                          (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed);
    public static bool CorrerAlternar => Gamepad.current != null && Gamepad.current.leftStickButton.wasPressedThisFrame;
    /// <summary>Agacharse: B o C.</summary>
    public static bool Agacharse => (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame) || Tecla(Key.C, Key.LeftCtrl);
    /// <summary>Golpear: RT o clic izquierdo.</summary>
    public static bool Golpe => (Gamepad.current != null && Gamepad.current.rightTrigger.wasPressedThisFrame) ||
                                (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
    /// <summary>Cambiar primera / tercera persona: R3 o V.</summary>
    public static bool CambiarCamara => (Gamepad.current != null && Gamepad.current.rightStickButton.wasPressedThisFrame) || Tecla(Key.V, Key.V);

    /// <summary>Texto del estado del control (para el menú).</summary>
    public static string EstadoControl()
    {
        var gp = Gamepad.current;
        if (gp == null) return "SIN CONTROL  //  USANDO TECLADO";
        string nombre = string.IsNullOrEmpty(gp.displayName) ? "GAMEPAD" : gp.displayName.ToUpperInvariant();
        return nombre + "  //  CONECTADO";
    }

    public static void Vibrar(float bajo, float alto, float segundos, MonoBehaviour duenio)
    {
        var gp = Gamepad.current;
        if (gp == null || duenio == null) return;
        duenio.StartCoroutine(VibrarRutina(gp, bajo, alto, segundos));
    }

    static System.Collections.IEnumerator VibrarRutina(Gamepad gp, float bajo, float alto, float s)
    {
        gp.SetMotorSpeeds(bajo, alto);
        yield return new WaitForSecondsRealtime(s);
        if (gp != null) gp.SetMotorSpeeds(0, 0);
    }
}

/// <summary>Estado global del juego: quién tiene el control.</summary>
public static class HorrorGameState
{
    public enum Modo { Jugando, Inventario, Pausa, Menu, Dialogo, Final }
    public static Modo modo = Modo.Jugando;

    public static bool PuedeJugar => modo == Modo.Jugando;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reiniciar()
    {
        modo = Modo.Jugando;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) =>
        {
            Time.timeScale = 1f;
            modo = Modo.Jugando;
        };
    }
}
