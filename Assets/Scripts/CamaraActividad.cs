using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CamaraActividad : MonoBehaviour
{
    public Transform objetivo;
    public Vector2 minimo = new Vector2(-16, -10);
    public Vector2 maximo = new Vector2(16, 10);
    private Camera camara;
    private void Awake() => camara = GetComponent<Camera>();
    private void LateUpdate()
    {
        if (objetivo == null) return;
        float alto = camara.orthographicSize;
        float ancho = alto * camara.aspect;
        float x = ancho * 2 >= maximo.x - minimo.x ? (minimo.x + maximo.x) / 2 : Mathf.Clamp(objetivo.position.x, minimo.x + ancho, maximo.x - ancho);
        float y = alto * 2 >= maximo.y - minimo.y ? (minimo.y + maximo.y) / 2 : Mathf.Clamp(objetivo.position.y, minimo.y + alto, maximo.y - alto);
        transform.position = new Vector3(x, y, -10);
    }
}
