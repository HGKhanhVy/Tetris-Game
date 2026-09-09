using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace BrickStacker
{
    public partial class BrickGameController : MonoBehaviour
    {
        void SpawnPiece()
        {
            if (nextBag.Count < 7)
                FillBag();

            currentSpecialKind = 0;
            if (!rules.UseResourceClusters && rules.AllowSpecialBlocks && piecesLocked > 4)
            {
                float roll = UnityEngine.Random.value;
                float twoRowChance = 0.050f + Mathf.Min(0.035f, journeyLevel * 0.001f);
                float fiveRowChance = journeyLevel >= 12 ? 0.010f + Mathf.Min(0.015f, journeyLevel * 0.0004f) : 0.004f;
                if (roll < fiveRowChance)
                    currentSpecialKind = 2;
                else if (roll < fiveRowChance + twoRowChance)
                    currentSpecialKind = 1;
            }

            currentPieceIsSpecial = currentSpecialKind > 0;
            currentType = currentPieceIsSpecial ? UnityEngine.Random.Range(0, palette.Length) : nextBag.Dequeue();
            origin = SpawnOriginForCurrentPiece();
            if (rules.UseResourceClusters)
                AssignActiveResources();
            rotation = 0;
            canHold = true;
            fallTimer = 0f;
            lockDelayTimer = 0f;
            touchingGround = false;
            ClearActive();

            if (!IsValid(origin, rotation))
            {
                EndGame(false);
                return;
            }

            DrawActive();
            UpdateUi();
        }

        void FillBag()
        {
            if (rules.ForcedPieceType >= 0)
            {
                while (nextBag.Count < 14)
                    nextBag.Enqueue(rules.ForcedPieceType);
                return;
            }

            var bag = new List<int> { 0, 1, 2, 3, 4, 5, 6 };
            while (bag.Count > 0)
            {
                int index = UnityEngine.Random.Range(0, bag.Count);
                nextBag.Enqueue(bag[index]);
                bag.RemoveAt(index);
            }
        }

        void TryMove(Vector2Int delta)
        {
            if (puzzlePausedForTacticalTurn)
                return;
            if (TutorialBlocks(TutorialAction.MoveSideways))
                return;   // bước hướng dẫn khác đang chạy -> khoá thao tác này

            if (!IsValid(origin + delta, rotation))
                return;

            origin += delta;
            movedHorizontallyThisFrame = true;
            // KHÔNG reset fallTimer — di chuyển ngang không được làm dừng đồng hồ rơi (trước đây giữ
            // tay kéo liên tục làm khối đứng yên, không tự rơi). Chỉ reset lock-delay khi đang chạm đất.
            if (touchingGround)
                lockDelayTimer = 0f;
            DrawActive();
            Beep(520f, 0.025f, 0.08f);
            TutorialNotify(TutorialAction.MoveSideways);
        }

        bool CanMoveDown()
        {
            if (currentPieceIsSpecial)
            {
                var below = origin + Vector2Int.down;
                return IsInBounds(below) && grid[below.x, below.y] == 0;
            }
            return IsValid(origin + Vector2Int.down, rotation);
        }

        void SoftDrop()
        {
            if (puzzlePausedForTacticalTurn)
                return;

            if (CanMoveDown())
            {
                origin += Vector2Int.down;
                touchingGround = false;
                lockDelayTimer = 0f;
                DrawActive();
                UpdateUi();
            }
            else
            {
                touchingGround = true;
            }
        }

        void StepDown()
        {
            if (puzzlePausedForTacticalTurn)
                return;

            if (CanMoveDown())
            {
                origin += Vector2Int.down;
                touchingGround = false;
                lockDelayTimer = 0f;
                DrawActive();
            }
            else
            {
                touchingGround = true;
            }
        }

        void HardDrop()
        {
            if (puzzlePausedForTacticalTurn)
                return;

            while (CanMoveDown())
                origin += Vector2Int.down;

            DrawActive();
            LockPiece();
            shake = 0.16f;
            Beep(110f, 0.08f, 0.18f);
        }

        bool IsInBounds(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < Width && cell.y >= 0 && cell.y < Height;
        }

        void TryRotate(int direction)
        {
            if (puzzlePausedForTacticalTurn)
                return;

            if (rules.RotationLimit > 0 && rotationsThisLevel >= rules.RotationLimit)
            {
                Beep(160f, 0.04f, 0.08f);
                return;
            }

            // Khối VUÔNG (O): vị trí xoay = chính nó, nhưng ở chế độ cụm ta xoay CÁCH SẮP tài nguyên
            // trong ô vuông (giữ nguyên vị trí) để người chơi rearrange được — trước đây không xoay được.
            if (currentType == 3)
            {
                if (rules.UseResourceClusters && activeResources != null && activeResources.Length >= 4)
                {
                    int steps = direction < 0 ? 3 : 1; // ngược = xoay 3 bước cùng chiều
                    for (int s = 0; s < steps; s++)
                        RotateSquareResources();
                    rotationsThisLevel++;
                    DrawActive();
                    Beep(720f, 0.035f, 0.08f);
                }
                return;
            }

            int nextRotation = (rotation + direction + 4) % 4;
            var kicks = new[] { Vector2Int.zero, Vector2Int.left, Vector2Int.right, new Vector2Int(0, 1), new Vector2Int(-2, 0), new Vector2Int(2, 0) };
            foreach (var kick in kicks)
            {
                if (IsValid(origin + kick, nextRotation))
                {
                    origin += kick;
                    rotation = nextRotation;
                    rotationsThisLevel++;
                    DrawActive();
                    Beep(720f, 0.035f, 0.08f);
                    return;
                }
            }
        }

        // Xoay tài nguyên 1 bước theo chiều kim đồng hồ trong ô vuông 2×2 (shapes[3] = TL,TR,BL,BR).
        // TL→TR→BR→BL→TL: new[1]=old[0], new[3]=old[1], new[2]=old[3], new[0]=old[2].
        void RotateSquareResources()
        {
            if (activeResources == null || activeResources.Length < 4)
                return;
            var oldR = (Puzzle.ResourceType[])activeResources.Clone();
            activeResources[1] = oldR[0];
            activeResources[3] = oldR[1];
            activeResources[2] = oldR[3];
            activeResources[0] = oldR[2];
            if (activeBlank != null && activeBlank.Length >= 4)
            {
                var oldB = (bool[])activeBlank.Clone();
                activeBlank[1] = oldB[0];
                activeBlank[3] = oldB[1];
                activeBlank[2] = oldB[3];
                activeBlank[0] = oldB[2];
            }
        }

        void RotateFromButton()
        {
            if (paused || resolving || gameOver || puzzlePausedForTacticalTurn)
                return;
            if (TutorialBlocks(TutorialAction.RotateButton))
                return;

            TryRotate(1);
            TutorialNotify(TutorialAction.RotateButton);
        }

        void SwapHoldPiece()
        {
            if (paused || resolving || gameOver || puzzlePausedForTacticalTurn || !canHold || currentPieceIsSpecial || rules.UseResourceClusters)
                return;

            RuntimeArt.PlayUiSwitchSound();
            holdsThisLevel++;
            ClearActive();
            if (holdType < 0)
            {
                holdType = currentType;
                SpawnPiece();
            }
            else
            {
                int old = currentType;
                currentType = holdType;
                holdType = old;
                currentPieceIsSpecial = false;
                currentSpecialKind = 0;
                origin = SpawnOriginForCurrentPiece();
                rotation = 0;
                fallTimer = 0f;
                if (!IsValid(origin, rotation))
                {
                    EndGame(false);
                    return;
                }
                DrawActive();
                UpdateUi();
            }

            canHold = false;
            Beep(330f, 0.06f, 0.12f);
        }

        void Hold()
        {
            SwapHoldPiece();
        }

        void LockPiece()
        {
            if (resolving)
                return;

            resolving = true;
            bool bombShouldExplode = false;
            Vector2Int bombCell = Vector2Int.zero;

            if (currentPieceIsSpecial)
            {
                bombCell = origin;
                if (bombCell.y < Height)
                {
                    grid[bombCell.x, bombCell.y] = currentType + 1;
                    if (!usingSceneGameplayCanvas)
                    {
                        Color specialColor = currentSpecialKind == 2 ? new Color(0.35f, 0.95f, 1f, 1f) : RuntimeArt.SpecialBlockColor;
                        var bombBlock = NewBlock(currentSpecialKind == 2 ? "Grand Bomb Block" : "Bomb Block", specialColor, settledRoot);
                        bombBlock.transform.position = CellToWorld(bombCell.x, bombCell.y);
                        lockedBlocks[bombCell.x, bombCell.y] = bombBlock;
                    }
                    bombShouldExplode = true;
                }
            }
            else
            {
                var shape = shapes[currentType];
                for (int i = 0; i < shape.Length; i++)
                {
                    var cell = CellFromLocal(shape[i], origin, rotation);
                    if (cell.x < 0 || cell.x >= Width || cell.y < 0 || cell.y >= Height)
                        continue;

                    bool clusters = rules.UseResourceClusters && activeResources != null && i < activeResources.Length;
                    int blockType = clusters ? CellDisplayType(i, activeResources, activeBlank) : currentType;
                    grid[cell.x, cell.y] = clusters ? CellLockValue(i) : currentType + 1;
                    if (!usingSceneGameplayCanvas)
                    {
                        var block = NewPieceBlock("Locked Block", blockType, settledRoot);
                        block.transform.position = CellToWorld(cell.x, cell.y);
                        lockedBlocks[cell.x, cell.y] = block;
                    }
                }
            }

            piecesLocked++;
            currentPieceIsSpecial = false;
            Haptics.Light(); // rung nhẹ xác nhận khối đã khóa
            feedbacks.Play(GameFeedbackId.PieceLock);
            ClearActive();
            int specialKind = currentSpecialKind;
            currentSpecialKind = 0;
            if (rules.UseResourceClusters)
                StartCoroutine(ResolveClustersThenSpawn());
            else
                StartCoroutine(ResolveLinesThenSpawn(bombShouldExplode, bombCell, specialKind));
        }

        IEnumerator ResolveLinesThenSpawn(bool bombShouldExplode, Vector2Int bombCell, int specialKind)
        {
            if (bombShouldExplode)
                yield return ExplodeSpecialBlock(bombCell, specialKind);

            int cleared = FindFullRows().Count;
            if (cleared > 0)
            {
                maxLinesClearedAtOnce = Mathf.Max(maxLinesClearedAtOnce, cleared);
                yield return ClearRows();
            }
            else
            {
                combo = 0;
            }

            int dangerTick = rules.RisingDangerSeconds > 0 ? Mathf.FloorToInt(gameplayTime / rules.RisingDangerSeconds) : 0;
            bool risingGarbage = dangerTick > 0 && dangerTick != lastRisingDangerTick;
            if (risingGarbage)
                lastRisingDangerTick = dangerTick;
            bool timedGarbage = rules.GarbageEveryPieces > 0 && piecesLocked % rules.GarbageEveryPieces == 0;
            bool surpriseGarbage = cleared == 0 && piecesLocked > 5 && rules.SurpriseGarbageChance > 0f && UnityEngine.Random.value < rules.SurpriseGarbageChance;
            if (!gameOver && (timedGarbage || surpriseGarbage || risingGarbage))
            {
                AddGarbageRow();
                shake = 0.2f;
                Beep(82f, 0.12f, 0.2f);
            }

            if (IsMissionComplete())
            {
                LevelComplete();
                yield break;
            }

            resolving = false;
            SpawnPiece();
        }

        // GD v3 – khởi tạo tầng puzzle cụm (gọi lười, mode theo Offline/Online hiện tại).
        void EnsureResourcePuzzle()
        {
            if (clusterResolver == null)
            {
                puzzleBoard = new Puzzle.PuzzleBoard(Width, Height);
                clusterResolver = new Puzzle.ClusterResolutionSystem(Width, Height);
            }
            if (resourceBag == null)
            {
                var mode = MultiplayerMatch.Active ? Puzzle.PuzzleMode.Online : Puzzle.PuzzleMode.Offline;
                resourceBag = new Puzzle.ResourceBagService(mode);
            }
        }

        // Gán tài nguyên cho mô hình đang rơi, giữ tỷ lệ số-loại 15/55/30 (GD §2.1).
        // Dùng lại bộ tài nguyên đã sinh trước cho ô Next để "TIẾP" khớp đúng mô hình sắp rơi.
        void AssignActiveResources()
        {
            EnsureResourcePuzzle();
            var shape = shapes[currentType];
            if (nextResources != null && nextResources.Length == shape.Length)
            {
                activeResources = nextResources;
                activeBlank = nextBlank;
            }
            else
            {
                activeResources = new Puzzle.ResourceType[shape.Length];
                Puzzle.ResourceAssigner.Assign(resourceBag, puzzleRng, activeResources);
                activeBlank = RollBlanks(shape.Length);
            }

            PrepareNextResources();
        }

        // Sinh trước tài nguyên cho mô hình kế tiếp (PeekNext) để ô Next hiển thị đúng.
        void PrepareNextResources()
        {
            int len = shapes[PeekNext(0)].Length;
            if (nextResources == null || nextResources.Length != len || ReferenceEquals(nextResources, activeResources))
                nextResources = new Puzzle.ResourceType[len];
            Puzzle.ResourceAssigner.Assign(resourceBag, puzzleRng, nextResources);
            nextBlank = RollBlanks(len);
        }

        // Random ô TRỐNG (rác) trong mô hình để làm loãng tài nguyên — chỉ offline; luôn chừa ≥1 ô tài nguyên.
        bool[] RollBlanks(int len)
        {
            var b = new bool[len];
            if (MultiplayerMatch.Active || len <= 1)
                return b;
            int blanks = 0;
            for (int i = 0; i < len; i++)
                if (puzzleRng.NextDouble() < OfflineBlankChance) { b[i] = true; blanks++; }
            if (blanks >= len)
                b[puzzleRng.Next(len)] = false;
            return b;
        }

        // Giá trị ghi vào grid khi khóa: ô trống = rác (GarbageValue), else tài nguyên+1.
        int CellLockValue(int i) => activeBlank != null && i < activeBlank.Length && activeBlank[i]
            ? Puzzle.GridBridge.GarbageValue
            : Puzzle.GridBridge.Encode(activeResources[i]);

        // Loại hiển thị: ô trống → >=4 (render xám); else index tài nguyên (0-3).
        int CellDisplayType(int i, Puzzle.ResourceType[] res, bool[] blank)
            => blank != null && i < blank.Length && blank[i]
                ? Puzzle.GridBridge.GarbageValue - 1
                : (int)res[i];

        // GD v3 – thay xóa hàng bằng: tìm cụm 4 hướng → nổ rác sát cạnh → xóa đồng thời →
        // gravity từng cột → combo dây chuyền. Chưa nối hiệu ứng Move/Attack/Shield/Energy (GĐ2/GĐ4).
        IEnumerator ResolveClustersThenSpawn()
        {
            EnsureResourcePuzzle();
            Puzzle.GridBridge.Load(grid, puzzleBoard);

            var outcome = new Puzzle.ResolutionOutcome();
            if (usingSceneGameplayCanvas)
                // Bàn UI: giải TỪNG BẬC dây chuyền, animate biến mất giữa các bậc (§3.5) để thấy rõ.
                yield return ResolveClustersAnimated(outcome);
            else
            {
                outcome = clusterResolver.Resolve(puzzleBoard);
                Puzzle.GridBridge.Store(puzzleBoard, grid);
            }

            if (outcome.ActivatedAnything)
            {
                combo = outcome.ChainCount;
                maxComboThisLevel = Mathf.Max(maxComboThisLevel, combo);

                int activated = 0;
                foreach (var step in outcome.Steps)
                    activated += step.Clusters.Count;

                float finalMultiplier = outcome.Steps[outcome.Steps.Count - 1].Multiplier;
                score += Mathf.RoundToInt(activated * 120 * Mathf.Max(1, rules.ScoreMultiplier) * finalMultiplier);
                shake = 0.12f + Mathf.Min(0.3f, activated * 0.04f);

                if (!usingSceneGameplayCanvas)
                {
                    Beep(760f + combo * 90f, 0.12f, 0.22f);
                    clearParticles.transform.position = CellToWorld(Width / 2, Height / 2);
                    clearParticles.Play();
                    RedrawLocked();
                }
                UpdateUi();
            }
            else
            {
                combo = 0;
            }

            // GD v3: nối cụm → hiệu ứng. Offline: bàn Monster. Online 1v1: đánh/khiên/energy (GĐ4).
            if (!MultiplayerMatch.Active && outcome.ActivatedAnything)
            {
                // Tutorial đã chạy hiệu ứng ngay theo từng nhóm ở trên rồi — cộng dồn lần nữa ở đây
                // là ăn đúp tài nguyên.
                if (tutorialHandledClusterEffects)
                {
                    tutorialHandledClusterEffects = false;
                    if (tacticalBoard != null && tacticalBoard.Status != TacticalBoardStatus.Running)
                    {
                        OnTacticalStatusResolved();
                        yield break;
                    }
                }
                else if (ApplyOfflineClusterEffects(outcome))
                {
                    OnTacticalStatusResolved();
                    yield break;
                }
            }
            else if (MultiplayerMatch.Active && outcome.ActivatedAnything)
            {
                ApplyOnlineClusterEffects(outcome);
            }

            int dangerTick = rules.RisingDangerSeconds > 0 ? Mathf.FloorToInt(gameplayTime / rules.RisingDangerSeconds) : 0;
            bool risingGarbage = dangerTick > 0 && dangerTick != lastRisingDangerTick;
            if (risingGarbage)
                lastRisingDangerTick = dangerTick;
            bool timedGarbage = rules.GarbageEveryPieces > 0 && piecesLocked % rules.GarbageEveryPieces == 0;
            if (!gameOver && (timedGarbage || risingGarbage))
            {
                AddGarbageRow();
                shake = 0.2f;
                Beep(82f, 0.12f, 0.2f);
            }

            if (IsMissionComplete())
            {
                LevelComplete();
                yield break;
            }

            resolving = false;
            SpawnPiece();
        }

        IEnumerator ExplodeSpecialBlock(Vector2Int center, int specialKind)
        {
            int removed = 0;
            // Xóa hàng bombCell đặt vào và 1 hàng kề (ưu tiên hàng dưới, fallback hàng trên).
            int landedRow = Mathf.Clamp(center.y, 0, Height - 1);
            var rowsToClear = new List<int> { landedRow };
            int neighborRow = landedRow - 1 >= 0 ? landedRow - 1 : landedRow + 1;
            if (neighborRow >= 0 && neighborRow < Height)
                rowsToClear.Add(neighborRow);

            if (center.x >= 0 && center.x < Width && center.y >= 0 && center.y < Height)
            {
                if (!rowsToClear.Contains(center.y))
                    grid[center.x, center.y] = 0;
                if (lockedBlocks[center.x, center.y] != null)
                {
                    PulseAndDestroy(lockedBlocks[center.x, center.y]);
                    lockedBlocks[center.x, center.y] = null;
                }
            }

            foreach (int y in rowsToClear)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (grid[x, y] == 0)
                        continue;

                    grid[x, y] = 0;
                    removed++;
                    if (lockedBlocks[x, y] != null)
                    {
                        PulseAndDestroy(lockedBlocks[x, y]);
                        lockedBlocks[x, y] = null;
                    }
                }

                clearParticles.transform.position = CellToWorld(Width / 2, y);
                clearParticles.Play();
            }

            score += Mathf.Max(removed, Width) * 90;
            lines += rowsToClear.Count;
            levelLines += rowsToClear.Count;
            PlayerPrefs.SetInt(LevelProgress.TotalLinesClearedKey, PlayerPrefs.GetInt(LevelProgress.TotalLinesClearedKey, 0) + rowsToClear.Count);
            // Khối đặc biệt nổ chỉ thưởng đúng 1 lượt đi — lượt "xịn" phải từ xóa hàng thường.
            AwardTacticalMoves(1, false);
            shake = 0.32f;
            Beep(160f, 0.16f, 0.28f);

            if (usingSceneGameplayCanvas && scenePuzzleCells != null)
                yield return FlashAndFadeClearRows(rowsToClear);
            else
                yield return new WaitForSeconds(0.18f);

            CompactRows(rowsToClear);
            RedrawLocked();
            UpdateUi();
        }

        IEnumerator ClearRows()
        {
            var rows = FindFullRows();
            combo++;
            int clearCount = rows.Count;
            lines += clearCount;
            levelLines += clearCount;
            PlayerPrefs.SetInt(LevelProgress.TotalLinesClearedKey, PlayerPrefs.GetInt(LevelProgress.TotalLinesClearedKey, 0) + clearCount);
            score += clearCount * 120 * rules.ScoreMultiplier;
            maxComboThisLevel = Mathf.Max(maxComboThisLevel, combo);
            AwardTacticalMoves(clearCount, combo > 1);
            shake = 0.1f + clearCount * 0.05f;
            Beep(880f + clearCount * 120f, 0.12f, 0.24f);

            // GD v3: 1v1 KHÔNG dùng luật xóa-hàng này (dùng cụm tài nguyên). Năng lượng/đánh/khiên
            // nạp từ CỤM trong ResolveClustersThenSpawn (ApplyOnlineClusterEffects).

            foreach (int row in rows)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (lockedBlocks[x, row] != null)
                    {
                        PulseAndDestroy(lockedBlocks[x, row]);
                        lockedBlocks[x, row] = null;
                    }
                    grid[x, row] = 0;
                }
                clearParticles.transform.position = CellToWorld(Width / 2, row);
                clearParticles.Play();
            }

            // Flash + fade animation for scene canvas mode.
            if (usingSceneGameplayCanvas && scenePuzzleCells != null)
                yield return FlashAndFadeClearRows(rows);
            else
                yield return new WaitForSeconds(0.18f);

            CompactRows(rows);

            RedrawLocked();
            UpdateUi();
        }

        IEnumerator FlashAndFadeClearRows(List<int> rows)
        {
            const float flashDuration = 0.05f;
            const float fadeDuration  = 0.24f;
            const float smokeDuration = 0.55f;

            // Collect cell world positions before clearing, spawn smoke particles.
            var smokeParticles = new List<(RectTransform rt, Vector2 vel, Color col)>();
            var rng = new System.Random();

            if (scenePuzzleGridRect != null)
            {
                // 4 fragment directions per cell: top-left, top-right, bottom-left, bottom-right.
                var dirs = new Vector2[] {
                    new Vector2(-1f,  1f), new Vector2( 1f,  1f),
                    new Vector2(-1f, -1f), new Vector2( 1f, -1f)
                };
                foreach (int row in rows)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        var cell = scenePuzzleCells[x, row];
                        if (cell == null || !cell.enabled) continue;

                        Color cellColor = cell.color;
                        cellColor.a = 0.9f;
                        var cellPos  = ScenePuzzleGridToUiPosition(x, row);
                        var gridAnchor = scenePuzzleGridRect.anchoredPosition;
                        float halfCell = puzzleCellSizeX * 0.25f;
                        float speed    = puzzleCellSizeX * 1.8f;

                        foreach (var dir in dirs)
                        {
                            var fragGo   = new GameObject("Frag", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                            var fragImg  = fragGo.GetComponent<UnityEngine.UI.Image>();
                            fragImg.sprite = cell.sprite;
                            fragImg.color  = cellColor;

                            var fragRect = fragGo.GetComponent<RectTransform>();
                            fragRect.SetParent(scenePuzzleGridRect.parent, false);
                            fragRect.anchorMin = fragRect.anchorMax = new Vector2(0.5f, 0.5f);
                            fragRect.pivot     = new Vector2(0.5f, 0.5f);
                            fragRect.anchoredPosition = gridAnchor + cellPos + dir * halfCell;
                            fragRect.sizeDelta = new Vector2(puzzleCellSizeX * 0.45f, puzzleCellSize * 0.45f);
                            fragRect.localScale = Vector3.one;

                            float jitter = (float)(rng.NextDouble() * 0.4 + 0.8);
                            smokeParticles.Add((fragRect, dir * speed * jitter, cellColor));
                        }
                    }
                }
            }

            // Quét sáng từ trái sang phải — từng cột bừng trắng lần lượt tạo cảm giác "lướt".
            const float sweepPerColumn = 0.016f;
            float sweepTime = 0f;
            int litColumns = 0;
            while (litColumns < Width)
            {
                sweepTime += UnityEngine.Time.deltaTime;
                int lit = Mathf.Min(Width, Mathf.FloorToInt(sweepTime / sweepPerColumn) + 1);
                for (int x = litColumns; x < lit; x++)
                {
                    foreach (int row in rows)
                        if (scenePuzzleCells[x, row] != null)
                        {
                            scenePuzzleCells[x, row].sprite  = null;
                            scenePuzzleCells[x, row].color   = new Color(1f, 0.97f, 0.86f, 1f);
                            scenePuzzleCells[x, row].enabled = true;
                        }
                }
                litColumns = lit;
                if (litColumns < Width)
                    yield return null;
            }

            yield return new WaitForSeconds(flashDuration);

            // Fade mượt bằng smoothstep (nhanh dần rồi hãm lại thay vì tuyến tính).
            float t = 0f;
            while (t < fadeDuration)
            {
                t += UnityEngine.Time.deltaTime;
                float p = Mathf.Clamp01(t / fadeDuration);
                float alpha = 1f - (p * p * (3f - 2f * p));
                foreach (int row in rows)
                    for (int x = 0; x < Width; x++)
                        if (scenePuzzleCells[x, row] != null)
                            scenePuzzleCells[x, row].color = new Color(1f, 0.97f, 0.86f, alpha);
                yield return null;
            }
            foreach (int row in rows)
                for (int x = 0; x < Width; x++)
                    if (scenePuzzleCells[x, row] != null)
                        scenePuzzleCells[x, row].enabled = false;

            // Fire-and-forget: animate fragments in the background so resolving can clear immediately.
            if (smokeParticles.Count > 0)
                StartCoroutine(AnimateFragments(smokeParticles, smokeDuration));
        }

        IEnumerator AnimateFragments(List<(RectTransform rt, Vector2 vel, Color col)> particles, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += UnityEngine.Time.deltaTime;
                float progress = t / duration;
                float alpha = Mathf.Lerp(0.9f, 0f, progress);
                float scale  = Mathf.Lerp(1f, 0.3f, progress);
                foreach (var (rt, vel, baseCol) in particles)
                {
                    if (rt == null) continue;
                    rt.anchoredPosition += vel * UnityEngine.Time.deltaTime;
                    rt.localScale = Vector3.one * scale;
                    var img = rt.GetComponent<UnityEngine.UI.Image>();
                    if (img != null) img.color = new Color(baseCol.r, baseCol.g, baseCol.b, alpha);
                }
                yield return null;
            }
            foreach (var (rt, _, _) in particles)
                if (rt != null) UnityEngine.Object.Destroy(rt.gameObject);
        }

        void CompactRows(List<int> rows)
        {
            var cleared = new HashSet<int>(rows);
            var nextGrid = new int[Width, Height];
            int writeY = 0;

            for (int y = 0; y < Height; y++)
            {
                if (cleared.Contains(y))
                    continue;

                for (int x = 0; x < Width; x++)
                    nextGrid[x, writeY] = grid[x, y];
                writeY++;
            }

            grid = nextGrid;
        }

        void AddGarbageRow()
        {
            for (int y = Height - 1; y > 0; y--)
            {
                for (int x = 0; x < Width; x++)
                    grid[x, y] = grid[x, y - 1];
            }

            int hole = UnityEngine.Random.Range(0, Width);
            // GD v3 §16: chế độ cụm → ô RÁC thật (không mang tài nguyên, phá bằng vụ nổ cụm sát cạnh).
            int fill = rules.UseResourceClusters ? Puzzle.GridBridge.GarbageValue : UnityEngine.Random.Range(1, 8);
            for (int x = 0; x < Width; x++)
                grid[x, 0] = x == hole ? 0 : fill;

            RedrawLocked();
        }

        void RedrawLocked()
        {
            foreach (Transform child in settledRoot)
                Destroy(child.gameObject);

            lockedBlocks = new GameObject[Width, Height];
            if (usingSceneGameplayCanvas)
            {
                RefreshScenePuzzleBoardUi();
                return;
            }

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (grid[x, y] <= 0)
                        continue;

                    var block = NewPieceBlock("Locked Block", grid[x, y] - 1, settledRoot);
                    block.transform.position = CellToWorld(x, y);
                    lockedBlocks[x, y] = block;
                }
            }

            RefreshScenePuzzleBoardUi();
        }

        // Giải dây chuyền TỪNG BẬC: hiện trạng thái trước xóa → animate ô biến mất → xóa + trọng lực
        // → vẽ lại → dừng nhẹ → bậc kế. Tích lũy outcome cho hiệu ứng đánh/khiên/energy sau đó.
        IEnumerator ResolveClustersAnimated(Puzzle.ResolutionOutcome outcome)
        {
            int chain = 0;
            while (true)
            {
                var clusters = clusterResolver.DetectStep(puzzleBoard);
                if (clusters == null)
                    break;
                chain++;
                tutorialAnyClear = true; // mốc tutorial: đã ăn cụm đầu tiên
                TutorialNotify(TutorialAction.FormCluster);

                // Bàn đang hiển thị trạng thái TRƯỚC khi xóa bậc này → cho ô phồng to + mờ dần.
                Puzzle.GridBridge.Store(puzzleBoard, grid);
                RedrawLocked();

                // Tutorial dạy LẦN LƯỢT từng loại: tách cụm theo loại tài nguyên rồi chạy trọn mạch
                // "giới thiệu → đúng nhóm đó biến mất → chức năng của nó" cho từng nhóm, xong mới
                // sang loại kế. Ngoài tutorial thì cả bậc vẫn là một nhóm duy nhất như cũ.
                BuildClusterGroups(clusters);

                for (int g = 0; g < clusterGroupCount; g++)
                {
                    var group = clusterGroups[g];

                    // Dừng đúng lúc nhóm còn nguyên trên bàn, chừa sáng riêng các ô của nó.
                    if (tutorialActive)
                        yield return TutorialSpotlightClusters(group);

                    ShowClusterCombo(chain);
                    SpawnClusterBursts(group); // nổ tia/khói tại tâm mỗi cụm
                    Beep(720f + chain * 110f, 0.10f, 0.20f);
                    Haptics.Combo(chain); // rung mạnh dần theo cấp combo
                    feedbacks.Play(GameFeedbackId.Combo, transform.position, 1f + (chain - 1) * 0.25f);
                    shake = Mathf.Max(shake, 0.14f);
                    yield return PlayClusterVanishFx(group);

                    // Xóa nhóm này (CHƯA rơi: rơi sớm sẽ dời chỗ các nhóm chưa xử lý).
                    var destroyed = clusterResolver.RemoveStep(puzzleBoard, group);
                    Puzzle.GridBridge.Store(puzzleBoard, grid);
                    RedrawLocked();
                    outcome.Steps.Add(new Puzzle.ResolutionStep(group, chain,
                        Puzzle.ClusterResolutionSystem.ComboMultiplier(chain), destroyed));

                    // Chức năng chỉ chạy SAU khi đúng nhóm đó đã biến mất.
                    if (tutorialActive)
                    {
                        tutorialHandledClusterEffects = true;
                        yield return TutorialExplainResources();
                        bool boardEnded = ApplyOfflineClusterEffects(group);
                        yield return TutorialWatchResourceEffects();
                        if (boardEnded)
                            yield break;
                    }
                }

                // Trọng lực sau khi cả bậc đã biến mất → vẽ lại, dừng nhẹ để mắt kịp theo.
                clusterResolver.ApplyGravityStep(puzzleBoard);
                Puzzle.GridBridge.Store(puzzleBoard, grid);
                RedrawLocked();
                yield return new WaitForSeconds(0.30f);
            }
        }

        // Tách các cụm của một bậc thành nhóm theo loại tài nguyên, giữ nguyên thứ tự gặp.
        // Ngoài tutorial chỉ có đúng một nhóm — luồng chơi thật không đổi nhịp.
        void BuildClusterGroups(List<Puzzle.Cluster> clusters)
        {
            clusterGroupOrder.Clear();
            clusterGroupCount = 0;

            if (!tutorialActive)
            {
                NextClusterGroup().AddRange(clusters);
                return;
            }

            foreach (var cluster in clusters)
            {
                int index = clusterGroupOrder.IndexOf(cluster.Resource);
                if (index < 0)
                {
                    index = clusterGroupOrder.Count;
                    clusterGroupOrder.Add(cluster.Resource);
                    NextClusterGroup();
                }
                clusterGroups[index].Add(cluster);
            }
        }

        // Danh sách con dùng lại qua từng bậc dây chuyền, không cấp phát mới mỗi lần.
        List<Puzzle.Cluster> NextClusterGroup()
        {
            while (clusterGroups.Count <= clusterGroupCount)
                clusterGroups.Add(new List<Puzzle.Cluster>());
            var group = clusterGroups[clusterGroupCount];
            group.Clear();
            clusterGroupCount++;
            return group;
        }

        // Vụ nổ tia/khói tại tâm mỗi cụm khi biến mất (mạnh hơn, không nhạt nhòa).
        void SpawnClusterBursts(List<Puzzle.Cluster> clusters)
        {
            if (scenePuzzleCells == null || clusters == null)
                return;
            foreach (var cluster in clusters)
            {
                int cx = 0, cy = 0, n = 0;
                foreach (var c in cluster.Cells) { cx += c.x; cy += c.y; n++; }
                if (n == 0) continue;
                cx /= n; cy /= n;
                Vector3 burstPos = PuzzleCellScreenPos(cx, cy);
                var burst = GetClusterBurst();
                if (burst != null)
                    burst.Play(burstPos, ClusterBurstColor(cluster.Resource));
                feedbacks.Play(GameFeedbackId.ClusterClear, burstPos);
            }
        }

        static Color ClusterBurstColor(Puzzle.ResourceType r)
        {
            switch (r)
            {
                case Puzzle.ResourceType.Move: return new Color(0.55f, 1f, 0.55f);
                case Puzzle.ResourceType.Attack: return new Color(1f, 0.55f, 0.3f);
                case Puzzle.ResourceType.Shield: return new Color(0.55f, 0.8f, 1f);
                default: return new Color(1f, 0.92f, 0.4f); // Energy/sấm sét
            }
        }

        Vector3 PuzzleCellScreenPos(int x, int y)
        {
            if (scenePuzzleCells != null && x >= 0 && x < Width && y >= 0 && y < Height && scenePuzzleCells[x, y] != null)
            {
                var crt = scenePuzzleCells[x, y].rectTransform;
                var canvas = scenePuzzleCells[x, y].GetComponentInParent<Canvas>();
                Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                Vector2 sp = RectTransformUtility.WorldToScreenPoint(cam, crt.position);
                return new Vector3(sp.x, sp.y, 0f);
            }
            return new Vector3(Screen.width * 0.4f, Screen.height * 0.5f, 0f);
        }

        ClusterBurst GetClusterBurst()
        {
            for (int i = 0; i < clusterBurstPool.Count; i++)
                if (clusterBurstPool[i] != null && !clusterBurstPool[i].IsPlaying)
                    return clusterBurstPool[i];
            Transform parent = EnsureFloatFxRoot();
            if (parent == null)
                return null;
            var go = new GameObject("Runtime Cluster Burst", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var burst = go.AddComponent<ClusterBurst>();
            burst.Build(10, RoundUiSprite());
            clusterBurstPool.Add(burst);
            return burst;
        }

        // Hiệu ứng ô tài nguyên biến mất: phồng to + mờ dần rồi mới vẽ lại bàn (cụm mode, bàn UI).
        readonly List<Vector2Int> clusterVanishBuffer = new List<Vector2Int>();
        IEnumerator PlayClusterVanishFx(List<Puzzle.Cluster> clusters)
        {
            if (scenePuzzleCells == null || clusters == null)
                yield break;

            clusterVanishBuffer.Clear();
            foreach (var cluster in clusters)
                foreach (var c in cluster.Cells)
                    clusterVanishBuffer.Add(c);
            if (clusterVanishBuffer.Count == 0)
                yield break;

            const float dur = 0.28f;
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                float k = Mathf.Clamp01(t / dur);
                // Nảy to (0–30%) rồi co nhỏ dần về 0 + mờ (30–100%): cảm giác "nổ rồi biến mất".
                float scale = k < 0.3f ? Mathf.Lerp(1f, 1.4f, k / 0.3f) : Mathf.Lerp(1.4f, 0.05f, (k - 0.3f) / 0.7f);
                float alpha = k < 0.5f ? 1f : Mathf.Clamp01(1f - (k - 0.5f) / 0.5f);
                for (int i = 0; i < clusterVanishBuffer.Count; i++)
                {
                    var c = clusterVanishBuffer[i];
                    if (c.x < 0 || c.x >= Width || c.y < 0 || c.y >= Height)
                        continue;
                    var img = scenePuzzleCells[c.x, c.y];
                    if (img == null)
                        continue;
                    img.rectTransform.localScale = new Vector3(scale, scale, 1f);
                    var col = img.color; col.a = alpha; img.color = col;
                }
                yield return null;
            }

            // Trả scale về 1 (RedrawLocked ngay sau sẽ đặt lại màu/sprite).
            for (int i = 0; i < clusterVanishBuffer.Count; i++)
            {
                var c = clusterVanishBuffer[i];
                if (c.x < 0 || c.x >= Width || c.y < 0 || c.y >= Height)
                    continue;
                var img = scenePuzzleCells[c.x, c.y];
                if (img != null)
                    img.rectTransform.localScale = Vector3.one;
            }
        }

        // Text "COMBO xN" khi có phản ứng dây chuyền (chain >= 2). Cấp càng cao màu càng nóng.
        void ShowClusterCombo(int chain)
        {
            if (!usingSceneGameplayCanvas || chain < 2)
                return;
            EnsureClusterComboText();
            if (clusterComboText == null)
                return;
            clusterComboText.text = "COMBO x" + chain + "!";
            comboBaseColor = chain >= 4 ? new Color(1f, 0.32f, 0.22f)
                : chain == 3 ? new Color(1f, 0.58f, 0.20f)
                : new Color(1f, 0.86f, 0.35f);
            shake = Mathf.Max(shake, 0.18f);
            if (clusterComboRoutine != null)
                StopCoroutine(clusterComboRoutine);
            clusterComboRoutine = StartCoroutine(AnimateComboText());
        }

        void EnsureClusterComboText()
        {
            if (clusterComboText != null)
                return;
            // Trên canvas overlay riêng (sortingOrder cao) để COMBO luôn nổi trên bàn.
            Transform parent = EnsureFloatFxRoot();
            if (parent == null)
                parent = sceneGameplayCanvas != null ? sceneGameplayCanvas.transform
                    : (safeAreaRoot != null ? (Transform)safeAreaRoot : transform);
            if (parent == null)
                return;
            clusterComboText = Ui.Text(parent, "", RuntimeArt.LoadMenuButtonFont(), 100, new Color(1f, 0.86f, 0.35f), TextAnchor.MiddleCenter);
            clusterComboText.fontStyle = FontStyle.Bold;
            clusterComboText.raycastTarget = false;
            clusterComboText.horizontalOverflow = HorizontalWrapMode.Overflow;
            clusterComboText.verticalOverflow = VerticalWrapMode.Overflow;
            Ui.Rect(clusterComboText, new Vector2(0.5f, 0.56f), new Vector2(0.5f, 0.56f), new Vector2(960, 220));
            AddDarkWoodTextEdge(clusterComboText, 2.2f, 1f);
            AddWarmTitleFinish(clusterComboText, 1.5f);
            var c = clusterComboText.color; c.a = 0f; clusterComboText.color = c;
        }

        IEnumerator AnimateComboText()
        {
            var rt = clusterComboText.rectTransform;
            var baseCol = comboBaseColor; baseCol.a = 1f;
            const float dur = 0.95f;
            for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
            {
                float k = t / dur;
                // Bung lớn (0.4→1.5) rồi ổn định; lắc nhẹ giảm dần cho "đã".
                float scale = k < 0.18f ? Mathf.Lerp(0.4f, 1.5f, k / 0.18f) : Mathf.Lerp(1.5f, 1.08f, Mathf.Clamp01((k - 0.18f) / 0.4f));
                rt.localScale = new Vector3(scale, scale, 1f);
                rt.localEulerAngles = new Vector3(0f, 0f, Mathf.Sin(t * 38f) * (1f - k) * 6f);
                var col = baseCol; col.a = k < 0.65f ? 1f : Mathf.Clamp01(1f - (k - 0.65f) / 0.35f); clusterComboText.color = col;
                yield return null;
            }
            var end = clusterComboText.color; end.a = 0f; clusterComboText.color = end;
            rt.localScale = Vector3.one;
            rt.localEulerAngles = Vector3.zero;
            clusterComboRoutine = null;
        }

        // Màu bóng mờ: rất trong + ám tối theo nền bàn (navy) nên nhìn thoáng qua không nhầm với
        // ô thật. Chỉnh alpha ở đây nếu muốn bóng đậm/nhạt hơn.
        static readonly Color GhostTint = new Color(0.58f, 0.64f, 0.80f, 0.10f);

        void RefreshScenePuzzleBoardUi()
        {
            if (scenePuzzleCells == null)
                return;

            RefreshScenePuzzleCellSizes();

            // Một lần gán cho mỗi ô (trước đây xoá rồi vẽ lại -> mỗi ô có gạch bị dirty 2 lần
            // mỗi khung hình khi kéo, làm canvas rebuild nặng gấp đôi).
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    if (grid[x, y] > 0)
                    {
                        int pieceType = Mathf.Clamp(grid[x, y] - 1, 0, palette.Length - 1);
                        SetScenePuzzleCell(x, y, Color.white, pieceType);
                    }
                    else
                    {
                        SetScenePuzzleCell(x, y, Color.clear, -1);
                    }
                }
            }

            if (!gameOver && !resolving && GhostVisible && !currentPieceIsSpecial)
            {
                var ghostOrigin = origin;
                while (IsValid(ghostOrigin + Vector2Int.down, rotation))
                    ghostOrigin += Vector2Int.down;

                bool clustersGhost = rules.UseResourceClusters && activeResources != null;
                var ghostShape = shapes[currentType];
                for (int i = 0; i < ghostShape.Length; i++)
                {
                    var cell = CellFromLocal(ghostShape[i], ghostOrigin, rotation);
                    if (cell.x < 0 || cell.x >= Width || cell.y < 0 || cell.y >= Height || grid[cell.x, cell.y] != 0)
                        continue;

                    // Chế độ cụm: ghost là sprite tài nguyên MỜ HẲN + tối đi (ám xanh nền bàn) để chỉ
                    // đọc như "vệt bóng", không bị nhìn nhầm thành ô tài nguyên đã đáp.
                    if (clustersGhost && i < activeResources.Length)
                        SetScenePuzzleCell(cell.x, cell.y, GhostTint, CellDisplayType(i, activeResources, activeBlank));
                    else
                        SetScenePuzzleCell(cell.x, cell.y, new Color(1f, 1f, 1f, 0.07f), -1);
                }
            }

            if (!gameOver && !resolving)
            {
                Color activeColor = currentPieceIsSpecial
                    ? (currentSpecialKind == 2 ? new Color(0.35f, 0.95f, 1f, 1f) : RuntimeArt.SpecialBlockColor)
                    : Color.white;

                if (currentPieceIsSpecial)
                {
                    foreach (var cell in Cells(origin, rotation))
                    {
                        if (cell.x >= 0 && cell.x < Width && cell.y >= 0 && cell.y < Height)
                            SetScenePuzzleCell(cell.x, cell.y, activeColor, -1);
                    }
                }
                else
                {
                    bool clusters = rules.UseResourceClusters && activeResources != null;
                    var shape = shapes[currentType];
                    for (int i = 0; i < shape.Length; i++)
                    {
                        var cell = CellFromLocal(shape[i], origin, rotation);
                        if (cell.x < 0 || cell.x >= Width || cell.y < 0 || cell.y >= Height)
                            continue;

                        int type = clusters && i < activeResources.Length ? CellDisplayType(i, activeResources, activeBlank) : currentType;
                        SetScenePuzzleCell(cell.x, cell.y, activeColor, type);
                    }
                }
            }
        }

        void SetScenePuzzleCell(int x, int y, Color color, int type)
        {
            if (scenePuzzleCells == null || x < 0 || x >= Width || y < 0 || y >= Height)
                return;

            var cell = scenePuzzleCells[x, y];
            if (cell == null)
                return;

            bool filled = color.a > 0.01f;
            if (filled)
            {
                // Chế độ cụm: khối tài nguyên LẤP ĐẦY ô (preserveAspect=false) để bằng ô khung.
                bool clusters = rules != null && rules.UseResourceClusters;
                bool clusterResource = clusters && type >= 0 && type < 4;
                bool clusterGarbage = clusters && type >= 4; // ô rác (§16): xám, không mang tài nguyên
                if (clusterGarbage)
                {
                    cell.sprite = blockSprite;
                    cell.preserveAspect = false;
                    cell.color = new Color(0.40f, 0.43f, 0.50f, 1f);
                }
                else
                {
                    cell.sprite = GetPieceBlockSprite(type);
                    cell.preserveAspect = !clusterResource;
                    cell.color = type < 0 ? PuzzleBlockColor(color) : color;
                }
            }
            else
            {
                // Ô trống = tile navy nhạt (thấy rõ từng ô để di chuyển mô hình); khe hở lộ nền
                // đậm = đường lưới.
                cell.sprite = null;
                cell.preserveAspect = false;
                cell.color = new Color(0.10f, 0.17f, 0.31f, 1f);
            }
            cell.enabled = true;
        }

        Sprite GetPieceBlockSprite(int type)
        {
            // GD v3: khi bật cụm, "type" là chỉ số ResourceType (0..3) → sprite ô tài nguyên.
            if (rules != null && rules.UseResourceClusters && resourceSprites != null)
            {
                if (type >= 0 && type < resourceSprites.Length && resourceSprites[type] != null)
                    return resourceSprites[type];
                return blockSprite;
            }

            if (type >= 0 && pieceBlockSprites != null && pieceBlockSprites.Length > 0)
            {
                var sprite = pieceBlockSprites[Mathf.Abs(type) % pieceBlockSprites.Length];
                if (sprite != null)
                    return sprite;
            }

            return blockSprite;
        }

        Color PuzzleBlockColor(Color color)
        {
            if (color.a <= 0.01f)
                return color;

            Color boosted = Color.Lerp(color, Color.white, 0.10f);
            boosted.r = Mathf.Clamp01(boosted.r * 1.08f);
            boosted.g = Mathf.Clamp01(boosted.g * 1.08f);
            boosted.b = Mathf.Clamp01(boosted.b * 1.08f);
            boosted.a = color.a;
            return boosted;
        }

        bool IsValid(Vector2Int testOrigin, int testRotation)
        {
            foreach (var cell in Cells(testOrigin, testRotation))
            {
                if (cell.x < 0 || cell.x >= Width || cell.y < 0)
                    return false;

                if (cell.y < Height && grid[cell.x, cell.y] > 0)
                    return false;
            }
            return true;
        }

        void DrawActive()
        {
            ClearActive();
            if (usingSceneGameplayCanvas)
            {
                RefreshScenePuzzleBoardUi();
                return;
            }

            if (currentPieceIsSpecial)
            {
                Color specialColor = currentSpecialKind == 2 ? new Color(0.35f, 0.95f, 1f, 1f) : RuntimeArt.SpecialBlockColor;
                var bomb = NewBlock(currentSpecialKind == 2 ? "Active Grand Bomb" : "Active Bomb", specialColor, activeRoot);
                bomb.transform.position = CellToWorld(origin.x, origin.y);
                bomb.transform.localScale = Vector3.one;
                activeBlocks.Add(bomb);
                DrawGhost();
                RefreshScenePuzzleBoardUi();
                return;
            }

            bool clusterActive = rules.UseResourceClusters && activeResources != null;
            var activeShape = shapes[currentType];
            for (int i = 0; i < activeShape.Length; i++)
            {
                var cell = CellFromLocal(activeShape[i], origin, rotation);
                int blockType = clusterActive && i < activeResources.Length ? CellDisplayType(i, activeResources, activeBlank) : currentType;
                var block = NewPieceBlock("Active Block", blockType, activeRoot);
                block.transform.position = CellToWorld(cell.x, cell.y);
                activeBlocks.Add(block);
            }

            DrawGhost();
            RefreshScenePuzzleBoardUi();
        }

        void DrawGhost()
        {
            foreach (var block in ghostBlocks)
                Destroy(block);
            ghostBlocks.Clear();

            if (usingSceneGameplayCanvas || !GhostVisible)
                return;

            var ghostOrigin = origin;
            while (IsValid(ghostOrigin + Vector2Int.down, rotation))
                ghostOrigin += Vector2Int.down;

            foreach (var cell in Cells(ghostOrigin, rotation))
            {
                var block = NewBlock("Ghost Block", new Color(1f, 1f, 1f, 0.08f), ghostRoot);
                block.transform.position = CellToWorld(cell.x, cell.y);
                block.transform.localScale = Vector3.one * 0.86f;
                block.GetComponent<SpriteRenderer>().sortingOrder = 5;
                ghostBlocks.Add(block);
            }
        }

        void ClearActive()
        {
            foreach (var block in activeBlocks)
                Destroy(block);
            activeBlocks.Clear();

            foreach (var block in ghostBlocks)
                Destroy(block);
            ghostBlocks.Clear();
        }

        GameObject NewBlock(string name, Color color, Transform parent)
        {
            var block = new GameObject(name);
            block.transform.SetParent(parent);
            block.transform.localScale = Vector3.one * 0.9f;
            var renderer = block.AddComponent<SpriteRenderer>();
            renderer.sprite = blockSprite;
            renderer.color = color;
            renderer.sortingOrder = 10;
            return block;
        }

        GameObject NewPieceBlock(string name, int type, Transform parent)
        {
            var block = new GameObject(name);
            block.transform.SetParent(parent);
            block.transform.localScale = Vector3.one * 0.82f;
            var renderer = block.AddComponent<SpriteRenderer>();
            renderer.sprite = GetPieceBlockSprite(type);
            renderer.color = Color.white;
            renderer.sortingOrder = 10;
            return block;
        }

        Vector3 CellToWorld(int x, int y)
        {
            return new Vector3(x - Width * 0.5f + 0.5f, y - Height * 0.5f + 0.5f, 0);
        }

        float CurrentFallInterval()
        {
            float interval = rules.FallInterval;
            if (rules.MaxFallSpeedMultiplier > 1f && rules.SpeedRampSeconds > 0f)
            {
                float ramp = Mathf.Clamp01(gameplayTime / rules.SpeedRampSeconds);
                float smoothRamp = Mathf.SmoothStep(0f, 1f, ramp);
                float multiplier = Mathf.Lerp(1f, rules.MaxFallSpeedMultiplier, smoothRamp);
                interval = rules.FallInterval / multiplier;
            }
            else
            {
                float speedUp = Mathf.Clamp(journeyLevel / 18f, 0f, 0.24f);
                interval = rules.FallInterval - speedUp;
            }

            if (rules.FastBlocks && (piecesLocked + currentType) % 5 == 0)
                interval *= 0.68f;

            if (currentPieceIsSpecial)
                interval *= 1.8f; // special block falls slower

            return Mathf.Max(0.08f, interval);
        }

        void RenderPiecePreview(List<Image> cells, int type, bool visible, Puzzle.ResourceType[] resources = null, bool[] blank = null)
        {
            bool clusters = rules != null && rules.UseResourceClusters && resourceSprites != null;
            for (int i = 0; i < cells.Count; i++)
            {
                cells[i].color = new Color(1f, 1f, 1f, 0f);
                cells[i].sprite = GetPieceBlockSprite(type);
            }

            if (!visible || type < 0)
                return;

            var shape = shapes[type];
            int minX = shape[0].x;
            int maxX = shape[0].x;
            int minY = shape[0].y;
            int maxY = shape[0].y;
            foreach (var cell in shape)
            {
                minX = Mathf.Min(minX, cell.x);
                maxX = Mathf.Max(maxX, cell.x);
                minY = Mathf.Min(minY, cell.y);
                maxY = Mathf.Max(maxY, cell.y);
            }

            float shapeCenterX = (minX + maxX) * 0.5f;
            float shapeCenterY = (minY + maxY) * 0.5f;
            float cellSize = cells.Count > 0 ? cells[0].rectTransform.sizeDelta.x : 12.5f;
            float step = PreviewCellStep(cellSize);
            for (int i = 0; i < shape.Length && i < cells.Count; i++)
            {
                var cell = shape[i];
                var image = cells[i];
                image.rectTransform.anchoredPosition = new Vector2((cell.x - shapeCenterX) * step, -(cell.y - shapeCenterY) * step);
                // Chế độ cụm: mỗi ô một sprite tài nguyên (khớp mô hình sắp rơi), không map theo shape id.
                bool isBlank = clusters && blank != null && i < blank.Length && blank[i];
                if (isBlank)
                {
                    image.sprite = blockSprite;
                    image.color = new Color(0.40f, 0.43f, 0.50f, 1f); // ô trống = xám như rác
                }
                else
                {
                    image.sprite = clusters && resources != null && i < resources.Length
                        ? GetPieceBlockSprite((int)resources[i])
                        : GetPieceBlockSprite(type);
                    image.color = Color.white;
                }
                image.preserveAspect = !clusters; // chế độ cụm: khối lấp đầy ô như bàn chính
            }
        }

        int PeekNext(int offset)
        {
            if (nextBag.Count <= offset)
                FillBag();
            return new List<int>(nextBag)[offset];
        }
    }
}
