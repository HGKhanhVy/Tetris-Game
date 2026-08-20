using NUnit.Framework;
using UnityEngine;

namespace BrickStacker.Puzzle.Tests
{
    public class ClusterResolutionSystemTests
    {
        static void Fill(PuzzleBoard board, ResourceType type, params Vector2Int[] cells)
        {
            foreach (var c in cells)
                board.Set(c, PuzzleCell.OfResource(type));
        }

        [Test]
        public void SingleCluster_IsRemovedInOneStep()
        {
            var board = new PuzzleBoard(8, 8);
            Fill(board, ResourceType.Move,
                new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0));

            var outcome = new ClusterResolutionSystem(board.Width, board.Height).Resolve(board);

            Assert.AreEqual(1, outcome.ChainCount);
            Assert.AreEqual(1.00f, outcome.Steps[0].Multiplier);
            for (int x = 0; x < 4; x++)
                Assert.IsTrue(board.Get(x, 0).IsEmpty);
        }

        [Test]
        public void NoCluster_ProducesEmptyOutcome()
        {
            var board = new PuzzleBoard(8, 8);
            Fill(board, ResourceType.Move, new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0));

            var outcome = new ClusterResolutionSystem(board.Width, board.Height).Resolve(board);

            Assert.IsFalse(outcome.ActivatedAnything);
        }

        [Test]
        public void AdjacentGarbage_IsDestroyedByBlast()
        {
            var board = new PuzzleBoard(8, 8);
            Fill(board, ResourceType.Move,
                new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(3, 1));
            board.Set(0, 0, PuzzleCell.Garbage()); // vertical neighbor of the cluster
            board.Set(1, 0, PuzzleCell.Garbage());

            var outcome = new ClusterResolutionSystem(board.Width, board.Height).Resolve(board);

            Assert.AreEqual(1, outcome.ChainCount);
            Assert.AreEqual(2, outcome.Steps[0].DestroyedObstacles.Count);
            Assert.IsTrue(board.Get(0, 0).IsEmpty);
            Assert.IsTrue(board.Get(1, 0).IsEmpty);
        }

        [Test]
        public void BreakableWithHealthTwo_SurvivesBasicBlast()
        {
            var board = new PuzzleBoard(8, 8);
            Fill(board, ResourceType.Move,
                new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(3, 1));
            board.Set(0, 0, PuzzleCell.BreakableObstacle(2)); // basic blast deals only 1

            var outcome = new ClusterResolutionSystem(board.Width, board.Height).Resolve(board);

            Assert.AreEqual(0, outcome.Steps[0].DestroyedObstacles.Count);
            Assert.AreEqual(CellKind.Breakable, board.Get(0, 0).Kind);
            Assert.AreEqual(1, board.Get(0, 0).Health);
        }

        [Test]
        public void ChainReaction_CountsMultipleSteps()
        {
            var board = new PuzzleBoard(8, 8);
            // Bottom row: 4 Attack → removed first.
            Fill(board, ResourceType.Attack,
                new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0));
            // Above: 3 Move in a row (not yet a cluster) plus a 4th floating with a gap, so they
            // only reach 4-connected after the bottom row clears and column 0 collapses.
            Fill(board, ResourceType.Move,
                new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(0, 3));

            var outcome = new ClusterResolutionSystem(board.Width, board.Height).Resolve(board);

            Assert.AreEqual(2, outcome.ChainCount);
            Assert.AreEqual(1.00f, outcome.Steps[0].Multiplier);
            Assert.AreEqual(1.25f, outcome.Steps[1].Multiplier);
            Assert.AreEqual(ResourceType.Attack, outcome.Steps[0].Clusters[0].Resource);
            Assert.AreEqual(ResourceType.Move, outcome.Steps[1].Clusters[0].Resource);
        }
    }
}
