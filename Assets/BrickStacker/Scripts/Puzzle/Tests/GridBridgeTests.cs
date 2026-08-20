using NUnit.Framework;

namespace BrickStacker.Puzzle.Tests
{
    public class GridBridgeTests
    {
        [Test]
        public void RoundTrip_PreservesResourcesAndGarbage()
        {
            var grid = new int[4, 4];
            grid[0, 0] = GridBridge.Encode(ResourceType.Move);
            grid[1, 0] = GridBridge.Encode(ResourceType.Energy);
            grid[2, 0] = GridBridge.GarbageValue;

            var board = new PuzzleBoard(4, 4);
            GridBridge.Load(grid, board);

            Assert.AreEqual(ResourceType.Move, board.Get(0, 0).Resource);
            Assert.AreEqual(ResourceType.Energy, board.Get(1, 0).Resource);
            Assert.AreEqual(CellKind.Garbage, board.Get(2, 0).Kind);
            Assert.IsTrue(board.Get(3, 3).IsEmpty);

            var back = new int[4, 4];
            GridBridge.Store(board, back);

            Assert.AreEqual(grid[0, 0], back[0, 0]);
            Assert.AreEqual(grid[1, 0], back[1, 0]);
            Assert.AreEqual(GridBridge.GarbageValue, back[2, 0]);
            Assert.AreEqual(0, back[3, 3]);
        }

        [Test]
        public void ResolveThroughBridge_ClearsClusterInIntGrid()
        {
            var grid = new int[8, 8];
            for (int x = 0; x < 4; x++)
                grid[x, 0] = GridBridge.Encode(ResourceType.Attack);

            var board = new PuzzleBoard(8, 8);
            GridBridge.Load(grid, board);
            var outcome = new ClusterResolutionSystem(8, 8).Resolve(board);
            GridBridge.Store(board, grid);

            Assert.AreEqual(1, outcome.ChainCount);
            for (int x = 0; x < 4; x++)
                Assert.AreEqual(0, grid[x, 0]);
        }
    }
}
