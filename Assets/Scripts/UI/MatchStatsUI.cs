using UnityEngine;
using TMPro;

public class MatchStatsUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI episodeCountText;
    public TextMeshProUGUI playerWinsText;
    public TextMeshProUGUI enemyWinsText;
    public TextMeshProUGUI drawsText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI matchResultText; // "PLAYER WINS!" gibi

    [Header("Settings")]
    public float resultDisplayDuration = 2f;

    private void Start()
    {
        // MatchManager event'lerine subscribe ol
        if (MatchManager.Instance != null)
        {
            MatchManager.Instance.OnMatchStart += OnMatchStart;
            MatchManager.Instance.OnMatchEnd += OnMatchEnd;
        }

        // Sonuc yazisini gizle
        if (matchResultText != null)
        {
            matchResultText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (MatchManager.Instance == null) return;

        // Episode sayisi
        if (episodeCountText != null)
        {
            episodeCountText.text = $"Episode: {MatchManager.Instance.currentEpisode}";
        }

        // Galibiyetler
        if (playerWinsText != null)
        {
            int total = MatchManager.Instance.currentEpisode;
            int wins = MatchManager.Instance.playerWins;
            float winRate = total > 0 ? (wins / (float)total) * 100f : 0f;
            playerWinsText.text = $"Player: {wins} ({winRate:F1}%)";
        }

        if (enemyWinsText != null)
        {
            int total = MatchManager.Instance.currentEpisode;
            int wins = MatchManager.Instance.enemyWins;
            float winRate = total > 0 ? (wins / (float)total) * 100f : 0f;
            enemyWinsText.text = $"Enemy: {wins} ({winRate:F1}%)";
        }

        if (drawsText != null)
        {
            int total = MatchManager.Instance.currentEpisode;
            int draws = MatchManager.Instance.draws;
            float drawRate = total > 0 ? (draws / (float)total) * 100f : 0f;
            drawsText.text = $"Draws: {draws} ({drawRate:F1}%)";
        }

        // Timer
        if (timerText != null)
        {
            float timeRemaining = MatchManager.Instance.GetMatchTimeRemaining();
            int minutes = Mathf.FloorToInt(timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(timeRemaining % 60f);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }

    private void OnMatchStart()
    {
        // Mac basladiginda sonuc yazisini gizle
        if (matchResultText != null)
        {
            matchResultText.gameObject.SetActive(false);
        }
    }

    private void OnMatchEnd(MatchManager.MatchResult result)
    {
        // Sonuc yazisini goster
        if (matchResultText != null)
        {
            matchResultText.gameObject.SetActive(true);

            switch (result)
            {
                case MatchManager.MatchResult.PlayerWin:
                    matchResultText.text = "PLAYER WINS!";
                    matchResultText.color = Color.cyan;
                    break;
                case MatchManager.MatchResult.EnemyWin:
                    matchResultText.text = "ENEMY WINS!";
                    matchResultText.color = Color.red;
                    break;
                case MatchManager.MatchResult.Draw:
                    matchResultText.text = "DRAW!";
                    matchResultText.color = Color.yellow;
                    break;
            }

            // Belli bir sure sonra gizle
            Invoke(nameof(HideResultText), resultDisplayDuration);
        }
    }

    private void HideResultText()
    {
        if (matchResultText != null)
        {
            matchResultText.gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        // Event'lerden unsubscribe ol
        if (MatchManager.Instance != null)
        {
            MatchManager.Instance.OnMatchStart -= OnMatchStart;
            MatchManager.Instance.OnMatchEnd -= OnMatchEnd;
        }
    }
}