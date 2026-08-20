using NUnit.Framework;
using UnityEngine;

namespace BrickStacker.Puzzle.Tests
{
    public class GravityResolverTests
    {
        [Test]
        public void FloatingCell_FallsToBottom()
        {
            var board = new PuzzleBoard(4, 8);
            board.Set(0, 5, PuzzleCell.OfResource(ResourceType.Move));

            bool moved = new GravityResolver(board.Height).Apply(board);

            Assert.IsTrue(moved);
            Assert.IsTrue(board.Get(0, 0).IsResource);
            Assert.IsTrue(board.Get(0, 5).IsEmpty);
        }

        [Test]
        public void CellsStackWithoutGaps()
        {
            var board = new PuzzleBoard(4, 8);
            board.Set(0, 1, PuzzleCell.OfResource(ResourceType.Move));
            board.Set(0, 4, PuzzleCell.OfResource(ResourceType.Attack));
            board.Set(0, 6, PuzzleCell.OfResource(ResourceType.Shield));

            new GravityResolver(board.Height).Apply(board);

            Assert.AreEqual(ResourceType.Move, board.Get(0, 0).Resource);
            Assert.AreEqual(ResourceType.Attack, board.Get(0, 1).Resource);
            Assert.AreEqual(ResourceType.Shield, board.Get(0, 2).Resource);
            Assert.IsTrue(board.Get(0, 3).IsEmpty);
        }

        [Test]
        public void MovableCell_RestsOnFixedAnchor()
        {
            var board = new PuzzleBoard(4, 8);
            board.Set(0, 2, PuzzleCell.Hard());
            board.Set(0, 5, PuzzleCell.OfResource(ResourceType.Move));

            new GravityResolver(board.Height).Apply(board);

            Assert.AreEqual(CellKind.LockedHard, board.Get(0, 2).Kind); // anchor stays
            Assert.IsTrue(board.Get(0, 3).IsResource);                  // rests on top of it
        }

        [Test]
        public void FixedCell_DoesNotFall()
        {
            var board = new PuzzleBoard(4, 8);
            board.Set(0, 1, PuzzleCell.OfResource(ResourceType.Move)); // below the anchor
            board.Set(0, 4, PuzzleCell.Hard());

            new GravityResolver(board.Height).Apply(board);

            Assert.IsTrue(board.Get(0, 0).IsResource);                  // movable dropped to floor
            Assert.AreEqual(CellKind.LockedHard, board.Get(0, 4).Kind); // anchor did not move
        }

        [Test]
        public void CompactColumn_ReturnsFalseWhenNothingMoves()
        {
            var board = new PuzzleBoard(4, 8);
            board.Set(0, 0, PuzzleCell.OfResource(ResourceType.Move));
            board.Set(0, 1, PuzzleCell.OfResource(ResourceType.Move));

            bool moved = new GravityResolver(board.Height).Apply(board);

            Assert.IsFalse(moved);
        }
    }
}
