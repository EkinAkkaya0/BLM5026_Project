using System.Collections.Generic;
using UnityEngine;

public class PathNodeManager : MonoBehaviour
{
    public List<PathNode> nodes = new List<PathNode>();

    private void Awake()
    {
        RefreshNodes();
    }

    private void OnValidate()
    {
        RefreshNodes();
    }

    private void RefreshNodes()
    {
        nodes.Clear();

        // Bu objenin çocuklarındaki tüm PathNode componentlerini topla
        GetComponentsInChildren<PathNode>(includeInactive: true, result: nodes);

        // X pozisyonuna göre sırala (soldan sağa)
        nodes.Sort((a, b) =>
            a.transform.position.x.CompareTo(b.transform.position.x)
        );

        // Lineer komşuluk: her node önceki ve sonraki ile bağlı
        for (int i = 0; i < nodes.Count; i++)
        {
            nodes[i].neighbors.Clear();

            if (i > 0)
                nodes[i].neighbors.Add(nodes[i - 1]);

            if (i < nodes.Count - 1)
                nodes[i].neighbors.Add(nodes[i + 1]);
        }
    }

    // Verilen dünya pozisyonuna en yakın nodu bul
    public PathNode GetClosestNode(Vector3 worldPos)
    {
        PathNode closest = null;
        float bestDist = float.MaxValue;

        foreach (var n in nodes)
        {
            float d = Vector2.SqrMagnitude((Vector2)(n.transform.position - worldPos));
            if (d < bestDist)
            {
                bestDist = d;
                closest = n;
            }
        }

        return closest;
    }

    // --- A* PATHFINDING ---

    public List<PathNode> FindPath(PathNode start, PathNode goal)
    {
        if (start == null || goal == null)
            return null;

        var openSet = new List<PathNode>();
        var closedSet = new HashSet<PathNode>();

        var cameFrom = new Dictionary<PathNode, PathNode>();
        var gScore = new Dictionary<PathNode, float>();
        var fScore = new Dictionary<PathNode, float>();

        // Tüm nodelar için başlangıç skorları
        foreach (var node in nodes)
        {
            gScore[node] = Mathf.Infinity;
            fScore[node] = Mathf.Infinity;
        }

        gScore[start] = 0f;
        fScore[start] = Heuristic(start, goal);

        openSet.Add(start);

        while (openSet.Count > 0)
        {
            // openSet içinden en düşük fScore'a sahip olanı bul
            PathNode current = openSet[0];
            float bestF = fScore[current];

            for (int i = 1; i < openSet.Count; i++)
            {
                PathNode n = openSet[i];
                float f = fScore[n];
                if (f < bestF)
                {
                    bestF = f;
                    current = n;
                }
            }

            // Hedefe ulaştık
            if (current == goal)
            {
                return ReconstructPath(cameFrom, current);
            }

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (var neighbor in current.neighbors)
            {
                if (neighbor == null || closedSet.Contains(neighbor))
                    continue;

                float tentativeG = gScore[current] +
                                   Vector2.Distance(current.transform.position,
                                                    neighbor.transform.position);

                if (!openSet.Contains(neighbor))
                {
                    openSet.Add(neighbor);
                }
                else if (tentativeG >= gScore[neighbor])
                {
                    continue; // Daha iyi bir yol değil
                }

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeG;
                fScore[neighbor] = tentativeG + Heuristic(neighbor, goal);
            }
        }

        // Yol bulunamadı
        return null;
    }

    private float Heuristic(PathNode a, PathNode b)
    {
        // Düz çizgi mesafe (bizim lineer arenada zaten gayet yeterli)
        return Vector2.Distance(a.transform.position, b.transform.position);
    }

    private List<PathNode> ReconstructPath(Dictionary<PathNode, PathNode> cameFrom, PathNode current)
    {
        var totalPath = new List<PathNode> { current };

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            totalPath.Insert(0, current);
        }

        return totalPath;
    }
}
