using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Cinemática cuando derrotas a un monstruo: cámara lenta, la cámara le da la vuelta,
/// se hunde en la tierra entre glitches.
///  · Antes de tener las 5 llaves: "...no está muerto. Va a volver." (regresa en ~45 s)
///  · Con las 5 llaves: muere de verdad — "DESTERRADA PARA SIEMPRE".
/// </summary>
public static class CinematicaMuerte
{
    public static IEnumerator Reproducir(EnemigoBase e, bool final)
    {
        Camera cam = Camera.main;
        CameraController cc = Object.FindAnyObjectByType<CameraController>();
        HorrorGameState.Modo previo = HorrorGameState.modo;
        if (HorrorGameState.modo == HorrorGameState.Modo.Jugando) HorrorGameState.modo = HorrorGameState.Modo.Dialogo;
        if (cc != null) cc.enabled = false;
        Vector3 pc0 = cam != null ? cam.transform.position : Vector3.zero;
        Quaternion rc0 = cam != null ? cam.transform.rotation : Quaternion.identity;

        Canvas c = HorrorUI.CrearCanvas("Muerte_" + e.name, 430);
        Image rojo = HorrorUI.CrearImagen(c.transform, "Rojo", new Color(0.5f, 0f, 0f, 0f));
        HorrorUI.Estirar(rojo.rectTransform);
        Text t = HorrorUI.CrearTexto(c.transform, "Titulo", "", 44, HorrorUI.Hueso, TextAnchor.MiddleCenter);
        HorrorUI.Colocar(t.rectTransform, new Vector2(0.5f, 0.32f), Vector2.zero, new Vector2(1500, 160));
        t.supportRichText = true;

        AudioSource a = c.gameObject.AddComponent<AudioSource>();
        a.spatialBlend = 0f; a.ignoreListenerPause = true;
        a.PlayOneShot(ProceduralAudio.Grito(e.name.GetHashCode() + 13), final ? 1f : 0.7f);
        a.PlayOneShot(ProceduralAudio.GolpeGrave(), 1f);
        VHSOverlay.Glitch(1f, final ? 2.5f : 1.2f);
        HorrorInput.Vibrar(1f, 0.8f, final ? 1.2f : 0.5f, cc != null ? (MonoBehaviour)cc : e);

        float dur = final ? 4.2f : 2.2f;
        Time.timeScale = final ? 0.2f : 0.35f;
        Vector3 centro = e.CentroPublico;
        Vector3 desde = cam != null ? cam.transform.position - centro : Vector3.back * 3f; desde.y = 0;
        float ang0 = Mathf.Atan2(desde.x, desde.z);
        Transform tr = e.transform;
        Vector3 pos0 = tr.position;
        for (float k = 0; k < dur; k += Time.unscaledDeltaTime)
        {
            float u = k / dur;
            if (cam != null)
            {
                float ang = ang0 + u * (final ? 2.2f : 1.1f);
                float r = Mathf.Lerp(4.2f, 2.6f, u);
                cam.transform.position = centro + new Vector3(Mathf.Sin(ang) * r, Mathf.Lerp(0.4f, 1.4f, u), Mathf.Cos(ang) * r);
                cam.transform.rotation = Quaternion.LookRotation(centro - cam.transform.position);
            }
            // se hunde en la tierra y parpadea
            if (u > 0.35f) tr.position = pos0 + Vector3.down * Mathf.SmoothStep(0f, final ? 2.2f : 1.4f, (u - 0.35f) / 0.65f);
            foreach (Renderer rd in e.GetComponentsInChildren<Renderer>()) rd.enabled = Random.value > u * 0.6f;
            rojo.color = new Color(0.45f, 0f, 0f, Mathf.Sin(u * Mathf.PI) * (final ? 0.35f : 0.18f));
            if (u > 0.3f && t.text == "")
                t.text = final ? e.nombre.ToUpper() + "\n<size=26>" + (e is ElAsomado ? "desterrado" : "desterrada") + " para siempre</size>" : "<size=30><i>...no está muerto. Va a volver.</i></size>";
            yield return null;
        }
        Time.timeScale = 1f;
        tr.position = pos0;
        Object.Destroy(c.gameObject);
        if (cam != null) cam.transform.SetPositionAndRotation(pc0, rc0);
        if (cc != null) cc.enabled = true;
        if (HorrorGameState.modo == HorrorGameState.Modo.Dialogo) HorrorGameState.modo = previo;
        if (final) DialogoUI.Susurrar("", "Uno menos. ...¿se acabó?", 3f);
    }
}
