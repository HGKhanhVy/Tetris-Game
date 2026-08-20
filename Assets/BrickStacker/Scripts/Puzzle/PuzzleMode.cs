namespace BrickStacker.Puzzle
{
    /// <summary>
    /// Which resource set the puzzle draws from (GD v3 §4).
    /// Offline: Move + Attack + Shield. Online: Attack + Shield + Energy.
    /// </summary>
    public enum PuzzleMode
    {
        Offline = 0,
        Online = 1
    }
}
