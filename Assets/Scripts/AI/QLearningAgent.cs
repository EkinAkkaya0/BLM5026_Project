using UnityEngine;
using System.Collections.Generic;

public class QLearningAgent : MonoBehaviour
{
    [Header("Q-Learning Parameters")]
    [Range(0f, 1f)]
    public float learningRate = 0.1f;           // Alpha - ne kadar hızlı öğrenir
    [Range(0f, 1f)]
    public float discountFactor = 0.95f;        // Gamma - gelecek ödüllerin önemi
    [Range(0f, 1f)]
    public float explorationRate = 1.0f;        // Epsilon - keşif oranı
    [Range(0f, 1f)]
    public float minExplorationRate = 0.01f;    // Minimum epsilon
    public float explorationDecay = 0.995f;     // Epsilon azalma oranı

    [Header("State Discretization")]
    public int distanceBins = 10;               // Mesafe için kaç kategori
    public int healthBins = 10;                 // Can için kaç kategori

    [Header("Training Stats")]
    public int totalEpisodes = 0;
    public float cumulativeReward = 0f;
    public float episodeReward = 0f;
    public int totalActionsToken = 0;

    // Action enum
    public enum Action
    {
        Idle = 0,
        Approach = 1,
        Retreat = 2,
        LightAttack = 3,
        HeavyAttack = 4,
        Block = 5
    }

    // Q-Table: Dictionary<state_string, float[]>
    // state_string: "dist_5_playerHP_8_enemyHP_7_playerAction_2"
    // float[]: 6 değer (her action için Q-value)
    private Dictionary<string, float[]> qTable = new Dictionary<string, float[]>();

    private string currentState;
    private Action currentAction;
    private float lastReward = 0f;

    private void Start()
    {
        // MatchManager event'lerine subscribe
        if (MatchManager.Instance != null)
        {
            MatchManager.Instance.OnMatchStart += OnEpisodeStart;
            MatchManager.Instance.OnMatchEnd += OnEpisodeEnd;
        }
    }

    // State'i string formatında döndür (Q-table key olarak kullanılır)
    public string GetStateString(float distance, float playerHealthRatio, float enemyHealthRatio, int playerLastAction, bool isBlocking)
    {
        // Mesafeyi discretize et (0-10 arası bin)
        int distBin = Mathf.Clamp(Mathf.FloorToInt(distance / 5f * distanceBins), 0, distanceBins - 1);
        
        // Can oranlarını discretize et
        int playerHPBin = Mathf.Clamp(Mathf.FloorToInt(playerHealthRatio * healthBins), 0, healthBins - 1);
        int enemyHPBin = Mathf.Clamp(Mathf.FloorToInt(enemyHealthRatio * healthBins), 0, healthBins - 1);
        
        // State string oluştur
        return $"{distBin}_{playerHPBin}_{enemyHPBin}_{playerLastAction}_{(isBlocking ? 1 : 0)}";
    }

    // Action seç (epsilon-greedy)
    public Action SelectAction(string state)
    {
        currentState = state;

        // Exploration vs Exploitation
        if (Random.value < explorationRate)
        {
            // Explore: Rastgele action seç
            currentAction = (Action)Random.Range(0, 6);
        }
        else
        {
            // Exploit: En iyi action'ı seç
            currentAction = GetBestAction(state);
        }

        return currentAction;
    }

    // State için en iyi action'ı döndür
    private Action GetBestAction(string state)
    {
        // Q-table'da bu state var mı?
        if (!qTable.ContainsKey(state))
        {
            InitializeState(state);
        }

        float[] qValues = qTable[state];
        int bestAction = 0;
        float bestValue = qValues[0];

        for (int i = 1; i < qValues.Length; i++)
        {
            if (qValues[i] > bestValue)
            {
                bestValue = qValues[i];
                bestAction = i;
            }
        }

        return (Action)bestAction;
    }

    // Yeni state'i initialize et (tüm Q-values 0)
    private void InitializeState(string state)
    {
        qTable[state] = new float[6]; // 6 action var
    }

    // Reward al ve Q-value'yu güncelle
    public void GiveReward(float reward, string nextState)
    {
        episodeReward += reward;
        cumulativeReward += reward;
        lastReward = reward;
        totalActionsToken++;

        // Q-Learning formülü: Q(s,a) = Q(s,a) + α[r + γ*max(Q(s',a')) - Q(s,a)]
        
        // Eski state'i kontrol et
        if (!qTable.ContainsKey(currentState))
        {
            InitializeState(currentState);
        }

        // Yeni state'i kontrol et
        if (!qTable.ContainsKey(nextState))
        {
            InitializeState(nextState);
        }

        float[] currentQValues = qTable[currentState];
        float[] nextQValues = qTable[nextState];

        // Next state'deki max Q-value'yu bul
        float maxNextQ = nextQValues[0];
        for (int i = 1; i < nextQValues.Length; i++)
        {
            if (nextQValues[i] > maxNextQ)
                maxNextQ = nextQValues[i];
        }

        // Q-value güncelle
        int actionIndex = (int)currentAction;
        float currentQ = currentQValues[actionIndex];
        float newQ = currentQ + learningRate * (reward + discountFactor * maxNextQ - currentQ);
        currentQValues[actionIndex] = newQ;

        // Güncellenen Q-table'ı kaydet
        qTable[currentState] = currentQValues;
    }

    // Episode başladığında
    private void OnEpisodeStart()
    {
        totalEpisodes++;
        episodeReward = 0f;

        // Exploration rate'i azalt
        explorationRate = Mathf.Max(minExplorationRate, explorationRate * explorationDecay);

        Debug.Log($"[Q-Learning] Episode {totalEpisodes} started | Exploration: {explorationRate:F3}");
    }

    // Episode bittiğinde
    private void OnEpisodeEnd(MatchManager.MatchResult result)
    {
        // Son episode reward'ını logla
        Debug.Log($"[Q-Learning] Episode {totalEpisodes} ended | Reward: {episodeReward:F2} | Result: {result}");
        Debug.Log($"[Q-Learning] Cumulative Reward: {cumulativeReward:F2} | Q-Table Size: {qTable.Count}");
    }

    // Q-Table'ı kaydet/yükle (opsiyonel, ileride eklenebilir)
    public void SaveQTable(string filename)
    {
        // TODO: JSON veya binary format ile kaydet
        Debug.Log($"Saving Q-Table with {qTable.Count} states...");
    }

    public void LoadQTable(string filename)
    {
        // TODO: Dosyadan yükle
        Debug.Log("Loading Q-Table...");
    }

    // Debug bilgileri
    public string GetDebugInfo()
    {
        return $"Episodes: {totalEpisodes} | Exploration: {explorationRate:F3} | " +
               $"Last Reward: {lastReward:F1} | Q-States: {qTable.Count}";
    }

    private void OnDestroy()
    {
        // Event'lerden unsubscribe
        if (MatchManager.Instance != null)
        {
            MatchManager.Instance.OnMatchStart -= OnEpisodeStart;
            MatchManager.Instance.OnMatchEnd -= OnEpisodeEnd;
        }
    }
}