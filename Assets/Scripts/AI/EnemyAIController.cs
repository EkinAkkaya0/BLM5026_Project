using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(QLearningAgent))]
[RequireComponent(typeof(SimplePerceptron))]
public class EnemyAIController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;

    [Header("Desired Distance To Player")]
    public float minDistance = 1.0f;
    public float maxDistance = 3.0f;
    public float desiredDistance = 1.0f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.1f;
    public LayerMask groundLayer;
    private bool isGrounded;

    [Header("Pathfinding")]
    public PathNodeManager pathManager;
    public float nodeReachThreshold = 0.25f;

    [Header("Combat")]
    public EnemyCombat combat;

    [Header("Q-Learning")]
    public float actionInterval = 0.3f;
    public bool useQLearning = true;

    [Header("Perceptron")]
    public bool usePerceptron = true;
    public float perceptronRewardWeight = 0.5f;

    // Reward constants
    private const float REWARD_HIT_SUCCESS = 10f;
    private const float REWARD_HIT_TAKEN = -15f;
    private const float REWARD_MISS = -5f;
    private const float REWARD_IDEAL_DISTANCE = 1f;
    private const float REWARD_WIN = 100f;
    private const float REWARD_LOSE = -100f;

    private Transform player;
    private Rigidbody2D rb;
    private Animator animator;
    private float moveDir;
    private Vector3 initialScale;
    private QLearningAgent qAgent;
    private SimplePerceptron perceptron;

    private List<PathNode> currentPath = new List<PathNode>();
    private int currentPathIndex = 0;

    private float nextActionTime = 0f;
    private QLearningAgent.Action lastAction;
    private int lastPlayerAction = 0;
    private float lastActionSuccess = 0f;

    // Health tracking
    private int lastEnemyHealth;
    private int lastPlayerHealth;

    // Perceptron state evaluation
    private float currentStateEvaluation = 0f;
    private float previousStateEvaluation = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        qAgent = GetComponent<QLearningAgent>();
        perceptron = GetComponent<SimplePerceptron>();
        initialScale = transform.localScale;
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        // İlk can değerleri
        if (combat != null && combat.GetComponent<FighterHealth>() != null)
        {
            lastEnemyHealth = combat.GetComponent<FighterHealth>().currentHealth;
        }

        PlayerCombat playerCombat = player.GetComponent<PlayerCombat>();
        if (playerCombat != null)
        {
            lastPlayerHealth = player.GetComponent<FighterHealth>().currentHealth;
        }

        // MatchManager events
        if (MatchManager.Instance != null)
        {
            MatchManager.Instance.OnMatchEnd += OnMatchEnd;
            MatchManager.Instance.OnMatchStart += OnMatchStart;
        }
    }

    private void Update()
    {
        if (player == null || !MatchManager.Instance.IsMatchInProgress()) return;

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

        FacePlayer(directionToPlayer);

        // Q-Learning + Perceptron action seçimi
        if (Time.time >= nextActionTime && useQLearning)
        {
            ExecuteHybridStep(distanceX);
            nextActionTime = Time.time + actionInterval;
        }

        UpdateMovement();
        CheckHealthChanges();
        
        // ANIMATION CONTROL
        UpdateAnimations();
    }

    private void UpdateAnimations()
    {
        if (animator == null) return;

        // Yürüyor mu?
        animator.SetBool("isWalking", Mathf.Abs(moveDir) > 0.1f);
        
        // Zıplıyor mu? (havada mı?)
        animator.SetBool("isJumping", !isGrounded);
    }

    private void ExecuteHybridStep(float distanceX)
    {
        // 1. PERCEPTRON: Current state'i değerlendir
        if (usePerceptron)
        {
            float[] perceptronInputs = GetPerceptronInputs(distanceX);
            previousStateEvaluation = currentStateEvaluation;
            currentStateEvaluation = perceptron.Evaluate(perceptronInputs);
        }

        // 2. Q-LEARNING: Current state'i oluştur ve action seç
        string currentState = GetCurrentState(distanceX);
        QLearningAgent.Action action = qAgent.SelectAction(currentState);
        lastAction = action;

        // 3. Action'ı uygula
        ExecuteAction(action, distanceX);

        // 4. Immediate reward ver
        float immediateReward = 0f;

        // Ideal mesafe reward'ı
        if (distanceX >= minDistance && distanceX <= maxDistance)
        {
            immediateReward += REWARD_IDEAL_DISTANCE;
        }

        // Perceptron evaluation reward'ı
        if (usePerceptron)
        {
            immediateReward += currentStateEvaluation * perceptronRewardWeight;
        }

        if (immediateReward != 0f)
        {
            qAgent.GiveReward(immediateReward, GetCurrentState(distanceX));
        }
    }

    private float[] GetPerceptronInputs(float distanceX)
    {
        float playerHealthRatio = 1f;
        float enemyHealthRatio = 1f;
        bool playerBlocking = false;

        FighterHealth playerHealth = player.GetComponent<FighterHealth>();
        if (playerHealth != null)
        {
            playerHealthRatio = playerHealth.currentHealth / (float)playerHealth.maxHealth;
        }

        FighterHealth enemyHealth = combat.GetComponent<FighterHealth>();
        if (enemyHealth != null)
        {
            enemyHealthRatio = enemyHealth.currentHealth / (float)enemyHealth.maxHealth;
        }

        PlayerCombat playerCombat = player.GetComponent<PlayerCombat>();
        if (playerCombat != null)
        {
            playerBlocking = playerCombat.isBlocking;
        }

        return perceptron.CreateInputVector(distanceX, playerHealthRatio, enemyHealthRatio, playerBlocking, lastActionSuccess);
    }

    private string GetCurrentState(float distanceX)
    {
        float playerHealthRatio = 1f;
        float enemyHealthRatio = 1f;

        FighterHealth playerHealth = player.GetComponent<FighterHealth>();
        if (playerHealth != null)
        {
            playerHealthRatio = playerHealth.currentHealth / (float)playerHealth.maxHealth;
        }

        FighterHealth enemyHealth = combat.GetComponent<FighterHealth>();
        if (enemyHealth != null)
        {
            enemyHealthRatio = enemyHealth.currentHealth / (float)enemyHealth.maxHealth;
        }

        PlayerCombat playerCombat = player.GetComponent<PlayerCombat>();
        bool playerBlocking = playerCombat != null && playerCombat.isBlocking;

        return qAgent.GetStateString(distanceX, playerHealthRatio, enemyHealthRatio, lastPlayerAction, playerBlocking);
    }

    private void ExecuteAction(QLearningAgent.Action action, float distanceX)
    {
        switch (action)
        {
            case QLearningAgent.Action.Idle:
                moveDir = 0f;
                currentPath.Clear();
                FighterUI.EnemyUI?.SetLastAction("Q+P: Idle");
                lastActionSuccess = 0f;
                break;

            case QLearningAgent.Action.Approach:
                if (distanceX > minDistance)
                {
                    RequestPathToPlayer(desiredDistance);
                    FighterUI.EnemyUI?.SetLastAction("Q+P: Approach");
                    lastActionSuccess = 0f;
                }
                break;

            case QLearningAgent.Action.Retreat:
                if (distanceX < maxDistance)
                {
                    RequestPathAwayFromPlayer();
                    FighterUI.EnemyUI?.SetLastAction("Q+P: Retreat");
                    lastActionSuccess = 0f;
                }
                break;

            case QLearningAgent.Action.LightAttack:
                if (combat != null && !combat.isBlocking && isGrounded)
                {
                    combat.TryLightAttack();
                }
                break;

            case QLearningAgent.Action.HeavyAttack:
                if (combat != null && !combat.isBlocking && isGrounded)
                {
                    combat.TryHeavyAttack();
                }
                break;

            case QLearningAgent.Action.Block:
                if (combat != null && !combat.isBlocking)
                {
                    combat.SetBlocking(true);
                    FighterUI.EnemyUI?.SetLastAction("Q+P: Block");
                    Invoke(nameof(StopBlocking), 0.6f);
                    lastActionSuccess = 0f;
                }
                break;
        }
    }

    private void StopBlocking()
    {
        if (combat != null)
        {
            combat.SetBlocking(false);
        }
    }

    private void CheckHealthChanges()
    {
        float distanceX = Vector3.Distance(transform.position, player.position);

        // Enemy health değişti mi?
        FighterHealth enemyHealth = combat.GetComponent<FighterHealth>();
        if (enemyHealth != null && enemyHealth.currentHealth < lastEnemyHealth)
        {
            int damage = lastEnemyHealth - enemyHealth.currentHealth;
            float reward = REWARD_HIT_TAKEN * (damage / 10f);
            
            qAgent.GiveReward(reward, GetCurrentState(distanceX));
            
            // Perceptron'a öğret
            if (usePerceptron)
            {
                float[] inputs = GetPerceptronInputs(distanceX);
                float target = perceptron.CalculateTargetOutput(reward, previousStateEvaluation);
                perceptron.Train(inputs, target);
            }

            lastActionSuccess = -1f;
            lastEnemyHealth = enemyHealth.currentHealth;
        }

        // Player health değişti mi?
        FighterHealth playerHealth = player.GetComponent<FighterHealth>();
        if (playerHealth != null && playerHealth.currentHealth < lastPlayerHealth)
        {
            int damage = lastPlayerHealth - playerHealth.currentHealth;
            float reward = REWARD_HIT_SUCCESS * (damage / 10f);
            
            qAgent.GiveReward(reward, GetCurrentState(distanceX));
            
            // Perceptron'a öğret
            if (usePerceptron)
            {
                float[] inputs = GetPerceptronInputs(distanceX);
                float target = perceptron.CalculateTargetOutput(reward, previousStateEvaluation);
                perceptron.Train(inputs, target);
            }

            lastActionSuccess = 1f;
            lastPlayerHealth = playerHealth.currentHealth;
        }
    }

    private void OnMatchEnd(MatchManager.MatchResult result)
    {
        float distanceX = Vector3.Distance(transform.position, player.position);
        string finalState = GetCurrentState(distanceX);

        float finalReward = 0f;
        if (result == MatchManager.MatchResult.EnemyWin)
        {
            finalReward = REWARD_WIN;
        }
        else if (result == MatchManager.MatchResult.PlayerWin)
        {
            finalReward = REWARD_LOSE;
        }

        qAgent.GiveReward(finalReward, finalState);

        // Perceptron'a final öğretimi
        if (usePerceptron)
        {
            float[] inputs = GetPerceptronInputs(distanceX);
            float target = perceptron.CalculateTargetOutput(finalReward, currentStateEvaluation);
            perceptron.Train(inputs, target);
            
            Debug.Log($"[Hybrid AI] Episode ended | Perceptron: {perceptron.GetDebugInfo()}");
        }

        ResetHealthTracking();
    }

    private void OnMatchStart()
    {
        ResetHealthTracking();
        lastActionSuccess = 0f;
        currentStateEvaluation = 0f;
        previousStateEvaluation = 0f;
    }

    private void ResetHealthTracking()
    {
        if (combat != null)
        {
            FighterHealth enemyHealth = combat.GetComponent<FighterHealth>();
            if (enemyHealth != null)
            {
                lastEnemyHealth = enemyHealth.currentHealth;
            }
        }

        if (player != null)
        {
            FighterHealth playerHealth = player.GetComponent<FighterHealth>();
            if (playerHealth != null)
            {
                lastPlayerHealth = playerHealth.currentHealth;
            }
        }
    }

    private void RequestPathToPlayer(float targetDistance)
    {
        if (pathManager == null || player == null) return;

        PathNode start = pathManager.GetClosestNode(transform.position);
        float dirToPlayer = Mathf.Sign(player.position.x - transform.position.x);
        Vector3 desiredPos = player.position - new Vector3(dirToPlayer * targetDistance, 0f, 0f);
        PathNode goal = pathManager.GetClosestNode(desiredPos);

        if (start != null && goal != null)
        {
            currentPath = pathManager.FindPath(start, goal) ?? new List<PathNode>();
            currentPathIndex = 0;
        }
    }

    private void RequestPathAwayFromPlayer()
    {
        if (pathManager == null || player == null) return;

        PathNode start = pathManager.GetClosestNode(transform.position);
        float dirAway = -Mathf.Sign(player.position.x - transform.position.x);
        Vector3 retreatPos = transform.position + new Vector3(dirAway * 2f, 0f, 0f);
        PathNode goal = pathManager.GetClosestNode(retreatPos);

        if (start != null && goal != null)
        {
            currentPath = pathManager.FindPath(start, goal) ?? new List<PathNode>();
            currentPathIndex = 0;
        }
    }

    private void UpdateMovement()
    {
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
    }

    private void FixedUpdate()
    {
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(moveDir * moveSpeed, rb.linearVelocity.y);
        }
    }

    private void FacePlayer(float direction)
    {
        if (Mathf.Abs(direction) > 0.05f)
        {
            if (direction > 0)
            {
                transform.localScale = new Vector3(Mathf.Abs(initialScale.x), initialScale.y, initialScale.z);
            }
            else
            {
                transform.localScale = new Vector3(-Mathf.Abs(initialScale.x), initialScale.y, initialScale.z);
            }
        }
    }

    private void OnDestroy()
    {
        if (MatchManager.Instance != null)
        {
            MatchManager.Instance.OnMatchEnd -= OnMatchEnd;
            MatchManager.Instance.OnMatchStart -= OnMatchStart;
        }
    }

    // Debug info
    public string GetHybridDebugInfo()
    {
        string info = $"Last Action: {lastAction} | ";
        if (usePerceptron)
        {
            info += $"State Eval: {currentStateEvaluation:F3} | {perceptron.GetDebugInfo()}";
        }
        return info;
    }
}