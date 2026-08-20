using UnityEngine;

namespace BrickStacker.Puzzle
{
    /// <summary>
    /// Piece shapes for BlockFall (GD v3 §2.3): 3–4 cell triominoes and tetrominoes.
    /// Offsets are normalized with min at (0,0); rotation is applied by <see cref="ResourcePiece"/>.
    /// </summary>
    public static class ShapeLibrary
    {
        public static readonly Vector2Int[][] Shapes =
        {
            // Triominoes (3 ô)
            new[] { V(0, 0), V(1, 0), V(2, 0) },            // I3
            new[] { V(0, 0), V(1, 0), V(1, 1) },            // L3 (góc)

            // Tetrominoes (4 ô)
            new[] { V(0, 0), V(1, 0), V(0, 1), V(1, 1) },   // O
            new[] { V(0, 0), V(1, 0), V(2, 0), V(3, 0) },   // I4
            new[] { V(0, 0), V(1, 0), V(2, 0), V(1, 1) },   // T
            new[] { V(0, 0), V(1, 0), V(1, 1), V(2, 1) },   // S
            new[] { V(1, 0), V(2, 0), V(0, 1), V(1, 1) },   // Z
            new[] { V(0, 0), V(1, 0), V(2, 0), V(2, 1) },   // L
            new[] { V(0, 0), V(1, 0), V(2, 0), V(0, 1) }    // J
        };

        public static int Count => Shapes.Length;

        public static Vector2Int[] Get(int index) => Shapes[index];

        static Vector2Int V(int x, int y) => new Vector2Int(x, y);
    }
}
