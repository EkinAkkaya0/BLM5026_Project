using System.Collections.Generic;
using UnityEngine;

public class PathNode : MonoBehaviour
{
    [Tooltip("Bu nodun bağlı olduğu komşu nodelar")]
    public List<PathNode> neighbors = new List<PathNode>();

    private void OnDrawGizmos()
    {
        // Node noktası
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.position, 0.15f);

        // Komşulara çizgi
        Gizmos.color = Color.yellow;
        foreach (var n in neighbors)
        {
            if (n != null)
            {
                Gizmos.DrawLine(transform.position, n.transform.position);
            }
        }
    }
}
