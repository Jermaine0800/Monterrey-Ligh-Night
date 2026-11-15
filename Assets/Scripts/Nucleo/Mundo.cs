using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Utilidades del mundo compartidas por enemigos, historia y easter eggs:
/// altura del terreno, árboles cercanos, saber si algo está a la vista del jugador.
/// </summary>
public static class Mundo
{
    public static float AlturaSuelo(Vector3 p)
    {
        Terrain t = Terrain.activeTerrain;
        if (t == null) return p.y;
        return t.SampleHeight(p) + t.transform.position.y;
    }

    public static Vector3 EnSuelo(Vector3 p, float extra = 0f)
    {
        p.y = AlturaSuelo(p) + extra;
        return p;
    }

    static Vector3[] cacheArboles;

    /// <summary>Posiciones (mundo) de los árboles del terreno.</summary>
    public static Vector3[] Arboles
    {
        get
        {
            if (cacheArboles != null) return cacheArboles;
            Terrain t = Terrain.activeTerrain;
            if (t == null || t.terrainData == null) return cacheArboles = new Vector3[0];
            TreeInstance[] arr = t.terrainData.treeInstances;
            Vector3 tam = t.terrainData.size, origen = t.transform.position;
            cacheArboles = new Vector3[arr.Length];
            for (int i = 0; i < arr.Length; i++) cacheArboles[i] = origen + Vector3.Scale(arr[i].position, tam);
            return cacheArboles;
        }
    }

    /// <summary>Árboles entre min y max metros de un punto (máximo "limite" resultados).</summary>
    public static List<Vector3> ArbolesCerca(Vector3 centro, float min, float max, int limite = 60)
    {
        List<Vector3> r = new List<Vector3>();
        float min2 = min * min, max2 = max * max;
        foreach (Vector3 a in Arboles)
        {
            float dx = a.x - centro.x, dz = a.z - centro.z;
            float d2 = dx * dx + dz * dz;
            if (d2 >= min2 && d2 <= max2) { r.Add(a); if (r.Count >= limite) break; }
        }
        return r;
    }

    /// <summary>¿El punto está dentro de lo que ve la cámara y sin nada en medio?</summary>
    public static bool Visible(Vector3 punto, float margen = 0.1f, bool revisarObstaculos = true)
    {
        Camera c = Camera.main;
        if (c == null) return false;
        Vector3 v = c.WorldToViewportPoint(punto);
        if (v.z <= 0 || v.x < margen || v.x > 1 - margen || v.y < margen * 0.5f || v.y > 1 - margen * 0.5f) return false;
        if (!revisarObstaculos) return true;
        Vector3 dir = punto - c.transform.position;
        return !Physics.Raycast(c.transform.position, dir.normalized, dir.magnitude - 0.5f, ~0, QueryTriggerInteraction.Ignore);
    }

    /// <summary>Un punto al azar a cierta distancia del jugador que la cámara NO esté viendo.</summary>
    public static Vector3 PuntoFueraDeVista(Vector3 centro, float min, float max, int intentos = 20)
    {
        for (int i = 0; i < intentos; i++)
        {
            Vector2 r = Random.insideUnitCircle.normalized * Random.Range(min, max);
            Vector3 p = EnSuelo(centro + new Vector3(r.x, 0, r.y));
            if (!Visible(p + Vector3.up, 0f, false)) return p;
        }
        Transform cam = Camera.main != null ? Camera.main.transform : null;
        Vector3 atras = cam != null ? -cam.forward : Vector3.back; atras.y = 0;
        return EnSuelo(centro + atras.normalized * Random.Range(min, max));
    }
}
