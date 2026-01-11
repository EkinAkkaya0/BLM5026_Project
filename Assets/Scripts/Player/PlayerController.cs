using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 7f;
    public float jumpForce = 12f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayer;

    private Rigidbody2D rb;
    private float inputX;
    private bool isGrounded;

    private Transform enemy;
    private Vector3 initialScale;


        // PlayerController.cs'e ekle:

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }


   /* private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        initialScale = transform.localScale;
    } */

     private void Start()
    {
        // Sahnedeki Enemy'yi tag'den bul
        GameObject enemyObj = GameObject.FindGameObjectWithTag("Enemy");
        if (enemyObj != null)
        {
            enemy = enemyObj.transform;
        }
    }

    private void Update()
    {
        animator.SetBool("isWalking", Mathf.Abs(inputX) > 0.1f);
        animator.SetBool("isJumping", !isGrounded);
        inputX = Input.GetAxisRaw("Horizontal");


        // Yerde miyiz?
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(
                groundCheck.position,
                groundCheckRadius,
                groundLayer
            );
        }

        // Zıplama
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

          if (enemy != null)
        {
            float dx = enemy.position.x - transform.position.x;

            // Çok yakınken deli gibi flip atmasın diye küçük bir eşik
            if (Mathf.Abs(dx) > 0.05f)
            {
                float dirToEnemy = Mathf.Sign(dx);

                if (dirToEnemy > 0)
                {
                    // Enemy sağda → sağa bak
                    transform.localScale = new Vector3(
                        Mathf.Abs(initialScale.x),
                        initialScale.y,
                        initialScale.z
                    );
                }
                else
                {
                    // Enemy solda → sola bak
                    transform.localScale = new Vector3(
                        -Mathf.Abs(initialScale.x),
                        initialScale.y,
                        initialScale.z
                    );
                }
            }
        }
    }

    private void FixedUpdate()
    {
        // Yürüyüş
        rb.linearVelocity = new Vector2(inputX * moveSpeed, rb.linearVelocity.y);
    }

    private void OnDrawGizmosSelected()
    {
        // GroundCheck gizmo (editor'da küçük bir daire görebilirsin)
        if (groundCheck != null)
        {
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
