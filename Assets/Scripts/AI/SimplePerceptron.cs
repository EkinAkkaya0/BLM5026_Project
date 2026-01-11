using UnityEngine;

public class SimplePerceptron : MonoBehaviour
{
    [Header("Perceptron Settings")]
    [Range(0f, 1f)]
    public float learningRate = 0.1f;
    public int inputSize = 5;

    [Header("Training Stats")]
    public int totalUpdates = 0;
    public float lastOutput = 0f;
    public float lastError = 0f;

    private float[] weights;
    private float bias;

    private void Awake()
    {
        InitializeWeights();
    }

    private void InitializeWeights()
    {
        weights = new float[inputSize];
        
        for (int i = 0; i < inputSize; i++)
        {
            weights[i] = Random.Range(-0.5f, 0.5f);
        }
        
        bias = Random.Range(-0.5f, 0.5f);

        Debug.Log($"[Perceptron] Initialized with {inputSize} inputs");
    }

    // Custom Tanh function (Unity'nin eski versiyonlarında Mathf.Tanh yok)
    private float Tanh(float x)
    {
        if (x > 10f) return 1f;
        if (x < -10f) return -1f;
        
        float exp2x = Mathf.Exp(2f * x);
        return (exp2x - 1f) / (exp2x + 1f);
    }

    public float Evaluate(float[] inputs)
    {
        if (inputs.Length != inputSize)
        {
            Debug.LogError($"[Perceptron] Wrong input size! Expected {inputSize}, got {inputs.Length}");
            return 0f;
        }

        float sum = bias;
        for (int i = 0; i < inputSize; i++)
        {
            sum += inputs[i] * weights[i];
        }

        // Tanh activation
        lastOutput = Tanh(sum);

        return lastOutput;
    }

    public void Train(float[] inputs, float targetOutput)
    {
        if (inputs.Length != inputSize)
        {
            Debug.LogError($"[Perceptron] Wrong input size for training!");
            return;
        }

        float predictedOutput = Evaluate(inputs);
        lastError = targetOutput - predictedOutput;

        // Tanh derivative
        float derivative = 1f - (predictedOutput * predictedOutput);

        // Gradient descent
        for (int i = 0; i < inputSize; i++)
        {
            float gradient = lastError * derivative * inputs[i];
            weights[i] += learningRate * gradient;
        }

        bias += learningRate * lastError * derivative;
        totalUpdates++;
    }

    public float[] CreateInputVector(float distance, float playerHealthRatio, float enemyHealthRatio, bool playerBlocking, float lastActionSuccess)
    {
        float[] inputs = new float[5];

        inputs[0] = Mathf.Clamp01(distance / 5f);
        inputs[1] = Mathf.Clamp01(playerHealthRatio);
        inputs[2] = Mathf.Clamp01(enemyHealthRatio);
        inputs[3] = playerBlocking ? 1f : 0f;
        inputs[4] = Mathf.Clamp(lastActionSuccess, -1f, 1f);

        return inputs;
    }

    public float CalculateTargetOutput(float reward, float currentEvaluation)
    {
        float target = currentEvaluation;

        if (reward > 0)
        {
            target += 0.1f * Mathf.Sign(reward);
        }
        else if (reward < 0)
        {
            target -= 0.1f * Mathf.Abs(Mathf.Sign(reward));
        }

        return Mathf.Clamp(target, -1f, 1f);
    }

    public string GetDebugInfo()
    {
        return $"Updates: {totalUpdates} | Last Output: {lastOutput:F3} | Last Error: {lastError:F3}";
    }

    public void LogWeights()
    {
        string weightsStr = "Weights: [";
        for (int i = 0; i < weights.Length; i++)
        {
            weightsStr += $"{weights[i]:F3}";
            if (i < weights.Length - 1) weightsStr += ", ";
        }
        weightsStr += $"] | Bias: {bias:F3}";
        Debug.Log($"[Perceptron] {weightsStr}");
    }

    public void SaveWeights(string filename)
    {
        Debug.Log($"[Perceptron] Saving weights to {filename}...");
    }

    public void LoadWeights(string filename)
    {
        Debug.Log($"[Perceptron] Loading weights from {filename}...");
    }

    public void Reset()
    {
        InitializeWeights();
        totalUpdates = 0;
        lastOutput = 0f;
        lastError = 0f;
        Debug.Log("[Perceptron] Reset completed");
    }
}