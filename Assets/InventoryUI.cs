using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryUI : MonoBehaviour
{
    public GameObject inventoryPanel;
    public bool hasBackPack = false;

    void Start()
    {
        inventoryPanel.SetActive(false);
    }

    void Update()
    {
        if (!hasBackPack || Gamepad.current == null) return;

        // Botón Y del control de Xbox (triángulo en PlayStation)
        if (Gamepad.current.buttonNorth.wasPressedThisFrame)
        {
            inventoryPanel.SetActive(!inventoryPanel.activeSelf);
        }
    }
}