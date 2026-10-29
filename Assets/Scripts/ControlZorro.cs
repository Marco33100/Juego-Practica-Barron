using UnityEngine;
[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class ControlZorro : MonoBehaviour
{
    private const float UmbralMovimiento = 0.15f;
    [SerializeField] private float velocidad = 5f;
    private ControlPlayer controlPlayer;
    private Vector2 movimiento;
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer sprite;
    private void Awake()
    {
        controlPlayer = new ControlPlayer();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();
    }
    private void OnEnable()
    {
        controlPlayer.Enable();
    }
    private void Update()
    {
        movimiento = controlPlayer.Player.Move.ReadValue<Vector2>();
        CambioEstado();
        if (movimiento.x > UmbralMovimiento)
            sprite.flipX = false;
        else if (movimiento.x < -UmbralMovimiento)
            sprite.flipX = true;
    }

      private void FixedUpdate()
    {
        Vector2 velocidadActual = rb.linearVelocity;
        velocidadActual.x = movimiento.x * velocidad;
        rb.linearVelocity = velocidadActual;
    }
    private void CambioEstado()
    {
        if (movimiento.x > UmbralMovimiento)
            animator.SetInteger("PlayerEstado", 1);
        else if (movimiento.x < -UmbralMovimiento)
            animator.SetInteger("PlayerEstado", 2);
        else
            animator.SetInteger("PlayerEstado", 5);
    }
    private void OnDisable()
    {
        controlPlayer.Disable();
        movimiento = Vector2.zero;
    }
    private void OnDestroy()
    {
        controlPlayer.Dispose();
    }
}
