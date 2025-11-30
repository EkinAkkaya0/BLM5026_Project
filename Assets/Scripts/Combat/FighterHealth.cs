using UnityEngine;

public class FighterHealth : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;

    private SpriteRenderer sr;

    private void Awake()
    {
        currentHealth = maxHealth;

        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = GetComponentInChildren<SpriteRenderer>();
    }


    public void TakeDamage(int amount)
    {
        int oldHealth = currentHealth;

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log($"{gameObject.name} took damage: {amount} | {oldHealth} → {currentHealth}");

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


    private System.Collections.IEnumerator HitFlash()
    {
        Color original = sr.color;
        sr.color = Color.red;
        yield return new WaitForSeconds(0.1f);
        sr.color = original;
    }

    private void Die()
    {
        // Şimdilik sadece devre dışı bırak
        Debug.Log(gameObject.name + " died");
        // İleride: KO animasyonu, reset round, vs.
    }
}
