using UnityEngine;

namespace BrickStacker.Puzzle
{
    /// <summary>
    /// A single board cell. Value type kept small to avoid GC on the grid (GD v3 §11 mobile).
    /// </summary>
    public struct PuzzleCell
    {
        public CellKind Kind;
        public ResourceType Resource;
        public int Health; // dùng cho Garbage / Breakable

        public bool IsEmpty => Kind == CellKind.Empty;
        public bool IsResource => Kind == CellKind.Resource;

        /// <summary>Chiếm chỗ: chặn mô hình đang rơi và tính là "đầy bàn".</summary>
        public bool IsSolid => Kind != CellKind.Empty;

        /// <summary>Ô cố định không rơi theo gravity (GD §10.2).</summary>
        public bool IsFixed => Kind == CellKind.LockedHard || Kind == CellKind.Breakable;

        /// <summary>Chặn liên kết cụm đi xuyên qua (GD §10.2 ô khóa cứng).</summary>
        public bool BlocksLink => Kind == CellKind.LockedHard || Kind == CellKind.Breakable;

        public static PuzzleCell Empty => new PuzzleCell { Kind = CellKind.Empty };

        public static PuzzleCell OfResource(ResourceType type)
            => new PuzzleCell { Kind = CellKind.Resource, Resource = type };

        public static PuzzleCell Garbage()
            => new PuzzleCell { Kind = CellKind.Garbage, Health = 1 };

        public static PuzzleCell Hard()
            => new PuzzleCell { Kind = CellKind.LockedHard };

        public static PuzzleCell BreakableObstacle(int health)
            => new PuzzleCell { Kind = CellKind.Breakable, Health = Mathf.Max(1, health) };
    }
}
