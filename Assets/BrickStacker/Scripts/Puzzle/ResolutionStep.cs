using System.Collections.Generic;
using UnityEngine;

namespace BrickStacker.Puzzle
{
    /// <summary>
    /// One chain link of a lock resolution (GD v3 §3.5). Describes the clusters activated
    /// simultaneously on this pass plus any garbage/breakable cells destroyed by their blast.
    /// Controllers read this to drive effects (moves, attack, shield, energy, garbage) and VFX.
    /// </summary>
    public sealed class ResolutionStep
    {
        public IReadOnlyList<Cluster> Clusters { get; }
        public int ChainIndex { get; }        // 1-based
        public float Multiplier { get; }
        public IReadOnlyList<Vector2Int> DestroyedObstacles { get; }

        public ResolutionStep(IReadOnlyList<Cluster> clusters, int chainIndex, float multiplier,
            IReadOnlyList<Vector2Int> destroyedObstacles)
        {
            Clusters = clusters;
            ChainIndex = chainIndex;
            Multiplier = multiplier;
            DestroyedObstacles = destroyedObstacles;
        }
    }
}
