using UnityEngine;

namespace BrickStacker.Puzzle
{
    /// <summary>
    /// Data container for the resource grid (GD v3 §2.4). Pure state + queries only;
    /// cluster detection, gravity and chain logic live in dedicated resolvers (SRP).
    /// Coordinate convention matches the existing puzzle: (0,0) = bottom-left, y grows up.
    /// </summary>
    public sealed class PuzzleBoard
    {
        readonly PuzzleCell[,] cells;

        public int Width { get; }
        public int Height { get; }

        public PuzzleBoard(int width, int height)
        {
            Width = width;
            Height = height;
            cells = new PuzzleCell[width, height];
        }

        public bool InBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

        public bool InBounds(Vector2Int p) => InBounds(p.x, p.y);

        public PuzzleCell Get(int x, int y) => cells[x, y];

        public PuzzleCell Get(Vector2Int p) => cells[p.x, p.y];

        public void Set(int x, int y, PuzzleCell cell) => cells[x, y] = cell;

        public void Set(Vector2Int p, PuzzleCell cell) => cells[p.x, p.y] = cell;

        public void Clear(int x, int y) => cells[x, y] = PuzzleCell.Empty;

        /// <summary>True nếu ô hợp lệ và đang trống — dùng khi đặt mô hình rơi.</summary>
        public bool IsFree(int x, int y) => InBounds(x, y) && cells[x, y].IsEmpty;

        public bool IsFree(Vector2Int p) => IsFree(p.x, p.y);

        /// <summary>Bàn bị "đầy" khi hàng trên cùng đã có ô chiếm chỗ (GD §5.2 / §17).</summary>
        public bool IsTopRowBlocked()
        {
            int top = Height - 1;
            for (int x = 0; x < Width; x++)
            {
                if (cells[x, top].IsSolid)
                    return true;
            }
            return false;
        }

        public void ResetAll()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                    cells[x, y] = PuzzleCell.Empty;
            }
        }
    }
}
