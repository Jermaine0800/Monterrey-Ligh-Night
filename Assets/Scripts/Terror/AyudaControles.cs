using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tarjeta de controles (como los "tips" de Resident Evil / Outlast):
/// - Aparece sola los primeros segundos de juego y se desvanece.
/// - Se vuelve a abrir/cerrar con [View] en el control o [F1] en el teclado.
/// - Siempre hay un recordatorio chiquito abajo a la derecha: "[F1] Controles".
/// Cambia sola los nombres de los botones según uses control o teclado.
/// </summary>
public class AyudaControles : MonoBehaviour
{
    Text titulo, cuerpo, recordatorio;
    CanvasGroup grupo;
    float visibleHasta;
    bool fijo, ultimoControl, primeraVez = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) => Asegurar();
        Asegurar();
    }

    static void Asegurar()
    {
        if (FindAnyObjectByType<PlayerMovement>() == null || FindAnyObjectByType<AyudaControles>() != null) return;
        new GameObject("AyudaControles").AddComponent<AyudaControles>();
    }

    void Start()
    {
        Canvas c = HorrorUI.CrearCanvas("Controles_Canvas", 115, transform);
        GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        panel.transform.SetParent(c.transform, false);
        RectTransform rp = panel.GetComponent<RectTransform>();
        rp.anchorMin = rp.anchorMax = rp.pivot = new Vector2(0f, 0f);
        rp.anchoredPosition = new Vector2(40, 140);
        rp.sizeDelta = new Vector2(520, 430);
        panel.GetComponent<Image>().color = new Color(0, 0, 0, 0.72f);
        panel.GetComponent<Image>().raycastTarget = false;
        grupo = panel.GetComponent<CanvasGroup>();
        grupo.alpha = 0f;

        titulo = Texto(panel.transform, 30, TextAnchor.UpperLeft, new Vector2(24, -18), new Vector2(480, 40));
        titulo.color = HorrorUI.Sangre;
        cuerpo = Texto(panel.transform, 23, TextAnchor.UpperLeft, new Vector2(24, -64), new Vector2(480, 360));
        cuerpo.lineSpacing = 1.15f;

        recordatorio = Texto(c.transform, 18, TextAnchor.LowerRight, Vector2.zero, new Vector2(320, 30));
        RectTransform rr = recordatorio.rectTransform;
        rr.anchorMin = rr.anchorMax = rr.pivot = new Vector2(1f, 0f);
        rr.anchoredPosition = new Vector2(-30, 18);
        recordatorio.color = HorrorUI.HuesoTenue;

        ultimoControl = !HorrorInput.UsandoControl;
    }

    static Text Texto(Transform padre, int tam, TextAnchor a, Vector2 pos, Vector2 size)
    {
        GameObject g = new GameObject("Texto", typeof(RectTransform));
        g.transform.SetParent(padre, false);
        Text t = g.AddComponent<Text>();
        t.font = HorrorUI.Fuente; t.fontSize = tam; t.alignment = a; t.color = HorrorUI.Hueso; t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform rt = t.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        return t;
    }

    void Update()
    {
        bool jugando = HorrorGameState.PuedeJugar && !DialogoUI.Activo && !DocumentoUI.Abierto;

        // La primera vez que de verdad puedes moverte, se muestra 20 s
        if (primeraVez && jugando && Time.timeSinceLevelLoad > 3f) { primeraVez = false; visibleHasta = Time.unscaledTime + 20f; }

        if (HorrorInput.VerControles) { fijo = !fijo; visibleHasta = 0f; }

        bool control = HorrorInput.UsandoControl;
        if (control != ultimoControl) { ultimoControl = control; Escribir(control); }

        bool ver = (fijo || Time.unscaledTime < visibleHasta) && (jugando || fijo);
        grupo.alpha = Mathf.MoveTowards(grupo.alpha, ver ? 1f : 0f, Time.unscaledDeltaTime * 2.5f);
        recordatorio.enabled = jugando;
    }

    void Escribir(bool c)
    {
        titulo.text = c ? "CONTROLES (XBOX)" : "CONTROLES (TECLADO)";
        string F(string tecla, string que) => "<b>" + tecla + "</b>   " + que;
        cuerpo.text = c
            ? string.Join("\n", new[] {
                F("Stick izq.", "caminar   ·   L3 / LB  correr"),
                F("Stick der.", "mirar   ·   R3  cámara 1ª / 3ª"),
                F("A", "acción: recoger, leer, abrir, esconderse"),
                F("B", "agacharse / salir del escondite"),
                F("X", "linterna"),
                F("Y", "maletín (inventario)"),
                F("RT", "golpear con el arma"),
                F("◀ ▶", "cambiar arma   ·   ▲  curarse"),
                F("Start", "pausa   ·   View  esta ayuda") })
            : string.Join("\n", new[] {
                F("WASD", "caminar   ·   Shift  correr"),
                F("Ratón", "mirar   ·   V  cámara 1ª / 3ª"),
                F("E", "acción: recoger, leer, abrir, esconderse"),
                F("C / Ctrl", "agacharse / salir del escondite"),
                F("F", "linterna"),
                F("Tab", "maletín (inventario)"),
                F("Clic izq.", "golpear con el arma"),
                F("Rueda / Q R", "cambiar arma   ·   H  curarse"),
                F("Esc", "pausa   ·   F1 / G  esta ayuda") });
        cuerpo.supportRichText = true;
        recordatorio.text = "[" + HorrorInput.Boton("controles") + "] Controles";
    }
}
