using System.Collections.Generic;
using UnityEngine;

namespace BrickStacker.Puzzle
{
    /// <summary>
    /// Assigns resources to the cells of a piece with a controlled distinct-type ratio
    /// (GD v3 §2.1: default 15% one type / 55% two / 30% three). Shared by
    /// <see cref="ResourcePieceGenerator"/> and the game controller so the rule lives in one place.
    /// </summary>
    public static class ResourceAssigner
    {
        /// <summary>
        /// Fill <paramref name="outResources"/> in place. Draws distinct types from
        /// <paramref name="bag"/> to respect resource frequency, guarantees each selected type
        /// appears once, then fills and shuffles the remaining cells.
        /// Pass a reusable <paramref name="distinctBuffer"/> to avoid per-call allocation.
        /// </summary>
        public static void Assign(ResourceBagService bag, System.Random rng, ResourceType[] outResources,
            float oneTypeRatio = 0.15f, float twoTypeRatio = 0.55f, List<ResourceType> distinctBuffer = null)
        {
            int cellCount = outResources.Length;
            int typeCount = RollTypeCount(rng, cellCount, oneTypeRatio, twoTypeRatio);

            var distinct = distinctBuffer ?? new List<ResourceType>(3);
            distinct.Clear();
            int guard = 0;
            while (distinct.Count < typeCount && guard++ < 64)
            {
                var type = bag.Next();
                if (!distinct.Contains(type))
                    distinct.Add(type);
            }

            for (int i = 0; i < distinct.Count; i++)
                outResources[i] = distinct[i];
            for (int i = distinct.Count; i < cellCount; i++)
                outResources[i] = distinct[rng.Next(distinct.Count)];

            for (int i = cellCount - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (outResources[i], outResources[j]) = (outResources[j], outResources[i]);
            }
        }

        static int RollTypeCount(System.Random rng, int cellCount, float oneTypeRatio, float twoTypeRatio)
        {
            double r = rng.NextDouble();
            int t;
            if (r < oneTypeRatio)
                t = 1;
            else if (r < oneTypeRatio + twoTypeRatio)
                t = 2;
            else
                t = 3;

            return Mathf.Clamp(t, 1, Mathf.Min(cellCount, 3));
        }
    }
}
