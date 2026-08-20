using System.Collections.Generic;
using UnityEngine;

namespace BrickStacker.Puzzle
{
    /// <summary>
    /// Produces the next falling model (GD v3 §2.1). Shape is picked from <see cref="ShapeLibrary"/>;
    /// the number of distinct resource types per piece is controlled (default 15% / 55% / 30% for
    /// one / two / three types) instead of pure random, while per-cell resources come from the
    /// weighted <see cref="ResourceBagService"/>.
    /// </summary>
    public sealed class ResourcePieceGenerator
    {
        readonly ResourceBagService bag;
        readonly System.Random rng;
        readonly float oneTypeRatio;
        readonly float twoTypeRatio;
        readonly List<ResourceType> distinctBuffer = new List<ResourceType>(3);

        public ResourcePieceGenerator(ResourceBagService bag, System.Random rng = null,
            float oneTypeRatio = 0.15f, float twoTypeRatio = 0.55f)
        {
            this.bag = bag;
            this.rng = rng ?? new System.Random();
            this.oneTypeRatio = oneTypeRatio;
            this.twoTypeRatio = twoTypeRatio;
        }

        public ResourcePiece Next()
        {
            var shape = (Vector2Int[])ShapeLibrary.Get(rng.Next(ShapeLibrary.Count)).Clone();
            var resources = new ResourceType[shape.Length];
            ResourceAssigner.Assign(bag, rng, resources, oneTypeRatio, twoTypeRatio, distinctBuffer);
            return new ResourcePiece(shape, resources);
        }
    }
}
