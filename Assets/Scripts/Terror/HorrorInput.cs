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

    /// <summary>Stick derecho (velocidad: se multiplica por el tiempo).</summary>
    public static Vector2 Mirar => Gamepad.current != null ? Gamepad.current.rightStick.ReadValue() : Vector2.zero;
    /// <summary>Ratón en píxeles este cuadro (NO se multiplica por el tiempo: así no cambia con los FPS).</summary>
    public static Vector2 MirarRaton => Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
    /// <summary>Invertir el eje vertical (Opciones). </summary>
    public static bool InvertirY => PlayerPrefs.GetInt("invertir_y", 0) == 1;

    /// <summary>Pasar diálogo / cerrar nota: A, E, Enter, Espacio o clic.</summary>
    public static bool Avanzar => Aceptar || RB || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
    /// <summary>Ver la tarjeta de controles: View/Back en el control, F1 en teclado.</summary>
    public static bool VerControles => (Gamepad.current != null && Gamepad.current.selectButton.wasPressedThisFrame) || Tecla(Key.F1, Key.G);
    static int RuedaRaton
    {
        get
        {
            if (Mouse.current == null) return 0;
            float y = Mouse.current.scroll.ReadValue().y;
            return y > 0.1f ? 1 : y < -0.1f ? -1 : 0;
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

    // ------------------------------------------------------------------ Esquema estándar (Xbox / PC)
    // A = acción (interactuar, recoger, esconderse, leer)   ·  B = agacharse / atrás / salir del escondite
    // X = linterna  ·  Y = maletín  ·  RT = golpear  ·  L3 o LB = correr  ·  R3 = cámara
    // Cruceta ◀ ▶ = cambiar arma  ·  Cruceta ▲ = curarse rápido  ·  Start = pausa
    // Teclado: WASD, ratón, E acción, Shift correr, C/Ctrl agacharse, F linterna, Tab maletín,
    //          clic izq. golpear, Q / R cambiar arma, H curarse, V cámara, Esc pausa.

    /// <summary>Acción contextual: A (estándar) o RB / E.</summary>
    public static bool Interactuar => (Gamepad.current != null && (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.rightShoulder.wasPressedThisFrame)) || Tecla(Key.E, Key.Enter);
    /// <summary>Salir del escondite: A, B o E.</summary>
    public static bool SalirEscondite => Interactuar || Agacharse;
    public static bool ArmaSiguiente => (Gamepad.current != null && Gamepad.current.dpad.right.wasPressedThisFrame) || Tecla(Key.R, Key.Digit2) || RuedaRaton < 0;
    public static bool ArmaAnterior => (Gamepad.current != null && Gamepad.current.dpad.left.wasPressedThisFrame) || Tecla(Key.Q, Key.Digit1) || RuedaRaton > 0;
    public static bool CurarRapido => (Gamepad.current != null && Gamepad.current.dpad.up.wasPressedThisFrame) || Tecla(Key.H, Key.H);

    static bool ultimoFueControl;
    /// <summary>¿El jugador está usando el control (true) o el teclado (false)? Para mostrar el botón correcto.</summary>
    public static bool UsandoControl
    {
        get
        {
            var gp = Gamepad.current;
            if (gp != null && (gp.wasUpdatedThisFrame && (gp.leftStick.ReadValue().sqrMagnitude > 0.1f || gp.buttonSouth.isPressed || gp.rightStick.ReadValue().sqrMagnitude > 0.1f))) ultimoFueControl = true;
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) ultimoFueControl = false;
            if (gp == null) ultimoFueControl = false;
            return ultimoFueControl;
        }
    }

    /// <summary>Etiqueta del botón para una acción, según el dispositivo en uso.</summary>
    public static string Boton(string accion)
    {
        bool c = UsandoControl;
        switch (accion)
        {
            case "accion": return c ? "A" : "E";
            case "linterna": return c ? "X" : "F";
            case "maletin": return c ? "Y" : "Tab";
            case "golpe": return c ? "RT" : "Clic";
            case "agacharse": return c ? "B" : "C";
            case "arma": return c ? "◀ ▶" : "Rueda / Q R";
            case "correr": return c ? "L3 / LB" : "Shift";
            case "camara": return c ? "R3" : "V";
            case "pausa": return c ? "Start" : "Esc";
            case "controles": return c ? "View" : "F1 / G";
            case "curar": return c ? "▲" : "H";
            default: return accion;
        }
    }

    /// <summary>Cambia los botones escritos en un texto ([RB], [X]...) por los del dispositivo en uso.</summary>
    public static string Traducir(string t)
    {
        if (string.IsNullOrEmpty(t)) return t;
        return t.Replace("[RB]", "[" + Boton("accion") + "]").Replace("[X]", "[" + Boton("linterna") + "]")
                .Replace("[Y]", "[" + Boton("maletin") + "]").Replace("[RT]", "[" + Boton("golpe") + "]")
                .Replace("[A]", "[" + Boton("accion") + "]").Replace("[B]", "[" + Boton("agacharse") + "]")
                .Replace("[LB]", "[" + Boton("correr") + "]").Replace("[▲]", "[" + Boton("curar") + "]");
    }

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
        try { gp.SetMotorSpeeds(bajo, alto); } catch { yield break; }
        yield return new WaitForSecondsRealtime(s);
        try { if (gp != null) gp.SetMotorSpeeds(0, 0); } catch { }
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
