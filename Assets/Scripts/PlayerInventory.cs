using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public int capacidad = 6;
    public List<ItemDefinition> objetos = new List<ItemDefinition>();

    public event Action OnChanged;

    void Awake() { capacidad = Mathf.Max(capacidad, 16); }   // 4 x 4 casillas en el maletín

    public bool Quitar(ItemDefinition item)
    {
        bool ok = objetos.Remove(item);
        if (ok) OnChanged?.Invoke();
        return ok;
    }

    public int Contar(ItemDefinition.Tipo tipo)
    {
        int n = 0;
        foreach (ItemDefinition it in objetos) if (it != null && it.tipo == tipo) n++;
        return n;
    }

    /// <summary>Para cuando cambia el contenido de una casilla (p. ej. "Llaves 3/5").</summary>
    public void Avisar() => OnChanged?.Invoke();

    public bool Contiene(ItemDefinition item) => objetos.Contains(item);

    public bool Agregar(ItemDefinition item)
    {
        if (item == null || objetos.Contains(item) || objetos.Count >= capacidad) return false;

        objetos.Add(item);
        OnChanged?.Invoke();
        return true;
    }
}