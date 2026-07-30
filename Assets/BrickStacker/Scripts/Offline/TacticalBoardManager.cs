using System;
using System.Collections.Generic;
using UnityEngine;

namespace BrickStacker
{
    public enum TacticalBoardStatus
    {
        Running,
        Won,
        Failed
    }

    // Quái ưu tiên mục tiêu nào — dùng cho luật đổi mục tiêu theo ngưỡng (design §2.6)
    // và cho HUD hiển thị ý định (design §2.7).
    public enum TacticalTarget
    {
        Player,
        Enemy
    }

    public abstract class TacticalPiece
    {
        public Vector2Int Position;

        protected TacticalPiece(Vector2Int position)
        {
            Position = position;
        }
    }

    public class PlayerPiece : TacticalPiece
    {
        public PlayerPiece(Vector2Int position) : base(position) { }
    }

    public class EnemyPiece : TacticalPiece
    {
        public EnemyPiece(Vector2Int position) : base(position) { }
    }

    public class MonsterPiece : TacticalPiece
    {
        public MonsterPiece(Vector2Int position) : base(position) { }
    }

    [Serializable]
    public class TacticalLevelData
    {
        public int LevelId;
        public int BoardWidth;
        public int BoardHeight;
        public Vector2Int PlayerStartPosition;
        public Vector2Int EnemyStartPosition;
        public Vector2Int MonsterStartPosition;
        public List<Vector2Int> WallPositions = new List<Vector2Int>();
        public int ThreeStarMoveLimit;
        public int TwoStarMoveLimit;
        public float InitialFallSpeed;
        public int LineToMoveRate = 1;
        public int CoinReward;
        public bool UnlockNextLevel = true;
        public int MonsterStepsPerTurn = 1;

        // === Thông số luồng mới (design §2.3, §2.5, §2.6, §4) ===
        // Quái tự đi sau mỗi khoảng thời gian này (giây). Giảm dần theo độ khó.
        public float MonsterAutoMoveInterval = 6.0f;
        // Timer tự động reset khi quái di chuyển vì player hành động (design §2.5).
        public bool ResetMonsterTimerAfterPlayerAction = true;
        // Trần điểm di chuyển tích luỹ (design §2.3).
        public int MaxMovementPoint = 5;
        // Ngưỡng để quái đổi từ đuổi player sang enemy (design §2.6).
        public int TargetSwitchThreshold = 2;

        // Loại Enemy của màn (design §2.8). Mặc định "nhút nhát" = hành vi cũ.
        public TacticalEnemyType EnemyType = TacticalEnemyType.Shy;
        // Tuyến tuần tra cho Enemy loại Patrol (danh sách waypoint theo thứ tự).
        public List<Vector2Int> EnemyPatrol = new List<Vector2Int>();

        // === Địa hình / chướng ngại (design §3) ===
        public List<Vector2Int> BoxPositions = new List<Vector2Int>();   // §3.2 thùng gỗ
        public List<Vector2Int> TrapPositions = new List<Vector2Int>();  // §3.3 bẫy
        public List<Vector2Int> IcePositions = new List<Vector2Int>();   // §3.4 ô băng
        public List<PortalPair> Portals = new List<PortalPair>();        // §3.5 cổng
        public List<SwitchDoor> SwitchDoors = new List<SwitchDoor>();    // §3.6 công tắc/cửa
        public int WoodenBoxHealth = 1;
        public int TrapStunTurns = 1;

        // === Sao theo thời gian (design §5) ===
        public float ThreeStarTime = 100f;
        public float TwoStarTime = 150f;

        public static TacticalLevelData Create(int level)
        {
            const int width = 8;
            const int height = 8;
            var data = new TacticalLevelData
            {
                LevelId = Mathf.Max(1, level),
                BoardWidth = width,
                BoardHeight = height,
                // Cân bằng nguy hiểm: quái mở màn săn ENEMY (gần hơn player 1-2 ô),
                // nhưng player đứng sát đường đuổi — đi ẩu là thành mục tiêu gần hơn
                // và bị quay xe săn ngay.
                PlayerStartPosition = new Vector2Int(6, 1),
                EnemyStartPosition = new Vector2Int(1, 5),
                MonsterStartPosition = new Vector2Int(4, 4),
                ThreeStarMoveLimit = Mathf.Max(8, 10 + level / 3),
                TwoStarMoveLimit = Mathf.Max(14, 16 + level / 2),
                InitialFallSpeed = Mathf.Max(0.34f, 0.82f - Mathf.Min(level, 30) * 0.010f),
                LineToMoveRate = 1,
                CoinReward = 45 + level * 5,
                UnlockNextLevel = true
            };

            // Mỗi pattern: dist(quái→enemy) 4 ≤ dist(quái→player) ≤ dist(quái→enemy)+2.
            // Quái mở màn săn enemy; player đứng gần đường đuổi nên vẫn phải dè chừng.
            int pattern = (level - 1) % 6;
            if (pattern == 1)
            {
                data.PlayerStartPosition = new Vector2Int(4, 2);
                data.EnemyStartPosition = new Vector2Int(5, 6);
                data.MonsterStartPosition = new Vector2Int(2, 5);
                data.WallPositions.Add(new Vector2Int(3, 4));
                data.WallPositions.Add(new Vector2Int(1, 2));
                data.WallPositions.Add(new Vector2Int(5, 3));
            }
            else if (pattern == 2)
            {
                data.PlayerStartPosition = new Vector2Int(6, 6);
                data.EnemyStartPosition = new Vector2Int(2, 1);
                data.MonsterStartPosition = new Vector2Int(5, 2);
                data.WallPositions.Add(new Vector2Int(4, 4));
                data.WallPositions.Add(new Vector2Int(3, 0));
                data.WallPositions.Add(new Vector2Int(6, 3));
            }
            else if (pattern == 3)
            {
                data.PlayerStartPosition = new Vector2Int(3, 1);
                data.EnemyStartPosition = new Vector2Int(3, 5);
                data.MonsterStartPosition = new Vector2Int(1, 3);
                data.WallPositions.Add(new Vector2Int(2, 5));
                data.WallPositions.Add(new Vector2Int(0, 6));
                data.WallPositions.Add(new Vector2Int(4, 3));
                data.WallPositions.Add(new Vector2Int(5, 5));
            }
            else if (pattern == 4)
            {
                data.PlayerStartPosition = new Vector2Int(4, 4);
                data.EnemyStartPosition = new Vector2Int(3, 5);
                data.MonsterStartPosition = new Vector2Int(6, 6);
                data.WallPositions.Add(new Vector2Int(5, 4));
                data.WallPositions.Add(new Vector2Int(2, 6));
                data.WallPositions.Add(new Vector2Int(6, 2));
            }
            else if (pattern == 5)
            {
                data.PlayerStartPosition = new Vector2Int(6, 2);
                data.EnemyStartPosition = new Vector2Int(1, 2);
                data.MonsterStartPosition = new Vector2Int(3, 0);
                data.WallPositions.Add(new Vector2Int(2, 2));
                data.WallPositions.Add(new Vector2Int(4, 3));
                data.WallPositions.Add(new Vector2Int(1, 1));
            }
            else
            {
                data.WallPositions.Add(new Vector2Int(3, 3));
                data.WallPositions.Add(new Vector2Int(5, 5));
                data.WallPositions.Add(new Vector2Int(2, 2));
                data.WallPositions.Add(new Vector2Int(6, 4));
            }

            // Design §2.5: quái đi theo TIMER thực (không còn số-bước-mỗi-lượt là cơ chế chính).
            // Mỗi trigger (timer hoặc player hành động) quái đi 1 bước.
            data.MonsterStepsPerTurn = 1;

            // Design §4: độ khó tăng bằng cách rút ngắn interval + giảm trần điểm.
            //   Dễ 8s · Thường 6s · Khó 4.5s · Rất khó 3.5s.
            data.MonsterAutoMoveInterval = Mathf.Max(3.5f, 7.5f - Mathf.Min(level, 30) * 0.16f);
            data.MaxMovementPoint = level >= 18 ? 3 : (level >= 10 ? 4 : 5);
            data.TargetSwitchThreshold = 2;

            // Design §2.8: mỗi màn một loại Enemy để đa dạng chiến thuật.
            // Màn đầu dễ (đứng yên/nhút nhát), về sau xuất hiện loại khó hơn.
            data.EnemyType = PickEnemyType(level);
            if (data.EnemyType == TacticalEnemyType.Patrol)
                data.BuildDefaultPatrolRoute();

            // Design §5: mốc thời gian cho sao (khó hơn thì siết chặt hơn một chút).
            data.ThreeStarTime = Mathf.Max(45f, 105f - level * 1.5f);
            data.TwoStarTime = Mathf.Max(80f, 160f - level * 1.5f);

            // Drop pattern walls that collide with start positions first — a wall on a
            // start cell makes the connectivity check in AddProgressiveWalls always fail.
            data.RemoveInvalidWalls();
            data.AddProgressiveWalls(level);

            // Design §3: đặt địa hình sau khi đã cố định tường + vị trí xuất phát.
            ObstacleSystem.Populate(data, level);
            return data;
        }

        // Adds extra obstacles as levels progress. Placement is deterministic per
        // level (seeded) and never allowed to cut the board apart.
        void AddProgressiveWalls(int level)
        {
            int extra = Mathf.Min(1 + (level - 1) / 3, 8);
            if (extra <= 0)
                return;

            var rng = new System.Random(level * 7919 + 17);
            int placed = 0;
            int attempts = 0;
            while (placed < extra && attempts < 200)
            {
                attempts++;
                var cell = new Vector2Int(rng.Next(0, BoardWidth), rng.Next(0, BoardHeight));
                if (WallPositions.Contains(cell))
                    continue;
                if (NearStart(cell, PlayerStartPosition) || NearStart(cell, EnemyStartPosition) || NearStart(cell, MonsterStartPosition))
                    continue;

                WallPositions.Add(cell);
                if (BoardIsConnected())
                    placed++;
                else
                    WallPositions.RemoveAt(WallPositions.Count - 1);
            }
        }

        bool NearStart(Vector2Int cell, Vector2Int start)
        {
            return Mathf.Abs(cell.x - start.x) + Mathf.Abs(cell.y - start.y) <= 1;
        }

        // BFS from the player start: player, enemy and monster must share one open region.
        bool BoardIsConnected()
        {
            var wallSet = new HashSet<Vector2Int>(WallPositions);
            var visited = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(PlayerStartPosition);
            visited.Add(PlayerStartPosition);
            Vector2Int[] dirs = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var dir in dirs)
                {
                    var next = current + dir;
                    if (next.x < 0 || next.x >= BoardWidth || next.y < 0 || next.y >= BoardHeight)
                        continue;
                    if (wallSet.Contains(next) || !visited.Add(next))
                        continue;
                    queue.Enqueue(next);
                }
            }
            return visited.Contains(EnemyStartPosition) && visited.Contains(MonsterStartPosition);
        }

        // Chọn loại Enemy theo tiến trình màn (design §2.8).
        static TacticalEnemyType PickEnemyType(int level)
        {
            if (level <= 1) return TacticalEnemyType.Stationary; // màn hướng dẫn: tự dụ quái
            switch ((level - 2) % 5)
            {
                case 0: return TacticalEnemyType.Shy;
                case 1: return TacticalEnemyType.Patrol;
                case 2: return TacticalEnemyType.Mimic;
                case 3: return level >= 8 ? TacticalEnemyType.Smart : TacticalEnemyType.Shy;
                default: return TacticalEnemyType.Shy;
            }
        }

        // Tuyến tuần tra mặc định: một hình chữ nhật quanh vị trí xuất phát Enemy,
        // bỏ waypoint rơi vào tường để không kẹt.
        public void BuildDefaultPatrolRoute()
        {
            EnemyPatrol.Clear();
            var wallSet = new HashSet<Vector2Int>(WallPositions);
            var candidates = new List<Vector2Int>
            {
                EnemyStartPosition,
                EnemyStartPosition + new Vector2Int(1, 0),
                EnemyStartPosition + new Vector2Int(1, 1),
                EnemyStartPosition + new Vector2Int(0, 1),
            };
            for (int i = 0; i < candidates.Count; i++)
            {
                var c = candidates[i];
                if (c.x >= 0 && c.x < BoardWidth && c.y >= 0 && c.y < BoardHeight && !wallSet.Contains(c))
                    EnemyPatrol.Add(c);
            }
            if (EnemyPatrol.Count < 2)
            {
                EnemyPatrol.Clear();
                EnemyPatrol.Add(EnemyStartPosition);
            }
        }

        void RemoveInvalidWalls()
        {
            for (int i = WallPositions.Count - 1; i >= 0; i--)
            {
                var wall = WallPositions[i];
                bool invalid = wall.x < 0 || wall.x >= BoardWidth || wall.y < 0 || wall.y >= BoardHeight;
                invalid |= wall == PlayerStartPosition || wall == EnemyStartPosition || wall == MonsterStartPosition;
                if (invalid)
                    WallPositions.RemoveAt(i);
            }
        }
    }

    public class TacticalBoardManager
    {
        public TacticalLevelData Data { get; private set; }
        public Vector2Int PlayerPosition { get; private set; }
        public Vector2Int EnemyPosition { get; private set; }
        public Vector2Int MonsterPosition { get; private set; }
        public int MoveBank { get; private set; }
        public int MovesUsed { get; private set; }
        public TacticalBoardStatus Status { get; private set; }
        public string LastMessage { get; set; }

        // === Trạng thái timer quái (design §2.5, §2.7) ===
        // Đếm ngược tới lần tự đi kế tiếp. HUD dùng để vẽ đồng hồ + cảnh báo.
        public float MonsterTimer { get; private set; }
        public float MonsterAutoMoveInterval => Data != null ? Data.MonsterAutoMoveInterval : 6f;
        // 0 = vừa reset, 1 = sắp đi. Dùng cho thanh cảnh báo.
        public float MonsterMoveProgress => MonsterAutoMoveInterval > 0.01f ? Mathf.Clamp01(1f - MonsterTimer / MonsterAutoMoveInterval) : 0f;
        // Mục tiêu quái đang đuổi (design §2.7) — HUD hiển thị.
        public TacticalTarget CurrentTarget { get; private set; } = TacticalTarget.Player;
        // Ô kế tiếp quái sẽ bước tới. HUD nhấp nháy ô này.
        public Vector2Int NextMonsterStep { get; private set; }

        public TacticalEnemyType EnemyType { get; private set; } = TacticalEnemyType.Shy;
        // Hướng player vừa đi (cho Enemy loại Mimic).
        public Vector2Int LastPlayerMove { get; private set; }
        // Vị trí trong tuyến tuần tra + chiều đi (cho Enemy loại Patrol).
        public int PatrolIndex { get; private set; }
        public int PatrolDirection { get; private set; } = 1;

        readonly HashSet<Vector2Int> walls = new HashSet<Vector2Int>();

        // === Trạng thái địa hình runtime (design §3) ===
        readonly Dictionary<Vector2Int, int> boxHp = new Dictionary<Vector2Int, int>();
        readonly HashSet<Vector2Int> traps = new HashSet<Vector2Int>();
        readonly HashSet<Vector2Int> ice = new HashSet<Vector2Int>();
        readonly Dictionary<Vector2Int, Vector2Int> portals = new Dictionary<Vector2Int, Vector2Int>();
        readonly Dictionary<Vector2Int, Vector2Int> switchToDoor = new Dictionary<Vector2Int, Vector2Int>();
        readonly HashSet<Vector2Int> closedDoors = new HashSet<Vector2Int>();
        int monsterStunTurns;

        // HUD đọc để vẽ ô đặc biệt.
        public bool IsBox(Vector2Int c) => boxHp.ContainsKey(c);
        public bool IsTrap(Vector2Int c) => traps.Contains(c);
        public bool IsIce(Vector2Int c) => ice.Contains(c);
        public bool IsPortal(Vector2Int c) => portals.ContainsKey(c);
        public bool IsSwitch(Vector2Int c) => switchToDoor.ContainsKey(c);
        public bool IsClosedDoor(Vector2Int c) => closedDoors.Contains(c);
        public bool IsMonsterStunned => monsterStunTurns > 0;

        internal static readonly Vector2Int[] Directions =
        {
            Vector2Int.up,
            Vector2Int.right,
            Vector2Int.down,
            Vector2Int.left
        };

        // === Truy vấn bàn cờ cho module hành vi Enemy (Offline/EnemyBehaviors.cs) ===
        public bool EnemyCanEnter(Vector2Int cell) => IsWalkableForEnemy(cell);
        public bool CellOpen(Vector2Int cell) => IsWalkable(cell);
        public int PathLen(Vector2Int from, Vector2Int to) => PathDistance(from, to);
        internal void SetEnemyPosition(Vector2Int pos) => EnemyPosition = pos;
        internal void AdvancePatrol(int index, int direction) { PatrolIndex = index; PatrolDirection = direction; }

        public TacticalBoardManager(TacticalLevelData data)
        {
            Reset(data);
        }

        public void Reset(TacticalLevelData data)
        {
            Data = data ?? TacticalLevelData.Create(1);
            PlayerPosition = Data.PlayerStartPosition;
            EnemyPosition = Data.EnemyStartPosition;
            MonsterPosition = Data.MonsterStartPosition;
            MoveBank = 0;
            MovesUsed = 0;
            Status = TacticalBoardStatus.Running;
            LastMessage = "Xóa dòng để nhận lượt di chuyển.";
            walls.Clear();
            for (int i = 0; i < Data.WallPositions.Count; i++)
                walls.Add(Data.WallPositions[i]);

            // Nạp địa hình runtime từ dữ liệu màn (design §3).
            boxHp.Clear();
            foreach (var b in Data.BoxPositions)
                boxHp[b] = Mathf.Max(1, Data.WoodenBoxHealth);
            traps.Clear();
            foreach (var t in Data.TrapPositions) traps.Add(t);
            ice.Clear();
            foreach (var i in Data.IcePositions) ice.Add(i);
            portals.Clear();
            foreach (var p in Data.Portals)
            {
                portals[p.A] = p.B;
                portals[p.B] = p.A;
            }
            switchToDoor.Clear();
            closedDoors.Clear();
            foreach (var s in Data.SwitchDoors)
            {
                switchToDoor[s.Switch] = s.Door;
                closedDoors.Add(s.Door); // cửa khởi đầu đóng
            }
            monsterStunTurns = 0;

            EnemyType = Data.EnemyType;
            LastPlayerMove = Vector2Int.zero;
            PatrolIndex = 0;
            PatrolDirection = 1;

            MonsterTimer = Data.MonsterAutoMoveInterval;
            CurrentTarget = TacticalTarget.Player;
            RecomputeMonsterIntent();
        }

        // Design §2.3: 1 dòng→1, 2 dòng→2, 3 dòng→2, 4+ dòng→3. Trần MaxMovementPoint.
        public int AddMovesForClearedLines(int clearedLines, bool comboBonus)
        {
            int gained = 0;
            if (clearedLines == 1)
                gained = 1;
            else if (clearedLines == 2)
                gained = 2;
            else if (clearedLines == 3)
                gained = 2;
            else if (clearedLines >= 4)
                gained = 3;

            gained *= Mathf.Max(1, Data.LineToMoveRate);
            // Combo (xóa nhiều lần đặt liên tiếp) thưởng thêm 1 điểm kỹ năng/di chuyển (design §2.3 ba dòng).
            if (comboBonus && gained > 0)
                gained += 1;

            int cap = Mathf.Max(1, Data.MaxMovementPoint);
            int before = MoveBank;
            MoveBank = Mathf.Min(cap, MoveBank + gained);
            int actualGain = MoveBank - before;

            if (actualGain > 0)
                LastMessage = "+ " + actualGain + " lượt chiến thuật.";
            else if (gained > 0)
                LastMessage = "Đã đầy lượt (" + cap + "). Hãy di chuyển!";
            return actualGain;
        }

        public TacticalBoardStatus MovePlayer(Vector2Int direction)
        {
            if (Status != TacticalBoardStatus.Running)
                return Status;

            if (MoveBank <= 0)
            {
                LastMessage = "Chưa có lượt. Hãy xóa dòng để kiếm lượt.";
                return Status;
            }

            var target = PlayerPosition + direction;
            if (!IsWalkableForPlayer(target))
            {
                LastMessage = "Không thể đi vào ô đó.";
                return Status;
            }

            PlayerPosition = target;
            LastPlayerMove = direction;
            // Địa hình: trượt băng, dịch chuyển cổng, đạp công tắc (design §3.4-3.6).
            PlayerPosition = ResolvePlayerLanding(PlayerPosition, direction);
            MoveBank--;
            MovesUsed++;
            if (Evaluate() != TacticalBoardStatus.Running)
                return Status;

            // Design §2.5 (trường hợp 2): player hành động → enemy đi 1 ô → quái đi 1 bước.
            // Timer quái reset để tránh đi 2 lần liên tiếp trong thời gian ngắn.
            MoveEnemy();
            if (Evaluate() != TacticalBoardStatus.Running)
                return Status;

            int steps = Data != null ? Mathf.Max(1, Data.MonsterStepsPerTurn) : 1;
            for (int i = 0; i < steps; i++)
            {
                StepMonsterOnce();
                if (Evaluate() != TacticalBoardStatus.Running)
                    return Status;
            }

            if (Data == null || Data.ResetMonsterTimerAfterPlayerAction)
                MonsterTimer = MonsterAutoMoveInterval;

            RecomputeMonsterIntent();
            return Status;
        }

        // Được game loop gọi mỗi frame (chỉ chế độ Offline). Đếm ngược timer;
        // khi về 0 quái tự đi 1 bước (design §2.5 trường hợp 1) — enemy KHÔNG đi.
        // Trả về true nếu quái vừa di chuyển (để UI vẽ lại).
        public bool TickMonsterTimer(float deltaTime)
        {
            if (Status != TacticalBoardStatus.Running || Data == null)
                return false;

            MonsterTimer -= deltaTime;
            if (MonsterTimer > 0f)
                return false;

            StepMonsterOnce();
            MonsterTimer = MonsterAutoMoveInterval;
            Evaluate();
            RecomputeMonsterIntent();
            return true;
        }

        public bool IsWall(Vector2Int cell)
        {
            return walls.Contains(cell);
        }

        public bool IsPlayerMoveTarget(Vector2Int cell)
        {
            return Status == TacticalBoardStatus.Running && IsWalkableForPlayer(cell) && Manhattan(cell, PlayerPosition) == 1;
        }

        public bool IsInside(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < Data.BoardWidth && cell.y >= 0 && cell.y < Data.BoardHeight;
        }

        // Đường đi dự kiến của quái (design §2.7): tối đa maxSteps ô kế tiếp trên đường ngắn nhất.
        public List<Vector2Int> GetMonsterPathPreview(int maxSteps)
        {
            var path = new List<Vector2Int>();
            if (Status != TacticalBoardStatus.Running)
                return path;

            Vector2Int target = CurrentTarget == TacticalTarget.Enemy ? EnemyPosition : PlayerPosition;
            bool huntingPlayer = CurrentTarget == TacticalTarget.Player;
            Vector2Int cursor = MonsterPosition;
            for (int i = 0; i < maxSteps; i++)
            {
                Vector2Int next = NextStepMonster(cursor, target, huntingPlayer);
                if (next == cursor || next == target)
                {
                    if (next != cursor)
                        path.Add(next);
                    break;
                }
                path.Add(next);
                cursor = next;
            }
            return path;
        }

        bool IsWalkable(Vector2Int cell)
        {
            // Thùng gỗ và cửa đang đóng chặn đường như tường; bẫy/băng/cổng/công tắc thì đi được.
            return IsInside(cell) && !walls.Contains(cell) && !boxHp.ContainsKey(cell) && !closedDoors.Contains(cell);
        }

        bool IsWalkableForPlayer(Vector2Int cell)
        {
            return IsWalkable(cell) && cell != EnemyPosition && cell != MonsterPosition;
        }

        bool IsWalkableForEnemy(Vector2Int cell)
        {
            return IsWalkable(cell) && cell != PlayerPosition && cell != MonsterPosition;
        }

        // Enemy đi 1 ô mỗi lượt player theo hành vi của loại (design §2.8),
        // rồi chịu hiệu ứng địa hình (trượt băng, dịch chuyển cổng).
        void MoveEnemy()
        {
            var chosen = EnemyBehaviors.ChooseMove(this);
            var dir = chosen - EnemyPosition;
            EnemyPosition = chosen;
            if (dir != Vector2Int.zero)
                EnemyPosition = ResolveEntityLanding(EnemyPosition, dir, forEnemy: true);
        }

        // Dùng chung cho hành vi "nhút nhát"/"thông minh": né xa quái theo đường đi.
        public int ManhattanTo(Vector2Int a, Vector2Int b) => Manhattan(a, b);

        // Quái đi đúng 1 bước theo mục tiêu hiện tại. Xử lý bẫy (stun), phá thùng,
        // trượt băng, cổng (design §3.2-3.5).
        void StepMonsterOnce()
        {
            if (Status != TacticalBoardStatus.Running)
                return;

            // Design §3.3: quái đang bị bẫy làm choáng thì bỏ lượt đi này.
            if (monsterStunTurns > 0)
            {
                monsterStunTurns--;
                LastMessage = "Quái đang mắc bẫy!";
                return;
            }

            CurrentTarget = SelectMonsterTarget();
            Vector2Int targetPos = CurrentTarget == TacticalTarget.Enemy ? EnemyPosition : PlayerPosition;
            bool huntingPlayer = CurrentTarget == TacticalTarget.Player;
            Vector2Int next = NextStepMonster(MonsterPosition, targetPos, huntingPlayer);

            if (next == MonsterPosition)
            {
                // Bị chặn — thử phá thùng gần hướng mục tiêu (design §3.2).
                if (TryMonsterBreakBox(targetPos))
                    return;
                return;
            }

            var dir = next - MonsterPosition;
            MonsterPosition = next;

            // Design §3.3: đạp bẫy → choáng lượt sau, bẫy biến mất.
            if (traps.Remove(MonsterPosition))
            {
                monsterStunTurns = Data != null ? Mathf.Max(1, Data.TrapStunTurns) : 1;
                LastMessage = "Quái dính bẫy!";
            }

            // Design §3.4/§3.5: quái cũng trượt băng và đi qua cổng.
            MonsterPosition = ResolveEntityLanding(MonsterPosition, dir, forEnemy: false);
        }

        // Quái phá 1 thùng kề nó theo hướng ngắn nhất tới mục tiêu (design §3.2).
        bool TryMonsterBreakBox(Vector2Int targetPos)
        {
            Vector2Int bestBox = MonsterPosition;
            int bestDist = int.MaxValue;
            foreach (var dir in Directions)
            {
                var cell = MonsterPosition + dir;
                if (!boxHp.ContainsKey(cell))
                    continue;
                int dist = Manhattan(cell, targetPos);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestBox = cell;
                }
            }
            if (bestBox == MonsterPosition)
                return false;

            int hp = boxHp[bestBox] - 1;
            if (hp <= 0)
            {
                boxHp.Remove(bestBox); // vỡ → thành đường đi
                LastMessage = "Quái đập vỡ thùng gỗ!";
            }
            else
            {
                boxHp[bestBox] = hp;
                LastMessage = "Quái đang phá thùng gỗ...";
            }
            return true;
        }

        // Trượt băng + dịch chuyển cổng cho player, kèm đạp công tắc.
        Vector2Int ResolvePlayerLanding(Vector2Int pos, Vector2Int dir)
        {
            pos = SlideOnIce(pos, dir, forEnemy: false, forPlayer: true);
            pos = ApplyPortal(pos);
            ToggleSwitchAt(pos);
            return pos;
        }

        Vector2Int ResolveEntityLanding(Vector2Int pos, Vector2Int dir, bool forEnemy)
        {
            pos = SlideOnIce(pos, dir, forEnemy: forEnemy, forPlayer: false);
            pos = ApplyPortal(pos);
            return pos;
        }

        // Design §3.4: trượt theo hướng hiện tại tới khi rời vùng băng hoặc gặp vật cản.
        Vector2Int SlideOnIce(Vector2Int pos, Vector2Int dir, bool forEnemy, bool forPlayer)
        {
            if (dir == Vector2Int.zero)
                return pos;
            int guard = 0;
            while (ice.Contains(pos) && guard++ < 32)
            {
                var next = pos + dir;
                bool canEnter = forPlayer ? IsWalkableForPlayer(next)
                              : forEnemy ? IsWalkableForEnemy(next)
                              : IsWalkable(next) && next != PlayerPosition && next != EnemyPosition;
                if (!canEnter)
                    break;
                pos = next;
            }
            return pos;
        }

        // Design §3.5: bước vào cổng thì hiện ra ở cổng liên kết (nếu ô đó trống).
        Vector2Int ApplyPortal(Vector2Int pos)
        {
            if (portals.TryGetValue(pos, out var dest))
            {
                bool free = dest != PlayerPosition && dest != EnemyPosition && dest != MonsterPosition;
                if (free && IsWalkable(dest))
                    return dest;
            }
            return pos;
        }

        // Design §3.6: đạp công tắc → đảo trạng thái cửa liên kết.
        void ToggleSwitchAt(Vector2Int pos)
        {
            if (!switchToDoor.TryGetValue(pos, out var door))
                return;
            if (closedDoors.Contains(door))
            {
                closedDoors.Remove(door);
                LastMessage = "Công tắc: cửa đã MỞ.";
            }
            else
            {
                // Không đóng cửa nếu đang có nhân vật đứng trên đó.
                if (door != PlayerPosition && door != EnemyPosition && door != MonsterPosition)
                {
                    closedDoors.Add(door);
                    LastMessage = "Công tắc: cửa đã ĐÓNG.";
                }
            }
            RecomputeMonsterIntent();
        }

        // Design §2.6: ưu tiên bắt ngay khi cạnh; đổi mục tiêu theo ngưỡng; hòa giữ mục tiêu cũ.
        TacticalTarget SelectMonsterTarget()
        {
            int enemyDistance = PathDistance(MonsterPosition, EnemyPosition, blockPlayer: false);
            int playerDistance = PathDistance(MonsterPosition, PlayerPosition, blockPlayer: false);
            int threshold = Data != null ? Mathf.Max(0, Data.TargetSwitchThreshold) : 2;

            if (enemyDistance <= 1)
                return TacticalTarget.Enemy;
            if (playerDistance <= 1)
                return TacticalTarget.Player;

            if (enemyDistance + threshold <= playerDistance)
                return TacticalTarget.Enemy;
            if (playerDistance + threshold <= enemyDistance)
                return TacticalTarget.Player;

            return CurrentTarget;
        }

        void RecomputeMonsterIntent()
        {
            if (Status != TacticalBoardStatus.Running)
            {
                NextMonsterStep = MonsterPosition;
                return;
            }
            CurrentTarget = SelectMonsterTarget();
            Vector2Int targetPos = CurrentTarget == TacticalTarget.Enemy ? EnemyPosition : PlayerPosition;
            bool huntingPlayer = CurrentTarget == TacticalTarget.Player;
            NextMonsterStep = NextStepMonster(MonsterPosition, targetPos, huntingPlayer);
        }

        // BFS từ start đến target, trả về bước đầu tiên trên đường ngắn nhất.
        // Khi đuổi enemy, không đi qua player (tránh thua oan).
        Vector2Int NextStepMonster(Vector2Int start, Vector2Int target, bool huntingPlayer)
        {
            if (start == target) return start;

            var queue = new Queue<Vector2Int>();
            var prev = new Dictionary<Vector2Int, Vector2Int>();
            queue.Enqueue(start);
            prev[start] = start;

            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var dir in Directions)
                {
                    var next = cell + dir;
                    if (!IsWalkable(next) || prev.ContainsKey(next))
                        continue;
                    if (!huntingPlayer && next == PlayerPosition && next != target)
                        continue;
                    prev[next] = cell;
                    if (next == target)
                    {
                        var step = next;
                        while (prev[step] != start)
                            step = prev[step];
                        return step;
                    }
                    queue.Enqueue(next);
                }
            }
            return start; // không tìm được đường
        }

        TacticalBoardStatus Evaluate()
        {
            if (MonsterPosition == EnemyPosition)
            {
                Status = TacticalBoardStatus.Won;
                LastMessage = "Quái đã bắt được đối thủ!";
            }
            else if (MonsterPosition == PlayerPosition)
            {
                Status = TacticalBoardStatus.Failed;
                LastMessage = "Quái đã bắt được bạn.";
            }
            return Status;
        }

        int PathDistance(Vector2Int from, Vector2Int to, bool blockPlayer = false)
        {
            if (from == to)
                return 0;

            var queue = new Queue<Vector2Int>();
            var distance = new Dictionary<Vector2Int, int>();
            queue.Enqueue(from);
            distance[from] = 0;

            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                int nextDistance = distance[cell] + 1;
                for (int i = 0; i < Directions.Length; i++)
                {
                    var next = cell + Directions[i];
                    if (!IsWalkable(next) || distance.ContainsKey(next))
                        continue;
                    if (blockPlayer && next == PlayerPosition && next != to)
                        continue;
                    if (next == to)
                        return nextDistance;
                    distance[next] = nextDistance;
                    queue.Enqueue(next);
                }
            }

            return 1000 + Manhattan(from, to);
        }

        int Manhattan(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }
    }
}
