using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrickStacker
{
    // Cặp cổng dịch chuyển liên kết (design §3.5).
    [Serializable]
    public class PortalPair
    {
        public Vector2Int A;
        public Vector2Int B;
        public PortalPair() { }
        public PortalPair(Vector2Int a, Vector2Int b) { A = a; B = b; }
    }

    // Công tắc mở/đóng một cửa (design §3.6). Cửa khởi đầu đóng (chặn đường).
    [Serializable]
    public class SwitchDoor
    {
        public Vector2Int Switch;
        public Vector2Int Door;
        public SwitchDoor() { }
        public SwitchDoor(Vector2Int sw, Vector2Int door) { Switch = sw; Door = door; }
    }

    // Đặt chướng ngại/địa hình cho từng màn (design §3). Đặt tất định theo seed level,
    // không cắt bàn cờ (thùng/cửa đóng phải để player-enemy-quái vẫn tới được nhau).
    public static class ObstacleSystem
    {
        public const int WoodenBoxHealth = 1;   // §3.2
        public const int TrapStunTurns = 1;     // §3.3

        public static void Populate(TacticalLevelData data, int level)
        {
            data.WoodenBoxHealth = WoodenBoxHealth;
            data.TrapStunTurns = TrapStunTurns;

            // Địa hình mở dần theo tiến trình để người chơi làm quen từng cơ chế.
            var rng = new System.Random(level * 2237 + 91);
            var occupied = BuildOccupiedSet(data);

            if (level >= 2)
                AddCells(data.TrapPositions, data, occupied, rng, Mathf.Min(1 + level / 6, 3));
            if (level >= 3)
                AddCells(data.BoxPositions, data, occupied, rng, Mathf.Min(1 + level / 5, 4));
            if (level >= 5)
                AddCells(data.IcePositions, data, occupied, rng, Mathf.Min(2 + level / 4, 6));
            if (level >= 7)
                AddPortalPair(data, occupied, rng);
            if (level >= 9)
                AddSwitchDoor(data, occupied, rng);
        }

        static HashSet<Vector2Int> BuildOccupiedSet(TacticalLevelData data)
        {
            var set = new HashSet<Vector2Int>
            {
                data.PlayerStartPosition, data.EnemyStartPosition, data.MonsterStartPosition
            };
            foreach (var w in data.WallPositions) set.Add(w);
            foreach (var b in data.BoxPositions) set.Add(b);
            foreach (var t in data.TrapPositions) set.Add(t);
            foreach (var i in data.IcePositions) set.Add(i);
            foreach (var p in data.Portals) { set.Add(p.A); set.Add(p.B); }
            foreach (var s in data.SwitchDoors) { set.Add(s.Switch); set.Add(s.Door); }
            return set;
        }

        // Thêm N ô ngẫu nhiên tất định, không đè ô đã dùng, cách ô xuất phát ≥1.
        static void AddCells(List<Vector2Int> target, TacticalLevelData data, HashSet<Vector2Int> occupied, System.Random rng, int count)
        {
            int placed = 0, attempts = 0;
            while (placed < count && attempts < 120)
            {
                attempts++;
                var c = new Vector2Int(rng.Next(0, data.BoardWidth), rng.Next(0, data.BoardHeight));
                if (occupied.Contains(c))
                    continue;
                if (NearStart(c, data))
                    continue;
                target.Add(c);
                occupied.Add(c);
                placed++;
            }
        }

        static void AddPortalPair(TacticalLevelData data, HashSet<Vector2Int> occupied, System.Random rng)
        {
            Vector2Int a = FindFreeCell(data, occupied, rng);
            if (a.x < 0) return;
            occupied.Add(a);
            Vector2Int b = FindFreeCell(data, occupied, rng);
            if (b.x < 0) return;
            occupied.Add(b);
            data.Portals.Add(new PortalPair(a, b));
        }

        static void AddSwitchDoor(TacticalLevelData data, HashSet<Vector2Int> occupied, System.Random rng)
        {
            Vector2Int door = FindFreeCell(data, occupied, rng);
            if (door.x < 0) return;
            occupied.Add(door);
            Vector2Int sw = FindFreeCell(data, occupied, rng);
            if (sw.x < 0) return;
            occupied.Add(sw);
            data.SwitchDoors.Add(new SwitchDoor(sw, door));
        }

        static Vector2Int FindFreeCell(TacticalLevelData data, HashSet<Vector2Int> occupied, System.Random rng)
        {
            for (int attempt = 0; attempt < 120; attempt++)
            {
                var c = new Vector2Int(rng.Next(0, data.BoardWidth), rng.Next(0, data.BoardHeight));
                if (!occupied.Contains(c) && !NearStart(c, data))
                    return c;
            }
            return new Vector2Int(-1, -1);
        }

        static bool NearStart(Vector2Int c, TacticalLevelData data)
        {
            return Near(c, data.PlayerStartPosition) || Near(c, data.EnemyStartPosition) || Near(c, data.MonsterStartPosition);
        }

        static bool Near(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) <= 1;
        }
    }
}
