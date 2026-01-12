using UnityEngine;

public class FighterDeathHandler : MonoBehaviour
{
    [Header("Refs")]
    public FighterHealth health;
    public Animator animator;
    public Rigidbody2D rb;

    [Header("Disable on death")]
    public MonoBehaviour[] disableThese; // PlayerController, PlayerCombat, EnemyAIController, EnemyCombat vb.

    [Header("Animator")]
    public string dieTrigger = "die";

    private bool dead;
    private AttackSFX sfx;

    private void Awake()
    {
        if (!health) health = GetComponent<FighterHealth>();
        if (!animator) animator = GetComponent<Animator>();
        if (!rb) rb = GetComponent<Rigidbody2D>();
        sfx = GetComponent<AttackSFX>();
    }

    private void Update()
    {
        if (dead || health == null) return;
        if (health.currentHealth <= 0f)
            Die();
    }

    private void Die()
    {
        dead = true;

        // anim
        if (animator) animator.SetTrigger(dieTrigger);

        // sound
        sfx?.PlayDeath();

        // stop physics
        if (rb)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            // İstersen tamamen dondur:
            // rb.simulated = false;
        }

        // disable gameplay scripts
        if (disableThese != null)
        {
            foreach (var b in disableThese)
                if (b) b.enabled = false;
        }
    }
}
