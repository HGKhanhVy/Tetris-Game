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
        void SetupTacticalBoard()
        {
            if (rules.TacticalData == null)
                rules.TacticalData = TacticalLevelData.Create(Mathf.Max(1, journeyLevel));
            tacticalBoard = new TacticalBoardManager(rules.TacticalData);
        }

        void BindSceneTacticalCells(Transform root)
        {
            tacticalCellButtons.Clear();
            tacticalCellLabels.Clear();
            tacticalCellIcons.Clear();
            if (tacticalBoard == null || root == null)
                return;

            int width = tacticalBoard.Data.BoardWidth;
            int height = tacticalBoard.Data.BoardHeight;
            int needed = width * height;
            var rects = new List<RectTransform>();
            var allRects = root.GetComponentsInChildren<RectTransform>(true);
            for (int i = 0; i < allRects.Length; i++)
            {
                string name = NormalizeObjectName(allRects[i].name);
                if (name == "tacticalgrid" || name.StartsWith("tacticalgrid("))
                    rects.Add(allRects[i]);
            }

            if (rects.Count < needed)
                return;

            rects.Sort((a, b) =>
            {
                float yDelta = a.anchoredPosition.y - b.anchoredPosition.y;
                if (Mathf.Abs(yDelta) > 2f)
                    return yDelta < 0f ? -1 : 1;
                float xDelta = a.anchoredPosition.x - b.anchoredPosition.x;
                if (Mathf.Abs(xDelta) <= 2f)
                    return 0;
                return xDelta < 0f ? -1 : 1;
            });

            for (int i = 0; i < needed; i++)
            {
                int x = i % width;
                int y = i / width;
                SetupSceneTacticalCell(rects[i], x, y);
            }
        }

        void BuildSceneTacticalGridIfNeeded(Transform root)
        {
            if (tacticalBoard == null || root == null || tacticalCellButtons.Count >= tacticalBoard.Data.BoardWidth * tacticalBoard.Data.BoardHeight)
                return;

            Transform boardTransform = FindChildLoose(root, "TacticalBoard");
            sceneTacticalBoardRect = boardTransform != null ? boardTransform.GetComponent<RectTransform>() : null;
            if (sceneTacticalBoardRect == null)
                return;

            int width = tacticalBoard.Data.BoardWidth;
            int height = tacticalBoard.Data.BoardHeight;
            tacticalCellButtons.Clear();
            tacticalCellLabels.Clear();
            tacticalCellIcons.Clear();
            ClearRuntimeChild(sceneTacticalBoardRect, "Runtime Tactical Grid");

            var gridRoot = new GameObject("Runtime Tactical Grid", typeof(RectTransform));
            gridRoot.transform.SetParent(sceneTacticalBoardRect, false);
            var gridRect = gridRoot.GetComponent<RectTransform>();
            // Neo cửa sổ TRONG viền khung frame-banco (căn theo ảnh render + phản hồi thực tế):
            // trái/phải/trên khít; minY tinh chỉnh cho mép dưới không lấn ra viền, không hụt.
            Ui.Rect(gridRoot, new Vector2(0.034f, 0.027f), new Vector2(0.967f, 0.968f), Vector2.zero);
            gridRect.SetAsLastSibling();

            // Nền = màu ĐƯỜNG KẺ của khung (navy đậm), khe hở giữa ô lộ ra thành lưới —
            // trùng màu lưới khung nên liền mạch.
            var gridBg = Ui.Panel(gridRoot.transform, "Tactical Grid BG", new Color(0.0f, 0.055f, 0.33f, 1f));
            Ui.Stretch(gridBg);
            gridBg.GetComponent<Image>().raycastTarget = false;
            // Canvas con: 64 nút bàn cờ chỉ rebuild khi có nước đi, không bị kéo theo
            // mỗi lần khối gạch nhích (và ngược lại). Cần raycaster riêng cho nút.
            MakeIsolatedCanvas(gridRoot, true);

            // Khe hở ĐỀU nhau ở cả mép ngoài lẫn giữa các ô: N ô + (N+1) khe bằng nhau.
            // gridRoot ~vuông nên khe theo pixel đều cả 2 trục.
            const float gap = 0.006f;
            float cellW = (1f - (width + 1) * gap) / width;
            float cellH = (1f - (height + 1) * gap) / height;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var cell = Ui.Panel(gridRoot.transform, "TacticalGrid (" + (y * width + x) + ")", Color.white);
                    float minX = gap + x * (cellW + gap);
                    float minY = gap + y * (cellH + gap);
                    Ui.Rect(cell, new Vector2(minX, minY), new Vector2(minX + cellW, minY + cellH), Vector2.zero);
                    SetupSceneTacticalCell(cell.GetComponent<RectTransform>(), x, y);
                }
            }
        }

        void SetupSceneTacticalCell(RectTransform rect, int x, int y)
        {
            var image = rect.GetComponent<Image>();
            if (image == null)
                image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = true;

            var button = rect.GetComponent<Button>();
            if (button == null)
                button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.onClick.RemoveAllListeners();

            var input = rect.GetComponent<TacticalCellInput>();
            if (input == null)
                input = rect.gameObject.AddComponent<TacticalCellInput>();
            input.Controller = this;
            input.X = x;
            input.Y = y;

            var label = rect.GetComponentInChildren<Text>(true);
            if (label == null)
            {
                label = Ui.Text(rect, "", font, 18, new Color(1f, 0.95f, 0.78f), TextAnchor.MiddleCenter);
                Ui.Stretch(label.gameObject);
            }
            label.gameObject.SetActive(false);

            var iconTransform = FindChildLoose(rect, "Tactical Piece Icon");
            Image icon = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
            if (icon == null)
                icon = Ui.Panel(rect, "Tactical Piece Icon", new Color(1f, 1f, 1f, 0f)).GetComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            var iconRt = icon.GetComponent<RectTransform>();
            iconRt.anchorMin = Vector2.zero;
            iconRt.anchorMax = Vector2.one;
            iconRt.pivot     = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = Vector2.zero;
            iconRt.sizeDelta = Vector2.zero;
            iconRt.localScale = new Vector3(1.18f, 1.18f, 1f);

            tacticalCellButtons.Add(button);
            tacticalCellLabels.Add(label);
            tacticalCellIcons.Add(icon);
        }

        void BuildTacticalBoardUi(Transform parent)
        {
            if (tacticalBoard == null)
                return;

            var shadow = Ui.Panel(parent, "Tactical Board Shadow", new Color(0.035f, 0.012f, 0.004f, 0.58f));
            tacticalWidgetShadowRect = shadow.GetComponent<RectTransform>();
            Ui.Rect(shadow, new Vector2(0.790f, 0.055f), new Vector2(0.960f, 0.245f), new Vector2(0, -5));

            var widget = Ui.Panel(parent, "Tactical Board Widget", Color.white);
            tacticalWidgetRect = widget.GetComponent<RectTransform>();
            Ui.Rect(widget, new Vector2(0.790f, 0.062f), new Vector2(0.960f, 0.252f), new Vector2(0, 0));
            var widgetImage = widget.GetComponent<Image>();
            widgetImage.sprite = RuntimeArt.CreateWoodPanelSprite();
            widgetImage.type = Image.Type.Sliced;
            widgetImage.color = new Color(0.54f, 0.31f, 0.14f, 0.98f);

            var title = Ui.Text(widget.transform, "Bàn chiến thuật", font, 18, new Color(1f, 0.91f, 0.68f), TextAnchor.MiddleCenter);
            Ui.Rect(title, new Vector2(0.05f, 0.900f), new Vector2(0.95f, 0.990f), new Vector2(0, 0));
            title.fontStyle = FontStyle.Bold;
            title.text = "Xóa dòng để nhận lượt đi";
            AddDarkWoodTextEdge(title, 0.72f, 0.78f);
            title.gameObject.SetActive(false);

            tacticalMovesText = Ui.Text(widget.transform, "Lượt: 0", font, 16, new Color(1f, 0.82f, 0.46f), TextAnchor.MiddleCenter);
            Ui.Rect(tacticalMovesText, new Vector2(0.05f, 0.820f), new Vector2(0.95f, 0.900f), new Vector2(0, 0));
            tacticalMovesText.gameObject.SetActive(false);
            AddDarkWoodTextEdge(tacticalMovesText, 0.55f, 0.70f);

            var gridPanel = Ui.Panel(widget.transform, "Tactical Grid", new Color(0.35f, 0.18f, 0.07f, 0.96f));
            Ui.Rect(gridPanel, new Vector2(0.045f, 0.055f), new Vector2(0.955f, 0.945f), new Vector2(0, 0));

            tacticalCellButtons.Clear();
            tacticalCellLabels.Clear();
            tacticalCellIcons.Clear();
            int width = tacticalBoard.Data.BoardWidth;
            int height = tacticalBoard.Data.BoardHeight;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int cellX = x;
                    int cellY = y;
                    var button = Ui.Button(gridPanel.transform, "", font, 12, () => { });
                    float minX = x / (float)width;
                    float maxX = (x + 1) / (float)width;
                    float minY = y / (float)height;
                    float maxY = (y + 1) / (float)height;
                    Ui.Rect(button.gameObject, new Vector2(minX + 0.010f, minY + 0.010f), new Vector2(maxX - 0.010f, maxY - 0.010f), new Vector2(0, 0));
                    var cellInput = button.gameObject.AddComponent<TacticalCellInput>();
                    cellInput.Controller = this;
                    cellInput.X = cellX;
                    cellInput.Y = cellY;

                    var label = button.GetComponentInChildren<Text>();
                    label.fontSize = 24;
                    label.fontStyle = FontStyle.Bold;
                    label.gameObject.SetActive(false);
                    var icon = Ui.Panel(button.transform, "Tactical Piece Icon", new Color(1f, 1f, 1f, 0f)).GetComponent<Image>();
                    icon.raycastTarget = false;
                    icon.preserveAspect = true;
                    var iconRect = icon.GetComponent<RectTransform>();
                    iconRect.anchorMin = Vector2.zero;
                    iconRect.anchorMax = Vector2.one;
                    iconRect.pivot     = new Vector2(0.5f, 0.5f);
                    iconRect.anchoredPosition = Vector2.zero;
                    iconRect.sizeDelta = Vector2.zero;
                    iconRect.localScale = new Vector3(1.18f, 1.18f, 1f);
                    tacticalCellButtons.Add(button);
                    tacticalCellLabels.Add(label);
                    tacticalCellIcons.Add(icon);
                }
            }

            tacticalStatusText = Ui.Text(widget.transform, "Chạm ô cạnh quân xanh để đi.", font, 13, new Color(1f, 0.88f, 0.64f), TextAnchor.MiddleCenter);
            tacticalStatusText.text = "Chạm quân xanh, chọn ô sáng hoặc kéo.";
            Ui.Rect(tacticalStatusText, new Vector2(0.05f, 0.025f), new Vector2(0.95f, 0.155f), new Vector2(0, 0));
            tacticalStatusText.gameObject.SetActive(false);
            AddDarkWoodTextEdge(tacticalStatusText, 0.42f, 0.62f);
            RefreshTacticalBoardUi();
        }

        void OnTacticalCellTapped(int x, int y)
        {
            if (paused || resolving || gameOver || tacticalBoard == null || tacticalBoard.Status != TacticalBoardStatus.Running)
                return;
            if (TutorialBlocksTactical())
                return;

            var target = new Vector2Int(x, y);
            if (target == tacticalBoard.PlayerPosition)
            {
                SelectTacticalPiece();
                return;
            }

            if (!tacticalPieceSelected)
            {
                tacticalBoard.LastMessage = tacticalBoard.MoveBank <= 0 ? "Cần lượt đi để di chuyển." : "Chọn quân xanh trước, rồi chọn ô sáng để đi.";
                RefreshTacticalBoardUi();
                return;
            }

            TryMoveTacticalPlayerTo(target);
            if (target.x < -9999)
            {

            var delta = target - tacticalBoard.PlayerPosition;
            if (Mathf.Abs(delta.x) + Mathf.Abs(delta.y) != 1)
            {
                tacticalBoard.LastMessage = "Chỉ đi được 1 ô theo 4 hướng.";
                RefreshTacticalBoardUi();
                return;
            }

            var result = tacticalBoard.MovePlayer(delta);
            RuntimeArt.PlayUiSwitchSound();
            RefreshTacticalBoardUi();
            UpdateUi();

            if (result == TacticalBoardStatus.Won)
                LevelComplete();
            else if (result == TacticalBoardStatus.Failed)
                EndGame(false);
            }
        }

        public void HandleTacticalPointerDown(int x, int y, Vector2 screenPosition)
        {
            if (paused || resolving || gameOver || tacticalBoard == null || tacticalBoard.Status != TacticalBoardStatus.Running)
                return;
            if (TutorialBlocksTactical())
                return;

            var cell = new Vector2Int(x, y);
            if (cell == tacticalBoard.PlayerPosition)
            {
                tacticalDragTracking = true;
                tacticalDragStart = screenPosition;
                SelectTacticalPiece();
            }
        }

        public void HandleTacticalPointerUp(int x, int y, Vector2 screenPosition)
        {
            if (paused || resolving || gameOver || tacticalBoard == null || tacticalBoard.Status != TacticalBoardStatus.Running)
                return;
            if (TutorialBlocksTactical())
                return;

            var cell = new Vector2Int(x, y);
            if (tacticalDragTracking && cell == tacticalBoard.PlayerPosition)
            {
                Vector2 delta = screenPosition - tacticalDragStart;
                float threshold = Mathf.Min(Screen.width, Screen.height) * 0.035f;
                if (delta.magnitude >= threshold)
                {
                    Vector2Int direction = Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
                        ? (delta.x > 0 ? Vector2Int.right : Vector2Int.left)
                        : (delta.y > 0 ? Vector2Int.up : Vector2Int.down);
                    TryMoveTacticalPlayerTo(tacticalBoard.PlayerPosition + direction);
                    tacticalDragTracking = false;
                    return;
                }
            }

            tacticalDragTracking = false;
            OnTacticalCellTapped(x, y);
        }

        void SelectTacticalPiece()
        {
            if (tacticalBoard == null)
                return;

            if (tacticalBoard.MoveBank <= 0)
            {
                tacticalPieceSelected = false;
                tacticalBoard.LastMessage = "Hết lượt đi! Xếp gạch tạo cụm Giày để có thêm lượt.";
                SpawnTacticalFloat("HẾT LƯỢT!", new Color(1f, 0.45f, 0.4f), tacticalBoard.PlayerPosition, 60f, 1f);
                Beep(180f, 0.12f, 0.25f);
                RefreshTacticalBoardUi();
                return;
            }

            PausePuzzle();
            tacticalPieceSelected = true;
            tacticalBoard.LastMessage = "Chọn ô sáng hoặc kéo quân xanh.";
            RefreshTacticalBoardUi();
        }

        void TryMoveTacticalPlayerTo(Vector2Int target)
        {
            if (tacticalBoard == null)
                return;

            if (!tacticalBoard.IsPlayerMoveTarget(target))
            {
                tacticalBoard.LastMessage = tacticalBoard.MoveBank <= 0 ? "Cần lượt đi để di chuyển." : "Ô đó không hợp lệ.";
                tacticalPieceSelected = false;
                tacticalDragTracking = false;
                ResumePuzzle();
                RefreshTacticalBoardUi();
                return;
            }

            var result = tacticalBoard.MovePlayer(target - tacticalBoard.PlayerPosition);
            tacticalPieceSelected = false;
            tacticalDragTracking = false;
            tutorialPlayerMoved = true; // mốc tutorial: đã đi quân trên bàn cờ lần đầu
            TutorialNotify(TutorialAction.TacticalMove);
            RuntimeArt.PlayUiSwitchSound();

            if (result == TacticalBoardStatus.Won)
            {
                RefreshTacticalBoardUi();
                UpdateUi();
                LevelComplete();
                return;
            }
            if (result == TacticalBoardStatus.Failed)
            {
                RefreshTacticalBoardUi();
                UpdateUi();
                EndGame(false);
                return;
            }

            // Còn lượt: giữ khối gạch đứng yên và chọn sẵn quân để đi tiếp liền mạch.
            if (tacticalBoard.MoveBank > 0)
            {
                tacticalPieceSelected = true;
                PausePuzzle();
            }
            else
            {
                StartCoroutine(ResumeAfterDelay(1.0f));
            }
            RefreshTacticalBoardUi();
            UpdateUi();
        }

        IEnumerator ResumeAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            ResumePuzzle();
        }

        void PausePuzzle()
        {
            if (gameOver || paused || resolving)
                return;

            puzzlePausedForTacticalTurn = true;
            fallTimer = 0f;
        }

        void ResumePuzzle()
        {
            puzzlePausedForTacticalTurn = false;
            fallTimer = 0f;
        }

        Sprite GetObstacleSprite(Vector2Int cell)
        {
            if (obstacleBlueSprite == null)
                obstacleBlueSprite = RuntimeArt.LoadV3SubSprite("screen-gameplay/chuongngaivat-2.png", new Rect(0.354f, 0.316f, 0.292f, 0.445f));
            if (obstacleBushSprite == null)
                obstacleBushSprite = RuntimeArt.LoadV3SubSprite("screen-gameplay/chuongngaivat-1.png", new Rect(0.297f, 0.219f, 0.445f, 0.613f));
            return (cell.x + cell.y) % 2 == 0 ? obstacleBlueSprite : obstacleBushSprite;
        }

        void RefreshTacticalBoardUi()
        {
            if (tacticalBoard == null || tacticalCellButtons.Count == 0)
                return;

            // Cache Image một lần — GetComponent 64 lần mỗi refresh là lãng phí.
            if (tacticalCellImageCache.Count != tacticalCellButtons.Count)
            {
                tacticalCellImageCache.Clear();
                for (int i = 0; i < tacticalCellButtons.Count; i++)
                    tacticalCellImageCache.Add(tacticalCellButtons[i].GetComponent<Image>());
            }

            int width = tacticalBoard.Data.BoardWidth;
            int height = tacticalBoard.Data.BoardHeight;
            bool canMove = tacticalBoard.Status == TacticalBoardStatus.Running && tacticalBoard.MoveBank > 0;

            // Đường đi dự kiến của quái (design §2.7) — vẽ chấm cảnh báo lên ô trống.
            var monsterPath = tacticalBoard.Status == TacticalBoardStatus.Running
                ? tacticalBoard.GetMonsterPathPreview(3)
                : null;
            Vector2Int monsterNext = tacticalBoard.NextMonsterStep;
            monsterNextCellIndex = -1;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    if (index < 0 || index >= tacticalCellButtons.Count)
                        continue;

                    var cell = new Vector2Int(x, y);
                    var button = tacticalCellButtons[index];
                    var image = tacticalCellImageCache[index];
                    var label = tacticalCellLabels[index];
                    var icon = index < tacticalCellIcons.Count ? tacticalCellIcons[index] : null;

                    // Ô game 10×10 màu XANH khớp ô của khung (135,181,246); khe hở lộ nền navy
                    // = đường lưới → nhìn y hệt lưới khung, quân khớp tuyệt đối với ô game.
                    image.sprite = null;
                    image.type = Image.Type.Simple;
                    image.preserveAspect = false;
                    image.color = new Color(0.53f, 0.71f, 0.965f, 1f);
                    image.raycastTarget = true;
                    label.text = "";
                    label.color = new Color(1f, 0.95f, 0.78f);
                    if (icon != null)
                    {
                        icon.sprite = null;
                        icon.color = Color.clear;
                        var ir = icon.rectTransform;
                        ir.anchorMin = Vector2.zero;
                        ir.anchorMax = Vector2.one;
                        ir.pivot = new Vector2(0.5f, 0.5f);
                        ir.anchoredPosition = Vector2.zero;
                        ir.sizeDelta = Vector2.zero;
                        // Cells are wider than tall; keep the character sprite proportional
                        // instead of stretching it flat, and scale it up slightly so it
                        // still reads large inside the cell.
                        ir.localScale = new Vector3(1.18f, 1.18f, 1f);
                        icon.preserveAspect = true;
                    }
                    bool legalTarget = tacticalPieceSelected && tacticalBoard.IsPlayerMoveTarget(cell);

                    if (tacticalBoard.IsWall(cell))
                    {
                        // Chướng ngại vật: khối xanh / bụi xanh lá (chuongngaivat) trên ô xanh.
                        if (icon != null)
                        {
                            icon.sprite = GetObstacleSprite(cell);
                            icon.color = Color.white;
                        }
                    }
                    else if (cell == tacticalBoard.PlayerPosition)
                    {
                        image.color = new Color(0.21f, 0.72f, 0.29f, 0.98f);
                        if (icon != null)
                        {
                            icon.sprite = RuntimeArt.CreateTacticalPlayerSprite();
                            icon.color = Color.white;
                        }
                        label.text = "●";
                        label.color = new Color(0.70f, 1f, 0.68f);
                    }
                    // Quái vẽ TRƯỚC enemy: khi quái bước vào ô enemy (thắng), enemy biến mất
                    // ngay trong khung hình cuối thay vì trông như còn sống.
                    else if (cell == tacticalBoard.MonsterPosition)
                    {
                        image.color = new Color(0.48f, 0.21f, 0.85f, 0.98f);
                        if (icon != null)
                        {
                            icon.sprite = RuntimeArt.CreateTacticalMonsterSprite();
                            icon.color = Color.white;
                        }
                        label.text = "◆";
                        label.color = new Color(1f, 0.66f, 0.76f);
                    }
                    else if (cell == tacticalBoard.EnemyPosition)
                    {
                        image.color = new Color(0.85f, 0.22f, 0.18f, 0.98f);
                        if (icon != null)
                        {
                            icon.sprite = RuntimeArt.CreateTacticalEnemySprite();
                            icon.color = Color.white;
                        }
                        label.text = "●";
                        label.color = new Color(1f, 0.54f, 0.44f);
                    }
                    // Địa hình (design §3) — render tạm bằng màu + nhãn; polish sprite ở đợt giao diện.
                    else if (tacticalBoard.IsClosedDoor(cell))
                    {
                        image.color = new Color(0.55f, 0.28f, 0.42f, 0.98f);
                        label.text = "D";
                        label.color = new Color(1f, 0.86f, 0.72f);
                    }
                    else if (tacticalBoard.IsBox(cell))
                    {
                        image.sprite = RuntimeArt.CreateTacticalWallSprite();
                        image.color = new Color(0.68f, 0.50f, 0.24f, 0.98f);
                        label.text = "T";
                        label.color = new Color(0.35f, 0.22f, 0.08f);
                    }
                    else if (tacticalBoard.IsTrap(cell))
                    {
                        image.color = new Color(0.42f, 0.20f, 0.20f, 0.98f);
                        label.text = "!";
                        label.color = new Color(1f, 0.62f, 0.34f);
                    }
                    else if (tacticalBoard.IsIce(cell))
                    {
                        image.color = new Color(0.42f, 0.66f, 0.82f, 0.95f);
                        label.text = "~";
                        label.color = new Color(0.92f, 0.98f, 1f);
                    }
                    else if (tacticalBoard.IsPortal(cell))
                    {
                        image.color = new Color(0.30f, 0.52f, 0.72f, 0.98f);
                        label.text = "O";
                        label.color = new Color(0.80f, 0.94f, 1f);
                    }
                    else if (tacticalBoard.IsSwitch(cell))
                    {
                        image.color = new Color(0.30f, 0.55f, 0.34f, 0.98f);
                        label.text = "S";
                        label.color = new Color(0.85f, 1f, 0.80f);
                    }

                    // Vẽ đường quái sắp đi lên ô trống (không đè ô nhân vật/tường).
                    bool plainCell = !tacticalBoard.IsWall(cell)
                        && cell != tacticalBoard.PlayerPosition
                        && cell != tacticalBoard.EnemyPosition
                        && cell != tacticalBoard.MonsterPosition;
                    if (plainCell && monsterPath != null && monsterPath.Contains(cell))
                    {
                        if (cell == monsterNext)
                        {
                            monsterNextCellIndex = index;
                            image.color = MonsterNextCellColor();
                        }
                        else
                        {
                            // Các ô xa hơn trên đường: tô nhạt dần.
                            image.color = new Color(0.66f, 0.34f, 0.42f, 0.98f);
                        }
                    }

                    if (legalTarget && !tacticalBoard.IsWall(cell) && cell != tacticalBoard.PlayerPosition && cell != tacticalBoard.EnemyPosition && cell != tacticalBoard.MonsterPosition)
                    {
                        image.sprite = RuntimeArt.CreateTacticalHighlightSprite();
                        image.color = new Color(1f, 0.85f, 0.24f, 0.98f);
                        label.text = "+";
                        label.color = new Color(0.28f, 0.10f, 0.03f);
                    }

                    if (cell == tacticalBoard.PlayerPosition && canMove)
                        image.color = tacticalPieceSelected ? new Color(0.25f, 0.90f, 0.34f, 1f) : new Color(0.21f, 0.72f, 0.29f, 0.98f);

                    if (cell == tacticalBoard.PlayerPosition)
                        label.text = "P";
                    else if (cell == tacticalBoard.MonsterPosition)
                        label.text = "M";
                    else if (cell == tacticalBoard.EnemyPosition)
                        label.text = "E";
                    bool obstacleCell = tacticalBoard.IsBox(cell) || tacticalBoard.IsTrap(cell) || tacticalBoard.IsIce(cell)
                        || tacticalBoard.IsPortal(cell) || tacticalBoard.IsSwitch(cell) || tacticalBoard.IsClosedDoor(cell);
                    label.gameObject.SetActive((cell == tacticalBoard.PlayerPosition || cell == tacticalBoard.EnemyPosition || cell == tacticalBoard.MonsterPosition || tacticalBoard.IsWall(cell) || obstacleCell) && (icon == null || icon.sprite == null));

                    button.interactable = tacticalBoard.Status == TacticalBoardStatus.Running;
                }
            }

            if (tacticalMovesText != null)
                tacticalMovesText.text = "Lượt: " + tacticalBoard.MoveBank + "   Đã đi: " + tacticalBoard.MovesUsed;
            if (tacticalStatusText != null)
                tacticalStatusText.text = tacticalBoard.LastMessage;

            if (tacticalMovesText != null)
                tacticalMovesText.text = "Lượt đi: " + tacticalBoard.MoveBank + "   Đã đi: " + tacticalBoard.MovesUsed;
            if (tacticalStatusText != null && tacticalBoard.Status == TacticalBoardStatus.Running && tacticalBoard.MoveBank <= 0)
                tacticalStatusText.text = "Cần lượt đi để di chuyển.";
        }

        Color MonsterNextCellColor()
        {
            if (tacticalBoard == null)
                return new Color(0.85f, 0.30f, 0.30f, 0.98f);

            float t = tacticalBoard.MonsterTimer;
            // <1s: nhấp nháy nhanh (nguy hiểm). 1-3s: cảnh báo. >3s: cam nhạt.
            float pulseSpeed = t < 1f ? 10f : (t < 3f ? 5f : 2.5f);
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * pulseSpeed);
            Color calm = t < 3f ? new Color(0.90f, 0.42f, 0.20f, 0.98f) : new Color(0.78f, 0.46f, 0.26f, 0.98f);
            Color hot = new Color(0.95f, 0.18f, 0.16f, 1f);
            return Color.Lerp(calm, hot, t < 3f ? pulse : pulse * 0.4f);
        }

        void UpdateMonsterTimerHud()
        {
            if (monsterNextCellIndex < 0 || monsterNextCellIndex >= tacticalCellImageCache.Count)
                return;
            var image = tacticalCellImageCache[monsterNextCellIndex];
            if (image != null)
                image.color = MonsterNextCellColor();
        }

        void OnTacticalStatusResolved()
        {
            if (tacticalBoard == null)
                return;
            if (tacticalShieldAura != null) tacticalShieldAura.gameObject.SetActive(false);
            RefreshTacticalBoardUi();
            UpdateUi();
            if (tacticalBoard.Status == TacticalBoardStatus.Won)
            {
                // Vừa xong màn tutorial: chèn thẻ "Hoàn thành hướng dẫn" rồi mới mở popup thắng.
                if (TutorialWantsCompletionCard())
                    StartCoroutine(TutorialShowCompletion(LevelComplete));
                else
                    LevelComplete();
            }
            else if (tacticalBoard.Status == TacticalBoardStatus.Failed)
            {
                TutorialFinish();
                EndGame(false);
            }
        }

        // Nhún nhẹ + react (nảy) cho Player/Monster/Enemy; cảnh báo nguy hiểm khi quái kề sát Player.
        // Gọi mỗi frame ở Update offline (RefreshTacticalBoardUi chỉ chạy khi có thay đổi nên bob phải ở đây).
        void AnimateTacticalPieces()
        {
            if (tacticalBoard == null || tacticalCellIcons.Count == 0)
                return;
            int w = tacticalBoard.Data.BoardWidth;
            int h = tacticalBoard.Data.BoardHeight;
            float t = Time.unscaledTime;

            AnimatePieceIcon(tacticalBoard.PlayerPosition, w, h, t, 0f, tacticalPlayerReactUntil);
            AnimatePieceIcon(tacticalBoard.MonsterPosition, w, h, t, 1.7f, tacticalMonsterReactUntil);
            AnimatePieceIcon(tacticalBoard.EnemyPosition, w, h, t, 3.1f, tacticalEnemyReactUntil);

            UpdateDangerWarning(t);
            UpdateTacticalShieldVisual(t);
        }

        // Ngưỡng cảnh báo sắp thua (giây còn lại của màn) và số hàng đỉnh cần soi ở bàn xếp gạch.
        const float WarningTimeLeft = 20f;
        const float EmergencyTimeLeft = 8f;
        const int DangerRowScan = 3;

        // Mức nguy hiểm hiện tại (0 = an toàn, 1 = cảnh báo, 2 = khẩn cấp).
        // Ba nguồn: quái kề sát Player, sắp hết giờ màn, bàn gạch sắp đầy.
        int CurrentDangerLevel()
        {
            if (tacticalBoard == null || tacticalBoard.Status != TacticalBoardStatus.Running || gameOver)
                return 0;

            int level = 0;

            var mp = tacticalBoard.MonsterPosition;
            var pp = tacticalBoard.PlayerPosition;
            int dist = Mathf.Abs(mp.x - pp.x) + Mathf.Abs(mp.y - pp.y);
            if (dist <= 1)
                level = tacticalBoard.ShieldLayers > 0 ? 1 : 2;

            float maxPlay = rules != null && rules.TacticalData != null ? rules.TacticalData.MaxPlaySeconds : 0f;
            if (maxPlay > 0f)
            {
                float left = maxPlay - gameplayTime;
                if (left <= EmergencyTimeLeft)
                    level = 2;
                else if (left <= WarningTimeLeft)
                    level = Mathf.Max(level, 1);
            }

            int stackRoom = TopEmptyRows();
            if (stackRoom <= 1)
                level = 2;
            else if (stackRoom <= 2)
                level = Mathf.Max(level, 1);

            return level;
        }

        // Số hàng trống liên tiếp tính từ NÓC bàn xếp gạch (tối đa DangerRowScan hàng — đủ để
        // biết bàn sắp đầy mà không phải quét cả lưới mỗi khung hình).
        int TopEmptyRows()
        {
            int empty = 0;
            for (int offset = 1; offset <= DangerRowScan; offset++)
            {
                int y = Height - offset;
                if (y < 0)
                    break;
                for (int x = 0; x < Width; x++)
                {
                    if (grid[x, y] > 0)
                        return empty;
                }
                empty++;
            }
            return empty;
        }

        // Sắp thua = màn hình nháy đỏ + rung từng đợt, càng nguy càng gấp.
        void UpdateDangerWarning(float t)
        {
            int level = CurrentDangerLevel();
            EnsureAttackFlashOverlay();
            if (attackFlashOverlay == null)
                return;

            if (level <= 0)
            {
                if (attackFlashOverlay.enabled)
                    attackFlashOverlay.enabled = false;
                highRiskAlerted = false;   // thoát hiểm -> lần sau nguy hiểm lại báo tiếp
                return;
            }

            bool emergency = level >= 2;
            // Báo âm thanh MỘT lần lúc vừa rơi vào mức khẩn cấp, không réo suốt theo nhịp nháy.
            if (emergency && !highRiskAlerted)
            {
                highRiskAlerted = true;
                GameAudio.PlayFx(GameAudio.FxHighRisk);
            }
            else if (!emergency)
            {
                highRiskAlerted = false;
            }
            float pulseSpeed = emergency ? 20f : 12f;
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * pulseSpeed);
            float alpha = (emergency ? 0.34f : 0.16f) * (0.45f + 0.55f * pulse);

            attackFlashOverlay.enabled = true;
            attackFlashOverlay.color = new Color(0.95f, 0.10f, 0.08f, alpha);
            float breathe = 1f + (emergency ? 0.05f : 0.03f) * pulse;
            attackFlashOverlay.rectTransform.localScale = new Vector3(breathe, breathe, 1f);

            // Rung + lắc camera theo từng đợt (không rung liên tục để đỡ nhiễu và đỡ tốn pin).
            if (t > nextDangerShakeTime)
            {
                nextDangerShakeTime = t + (emergency ? 0.42f : 0.85f);
                shake = Mathf.Max(shake, emergency ? 0.20f : 0.11f);
                if (emergency)
                    Haptics.Warning();
                else
                    Haptics.Soft();
            }
        }

        // Bong bóng khiên xanh phập phồng + badge "x{N}" quanh Player khi có khiên (rõ ngay trên bàn).
        void UpdateTacticalShieldVisual(float t)
        {
            int layers = tacticalBoard != null ? tacticalBoard.ShieldLayers : 0;
            bool show = layers > 0 && !MultiplayerMatch.Active
                && tacticalBoard != null && tacticalBoard.Status == TacticalBoardStatus.Running;

            EnsureTacticalShieldVisual();
            if (tacticalShieldAura == null)
                return;
            if (!show)
            {
                if (tacticalShieldAura.gameObject.activeSelf)
                    tacticalShieldAura.gameObject.SetActive(false);
                return;
            }

            var pp = tacticalBoard.PlayerPosition;
            int w = tacticalBoard.Data.BoardWidth;
            int idx = pp.y * w + pp.x;
            if (idx < 0 || idx >= tacticalCellButtons.Count || tacticalCellButtons[idx] == null)
            {
                tacticalShieldAura.gameObject.SetActive(false);
                return;
            }

            var cellRt = (RectTransform)tacticalCellButtons[idx].transform;
            var auraRt = tacticalShieldAura.rectTransform;
            auraRt.anchorMin = cellRt.anchorMin;
            auraRt.anchorMax = cellRt.anchorMax;
            auraRt.offsetMin = Vector2.zero;
            auraRt.offsetMax = Vector2.zero;

            float pulse = 0.5f + 0.5f * Mathf.Sin(t * 4.5f);
            auraRt.localScale = Vector3.one * (1.45f + 0.12f * pulse); // bao trọn player
            tacticalShieldAura.color = new Color(0.5f, 0.88f, 1f, 0.78f + 0.22f * pulse); // vòng sáng rõ
            if (!tacticalShieldAura.gameObject.activeSelf)
                tacticalShieldAura.gameObject.SetActive(true);
            if (tacticalShieldCount != null)
                tacticalShieldCount.text = layers.ToString();
        }

        void EnsureTacticalShieldVisual()
        {
            if (tacticalShieldAura != null)
                return;
            if (tacticalCellButtons.Count == 0 || tacticalCellButtons[0] == null)
                return;
            Transform parent = tacticalCellButtons[0].transform.parent; // gridRoot (cùng canvas với ô)
            var go = Ui.Panel(parent, "Runtime Tactical Shield", new Color(0.5f, 0.88f, 1f, 0.8f));
            var img = go.GetComponent<Image>();
            img.sprite = ShieldBubbleSprite(); // vòng tròn bao quanh player
            img.type = Image.Type.Simple;
            img.raycastTarget = false;
            tacticalShieldAura = img;

            // Số lớp khiên đặt ở góc trên-phải trên vòng, chữ to rõ, viền tối.
            tacticalShieldCount = Ui.Text(go.transform, "", font, 22, new Color(0.85f, 0.98f, 1f), TextAnchor.MiddleCenter);
            tacticalShieldCount.fontStyle = FontStyle.Bold;
            tacticalShieldCount.raycastTarget = false;
            tacticalShieldCount.resizeTextForBestFit = true;
            tacticalShieldCount.resizeTextMaxSize = 34;
            tacticalShieldCount.resizeTextMinSize = 8;
            Ui.Rect(tacticalShieldCount, new Vector2(0.60f, 0.60f), new Vector2(1.02f, 1.02f), Vector2.zero);
            AddDarkWoodTextEdge(tacticalShieldCount, 0.9f, 1f);
            go.transform.SetAsLastSibling();
            go.SetActive(false);
        }

        void AnimatePieceIcon(Vector2Int pos, int w, int h, float t, float phase, float reactUntil)
        {
            if (pos.x < 0 || pos.x >= w || pos.y < 0 || pos.y >= h)
                return;
            int index = pos.y * w + pos.x;
            if (index < 0 || index >= tacticalCellIcons.Count)
                return;
            var icon = tacticalCellIcons[index];
            if (icon == null || icon.sprite == null)
                return;

            float bob = Mathf.Sin(t * 3f + phase);
            float react = 1f;
            if (t < reactUntil)
                react = 1f + 0.4f * Mathf.Clamp01((reactUntil - t) / 0.3f);
            float s = 1.18f * (1f + 0.06f * bob) * react;
            var rt = icon.rectTransform;
            rt.localScale = new Vector3(s, s, 1f);
            rt.anchoredPosition = new Vector2(0f, bob * 3f);
        }

        // Chữ nổi bay lên trên một ô bàn cờ (pool FloatingLabel, tái dùng).
        // rise nhỏ + dur ngắn = kiểu "damage popup" hiện ngay trên quân; lớn = chữ thưởng bay cao.
        void SpawnTacticalFloat(string message, Color color, Vector2Int cell, float rise = 92f, float dur = 1.15f)
        {
            var label = GetFloatingLabel();
            if (label == null)
                return;
            label.Play(message, color, TacticalCellScreenPos(cell) + new Vector3(0f, 12f, 0f), rise, dur);
        }

        // Toạ độ MÀN HÌNH (px) của một ô bàn cờ — bàn cờ ở canvas Screen Space Camera nên phải
        // WorldToScreenPoint qua camera của canvas đó, rồi đặt chữ lên canvas overlay (px).
        Vector3 TacticalCellScreenPos(Vector2Int cell)
        {
            if (tacticalBoard != null)
            {
                int w = tacticalBoard.Data.BoardWidth;
                int index = cell.y * w + cell.x;
                if (index >= 0 && index < tacticalCellButtons.Count && tacticalCellButtons[index] != null)
                {
                    var crt = (RectTransform)tacticalCellButtons[index].transform;
                    var canvas = tacticalCellButtons[index].GetComponentInParent<Canvas>();
                    Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                    Vector2 sp = RectTransformUtility.WorldToScreenPoint(cam, crt.position);
                    return new Vector3(sp.x, sp.y, 0f);
                }
            }
            return new Vector3(Screen.width * 0.5f, Screen.height * 0.6f, 0f);
        }

        Transform EnsureFloatFxRoot()
        {
            if (floatFxRoot != null)
                return floatFxRoot;
            var go = new GameObject("Runtime Float FX Canvas");
            var cv = go.AddComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 30000; // trên mọi thứ (kể cả canvas isolated của bàn cờ)
            floatFxRoot = go.transform;
            return floatFxRoot;
        }

        FloatingLabel GetFloatingLabel()
        {
            for (int i = 0; i < floatingLabelPool.Count; i++)
                if (floatingLabelPool[i] != null && !floatingLabelPool[i].IsPlaying)
                    return floatingLabelPool[i];

            Transform parent = EnsureFloatFxRoot();
            if (parent == null)
                return null;
            var txt = Ui.Text(parent, "", RuntimeArt.LoadMenuButtonFont(), 42, Color.white, TextAnchor.MiddleCenter);
            txt.fontStyle = FontStyle.Bold;
            txt.raycastTarget = false;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            Ui.Rect(txt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(380, 84));
            AddDarkWoodTextEdge(txt, 1.3f, 0.95f);
            var fl = txt.gameObject.AddComponent<FloatingLabel>();
            txt.gameObject.SetActive(false);
            floatingLabelPool.Add(fl);
            return fl;
        }

        // GD v3 GĐ2: chuyển cụm tài nguyên đã kích hoạt thành hiệu ứng bàn Monster (offline).
        // Giày → điểm di chuyển, Kiếm → đẩy quái lùi, Khiên → lớp khiên. Cộng dồn mọi cụm/chain.
        // Trả về true nếu bàn cờ vừa kết thúc (quái bị đẩy trúng Enemy = thắng).
        bool ApplyOfflineClusterEffects(Puzzle.ResolutionOutcome outcome)
        {
            if (tacticalBoard == null || tacticalBoard.Status != TacticalBoardStatus.Running)
                return false;

            int moveGain = 0, shieldGain = 0, knockback = 0;
            foreach (var step in outcome.Steps)
                AccumulateClusterEffects(step.Clusters, ref moveGain, ref shieldGain, ref knockback);

            return ApplyTacticalClusterGains(moveGain, shieldGain, knockback);
        }

        // Bản chạy cho ĐÚNG một nhóm cụm. Tutorial dùng để cho chức năng của từng loại tài nguyên
        // chạy ngay sau khi đúng nhóm đó biến mất, thay vì cộng dồn hết cả chuỗi rồi mới chạy.
        bool ApplyOfflineClusterEffects(IReadOnlyList<Puzzle.Cluster> clusters)
        {
            if (tacticalBoard == null || tacticalBoard.Status != TacticalBoardStatus.Running)
                return false;

            int moveGain = 0, shieldGain = 0, knockback = 0;
            AccumulateClusterEffects(clusters, ref moveGain, ref shieldGain, ref knockback);

            return ApplyTacticalClusterGains(moveGain, shieldGain, knockback);
        }

        static void AccumulateClusterEffects(IReadOnlyList<Puzzle.Cluster> clusters,
            ref int moveGain, ref int shieldGain, ref int knockback)
        {
            foreach (var cluster in clusters)
            {
                bool strong = cluster.Tier == Puzzle.ClusterTier.Strong;
                switch (cluster.Resource)
                {
                    case Puzzle.ResourceType.Move: moveGain += strong ? 2 : 1; break;
                    case Puzzle.ResourceType.Attack: knockback += strong ? 2 : 1; break;
                    case Puzzle.ResourceType.Shield: shieldGain += strong ? 2 : 1; break;
                }
            }
        }

        // Trả về true nếu bàn cờ vừa kết thúc (quái bị đẩy trúng Enemy = thắng).
        bool ApplyTacticalClusterGains(int moveGain, int shieldGain, int knockback)
        {
            float now = Time.unscaledTime;
            var playerPos = tacticalBoard.PlayerPosition;
            if (moveGain > 0)
            {
                tacticalBoard.AddMovementPoints(moveGain);
                SpawnTacticalFloat("+" + moveGain + " Lượt", new Color(0.55f, 1f, 0.55f), playerPos);
                tacticalPlayerReactUntil = now + 0.3f;
            }
            if (shieldGain > 0)
            {
                tacticalBoard.AddShield(shieldGain);
                SpawnTacticalFloat("Khiên +" + shieldGain, new Color(0.55f, 0.85f, 1f), playerPos);
                tacticalPlayerReactUntil = now + 0.3f;
            }
            if (knockback > 0)
            {
                // Chữ hiện NGAY tại Monster nơi trúng đòn (kiểu damage popup: bung nhẹ, ít trôi).
                var hitPos = tacticalBoard.MonsterPosition;
                tacticalBoard.KnockbackMonster(knockback);
                SpawnTacticalFloat("ĐẨY LÙI!", new Color(1f, 0.5f, 0.3f), hitPos, 40f, 0.85f);
                tacticalMonsterReactUntil = now + 0.3f;
                shake = Mathf.Max(shake, 0.25f);
                Beep(300f, 0.10f, 0.24f);
            }

            RefreshTacticalBoardUi();
            return tacticalBoard.Status != TacticalBoardStatus.Running;
        }

        void AwardTacticalMoves(int clearedLines, bool comboBonus)
        {
            if (tacticalBoard == null || tacticalBoard.Status != TacticalBoardStatus.Running || clearedLines <= 0)
                return;

            tacticalBoard.AddMovesForClearedLines(clearedLines, comboBonus);
            RefreshTacticalBoardUi();
        }

        void WriteTacticalPos(int index, Vector2Int pos)
        {
            bool valid = pos.x >= 0 && pos.x < 255 && pos.y >= 0 && pos.y < 255;
            tacticalSnapshot[index] = valid ? (byte)pos.x : (byte)255;
            tacticalSnapshot[index + 1] = valid ? (byte)pos.y : (byte)255;
        }
    }
}
