using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class TrainingMetrics : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI qLearningStatsText;
    public TextMeshProUGUI perceptronStatsText;      // YENİ
    public TextMeshProUGUI recentPerformanceText;

    [Header("Agent References")]
    public QLearningAgent qAgent;
    public SimplePerceptron perceptron;              // YENİ

    [Header("Performance Tracking")]
    public int recentWindowSize = 10;

    private Queue<float> recentRewards = new Queue<float>();
    private Queue<int> recentResults = new Queue<int>();
    private float currentEpisodeStartReward = 0f;

    private void Start()
    {
        if (MatchManager.Instance != null)
        {
            MatchManager.Instance.OnMatchStart += OnEpisodeStart;
            MatchManager.Instance.OnMatchEnd += OnEpisodeEnd;
        }
    }

    private void Update()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        // Q-Learning stats
        if (qAgent != null && qLearningStatsText != null)
        {
            qLearningStatsText.text = $"=== Q-LEARNING ===\n" +
                                      $"Episodes: {qAgent.totalEpisodes}\n" +
                                      $"Exploration: {qAgent.explorationRate:F3}\n" +
                                      $"Episode Reward: {qAgent.episodeReward:F1}\n" +
                                      $"Total Reward: {qAgent.cumulativeReward:F1}\n" +
                                      $"Q-States: {qAgent.GetDebugInfo().Split('|')[3].Trim()}";
        }

        // Perceptron stats (YENİ)
        if (perceptron != null && perceptronStatsText != null)
        {
            perceptronStatsText.text = $"=== PERCEPTRON ===\n" +
                                       $"Updates: {perceptron.totalUpdates}\n" +
                                       $"Last Output: {perceptron.lastOutput:F3}\n" +
                                       $"Last Error: {perceptron.lastError:F3}\n" +
                                       $"State Eval: {(perceptron.lastOutput > 0 ? "GOOD" : "BAD")}";
        }

        // Recent performance
        if (recentPerformanceText != null && recentRewards.Count > 0)
        {
            float avgReward = GetAverageReward();
            float winRate = GetRecentWinRate();

            recentPerformanceText.text = $"=== LAST {recentWindowSize} ===\n" +
                                        $"Avg Reward: {avgReward:F1}\n" +
                                        $"Win Rate: {winRate:F1}%";
        }
    }

    private void OnEpisodeStart()
    {
        if (qAgent != null)
        {
            currentEpisodeStartReward = qAgent.cumulativeReward;
        }
    }

    private void OnEpisodeEnd(MatchManager.MatchResult result)
    {
        if (qAgent == null) return;

        float episodeReward = qAgent.cumulativeReward - currentEpisodeStartReward;
        recentRewards.Enqueue(episodeReward);

        if (recentRewards.Count > recentWindowSize)
        {
            recentRewards.Dequeue();
        }

        int resultValue = 0;
        if (result == MatchManager.MatchResult.EnemyWin)
            resultValue = 1;
        else if (result == MatchManager.MatchResult.PlayerWin)
            resultValue = -1;

        recentResults.Enqueue(resultValue);
        if (recentResults.Count > recentWindowSize)
        {
            recentResults.Dequeue();
        }

        // Perceptron log (YENİ)
        string perceptronInfo = perceptron != null ? perceptron.GetDebugInfo() : "N/A";

        Debug.Log($"[Training] Ep {qAgent.totalEpisodes} | " +
                  $"Reward: {episodeReward:F1} | Avg: {GetAverageReward():F1} | " +
                  $"Win: {GetRecentWinRate():F1}% | Perceptron: {perceptronInfo}");
    }

    private float GetAverageReward()
    {
        if (recentRewards.Count == 0) return 0f;

        float sum = 0f;
        foreach (float reward in recentRewards)
        {
            sum += reward;
        }
        return sum / recentRewards.Count;
    }

    private float GetRecentWinRate()
    {
        if (recentResults.Count == 0) return 0f;

        int wins = 0;
        foreach (int result in recentResults)
        {
            if (result == 1) wins++;
        }
        return (wins / (float)recentResults.Count) * 100f;
    }

    public void ExportMetrics(string filename)
    {
        Debug.Log($"Exporting metrics to {filename}...");
    }

    private void OnDestroy()
    {
        if (MatchManager.Instance != null)
        {
            MatchManager.Instance.OnMatchStart -= OnEpisodeStart;
            MatchManager.Instance.OnMatchEnd -= OnEpisodeEnd;
        }
    }
}