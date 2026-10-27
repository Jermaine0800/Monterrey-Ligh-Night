using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    public GameObject inventoryPanel;
    public PlayerInventory inventario;
    public bool hasBackPack = false;

    private Image[] iconos;
    private int slotSeleccionado = 0; // Para saber qué casilla estamos seleccionando

    void Start()
    {
        // 1. Intenta buscar el inventario del jugador si no fue asignado manualmente
        if (inventario == null)
            inventario = FindFirstObjectByType<PlayerInventory>();

        // 2. Prepara las imágenes dentro de las casillas
        if (inventoryPanel != null)
        {
            int n = inventoryPanel.transform.childCount;
            iconos = new Image[n];

            for (int i = 0; i < n; i++)
            {
                Transform casilla = inventoryPanel.transform.GetChild(i);
                Transform hijo = casilla.Find("Icon");

                if (hijo == null)
                {
                    GameObject go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                    go.transform.SetParent(casilla, false);

                    RectTransform rt = go.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = new Vector2(8, 8);
                    rt.offsetMax = new Vector2(-8, -8);

                    hijo = go.transform;
                }

                iconos[i] = hijo.GetComponent<Image>();
                iconos[i].raycastTarget = false;
                iconos[i].enabled = false;
            }
        }
        else
        {
            Debug.LogError("¡Falta asignar el inventoryPanel en el Inspector del Canvas!");
        }

        // 3. Suscripción segura (evita el NullReferenceException)
        if (inventario != null)
        {
            inventario.OnChanged += Refrescar;
        }
        else
        {
            Debug.LogError("¡No se encontró ningún PlayerInventory en la escena! Asígnalo al personaje.");
        }

        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);
    }

    void Update()
    {
        if (Gamepad.current == null) return;

        // Abrir / Cerrar inventario con Botón Y (North) solo si tiene mochila
        if (Gamepad.current.buttonNorth.wasPressedThisFrame)
        {
            if (hasBackPack)
            {
                if (inventoryPanel != null)
                {
                    bool estadoActual = inventoryPanel.activeSelf;
                    inventoryPanel.SetActive(!estadoActual);
                }
            }
            else
            {
                Debug.Log("No tienes la mochila, no puedes abrir el inventario.");
            }
        }

        // Si el inventario está abierto, usamos LB y RB para movernos entre casillas
        if (inventoryPanel != null && inventoryPanel.activeSelf)
        {
            if (Gamepad.current.leftShoulder.wasPressedThisFrame)
            {
                slotSeleccionado--;
                if (slotSeleccionado < 0) slotSeleccionado = iconos.Length - 1;
                ActualizarSeleccionVisual();
            }

            if (Gamepad.current.rightShoulder.wasPressedThisFrame)
            {
                slotSeleccionado++;
                if (slotSeleccionado >= iconos.Length) slotSeleccionado = 0;
                ActualizarSeleccionVisual();
            }
        }
    }

    void Refrescar()
    {
        if (iconos == null || inventario == null) return;

        for (int i = 0; i < iconos.Length; i++)
        {
            if (i < inventario.objetos.Count)
            {
                iconos[i].sprite = inventario.objetos[i].icono;
                iconos[i].enabled = true;
            }
            else
            {
                iconos[i].enabled = false;
            }
        }
    }

    void ActualizarSeleccionVisual()
    {
        Debug.Log("Casilla seleccionada en inventario: " + slotSeleccionado);
    }
}