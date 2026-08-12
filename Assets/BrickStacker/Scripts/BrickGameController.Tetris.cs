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
            if (rules.AllowSpecialBlocks && piecesLocked > 4)
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

            if (!IsValid(origin + delta, rotation))
                return;

            origin += delta;
            movedHorizontallyThisFrame = true;
            fallTimer = 0f;
            lockDelayTimer = 0f;
            DrawActive();
            Beep(520f, 0.025f, 0.08f);
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

        void RotateFromButton()
        {
            if (paused || resolving || gameOver || puzzlePausedForTacticalTurn)
                return;

            TryRotate(1);
        }

        void SwapHoldPiece()
        {
            if (paused || resolving || gameOver || puzzlePausedForTacticalTurn || !canHold || currentPieceIsSpecial)
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
                foreach (var localCell in shapes[currentType])
                {
                    var cell = CellFromLocal(localCell, origin, rotation);
                    if (cell.x < 0 || cell.x >= Width || cell.y < 0 || cell.y >= Height)
                        continue;

                    grid[cell.x, cell.y] = currentType + 1;
                    if (!usingSceneGameplayCanvas)
                    {
                        var block = NewPieceBlock("Locked Block", currentType, settledRoot);
                        block.transform.position = CellToWorld(cell.x, cell.y);
                        lockedBlocks[cell.x, cell.y] = block;
                    }
                }
            }

            piecesLocked++;
            currentPieceIsSpecial = false;
            ClearActive();
            int specialKind = currentSpecialKind;
            currentSpecialKind = 0;
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

            // Trận 1v1 (design §6.4): xóa hàng nạp NĂNG LƯỢNG (1/3/5/8 + combo).
            // Người chơi chủ động tiêu năng lượng cho Đánh / Khiên / Rác.
            if (MultiplayerMatch.Active)
            {
                int gained = energySystem.GainFromLines(clearCount, combo);
                RefreshSkillBar();
                if (gained > 0 && tacticalBoard != null)
                {
                    tacticalBoard.LastMessage = "+" + gained + " năng lượng (" + energySystem.Energy + "/" + energySystem.Max + ")";
                    RefreshTacticalBoardUi();
                }
            }

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
            for (int x = 0; x < Width; x++)
                grid[x, 0] = x == hole ? 0 : UnityEngine.Random.Range(1, 8);

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

        void RefreshScenePuzzleBoardUi()
        {
            if (scenePuzzleCells == null)
                return;

            RefreshScenePuzzleCellSizes();

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    SetScenePuzzleCell(x, y, Color.clear, -1);
                    if (grid[x, y] > 0)
                    {
                        int pieceType = Mathf.Clamp(grid[x, y] - 1, 0, palette.Length - 1);
                        SetScenePuzzleCell(x, y, Color.white, pieceType);
                    }
                }
            }

            if (!gameOver && !resolving && GhostVisible && !currentPieceIsSpecial)
            {
                var ghostOrigin = origin;
                while (IsValid(ghostOrigin + Vector2Int.down, rotation))
                    ghostOrigin += Vector2Int.down;

                foreach (var cell in Cells(ghostOrigin, rotation))
                {
                    if (cell.x >= 0 && cell.x < Width && cell.y >= 0 && cell.y < Height && grid[cell.x, cell.y] == 0)
                        SetScenePuzzleCell(cell.x, cell.y, new Color(1f, 1f, 1f, 0.22f), -1);
                }
            }

            if (!gameOver && !resolving)
            {
                Color activeColor = currentPieceIsSpecial
                    ? (currentSpecialKind == 2 ? new Color(0.35f, 0.95f, 1f, 1f) : RuntimeArt.SpecialBlockColor)
                    : Color.white;

                foreach (var cell in Cells(origin, rotation))
                {
                    if (cell.x >= 0 && cell.x < Width && cell.y >= 0 && cell.y < Height)
                        SetScenePuzzleCell(cell.x, cell.y, activeColor, currentPieceIsSpecial ? -1 : currentType);
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
                cell.sprite = GetPieceBlockSprite(type);
                cell.preserveAspect = true;
                cell.color = type < 0 ? PuzzleBlockColor(color) : color;
            }
            else
            {
                // Ô trống = màu board của khung (khe hở lộ nền navy nhạt = đường lưới).
                cell.sprite = null;
                cell.preserveAspect = false;
                cell.color = new Color(0.063f, 0.153f, 0.30f, 1f);
            }
            cell.enabled = true;
        }

        Sprite GetPieceBlockSprite(int type)
        {
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

            foreach (var localCell in shapes[currentType])
            {
                var cell = CellFromLocal(localCell, origin, rotation);
                var block = NewPieceBlock("Active Block", currentType, activeRoot);
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
                var block = NewBlock("Ghost Block", new Color(1f, 1f, 1f, 0.22f), ghostRoot);
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

        void RenderPiecePreview(List<Image> cells, int type, bool visible)
        {
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
                image.sprite = GetPieceBlockSprite(type);
                image.color = Color.white;
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
