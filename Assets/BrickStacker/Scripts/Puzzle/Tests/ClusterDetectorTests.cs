using NUnit.Framework;
using UnityEngine;

namespace BrickStacker.Puzzle.Tests
{
    public class ClusterDetectorTests
    {
        static PuzzleBoard Board(int w = 8, int h = 8) => new PuzzleBoard(w, h);

        static void Fill(PuzzleBoard board, ResourceType type, params Vector2Int[] cells)
        {
            foreach (var c in cells)
                board.Set(c, PuzzleCell.OfResource(type));
        }

        [Test]
        public void FourInARow_IsBasicCluster()
        {
            var board = Board();
            Fill(board, ResourceType.Move,
                new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0));

            var clusters = new ClusterDetector(board.Width, board.Height).DetectAll(board);

            Assert.AreEqual(1, clusters.Count);
            Assert.AreEqual(ClusterTier.Basic, clusters[0].Tier);
            Assert.AreEqual(4, clusters[0].Size);
            Assert.AreEqual(ResourceType.Move, clusters[0].Resource);
        }

        [Test]
        public void SixConnected_IsStrongCluster()
        {
            var board = Board();
            Fill(board, ResourceType.Attack,
                new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0),
                new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1));

            var clusters = new ClusterDetector(board.Width, board.Height).DetectAll(board);

            Assert.AreEqual(1, clusters.Count);
            Assert.AreEqual(ClusterTier.Strong, clusters[0].Tier);
            Assert.AreEqual(6, clusters[0].Size);
        }

        [Test]
        public void ThreeConnected_IsNotACluster()
        {
            var board = Board();
            Fill(board, ResourceType.Shield,
                new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0));

            var clusters = new ClusterDetector(board.Width, board.Height).DetectAll(board);

            Assert.AreEqual(0, clusters.Count);
        }

        [Test]
        public void DifferentResources_DoNotMerge()
        {
            var board = Board();
            Fill(board, ResourceType.Move, new Vector2Int(0, 0), new Vector2Int(1, 0));
            Fill(board, ResourceType.Attack, new Vector2Int(2, 0), new Vector2Int(3, 0));

            var clusters = new ClusterDetector(board.Width, board.Height).DetectAll(board);

            Assert.AreEqual(0, clusters.Count); // 2 + 2, neither reaches threshold
        }

        [Test]
        public void HardCell_BreaksAdjacency()
        {
            var board = Board();
            // Move | Move | [HARD] | Move | Move  → two groups of 2, no valid cluster
            Fill(board, ResourceType.Move, new Vector2Int(0, 0), new Vector2Int(1, 0));
            board.Set(new Vector2Int(2, 0), PuzzleCell.Hard());
            Fill(board, ResourceType.Move, new Vector2Int(3, 0), new Vector2Int(4, 0));

            var clusters = new ClusterDetector(board.Width, board.Height).DetectAll(board);

            Assert.AreEqual(0, clusters.Count);
        }

        [Test]
        public void Diagonal_DoesNotConnect()
        {
            var board = Board();
            Fill(board, ResourceType.Move,
                new Vector2Int(0, 0), new Vector2Int(1, 1), new Vector2Int(2, 2), new Vector2Int(3, 3));

            var clusters = new ClusterDetector(board.Width, board.Height).DetectAll(board);

            Assert.AreEqual(0, clusters.Count);
        }
    }
}
