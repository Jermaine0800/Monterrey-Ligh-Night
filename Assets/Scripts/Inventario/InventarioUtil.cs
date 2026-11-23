using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Crea objetos del maletín en tiempo de ejecución (curaciones, armas, llaves, notas) con su ícono
/// (Assets/Resources/Iconos/&lt;nombre&gt;.png) y guarda el modelo 3D de cada arma para poder re-equiparla.
/// </summary>
public static class InventarioUtil
{
    static readonly Dictionary<string, Sprite> iconos = new Dictionary<string, Sprite>();
    static readonly Dictionary<string, GameObject> visuales = new Dictionary<string, GameObject>();

    public static Sprite Icono(string archivo)
    {
        if (string.IsNullOrEmpty(archivo)) return null;
        if (iconos.TryGetValue(archivo, out Sprite s)) return s;
        Texture2D t = Resources.Load<Texture2D>("Iconos/" + archivo);
        if (t == null) t = Resources.Load<Texture2D>("Iconos/Tubo");
        s = t != null ? Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f)) : null;
        iconos[archivo] = s;
        return s;
    }

    public static ItemDefinition Crear(string nombre, ItemDefinition.Tipo tipo, string icono, string descripcion, float valor = 0, float alcance = 0, int usos = 0)
    {
        ItemDefinition it = ScriptableObject.CreateInstance<ItemDefinition>();
        it.name = nombre;
        it.nombre = nombre;
        it.tipo = tipo;
        it.icono = Icono(icono);
        it.descripcion = descripcion;
        it.valor = valor;
        it.alcance = alcance;
        it.usos = usos;
        return it;
    }

    /// <summary>Guarda una copia oculta del modelo del arma (para volver a ponerla en la mano después).</summary>
    public static void RegistrarVisual(string nombre, GameObject original)
    {
        if (visuales.ContainsKey(nombre) && visuales[nombre] != null) return;
        GameObject copia = Object.Instantiate(original);
        copia.name = "Visual_" + nombre;
        foreach (Collider c in copia.GetComponentsInChildren<Collider>()) Object.Destroy(c);
        foreach (MonoBehaviour mb in copia.GetComponentsInChildren<MonoBehaviour>()) Object.Destroy(mb);
        foreach (Light l in copia.GetComponentsInChildren<Light>()) Object.Destroy(l.gameObject);
        copia.transform.localScale = original.transform.lossyScale;
        copia.SetActive(false);
        Object.DontDestroyOnLoad(copia);
        visuales[nombre] = copia;
    }

    public static GameObject NuevoVisual(string nombre)
    {
        if (!visuales.TryGetValue(nombre, out GameObject v) || v == null) return null;
        GameObject g = Object.Instantiate(v);
        g.SetActive(true);
        return g;
    }

    public static PlayerInventory Inventario => Object.FindAnyObjectByType<PlayerInventory>();
    public static bool TieneMochila { get { InventoryUI ui = Object.FindAnyObjectByType<InventoryUI>(); return ui == null || ui.hasBackPack; } }
}
