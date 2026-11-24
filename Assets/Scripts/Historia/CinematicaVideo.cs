using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Reproduce una cinemática renderizada en Blender (StreamingAssets/Cinematicas/*.mp4) a pantalla completa,
/// con subtítulos sincronizados. Se puede saltar manteniendo [Start] / [Esc].
/// Va debajo del filtro VHS del juego, así se ve como parte de la misma cinta.
/// </summary>
public static class CinematicaVideo
{
    public struct Sub
    {
        public float desde, hasta; public string quien, texto;
        public Sub(float d, float h, string q, string t) { desde = d; hasta = h; quien = q; texto = t; }
    }

    public static IEnumerator Reproducir(string archivo, Sub[] subs)
    {
        string ruta = System.IO.Path.Combine(Application.streamingAssetsPath, "Cinematicas", archivo);
        Canvas c = HorrorUI.CrearCanvas("Cinematica_" + archivo, 450);
        Image negro = HorrorUI.CrearImagen(c.transform, "Negro", Color.black);
        HorrorUI.Estirar(negro.rectTransform);
        RawImage pantalla = new GameObject("Video", typeof(RectTransform)).AddComponent<RawImage>();
        pantalla.transform.SetParent(c.transform, false);
        HorrorUI.Estirar(pantalla.rectTransform);
        AspectRatioFitter ar = pantalla.gameObject.AddComponent<AspectRatioFitter>();
        ar.aspectMode = AspectRatioFitter.AspectMode.FitInParent; ar.aspectRatio = 16f / 9f;
        pantalla.color = Color.black;

        Text sub = HorrorUI.CrearTexto(c.transform, "Subtitulo", "", 34, HorrorUI.Hueso, TextAnchor.LowerCenter);
        HorrorUI.Colocar(sub.rectTransform, new Vector2(0.5f, 0f), new Vector2(0, 90), new Vector2(1500, 120));
        sub.supportRichText = true;
        Outline ol = sub.gameObject.AddComponent<Outline>(); ol.effectColor = Color.black; ol.effectDistance = new Vector2(2, -2);
        Text saltar = HorrorUI.CrearTexto(c.transform, "Saltar", "", 20, HorrorUI.HuesoTenue, TextAnchor.LowerRight);
        HorrorUI.Colocar(saltar.rectTransform, new Vector2(1f, 0f), new Vector2(-60, 30), new Vector2(600, 30));

        GameObject g = new GameObject("VideoPlayer");
        g.transform.SetParent(c.transform, false);
        VideoPlayer vp = g.AddComponent<VideoPlayer>();
        AudioSource audio = g.AddComponent<AudioSource>();
        audio.spatialBlend = 0f; audio.volume = 1f;
        vp.playOnAwake = false;
        vp.source = VideoSource.Url;
        vp.url = ruta;
        vp.renderMode = VideoRenderMode.RenderTexture;
        RenderTexture rt = new RenderTexture(1280, 720, 0);
        vp.targetTexture = rt;
        pantalla.texture = rt;
        vp.audioOutputMode = VideoAudioOutputMode.AudioSource;
        vp.EnableAudioTrack(0, true);
        vp.SetTargetAudioSource(0, audio);
        bool error = false;
        vp.errorReceived += (v, m) => { error = true; Debug.LogWarning("[Cinemática] " + m); };
        vp.Prepare();
        float t0 = Time.realtimeSinceStartup;
        while (!vp.isPrepared && !error && Time.realtimeSinceStartup - t0 < 8f) yield return null;
        if (!vp.isPrepared || error)
        {
            Debug.LogWarning("[Cinemática] No se pudo abrir " + ruta);
            Object.Destroy(c.gameObject); rt.Release();
            yield break;
        }
        MusicaFondo.Desvanecer(1f);
        vp.Play();
        pantalla.color = Color.white;
        float mantener = 0f;
        double duracion = vp.length > 0 ? vp.length : 60;
        while (vp.isPlaying || vp.time < 0.2)
        {
            float t = (float)vp.time;
            string s = "";
            if (subs != null)
                foreach (Sub x in subs)
                    if (t >= x.desde && t <= x.hasta)
                        s = string.IsNullOrEmpty(x.quien) ? "<i>" + x.texto + "</i>" : "<color=#c9b98a>" + x.quien + ":</color>  " + x.texto;
            sub.text = s;
            if (t > 2f) saltar.text = "Mantén [" + HorrorInput.Boton("pausa") + "] para saltar";
            bool sost = (UnityEngine.InputSystem.Gamepad.current != null && UnityEngine.InputSystem.Gamepad.current.startButton.isPressed) ||
                        (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.isPressed);
            mantener = sost ? mantener + Time.unscaledDeltaTime : 0f;
            if (mantener > 0.8f) break;
            if (t >= duracion - 0.05) break;
            yield return null;
        }
        vp.Stop();
        Object.Destroy(c.gameObject);
        rt.Release();
    }
}
