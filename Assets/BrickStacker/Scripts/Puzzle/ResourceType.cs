namespace BrickStacker.Puzzle
{
    /// <summary>
    /// Resource carried by a single settled cell (GD v3 §2.2).
    /// Order matches the block sprite / resource-bag indices, do not reorder.
    /// </summary>
    public enum ResourceType
    {
        Move = 0,   // Đôi giày   – Offline: tích Movement Point
        Attack = 1, // Thanh kiếm – Offline knockback / Online auto attack
        Shield = 2, // Tấm khiên  – phòng thủ
        Energy = 3  // Sấm sét    – Online: nạp Energy dùng Skill
    }
}
