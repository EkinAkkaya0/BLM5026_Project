using UnityEngine;
using System;

public class MatchManager : MonoBehaviour
{
    public static MatchManager Instance { get; private set; }

    [Header("Fighter References")]
    public FighterHealth playerHealth;
    public FighterHealth enemyHealth;
    public Transform playerTransform;
    public Transform enemyTransform;

    [Header("Match Settings")]
    public float matchDuration = 90f; // 90 saniye timeout
    public Vector3 playerStartPosition = new Vector3(-3f, 0.5f, 0f);
    public Vector3 enemyStartPosition = new Vector3(3f, 0.5f, 0f);

    [Header("Match Stats")]
    public int currentEpisode = 0;
    public int playerWins = 0;
    public int enemyWins = 0;
    public int draws = 0;

    // Events
    public event Action<MatchResult> OnMatchEnd;
    public event Action OnMatchStart;

    private float matchTimer;
    private bool matchInProgress = false;

    public enum MatchResult
    {
        PlayerWin,
        EnemyWin,
        Draw
    }

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // İlk maçı başlat
        StartMatch();
    }

    private void Update()
    {
        if (!matchInProgress) return;

        // Timer kontrolü
        matchTimer -= Time.deltaTime;
        if (matchTimer <= 0f)
        {
            EndMatch(MatchResult.Draw);
        }

        // Can kontrolü
        if (playerHealth != null && playerHealth.currentHealth <= 0)
        {
            EndMatch(MatchResult.EnemyWin);
        }
        else if (enemyHealth != null && enemyHealth.currentHealth <= 0)
        {
            EndMatch(MatchResult.PlayerWin);
        }
    }

    public void StartMatch()
    {
        currentEpisode++;
        matchInProgress = true;
        matchTimer = matchDuration;

        // Pozisyonları resetle
        ResetPositions();

        // Canları doldur
        ResetHealth();

        // Event fırlat
        OnMatchStart?.Invoke();

        Debug.Log($"=== EPISODE {currentEpisode} BAŞLADI ===");
    }

    private void EndMatch(MatchResult result)
    {
        if (!matchInProgress) return; // Zaten bittiyse tekrar çağırma

        matchInProgress = false;

        // İstatistikleri güncelle
        switch (result)
        {
            case MatchResult.PlayerWin:
                playerWins++;
                Debug.Log($">>> PLAYER KAZANDI! (Episode {currentEpisode})");
                break;
            case MatchResult.EnemyWin:
                enemyWins++;
                Debug.Log($">>> ENEMY KAZANDI! (Episode {currentEpisode})");
                break;
            case MatchResult.Draw:
                draws++;
                Debug.Log($">>> BERABERE! (Episode {currentEpisode})");
                break;
        }

        // Event fırlat
        OnMatchEnd?.Invoke(result);

        // İstatistikleri logla
        LogMatchStats();

        // 2 saniye sonra yeni maç başlat
        Invoke(nameof(StartMatch), 2f);
    }

    private void ResetPositions()
    {
        if (playerTransform != null)
        {
            playerTransform.position = playerStartPosition;
            // Velocity'yi sıfırla
            Rigidbody2D playerRb = playerTransform.GetComponent<Rigidbody2D>();
            if (playerRb != null)
            {
                playerRb.linearVelocity = Vector2.zero;
                playerRb.angularVelocity = 0f;
            }
        }

        if (enemyTransform != null)
        {
            enemyTransform.position = enemyStartPosition;
            // Velocity'yi sıfırla
            Rigidbody2D enemyRb = enemyTransform.GetComponent<Rigidbody2D>();
            if (enemyRb != null)
            {
                enemyRb.linearVelocity = Vector2.zero;
                enemyRb.angularVelocity = 0f;
            }
        }
    }

    private void ResetHealth()
    {
        if (playerHealth != null)
        {
            playerHealth.ResetHealth();
        }

        if (enemyHealth != null)
        {
            enemyHealth.ResetHealth();
        }
    }

    private void LogMatchStats()
    {
        Debug.Log($"=== TOPLAM İSTATİSTİKLER ===");
        Debug.Log($"Toplam Episode: {currentEpisode}");
        Debug.Log($"Player Galibiyetleri: {playerWins} ({GetWinRate(playerWins)}%)");
        Debug.Log($"Enemy Galibiyetleri: {enemyWins} ({GetWinRate(enemyWins)}%)");
        Debug.Log($"Beraberlikler: {draws} ({GetWinRate(draws)}%)");
    }

    private float GetWinRate(int wins)
    {
        if (currentEpisode == 0) return 0f;
        return (wins / (float)currentEpisode) * 100f;
    }

    // Manuel maç bitiş (debugging için)
    public void ForceEndMatch(MatchResult result)
    {
        EndMatch(result);
    }

    // Getter'lar
    public bool IsMatchInProgress()
    {
        return matchInProgress;
    }

    public float GetMatchTimeRemaining()
    {
        return matchTimer;
    }
}