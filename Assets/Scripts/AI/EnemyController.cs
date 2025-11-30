using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;

    [Header("Desired Distance To Player")]
    public float minDistance = 1.5f;      // Çok yaklaşma sınırı
    public float maxDistance = 3.0f;      // Çok uzaklaşma sınırı
    public float desiredDistance = 2.2f;  // Tercih edilen ideal mesafe

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayer;
    private bool isGrounded;


    [Header("Pathfinding")]
    public PathNodeManager pathManager;
    public float nodeReachThreshold = 0.25f;
    public float repathInterval = 0.3f;

    [Header("Combat")]
    public EnemyCombat combat;

    [Header("Block & Jump")]
    public float blockCloseDistance = 1.5f;
    public float blockDuration = 0.6f;
    public float blockCooldown = 1.5f;
    public float jumpForce = 10f;
    public float jumpCooldown = 2f;

    private Transform player;
    private Rigidbody2D rb;
    private float moveDir;              // -1: sola, 0: dur, 1: sağa
    private Vector3 initialScale;

    private List<PathNode> currentPath = new List<PathNode>();
    private int currentPathIndex = 0;
    private float nextRepathTime = 0f;

    // Block & jump zamanlayıcıları
    private float blockEndTime = 0f;
    private float nextBlockAllowedTime = 0f;
    private float nextJumpTime = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        initialScale = transform.localScale;
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogError("EnemyController: 'Player' tag'li obje bulunamadı!");
        }
    }

    private void Update()
    {
        if (player == null) return;

                // Ground check
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(
                groundCheck.position,
                groundCheckRadius,
                groundLayer
            );
        }


        float distanceX = Mathf.Abs(player.position.x - transform.position.x);
        float directionToPlayer = Mathf.Sign(player.position.x - transform.position.x);

        // Yüze dön: her zaman player'a bak
        if (directionToPlayer > 0)
        {
            transform.localScale = new Vector3(
                Mathf.Abs(initialScale.x),
                initialScale.y,
                initialScale.z
            );
        }
        else if (directionToPlayer < 0)
        {
            transform.localScale = new Vector3(
                -Mathf.Abs(initialScale.x),
                initialScale.y,
                initialScale.z
            );
        }

        bool closeToPlayer = distanceX < blockCloseDistance;

        // ---------- BLOCK LOGIC ----------
        if (combat != null)
        {
            // Block süresi bittiyse block'u kapat
            if (combat.isBlocking && Time.time >= blockEndTime)
            {
                combat.SetBlocking(false);
            }

            // Yeterince yakınsa ara sıra block'a girsin
            if (!combat.isBlocking && closeToPlayer && Time.time >= nextBlockAllowedTime)
            {
                // Basit: %25 ihtimalle block denesin
                if (Random.value < 0.25f)
                {
                    combat.SetBlocking(true);
                    blockEndTime = Time.time + blockDuration;
                    nextBlockAllowedTime = Time.time + blockCooldown;
                    FighterUI.EnemyUI?.SetLastAction("Started Block");
                }
            }
        }

                // ---- JUMP KARARI (TAKTİKSEL, SEYREK) ----
        if (combat != null && !combat.isBlocking && isGrounded && Time.time >= nextJumpTime)
        {
            float veryCloseDist = 1.0f;   // çok yakın: kaçış için
            float midDist       = 2.5f;   // bunun altında: atılma için

            bool inAttackRange = distanceX <= (combat.attackRange + 0.2f);
            float rand = Random.value;

            if (inAttackRange)
            {
                // Saldırı mesafesindeysek: normalde saldır, BAZEN geri zıpla
                if (distanceX < veryCloseDist && rand < 0.10f) // %10 back-jump
                {
                    float dirAway = -Mathf.Sign(player.position.x - transform.position.x);
                    rb.linearVelocity = new Vector2(dirAway * 5f, jumpForce);

                    nextJumpTime = Time.time + jumpCooldown;
                    FighterUI.EnemyUI?.SetLastAction("Back Jump (escape)");

                    currentPath.Clear();
                    currentPathIndex = 0;
                }
            }
            else if (distanceX < midDist)
            {
                // Saldırı mesafesinin biraz dışındayız: BAZEN ileri atılma jump
                if (rand < 0.20f) // %20 forward-jump
                {
                    float dirToPlayer = Mathf.Sign(player.position.x - transform.position.x);
                    rb.linearVelocity = new Vector2(dirToPlayer * 5f, jumpForce);

                    nextJumpTime = Time.time + jumpCooldown;
                    FighterUI.EnemyUI?.SetLastAction("Forward Jump (attack)");

                    currentPath.Clear();
                    currentPathIndex = 0;
                }
            }
        }


        // ---------- PATHFINDING / MESAFE AYARI ----------
        float hysteresis = 0.2f; // ileri-geri titremeyi azaltmak için tampon

        if (Time.time >= nextRepathTime)
        {
            if (distanceX > maxDistance + hysteresis || distanceX < minDistance - hysteresis)
            {
                RequestNewPath();
            }
            else
            {
                // İdeal bandın içindeyiz → path'i temizle, yerinde dur
                currentPath.Clear();
                currentPathIndex = 0;
                moveDir = 0f;
            }

            nextRepathTime = Time.time + repathInterval;
        }

        UpdateMoveDirectionAlongPath();

                // ---------- COMBAT ----------
        if (combat != null && !combat.isBlocking && isGrounded)
        {
            // Saldırı mesafesindeysek saldırmayı dene
            float attackRangeWithMargin = combat.attackRange + 0.2f;

            if (distanceX <= attackRangeWithMargin)
            {
                if (Random.value < 0.7f)
                    combat.TryLightAttack();
                else
                    combat.TryHeavyAttack();
            }
        }

    }

    private void FixedUpdate()
    {
        // Sadece x ekseninde hareket
        rb.linearVelocity = new Vector2(moveDir * moveSpeed, rb.linearVelocity.y);
    }

    private void RequestNewPath()
    {
        if (pathManager == null || player == null) return;

        PathNode start = pathManager.GetClosestNode(transform.position);

        // Player'a göre ideal hedef pozisyon
        float dirToPlayer = Mathf.Sign(player.position.x - transform.position.x);
        float prefDist = Mathf.Clamp(desiredDistance, minDistance, maxDistance);

        Vector3 desiredPos = player.position - new Vector3(dirToPlayer * prefDist, 0f, 0f);
        PathNode goal = pathManager.GetClosestNode(desiredPos);

        if (start == null || goal == null)
        {
            currentPath.Clear();
            currentPathIndex = 0;
            return;
        }

        currentPath = pathManager.FindPath(start, goal) ?? new List<PathNode>();
        currentPathIndex = 0;
    }

    private void UpdateMoveDirectionAlongPath()
    {
        // Block halindeyken yürümeyi kes
        if (combat != null && combat.isBlocking)
        {
            moveDir = 0f;
            return;
        }

        if (currentPath == null || currentPath.Count == 0 || currentPathIndex >= currentPath.Count)
        {
            moveDir = 0f;
            return;
        }

        Vector3 targetPos = currentPath[currentPathIndex].transform.position;
        float diffX = targetPos.x - transform.position.x;

        // Hedef node'a yeterince yaklaştıysak bir sonrakine geç
        if (Mathf.Abs(diffX) <= nodeReachThreshold)
        {
            currentPathIndex++;

            if (currentPathIndex >= currentPath.Count)
            {
                moveDir = 0f;
                return;
            }

            targetPos = currentPath[currentPathIndex].transform.position;
            diffX = targetPos.x - transform.position.x;
        }

        moveDir = Mathf.Sign(diffX);

        // Çok küçük farkta ileri-geri zıplamasın
        if (Mathf.Abs(diffX) < 0.01f)
        {
            moveDir = 0f;
        }
    }
}
