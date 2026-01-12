using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    public Transform attackPoint;
    public LayerMask enemyLayers;

    [Header("Attack Settings")]
    public float attackRange = 0.6f;
    public int lightAttackDamage = 10;
    public int heavyAttackDamage = 20;
    public float lightAttackCooldown = 0.2f;
    public float heavyAttackCooldown = 0.6f;

    [Header("Block Settings")]
    public bool isBlocking = false;
    public float blockDamageMultiplier = 0.3f;

    private float nextLightAttackTime = 0f;
    private float nextHeavyAttackTime = 0f;

    private AttackSFX attackSfx;
    private SpriteRenderer sr;
    private Animator animator;
    private FighterHealth health;

    private void Awake()
    {
        attackSfx = GetComponent<AttackSFX>();
        sr = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponent<Animator>();
        health = GetComponent<FighterHealth>();
    }

    private void Update()
    {
        // BLOCK
        isBlocking = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        // Block animation
        if (animator != null)
        {
            animator.SetBool("isBlocking", isBlocking);
        }

        if (sr != null)
        {
            sr.color = isBlocking ? Color.cyan : Color.white;
        }

        // LIGHT ATTACK: J
        if (Input.GetKeyDown(KeyCode.J) && Time.time >= nextLightAttackTime)
        {
            nextLightAttackTime = Time.time + lightAttackCooldown;
            PerformAttack(lightAttackDamage, 0.1f, "Light Attack");
        }

        // HEAVY ATTACK: K
        if (Input.GetKeyDown(KeyCode.K) && Time.time >= nextHeavyAttackTime)
        {
            nextHeavyAttackTime = Time.time + heavyAttackCooldown;
            PerformAttack(heavyAttackDamage, 0.2f, "Heavy Attack");
        }
    }

    private void PerformAttack(int damage, float flashTime, string attackName)
    {
        attackSfx?.PlayAttack();
        // Trigger correct attack animation based on attack type
        if (animator != null)
        {
            if (attackName.Contains("Light"))
            {
                animator.SetTrigger("lightAttackTrigger");
            }
            else if (attackName.Contains("Heavy"))
            {
                animator.SetTrigger("heavyAttackTrigger");
            }
        }

        if (sr != null)
        {
            StopAllCoroutines();
            StartCoroutine(AttackFlash(flashTime));
        }

        if (attackPoint == null) return;

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(
            attackPoint.position,
            attackRange,
            enemyLayers
        );

        int hitCount = 0;

        foreach (Collider2D enemy in hitEnemies)
        {
            EnemyCombat enemyCombat = enemy.GetComponent<EnemyCombat>();
            FighterHealth enemyHealth = enemy.GetComponent<FighterHealth>();

            if (enemyCombat != null)
            {
                enemyCombat.ReceiveDamage(damage);
                hitCount++;
            }
            else if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
                hitCount++;
            }
        }

        if (hitCount > 0)
        {
            FighterUI.PlayerUI?.SetLastAction($"{attackName}: {damage} dmg (hit x{hitCount})");
        }
        else
        {
            FighterUI.PlayerUI?.SetLastAction($"{attackName}: missed");
        }
    }

    private System.Collections.IEnumerator AttackFlash(float time)
    {
        if (sr == null) yield break;
        
        Color original = sr.color;
        sr.color = Color.yellow;
        yield return new WaitForSeconds(time);
        sr.color = original;
    }

    public void ReceiveDamage(int amount)
    {
        if (health == null) return;

        int finalDamage = amount;

        if (isBlocking)
        {
            finalDamage = Mathf.CeilToInt(amount * blockDamageMultiplier);
        }

        health.TakeDamage(finalDamage);

        if (isBlocking)
        {
            FighterUI.PlayerUI?.SetLastAction($"Blocked: received {finalDamage} dmg (reduced)");
        }
        else
        {
            FighterUI.PlayerUI?.SetLastAction($"Got hit: {finalDamage} dmg");
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}