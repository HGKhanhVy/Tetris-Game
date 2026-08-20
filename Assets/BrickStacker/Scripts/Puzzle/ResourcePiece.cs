using UnityEngine;

namespace BrickStacker.Puzzle
{
    /// <summary>
    /// A falling model: shape offsets plus a resource per cell (GD v3 §2.1). During the fall the
    /// cells move and rotate together; after locking the board stores each resource independently.
    /// <see cref="Resources"/> is parallel to <see cref="BaseCells"/> and to the output of
    /// <see cref="WriteCells"/>, so index i always maps to the same resource through any rotation.
    /// </summary>
    public sealed class ResourcePiece
    {
        public Vector2Int[] BaseCells { get; }
        public ResourceType[] Resources { get; }

        public Vector2Int Origin;
        public int Rotation;

        public int CellCount => BaseCells.Length;

        public ResourcePiece(Vector2Int[] baseCells, ResourceType[] resources)
        {
            BaseCells = baseCells;
            Resources = resources;
        }

        /// <summary>Rotate an offset clockwise by the given quarter-turn count.</summary>
        public static Vector2Int RotateOffset(Vector2Int c, int rotation)
        {
            switch (rotation & 3)
            {
                case 1: return new Vector2Int(c.y, -c.x);
                case 2: return new Vector2Int(-c.x, -c.y);
                case 3: return new Vector2Int(-c.y, c.x);
                default: return c;
            }
        }

        /// <summary>
        /// Fill <paramref name="outCells"/> (length must equal <see cref="CellCount"/>) with the
        /// board positions for the given origin and rotation, re-normalized to the origin.
        /// Allocation-free so it is safe to call every frame while the piece falls.
        /// </summary>
        public void WriteCells(Vector2Int origin, int rotation, Vector2Int[] outCells)
        {
            int minX = int.MaxValue;
            int minY = int.MaxValue;
            for (int i = 0; i < BaseCells.Length; i++)
            {
                var r = RotateOffset(BaseCells[i], rotation);
                outCells[i] = r;
                if (r.x < minX) minX = r.x;
                if (r.y < minY) minY = r.y;
            }

            for (int i = 0; i < outCells.Length; i++)
                outCells[i] = new Vector2Int(origin.x + outCells[i].x - minX, origin.y + outCells[i].y - minY);
        }
    }
}
