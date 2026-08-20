namespace BrickStacker.Puzzle
{
    /// <summary>
    /// Applies per-column independent gravity (GD v3 §2.4). Movable cells (resource, garbage)
    /// fall into the lowest free rows; fixed cells (hard, breakable) stay anchored and act as
    /// floors, so cells above them cannot fall past (GD §10.2).
    /// </summary>
    public sealed class GravityResolver
    {
        readonly PuzzleCell[] columnBuffer;

        public GravityResolver(int height)
        {
            columnBuffer = new PuzzleCell[height];
        }

        /// <summary>Compact every column. Returns true if any cell moved.</summary>
        public bool Apply(PuzzleBoard board)
        {
            bool moved = false;
            for (int x = 0; x < board.Width; x++)
                moved |= ApplyColumn(board, x);
            return moved;
        }

        bool ApplyColumn(PuzzleBoard board, int x)
        {
            int height = board.Height;
            for (int y = 0; y < height; y++)
                columnBuffer[y] = PuzzleCell.Empty;

            int dest = 0; // lowest row a movable cell can settle into
            bool moved = false;

            for (int y = 0; y < height; y++)
            {
                var cell = board.Get(x, y);
                if (cell.IsEmpty)
                    continue;

                if (cell.IsFixed)
                {
                    // Anchored in place; nothing falls below it afterwards.
                    columnBuffer[y] = cell;
                    dest = y + 1;
                }
                else
                {
                    columnBuffer[dest] = cell;
                    if (dest != y)
                        moved = true;
                    dest++;
                }
            }

            if (moved)
            {
                for (int y = 0; y < height; y++)
                    board.Set(x, y, columnBuffer[y]);
            }

            return moved;
        }
    }
}
