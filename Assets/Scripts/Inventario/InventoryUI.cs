using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Inventario estilo maletín (inspirado en Resident Evil / Silent Hill).
/// Mantiene la misma API que antes (hasBackPack, inventario, inventoryPanel) para que
/// BackpackPickup, ItemPickup e InteractableItem sigan funcionando igual.
/// El panel viejo de 6 slots se oculta; la interfaz nueva se construye por código.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("Referencias (igual que antes)")]
    public GameObject inventoryPanel;          // panel viejo: se oculta
    public PlayerInventory inventario;
    public bool hasBackPack = false;

    [Header("Maletín")]
    public string nombreJugador = "INVESTIGADOR";
    public int columnas = 4;
    public float tamanoCelda = 200f;

    /// <summary>Texto del objetivo actual (cámbialo desde cualquier script).</summary>
    public static string Objetivo = "Encuentra la llave de la cabaña";

    public static bool Abierto { get; private set; }

    // UI
    GameObject raiz;
    readonly List<Image> celdas = new List<Image>();
    readonly List<Image> iconos = new List<Image>();
    readonly List<Text> etiquetas = new List<Text>();
    GameObject cursor;
    Text nombreItem, descItem, textoObjetivo, textoCondicion;
    RawImage ecg;
    int sel;
    int filas;

    AudioSource src;
    AudioClip clic, golpe;

    void Start()
    {
        if (inventario == null) inventario = FindFirstObjectByType<PlayerInventory>();
        if (inventario != null) inventario.OnChanged += Refrescar;
        else Debug.LogError("¡No se encontró ningún PlayerInventory en la escena! Asígnalo al personaje.");

        if (inventoryPanel != null) inventoryPanel.SetActive(false);

        src = gameObject.AddComponent<AudioSource>();
        src.ignoreListenerPause = true;
        clic = ProceduralAudio.ClicMenu();
        golpe = ProceduralAudio.GolpeGrave();

        Construir();
        raiz.SetActive(false);
        Abierto = false;
    }

    void OnDestroy()
    {
        if (inventario != null) inventario.OnChanged -= Refrescar;
        Abierto = false;
    }

    // ------------------------------------------------------------------ Construcción

    void Construir()
    {
        int capacidad = inventario != null ? inventario.capacidad : 6;
        filas = Mathf.Max(2, Mathf.CeilToInt(capacidad / (float)columnas));

        Canvas c = HorrorUI.CrearCanvas("Maletin_Canvas", 300);
        raiz = c.gameObject;
        Transform r = c.transform;

        Image fondo = HorrorUI.CrearImagen(r, "Fondo", new Color(0.02f, 0.02f, 0.02f, 0.93f));
        HorrorUI.Estirar(fondo.rectTransform);
        RawImage grano = HorrorUI.CrearRaw(r, "Grano", HorrorUI.Ruido, new Color(1, 1, 1, 0.05f));
        HorrorUI.Estirar(grano.rectTransform);
        grano.uvRect = new Rect(0, 0, 4, 2.25f);

        // ---- Pestañas superiores
        Text pestanas = HorrorUI.CrearTexto(r, "Pestanas", "MAPA        LLAVES        ▣ MALETÍN        ARCHIVOS", 26, HorrorUI.HuesoTenue, TextAnchor.MiddleCenter);
        HorrorUI.Colocar(pestanas.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -70), new Vector2(1200, 40));
        Text mi = HorrorUI.CrearTexto(r, "MiInventario", "Mi inventario", 30, HorrorUI.Hueso, TextAnchor.MiddleCenter);
        HorrorUI.Colocar(mi.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -112), new Vector2(600, 40));
        Image sub = HorrorUI.CrearImagen(r, "Subrayado", HorrorUI.Hueso);
        HorrorUI.Colocar(sub.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -142), new Vector2(160, 2));

        // ---- Tarjeta de identificación (izquierda)
        Image tarjeta = HorrorUI.CrearImagen(r, "Tarjeta", new Color(0.05f, 0.05f, 0.05f, 1));
        HorrorUI.Colocar(tarjeta.rectTransform, new Vector2(0, 1), new Vector2(90, -190), new Vector2(230, 280));
        HorrorUI.CrearMarco(tarjeta.transform, "Marco", HorrorUI.Hueso, 4);
        Text nom = HorrorUI.CrearTexto(tarjeta.transform, "Nombre", nombreJugador, 26, HorrorUI.Hueso, TextAnchor.MiddleCenter, FontStyle.Bold);
        HorrorUI.Colocar(nom.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(220, 40));
        Image foto = HorrorUI.CrearImagen(tarjeta.transform, "Foto", new Color(0.12f, 0.12f, 0.12f));
        HorrorUI.Colocar(foto.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -70), new Vector2(170, 170));
        RawImage fotoRuido = HorrorUI.CrearRaw(foto.transform, "Estatica", HorrorUI.Ruido, new Color(1, 1, 1, 0.25f));
        HorrorUI.Estirar(fotoRuido.rectTransform);
        Text firma = HorrorUI.CrearTexto(tarjeta.transform, "Firma", "∫ ~ ʃ", 26, HorrorUI.HuesoTenue, TextAnchor.MiddleCenter, FontStyle.Italic);
        HorrorUI.Colocar(firma.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(200, 30));

        // ---- Monitor de condición
        Image monitor = HorrorUI.CrearImagen(r, "Monitor", new Color(0.03f, 0.06f, 0.04f, 1));
        HorrorUI.Colocar(monitor.rectTransform, new Vector2(0, 1), new Vector2(350, -230), new Vector2(330, 130));
        HorrorUI.CrearMarco(monitor.transform, "Marco", HorrorUI.Metal, 6);
        RawImage rejilla = HorrorUI.CrearRaw(monitor.transform, "Rejilla", HorrorUI.Scanlines, new Color(0.2f, 1f, 0.3f, 0.25f));
        HorrorUI.Estirar(rejilla.rectTransform, 6);
        rejilla.uvRect = new Rect(0, 0, 1, 30);
        ecg = HorrorUI.CrearRaw(monitor.transform, "ECG", TexturaECG(), HorrorUI.VerdeMonitor);
        HorrorUI.Estirar(ecg.rectTransform, 10);
        textoCondicion = HorrorUI.CrearTexto(monitor.transform, "Estado", "Bien", 30, HorrorUI.VerdeMonitor, TextAnchor.LowerRight, FontStyle.Bold);
        HorrorUI.Colocar(textoCondicion.rectTransform, new Vector2(1, 0), new Vector2(-16, 10), new Vector2(200, 40));
        Text cond = HorrorUI.CrearTexto(r, "Condicion", "C O N D I C I Ó N", 22, HorrorUI.Hueso, TextAnchor.UpperLeft);
        HorrorUI.Colocar(cond.rectTransform, new Vector2(0, 1), new Vector2(350, -368), new Vector2(330, 30));

        // ---- Objetivo (derecha)
        textoObjetivo = HorrorUI.CrearTexto(r, "Objetivo", "", 28, HorrorUI.Hueso, TextAnchor.MiddleLeft);
        HorrorUI.Colocar(textoObjetivo.rectTransform, new Vector2(1, 1), new Vector2(-90, -280), new Vector2(560, 40));
        Image lineaObj = HorrorUI.CrearImagen(r, "LineaObjetivo", HorrorUI.HuesoTenue);
        HorrorUI.Colocar(lineaObj.rectTransform, new Vector2(1, 1), new Vector2(0, -306), new Vector2(650, 2));

        // ---- Maletín (centro)
        float anchoInterior = columnas * tamanoCelda;
        float altoInterior = filas * tamanoCelda;
        Image caja = HorrorUI.CrearImagen(r, "Maletin", new Color(0.32f, 0.32f, 0.34f));
        HorrorUI.Colocar(caja.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -75), new Vector2(anchoInterior + 60, altoInterior + 60));
        HorrorUI.CrearMarco(caja.transform, "Brillo", new Color(0.8f, 0.8f, 0.82f), 3);
        RawImage desgaste = HorrorUI.CrearRaw(caja.transform, "Desgaste", HorrorUI.Ruido, new Color(0, 0, 0, 0.35f));
        HorrorUI.Estirar(desgaste.rectTransform);
        desgaste.uvRect = new Rect(0, 0, 1.2f, 1f);

        Image interior = HorrorUI.CrearImagen(caja.transform, "Interior", new Color(0.03f, 0.03f, 0.03f, 1));
        HorrorUI.Estirar(interior.rectTransform, 30);

        // Asa y broches
        Image asa = HorrorUI.CrearImagen(caja.transform, "Asa", HorrorUI.Metal);
        asa.rectTransform.anchorMin = asa.rectTransform.anchorMax = new Vector2(0.5f, 0);
        asa.rectTransform.pivot = new Vector2(0.5f, 1);
        asa.rectTransform.anchoredPosition = new Vector2(0, 0);
        asa.rectTransform.sizeDelta = new Vector2(160, 26);
        HorrorUI.CrearMarco(asa.transform, "Marco", new Color(0.3f, 0.3f, 0.3f), 3);
        for (int k = -1; k <= 1; k += 2)
        {
            Image broche = HorrorUI.CrearImagen(caja.transform, "Broche", new Color(0.75f, 0.75f, 0.75f));
            broche.rectTransform.anchorMin = broche.rectTransform.anchorMax = new Vector2(0.5f, 0);
            broche.rectTransform.pivot = new Vector2(0.5f, 1);
            broche.rectTransform.anchoredPosition = new Vector2(k * 200, 4);
            broche.rectTransform.sizeDelta = new Vector2(34, 30);
        }

        int capacidadTotal = inventario != null ? inventario.capacidad : 6;
        for (int f = 0; f < filas; f++)
        {
            for (int col = 0; col < columnas; col++)
            {
                int i = f * columnas + col;
                bool usable = i < capacidadTotal;
                Image celda = HorrorUI.CrearImagen(interior.transform, "Celda" + i,
                    usable ? new Color(0.13f, 0.13f, 0.14f, 1) : new Color(0.06f, 0.06f, 0.06f, 1));
                RectTransform rt = celda.rectTransform;
                rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(col * tamanoCelda + 4, -f * tamanoCelda - 4);
                rt.sizeDelta = new Vector2(tamanoCelda - 8, tamanoCelda - 8);
                HorrorUI.CrearMarco(celda.transform, "Linea", new Color(0.45f, 0.45f, 0.47f, usable ? 0.6f : 0.15f), 1.5f);

                // Rejilla fina interna como en la referencia
                RawImage rej = HorrorUI.CrearRaw(celda.transform, "Rejilla", HorrorUI.Scanlines, new Color(1, 1, 1, usable ? 0.08f : 0.03f));
                HorrorUI.Estirar(rej.rectTransform, 2);
                rej.uvRect = new Rect(0, 0, 1, 10);

                Image icono = HorrorUI.CrearImagen(celda.transform, "Icono", Color.white);
                HorrorUI.Estirar(icono.rectTransform, 14);
                icono.preserveAspect = true;
                icono.enabled = false;

                Text etiqueta = HorrorUI.CrearTexto(celda.transform, "Etiqueta", "", 22, HorrorUI.Hueso, TextAnchor.MiddleCenter, FontStyle.Bold);
                HorrorUI.Estirar(etiqueta.rectTransform, 8);
                etiqueta.horizontalOverflow = HorizontalWrapMode.Wrap;

                if (usable) { celdas.Add(celda); iconos.Add(icono); etiquetas.Add(etiqueta); }
            }
        }

        // Cursor de selección
        cursor = new GameObject("Cursor", typeof(RectTransform));
        cursor.transform.SetParent(interior.transform, false);
        RectTransform rc = HorrorUI.Rect(cursor);
        rc.anchorMin = rc.anchorMax = new Vector2(0, 1);
        rc.pivot = new Vector2(0, 1);
        rc.sizeDelta = new Vector2(tamanoCelda, tamanoCelda);
        HorrorUI.CrearMarco(cursor.transform, "Marco", HorrorUI.Hueso, 4);

        // ---- Nombre y descripción
        nombreItem = HorrorUI.CrearTexto(r, "NombreItem", "", 36, HorrorUI.Hueso, TextAnchor.MiddleCenter, FontStyle.Bold);
        HorrorUI.Colocar(nombreItem.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 150), new Vector2(1200, 50));
        descItem = HorrorUI.CrearTexto(r, "DescItem", "", 24, HorrorUI.HuesoTenue, TextAnchor.UpperCenter);
        descItem.horizontalOverflow = HorizontalWrapMode.Wrap;
        HorrorUI.Colocar(descItem.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 60), new Vector2(1000, 90));

        Text ayuda = HorrorUI.CrearTexto(r, "Ayuda", "[✚] MOVER     [LB / RB] CASILLA     [B] CERRAR", 22, HorrorUI.HuesoTenue, TextAnchor.MiddleRight);
        HorrorUI.Colocar(ayuda.rectTransform, new Vector2(1, 0), new Vector2(-70, 110), new Vector2(900, 30));
    }

    static Texture2D TexturaECG()
    {
        const int w = 256, h = 64;
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Repeat;
        Color[] px = new Color[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = new Color(1, 1, 1, 0);
        int prevY = h / 2;
        for (int x = 0; x < w; x++)
        {
            float u = (x % 128) / 128f;
            float y = 0.5f;
            if (u > 0.40f && u < 0.44f) y = 0.5f + (u - 0.40f) * 4f;          // P
            else if (u >= 0.44f && u < 0.47f) y = 0.66f - (u - 0.44f) * 12f; // Q
            else if (u >= 0.47f && u < 0.50f) y = 0.30f + (u - 0.47f) * 21f; // R (pico)
            else if (u >= 0.50f && u < 0.53f) y = 0.93f - (u - 0.50f) * 28f; // S
            else if (u >= 0.53f && u < 0.56f) y = 0.09f + (u - 0.53f) * 13.6f;
            else if (u > 0.65f && u < 0.75f) y = 0.5f + Mathf.Sin((u - 0.65f) / 0.1f * Mathf.PI) * 0.08f; // T
            int yi = Mathf.Clamp(Mathf.RoundToInt(y * (h - 1)), 0, h - 1);
            int a = Mathf.Min(prevY, yi), b = Mathf.Max(prevY, yi);
            for (int yy = a; yy <= b; yy++)
                for (int g = -1; g <= 1; g++)
                {
                    int yg = Mathf.Clamp(yy + g, 0, h - 1);
                    px[yg * w + x] = new Color(1, 1, 1, g == 0 ? 1f : 0.35f);
                }
            prevY = yi;
        }
        t.SetPixels(px);
        t.Apply();
        return t;
    }

    // ------------------------------------------------------------------ Lógica

    void Update()
    {
#if UNITY_EDITOR
        // Atajo SOLO del editor para probar: F9 te da la mochila
        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.f9Key.wasPressedThisFrame)
        { hasBackPack = true; Debug.Log("[Prueba] Mochila otorgada con F9"); }
#endif
        if (!Abierto)
        {
            if (HorrorGameState.PuedeJugar && HorrorInput.Inventario)
            {
                if (hasBackPack) Abrir();
                else Debug.Log("No tienes la mochila, no puedes abrir el inventario.");
            }
            return;
        }

        // Monitor cardiaco
        ecg.uvRect = new Rect(Time.unscaledTime * 0.35f, 0, 1.5f, 1);

        if (HorrorInput.Inventario || HorrorInput.Cancelar) { Cerrar(); return; }

        int antes = sel;
        int total = celdas.Count;
        if (HorrorInput.Derecha || HorrorInput.RB) sel = (sel + 1) % total;
        if (HorrorInput.Izquierda || HorrorInput.LB) sel = (sel - 1 + total) % total;
        if (HorrorInput.Abajo) sel = Mathf.Min(sel + columnas, total - 1);
        if (HorrorInput.Arriba) sel = Mathf.Max(sel - columnas, 0);
        if (sel != antes) { src.PlayOneShot(clic, 0.5f); MostrarSeleccion(); }

        // Pulso del cursor
        float a = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3f));
        foreach (Image l in cursor.GetComponentsInChildren<Image>())
            l.color = new Color(HorrorUI.Hueso.r, HorrorUI.Hueso.g, HorrorUI.Hueso.b, a);
    }

    void Abrir()
    {
        Abierto = true;
        raiz.SetActive(true);
        HorrorGameState.modo = HorrorGameState.Modo.Inventario;
        Time.timeScale = 0f;
        src.PlayOneShot(golpe, 0.8f);
        VHSOverlay.Glitch(0.4f, 0.15f);
        Refrescar();
    }

    void Cerrar()
    {
        Abierto = false;
        raiz.SetActive(false);
        Time.timeScale = 1f;
        HorrorGameState.modo = HorrorGameState.Modo.Jugando;
        src.PlayOneShot(clic, 0.5f);
    }

    void Refrescar()
    {
        if (iconos.Count == 0 || inventario == null) return;
        for (int i = 0; i < iconos.Count; i++)
        {
            ItemDefinition it = i < inventario.objetos.Count ? inventario.objetos[i] : null;
            iconos[i].sprite = it != null ? it.icono : null;
            iconos[i].enabled = it != null && it.icono != null;
            // Sin ícono: escribimos el nombre dentro de la casilla
            etiquetas[i].text = it != null && it.icono == null ? (it.nombre ?? "???").ToUpperInvariant() : "";
        }
        textoObjetivo.text = "◆  " + Objetivo;

        textoCondicion.text = "Bien";
        textoCondicion.color = HorrorUI.VerdeMonitor;

        MostrarSeleccion();
    }

    void MostrarSeleccion()
    {
        if (celdas.Count == 0) return;
        sel = Mathf.Clamp(sel, 0, celdas.Count - 1);
        RectTransform rc = cursor.GetComponent<RectTransform>();
        RectTransform celda = celdas[sel].rectTransform;
        rc.anchoredPosition = celda.anchoredPosition + new Vector2(-4, 4);

        ItemDefinition it = inventario != null && sel < inventario.objetos.Count ? inventario.objetos[sel] : null;
        nombreItem.text = it != null ? (it.nombre ?? "???").ToUpperInvariant() : "— vacío —";
        descItem.text = it != null ? (it.descripcion ?? "") : "";
    }
}
