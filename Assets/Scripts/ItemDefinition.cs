using UnityEngine;

[CreateAssetMenu(fileName = "NuevoObjeto", menuName = "Inventario/Objeto")]
public class ItemDefinition : ScriptableObject
{
    public string nombre;
    public Sprite icono;
    [TextArea] public string descripcion;
}