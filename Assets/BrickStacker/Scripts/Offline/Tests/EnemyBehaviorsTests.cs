using NUnit.Framework;
using UnityEngine;

namespace BrickStacker.Offline.Tests
{
    // Design §2.8 — mỗi loại Enemy chọn ô đi theo một quy tắc riêng.
    public class EnemyBehaviorsTests
    {
        static TacticalBoardManager BoardWith(TacticalEnemyType type, int level = 1)
        {
            var data = TacticalLevelData.Create(level);
            data.EnemyType = type;
            return new TacticalBoardManager(data);
        }

        [Test]
        public void Stationary_StaysPut()
        {
            var board = BoardWith(TacticalEnemyType.Stationary);

            Assert.AreEqual(board.EnemyPosition, EnemyBehaviors.ChooseMove(board));
        }

        [Test]
        public void Mimic_StaysPutBeforeThePlayerHasMoved()
        {
            var board = BoardWith(TacticalEnemyType.Mimic);
            Assert.AreEqual(Vector2Int.zero, board.LastPlayerMove, "Test setup expects a fresh board.");

            Assert.AreEqual(board.EnemyPosition, EnemyBehaviors.ChooseMove(board));
        }

        [Test]
        public void Shy_NeverStepsCloserToTheMonster()
        {
            for (int level = 1; level <= 10; level++)
            {
                var board = BoardWith(TacticalEnemyType.Shy, level);
                int before = board.PathLen(board.EnemyPosition, board.MonsterPosition);

                Vector2Int chosen = EnemyBehaviors.ChooseMove(board);

                Assert.GreaterOrEqual(board.PathLen(chosen, board.MonsterPosition), before,
                    $"enemy nhut nhat di vao gan quai hon o level {level}");
            }
        }

        [Test]
        public void EveryBehavior_PicksACellTheEnemyMayActuallyStandOn()
        {
            foreach (TacticalEnemyType type in System.Enum.GetValues(typeof(TacticalEnemyType)))
            {
                for (int level = 1; level <= 10; level++)
                {
                    var board = BoardWith(type, level);
                    Vector2Int chosen = EnemyBehaviors.ChooseMove(board);

                    bool staysPut = chosen == board.EnemyPosition;
                    Assert.IsTrue(staysPut || board.EnemyCanEnter(chosen),
                        $"{type} chon o khong di duoc o level {level}: {chosen}");
                }
            }
        }

        [Test]
        public void EveryBehavior_MovesAtMostOneCellPerTurn()
        {
            foreach (TacticalEnemyType type in System.Enum.GetValues(typeof(TacticalEnemyType)))
            {
                for (int level = 1; level <= 10; level++)
                {
                    var board = BoardWith(type, level);
                    Vector2Int chosen = EnemyBehaviors.ChooseMove(board);
                    Vector2Int delta = chosen - board.EnemyPosition;

                    Assert.LessOrEqual(Mathf.Abs(delta.x) + Mathf.Abs(delta.y), 1,
                        $"{type} nhay qua 1 o o level {level}");
                }
            }
        }
    }
}
