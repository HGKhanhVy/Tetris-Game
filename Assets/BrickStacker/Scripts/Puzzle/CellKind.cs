namespace BrickStacker.Puzzle
{
    /// <summary>
    /// What a board cell currently holds (GD v3 §10, §16).
    /// A cell is either empty, a settled resource, an incoming garbage cell,
    /// a hard locked cell, or a breakable obstacle.
    /// </summary>
    public enum CellKind
    {
        Empty = 0,
        Resource = 1,   // mang một ResourceType, tham gia ghép cụm
        Garbage = 2,    // chiếm chỗ, không ghép cụm, bị phá bởi vụ nổ cụm sát cạnh
        LockedHard = 3, // cố định, không phá, chặn liên kết cụm
        Breakable = 4   // cố định, phá khi cụm kích hoạt sát cạnh
    }
}
