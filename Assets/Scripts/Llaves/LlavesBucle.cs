using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// El bucle temporal: cuántas llaves llevas (de 5), en qué bucle vas (cada vez que mueres vuelve a empezar)
/// y el HUD de la cinta que te dice hacia dónde queda el siguiente lugar.
/// Las llaves NO se pierden al morir: es lo único que sobrevive al bucle.
/// </summary>
public class LlavesBucle : MonoBehaviour, IReiniciable
{
    public static LlavesBucle Instancia { get; private set; }
    public const int Total = 5;

    public int Llaves { get; private set; }
    public int Bucle { get; private set; } = 1;
    public event Action<int> AlConseguirLlave;

    /// <summary>Lo que se marca en el HUD (lugar actual). Lo pone la historia.</summary>
    public Transform destino;
    public string nombreDestino = "";

    Text textoLlaves, textoDestino, textoBucle;
    float finMensajeBucle;

    void Awake() { Instancia = this; }

    void Start()
    {
        Canvas c = HorrorUI.CrearCanvas("Llaves_Canvas", 120, transform);
        textoLlaves = HorrorUI.CrearTexto(c.transform, "Llaves", "", 26, HorrorUI.Hueso, TextAnchor.UpperLeft);
        HorrorUI.Colocar(textoLlaves.rectTransform, new Vector2(0f, 1f), new Vector2(230, -120), new Vector2(420, 40));
        textoDestino = HorrorUI.CrearTexto(c.transform, "Destino", "", 22, HorrorUI.HuesoTenue, TextAnchor.UpperLeft);
        HorrorUI.Colocar(textoDestino.rectTransform, new Vector2(0f, 1f), new Vector2(230, -152), new Vector2(420, 36));
        textoBucle = HorrorUI.CrearTexto(c.transform, "Bucle", "", 44, new Color(1, 1, 1, 0), TextAnchor.MiddleCenter);
        HorrorUI.Estirar(textoBucle.rectTransform);
        Refrescar();
    }

    public void Sumar()
    {
        Llaves = Mathf.Min(Total, Llaves + 1);
        Refrescar();
        AlConseguirLlave?.Invoke(Llaves);
    }

    void Refrescar()
    {
        if (textoLlaves == null) return;
        string s = "LLAVES  ";
        for (int i = 0; i < Total; i++) s += i < Llaves ? "■ " : "□ ";
        textoLlaves.text = s + " " + Llaves + "/" + Total;
    }

    void Update()
    {
        if (textoDestino == null) return;
        bool jugando = HorrorGameState.PuedeJugar;
        textoLlaves.enabled = jugando;
        bool mostrar = destino != null && HorrorGameState.PuedeJugar && Camera.main != null;
        if (!mostrar) { textoDestino.text = ""; }
        else
        {
            Transform cam = Camera.main.transform;
            Vector3 d = destino.position - cam.position; d.y = 0;
            Vector3 f = cam.forward; f.y = 0;
            float ang = Vector3.SignedAngle(f, d, Vector3.up);
            string flecha = Mathf.Abs(ang) < 25f ? "▲" : Mathf.Abs(ang) > 140f ? "▼" : ang > 0 ? "▶" : "◀";
            textoDestino.text = flecha + "  " + nombreDestino + "   " + Mathf.RoundToInt(d.magnitude) + " m";
        }

        if (textoBucle.color.a > 0f && Time.time > finMensajeBucle)
            textoBucle.color = new Color(1, 1, 1, Mathf.Max(0f, textoBucle.color.a - Time.deltaTime * 0.5f));
    }

    /// <summary>Al morir: el bucle se repite (las llaves se quedan).</summary>
    public void Reiniciar()
    {
        Bucle++;
        textoBucle.text = "B U C L E   # " + Bucle.ToString("00") + "\n<size=22>" + Llaves + " de " + Total + " llaves siguen contigo</size>";
        textoBucle.supportRichText = true;
        textoBucle.color = new Color(HorrorUI.Hueso.r, HorrorUI.Hueso.g, HorrorUI.Hueso.b, 1f);
        finMensajeBucle = Time.time + 3f;
    }
}
