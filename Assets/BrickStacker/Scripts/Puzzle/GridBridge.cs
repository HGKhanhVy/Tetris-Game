namespace BrickStacker.Puzzle
{
    /// <summary>
    /// Bridges the controller's legacy <c>int[,]</c> grid to the pure <see cref="PuzzleBoard"/>
    /// model so the tested cluster/gravity/chain layer can run without rewriting every grid
    /// reference in the controller (snapshot, layout, obstacles) at once.
    ///
    /// Cell encoding in the int grid:
    ///   0            = empty
    ///   1..4         = ResourceType + 1  (Move=1, Attack=2, Shield=3, Energy=4)
    ///   GarbageValue = garbage cell
    /// </summary>
    public static class GridBridge
    {
        public const int GarbageValue = 5;

        public static int Encode(ResourceType type) => (int)type + 1;

        public static bool IsResourceValue(int value) => value >= 1 && value <= 4;

        public static ResourceType DecodeResource(int value) => (ResourceType)(value - 1);

        public static void Load(int[,] grid, PuzzleBoard board)
        {
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    int value = grid[x, y];
                    if (value == 0)
                        board.Set(x, y, PuzzleCell.Empty);
                    else if (IsResourceValue(value))
                        board.Set(x, y, PuzzleCell.OfResource(DecodeResource(value)));
                    else
                        // GarbageValue and any legacy non-resource value (stone/garbage 6,7…)
                        // occupy space but never join a cluster.
                        board.Set(x, y, PuzzleCell.Garbage());
                }
            }
        }

        public static void Store(PuzzleBoard board, int[,] grid)
        {
            for (int x = 0; x < board.Width; x++)
            {
                for (int y = 0; y < board.Height; y++)
                {
                    var cell = board.Get(x, y);
                    switch (cell.Kind)
                    {
                        case CellKind.Resource:
                            grid[x, y] = Encode(cell.Resource);
                            break;
                        case CellKind.Garbage:
                            grid[x, y] = GarbageValue;
                            break;
                        default:
                            grid[x, y] = 0;
                            break;
                    }
                }
            }
        }
    }
}
