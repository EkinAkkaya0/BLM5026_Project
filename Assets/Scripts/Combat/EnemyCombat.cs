using UnityEngine;

public class EnemyCombat : MonoBehaviour
{
    [Header("References")]
    public Transform attackPoint;
    public LayerMask playerLayers;

    [Header("Attack Settings")]
    public float attackRange = 1.3f;
    public int lightAttackDamage = 8;
    public int heavyAttackDamage = 16;
    public float lightAttackCooldown = 0.5f;
    public float heavyAttackCooldown = 1.0f;

    [Header("Block Settings")]
    public bool isBlocking = false;
    public float blockDamageMultiplier = 0.3f; // block varken alınan hasar oranı

    private float nextLightAttackTime = 0f;
    private float nextHeavyAttackTime = 0f;

    private SpriteRenderer sr;
    private Transform player;
    private FighterHealth health;

    private void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        health = GetComponent<FighterHealth>();
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
            Debug.LogError("EnemyCombat: Player not found!");
        }

        UpdateBlockVisual(); // başlangıç rengi
    }

    // ---- BLOCK ----

    public void SetBlocking(bool value)
    {
        isBlocking = value;
        UpdateBlockVisual();
    }

    private void UpdateBlockVisual()
    {
        if (sr == null) return;

        // Block'tayken cyan, değilken kırmızı
        sr.color = isBlocking ? Color.magenta : Color.red;
    }

    public void ReceiveDamage(int amount)
    {
        if (health == null) return;

        int final = isBlocking
            ? Mathf.CeilToInt(amount * blockDamageMultiplier)
            : amount;

        health.TakeDamage(final);

        if (isBlocking)
            FighterUI.EnemyUI?.SetLastAction($"Blocked: received {final} dmg (reduced)");
        else
            FighterUI.EnemyUI?.SetLastAction($"Got hit: {final} dmg");
    }

    // ---- ATTACK ----

    public void TryLightAttack()
    {
        if (Time.time < nextLightAttackTime) return;
        if (player == null) return;

        float distanceX = Mathf.Abs(player.position.x - transform.position.x);
        if (distanceX > attackRange + 0.2f) return;

        nextLightAttackTime = Time.time + lightAttackCooldown;
        PerformAttack(lightAttackDamage, 0.1f, "Light Attack");
    }

    public void TryHeavyAttack()
    {
        if (Time.time < nextHeavyAttackTime) return;
        if (player == null) return;

        float distanceX = Mathf.Abs(player.position.x - transform.position.x);
        if (distanceX > attackRange + 0.2f) return;

        nextHeavyAttackTime = Time.time + heavyAttackCooldown;
        PerformAttack(heavyAttackDamage, 0.2f, "Heavy Attack");
    }

    private void PerformAttack(int damage, float flashTime, string attackName)
    {
        if (sr != null)
        {
            StopAllCoroutines();
            StartCoroutine(AttackFlash(flashTime));
        }

        if (attackPoint == null)
        {
            Debug.LogWarning("EnemyCombat: AttackPoint not assigned!");
            return;
        }

        Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(
            attackPoint.position,
            attackRange,
            playerLayers
        );

        int hitCount = 0;

        foreach (Collider2D col in hitPlayers)
        {
            PlayerCombat pc = col.GetComponent<PlayerCombat>();
            if (pc != null)
            {
                pc.ReceiveDamage(damage);
                hitCount++;
            }
        }

        if (hitCount > 0)
        {
            FighterUI.EnemyUI?.SetLastAction($"{attackName}: {damage} dmg (hit x{hitCount})");
        }
        else
        {
            FighterUI.EnemyUI?.SetLastAction($"{attackName}: missed");
        }

        Debug.Log($"Enemy attacked. Damage: {damage} | Hit count: {hitCount}");
    }

    private System.Collections.IEnumerator AttackFlash(float time)
    {
        if (sr == null)
            yield break;

        Color original = sr.color;
        sr.color = Color.magenta;
        yield return new WaitForSeconds(time);
        // Block durumuna göre rengi geri çek
        UpdateBlockVisual();
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
