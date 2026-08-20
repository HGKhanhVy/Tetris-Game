namespace BrickStacker.Puzzle
{
    /// <summary>
    /// Cluster strength by size (GD v3 §3.2).
    /// 4–5 ô = Basic, 6+ ô = Strong, dưới 4 ô không kích hoạt.
    /// </summary>
    public enum ClusterTier
    {
        None = 0,
        Basic = 1,
        Strong = 2
    }
}
