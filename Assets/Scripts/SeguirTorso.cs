using UnityEngine;

/// <summary>Mantiene la mochila derecha respecto al cuerpo (el hueso puede girar, la mochila no se chuequea).</summary>
public class SeguirTorso : MonoBehaviour
{
    public Transform raiz;
    public Quaternion rotacionLocal = Quaternion.identity;
    void LateUpdate() { if (raiz != null) transform.rotation = raiz.rotation * rotacionLocal; }
}
