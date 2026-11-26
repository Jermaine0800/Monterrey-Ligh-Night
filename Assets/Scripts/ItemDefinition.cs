using UnityEngine;

/// <summary>Un objeto del maletín. Los de la historia se crean en tiempo de ejecución con InventarioUtil.</summary>
[CreateAssetMenu(fileName = "NuevoObjeto", menuName = "Inventario/Objeto")]
public class ItemDefinition : ScriptableObject
{
    public enum Tipo { Objeto, Curacion, Arma, Llave, Nota }

    public string nombre;
    public Sprite icono;
    [TextArea] public string descripcion;

    [Header("Uso")]
    public Tipo tipo = Tipo.Objeto;
    [Tooltip("Curación: vida que recupera. Arma: daño por golpe.")]
    public float valor;
    [Tooltip("Arma: alcance del golpe en metros")]
    public float alcance;
    [Tooltip("Arma: golpes que aguanta antes de romperse")]
    public int usos;
    [TextArea] [Tooltip("Nota: texto completo para volver a leerla")]
    public string texto;
}
