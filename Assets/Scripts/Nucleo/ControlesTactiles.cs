using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Controles táctiles para jugar en el CELULAR (navegador o app). Crean un "control de Xbox virtual":
/// stick izquierdo para caminar, arrastrar el lado derecho para mirar, y botones A/B/X/Y, golpe (RT),
/// correr (LB), curar y pausa. Como el juego ya entiende el control de Xbox, todo funciona igual.
/// Solo aparecen en pantallas táctiles.
/// </summary>
public class ControlesTactiles : MonoBehaviour
{
    static ControlesTactiles inst;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Iniciar()
    {
        bool tactil = Application.isMobilePlatform || (Input.touchSupported && Application.platform == RuntimePlatform.WebGLPlayer && Screen.width < 1400);
#if UNITY_EDITOR
        tactil = tactil || PlayerPrefs.GetInt("probar_tactil", 0) == 1;
#endif
        if (!tactil || inst != null) return;
        GameObject g = new GameObject("ControlesTactiles");
        DontDestroyOnLoad(g);
        inst = g.AddComponent<ControlesTactiles>();
    }

    Canvas canvas;

    void Start()
    {
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            DontDestroyOnLoad(es);
        }
        canvas = HorrorUI.CrearCanvas("Tactil_Canvas", 900, transform);
        CanvasScaler sc = canvas.GetComponent<CanvasScaler>();
        if (sc != null) { sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1280, 720); sc.matchWidthOrHeight = 0.5f; }
        if (canvas.GetComponent<GraphicRaycaster>() == null) canvas.gameObject.AddComponent<GraphicRaycaster>();

        // Mirar: toda la mitad derecha es una "zona" que funciona como stick derecho
        Zona("Mirar", new Vector2(0.5f, 0f), new Vector2(1f, 1f), "<Gamepad>/rightStick", 140f, 0f);
        // Caminar
        Stick("Caminar", new Vector2(0f, 0f), new Vector2(170, 170), 150, "<Gamepad>/leftStick", 80f);
        // Botones (abajo a la derecha, como en un control)
        Boton("A", "<Gamepad>/buttonSouth", new Vector2(-150, 90), 90, new Color(0.25f, 0.6f, 0.3f, 0.55f));
        Boton("B", "<Gamepad>/buttonEast", new Vector2(-60, 180), 80, new Color(0.7f, 0.2f, 0.18f, 0.55f));
        Boton("X", "<Gamepad>/buttonWest", new Vector2(-240, 180), 80, new Color(0.2f, 0.35f, 0.75f, 0.55f));
        Boton("Y", "<Gamepad>/buttonNorth", new Vector2(-150, 270), 80, new Color(0.8f, 0.65f, 0.15f, 0.55f));
        Boton("GOLPE", "<Gamepad>/rightTrigger", new Vector2(-330, 90), 100, new Color(0.55f, 0.1f, 0.1f, 0.6f));
        Boton("CORRER", "<Gamepad>/leftShoulder", new Vector2(330, 90), 90, new Color(0.3f, 0.3f, 0.3f, 0.5f), true);
        Boton("CURAR", "<Gamepad>/dpad/up", new Vector2(-60, 360), 70, new Color(0.3f, 0.5f, 0.3f, 0.5f));
        Boton("ARMA", "<Gamepad>/dpad/right", new Vector2(-240, 360), 70, new Color(0.4f, 0.4f, 0.4f, 0.5f));
        Boton("II", "<Gamepad>/start", new Vector2(-60, -60), 60, new Color(0.2f, 0.2f, 0.2f, 0.5f), false, true);
    }

    RectTransform Rect(string n, Transform padre)
    {
        GameObject g = new GameObject(n, typeof(RectTransform));
        g.transform.SetParent(padre, false);
        return (RectTransform)g.transform;
    }

    void Zona(string n, Vector2 min, Vector2 max, string ruta, float rango, float alfa)
    {
        RectTransform r = Rect(n, canvas.transform);
        r.anchorMin = min; r.anchorMax = max; r.offsetMin = r.offsetMax = Vector2.zero;
        Image img = r.gameObject.AddComponent<Image>(); img.color = new Color(1, 1, 1, alfa);
        OnScreenStick s = r.gameObject.AddComponent<OnScreenStick>();
        s.controlPath = ruta; s.movementRange = rango;
        s.behaviour = OnScreenStick.Behaviour.RelativePositionWithStaticOrigin;
    }

    void Stick(string n, Vector2 ancla, Vector2 pos, float tam, string ruta, float rango)
    {
        RectTransform fondo = Rect(n + "_Fondo", canvas.transform);
        fondo.anchorMin = fondo.anchorMax = ancla; fondo.anchoredPosition = pos; fondo.sizeDelta = new Vector2(tam * 1.6f, tam * 1.6f);
        Image f = fondo.gameObject.AddComponent<Image>(); f.color = new Color(1f, 1f, 1f, 0.12f);
        RectTransform perilla = Rect(n, fondo);
        perilla.anchorMin = perilla.anchorMax = new Vector2(0.5f, 0.5f); perilla.sizeDelta = new Vector2(tam * 0.7f, tam * 0.7f);
        Image p = perilla.gameObject.AddComponent<Image>(); p.color = new Color(0.85f, 0.8f, 0.7f, 0.45f);
        OnScreenStick s = perilla.gameObject.AddComponent<OnScreenStick>();
        s.controlPath = ruta; s.movementRange = rango;
    }

    void Boton(string texto, string ruta, Vector2 pos, float tam, Color c, bool izquierda = false, bool arriba = false)
    {
        RectTransform r = Rect("Boton_" + texto, canvas.transform);
        Vector2 a = new Vector2(izquierda ? 0f : 1f, arriba ? 1f : 0f);
        r.anchorMin = r.anchorMax = a;
        r.anchoredPosition = new Vector2(izquierda ? Mathf.Abs(pos.x) - 120f : pos.x, pos.y);
        r.sizeDelta = new Vector2(tam, tam);
        Image img = r.gameObject.AddComponent<Image>(); img.color = c;
        OnScreenButton b = r.gameObject.AddComponent<OnScreenButton>(); b.controlPath = ruta;
        Text t = HorrorUI.CrearTexto(r, "Texto", texto, texto.Length > 2 ? 16 : 30, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
        t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one;
        t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
    }
}
