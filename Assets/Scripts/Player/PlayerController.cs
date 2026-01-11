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
    private Animator animator;
    private float inputX;
    private bool isGrounded;

    private Transform enemy;
    private Vector3 initialScale;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        initialScale = transform.localScale;
    }

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
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            }
        }

        // Enemy'e bak
        if (enemy != null)
        {
            float dx = enemy.position.x - transform.position.x;

            if (Mathf.Abs(dx) > 0.05f)
            {
                float dirToEnemy = Mathf.Sign(dx);

                if (dirToEnemy > 0)
                {
                    transform.localScale = new Vector3(
                        Mathf.Abs(initialScale.x),
                        initialScale.y,
                        initialScale.z
                    );
                }
                else
                {
                    transform.localScale = new Vector3(
                        -Mathf.Abs(initialScale.x),
                        initialScale.y,
                        initialScale.z
                    );
                }
            }
        }

        // ANIMATION CONTROL
        if (animator != null)
        {
            animator.SetBool("isWalking", Mathf.Abs(inputX) > 0.1f);
            animator.SetBool("isJumping", !isGrounded);
        }
    }

    private void FixedUpdate()
    {
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(inputX * moveSpeed, rb.linearVelocity.y);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}