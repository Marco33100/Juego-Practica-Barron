using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(SpriteRenderer))]
public class MovimientoActividad : MonoBehaviour
{
    [SerializeField] private float velocidad = 5f;
    private ControlPlayer controles;
    private Rigidbody2D cuerpo;
    private Animator animador;
    private SpriteRenderer imagen;
    private Vector2 direccion;
    public Vector2 Direccion => direccion;

    private void Awake()
    {
        controles = new ControlPlayer();
        cuerpo = GetComponent<Rigidbody2D>();
        animador = GetComponent<Animator>();
        imagen = GetComponent<SpriteRenderer>();
        cuerpo.gravityScale = 0;
        cuerpo.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    private void OnEnable() => controles.Enable();

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        // Limitar la magnitud evita correr mas rapido en diagonal.
        direccion = Vector2.ClampMagnitude(controles.Player.Move.ReadValue<Vector2>(), 1f);
        if (Mathf.Abs(direccion.x) > 0.01f)
            imagen.flipX = direccion.x < 0;
        animador.SetInteger("PlayerEstado", direccion.sqrMagnitude < 0.01f ? 5 : (imagen.flipX ? 2 : 1));
    }

    private void FixedUpdate() => cuerpo.linearVelocity = direccion * velocidad;

    private void OnDisable()
    {
        controles.Disable();
        direccion = Vector2.zero;
        if (cuerpo != null) cuerpo.linearVelocity = Vector2.zero;
    }

    private void OnDestroy() => controles?.Dispose();
}
