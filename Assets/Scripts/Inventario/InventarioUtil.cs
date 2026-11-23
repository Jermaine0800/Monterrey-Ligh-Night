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

    // ------------------------------------------------------------------ Objetos que se apilan en UNA casilla
    // Las 5 llaves ocupan una sola casilla ("Llaves del bucle 3/5") y todas las notas otra ("Notas (6)"),
    // como los "Archivos" de Resident Evil: así el maletín no se llena de papeles.

    public static readonly List<(string titulo, string texto)> Notas = new List<(string, string)>();
    static int notaActual = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Limpiar() { Notas.Clear(); notaActual = -1; iconos.Clear(); visuales.Clear(); }

    static ItemDefinition Buscar(PlayerInventory inv, ItemDefinition.Tipo tipo)
    {
        if (inv == null) return null;
        foreach (ItemDefinition it in inv.objetos) if (it != null && it.tipo == tipo) return it;
        return null;
    }

    /// <summary>Pone (o actualiza) la casilla de llaves con el número actual.</summary>
    public static void ActualizarLlaves(int n, int total, string frase)
    {
        PlayerInventory inv = Inventario;
        if (inv == null || n <= 0) return;
        ItemDefinition it = Buscar(inv, ItemDefinition.Tipo.Llave);
        if (it == null)
        {
            it = Crear("Llaves", ItemDefinition.Tipo.Llave, "Llave", "");
            if (inv.objetos.Count >= inv.capacidad) inv.capacidad++;
            inv.Agregar(it);
        }
        it.nombre = it.name = "Llaves del bucle  " + n + "/" + total;
        it.descripcion = "Las llaves de la puerta del bucle. Junta las " + total + " y vuelve al claro." + (string.IsNullOrEmpty(frase) ? "" : "\n\nÚltima: " + frase);
        inv.Avisar();
    }

    /// <summary>Guarda una nota leída y pone (o actualiza) la casilla de notas.</summary>
    public static void GuardarNota(string titulo, string texto)
    {
        foreach (var n in Notas) if (n.titulo == titulo && n.texto == texto) return;
        Notas.Add((titulo, texto));
        notaActual = Notas.Count - 1;
        CasillaNotas();
    }

    public static void CasillaNotas()
    {
        PlayerInventory inv = Inventario;
        if (inv == null || Notas.Count == 0 || !TieneMochila) return;
        ItemDefinition it = Buscar(inv, ItemDefinition.Tipo.Nota);
        if (it == null)
        {
            it = Crear("Notas", ItemDefinition.Tipo.Nota, "Nota", "");
            if (inv.objetos.Count >= inv.capacidad) inv.capacidad++;
            inv.Agregar(it);
        }
        it.nombre = it.name = "Notas  (" + Notas.Count + ")";
        it.descripcion = "Todo lo que has leído. [A / E] para releerlas; vuelve a presionar para pasar a la siguiente.\nÚltima: " + Notas[Notas.Count - 1].titulo;
        inv.Avisar();
    }

    /// <summary>La siguiente nota para releer (empieza por la más reciente).</summary>
    public static (string titulo, string texto) SiguienteNota()
    {
        if (Notas.Count == 0) return ("", "");
        if (notaActual < 0 || notaActual >= Notas.Count) notaActual = Notas.Count - 1;
        var n = Notas[notaActual];
        notaActual = (notaActual - 1 + Notas.Count) % Notas.Count;
        return (n.titulo + "   (" + (Notas.IndexOf(n) + 1) + "/" + Notas.Count + ")", n.texto);
    }
    public static bool TieneMochila { get { InventoryUI ui = Object.FindAnyObjectByType<InventoryUI>(); return ui == null || ui.hasBackPack; } }
}
