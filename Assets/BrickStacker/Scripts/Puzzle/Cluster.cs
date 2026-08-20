using System.Collections.Generic;
using UnityEngine;

namespace BrickStacker.Puzzle
{
    /// <summary>
    /// A connected group of same-resource cells found by <see cref="ClusterDetector"/> (GD v3 §3).
    /// </summary>
    public sealed class Cluster
    {
        public ResourceType Resource { get; }
        public ClusterTier Tier { get; }
        public IReadOnlyList<Vector2Int> Cells { get; }

        public int Size => Cells.Count;

        public Cluster(ResourceType resource, ClusterTier tier, IReadOnlyList<Vector2Int> cells)
        {
            Resource = resource;
            Tier = tier;
            Cells = cells;
        }

        public static ClusterTier TierForSize(int size)
        {
            if (size >= 6)
                return ClusterTier.Strong;
            if (size >= 4)
                return ClusterTier.Basic;
            return ClusterTier.None;
        }
    }
}
