using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menú principal estilo Silent Hill / terror analógico. Todo se construye por código.
/// Se navega con el control de Xbox (stick o cruceta, A aceptar, B atrás) o teclado.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Juego")]
    public string tituloJuego = "MONTERREY\nLIGHT NIGHT";
    public string subtitulo = "grabación recuperada  ·  11 / 11";
    public string escenaJuego = "SampleScene";

    enum Pantalla { PresionaStart, Principal, Opciones, Controles, Cargando }
    Pantalla pantalla = Pantalla.PresionaStart;

    Canvas canvas;
    Text titulo, sub, presionaStart, estadoControl, ayuda;
    Image linea;
    GameObject grupoPrincipal, grupoOpciones, grupoControles;
    readonly List<Text> itemsPrincipal = new List<Text>();
    readonly List<Text> itemsOpciones = new List<Text>();
    readonly List<Text> valoresOpciones = new List<Text>();
    Image fundido;
    int sel = 0, selOpc = 0;

    AudioSource srcDrone, srcEstatica, srcSfx;
    AudioClip clic, golpe;

    readonly string[] nombresPrincipal = { "NUEVA PARTIDA", "CONTINUAR", "SISTEMA DE COMANDOS", "OPCIONES", "SALIR" };

    // Opciones (se guardan en PlayerPrefs y las leen los demás scripts)
    float brillo, volumen, sensibilidad; bool vibracion;

    void Start()
    {
        HorrorGameState.modo = HorrorGameState.Modo.Menu;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;

        brillo = PlayerPrefs.GetFloat("brillo", 1f);
        volumen = PlayerPrefs.GetFloat("volumen", 0.8f);
        sensibilidad = PlayerPrefs.GetFloat("sensibilidad", 120f);
        vibracion = PlayerPrefs.GetInt("vibracion", 1) == 1;
        AudioListener.volume = volumen;

        if (Camera.main != null)
        {
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = Color.black;
        }

        CrearAudio();
        CrearVHS();
        Construir();
        MostrarPantalla(Pantalla.PresionaStart);
    }

    // ------------------------------------------------------------------ Construcción

    void CrearAudio()
    {
        srcDrone = gameObject.AddComponent<AudioSource>();
        srcDrone.clip = ProceduralAudio.Drone();
        srcDrone.loop = true; srcDrone.volume = 0.45f; srcDrone.Play();

        srcEstatica = gameObject.AddComponent<AudioSource>();
        srcEstatica.clip = ProceduralAudio.Estatica();
        srcEstatica.loop = true; srcEstatica.volume = 0.05f; srcEstatica.Play();

        srcSfx = gameObject.AddComponent<AudioSource>();
        clic = ProceduralAudio.ClicMenu();
        golpe = ProceduralAudio.GolpeGrave();
    }

    void CrearVHS()
    {
        if (FindFirstObjectByType<VHSOverlay>() != null) return;
        GameObject go = new GameObject("VHS");
        go.SetActive(false);
        VHSOverlay v = go.AddComponent<VHSOverlay>();
        v.modoMenu = true;
        v.etiquetaCinta = "CINTA 00 // NO GRABAR ENCIMA";
        go.SetActive(true);
    }

    void Construir()
    {
        canvas = HorrorUI.CrearCanvas("Menu_Canvas", 100, transform);
        Transform raiz = canvas.transform;

        Image fondo = HorrorUI.CrearImagen(raiz, "Fondo", Color.black);
        HorrorUI.Estirar(fondo.rectTransform);

        // Línea roja tenue y título
        linea = HorrorUI.CrearImagen(raiz, "Linea", new Color(HorrorUI.Sangre.r, HorrorUI.Sangre.g, HorrorUI.Sangre.b, 0.8f));
        HorrorUI.Colocar(linea.rectTransform, new Vector2(0, 1), new Vector2(180, -500), new Vector2(620, 3));

        titulo = HorrorUI.CrearTexto(raiz, "Titulo", Espaciar(tituloJuego), 92, HorrorUI.Hueso, TextAnchor.LowerLeft);
        titulo.lineSpacing = 0.9f;
        HorrorUI.Colocar(titulo.rectTransform, new Vector2(0, 1), new Vector2(175, -220), new Vector2(1400, 260));

        sub = HorrorUI.CrearTexto(raiz, "Subtitulo", subtitulo, 26, HorrorUI.HuesoTenue, TextAnchor.UpperLeft, FontStyle.Italic);
        HorrorUI.Colocar(sub.rectTransform, new Vector2(0, 1), new Vector2(182, -514), new Vector2(900, 40));

        // Presiona Start
        presionaStart = HorrorUI.CrearTexto(raiz, "PresionaStart", "—   P R E S I O N A   S T A R T   —", 34, HorrorUI.Hueso, TextAnchor.MiddleCenter);
        HorrorUI.Colocar(presionaStart.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 260), new Vector2(1200, 60));

        estadoControl = HorrorUI.CrearTexto(raiz, "EstadoControl", "", 22, HorrorUI.HuesoTenue, TextAnchor.MiddleCenter);
        HorrorUI.Colocar(estadoControl.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 150), new Vector2(1200, 40));

        ayuda = HorrorUI.CrearTexto(raiz, "Ayuda", "", 24, HorrorUI.HuesoTenue, TextAnchor.MiddleRight);
        HorrorUI.Colocar(ayuda.rectTransform, new Vector2(1, 0), new Vector2(-80, 150), new Vector2(900, 40));

        // Menú principal
        grupoPrincipal = Grupo(raiz, "Principal");
        for (int i = 0; i < nombresPrincipal.Length; i++)
        {
            Text t = HorrorUI.CrearTexto(grupoPrincipal.transform, nombresPrincipal[i], nombresPrincipal[i], 40, HorrorUI.HuesoTenue);
            HorrorUI.Colocar(t.rectTransform, new Vector2(0, 1), new Vector2(180, -600 - i * 64), new Vector2(900, 60));
            itemsPrincipal.Add(t);
        }

        // Opciones
        grupoOpciones = Grupo(raiz, "Opciones");
        Text tOpc = HorrorUI.CrearTexto(grupoOpciones.transform, "TituloOpc", "O P C I O N E S", 34, HorrorUI.Sangre);
        HorrorUI.Colocar(tOpc.rectTransform, new Vector2(0, 1), new Vector2(180, -200), new Vector2(900, 50));
        for (int i = 0; i < 5; i++)
        {
            Text t = HorrorUI.CrearTexto(grupoOpciones.transform, "Opc" + i, "", 36, HorrorUI.HuesoTenue);
            HorrorUI.Colocar(t.rectTransform, new Vector2(0, 1), new Vector2(180, -300 - i * 70), new Vector2(1300, 56));
            itemsOpciones.Add(t);
            Text v = HorrorUI.CrearTexto(grupoOpciones.transform, "Valor" + i, "", 36, HorrorUI.HuesoTenue);
            HorrorUI.Colocar(v.rectTransform, new Vector2(0, 1), new Vector2(720, -300 - i * 70), new Vector2(700, 56));
            valoresOpciones.Add(v);
        }

        // Sistema de comandos
        grupoControles = Grupo(raiz, "Controles");
        ConstruirControles(grupoControles.transform);

        fundido = HorrorUI.CrearImagen(raiz, "Fundido", new Color(0, 0, 0, 0));
        HorrorUI.Estirar(fundido.rectTransform);
    }

    GameObject Grupo(Transform padre, string nombre)
    {
        GameObject g = new GameObject(nombre, typeof(RectTransform));
        g.transform.SetParent(padre, false);
        HorrorUI.Estirar(HorrorUI.Rect(g));
        return g;
    }

    void ConstruirControles(Transform padre)
    {
        Text t = HorrorUI.CrearTexto(padre, "TituloCtrl", "S I S T E M A   D E   C O M A N D O S", 34, HorrorUI.Sangre);
        HorrorUI.Colocar(t.rectTransform, new Vector2(0, 1), new Vector2(180, -200), new Vector2(1200, 50));

        // (botón, color Xbox apagado, acción)
        var filas = new (string boton, Color color, string accion)[]
        {
            ("L",     HorrorUI.Hueso,                       "Stick izquierdo  —  caminar"),
            ("R",     HorrorUI.Hueso,                       "Stick derecho  —  mover la cámara"),
            ("RB",    HorrorUI.Hueso,                       "Interactuar  /  recoger objetos"),
            ("Y",     new Color(0.85f, 0.7f, 0.2f),         "Abrir el maletín (necesitas la mochila)"),
            ("X",     new Color(0.25f, 0.45f, 0.85f),       "Encender / apagar la linterna"),
            ("A",     new Color(0.3f, 0.7f, 0.3f),          "Aceptar"),
            ("B",     new Color(0.75f, 0.2f, 0.2f),         "Atrás  /  cerrar maletín"),
            ("☰",     HorrorUI.Hueso,                       "START  —  pausa"),
            ("+",     HorrorUI.Hueso,                       "Cruceta  —  moverse dentro del maletín"),
        };

        for (int i = 0; i < filas.Length; i++)
        {
            float y = -290 - i * 60;
            Image caja = HorrorUI.CrearImagen(padre, "Glifo" + i, new Color(0, 0, 0, 0.6f));
            HorrorUI.Colocar(caja.rectTransform, new Vector2(0, 1), new Vector2(180, y), new Vector2(70, 42));
            HorrorUI.CrearMarco(caja.transform, "Marco", filas[i].color, 2);
            Text g = HorrorUI.CrearTexto(caja.transform, "Letra", filas[i].boton, 24, filas[i].color, TextAnchor.MiddleCenter, FontStyle.Bold);
            HorrorUI.Estirar(g.rectTransform);

            Text a = HorrorUI.CrearTexto(padre, "Accion" + i, filas[i].accion, 28, HorrorUI.Hueso);
            HorrorUI.Colocar(a.rectTransform, new Vector2(0, 1), new Vector2(280, y - 2), new Vector2(1200, 42));
        }
    }

    static string Espaciar(string s)
    {
        // Separa las letras para el look de título de PS1/PS2
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (char ch in s)
        {
            sb.Append(ch);
            if (ch != '\n' && ch != ' ') sb.Append(' ');
        }
        return sb.ToString();
    }

    // ------------------------------------------------------------------ Lógica

    void MostrarPantalla(Pantalla p)
    {
        pantalla = p;
        presionaStart.gameObject.SetActive(p == Pantalla.PresionaStart);
        grupoPrincipal.SetActive(p == Pantalla.Principal);
        grupoOpciones.SetActive(p == Pantalla.Opciones);
        grupoControles.SetActive(p == Pantalla.Controles);
        bool grande = p == Pantalla.PresionaStart || p == Pantalla.Principal || p == Pantalla.Cargando;
        titulo.gameObject.SetActive(grande);
        sub.gameObject.SetActive(grande);
        linea.gameObject.SetActive(grande);

        switch (p)
        {
            case Pantalla.PresionaStart: ayuda.text = ""; break;
            case Pantalla.Principal:     ayuda.text = "[A] ACEPTAR      [B] ATRÁS"; RefrescarPrincipal(); break;
            case Pantalla.Opciones:      ayuda.text = "[◄ ►] AJUSTAR      [B] GUARDAR Y VOLVER"; RefrescarOpciones(); break;
            case Pantalla.Controles:     ayuda.text = "[B] VOLVER"; break;
        }
    }

    bool HayPartida => PlayerPrefs.GetInt("partida_guardada", 0) == 1;

    void Update()
    {
        estadoControl.text = HorrorInput.EstadoControl();

        switch (pantalla)
        {
            case Pantalla.PresionaStart:
                presionaStart.color = new Color(HorrorUI.Hueso.r, HorrorUI.Hueso.g, HorrorUI.Hueso.b,
                                                0.25f + 0.75f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 1.6f)));
                if (HorrorInput.Start || HorrorInput.Aceptar)
                {
                    Sonar(golpe, 0.9f);
                    VHSOverlay.Glitch(0.8f, 0.35f);
                    Vibrar(0.4f, 0.2f, 0.2f);
                    MostrarPantalla(Pantalla.Principal);
                }
                break;

            case Pantalla.Principal:
                if (HorrorInput.Arriba) { sel = Siguiente(sel, -1); Sonar(clic, 0.6f); RefrescarPrincipal(); }
                if (HorrorInput.Abajo)  { sel = Siguiente(sel, +1); Sonar(clic, 0.6f); RefrescarPrincipal(); }
                if (HorrorInput.Cancelar) { Sonar(clic, 0.6f); MostrarPantalla(Pantalla.PresionaStart); }
                else if (HorrorInput.Aceptar) ElegirPrincipal();
                AnimarSeleccion(itemsPrincipal, sel);
                break;

            case Pantalla.Opciones:
                if (HorrorInput.Arriba) { selOpc = (selOpc + itemsOpciones.Count - 1) % itemsOpciones.Count; Sonar(clic, 0.6f); RefrescarOpciones(); }
                if (HorrorInput.Abajo)  { selOpc = (selOpc + 1) % itemsOpciones.Count; Sonar(clic, 0.6f); RefrescarOpciones(); }
                if (HorrorInput.Izquierda) AjustarOpcion(-1);
                if (HorrorInput.Derecha)   AjustarOpcion(+1);
                if (HorrorInput.Cancelar || (HorrorInput.Aceptar && selOpc == 4)) { GuardarOpciones(); Sonar(clic, 0.6f); MostrarPantalla(Pantalla.Principal); }
                AnimarSeleccion(itemsOpciones, selOpc);
                break;

            case Pantalla.Controles:
                if (HorrorInput.Cancelar || HorrorInput.Aceptar) { Sonar(clic, 0.6f); MostrarPantalla(Pantalla.Principal); }
                break;
        }
    }

    int Siguiente(int actual, int paso)
    {
        int n = itemsPrincipal.Count;
        int s = actual;
        for (int i = 0; i < n; i++)
        {
            s = (s + paso + n) % n;
            if (s == 1 && !HayPartida) continue; // saltar CONTINUAR si no hay partida
            return s;
        }
        return actual;
    }

    void RefrescarPrincipal()
    {
        for (int i = 0; i < itemsPrincipal.Count; i++)
        {
            string nombre = nombresPrincipal[i];
            bool deshabilitado = i == 1 && !HayPartida;
            itemsPrincipal[i].text = (i == sel ? "▸  " : "    ") + nombre;
            itemsPrincipal[i].color = deshabilitado ? new Color(1, 1, 1, 0.12f) : (i == sel ? HorrorUI.Sangre : HorrorUI.HuesoTenue);
        }
    }

    void AnimarSeleccion(List<Text> items, int indice)
    {
        // Pequeño temblor del elemento seleccionado, como imagen de cinta gastada
        for (int i = 0; i < items.Count; i++)
        {
            Vector2 p = items[i].rectTransform.anchoredPosition;
            p.x = 180 + (i == indice && Random.value < 0.08f ? Random.Range(-3f, 3f) : 0f);
            items[i].rectTransform.anchoredPosition = p;
        }
    }

    void ElegirPrincipal()
    {
        Sonar(golpe, 0.8f);
        Vibrar(0.3f, 0.1f, 0.15f);
        switch (sel)
        {
            case 0: PlayerPrefs.SetInt("partida_guardada", 1); StartCoroutine(Cargar()); break;
            case 1: if (HayPartida) StartCoroutine(Cargar()); break;
            case 2: MostrarPantalla(Pantalla.Controles); break;
            case 3: selOpc = 0; MostrarPantalla(Pantalla.Opciones); break;
            case 4:
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
                break;
        }
    }

    // ------------------------------------------------------------------ Opciones

    static string Barra(float v01)
    {
        int n = Mathf.RoundToInt(Mathf.Clamp01(v01) * 10);
        return new string('|', n) + new string('·', 10 - n);
    }

    void RefrescarOpciones()
    {
        string[] nombres = { "BRILLO", "VOLUMEN", "SENSIBILIDAD", "VIBRACIÓN", "VOLVER" };
        string[] valores =
        {
            "◄   " + Barra((brillo - 0.5f) / 1f) + "   ►",
            "◄   " + Barra(volumen) + "   ►",
            "◄   " + Barra((sensibilidad - 40f) / 200f) + "   ►",
            "◄   " + (vibracion ? "SÍ" : "NO") + "   ►",
            "",
        };
        for (int i = 0; i < itemsOpciones.Count; i++)
        {
            Color c = i == selOpc ? HorrorUI.Sangre : HorrorUI.HuesoTenue;
            itemsOpciones[i].text = (i == selOpc ? "▸  " : "    ") + nombres[i];
            itemsOpciones[i].color = c;
            valoresOpciones[i].text = valores[i];
            valoresOpciones[i].color = c;
        }
    }

    void AjustarOpcion(int d)
    {
        switch (selOpc)
        {
            case 0: brillo = Mathf.Clamp(brillo + d * 0.1f, 0.5f, 1.5f); break;
            case 1: volumen = Mathf.Clamp01(volumen + d * 0.1f); AudioListener.volume = volumen; break;
            case 2: sensibilidad = Mathf.Clamp(sensibilidad + d * 20f, 40f, 240f); break;
            case 3: vibracion = !vibracion; if (vibracion) Vibrar(0.5f, 0.5f, 0.2f); break;
            default: return;
        }
        Sonar(clic, 0.6f);
        RefrescarOpciones();
    }

    void GuardarOpciones()
    {
        PlayerPrefs.SetFloat("brillo", brillo);
        PlayerPrefs.SetFloat("volumen", volumen);
        PlayerPrefs.SetFloat("sensibilidad", sensibilidad);
        PlayerPrefs.SetInt("vibracion", vibracion ? 1 : 0);
        PlayerPrefs.Save();
    }

    // ------------------------------------------------------------------ Utilidades

    void Sonar(AudioClip c, float vol)
    {
        if (c != null) srcSfx.PlayOneShot(c, vol);
    }

    void Vibrar(float bajo, float alto, float s)
    {
        if (vibracion) HorrorInput.Vibrar(bajo, alto, s, this);
    }

    IEnumerator Cargar()
    {
        pantalla = Pantalla.Cargando;
        GuardarOpciones();
        srcEstatica.volume = 0.5f;
        VHSOverlay.Glitch(1f, 1.2f);

        AsyncOperation op = SceneManager.LoadSceneAsync(escenaJuego);
        op.allowSceneActivation = false;

        float t = 0;
        while (t < 1.2f)
        {
            t += Time.unscaledDeltaTime;
            fundido.color = new Color(0, 0, 0, Mathf.Clamp01(t / 1.2f));
            srcDrone.volume = Mathf.Lerp(0.45f, 0f, t / 1.2f);
            yield return null;
        }
        while (op.progress < 0.9f) yield return null;
        op.allowSceneActivation = true;
    }
}
