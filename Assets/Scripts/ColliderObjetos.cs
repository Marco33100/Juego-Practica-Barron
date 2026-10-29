using System.Collections.Generic;
using UnityEngine;

public class ColliderObjetos : MonoBehaviour
{
    private readonly HashSet<int> objetosRecogidos = new HashSet<int>();
    private int puntaje;

    private void Start() => Debug.Log("Puntaje actual: 0");

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!enabled) return;
        bool positivo = other.CompareTag("Moneda");
        if (!positivo && !other.CompareTag("Enemigo")) return;
        if (!objetosRecogidos.Add(other.gameObject.GetInstanceID())) return;

        puntaje += positivo ? 1 : -1;
        other.enabled = false;
        Destroy(other.gameObject);
        Debug.Log($"{(positivo ? "+1 por tocar frutas insanas" : "-1 por tocar frutas agrias")} | Puntaje actual: {puntaje}");
    }
}
