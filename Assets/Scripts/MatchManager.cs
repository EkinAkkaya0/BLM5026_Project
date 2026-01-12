using UnityEngine;
using System;
using System.Collections;

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

    [Header("End/Restart Timing")]
    public float endDelayForDeathAnim = 1.0f; // death animasyonunu oynatmak için bekleme
    public float restartDelayAfterEnd = 1.0f; // EndMatch log/stat sonrası yeni maç gecikmesi

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

    private bool endRequested = false;
    private Coroutine endRoutine;

    public enum MatchResult
    {
        PlayerWin,
        EnemyWin,
        Draw
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        StartMatch();
    }

    private void Update()
    {
        if (!matchInProgress) return;
        if (endRequested) return;

        // Timer kontrolü
        matchTimer -= Time.deltaTime;
        if (matchTimer <= 0f)
        {
            RequestEndMatch(MatchResult.Draw, 0f);
            return;
        }

        // Can kontrolü (maçı burada hemen bitirmiyoruz, death animasyonuna süre veriyoruz)
        if (playerHealth != null && playerHealth.currentHealth <= 0)
        {
            RequestEndMatch(MatchResult.EnemyWin, endDelayForDeathAnim);
        }
        else if (enemyHealth != null && enemyHealth.currentHealth <= 0)
        {
            RequestEndMatch(MatchResult.PlayerWin, endDelayForDeathAnim);
        }
    }

    public void StartMatch()
    {
        // önceki invoke/coroutine temizle
        CancelInvoke(nameof(StartMatch));
        if (endRoutine != null) StopCoroutine(endRoutine);
        endRoutine = null;

        endRequested = false;

        currentEpisode++;
        matchInProgress = true;
        matchTimer = matchDuration;

        ResetPositions();
        ResetHealth();

        OnMatchStart?.Invoke();
        Debug.Log($"=== EPISODE {currentEpisode} BAŞLADI ===");
    }

    private void RequestEndMatch(MatchResult result, float delay)
    {
        if (endRequested) return;
        endRequested = true;

        // AI vs dursun diye match'i “in progress” olmaktan çıkar
        matchInProgress = false;

        if (endRoutine != null) StopCoroutine(endRoutine);
        endRoutine = StartCoroutine(EndMatchAfterDelay(result, delay));
    }

    private IEnumerator EndMatchAfterDelay(MatchResult result, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        EndMatchNow(result);
    }

    private void EndMatchNow(MatchResult result)
    {
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

        OnMatchEnd?.Invoke(result);
        LogMatchStats();

        // restart
        Invoke(nameof(StartMatch), restartDelayAfterEnd);
    }

    private void ResetPositions()
    {
        if (playerTransform != null)
        {
            playerTransform.position = playerStartPosition;
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
        if (playerHealth != null) playerHealth.ResetHealth();
        if (enemyHealth != null) enemyHealth.ResetHealth();
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

    // Debug için
    public void ForceEndMatch(MatchResult result)
    {
        RequestEndMatch(result, 0f);
    }

    public bool IsMatchInProgress()
    {
        return matchInProgress;
    }

    public float GetMatchTimeRemaining()
    {
        return matchTimer;
    }
}
