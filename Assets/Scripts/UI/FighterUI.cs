using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum FighterSide
{
    Player,
    Enemy
}

public class FighterUI : MonoBehaviour
{
    public FighterSide side;

    [Header("References")]
    public FighterHealth fighterHealth;
    public Slider healthBar;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI lastActionText;

    public static FighterUI PlayerUI;
    public static FighterUI EnemyUI;

    private void Awake()
    {
        if (side == FighterSide.Player)
        {
            PlayerUI = this;
            Debug.Log("FighterUI: PlayerUI registered");
        }
        else if (side == FighterSide.Enemy)
        {
            EnemyUI = this;
            Debug.Log("FighterUI: EnemyUI registered");
        }
    }

    private void Start()
    {
        if (fighterHealth != null && healthBar != null)
        {
            healthBar.minValue = 0;
            healthBar.maxValue = fighterHealth.maxHealth;
            healthBar.value = fighterHealth.currentHealth;
        }

        if (lastActionText != null)
        {
            lastActionText.text = "Ready";
        }
    }

    private void Update()
    {
        if (fighterHealth != null && healthBar != null)
        {
            healthBar.value = fighterHealth.currentHealth;

            if (healthText != null)
            {
                healthText.text = $"{fighterHealth.currentHealth} / {fighterHealth.maxHealth}";
            }
        }
    }

    public void SetLastAction(string desc)
    {
        if (lastActionText != null)
        {
            lastActionText.text = desc;
        }

        Debug.Log($"FighterUI ({side}) last action -> {desc}");
    }
}
