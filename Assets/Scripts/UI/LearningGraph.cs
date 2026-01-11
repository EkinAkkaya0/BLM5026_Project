using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class LearningGraph : MonoBehaviour
{
    [Header("Graph Settings")]
    public RectTransform graphContainer;
    public GameObject dotPrefab;                // Nokta prefab (küçük bir Image)
    public Color lineColor = Color.green;
    public float graphWidth = 400f;
    public float graphHeight = 200f;
    public int maxDataPoints = 20;              // Maksimum gösterilecek episode

    [Header("Data")]
    private List<float> rewardData = new List<float>();
    private List<GameObject> dotObjects = new List<GameObject>();
    private List<GameObject> lineObjects = new List<GameObject>();

    [Header("Display")]
    public TMPro.TextMeshProUGUI maxRewardText;
    public TMPro.TextMeshProUGUI minRewardText;

    private float maxReward = 100f;
    private float minReward = -100f;

    private void Start()
    {
        // MatchManager event'lerine subscribe
        if (MatchManager.Instance != null)
        {
            MatchManager.Instance.OnMatchEnd += OnEpisodeEnd;
        }
    }

    private void OnEpisodeEnd(MatchManager.MatchResult result)
    {
        // TrainingMetrics'ten son episode reward'ını al
        TrainingMetrics metrics = FindObjectOfType<TrainingMetrics>();
        if (metrics != null && metrics.qAgent != null)
        {
            float episodeReward = metrics.qAgent.episodeReward;
            AddDataPoint(episodeReward);
        }
    }

    public void AddDataPoint(float reward)
    {
        rewardData.Add(reward);

        // Max veri sayısını aşarsa en eskiyi sil
        if (rewardData.Count > maxDataPoints)
        {
            rewardData.RemoveAt(0);
        }

        // Grafiği yeniden çiz
        UpdateGraph();
    }

    private void UpdateGraph()
    {
        // Eski nokta ve çizgileri temizle
        foreach (GameObject dot in dotObjects)
        {
            Destroy(dot);
        }
        dotObjects.Clear();

        foreach (GameObject line in lineObjects)
        {
            Destroy(line);
        }
        lineObjects.Clear();

        if (rewardData.Count == 0) return;

        // Min ve max reward'ı bul
        maxReward = rewardData[0];
        minReward = rewardData[0];
        foreach (float reward in rewardData)
        {
            if (reward > maxReward) maxReward = reward;
            if (reward < minReward) minReward = reward;
        }

        // Biraz padding ekle
        float range = maxReward - minReward;
        if (range < 10f) range = 10f; // Minimum range
        maxReward += range * 0.1f;
        minReward -= range * 0.1f;

        // Min/Max text'leri güncelle
        if (maxRewardText != null)
            maxRewardText.text = $"{maxReward:F1}";
        if (minRewardText != null)
            minRewardText.text = $"{minReward:F1}";

        // Noktaları ve çizgileri çiz
        GameObject previousDot = null;
        for (int i = 0; i < rewardData.Count; i++)
        {
            float xPos = (i / (float)(maxDataPoints - 1)) * graphWidth;
            float normalizedValue = (rewardData[i] - minReward) / (maxReward - minReward);
            float yPos = normalizedValue * graphHeight;

            // Nokta oluştur
            GameObject dot = CreateDot(new Vector2(xPos, yPos));
            dotObjects.Add(dot);

            // Çizgi oluştur (önceki noktaya)
            if (previousDot != null)
            {
                GameObject line = CreateLine(previousDot.GetComponent<RectTransform>().anchoredPosition,
                                            dot.GetComponent<RectTransform>().anchoredPosition);
                lineObjects.Add(line);
            }

            previousDot = dot;
        }
    }

    private GameObject CreateDot(Vector2 position)
    {
        GameObject dot;
        
        if (dotPrefab != null)
        {
            dot = Instantiate(dotPrefab, graphContainer);
        }
        else
        {
            // Prefab yoksa basit bir Image oluştur
            dot = new GameObject("Dot", typeof(RectTransform));
            dot.transform.SetParent(graphContainer, false);
            Image image = dot.AddComponent<Image>();
            image.color = lineColor;
        }

        RectTransform rectTransform = dot.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(8, 8); // Nokta boyutu
        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);

        return dot;
    }

    private GameObject CreateLine(Vector2 startPos, Vector2 endPos)
    {
        GameObject line = new GameObject("Line", typeof(RectTransform));
        line.transform.SetParent(graphContainer, false);
        Image image = line.AddComponent<Image>();
        image.color = lineColor;

        RectTransform rectTransform = line.GetComponent<RectTransform>();
        
        Vector2 dir = (endPos - startPos).normalized;
        float distance = Vector2.Distance(startPos, endPos);

        rectTransform.anchorMin = new Vector2(0, 0);
        rectTransform.anchorMax = new Vector2(0, 0);
        rectTransform.sizeDelta = new Vector2(distance, 3f); // Çizgi kalınlığı
        rectTransform.anchoredPosition = startPos + dir * distance * 0.5f;
        
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rectTransform.localEulerAngles = new Vector3(0, 0, angle);

        return line;
    }

    private void OnDestroy()
    {
        if (MatchManager.Instance != null)
        {
            MatchManager.Instance.OnMatchEnd -= OnEpisodeEnd;
        }
    }

    // Manuel test için
    [ContextMenu("Add Test Data")]
    public void AddTestData()
    {
        AddDataPoint(Random.Range(-50f, 150f));
    }
}   