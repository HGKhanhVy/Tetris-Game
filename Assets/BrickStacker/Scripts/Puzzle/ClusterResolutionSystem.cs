using System.Collections.Generic;
using UnityEngine;

namespace BrickStacker.Puzzle
{
    /// <summary>
    /// Orchestrates a full lock resolution (GD v3 §3.3): detect all clusters → blast adjacent
    /// garbage/breakables → remove simultaneously → gravity → repeat as a chain until stable.
    /// Pure logic: returns a <see cref="ResolutionOutcome"/> the caller turns into effects + VFX.
    /// </summary>
    public sealed class ClusterResolutionSystem
    {
        static readonly Vector2Int[] Neighbors =
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        readonly ClusterDetector detector;
        readonly GravityResolver gravity;
        readonly Dictionary<Vector2Int, int> obstacleDamage = new Dictionary<Vector2Int, int>();

        public ClusterResolutionSystem(int width, int height)
        {
            detector = new ClusterDetector(width, height);
            gravity = new GravityResolver(height);
        }

        public ClusterResolutionSystem(ClusterDetector detector, GravityResolver gravity)
        {
            this.detector = detector;
            this.gravity = gravity;
        }

        public ResolutionOutcome Resolve(PuzzleBoard board)
        {
            var outcome = new ResolutionOutcome();
            int chain = 0;

            while (true)
            {
                var detected = detector.DetectAll(board);
                if (detected.Count == 0)
                    break;

                chain++;

                // Snapshot: detector reuses its result list across the chain.
                var clusters = new List<Cluster>(detected);
                var destroyed = ApplyBlastAndRemove(board, clusters);

                outcome.Steps.Add(new ResolutionStep(clusters, chain, ComboMultiplier(chain), destroyed));

                gravity.Apply(board);
            }

            return outcome;
        }

        List<Vector2Int> ApplyBlastAndRemove(PuzzleBoard board, List<Cluster> clusters)
        {
            obstacleDamage.Clear();

            // 1) Accumulate blast damage on garbage/breakable cells touching any cluster (GD §16.2).
            foreach (var cluster in clusters)
            {
                int damage = cluster.Tier == ClusterTier.Strong ? 2 : 1;
                foreach (var cell in cluster.Cells)
                {
                    for (int i = 0; i < Neighbors.Length; i++)
                    {
                        var n = cell + Neighbors[i];
                        if (!board.InBounds(n))
                            continue;

                        var kind = board.Get(n).Kind;
                        if (kind == CellKind.Garbage || kind == CellKind.Breakable)
                            obstacleDamage[n] = (obstacleDamage.TryGetValue(n, out int d) ? d : 0) + damage;
                    }
                }
            }

            // 2) Remove all activated resource cells simultaneously.
            foreach (var cluster in clusters)
            {
                foreach (var cell in cluster.Cells)
                    board.Clear(cell.x, cell.y);
            }

            // 3) Apply blast damage; clear obstacles that reached 0 HP.
            var destroyed = new List<Vector2Int>();
            foreach (var pair in obstacleDamage)
            {
                var pos = pair.Key;
                var cell = board.Get(pos.x, pos.y);
                if (cell.IsEmpty)
                    continue; // already removed as part of a cluster this pass

                cell.Health -= pair.Value;
                if (cell.Health <= 0)
                {
                    board.Clear(pos.x, pos.y);
                    destroyed.Add(pos);
                }
                else
                {
                    board.Set(pos.x, pos.y, cell);
                }
            }

            return destroyed;
        }

        /// <summary>Chain multiplier table (GD v3 §3.5).</summary>
        public static float ComboMultiplier(int chainIndex)
        {
            switch (chainIndex)
            {
                case 1: return 1.00f;
                case 2: return 1.25f;
                case 3: return 1.50f;
                default: return 2.00f;
            }
        }
    }
}
