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
    // --- Backward compatible static access (senin hatanı çözen kısım) ---
    public static FighterUI PlayerUI { get; private set; }
    public static FighterUI EnemyUI  { get; private set; }

    [Header("Side")]
    public FighterSide side = FighterSide.Player;

    [Header("References")]
    public FighterHealth fighterHealth;
    public Slider healthBar;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI lastActionText;

    [Header("Bar Tuning")]
    [SerializeField] private float smooth = 12f;
    [SerializeField] private float minVisualValue = 0f;

    private float shownHp;

    private void Awake()
    {
        // Statik referansları bağla (PlayerUI / EnemyUI)
        if (side == FighterSide.Player)
        {
            if (PlayerUI != null && PlayerUI != this)
                Debug.LogWarning("PlayerUI zaten set edilmiş. Sahnedeki FighterUI sayısını kontrol et.");
            PlayerUI = this;
        }
        else
        {
            if (EnemyUI != null && EnemyUI != this)
                Debug.LogWarning("EnemyUI zaten set edilmiş. Sahnedeki FighterUI sayısını kontrol et.");
            EnemyUI = this;
        }
    }

    private void OnDestroy()
    {
        if (PlayerUI == this) PlayerUI = null;
        if (EnemyUI == this) EnemyUI = null;
    }

    private void Reset()
    {
        fighterHealth = GetComponent<FighterHealth>();

        if (!healthBar)
            healthBar = GetComponentInChildren<Slider>(true);

        if (!healthText || !lastActionText)
        {
            var tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
            if (!healthText && tmps.Length > 0) healthText = tmps[0];
            if (!lastActionText && tmps.Length > 1) lastActionText = tmps[1];
        }
    }

    private void Start()
    {
        InitBar();
        UpdateHealthTextImmediate();
        if (lastActionText) lastActionText.text = "Ready";
    }

    private void OnEnable()
    {
        InitBar();
        UpdateHealthTextImmediate();
    }

    private void InitBar()
    {
        if (fighterHealth == null || healthBar == null) return;

        healthBar.minValue = 0f;
        healthBar.maxValue = fighterHealth.maxHealth;

        shownHp = Mathf.Clamp(fighterHealth.currentHealth, 0f, fighterHealth.maxHealth);
        healthBar.value = ApplyMinVisual(shownHp);
    }

    private void Update()
    {
        if (fighterHealth == null || healthBar == null) return;

        float targetHp = Mathf.Clamp(fighterHealth.currentHealth, 0f, fighterHealth.maxHealth);

        // Smooth bar
        shownHp = Mathf.Lerp(shownHp, targetHp, 1f - Mathf.Exp(-smooth * Time.deltaTime));
        healthBar.value = ApplyMinVisual(shownHp);

        // Text
        if (healthText)
        {
            int cur = Mathf.RoundToInt(targetHp);
            int max = Mathf.RoundToInt(fighterHealth.maxHealth);
            healthText.text = $"{cur}/{max}";
        }
    }

    private float ApplyMinVisual(float value)
    {
        if (minVisualValue <= 0f) return value;
        if (value <= 0f) return 0f;
        return Mathf.Max(value, minVisualValue);
    }

    private void UpdateHealthTextImmediate()
    {
        if (!fighterHealth || !healthText) return;

        float hp = Mathf.Clamp(fighterHealth.currentHealth, 0f, fighterHealth.maxHealth);
        int cur = Mathf.RoundToInt(hp);
        int max = Mathf.RoundToInt(fighterHealth.maxHealth);
        healthText.text = $"{cur}/{max}";
    }

    public void SetLastAction(string desc)
    {
        if (lastActionText)
            lastActionText.text = string.IsNullOrWhiteSpace(desc) ? "-" : desc;
    }

    public void ForceRefresh()
    {
        InitBar();
        UpdateHealthTextImmediate();
    }
}
