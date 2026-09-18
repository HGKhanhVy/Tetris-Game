using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace BrickStacker.Offline.Tests
{
    // TacticalLevelData.Create also runs ObstacleSystem.Populate, so these cover
    // level generation and terrain placement together (design §2, §3).
    public class TacticalLevelDataTests
    {
        // Levels the game currently ships, plus headroom for the generated curve.
        const int LevelsToSweep = 20;

        // PathDistance returns 1000 + Manhattan when no route exists.
        const int UnreachablePathLength = 1000;

        static IEnumerable<int> Levels()
        {
            for (int level = 1; level <= LevelsToSweep; level++)
            {
                yield return level;
            }
        }

        [Test]
        public void Create_IsDeterministicForTheSameLevel()
        {
            foreach (int level in Levels())
            {
                var first = TacticalLevelData.Create(level);
                var second = TacticalLevelData.Create(level);

                Assert.AreEqual(first.PlayerStartPosition, second.PlayerStartPosition, $"level {level}");
                Assert.AreEqual(first.EnemyStartPosition, second.EnemyStartPosition, $"level {level}");
                Assert.AreEqual(first.MonsterStartPosition, second.MonsterStartPosition, $"level {level}");
                CollectionAssert.AreEqual(first.WallPositions, second.WallPositions, $"tuong level {level}");
                CollectionAssert.AreEqual(first.BoxPositions, second.BoxPositions, $"thung level {level}");
                CollectionAssert.AreEqual(first.TrapPositions, second.TrapPositions, $"bay level {level}");
                CollectionAssert.AreEqual(first.IcePositions, second.IcePositions, $"bang level {level}");
            }
        }

        [Test]
        public void Create_PutsEveryPieceOnItsOwnCellInsideTheBoard()
        {
            foreach (int level in Levels())
            {
                var data = TacticalLevelData.Create(level);

                AssertInsideBoard(data, data.PlayerStartPosition, "player", level);
                AssertInsideBoard(data, data.EnemyStartPosition, "enemy", level);
                AssertInsideBoard(data, data.MonsterStartPosition, "monster", level);

                Assert.AreNotEqual(data.PlayerStartPosition, data.EnemyStartPosition, $"level {level}");
                Assert.AreNotEqual(data.PlayerStartPosition, data.MonsterStartPosition, $"level {level}");
                Assert.AreNotEqual(data.EnemyStartPosition, data.MonsterStartPosition, $"level {level}");
            }
        }

        [Test]
        public void Create_NeverBuriesAStartCellUnderTerrain()
        {
            foreach (int level in Levels())
            {
                var data = TacticalLevelData.Create(level);

                foreach (Vector2Int start in new[]
                         {
                             data.PlayerStartPosition,
                             data.EnemyStartPosition,
                             data.MonsterStartPosition
                         })
                {
                    Assert.IsFalse(data.WallPositions.Contains(start), $"tuong de len o xuat phat, level {level}");
                    Assert.IsFalse(data.BoxPositions.Contains(start), $"thung de len o xuat phat, level {level}");
                }
            }
        }

        // The design forbids terrain that walls the three pieces off from each other:
        // if the monster cannot reach anyone, the level can never be won or lost.
        [Test]
        public void Create_LeavesEveryPieceAbleToReachTheOthers()
        {
            foreach (int level in Levels())
            {
                var board = new TacticalBoardManager(TacticalLevelData.Create(level));

                AssertReachable(board, board.PlayerPosition, board.EnemyPosition, "player -> enemy", level);
                AssertReachable(board, board.PlayerPosition, board.MonsterPosition, "player -> quai", level);
                AssertReachable(board, board.MonsterPosition, board.EnemyPosition, "quai -> enemy", level);
            }
        }

        [Test]
        public void Create_ClampsLevelIdToAtLeastOne()
        {
            Assert.AreEqual(1, TacticalLevelData.Create(0).LevelId);
            Assert.AreEqual(1, TacticalLevelData.Create(-5).LevelId);
        }

        [Test]
        public void Create_KeepsStarMoveLimitsOrdered()
        {
            foreach (int level in Levels())
            {
                var data = TacticalLevelData.Create(level);

                Assert.Less(data.ThreeStarMoveLimit, data.TwoStarMoveLimit,
                    $"3 sao phai kho hon 2 sao, level {level}");
                Assert.Greater(data.ThreeStarMoveLimit, 0, $"level {level}");
            }
        }

        static void AssertInsideBoard(TacticalLevelData data, Vector2Int cell, string who, int level)
        {
            Assert.IsTrue(
                cell.x >= 0 && cell.x < data.BoardWidth && cell.y >= 0 && cell.y < data.BoardHeight,
                $"{who} nam ngoai ban co o level {level}: {cell}");
        }

        static void AssertReachable(TacticalBoardManager board, Vector2Int from, Vector2Int to, string what, int level)
        {
            Assert.Less(board.PathLen(from, to), UnreachablePathLength,
                $"{what} bi chan hoan toan o level {level}");
        }
    }
}
