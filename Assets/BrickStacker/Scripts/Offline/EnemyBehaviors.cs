using System.Collections.Generic;
using UnityEngine;

namespace BrickStacker
{
    // Design §2.8 — các loại hành vi Enemy.
    public enum TacticalEnemyType
    {
        Stationary, // Đứng yên — màn hướng dẫn, player tự dụ quái tới enemy.
        Shy,        // Nhút nhát — luôn tìm ô tăng khoảng cách với quái.
        Patrol,     // Tuần tra — đi theo tuyến định sẵn.
        Mimic,      // Bắt chước — đi cùng hướng với player nếu hợp lệ.
        Smart       // Thông minh — tránh cả quái lẫn player, ưu tiên xa quái nhất.
    }

    // Chọn ô Enemy sẽ đi tới trong 1 lượt player, theo loại Enemy của màn.
    // Tách riêng khỏi TacticalBoardManager để dễ thêm/kiểm thử hành vi (design §2.8).
    public static class EnemyBehaviors
    {
        public static Vector2Int ChooseMove(TacticalBoardManager b)
        {
            switch (b.EnemyType)
            {
                case TacticalEnemyType.Stationary: return b.EnemyPosition;
                case TacticalEnemyType.Patrol:     return Patrol(b);
                case TacticalEnemyType.Mimic:      return Mimic(b);
                case TacticalEnemyType.Smart:      return Smart(b);
                case TacticalEnemyType.Shy:
                default:                           return Shy(b);
            }
        }

        // Nhút nhát: tối đa hoá khoảng cách đường-đi tới quái; hòa thì né xa player.
        static Vector2Int Shy(TacticalBoardManager b)
        {
            Vector2Int best = b.EnemyPosition;
            int bestDistance = b.PathLen(b.EnemyPosition, b.MonsterPosition);
            foreach (var dir in TacticalBoardManager.Directions)
            {
                var candidate = b.EnemyPosition + dir;
                if (!b.EnemyCanEnter(candidate))
                    continue;

                int distance = b.PathLen(candidate, b.MonsterPosition);
                if (distance > bestDistance ||
                    (distance == bestDistance && best != b.EnemyPosition &&
                     b.ManhattanTo(candidate, b.PlayerPosition) > b.ManhattanTo(best, b.PlayerPosition)))
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }
            return best;
        }

        // Tuần tra: tiến tới waypoint kế tiếp trong tuyến; tới nơi thì sang waypoint sau
        // (đảo chiều ở hai đầu tuyến). Nếu bị chặn thì đứng yên lượt này.
        static Vector2Int Patrol(TacticalBoardManager b)
        {
            var route = b.Data != null ? b.Data.EnemyPatrol : null;
            if (route == null || route.Count == 0)
                return b.EnemyPosition;
            if (route.Count == 1)
                return b.EnemyPosition;

            int index = Mathf.Clamp(b.PatrolIndex, 0, route.Count - 1);
            int dir = b.PatrolDirection == 0 ? 1 : b.PatrolDirection;

            // Đã đứng ngay waypoint hiện tại → nhắm waypoint kế tiếp.
            if (b.EnemyPosition == route[index])
            {
                int nextIndex = index + dir;
                if (nextIndex >= route.Count) { nextIndex = route.Count - 2; dir = -1; }
                else if (nextIndex < 0) { nextIndex = 1; dir = 1; }
                index = Mathf.Clamp(nextIndex, 0, route.Count - 1);
            }

            Vector2Int goal = route[index];
            Vector2Int step = StepToward(b, b.EnemyPosition, goal);
            b.AdvancePatrol(index, dir);
            return step;
        }

        // Bắt chước: đi cùng hướng player vừa đi nếu ô đó hợp lệ; không thì đứng yên.
        static Vector2Int Mimic(TacticalBoardManager b)
        {
            var dir = b.LastPlayerMove;
            if (dir == Vector2Int.zero)
                return b.EnemyPosition;
            var candidate = b.EnemyPosition + dir;
            return b.EnemyCanEnter(candidate) ? candidate : b.EnemyPosition;
        }

        // Thông minh: né cả quái lẫn player. Ưu tiên ô xa quái nhất (đường đi),
        // phạt nặng ô sát player để không tự nộp mạng cho hướng player dụ quái.
        static Vector2Int Smart(TacticalBoardManager b)
        {
            Vector2Int best = b.EnemyPosition;
            float bestScore = Score(b, b.EnemyPosition);
            foreach (var dir in TacticalBoardManager.Directions)
            {
                var candidate = b.EnemyPosition + dir;
                if (!b.EnemyCanEnter(candidate))
                    continue;
                float score = Score(b, candidate);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }
            return best;
        }

        static float Score(TacticalBoardManager b, Vector2Int cell)
        {
            int distMonster = b.PathLen(cell, b.MonsterPosition);
            int distPlayer = b.PathLen(cell, b.PlayerPosition);
            // Càng xa quái càng tốt; quá sát player (≤1) bị phạt để tránh bị dụ.
            float score = distMonster;
            if (distPlayer <= 1)
                score -= 3f;
            return score;
        }

        // Một bước BFS từ start tới goal cho Enemy (tránh quái/player).
        static Vector2Int StepToward(TacticalBoardManager b, Vector2Int start, Vector2Int goal)
        {
            if (start == goal)
                return start;

            var queue = new Queue<Vector2Int>();
            var prev = new Dictionary<Vector2Int, Vector2Int>();
            queue.Enqueue(start);
            prev[start] = start;

            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var dir in TacticalBoardManager.Directions)
                {
                    var next = cell + dir;
                    if (prev.ContainsKey(next))
                        continue;
                    // Cho phép bước cuối chính là goal dù goal có thể là ô đặc biệt.
                    bool passable = next == goal ? b.CellOpen(next) : b.EnemyCanEnter(next);
                    if (!passable)
                        continue;
                    prev[next] = cell;
                    if (next == goal)
                    {
                        var s = next;
                        while (prev[s] != start)
                            s = prev[s];
                        return s;
                    }
                    queue.Enqueue(next);
                }
            }
            return start; // kẹt — đứng yên
        }
    }
}
