using System.Collections.Generic;
using UnityEngine;

namespace BrickStacker.Puzzle
{
    /// <summary>
    /// Finds all activatable resource clusters via 4-directional flood fill (GD v3 §3.1, §3.4).
    /// Reuses internal buffers so repeated detection during a chain avoids per-call GC (§11 mobile).
    /// One instance is bound to a fixed board size.
    /// </summary>
    public sealed class ClusterDetector
    {
        static readonly Vector2Int[] Neighbors =
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        readonly int width;
        readonly int height;
        readonly bool[,] visited;
        readonly Stack<Vector2Int> frontier = new Stack<Vector2Int>();
        readonly List<Cluster> results = new List<Cluster>();

        public ClusterDetector(int width, int height)
        {
            this.width = width;
            this.height = height;
            visited = new bool[width, height];
        }

        /// <summary>
        /// Detect every cluster of size >= 4 (Basic or Strong). Cells below the threshold are
        /// skipped. Returned list is reused between calls — copy it if you need to keep it.
        /// </summary>
        public IReadOnlyList<Cluster> DetectAll(PuzzleBoard board)
        {
            results.Clear();
            System.Array.Clear(visited, 0, visited.Length);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (visited[x, y])
                        continue;

                    var cell = board.Get(x, y);
                    if (!cell.IsResource)
                    {
                        visited[x, y] = true;
                        continue;
                    }

                    var group = Flood(board, new Vector2Int(x, y), cell.Resource);
                    var tier = Cluster.TierForSize(group.Count);
                    if (tier != ClusterTier.None)
                        results.Add(new Cluster(cell.Resource, tier, group));
                }
            }

            return results;
        }

        List<Vector2Int> Flood(PuzzleBoard board, Vector2Int start, ResourceType resource)
        {
            // A fresh list per cluster is intentional: the caller keeps cells for resolution.
            var group = new List<Vector2Int>();
            frontier.Clear();
            frontier.Push(start);
            visited[start.x, start.y] = true;

            while (frontier.Count > 0)
            {
                var current = frontier.Pop();
                group.Add(current);

                for (int i = 0; i < Neighbors.Length; i++)
                {
                    var next = current + Neighbors[i];
                    if (!board.InBounds(next) || visited[next.x, next.y])
                        continue;

                    var cell = board.Get(next.x, next.y);
                    // Hard/breakable cells break adjacency (GD §10.2), garbage/empty never link.
                    if (cell.IsResource && cell.Resource == resource)
                    {
                        visited[next.x, next.y] = true;
                        frontier.Push(next);
                    }
                }
            }

            return group;
        }
    }
}
