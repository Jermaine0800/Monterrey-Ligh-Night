using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Interactuar con lo que tienes enfrente: [A] en control / [E] en teclado.
/// Muestra abajo al centro qué vas a hacer ("[A] Recoger Pala", "[E] Leer", "[A] Esconderse") —
/// como en Resident Evil / The Last of Us: si no sale el letrero, no hay nada que usar.
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    public float alcance = 1.5f;
    public Transform socketEspalda;

    Text letrero;
    CanvasGroup grupoLetrero;
    float siguienteBusqueda;
    IInteractable enMira;

    void Update()
    {
        PlayerEscondite esc = GetComponent<PlayerEscondite>();
        bool bloqueado = !HorrorGameState.PuedeJugar || DocumentoUI.Abierto || (esc != null && (esc.Escondido || Time.time - esc.UltimaSalida < 0.5f));
        if (Time.time >= siguienteBusqueda)
        {
            siguienteBusqueda = Time.time + 0.12f;
            enMira = bloqueado ? null : Buscar();
            MostrarLetrero(enMira);
        }
        if (bloqueado) return;
        if (HorrorInput.Interactuar)
        {
            IInteractable o = Buscar();
            if (o != null) o.Interact(this);
            siguienteBusqueda = 0f;
        }
    }

    IInteractable Buscar()
    {
        Vector3 centro = transform.position + Vector3.up * 0.9f;
        Collider[] cercanos = Physics.OverlapSphere(centro, alcance, ~0, QueryTriggerInteraction.Collide);

        // El más cercano a lo que estás viendo (las llaves tienen prioridad)
        Vector3 frente = centro + transform.forward * 0.6f;
        IInteractable mejor = null;
        float mejorPuntaje = float.MaxValue;
        foreach (Collider c in cercanos)
        {
            IInteractable objeto = c.GetComponentInParent<IInteractable>();
            if (objeto == null) continue;
            MonoBehaviour mb = objeto as MonoBehaviour;
            if (mb != null && !mb.isActiveAndEnabled) continue;
            if (objeto is LlaveBucle lb && (lb.Tomada || !lb.Disponible)) continue;
            if (objeto is VelaPanteon vp && (vp.Prendida || vp.Encendiendo)) continue;
            if (objeto is FogataCampamento fc && (fc.Prendida || fc.Encendiendo)) continue;
            float puntaje = Vector3.Distance(frente, c.ClosestPoint(frente));
            if (objeto is LlaveBucle) puntaje -= 1f;
            if (objeto is Casillero cas && cas.Abierto) puntaje += 2f;
            if (puntaje < mejorPuntaje) { mejorPuntaje = puntaje; mejor = objeto; }
        }
        return mejor;
    }

    static string Accion(IInteractable o)
    {
        switch (o)
        {
            case LlaveBucle _: return "Tomar la llave";
            case ArmaRecogible a: return "Recoger " + a.nombre;
            case Curacion c: return "Recoger " + c.nombre;
            case Recogible r: return "Recoger " + r.nombre;
            case NotaLeible n: return n.Leida ? "Volver a leer" : "Leer";
            case Escondite e: return "Esconderse (" + e.nombre + ")";
            case Casillero _: return "Abrir";
            case BackpackPickup _: return "Tomar la mochila";
        }
        string n2 = o.GetType().Name;
        if (n2.Contains("Linterna") || n2.Contains("Flashlight")) return "Tomar la linterna";
        if (n2.Contains("Vela")) return "Mantén para prender la vela";
        if (n2.Contains("Fogata")) return "Mantén para prender la fogata";
        if (n2.Contains("Puerta")) return "Abrir";
        return "Usar";
    }

    void MostrarLetrero(IInteractable o)
    {
        if (letrero == null)
        {
            Canvas c = HorrorUI.CrearCanvas("Letrero_Accion", 110, transform);
            GameObject g = new GameObject("Accion");
            g.transform.SetParent(c.transform, false);
            letrero = g.AddComponent<Text>();
            letrero.font = HorrorUI.Fuente;
            letrero.fontSize = 26;
            letrero.alignment = TextAnchor.MiddleCenter;
            letrero.color = HorrorUI.Hueso;
            letrero.raycastTarget = false;
            Outline ol = g.AddComponent<Outline>(); ol.effectColor = new Color(0, 0, 0, 0.9f); ol.effectDistance = new Vector2(2, -2);
            RectTransform rt = letrero.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0f); rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f); rt.anchoredPosition = new Vector2(0, 150); rt.sizeDelta = new Vector2(900, 40);
            grupoLetrero = g.AddComponent<CanvasGroup>();
            grupoLetrero.alpha = 0f;
        }
        if (o != null) letrero.text = "[" + HorrorInput.Boton("accion") + "]  " + Accion(o);
        grupoLetrero.alpha = o != null ? 1f : 0f;
    }
}
