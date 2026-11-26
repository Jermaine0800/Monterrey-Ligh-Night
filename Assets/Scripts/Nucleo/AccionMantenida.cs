using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Acciones de varios pasos manteniendo el botón de acción (prender la fogata, etc.).
/// Muestra el paso actual y una barra. Si sueltas el botón (o te mueves en un paso "quieto"), falla.
/// </summary>
public static class AccionMantenida
{
    static Canvas canvas;
    static Text texto;
    static Image relleno;
    public static bool Ocupado { get; private set; }

    /// <param name="pasoQuieto">índice del paso en el que no te puedes mover (-1 = ninguno)</param>
    public static IEnumerator Ejecutar(Transform jugador, string[] pasos, float[] tiempos, int pasoQuieto, Action<bool> fin)
    {
        Ocupado = true;
        Barra(true);
        float total = 0f; foreach (float t in tiempos) total += t;
        float hecho = 0f;
        for (int p = 0; p < pasos.Length; p++)
        {
            Vector3 donde = jugador.position;
            for (float t = 0f; t < tiempos[p]; t += Time.deltaTime)
            {
                bool movio = (jugador.position - donde).sqrMagnitude > 0.04f;
                if (!HorrorInput.InteractuarMantenido || !HorrorGameState.PuedeJugar || (p == pasoQuieto && movio))
                {
                    Barra(false); Ocupado = false; fin?.Invoke(false); yield break;
                }
                Mostrar(pasos[p], (hecho + t) / total);
                yield return null;
            }
            hecho += tiempos[p];
        }
        Barra(false); Ocupado = false;
        fin?.Invoke(true);
    }

    static void Barra(bool visible)
    {
        if (canvas == null)
        {
            if (!visible) return;
            canvas = HorrorUI.CrearCanvas("AccionMantenida_Canvas", 140, null);
            texto = HorrorUI.CrearTexto(canvas.transform, "Paso", "", 26, HorrorUI.Hueso, TextAnchor.MiddleCenter);
            HorrorUI.Colocar(texto.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -150), new Vector2(1000, 40));
            Image fondo = HorrorUI.CrearImagen(canvas.transform, "Fondo", new Color(0f, 0f, 0f, 0.6f));
            HorrorUI.Colocar(fondo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -190), new Vector2(360, 14));
            relleno = HorrorUI.CrearImagen(fondo.transform, "Relleno", new Color(1f, 0.6f, 0.2f, 0.9f));
            relleno.rectTransform.anchorMin = Vector2.zero; relleno.rectTransform.anchorMax = new Vector2(0, 1);
            relleno.rectTransform.pivot = new Vector2(0, 0.5f);
            relleno.rectTransform.anchoredPosition = Vector2.zero; relleno.rectTransform.sizeDelta = Vector2.zero;
        }
        canvas.gameObject.SetActive(visible);
    }

    static void Mostrar(string paso, float p01)
    {
        if (texto == null) return;
        texto.text = HorrorInput.Traducir(paso + "   (mantén [A])");
        relleno.rectTransform.sizeDelta = new Vector2(360f * Mathf.Clamp01(p01), 0);
    }
}
