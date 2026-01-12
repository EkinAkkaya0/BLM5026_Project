using System.Collections;
using UnityEngine;

public class FighterHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;

    [Header("Hit Flash")]
    [SerializeField] private float hitFlashDuration = 0.1f;

    [Header("Death (Animator + SFX)")]

    public string dieParamName = "die";


    public AudioClip deathSfx;

    [Tooltip("Ölünce devre dışı bırakılacak scriptler (PlayerController, PlayerCombat / EnemyAIController, EnemyCombat vs.)")]
    public MonoBehaviour[] disableOnDeath;

    [Header("Death (Physics)")]

    public bool freezeWhenLanded = true;

    [Tooltip("Ground layer mask (Ground objenin layer'ı)")]
    public LayerMask groundLayer;

    [Tooltip("Yere değdi mi kontrol için ray mesafesi")]
    public float groundCheckDistance = 0.15f;


    public float reportToMatchManagerDelay = 0.05f;

    public bool IsDead => isDead;

    private bool isDead = false;

    private SpriteRenderer sr;
    private Animator animator;
    private AudioSource audioSource;
    private Rigidbody2D rb;
    private Collider2D col;

    private RigidbodyConstraints2D initialConstraints;
    private float initialGravityScale;
    private RigidbodyType2D initialBodyType;

    private void Awake()
    {
        currentHealth = maxHealth;

        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();

        animator = GetComponent<Animator>();

        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        if (rb != null)
        {
            initialConstraints = rb.constraints;
            initialGravityScale = rb.gravityScale;
            initialBodyType = rb.bodyType;
        }

        // AudioSource yoksa ekle
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f; // 2D
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        int oldHealth = currentHealth;

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log($"{gameObject.name} took damage: {amount} | {oldHealth} -> {currentHealth}");

        if (sr != null)
        {
            StopAllCoroutines();
            StartCoroutine(HitFlash());
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator HitFlash()
    {
        Color original = sr.color;
        sr.color = Color.red;
        yield return new WaitForSeconds(hitFlashDuration);
        sr.color = original;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log(gameObject.name + " died");

        // 1) Animator -> die param bas
        TriggerDieOnAnimator();

        // 2) Ölüm sesi
        if (deathSfx != null && audioSource != null)
            audioSource.PlayOneShot(deathSfx);

        // 3) Kontrolleri / combat / AI kapat
        if (disableOnDeath != null)
        {
            for (int i = 0; i < disableOnDeath.Length; i++)
            {
                if (disableOnDeath[i] != null)
                    disableOnDeath[i].enabled = false;
            }
        }

        // 4) Fizik: Yatay kaymayı kes, düşmeye izin ver
        if (rb != null)
        {
            // öldüğü anda yukarı fırladıysa yukarı hızını kes, aşağı düşsün
            Vector2 v = rb.linearVelocity;
            rb.linearVelocity = new Vector2(0f, Mathf.Min(v.y, 0f));

            // rotation donsun, Y düşüş serbest kalsın
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            // bazı projelerde ölümde gravity 0 olabiliyor; garantiye al
            if (rb.gravityScale <= 0f) rb.gravityScale = initialGravityScale > 0f ? initialGravityScale : 3f;

            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.WakeUp();

            if (freezeWhenLanded)
            {
                StopCoroutine(nameof(FreezeAfterLanding));
                StartCoroutine(FreezeAfterLanding());
            }
        }

        // 5) MatchManager'ı tetikle (küçük delay)
        if (MatchManager.Instance != null)
        {
            CancelInvoke(nameof(ReportResultToMatchManager));
            Invoke(nameof(ReportResultToMatchManager), reportToMatchManagerDelay);
        }
    }

    private void TriggerDieOnAnimator()
    {
        if (animator == null || string.IsNullOrEmpty(dieParamName))
            return;

        // Trigger mı Bool mu bilmiyorsan: önce Trigger dene, olmazsa Bool set et.
        // (Unity API param tipini direkt okumak zor; pratik yöntem: ikisini de dene.)
        try
        {
            animator.ResetTrigger(dieParamName);
            animator.SetTrigger(dieParamName);
        }
        catch
        {
            // fallback
            animator.SetBool(dieParamName, true);
        }
    }

    private IEnumerator FreezeAfterLanding()
    {
        // 2 saniyeye kadar yere değmeyi bekle
        float t = 0f;
        const float maxWait = 2.0f;

        while (t < maxWait)
        {
            if (IsGrounded())
            {
                // bir frame daha beklet ki tam otursun
                yield return null;

                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                    rb.constraints = RigidbodyConstraints2D.FreezeAll;
                    rb.Sleep();
                }
                yield break;
            }

            t += Time.deltaTime;
            yield return null;
        }

        // yere değmedi (uçuyorsa vs) — yine de sabitle
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
            rb.Sleep();
        }
    }

    private bool IsGrounded()
    {
        if (col == null) col = GetComponent<Collider2D>();
        if (col == null) return false;

        Bounds b = col.bounds;
        Vector2 origin = new Vector2(b.center.x, b.min.y + 0.02f);

        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayer);
        return hit.collider != null;
    }

    private void ReportResultToMatchManager()
    {
        if (MatchManager.Instance == null) return;

        if (CompareTag("Player"))
            MatchManager.Instance.ForceEndMatch(MatchManager.MatchResult.EnemyWin);
        else if (CompareTag("Enemy"))
            MatchManager.Instance.ForceEndMatch(MatchManager.MatchResult.PlayerWin);
    }

    // Maç başında çağrılıyor
    public void ResetHealth()
    {
        currentHealth = maxHealth;
        isDead = false;

        Debug.Log($"{gameObject.name} health reset to {maxHealth}");

        // renk reset
        if (sr != null) sr.color = Color.white;

        // scriptleri geri aç
        if (disableOnDeath != null)
        {
            for (int i = 0; i < disableOnDeath.Length; i++)
            {
                if (disableOnDeath[i] != null)
                    disableOnDeath[i].enabled = true;
            }
        }

        // rigidbody reset
        if (rb != null)
        {
            rb.constraints = initialConstraints;
            rb.gravityScale = initialGravityScale;
            rb.bodyType = initialBodyType;

            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.WakeUp();
        }

        // animator death'te kalmasın
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }
    }
}
