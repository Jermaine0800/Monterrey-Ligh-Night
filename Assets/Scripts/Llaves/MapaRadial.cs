using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mapa redondo en la esquina superior derecha. Gira con la cámara (arriba = hacia donde miras).
/// Dibuja el relieve del cerro (sombreado de alturas) y marca los 5 lugares de las llaves:
/// ◆ dorado = falta la llave, ◇ gris = ya la tienes, ✚ = la puerta del bucle. El que toca ahorita parpadea.
/// Si un lugar queda fuera del mapa, su marca se queda en la orilla apuntando hacia allá.
/// </summary>
public class MapaRadial : MonoBehaviour
{
    public float radioMundo = 160f;      // metros que caben del centro a la orilla
    public float tamano = 250f;          // píxeles (en 1920x1080)

    RectTransform marco, contenido;
    RawImage relieve;
    Text flecha;
    readonly List<(Transform lugar, Text marca, SitioLlave sitio)> marcas = new List<(Transform, Text, SitioLlave)>();
    Transform jugador;
    Terrain t;
    Image borde;

    void Start()
    {
        Canvas c = HorrorUI.CrearCanvas("Mapa_Canvas", 122, transform);

        // Marco circular con máscara
        Sprite circulo = Circulo(256);
        borde = HorrorUI.CrearImagen(c.transform, "Borde", new Color(HorrorUI.Hueso.r, HorrorUI.Hueso.g, HorrorUI.Hueso.b, 0.55f));
        borde.sprite = circulo;
        Fijar(borde.rectTransform, new Vector2(-70 - tamano / 2, -170 - tamano / 2), new Vector2(tamano + 8, tamano + 8));

        Image mascara = HorrorUI.CrearImagen(c.transform, "Mascara", new Color(0.03f, 0.035f, 0.045f, 0.85f));
        mascara.sprite = circulo;
        Fijar(mascara.rectTransform, new Vector2(-70 - tamano / 2, -170 - tamano / 2), new Vector2(tamano, tamano));
        mascara.gameObject.AddComponent<Mask>().showMaskGraphic = true;
        marco = mascara.rectTransform;

        contenido = HorrorUI.Rect(new GameObject("Contenido"));
        contenido.SetParent(marco, false);
        contenido.anchorMin = contenido.anchorMax = new Vector2(0.5f, 0.5f);
        contenido.sizeDelta = Vector2.zero;

        t = Terrain.activeTerrain;
        if (t != null)
        {
            relieve = HorrorUI.CrearRaw(contenido, "Relieve", Relieve(t, 256), new Color(1, 1, 1, 0.75f));
            Vector3 tam = t.terrainData.size;
            float escala = (tamano / 2f) / radioMundo;
            relieve.rectTransform.anchorMin = relieve.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            relieve.rectTransform.pivot = Vector2.zero;
            relieve.rectTransform.sizeDelta = new Vector2(tam.x, tam.z) * escala;
        }

        flecha = HorrorUI.CrearTexto(marco, "Jugador", "▲", 26, Color.white, TextAnchor.MiddleCenter);
        flecha.rectTransform.anchorMin = flecha.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        flecha.rectTransform.sizeDelta = new Vector2(30, 30);
        flecha.rectTransform.anchoredPosition = Vector2.zero;

        Text n = HorrorUI.CrearTexto(c.transform, "Norte", "MAPA", 18, HorrorUI.HuesoTenue, TextAnchor.MiddleCenter);
        Fijar(n.rectTransform, new Vector2(-70 - tamano / 2, -170 - tamano - 18), new Vector2(120, 24));

        MisionManager mm = FindAnyObjectByType<MisionManager>();
        if (mm != null && mm.sitios != null)
            foreach (SitioLlave s in mm.sitios) if (s != null) Agregar(s.transform, s, "◆");
        if (mm != null && mm.puertaBucle != null) Agregar(mm.puertaBucle.transform, null, "✚");
    }

    void Agregar(Transform lugar, SitioLlave sitio, string simbolo)
    {
        Text m = HorrorUI.CrearTexto(marco, "Marca_" + lugar.name, simbolo, 24, new Color(0.95f, 0.78f, 0.38f), TextAnchor.MiddleCenter, FontStyle.Bold);
        m.rectTransform.anchorMin = m.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        m.rectTransform.sizeDelta = new Vector2(30, 30);
        marcas.Add((lugar, m, sitio));
    }

    static void Fijar(RectTransform rt, Vector2 centro, Vector2 tam)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = centro;
        rt.sizeDelta = tam;
    }

    void LateUpdate()
    {
        bool ver = HorrorGameState.PuedeJugar;
        marco.gameObject.SetActive(ver);
        borde.enabled = ver;
        if (!ver) return;
        if (jugador == null) { PlayerMovement pm = FindAnyObjectByType<PlayerMovement>(); if (pm) jugador = pm.transform; if (jugador == null) return; }
        Camera cam = Camera.main;
        float yaw = cam != null ? cam.transform.eulerAngles.y : jugador.eulerAngles.y;
        float escala = (tamano / 2f) / radioMundo;

        // El mundo gira alrededor del jugador
        contenido.localRotation = Quaternion.Euler(0, 0, yaw);
        if (relieve != null && t != null)
        {
            Vector3 l = jugador.position - t.transform.position;
            relieve.rectTransform.anchoredPosition = new Vector2(-l.x, -l.z) * escala;
        }

        LlavesBucle hud = LlavesBucle.Instancia;
        Transform actual = hud != null ? hud.destino : null;
        foreach (var m in marcas)
        {
            if (m.lugar == null) continue;
            Vector3 d = m.lugar.position - jugador.position;
            Vector2 p = Quaternion.Euler(0, 0, yaw) * new Vector2(d.x, d.z) * escala;
            float max = tamano / 2f - 14f;
            if (p.magnitude > max) p = p.normalized * max;
            m.marca.rectTransform.anchoredPosition = p;

            bool hecha = m.sitio != null && m.sitio.Completado;
            bool esActual = actual != null && (actual == m.lugar || actual.IsChildOf(m.lugar));
            m.marca.text = m.sitio == null ? "✚" : hecha ? "◇" : "◆";
            Color c = hecha ? new Color(0.6f, 0.6f, 0.6f, 0.6f) : m.sitio == null ? new Color(0.75f, 0.85f, 1f) : new Color(0.95f, 0.78f, 0.38f);
            if (esActual) c.a = 0.4f + 0.6f * Mathf.Abs(Mathf.Sin(Time.time * 3f));
            m.marca.color = c;
            m.marca.fontSize = esActual ? 30 : 22;
        }
    }

    // ------------------------------------------------------------------ Texturas generadas

    static Sprite Circulo(int n)
    {
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        float r = n / 2f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(r - d)));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
    }

    /// <summary>Relieve del terreno: sombreado por pendiente + curvas de nivel, en tonos de tinta vieja.</summary>
    static Texture2D Relieve(Terrain t, int n)
    {
        TerrainData td = t.terrainData;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        float alto = td.size.y;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = (x + 0.5f) / n, v = (y + 0.5f) / n;
                float h = td.GetInterpolatedHeight(u, v) / Mathf.Max(1f, alto);
                Vector3 nor = td.GetInterpolatedNormal(u, v);
                float luz = Mathf.Clamp01(Vector3.Dot(nor, new Vector3(-0.5f, 0.8f, 0.3f).normalized));
                float curva = Mathf.Abs(Mathf.Repeat(h * 25f, 1f) - 0.5f) < 0.04f ? 0.25f : 0f;
                float g = 0.15f + luz * 0.35f + h * 0.25f + curva;
                tex.SetPixel(x, y, new Color(g * 0.95f, g, g * 0.85f, 1f));
            }
        // árboles como puntitos oscuros
        foreach (TreeInstance ti in td.treeInstances)
        {
            int px = (int)(ti.position.x * n), py = (int)(ti.position.z * n);
            if (px >= 0 && py >= 0 && px < n && py < n) tex.SetPixel(px, py, new Color(0.05f, 0.09f, 0.05f, 1f));
        }
        tex.Apply();
        return tex;
    }
}
