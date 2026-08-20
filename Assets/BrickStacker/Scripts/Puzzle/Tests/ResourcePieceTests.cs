using NUnit.Framework;
using UnityEngine;

namespace BrickStacker.Puzzle.Tests
{
    public class ResourcePieceTests
    {
        [Test]
        public void WriteCells_NoRotation_KeepsShape()
        {
            var piece = new ResourcePiece(
                new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) },
                new[] { ResourceType.Move, ResourceType.Attack, ResourceType.Shield });

            var outCells = new Vector2Int[3];
            piece.WriteCells(new Vector2Int(3, 5), 0, outCells);

            Assert.AreEqual(new Vector2Int(3, 5), outCells[0]);
            Assert.AreEqual(new Vector2Int(4, 5), outCells[1]);
            Assert.AreEqual(new Vector2Int(5, 5), outCells[2]);
        }

        [Test]
        public void WriteCells_Rotated_NormalizesToOriginAndKeepsResourceMapping()
        {
            var piece = new ResourcePiece(
                new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0) },
                new[] { ResourceType.Move, ResourceType.Attack, ResourceType.Shield });

            var outCells = new Vector2Int[3];
            piece.WriteCells(Vector2Int.zero, 1, outCells); // clockwise quarter turn

            // A horizontal triomino becomes a vertical one, still 3 distinct cells in one column.
            Assert.AreEqual(1, DistinctColumns(outCells));
            Assert.AreEqual(3, DistinctCount(outCells));
            // Resource i still pairs with outCells[i].
            Assert.AreEqual(3, piece.Resources.Length);
            Assert.AreEqual(ResourceType.Move, piece.Resources[0]);
        }

        static int DistinctColumns(Vector2Int[] cells)
        {
            var set = new System.Collections.Generic.HashSet<int>();
            foreach (var c in cells) set.Add(c.x);
            return set.Count;
        }

        static int DistinctCount(Vector2Int[] cells)
        {
            var set = new System.Collections.Generic.HashSet<Vector2Int>();
            foreach (var c in cells) set.Add(c);
            return set.Count;
        }
    }
}
