using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Pausa con START. Estilo cinta detenida (PAUSE ▌▌).</summary>
public class PauseMenu : MonoBehaviour
{
    public string escenaMenu = "MainMenu";

    GameObject raiz, grupoComandos;
    Text[] items;
    readonly string[] nombres = { "REANUDAR", "SISTEMA DE COMANDOS", "MENÚ PRINCIPAL" };
    int sel;
    bool viendoComandos;
    AudioSource src;
    AudioClip clic;

    void Start()
    {
        src = gameObject.AddComponent<AudioSource>();
        src.ignoreListenerPause = true;
        clic = ProceduralAudio.ClicMenu();
        Construir();
        raiz.SetActive(false);
    }

    void Construir()
    {
        Canvas c = HorrorUI.CrearCanvas("Pausa_Canvas", 400, transform);
        raiz = c.gameObject;

        Image fondo = HorrorUI.CrearImagen(c.transform, "Fondo", new Color(0, 0, 0, 0.82f));
        HorrorUI.Estirar(fondo.rectTransform);

        Text t = HorrorUI.CrearTexto(c.transform, "Pausa", "▌▌  P A U S E", 64, HorrorUI.Hueso, TextAnchor.MiddleLeft, FontStyle.Bold);
        HorrorUI.Colocar(t.rectTransform, new Vector2(0, 1), new Vector2(180, -260), new Vector2(900, 90));

        Image linea = HorrorUI.CrearImagen(c.transform, "Linea", HorrorUI.Sangre);
        HorrorUI.Colocar(linea.rectTransform, new Vector2(0, 1), new Vector2(180, -350), new Vector2(520, 3));

        items = new Text[nombres.Length];
        for (int i = 0; i < nombres.Length; i++)
        {
            items[i] = HorrorUI.CrearTexto(c.transform, nombres[i], nombres[i], 40, HorrorUI.HuesoTenue);
            HorrorUI.Colocar(items[i].rectTransform, new Vector2(0, 1), new Vector2(180, -420 - i * 70), new Vector2(900, 60));
        }

        grupoComandos = new GameObject("Comandos", typeof(RectTransform));
        grupoComandos.transform.SetParent(c.transform, false);
        HorrorUI.Estirar(HorrorUI.Rect(grupoComandos));
        string[] filas =
        {
            "[STICK IZQ]   Caminar",
            "[STICK DER]   Cámara",
            "[RB]          Interactuar / recoger",
            "[Y]           Maletín",
            "[X]           Linterna",
            "[START]       Pausa",
            "[B]           Atrás",
        };
        for (int i = 0; i < filas.Length; i++)
        {
            Text f = HorrorUI.CrearTexto(grupoComandos.transform, "F" + i, filas[i], 30, HorrorUI.Hueso);
            HorrorUI.Colocar(f.rectTransform, new Vector2(0, 1), new Vector2(1000, -420 - i * 50), new Vector2(800, 46));
        }
        grupoComandos.SetActive(false);

        Text ayuda = HorrorUI.CrearTexto(c.transform, "Ayuda", "[A] ACEPTAR      [B] / [START] VOLVER", 24, HorrorUI.HuesoTenue, TextAnchor.MiddleRight);
        HorrorUI.Colocar(ayuda.rectTransform, new Vector2(1, 0), new Vector2(-80, 150), new Vector2(900, 40));
    }

    void Update()
    {
        if (raiz == null) return;

        if (!raiz.activeSelf)
        {
            if (HorrorGameState.PuedeJugar && HorrorInput.Start) Abrir();
            return;
        }

        if (HorrorInput.Start || HorrorInput.Cancelar)
        {
            if (viendoComandos) { viendoComandos = false; grupoComandos.SetActive(false); Sonar(); }
            else Cerrar();
            return;
        }
        if (HorrorInput.Arriba) { sel = (sel + items.Length - 1) % items.Length; Sonar(); }
        if (HorrorInput.Abajo)  { sel = (sel + 1) % items.Length; Sonar(); }
        if (HorrorInput.Aceptar)
        {
            Sonar();
            if (sel == 0) Cerrar();
            else if (sel == 1) { viendoComandos = !viendoComandos; grupoComandos.SetActive(viendoComandos); }
            else IrAlMenu();
        }

        for (int i = 0; i < items.Length; i++)
        {
            items[i].text = (i == sel ? "▸  " : "    ") + nombres[i];
            items[i].color = i == sel ? HorrorUI.Sangre : HorrorUI.HuesoTenue;
        }
    }

    void Abrir()
    {
        sel = 0;
        raiz.SetActive(true);
        HorrorGameState.modo = HorrorGameState.Modo.Pausa;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        VHSOverlay.Glitch(0.5f, 0.2f);
        Sonar();
    }

    void Cerrar()
    {
        raiz.SetActive(false);
        viendoComandos = false;
        grupoComandos.SetActive(false);
        Time.timeScale = 1f;
        AudioListener.pause = false;
        HorrorGameState.modo = HorrorGameState.Modo.Jugando;
    }

    void IrAlMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (Application.CanStreamedLevelBeLoaded(escenaMenu)) SceneManager.LoadScene(escenaMenu);
        else Debug.LogWarning("La escena '" + escenaMenu + "' no está en Build Settings. Usa el menú Monterrey > 1.");
    }

    void Sonar() { if (src != null) src.PlayOneShot(clic, 0.6f); }
}
