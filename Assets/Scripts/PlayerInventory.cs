using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public int capacidad = 6;
    public List<ItemDefinition> objetos = new List<ItemDefinition>();

    public event Action OnChanged;

    public bool Agregar(ItemDefinition item)
    {
        if (objetos.Count >= capacidad) return false;

        objetos.Add(item);
        OnChanged?.Invoke();
        return true;
    }
}