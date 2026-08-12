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
    public static class GameSession
    {
        public static int SelectedLevel = 1;
        public static int JourneyLevel = 1;
    }

    public static class LevelProgress
    {
        public const int MaxLevels = 20;
        // Giá trị chuỗi key giữ nguyên tên "TOWER" cũ để không mất save hiện có.
        public const string UnlockedLevelKey = "BLOCKFALL_TOWER_UNLOCKED_FLOOR";
        public const string CoinsKey = "BLOCKFALL_COINS";
        public const string TotalLinesClearedKey = "BLOCKFALL_TOTAL_LINES_CLEARED";

        public static int CurrentUnlockedLevel => Mathf.Clamp(PlayerPrefs.GetInt(UnlockedLevelKey, 1), 1, MaxLevels);

        public static string StarKey(int level)
        {
            return "BLOCKFALL_TOWER_FLOOR_" + Mathf.Clamp(level, 1, MaxLevels) + "_STARS";
        }

        public static string BestScoreKeyForLevel(int level)
        {
            return "BLOCKFALL_TOWER_LEVEL_" + Mathf.Clamp(level, 1, MaxLevels) + "_BEST_SCORE";
        }

        public static int Coins => PlayerPrefs.GetInt(CoinsKey, 0);

        public static void AddCoins(int amount)
        {
            PlayerPrefs.SetInt(CoinsKey, Mathf.Max(0, Coins + amount));
        }

        public static int StarsForLevel(int level)
        {
            return Mathf.Clamp(PlayerPrefs.GetInt(StarKey(level), 0), 0, 3);
        }

        public static void SaveLevelResult(int level, int stars)
        {
            level = Mathf.Clamp(level, 1, MaxLevels);
            stars = Mathf.Clamp(stars, 1, 3);
            if (stars > StarsForLevel(level))
                PlayerPrefs.SetInt(StarKey(level), stars);

            PlayerPrefs.SetInt(UnlockedLevelKey, Mathf.Max(CurrentUnlockedLevel, Mathf.Min(MaxLevels, level + 1)));
            PlayerPrefs.Save();
        }

        public static void SaveLevelBestScore(int level, int score)
        {
            string key = BestScoreKeyForLevel(level);
            if (score > PlayerPrefs.GetInt(key, 0))
                PlayerPrefs.SetInt(key, score);
        }
    }

    [Serializable]
    public class LevelRules
    {
        public Color BackgroundB;
        public float FallInterval;
        public float SpeedRampSeconds;
        public float MaxFallSpeedMultiplier = 1f;
        public int GarbageEveryPieces;
        public float SurpriseGarbageChance;
        public int ScoreMultiplier;
        public int ForcedPieceType = -1;
        public bool AllowSpecialBlocks = true;
        public bool GhostPreview = true;
        public bool FastBlocks;
        public bool HasStoneBlocks;
        public bool HasFixedObstacles;
        public int RotationLimit;
        public int RisingDangerSeconds;
        public int CoinReward = 50;
        public TacticalLevelData TacticalData;

        public static LevelRules CreateJourney(int level)
        {
            int stage = Mathf.Max(1, level);
            var rules = new LevelRules
            {
                BackgroundB = new Color(0.08f, 0.22f, 0.18f),
                FallInterval = Mathf.Max(0.34f, 0.80f - Mathf.Min(stage - 1, 30) * 0.010f),
                // Every level ramps up gently over time; pattern levels may override
                // with a stronger ramp in ApplyLevelConfig.
                SpeedRampSeconds = 150f,
                MaxFallSpeedMultiplier = 1.6f,
                GarbageEveryPieces = 0,
                SurpriseGarbageChance = 0f,
                ScoreMultiplier = 1,
                AllowSpecialBlocks = true,
                // Shown only for the first GhostPreviewPieces drops of each game.
                GhostPreview = true,
                FastBlocks = stage >= 15,
                RotationLimit = 0,
                RisingDangerSeconds = 0,
                CoinReward = 45 + stage * 5
            };

            ApplyLevelConfig(rules, stage);
            rules.TacticalData = TacticalLevelData.Create(stage);
            rules.FallInterval = rules.TacticalData.InitialFallSpeed;
            rules.CoinReward = rules.TacticalData.CoinReward;
            return rules;
        }

        static void ApplyLevelConfig(LevelRules rules, int level)
        {
            int pattern = (level - 1) % 10;
            if (pattern == 4)
                rules.GhostPreview = false;
            else if (pattern == 5)
            {
                rules.SpeedRampSeconds = Mathf.Max(80f, 180f - level * 3f);
                rules.MaxFallSpeedMultiplier = Mathf.Min(2.8f, 1.35f + level * 0.04f);
            }
            else if (pattern == 6)
                rules.SurpriseGarbageChance = Mathf.Min(0.20f, 0.08f + level * 0.004f);
            else if (pattern == 8)
            {
                rules.SpeedRampSeconds = 150f;
                rules.MaxFallSpeedMultiplier = Mathf.Min(2.7f, 1.45f + level * 0.035f);
            }
            else if (pattern == 9)
                rules.HasStoneBlocks = true;

            if (level >= 12 && level % 4 == 0)
                rules.RotationLimit = Mathf.Max(10, 24 - level / 2);

            if (level >= 18 && level % 6 == 0)
                rules.RisingDangerSeconds = Mathf.Clamp(34 - level / 2, 16, 34);

            if (level >= 24 && level % 8 == 0)
                rules.HasFixedObstacles = true;
        }
    }

    public class MenuController : MonoBehaviour
    {
        Font font;
        Font titleFont;
        Font boldFont;
        GameObject mapOverlay;

        void Start()
        {
            font = LoadFont();
            titleFont = RuntimeArt.LoadDisplayFont();
            boldFont = Resources.Load<Font>("BrickStacker/VietnameseArial") ?? font;
            Time.timeScale = 1f;
            BuildCamera();
            BuildBackground();
            BuildUi();
            PromptNameOnFirstLaunch();
        }

        const string NamePromptedKey = "BLOCKFALL_NAME_PROMPTED";

        // Lần đầu vào game (chưa từng hỏi + chưa có tên trên server) → hỏi tên luôn.
        async void PromptNameOnFirstLaunch()
        {
            if (PlayerPrefs.GetInt(NamePromptedKey, 0) == 1)
                return;

            await ServicesManager.EnsureSignedInAsync();
            if (this == null || mapOverlay != null)
                return;
            if (!string.IsNullOrEmpty(ServicesManager.PlayerName))
            {
                PlayerPrefs.SetInt(NamePromptedKey, 1);
                return;
            }

            var panel = GameObject.Find("Menu Panel");
            if (panel != null)
                ShowNamePopup(panel.transform);
        }

        void ShowNamePopup(Transform parent)
        {
            if (mapOverlay != null) Destroy(mapOverlay);
            mapOverlay = Ui.Panel(parent, "Name Overlay", new Color(0, 0, 0, 0.72f));
            Ui.Stretch(mapOverlay);

            var box = Ui.Panel(mapOverlay.transform, "Name Box", Color.white);
            Ui.Rect(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(680, 560));
            StyleWoodPopupFrame(box);

            var titleLabel = Ui.Text(box.transform, "CHÀO BẠN!", font, 52, new Color(1f, 0.84f, 0.50f), TextAnchor.MiddleCenter);
            titleLabel.fontStyle = FontStyle.Bold;
            Ui.Rect(titleLabel, new Vector2(0.5f, 0.84f), new Vector2(0.5f, 0.84f), new Vector2(500, 100));
            AddDarkWoodTextEdge(titleLabel, 1.0f, 0.86f);
            AddWarmTitleFinish(titleLabel, 0.48f);

            var descLabel = Ui.Text(box.transform, "Đặt tên hiển thị của bạn —\ndùng cho bảng xếp hạng và trận đấu 1v1.", font, 26, new Color(1f, 0.91f, 0.74f), TextAnchor.MiddleCenter);
            Ui.Rect(descLabel, new Vector2(0.5f, 0.655f), new Vector2(0.5f, 0.655f), new Vector2(580, 90));
            AddDarkWoodTextEdge(descLabel, 0.6f, 0.74f);

            var nameInput = BuildNameInput(box.transform, new Vector2(0.5f, 0.46f), new Vector2(420, 78));

            var statusLabel = Ui.Text(box.transform, "", font, 22, new Color(1f, 0.86f, 0.60f, 0.9f), TextAnchor.MiddleCenter);
            Ui.Rect(statusLabel, new Vector2(0.5f, 0.335f), new Vector2(0.5f, 0.335f), new Vector2(580, 40));
            AddDarkWoodTextEdge(statusLabel, 0.5f, 0.7f);

            var (confirmBtn, _) = AddMenuButton(box.transform, "XÁC NHẬN", new Vector2(0.5f, 0.185f), Vector2.zero, () => { }, new Vector2(420, 84), 34);
            confirmBtn.onClick.AddListener(() =>
            {
                RuntimeArt.PlayUiSwitchSound();
                ConfirmFirstName(nameInput, statusLabel, confirmBtn);
            });

            // Nút đóng = bỏ qua (vẫn đặt được sau trong Bảng xếp hạng), không hỏi lại nữa.
            var closeBtn = Ui.Button(box.transform, "", font, 1, () =>
            {
                RuntimeArt.PlayUiSwitchSound();
                PlayerPrefs.SetInt(NamePromptedKey, 1);
                PlayerPrefs.Save();
                Destroy(mapOverlay);
            });
            Ui.Rect(closeBtn.gameObject, new Vector2(0.118f, 0.85f), new Vector2(0.118f, 0.85f), new Vector2(60, 60));
            StyleMapBackButton(closeBtn);
        }

        async void ConfirmFirstName(InputField nameInput, Text statusLabel, Button confirmBtn)
        {
            string name = nameInput.text != null ? nameInput.text.Trim() : "";
            if (name.Length < 2)
            {
                statusLabel.text = "Tên cần ít nhất 2 ký tự.";
                return;
            }

            confirmBtn.interactable = false;
            statusLabel.text = "Đang lưu tên...";
            bool ok = await ServicesManager.SetPlayerNameAsync(name);
            if (this == null || mapOverlay == null)
                return;
            if (ok)
            {
                PlayerPrefs.SetInt(NamePromptedKey, 1);
                PlayerPrefs.Save();
                Destroy(mapOverlay);
            }
            else
            {
                statusLabel.text = "Không lưu được tên. Kiểm tra mạng rồi thử lại.";
                confirmBtn.interactable = true;
            }
        }

        void BuildCamera()
        {
            var cam = Camera.main ?? FindAnyObjectByType<Camera>();
            GameObject cameraObject;
            if (cam == null)
            {
                cameraObject = new GameObject("Main Camera");
                cam = cameraObject.AddComponent<Camera>();
            }
            else
            {
                cameraObject = cam.gameObject;
            }
            cam.tag = "MainCamera";
            cam.orthographic = true;
            cam.orthographicSize = 5.4f;
            cam.backgroundColor = new Color(0.33f, 0.15f, 0.055f);
            cameraObject.transform.position = new Vector3(0, 0, -10);
            if (cameraObject.GetComponent<AudioListener>() == null)
                cameraObject.AddComponent<AudioListener>();
        }

        void BuildBackground()
        {
            var cam = Camera.main ?? FindAnyObjectByType<Camera>();
            if (cam != null)
                cam.backgroundColor = new Color(0.42f, 0.68f, 0.92f);
        }

        void BuildUi()
        {
            var canvas = Ui.CreateCanvas("Menu Canvas");

            // Nền menu phủ KÍN màn hình (cover) — ảnh mới có nhiều khoảng trời quanh logo
            // nên phần bị cắt hai đầu trên màn rộng/hẹp không đụng tới logo/nhân vật.
            var bgSpr = RuntimeArt.LoadV3Sprite("screen-menu/bg-menu.png");
            if (bgSpr != null)
            {
                var bg = Ui.Panel(canvas.transform, "BG", Color.white);
                Ui.Stretch(bg);
                bg.transform.SetAsFirstSibling();
                var bgImg = bg.GetComponent<Image>();
                bgImg.sprite = bgSpr; bgImg.type = Image.Type.Simple; bgImg.preserveAspect = false;
                var bgRt = bg.GetComponent<RectTransform>();
                bgRt.anchorMin = bgRt.anchorMax = new Vector2(0.5f, 0.5f);
                bgRt.pivot = new Vector2(0.5f, 0.5f);
                bgRt.anchoredPosition = Vector2.zero;
                var arf = bg.AddComponent<AspectRatioFitter>();
                arf.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                arf.aspectRatio = (float)bgSpr.texture.width / bgSpr.texture.height;
            }

            var safe = Ui.Panel(canvas.transform, "Safe", new Color(0, 0, 0, 0));
            Ui.Stretch(safe);
            safe.AddComponent<SafeAreaFitter>();
            var panel = Ui.Panel(safe.transform, "Panel", new Color(0, 0, 0, 0));
            Ui.Stretch(panel);

            // Hai nút chính dưới giữa: BẮT ĐẦU (cam) · ĐẤU 1 VS 1 (xanh) — icon trái, chữ phải.
            var startBtn = BuildMenuActionButton(panel.transform, "screen-menu/btn-batdau.png",
                new Rect(0.067f, 0.333f, 0.792f, 0.355f), "BẮT ĐẦU", new Vector2(0.283f, 0.125f), 430f, 3.34f, 38, 0.32f,
                () => { RuntimeArt.PlayUiSwitchSound(); SceneManager.LoadScene("BrickLevel"); });
            StartCoroutine(PulseButton(startBtn.transform, null));

            BuildMenuActionButton(panel.transform, "screen-menu/btn-1vs1.png",
                new Rect(0.110f, 0.335f, 0.840f, 0.340f), "ĐẤU 1 VS 1", new Vector2(0.717f, 0.125f), 450f, 3.71f, 32, 0.30f,
                () => { RuntimeArt.PlayUiSwitchSound(); MultiplayerManager.PrewarmQuickQuery(); ShowMultiplayerOverlay(panel.transform); });

            // Hai icon tròn góc trên-trái: hướng dẫn (trên) · bảng xếp hạng (dưới) — giãn nhẹ.
            BuildMenuIconButton(panel.transform, "screen-menu/btn-guide.png",
                new Rect(0.100f, 0.232f, 0.800f, 0.570f), new Vector2(0.052f, 0.880f), 118f,
                () => { RuntimeArt.PlayUiSwitchSound(); ShowTutorialOverlay(panel.transform); });

            BuildMenuIconButton(panel.transform, "screen-menu/btn-bxh.png",
                new Rect(0.237f, 0.093f, 0.513f, 0.790f), new Vector2(0.052f, 0.690f), 118f,
                () => { RuntimeArt.PlayUiSwitchSound(); ShowLeaderboardOverlay(panel.transform); });
        }

        // Nút ngang lớn dùng sprite đã crop gọn (icon trái, chữ phải). widthPx: bề ngang trên
        // canvas ref; aspect: tỉ lệ ngang/dọc của vùng crop; textLeftFrac: chữ bắt đầu sau icon.
        Button BuildMenuActionButton(Transform parent, string spriteAsset, Rect crop, string label,
            Vector2 anchor, float widthPx, float aspect, int fontSize, float textLeftFrac, UnityEngine.Events.UnityAction action)
        {
            var button = Ui.Button(parent, "", font, fontSize, action);
            var rt = button.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(widthPx, widthPx / aspect);

            var img = button.GetComponent<Image>();
            var spr = RuntimeArt.LoadV3SubSprite(spriteAsset, crop);
            if (spr != null) { img.sprite = spr; img.type = Image.Type.Simple; img.preserveAspect = true; img.color = Color.white; }
            else img.color = new Color(0.72f, 0.48f, 0.14f);

            var txt = Ui.Text(button.transform, label, RuntimeArt.LoadMenuButtonFont(), fontSize, new Color(1f, 0.98f, 0.88f), TextAnchor.MiddleCenter);
            txt.fontStyle = FontStyle.Bold;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.raycastTarget = false;
            var txtRt = txt.rectTransform;
            txtRt.anchorMin = new Vector2(textLeftFrac, 0.14f);
            txtRt.anchorMax = new Vector2(0.965f, 0.86f);
            txtRt.offsetMin = txtRt.offsetMax = Vector2.zero;
            AddDarkWoodTextEdge(txt, 1.1f, 0.92f);
            var outline = txt.GetComponent<Outline>();
            if (outline != null) { outline.effectColor = Color.black; outline.effectDistance = new Vector2(Mathf.Max(2f, fontSize * 0.07f), Mathf.Max(2f, fontSize * 0.07f)); }

            AddPressScaleFeedback(button.gameObject, 0.94f);
            return button;
        }

        // Nút icon tròn (crop lấy đúng vòng tròn từ sprite) — dùng cho hướng dẫn / BXH.
        Button BuildMenuIconButton(Transform parent, string spriteAsset, Rect crop, Vector2 anchor, float sizePx, UnityEngine.Events.UnityAction action)
        {
            var button = Ui.Button(parent, "", font, 1, action);
            var rt = button.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(sizePx, sizePx);

            var img = button.GetComponent<Image>();
            var spr = RuntimeArt.LoadV3SubSprite(spriteAsset, crop);
            if (spr != null) { img.sprite = spr; img.type = Image.Type.Simple; img.preserveAspect = true; img.color = Color.white; }
            else img.color = new Color(0.9f, 0.7f, 0.2f);

            AddPressScaleFeedback(button.gameObject, 0.90f);
            return button;
        }

        System.Collections.IEnumerator PulseButton(Transform btn, Transform shadow)
        {
            float amplitude = 0.030f;
            float speed = 0.65f;
            while (btn != null)
            {
                float s = 1f + amplitude * Mathf.Sin(Time.time * speed * Mathf.PI * 2f);
                btn.localScale = new Vector3(s, s, 1f);
                if (shadow != null) shadow.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
        }

        // Nút bảng xếp hạng góc phải dưới, đối xứng với nút Hướng dẫn góc trái.

        // Màn BẢNG XẾP HẠNG (thiết kế screen-bxh): full màn ngang, nền dùng chung trang level,
        // nút back dùng chung, tiêu đề gỗ + panel CÚP góc phải, bảng xanh 10 dòng
        // (top1/2/3 khung vàng/bạc/đồng + huy hiệu; còn lại khung xanh + số hạng).
        const string BXH = "screen-bxh/";

        void ShowLeaderboardOverlay(Transform parent)
        {
            if (mapOverlay != null) Destroy(mapOverlay);
            var rootCanvas = parent.GetComponentInParent<Canvas>();
            Transform overlayParent = rootCanvas != null ? rootCanvas.rootCanvas.transform : parent;
            mapOverlay = Ui.Panel(overlayParent, "Leaderboard Overlay", new Color(0.07f, 0.20f, 0.55f, 1f));
            Ui.Stretch(mapOverlay);
            var ovCanvas = mapOverlay.AddComponent<Canvas>();
            ovCanvas.overrideSorting = true;
            ovCanvas.sortingOrder = 500;
            mapOverlay.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Nền dùng chung trang level (bg-level), phủ kín (envelope).
            var bg = MakeV3Image(mapOverlay.transform, "BG", "screen-level/bg-level.png", new Rect(0f, 0f, 1f, 1f), false);
            bg.preserveAspect = false;
            bg.raycastTarget = false;
            var bgRt = bg.rectTransform;
            bgRt.anchorMin = bgRt.anchorMax = new Vector2(0.5f, 0.5f);
            bgRt.pivot = new Vector2(0.5f, 0.5f);
            if (bg.sprite != null)
            {
                var arf = bg.gameObject.AddComponent<AspectRatioFitter>();
                arf.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                arf.aspectRatio = (float)bg.sprite.texture.width / bg.sprite.texture.height;
            }

            // Nút back dùng chung (btn-back của trang level), đóng overlay.
            var backBtn = Ui.Button(mapOverlay.transform, "", font, 1, () =>
            {
                RuntimeArt.PlayUiSwitchSound();
                Destroy(mapOverlay);
            });
            var bkRt = backBtn.GetComponent<RectTransform>();
            bkRt.anchorMin = bkRt.anchorMax = new Vector2(0.052f, 0.9f);
            bkRt.pivot = new Vector2(0.5f, 0.5f);
            bkRt.sizeDelta = new Vector2(92f, 92f);
            var bkImg = backBtn.GetComponent<Image>();
            var bkSpr = RuntimeArt.LoadV3SubSprite("screen-level/btn-back.png", new Rect(0.345f, 0.300f, 0.309f, 0.435f));
            if (bkSpr != null) { bkImg.sprite = bkSpr; bkImg.type = Image.Type.Simple; bkImg.preserveAspect = true; bkImg.color = Color.white; }
            else bkImg.color = new Color(0.2f, 0.5f, 0.95f);
            bkRt.localScale = new Vector3(-1f, 1f, 1f); // mũi tên gốc chỉ phải → lật sang trái
            AddPressScaleFeedback(backBtn.gameObject, 0.9f);

            // Tiêu đề gỗ BẢNG XẾP HẠNG (giữa-trên).
            var title = MakeV3Image(mapOverlay.transform, "BXH Title", BXH + "title.png", new Rect(0.007f, 0.400f, 0.986f, 0.229f), false);
            var tiRt = title.rectTransform;
            tiRt.anchorMin = tiRt.anchorMax = new Vector2(0.5f, 0.905f);
            tiRt.pivot = new Vector2(0.5f, 0.5f);
            float titleW = 440f;
            tiRt.sizeDelta = new Vector2(titleW, titleW / 4.31f);

            // Panel CÚP góc phải-trên + số.
            var cupPanel = MakeV3Image(mapOverlay.transform, "Cup Panel", BXH + "panel-cup.png", new Rect(0.009f, 0.329f, 0.982f, 0.383f), false);
            var cpRt = cupPanel.rectTransform;
            cpRt.anchorMin = cpRt.anchorMax = new Vector2(0.965f, 0.9f);
            cpRt.pivot = new Vector2(1f, 0.5f);
            float cupW = 224f;
            cpRt.sizeDelta = new Vector2(cupW, cupW / 2.567f);
            var cupText = Ui.Text(cupPanel.transform, "0", RuntimeArt.LoadMenuButtonFont(), 28, new Color(1f, 0.98f, 0.9f), TextAnchor.MiddleCenter);
            cupText.fontStyle = FontStyle.Bold;
            cupText.raycastTarget = false;
            cupText.horizontalOverflow = HorizontalWrapMode.Overflow;
            cupText.verticalOverflow = VerticalWrapMode.Overflow;
            var cutRt = cupText.rectTransform;
            cutRt.anchorMin = new Vector2(0.35f, 0.08f);
            cutRt.anchorMax = new Vector2(0.97f, 0.92f);
            cutRt.offsetMin = cutRt.offsetMax = Vector2.zero;

            // Bảng xanh (table-bxh) — có sẵn header HẠNG | NGƯỜI CHƠI | CÚP.
            var table = MakeV3Image(mapOverlay.transform, "BXH Table", BXH + "table-bxh.png", new Rect(0.025f, 0.061f, 0.949f, 0.895f), false);
            var taRt = table.rectTransform;
            taRt.anchorMin = taRt.anchorMax = new Vector2(0.5f, 0.42f);
            taRt.pivot = new Vector2(0.5f, 0.5f);
            float tableW = 902f;
            taRt.sizeDelta = new Vector2(tableW, tableW / 1.592f);

            // Thân bảng: vùng CUỘN (hiện 10 dòng, cuộn xem tới top 20).
            const float bodyTop = 0.84f, bodyBot = 0.045f;
            float rowH = (bodyTop - bodyBot) * (tableW / 1.592f) / 10f;

            var scrollGO = new GameObject("LB Scroll", typeof(RectTransform), typeof(UnityEngine.UI.ScrollRect));
            scrollGO.transform.SetParent(table.transform, false);
            var scRt = scrollGO.GetComponent<RectTransform>();
            scRt.anchorMin = new Vector2(0.035f, bodyBot);
            scRt.anchorMax = new Vector2(0.965f, bodyTop);
            scRt.offsetMin = scRt.offsetMax = Vector2.zero;
            var scroll = scrollGO.GetComponent<UnityEngine.UI.ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = rowH * 0.7f;

            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(UnityEngine.UI.RectMask2D), typeof(Image));
            viewport.transform.SetParent(scrollGO.transform, false);
            var vpRt = viewport.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = vpRt.offsetMax = Vector2.zero;
            viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f); // trong suốt, bắt thao tác kéo cuộn

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            var ctRt = content.GetComponent<RectTransform>();
            ctRt.anchorMin = new Vector2(0f, 1f);
            ctRt.anchorMax = new Vector2(1f, 1f);
            ctRt.pivot = new Vector2(0.5f, 1f);
            ctRt.anchoredPosition = Vector2.zero;
            ctRt.sizeDelta = Vector2.zero;

            scroll.viewport = vpRt;
            scroll.content = ctRt;

            // Dòng trạng thái (đang tải / trống / lỗi mạng) — ẩn khi có dữ liệu.
            var statusLabel = Ui.Text(table.transform, "Đang tải...", RuntimeArt.LoadMenuButtonFont(), 30, new Color(1f, 0.98f, 0.92f), TextAnchor.MiddleCenter);
            statusLabel.raycastTarget = false;
            var stRt = statusLabel.rectTransform;
            stRt.anchorMin = new Vector2(0.1f, bodyBot);
            stRt.anchorMax = new Vector2(0.9f, bodyTop);
            stRt.offsetMin = stRt.offsetMax = Vector2.zero;

            PopulateLeaderboard(mapOverlay, ctRt, cupText, rowH, statusLabel);
        }

        InputField BuildNameInput(Transform parent, Vector2 anchor, Vector2 size)
        {
            var frame = Ui.Panel(parent, "Name Input", new Color(0.16f, 0.07f, 0.025f, 0.92f));
            Ui.Rect(frame, anchor, anchor, size);

            var placeholder = Ui.Text(frame.transform, "Tên của bạn...", font, 24, new Color(1f, 0.88f, 0.62f, 0.45f), TextAnchor.MiddleCenter);
            Ui.Stretch(placeholder.gameObject);
            placeholder.fontStyle = FontStyle.Italic;

            var inputText = Ui.Text(frame.transform, "", font, 28, new Color(1f, 0.94f, 0.75f), TextAnchor.MiddleCenter);
            Ui.Stretch(inputText.gameObject);
            inputText.supportRichText = false;

            var input = frame.AddComponent<InputField>();
            input.textComponent = inputText;
            input.placeholder = placeholder;
            input.characterLimit = 12;
            input.contentType = InputField.ContentType.Alphanumeric;
            return input;
        }

        async void SavePlayerName(GameObject box, InputField nameInput, Button saveBtn, Text nameStatus)
        {
            string name = nameInput.text != null ? nameInput.text.Trim() : "";
            if (name.Length < 2)
            {
                nameStatus.text = "Tên cần ít nhất 2 ký tự.";
                return;
            }

            saveBtn.interactable = false;
            nameStatus.text = "Đang lưu tên...";
            bool ok = await ServicesManager.SetPlayerNameAsync(name);
            if (box == null) return; // popup đã đóng
            nameStatus.text = ok
                ? "Đã lưu tên: " + ServicesManager.PlayerName
                : "Không lưu được tên. Kiểm tra mạng rồi thử lại.";
            saveBtn.interactable = true;
        }

        // Avatar mẫu (icon nhân vật) xoay vòng cho từng dòng — leaderboard thật không kèm avatar.
        static readonly string[] LbAvatars = { "red_demon", "purple_monster", "green_knight", "red_robot", "blue_knight", "purple_bat" };

        static string FormatLbScore(long v) => v.ToString("#,0").Replace(',', '.');

        const int LbTopCount = 20;

        async void PopulateLeaderboard(GameObject overlay, RectTransform content, Text cupText, float rowH, Text statusLabel)
        {
            var names = new System.Collections.Generic.List<string>();
            var scores = new System.Collections.Generic.List<long>();
            int ownIndex = -1;
            long ownScore = 0;
            bool hasOwn = false;
            bool loaded = false; // true nếu dịch vụ trả lời trong thời gian chờ (dù bảng trống)

            try
            {
                // Đọc dữ liệu THẬT của người chơi (bảng "weekly"). Có timeout để không treo khi mất mạng.
                var loadTask = LeaderboardsSync.LoadWeeklyAsync(LbTopCount);
                if (await System.Threading.Tasks.Task.WhenAny(loadTask, System.Threading.Tasks.Task.Delay(4000)) == loadTask)
                {
                    var (top, me) = loadTask.Result;
                    if (overlay == null) return; // đã đóng trong lúc chờ
                    loaded = true;
                    if (top != null)
                    {
                        for (int i = 0; i < top.Count && i < LbTopCount; i++)
                        {
                            string nm = LeaderboardsSync.DisplayName(top[i]);
                            bool isOwn = me != null && top[i].PlayerId == me.PlayerId;
                            if (isOwn)
                            {
                                ownIndex = i;
                                if (!string.IsNullOrEmpty(ServicesManager.PlayerName)) nm = ServicesManager.PlayerName;
                            }
                            names.Add(nm);
                            scores.Add((long)top[i].Score);
                        }
                    }
                    if (me != null) { ownScore = (long)me.Score; hasOwn = true; }
                }
                if (overlay == null) return;
            }
            catch (Exception) { if (overlay == null) return; loaded = false; }

            // Số cúp của CHÍNH mình (chưa có thì "--").
            if (cupText != null)
                cupText.text = hasOwn ? FormatLbScore(ownScore) : "--";

            // Bảng trống hoặc mất mạng → báo trạng thái, KHÔNG hiện dữ liệu giả.
            if (names.Count == 0)
            {
                if (statusLabel != null)
                {
                    statusLabel.text = "Ở đây hơi trống vắng nhỉ ?";
                    var stRt = statusLabel.rectTransform;
                    stRt.anchorMin = new Vector2(0.08f, 0.60f);
                    stRt.anchorMax = new Vector2(0.92f, 0.78f);
                    stRt.offsetMin = stRt.offsetMax = Vector2.zero;

                    // Nhân vật buồn bên dưới câu chữ.
                    var chr = MakeV3Image(statusLabel.transform.parent, "Empty Char", "screen-bxh/decor-player.png", new Rect(0.234f, 0.003f, 0.568f, 0.989f), false);
                    var chRt = chr.rectTransform;
                    chRt.anchorMin = chRt.anchorMax = new Vector2(0.5f, 0.37f);
                    chRt.pivot = new Vector2(0.5f, 0.5f);
                    chRt.sizeDelta = new Vector2(230f * 0.862f, 230f);
                }
                return;
            }

            if (statusLabel != null) statusLabel.gameObject.SetActive(false);

            int count = Mathf.Min(names.Count, LbTopCount);
            content.sizeDelta = new Vector2(0f, count * rowH);
            for (int i = 0; i < count; i++)
                BuildLeaderboardRow(content, i, names[i], scores[i], i == ownIndex, rowH);
        }

        // Sprite thanh bo tròn (góc trong suốt, 9-slice) — vẽ 1 lần, tô màu theo hạng.
        static Sprite lbRoundedBar;
        static Sprite GetRoundedBarSprite()
        {
            if (lbRoundedBar != null) return lbRoundedBar;
            int W = 48, H = 48, r = 16;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float dx = Mathf.Max(Mathf.Max((r - 1) - x, x - (W - r)), 0f);
                    float dy = Mathf.Max(Mathf.Max((r - 1) - y, y - (H - r)), 0f);
                    float a = Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            lbRoundedBar = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(r, r, r, r));
            return lbRoundedBar;
        }

        // Màu thanh dòng lấy từ chính asset frame-topX (điểm giữa) để đúng vàng/bạc/đồng/xanh.
        Color LbBarColor(int i)
        {
            string frame = i == 0 ? "frame-top1" : i == 1 ? "frame-top2" : i == 2 ? "frame-top3" : "frame-topother";
            var sp = RuntimeArt.LoadV3Sprite(BXH + frame + ".png");
            if (sp != null)
            {
                try { return sp.texture.GetPixelBilinear(0.5f, 0.5f); } catch { }
            }
            return i == 0 ? new Color(0.93f, 0.66f, 0.12f) : i == 1 ? new Color(0.75f, 0.80f, 0.93f) : i == 2 ? new Color(0.80f, 0.52f, 0.30f) : new Color(0.09f, 0.20f, 0.47f);
        }

        void BuildLeaderboardRow(Transform content, int i, string name, long score, bool isOwn, float rowH)
        {
            var row = new GameObject("Row" + i, typeof(RectTransform));
            row.transform.SetParent(content, false);
            var rowRt = row.GetComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0f, 1f);
            rowRt.anchorMax = new Vector2(1f, 1f);
            rowRt.pivot = new Vector2(0.5f, 1f);
            rowRt.sizeDelta = new Vector2(0f, rowH);
            rowRt.anchoredPosition = new Vector2(0f, -i * rowH);

            // Thanh nền bo tròn (vẽ thủ tục, không lấy từ PNG có nền trắng).
            var barGO = Ui.Panel(row.transform, "Bar", LbBarColor(i));
            var barImg = barGO.GetComponent<Image>();
            barImg.sprite = GetRoundedBarSprite();
            barImg.type = Image.Type.Sliced;
            barImg.raycastTarget = false;
            var barRt = barGO.GetComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0.02f, 0.09f);
            barRt.anchorMax = new Vector2(0.98f, 0.91f);
            barRt.offsetMin = barRt.offsetMax = Vector2.zero;

            bool top3 = i < 3;
            var txtColor = top3 ? new Color(0.20f, 0.11f, 0.03f) : new Color(1f, 0.98f, 0.92f);

            // Cột HẠNG: huy hiệu top1/2/3 hoặc số hạng.
            if (top3)
            {
                var badge = MakeV3Image(row.transform, "Rank", BXH + "icon-top" + (i + 1) + ".png", new Rect(0.06f, 0.075f, 0.87f, 0.85f), false);
                var baRt = badge.rectTransform;
                baRt.anchorMin = baRt.anchorMax = new Vector2(0.12f, 0.5f);
                baRt.pivot = new Vector2(0.5f, 0.5f);
                baRt.sizeDelta = new Vector2(56f, 56f);
            }
            else
            {
                var rankT = Ui.Text(row.transform, (i + 1).ToString(), RuntimeArt.LoadMenuButtonFont(), 23, txtColor, TextAnchor.MiddleCenter);
                rankT.fontStyle = FontStyle.Bold;
                rankT.raycastTarget = false;
                var raRt = rankT.rectTransform;
                raRt.anchorMin = new Vector2(0.055f, 0f);
                raRt.anchorMax = new Vector2(0.185f, 1f);
                raRt.offsetMin = raRt.offsetMax = Vector2.zero;
            }

            // Avatar: khung chân dung (input-number) + đầu nhân vật đặt trong khung.
            var avFrame = MakeV3Image(row.transform, "AvatarFrame", "popup-1vs1/input-number.png", new Rect(0.309f, 0.237f, 0.382f, 0.532f), false);
            avFrame.preserveAspect = false;
            avFrame.raycastTarget = false;
            var afRt = avFrame.rectTransform;
            afRt.anchorMin = afRt.anchorMax = new Vector2(0.35f, 0.5f);
            afRt.pivot = new Vector2(0.5f, 0.5f);
            afRt.sizeDelta = new Vector2(42f, 42f);

            var avatar = MakeV3Image(row.transform, "Avatar", "character/" + LbAvatars[i % LbAvatars.Length] + "_09_icon.png", new Rect(0.04f, 0.05f, 0.92f, 0.90f), false);
            var avRt = avatar.rectTransform;
            avRt.anchorMin = avRt.anchorMax = new Vector2(0.35f, 0.5f);
            avRt.pivot = new Vector2(0.5f, 0.5f);
            avRt.sizeDelta = new Vector2(34f, 34f);

            // Tên người chơi.
            var nameT = Ui.Text(row.transform, name, RuntimeArt.LoadMenuButtonFont(), 22, txtColor, TextAnchor.MiddleLeft);
            nameT.fontStyle = FontStyle.Bold;
            nameT.raycastTarget = false;
            nameT.horizontalOverflow = HorizontalWrapMode.Overflow;
            nameT.verticalOverflow = VerticalWrapMode.Overflow;
            var nmRt = nameT.rectTransform;
            nmRt.anchorMin = new Vector2(0.415f, 0f);
            nmRt.anchorMax = new Vector2(0.73f, 1f);
            nmRt.offsetMin = nmRt.offsetMax = Vector2.zero;

            // Cột CÚP: icon cúp + số.
            var cup = MakeV3Image(row.transform, "Cup", BXH + "icon-cup.png", new Rect(0.135f, 0.051f, 0.730f, 0.897f), false);
            var cuRt = cup.rectTransform;
            cuRt.anchorMin = cuRt.anchorMax = new Vector2(0.785f, 0.5f);
            cuRt.pivot = new Vector2(0.5f, 0.5f);
            cuRt.sizeDelta = new Vector2(32f, 32f);

            var scoreT = Ui.Text(row.transform, FormatLbScore(score), RuntimeArt.LoadMenuButtonFont(), 22, txtColor, TextAnchor.MiddleLeft);
            scoreT.fontStyle = FontStyle.Bold;
            scoreT.raycastTarget = false;
            scoreT.horizontalOverflow = HorizontalWrapMode.Overflow;
            scoreT.verticalOverflow = VerticalWrapMode.Overflow;
            var scRt = scoreT.rectTransform;
            scRt.anchorMin = new Vector2(0.835f, 0f);
            scRt.anchorMax = new Vector2(0.99f, 1f);
            scRt.offsetMin = scRt.offsetMax = Vector2.zero;
        }

        // Popup 1 vs 1 online — dựng theo thiết kế popup-1vs1 (ONLINE / JOIN ROOM):
        // panel gỗ-xanh có sẵn tiêu đề + "ENTER ROOM CODE", 4 ô nhập mã, nút JOIN ROOM (xanh)
        // và QUICK MATCH (vàng), nút X đỏ góc phải, hai nhân vật xanh/đỏ hai bên.
        // Vẫn giữ Tạo phòng + Xem thử dạng nút phụ nhỏ dưới panel để không mất tính năng.
        const string V1V1 = "popup-1vs1/";

        void ShowMultiplayerOverlay(Transform parent)
        {
            if (mapOverlay != null) Destroy(mapOverlay);
            // Nền mờ phủ TOÀN màn hình và nổi lên trên mọi thứ (logo/nền phía sau) để
            // popup tách hẳn khỏi nền. Gắn vào canvas gốc + overrideSorting cao.
            var rootCanvas = parent.GetComponentInParent<Canvas>();
            Transform overlayParent = rootCanvas != null ? rootCanvas.rootCanvas.transform : parent;
            mapOverlay = Ui.Panel(overlayParent, "Multiplayer Overlay", new Color(0, 0, 0, 0.99f));
            Ui.Stretch(mapOverlay);
            var ovCanvas = mapOverlay.AddComponent<Canvas>();
            ovCanvas.overrideSorting = true;
            ovCanvas.sortingOrder = 5000;
            mapOverlay.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Panel nền (đã bao sẵn tiêu đề ONLINE/JOIN ROOM và chữ ENTER ROOM CODE).
            float boxW = 920f, boxH = boxW / 1.509f;
            var box = Ui.Panel(mapOverlay.transform, "Multiplayer Box", Color.white);
            Ui.Rect(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(boxW, boxH));
            var boxImg = box.GetComponent<Image>();
            var panelSpr = RuntimeArt.LoadV3SubSprite(V1V1 + "panel-bg.png", new Rect(0.054f, 0.066f, 0.865f, 0.859f));
            if (panelSpr != null) { boxImg.sprite = panelSpr; boxImg.type = Image.Type.Simple; boxImg.preserveAspect = false; boxImg.color = Color.white; }

            // Nhân vật hai bên (đè lên mép panel, đứng trên bệ đá).
            BuildOnlineCharacter(box.transform, V1V1 + "decor-characterxanh.png", new Rect(0.230f, 0.089f, 0.550f, 0.803f), new Vector2(0.0f, 0.25f), new Vector2(56f, 0f));
            BuildOnlineCharacter(box.transform, V1V1 + "decor-characterdo.png", new Rect(0.273f, 0.114f, 0.526f, 0.780f), new Vector2(1.0f, 0.25f), new Vector2(-56f, 0f));

            // 4 ô nhập mã phòng (dịch nhẹ sang phải cho cân với ô beige).
            var codeInput = BuildCodeBoxes(box.transform, new Vector2(0.528f, 0.47f), 92f, 116f);

            // Dòng trạng thái/thông báo (giữa ô mã và hàng nút).
            var statusLabel = Ui.Text(box.transform, "", font, 24, new Color(0.36f, 0.24f, 0.14f), TextAnchor.MiddleCenter);
            statusLabel.fontStyle = FontStyle.Bold;
            statusLabel.raycastTarget = false;
            Ui.Rect(statusLabel, new Vector2(0.5f, 0.345f), new Vector2(0.5f, 0.345f), new Vector2(620, 70));

            // Nút JOIN ROOM (xanh) + QUICK MATCH (vàng).
            var joinBtn = BuildOnlineActionButton(box.transform, V1V1 + "btn-join.png", new Rect(0.137f, 0.332f, 0.750f, 0.389f),
                "JOIN\nROOM", new Vector2(0.5f, 0.245f), new Vector2(-120f, 0f), new Vector2(240, 83), new Color(1f, 0.99f, 0.96f));
            var quickBtn = BuildOnlineActionButton(box.transform, V1V1 + "btn-quickjoin.png", new Rect(0.169f, 0.370f, 0.703f, 0.300f),
                "QUICK\nMATCH", new Vector2(0.5f, 0.245f), new Vector2(136f, 0f), new Vector2(240, 83), new Color(0.30f, 0.17f, 0.03f));

            // Nút X đỏ góc phải trên, nằm ngoài panel.
            var closeBtn = Ui.Button(box.transform, "", font, 1, () =>
            {
                RuntimeArt.PlayUiSwitchSound();
                var manager = MultiplayerManager.Instance;
                if (manager != null && manager.InSession)
                    _ = manager.LeaveAsync(); // hủy phòng đang chờ
                Destroy(mapOverlay);
            });
            var cbRt = closeBtn.GetComponent<RectTransform>();
            cbRt.anchorMin = cbRt.anchorMax = new Vector2(1.0f, 0.76f);
            cbRt.pivot = new Vector2(0.5f, 0.5f);
            cbRt.sizeDelta = new Vector2(96, 96);
            cbRt.anchoredPosition = new Vector2(-100f, 0f);
            var cbImg = closeBtn.GetComponent<Image>();
            var closeSpr = RuntimeArt.LoadV3SubSprite(V1V1 + "btn-close.png", new Rect(0.309f, 0.254f, 0.382f, 0.560f));
            if (closeSpr != null) { cbImg.sprite = closeSpr; cbImg.type = Image.Type.Simple; cbImg.preserveAspect = true; cbImg.color = Color.white; }
            AddPressScaleFeedback(closeBtn.gameObject, 0.9f);

            // Nút phụ nhỏ dưới panel: Tạo phòng (host + mã rủ bạn) và Xem thử giao diện.
            var (createBtn, _) = AddMenuButton(box.transform, "TẠO PHÒNG", new Vector2(0.5f, 0.0f), new Vector2(0f, -66f), () => { }, new Vector2(300, 60), 24);
            AddMenuButton(box.transform, "XEM THỬ GIAO DIỆN", new Vector2(0.5f, 0.0f), new Vector2(0f, -134f), () =>
            {
                RuntimeArt.PlayUiSwitchSound();
                MultiplayerMatch.Begin(1, UnityEngine.Random.Range(1, 999999));
                MultiplayerMatch.Preview = true;
                MultiplayerMatch.OpponentName = "Đối thủ (thử)";
                MultiplayerMatch.OpponentHealth = OnlineConfig.MaxHealth;
                GameSession.SelectedLevel = 1;
                GameSession.JourneyLevel = 1;
                SceneManager.LoadScene("BrickGame");
            }, new Vector2(300, 54), 22);

            var buttons = new[] { quickBtn, createBtn, joinBtn };
            quickBtn.onClick.AddListener(() =>
            {
                RuntimeArt.PlayUiSwitchSound();
                QuickMatch(box, statusLabel, buttons);
            });
            createBtn.onClick.AddListener(() =>
            {
                RuntimeArt.PlayUiSwitchSound();
                CreateRoom(box, codeInput, statusLabel, buttons);
            });
            joinBtn.onClick.AddListener(() =>
            {
                RuntimeArt.PlayUiSwitchSound();
                JoinRoom(box, codeInput, statusLabel, buttons);
            });
        }

        Image MakeV3Image(Transform parent, string name, string asset, Rect crop, bool raycast)
        {
            var go = Ui.Panel(parent, name, Color.white);
            var img = go.GetComponent<Image>();
            var spr = RuntimeArt.LoadV3SubSprite(asset, crop);
            if (spr != null) { img.sprite = spr; img.type = Image.Type.Simple; }
            img.preserveAspect = true;
            img.raycastTarget = raycast;
            return img;
        }

        void BuildOnlineCharacter(Transform parent, string asset, Rect crop, Vector2 anchor, Vector2 offset)
        {
            var img = MakeV3Image(parent, "Online Char", asset, crop, false);
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(300f, 300f);
            rt.anchoredPosition = offset;
        }

        Button BuildOnlineActionButton(Transform parent, string asset, Rect crop, string label, Vector2 anchor, Vector2 offset, Vector2 size, Color textColor)
        {
            var btn = Ui.Button(parent, "", font, 1, () => { });
            var rt = btn.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = offset;
            var img = btn.GetComponent<Image>();
            var spr = RuntimeArt.LoadV3SubSprite(asset, crop);
            // preserveAspect=false: lấp đầy khung để hai nút (2 sprite lệch tỉ lệ) hiện bằng nhau.
            if (spr != null) { img.sprite = spr; img.type = Image.Type.Simple; img.preserveAspect = false; img.color = Color.white; }

            // Chữ nằm ở phần bên phải nút (biểu tượng đã chiếm ~1/3 bên trái).
            var txt = Ui.Text(btn.transform, label, RuntimeArt.LoadMenuButtonFont(), (int)(size.y * 0.255f), textColor, TextAnchor.MiddleCenter);
            txt.fontStyle = FontStyle.Bold;
            txt.raycastTarget = false;
            txt.lineSpacing = 0.82f;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            var lrt = txt.rectTransform;
            lrt.anchorMin = new Vector2(0.32f, 0f);
            lrt.anchorMax = new Vector2(0.97f, 1f);
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            AddDarkWoodTextEdge(txt, 0.6f, 0.45f);
            AddPressScaleFeedback(btn.gameObject, 0.94f);
            return btn;
        }

        // 4 ô nhập mã: mỗi ô là sprite input-number với 1 chữ số; một InputField ẩn bắt phím,
        // onValueChanged đổ từng chữ số vào 4 ô. Bấm bất kỳ ô nào cũng focus để gõ.
        InputField BuildCodeBoxes(Transform parent, Vector2 rowAnchor, float boxSize, float spacing)
        {
            var row = new GameObject("Code Row", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            row.transform.SetParent(parent, false);
            var rowRt = row.GetComponent<RectTransform>();
            rowRt.anchorMin = rowRt.anchorMax = rowAnchor;
            rowRt.pivot = new Vector2(0.5f, 0.5f);
            rowRt.sizeDelta = new Vector2(spacing * 3f + boxSize, boxSize);
            rowRt.anchoredPosition = Vector2.zero;
            var rowImg = row.GetComponent<Image>();
            rowImg.color = new Color(0, 0, 0, 0); // vùng bắt chạm, trong suốt

            var boxTexts = new Text[4];
            float startX = -spacing * 1.5f;
            for (int i = 0; i < 4; i++)
            {
                var b = MakeV3Image(row.transform, "Code Box " + i, V1V1 + "input-number.png", new Rect(0.309f, 0.237f, 0.382f, 0.532f), false);
                var brt = b.rectTransform;
                brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
                brt.pivot = new Vector2(0.5f, 0.5f);
                brt.sizeDelta = new Vector2(boxSize, boxSize);
                brt.anchoredPosition = new Vector2(startX + spacing * i, 0f);

                var t = Ui.Text(b.transform, "", font, (int)(boxSize * 0.62f), new Color(1f, 0.97f, 0.86f), TextAnchor.MiddleCenter);
                t.fontStyle = FontStyle.Bold;
                t.raycastTarget = false;
                Ui.Stretch(t.gameObject);
                AddDarkWoodTextEdge(t, 0.6f, 0.5f);
                boxTexts[i] = t;
            }

            // Text ẩn phục vụ InputField (không hiển thị caret riêng).
            var hidden = Ui.Text(row.transform, "", font, 12, new Color(0, 0, 0, 0), TextAnchor.MiddleCenter);
            Ui.Stretch(hidden.gameObject);
            hidden.raycastTarget = false;
            hidden.supportRichText = false;

            var input = row.AddComponent<InputField>();
            input.textComponent = hidden;
            input.targetGraphic = rowImg;
            input.characterLimit = 4;
            input.contentType = InputField.ContentType.IntegerNumber;
            input.onValueChanged.AddListener(v =>
            {
                for (int i = 0; i < 4; i++)
                    boxTexts[i].text = i < v.Length ? v[i].ToString() : "";
            });
            return input;
        }

        static void SetButtonsInteractable(Button[] buttons, bool value)
        {
            foreach (var button in buttons)
                if (button != null)
                    button.interactable = value;
        }

        Coroutine statusDotsRoutine;

        // Chấm động "Đang tạo phòng." → ".." → "..." trong lúc chờ dịch vụ Unity
        // (tạo phòng mất vài giây do lobby + relay — cho người chơi thấy game còn sống).
        void StartStatusDots(Text label, string baseText)
        {
            StopStatusDots();
            statusDotsRoutine = StartCoroutine(AnimateStatusDots(label, baseText));
        }

        void StopStatusDots()
        {
            if (statusDotsRoutine != null)
            {
                StopCoroutine(statusDotsRoutine);
                statusDotsRoutine = null;
            }
        }

        System.Collections.IEnumerator AnimateStatusDots(Text label, string baseText)
        {
            int tick = 0;
            while (label != null)
            {
                label.text = baseText + new string('.', 1 + tick % 3);
                tick++;
                yield return new WaitForSecondsRealtime(0.35f);
            }
        }

        async void QuickMatch(GameObject box, Text statusLabel, Button[] buttons)
        {
            SetButtonsInteractable(buttons, false);
            StartStatusDots(statusLabel, "Đang tìm đối thủ");
            try
            {
                var manager = MultiplayerManager.Ensure();
                bool joined = await manager.QuickMatchAsync();
                StopStatusDots();
                if (box == null) return; // popup đã đóng
                statusLabel.text = joined
                    ? "Đã tìm thấy đối thủ!\nĐang vào trận..."
                    : "Chưa có ai đang chờ.\nĐã mở phòng chờ — sẽ vào trận ngay khi có người!";
                // Trận tự bắt đầu qua READY/START khi hai bên kết nối.
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Multiplayer] Ghép nhanh thất bại: {e.Message}");
                StopStatusDots();
                if (box == null) return;
                statusLabel.text = "Không ghép được trận.\nKiểm tra kết nối mạng rồi thử lại.";
                SetButtonsInteractable(buttons, true);
            }
        }

        async void CreateRoom(GameObject box, InputField codeInput, Text statusLabel, Button[] buttons)
        {
            SetButtonsInteractable(buttons, false);
            StartStatusDots(statusLabel, "Đang tạo phòng");
            try
            {
                var manager = MultiplayerManager.Ensure();
                string code = await manager.CreateRoomAsync();
                StopStatusDots();
                if (box == null) return; // popup đã đóng
                if (codeInput != null) codeInput.text = code; // đổ mã vào 4 ô
                statusLabel.text = "Gửi mã này cho bạn bè.\nĐang chờ đối thủ vào...";
                // Khi đối thủ kết nối, host tự bắt đầu trận và load scene — không cần gì thêm.
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Multiplayer] Không tạo được phòng: {e.Message}");
                StopStatusDots();
                if (box == null) return;
                statusLabel.text = "Không tạo được phòng.\nKiểm tra kết nối mạng rồi thử lại.";
                SetButtonsInteractable(buttons, true);
            }
        }

        async void JoinRoom(GameObject box, InputField codeInput, Text statusLabel, Button[] buttons)
        {
            string code = codeInput.text != null ? codeInput.text.Trim() : "";
            if (code.Length != 4 || !int.TryParse(code, out _))
            {
                statusLabel.text = "Mã phòng gồm đúng 4 chữ số.";
                return;
            }

            SetButtonsInteractable(buttons, false);
            StartStatusDots(statusLabel, "Đang tìm phòng " + code);
            try
            {
                var manager = MultiplayerManager.Ensure();
                await manager.JoinRoomAsync(code);
                StopStatusDots();
                if (box == null) return;
                statusLabel.text = "Đã vào phòng!\nĐang chờ trận bắt đầu...";
                // Host sẽ gửi START ngay khi thấy mình kết nối → scene tự load.
            }
            catch (InvalidOperationException e)
            {
                StopStatusDots();
                if (box == null) return;
                statusLabel.text = e.Message; // "Không tìm thấy phòng XXXX..."
                SetButtonsInteractable(buttons, true);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Multiplayer] Không vào được phòng: {e.Message}");
                StopStatusDots();
                if (box == null) return;
                statusLabel.text = "Không vào được phòng.\nKiểm tra kết nối mạng rồi thử lại.";
                SetButtonsInteractable(buttons, true);
            }
        }

        void AddPressScaleFeedback(GameObject target, float pressedScale)
        {
            var feedback = target.GetComponent<PressScaleFeedback>() ?? target.AddComponent<PressScaleFeedback>();
            feedback.PressedScale = pressedScale;
        }

        (Button btn, GameObject shadow) AddMenuButton(Transform parent, string label, Vector2 anchor, Vector2 offset, UnityEngine.Events.UnityAction action)
        {
            return AddMenuButton(parent, label, anchor, offset, action, new Vector2(420, 68), 27);
        }

        (Button btn, GameObject shadow) AddMenuButton(Transform parent, string label, Vector2 anchor, Vector2 offset, UnityEngine.Events.UnityAction action, Vector2 size, int fontSize)
        {
            bool primaryButton = label.Contains("BẮT");
            var shadowColor = primaryButton
                ? new Color(0.05f, 0.018f, 0.008f, 0.72f)
                : new Color(0.07f, 0.03f, 0.015f, 0.45f);
            var shadowPad = primaryButton ? new Vector2(20, 14) : new Vector2(8, 6);
            var shadowDy = primaryButton ? -7f : -4f;
            var btnShadow = Ui.Panel(parent, label + " Shadow", shadowColor);
            Ui.Rect(btnShadow, anchor, anchor, size + shadowPad);
            btnShadow.GetComponent<RectTransform>().anchoredPosition = offset + new Vector2(0, shadowDy);

            var button = Ui.Button(parent, label, font, 24, action);
            Ui.Rect(button.gameObject, anchor, anchor, size);
            button.GetComponent<RectTransform>().anchoredPosition = offset;
            StyleWoodRectButton(button, fontSize);
            if (primaryButton)
                button.GetComponent<Image>().color = new Color(1f, 0.96f, 0.78f, 1f);
            AddPressScaleFeedback(button.gameObject, 0.96f);
            return (button, btnShadow);
        }

        void ShowTutorialOverlay(Transform parent)
        {
            if (mapOverlay != null) Destroy(mapOverlay);
            mapOverlay = Ui.Panel(parent, "Tutorial Overlay", new Color(0, 0, 0, 0.72f));
            Ui.Stretch(mapOverlay);

            var box = Ui.Panel(mapOverlay.transform, "Tutorial Box", Color.white);
            Ui.Rect(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(680, 820));
            StyleWoodPopupFrame(box);

            var closeBtn = Ui.Button(box.transform, "", font, 1, () =>
            {
                RuntimeArt.PlayUiSwitchSound();
                Destroy(mapOverlay);
            });
            Ui.Rect(closeBtn.gameObject, new Vector2(0.118f, 0.892f), new Vector2(0.118f, 0.892f), new Vector2(60, 60));
            StyleMapBackButton(closeBtn);

            var titleLabel = Ui.Text(box.transform, "HƯỚNG DẪN", font, 58, new Color(1f, 0.84f, 0.50f), TextAnchor.MiddleCenter);
            titleLabel.fontStyle = FontStyle.Bold;
            Ui.Rect(titleLabel, new Vector2(0.5f, 0.886f), new Vector2(0.5f, 0.886f), new Vector2(500, 108));
            AddDarkWoodTextEdge(titleLabel, 1.0f, 0.86f);
            AddWarmTitleFinish(titleLabel, 0.48f);

            var topSep = Ui.Panel(box.transform, "TopSep", new Color(0.80f, 0.48f, 0.24f, 0.58f));
            Ui.Rect(topSep, new Vector2(0.5f, 0.822f), new Vector2(0.5f, 0.822f), new Vector2(620, 4));

            int totalPages = 5;
            var pageAccentColors = new Color[]
            {
                new Color(0.38f, 0.72f, 1.00f, 1f),
                new Color(0.42f, 0.94f, 0.58f, 1f),
                new Color(1.00f, 0.84f, 0.30f, 1f),
                new Color(1.00f, 0.52f, 0.38f, 1f),
                new Color(0.82f, 0.56f, 1.00f, 1f)
            };
            var pageTitles = new[] { "Xếp gạch", "Giành lượt đi", "Thắng bàn cờ", "Đấu 1 vs 1", "Đạn rác 1 vs 1" };
            var pageContents = new[]
            {
                "Kéo khối gạch sang trái hoặc phải\nđể căn vị trí chính xác.\n\nXoay khối cho khớp khoảng trống.\n\nXếp kín hàng ngang để phá dòng.",
                "Mỗi hàng phá được = 1 lượt đi.\n\nPhá nhiều hàng cùng lúc\n→ càng nhiều lượt đi.\n\nDùng lượt đi để đi quân\ntrên bàn cờ phía trên.",
                "Dùng lượt đi để di chuyển\nquân của bạn (màu xanh).\n\nBạn đi 1 ô → địch (đỏ) chạy 1 ô,\nquái (tím) đuổi theo 2 ô.\n\nDụ quái bắt được địch → THẮNG.\nĐể quái bắt bạn → THUA.",
                "GHÉP NHANH với người lạ, hoặc\nTẠO PHÒNG lấy mã 4 số gửi bạn bè.\n\nHai người chơi cùng màn,\ncùng thứ tự khối gạch.\n\nAi xong bàn cờ trước → THẮNG.",
                "Phá 3 hàng cùng lúc nạp 1 viên rác,\n4 hàng nạp 2 viên (giữ tối đa 3).\n\nBấm nút RÁC để thả một hàng rác\nsang bàn của đối thủ.\n\nCanh bàn mini đối thủ mà bắn!"
            };

            var pages = new GameObject[totalPages];
            var dots = new Image[totalPages];

            for (int i = 0; i < totalPages; i++)
            {
                int idx = i;
                var page = Ui.Panel(box.transform, "TutPage" + i, new Color(0, 0, 0, 0));
                Ui.Rect(page, new Vector2(0.5f, 0.502f), new Vector2(0.5f, 0.502f), new Vector2(620, 480));
                page.SetActive(i == 0);
                pages[i] = page;

                var accentColor = pageAccentColors[idx];
                var numBg = Ui.Panel(page.transform, "NumBg", new Color(accentColor.r, accentColor.g, accentColor.b, 0.18f));
                Ui.Rect(numBg, new Vector2(0.5f, 0.876f), new Vector2(0.5f, 0.876f), new Vector2(70, 70));

                var numLabel = Ui.Text(page.transform, (idx + 1).ToString(), font, 46, accentColor, TextAnchor.MiddleCenter);
                numLabel.fontStyle = FontStyle.Bold;
                Ui.Rect(numLabel, new Vector2(0.5f, 0.876f), new Vector2(0.5f, 0.876f), new Vector2(70, 70));
                AddDarkWoodTextEdge(numLabel, 0.6f, 0.85f);

                var pTitle = Ui.Text(page.transform, pageTitles[idx], font, 34, new Color(1f, 0.88f, 0.60f), TextAnchor.MiddleCenter);
                pTitle.fontStyle = FontStyle.Bold;
                Ui.Rect(pTitle, new Vector2(0.5f, 0.736f), new Vector2(0.5f, 0.736f), new Vector2(560, 52));
                AddDarkWoodTextEdge(pTitle, 0.9f, 0.86f);

                var pLine = Ui.Panel(page.transform, "PLine", new Color(accentColor.r, accentColor.g, accentColor.b, 0.55f));
                Ui.Rect(pLine, new Vector2(0.5f, 0.672f), new Vector2(0.5f, 0.672f), new Vector2(340, 4));

                var bodyLabel = Ui.Text(page.transform, pageContents[idx], font, 26, new Color(1f, 0.91f, 0.74f), TextAnchor.MiddleCenter);
                Ui.Rect(bodyLabel, new Vector2(0.5f, 0.348f), new Vector2(0.5f, 0.348f), new Vector2(560, 290));
                AddDarkWoodTextEdge(bodyLabel, 0.65f, 0.74f);
            }

            for (int i = 0; i < totalPages; i++)
            {
                var dot = Ui.Panel(box.transform, "Dot" + i, Color.white);
                Ui.Rect(dot, new Vector2(0.5f + (i - 2) * 0.072f, 0.112f), new Vector2(0.5f + (i - 2) * 0.072f, 0.112f), new Vector2(18, 18));
                dots[i] = dot.GetComponent<Image>();
                dots[i].color = i == 0 ? new Color(1f, 0.78f, 0.36f, 1f) : new Color(0.58f, 0.36f, 0.14f, 0.55f);
            }

            int[] cur = { 0 };
            System.Action<int> goTo = null;
            goTo = newIdx =>
            {
                if (newIdx < 0 || newIdx >= totalPages) return;
                pages[cur[0]].SetActive(false);
                dots[cur[0]].color = new Color(0.58f, 0.36f, 0.14f, 0.55f);
                cur[0] = newIdx;
                pages[cur[0]].SetActive(true);
                dots[cur[0]].color = new Color(1f, 0.78f, 0.36f, 1f);
            };

            var prevBtn = Ui.Button(box.transform, "<", font, 34, () => { RuntimeArt.PlayUiSwitchSound(); goTo(cur[0] - 1); });
            Ui.Rect(prevBtn.gameObject, new Vector2(0.152f, 0.112f), new Vector2(0.152f, 0.112f), new Vector2(64, 64));
            StyleWoodRectButton(prevBtn, 32);

            var nextBtn = Ui.Button(box.transform, ">", font, 34, () => { RuntimeArt.PlayUiSwitchSound(); goTo(cur[0] + 1); });
            Ui.Rect(nextBtn.gameObject, new Vector2(0.848f, 0.112f), new Vector2(0.848f, 0.112f), new Vector2(64, 64));
            StyleWoodRectButton(nextBtn, 32);
        }

        void StyleMapBackButton(Button button)
        {
            var background = button.GetComponent<Image>();
            background.color = new Color(1f, 1f, 1f, 0f);
            background.sprite = null;
            background.type = Image.Type.Simple;

            var icon = Ui.Panel(button.transform, "Back Arrow Icon", Color.white).GetComponent<Image>();
            icon.sprite = RuntimeArt.CreateBackArrowSprite();
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.color = new Color(1f, 0.84f, 0.48f, 1f);
            Ui.Rect(icon, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(34, 34));

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.88f, 0.58f, 1f);
            colors.pressedColor = new Color(0.78f, 0.46f, 0.20f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
        }

        void AddDarkWoodTextEdge(Text text, float thickness, float alpha)
        {
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.13f, 0.050f, 0.014f, alpha);
            outline.effectDistance = new Vector2(thickness, thickness);
            outline.useGraphicAlpha = true;

            var depth = text.gameObject.AddComponent<Shadow>();
            depth.effectColor = new Color(0.045f, 0.016f, 0.005f, 0.62f);
            depth.effectDistance = new Vector2(0.75f, -0.85f);
            depth.useGraphicAlpha = true;
        }

        void AddWarmTitleFinish(Text text, float glowStrength)
        {
            text.fontStyle = FontStyle.Bold;

            var topGlow = text.gameObject.AddComponent<Shadow>();
            topGlow.effectColor = new Color(1f, 0.68f, 0.30f, 0.26f * glowStrength);
            topGlow.effectDistance = new Vector2(-0.55f, 0.65f);
            topGlow.useGraphicAlpha = true;

            var carvedDrop = text.gameObject.AddComponent<Shadow>();
            carvedDrop.effectColor = new Color(0.035f, 0.012f, 0.004f, 0.78f);
            carvedDrop.effectDistance = new Vector2(0.8f, -0.9f);
            carvedDrop.useGraphicAlpha = true;
        }

        void StyleWoodRectButton(Button button, int fontSize)
        {
            var image = button.GetComponent<Image>();
            image.sprite = RuntimeArt.CreateWoodButtonSprite();
            image.type = Image.Type.Sliced;
            image.color = Color.white;

            var text = button.GetComponentInChildren<Text>();
            text.color = new Color(1f, 0.90f, 0.68f);
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.resizeTextMinSize = Mathf.Min(16, fontSize);
            text.resizeTextMaxSize = fontSize;
            AddDarkWoodTextEdge(text, 0.95f, 0.88f);

            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.055f, 0.018f, 0.006f, 0.58f);
            shadow.effectDistance = new Vector2(0.8f, -0.9f);

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.92f, 0.74f, 1f);
            colors.pressedColor = new Color(0.78f, 0.52f, 0.30f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;
        }

        void StyleWoodPopupFrame(GameObject panel)
        {
            var image = panel.GetComponent<Image>();
            image.sprite = RuntimeArt.CreateWoodPanelSprite();
            image.type = Image.Type.Sliced;
            image.color = Color.white;
        }

        // ── V3.0 menu helpers ────────────────────────────────────────────────

        (Button btn, GameObject shadow) AddV3MainButton(Transform parent, string label, string spriteAsset,
            Vector2 anchor, Vector2 size, int fontSize, UnityEngine.Events.UnityAction action, float leftInsetFrac = 0.24f)
        {
            var button = Ui.Button(parent, label, font, fontSize, action);
            Ui.Rect(button.gameObject, anchor, anchor, size);

            var bg = button.GetComponent<Image>();
            var spr = RuntimeArt.LoadV3Sprite(spriteAsset);
            if (spr != null) { bg.sprite = spr; bg.type = Image.Type.Simple; bg.preserveAspect = true; bg.color = Color.white; }
            else bg.color = new Color(0.72f, 0.48f, 0.14f);

            var txt = button.GetComponentInChildren<Text>();
            if (txt != null)
            {
                txt.font = RuntimeArt.LoadMenuButtonFont();
                txt.color = new Color(1f, 0.97f, 0.84f);
                txt.fontSize = fontSize;
                txt.fontStyle = FontStyle.Bold;
                txt.resizeTextMaxSize = fontSize;
                txt.resizeTextMinSize = Mathf.Max(16, fontSize - 10);
                // Nút notext có icon bên trái → chữ căn giữa trong vùng bên phải sau icon,
                // lề đều 2 bên nên chữ nằm chính giữa khoảng còn lại.
                // vNudge: đẩy chữ lên vì font BTDanta hơi lệch xuống → cân giữa theo chiều dọc.
                const float sideMargin = 0.03f;
                float vNudge = size.y * 0.09f;
                var txtRect = txt.GetComponent<RectTransform>();
                txtRect.offsetMin = new Vector2(size.x * (leftInsetFrac + sideMargin), vNudge);
                txtRect.offsetMax = new Vector2(-size.x * sideMargin, vNudge);
                // Ép 1 dòng: không xuống dòng, để bestfit tự co cho vừa bề ngang.
                txt.horizontalOverflow = HorizontalWrapMode.Overflow;
                AddDarkWoodTextEdge(txt, 1.15f, 0.92f);
                // Viền đen quanh chữ.
                var outline = txt.GetComponent<Outline>();
                if (outline != null)
                {
                    outline.effectColor = Color.black;
                    outline.effectDistance = new Vector2(Mathf.Max(2f, fontSize * 0.06f), Mathf.Max(2f, fontSize * 0.06f));
                }
            }

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.90f, 0.74f, 1f);
            colors.pressedColor = new Color(0.75f, 0.65f, 0.55f, 1f);
            button.colors = colors;

            AddPressScaleFeedback(button.gameObject, 0.96f);
            return (button, null);
        }

        // Nút vuông kiểu btn-bxh: nền bóng + icon (sprite hoặc chữ) phía trên + nhãn phía dưới.
        // Dùng cho hàng 3 nút đều nhau ở menu (HƯỚNG DẪN · 1 VS 1 · BXH).

        Font LoadFont()
        {
            return RuntimeArt.LoadUiFont();
        }
    }

    public class BrickGameController : MonoBehaviour
    {
        const int Width = 10;
        const int Height = 20;
        readonly Color[] palette =
        {
            new Color(0.34f, 0.86f, 0.86f), new Color(0.34f, 0.62f, 0.98f), new Color(1.00f, 0.48f, 0.20f),
            new Color(1.00f, 0.78f, 0.24f), new Color(0.42f, 0.86f, 0.30f), new Color(0.78f, 0.34f, 0.86f),
            new Color(0.96f, 0.24f, 0.20f)
        };

        readonly Vector2Int[][] shapes =
        {
            new [] { new Vector2Int(-1,0), new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0) },
            new [] { new Vector2Int(-1,1), new Vector2Int(-1,0), new Vector2Int(0,0), new Vector2Int(1,0) },
            new [] { new Vector2Int(1,1), new Vector2Int(-1,0), new Vector2Int(0,0), new Vector2Int(1,0) },
            new [] { new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(0,0), new Vector2Int(1,0) },
            new [] { new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(-1,0), new Vector2Int(0,0) },
            new [] { new Vector2Int(0,1), new Vector2Int(-1,0), new Vector2Int(0,0), new Vector2Int(1,0) },
            new [] { new Vector2Int(-1,1), new Vector2Int(0,1), new Vector2Int(0,0), new Vector2Int(1,0) }
        };

        LevelRules rules;
        Font font;
        Font titleFont;
        TMP_FontAsset popupTitleFont;
        Sprite blockSprite;
        Sprite[] pieceBlockSprites;
        Transform boardRoot;
        Transform activeRoot;
        Transform ghostRoot;
        Transform settledRoot;
        Transform fxRoot;
        Camera cam;
        Text scoreText;
        Text levelText;
        Text bestText;
        Text linesText;
        Text opponentText;
        Image[,] opponentMiniCells; // bàn mini của đối thủ (multiplayer)
        RectTransform opponentMiniPanelRect;
        Image[,] opponentTacticalCells; // bàn cờ mini của đối thủ
        RectTransform opponentTacticalPanelRect;
        byte[] boardSnapshot;       // buffer gửi bàn của mình cho đối thủ
        byte[] tacticalSnapshot;    // 6 byte vị trí quân bàn cờ của mình
        float nextBoardSendTime;
        int lastBoardHash;
        float garbageWarnUntil;     // > 0 = đang nhấp nháy cảnh báo rác sắp vào
        Text garbageWarningText;
        float nextAttackTime;       // chống spam: giãn cách giữa hai lần dùng kỹ năng
        Button attackButton;        // = skillGarbageButton (giữ tên cũ cho layout/legacy)
        RectTransform attackButtonRect;

        // Hệ năng lượng/máu 1v1 (design §6-11).
        readonly EnergySystem energySystem = new EnergySystem();
        readonly HealthSystem healthSystem = new HealthSystem();

        // HUD online mới (design #7): thanh máu trên, nhân vật/VS, tấn công/phòng thủ,
        // thanh năng lượng, 3 nút kỹ năng có giá.
        bool onlineHudBuilt;
        // Thanh trên (avatar + tên + cúp + máu + VS).
        RectTransform onlineTopBarRect;
        Image playerAvatarImg, oppAvatarImg;
        Image hpYouFill, hpOppFill;
        Text hpYouText, hpOppText, playerNameText, oppNameText, playerCupText, oppCupText;
        // Cột giữa (nhân vật + 3 lá kỹ năng + TẤN CÔNG/PHÒNG THỦ).
        RectTransform onlineCenterRect;
        RectTransform onlineAtkRect, onlineDefRect;
        Text onlineAtkText, onlineDefText;
        RectTransform[] onlineSkillRect = new RectTransform[3];
        Button[] onlineSkillBtn = new Button[3];
        Text[] onlineSkillCount = new Text[3];
        // Khung bàn + thanh năng lượng dưới mỗi bàn.
        Image playerBoardFrameImg, oppBoardFrameImg;
        RectTransform onlineYouEnergyRect, onlineOppEnergyRect;
        Image[] youEnergySeg = new Image[10];
        Image[] oppEnergySeg = new Image[10];
        Text onlineEnergyText, onlineOppEnergyText;
        readonly Color youEnergyColor = new Color(0.22f, 0.66f, 1f, 1f);
        readonly Color oppEnergyColor = new Color(1f, 0.34f, 0.26f, 1f);
        readonly Color energyOffColor = new Color(0.05f, 0.07f, 0.12f, 0.92f);
        Text statusText;
        Text nextText;
        Text tacticalMovesText;
        Text tacticalStatusText;
        List<Image> nextPreviewCells = new List<Image>();
        List<Image> holdPreviewCells = new List<Image>();
        readonly List<Button> tacticalCellButtons = new List<Button>();
        readonly List<Text> tacticalCellLabels = new List<Text>();
        readonly List<Image> tacticalCellIcons = new List<Image>();
        Button pauseButton;
        Button rotateButton;
        ParticleSystem clearParticles;
        AudioSource audioSource;
        AudioSource musicSource;
        GameObject pauseOverlay;
        GameObject gameOverOverlay;
        GameObject gameLoseOverlay;
        Text gameLoseSubtitle;
        GameObject missionOverlay;
        GameObject levelClearOverlay;
        TMP_Text gameOverTitleText;
        Text gameOverScoreText;
        TMP_Text missionTitleText;
        Text missionBodyText;
        Text missionDescText;
        Text missionStar3CondText;
        Text missionStar2CondText;
        Text missionStar1CondText;
        TMP_Text levelClearTitleText;
        Text levelClearStarsText;
        Text levelClearBodyText;
        Button continueButton;
        Button stopButton;
        Button nextButton;
        Image[] levelClearStarImgs;
        Vector3 cameraHome;
        RectTransform safeAreaRoot;
        RectTransform hudPanelRect;
        RectTransform hudShadowRect;
        RectTransform holdWidgetRect;
        RectTransform holdWidgetShadowRect;
        RectTransform nextWidgetRect;
        RectTransform nextWidgetShadowRect;
        RectTransform tacticalWidgetRect;
        RectTransform tacticalWidgetShadowRect;
        RectTransform moveHintPanelRect;
        RectTransform moveHintPanelShadowRect;
        RectTransform pauseButtonRect;
        RectTransform rotateButtonRect;
        Canvas sceneGameplayCanvas;
        bool usingSceneGameplayCanvas;
        RectTransform sceneGameplayRootRect;
        RectTransform sceneMobileGameplayRootRect;
        RectTransform sceneTabletGameplayRootRect;
        RectTransform sceneSafeAreaContainerRect;
        RectTransform sceneContentAreaRect;
        RectTransform sceneBackgroundRect;
        RectTransform sceneHeaderRect;
        RectTransform sceneNextPanelRect;
        RectTransform sceneNextPreviewRect;
        Vector2 sceneGameplayReferenceResolution = new Vector2(1284f, 2778f);
        TMP_Text sceneLevelText;
        TMP_Text sceneMoveText;
        TMP_Text sceneNextText;
        RectTransform scenePuzzleBoardAnchorRect;
        RectTransform sceneTacticalBoardRect;
        Image[,] scenePuzzleCells;
        RectTransform[,] scenePuzzleSlots;
        RectTransform scenePuzzleGridRect;
        bool sceneUsingTabletGameplayRoot;
        float puzzleCellSize;   // local-space cell height (Y axis)
        float puzzleCellSizeX;  // local-space cell width  (X axis)
        float nextPreviewCellSize;
        int lastScreenWidth;
        int lastScreenHeight;
        Rect lastAppliedSafeArea;
        Vector2 gestureStart;
        Vector2 gestureLastPosition;
        float gestureStartTime;
        float gameplayTime;
        bool gestureTracking;
        bool gestureMoved;
        bool gestureMovedHorizontally;
        bool movedHorizontallyThisFrame;

        int[,] grid = new int[Width, Height];
        GameObject[,] lockedBlocks = new GameObject[Width, Height];
        List<GameObject> activeBlocks = new List<GameObject>();
        List<GameObject> ghostBlocks = new List<GameObject>();
        Queue<int> nextBag = new Queue<int>();
        int currentType;
        int holdType = -1;
        bool currentPieceIsSpecial;
        int currentSpecialKind;
        bool canHold;
        Vector2Int origin;
        int rotation;
        float fallTimer;
        float lockDelayTimer;
        bool touchingGround;
        int score;
        int bestScore;
        int lines;
        int levelLines;
        int levelStartScore;
        int journeyLevel;
        int starsEarned;
        int rotationsThisLevel;
        int holdsThisLevel;
        int maxLinesClearedAtOnce;
        int combo;
        int maxComboThisLevel;
        int piecesLocked;
        // Ghost preview is a learning aid: visible only for the first few pieces.
        const int GhostPreviewPieces = 3;
        bool GhostVisible => rules.GhostPreview && piecesLocked < GhostPreviewPieces;
        int lastRisingDangerTick;
        bool gameOver;
        bool paused;
        bool puzzlePausedForTacticalTurn;
        bool resolving;
        float shake;
        TacticalBoardManager tacticalBoard;
        bool tacticalPieceSelected;
        bool tacticalDragTracking;
        Vector2 tacticalDragStart;

        void Start()
        {
            RuntimeArt.ResetTacticalSpriteCache();
            font = RuntimeArt.LoadUiFont();
            titleFont = RuntimeArt.LoadDisplayFont();
            journeyLevel = Mathf.Max(1, GameSession.JourneyLevel);
            GameSession.JourneyLevel = journeyLevel;
            SetupModeRules();
            SetupTacticalBoard();
            bestScore = PlayerPrefs.GetInt(BestScoreKey(), 0);
            blockSprite = RuntimeArt.CreateBlockSprite();
            pieceBlockSprites = RuntimeArt.LoadPieceBlockSprites();
            BuildWorld();
            BuildUi();
            // Trận 1v1 không có khái niệm màn/sao — vào thẳng, khỏi popup nhiệm vụ.
            BeginLevelMission(!MultiplayerMatch.Active);
            // Trận 1v1: cùng seed để hai bên nhận chuỗi khối giống nhau.
            if (MultiplayerMatch.Active)
                UnityEngine.Random.InitState(MultiplayerMatch.Seed);
            FillBag();
            SpawnPiece();
            UpdateUi();
        }

        void SetupModeRules()
        {
            journeyLevel = Mathf.Clamp(GameSession.SelectedLevel > 0 ? GameSession.SelectedLevel : journeyLevel, 1, LevelProgress.MaxLevels);
            rules = LevelRules.CreateJourney(journeyLevel);
        }

        void SetupTacticalBoard()
        {
            if (rules.TacticalData == null)
                rules.TacticalData = TacticalLevelData.Create(Mathf.Max(1, journeyLevel));
            tacticalBoard = new TacticalBoardManager(rules.TacticalData);
        }

        void Update()
        {
            if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight || Screen.safeArea != lastAppliedSafeArea)
                ConfigureResponsiveCamera();

            RefreshSceneHud();

            if (MultiplayerMatch.Active)
            {
                if (!gameOver)
                {
                    CheckOpponentMatchEvents();
                    ProcessIncomingAttacks();
                    CheckMatchTimeLimit();
                }
                ApplyPendingGarbage();
                SendBoardSnapshotIfNeeded();
                if (MultiplayerMatch.OpponentBoardDirty)
                    RepaintOpponentMiniBoard();
            }

            if (gameOver)
            {
                if (KeyPressed(KeyCode.R, Key.R)) Restart();
                if (KeyPressed(KeyCode.Escape, Key.Escape)) BackToMenu();
                return;
            }

            if (KeyPressed(KeyCode.P, Key.P) || KeyPressed(KeyCode.Escape, Key.Escape))
                TogglePause();

            if (paused || resolving)
                return;

            // Offline (design §2.5): quái tự đi theo timer thực, tạo áp lực thời gian
            // ngay cả khi người chơi đang xếp gạch. 1v1 không dùng bàn chiến thuật kiểu này.
            if (!MultiplayerMatch.Active && tacticalBoard != null && tacticalBoard.Status == TacticalBoardStatus.Running)
            {
                if (tacticalBoard.TickMonsterTimer(Time.deltaTime))
                    RefreshTacticalBoardUi();
                if (tacticalBoard.Status != TacticalBoardStatus.Running)
                {
                    OnTacticalStatusResolved();
                    return;
                }
                UpdateMonsterTimerHud();
            }

            if (!puzzlePausedForTacticalTurn)
            {
                movedHorizontallyThisFrame = false;
                HandleInput();
                gameplayTime += Time.deltaTime;
                fallTimer += Time.deltaTime;
                if (fallTimer >= CurrentFallInterval())
                {
                    fallTimer = 0f;
                    StepDown();
                }

                if (touchingGround && !CanMoveDown())
                {
                    lockDelayTimer += Time.deltaTime;
                    if (lockDelayTimer >= 0.4f)
                        LockPiece();
                }
                else if (CanMoveDown())
                {
                    touchingGround = false;
                    lockDelayTimer = 0f;
                }
            }

            UpdateCameraShake();
        }

        void BuildWorld()
        {
            cam = Camera.main ?? FindAnyObjectByType<Camera>();
            GameObject camObject;
            if (cam == null)
            {
                camObject = new GameObject("Main Camera");
                cam = camObject.AddComponent<Camera>();
            }
            else
            {
                camObject = cam.gameObject;
            }

            cam.tag = "MainCamera";
            cam.orthographic = true;
            cam.backgroundColor = new Color(0.33f, 0.15f, 0.055f);
            ConfigureResponsiveCamera();
            if (camObject.GetComponent<AudioListener>() == null)
                camObject.AddComponent<AudioListener>();
            audioSource = gameObject.AddComponent<AudioSource>();
            musicSource = gameObject.AddComponent<AudioSource>();
            StartBackgroundMusic();

            boardRoot = new GameObject("Board").transform;
            settledRoot = new GameObject("Locked Blocks").transform;
            activeRoot = new GameObject("Active Piece").transform;
            ghostRoot = new GameObject("Ghost Piece").transform;
            fxRoot = new GameObject("Effects").transform;

            CreateBackdrop();
            CreateBoardFrame();
            CreateGrid();
            clearParticles = RuntimeArt.CreateLineParticles(fxRoot, rules.BackgroundB);
        }

        void CreateBackdrop()
        {
            RuntimeArt.CreateWoodBackdrop("Warm Wood Backdrop", cam, 1.2f, new Color(0.23f, 0.12f, 0.06f, 0.46f));
        }

        void CreateGrid()
        {
            if (RuntimeArt.HasBoardFrameSprite())
                return;

            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                {
                    var cell = NewBlock("Grid Cell", RuntimeArt.GridColor, boardRoot);
                    cell.transform.position = CellToWorld(x, y);
                    cell.transform.localScale = Vector3.one * 0.94f;
                    cell.GetComponent<SpriteRenderer>().sortingOrder = 0;
                }
        }

        void CreateBoardFrame()
        {
            var boardSprite = RuntimeArt.CreateBoardFrameSprite();
            if (boardSprite != null)
            {
                var boardArt = new GameObject("Wood Board Art");
                boardArt.transform.SetParent(boardRoot);
                boardArt.transform.position = new Vector3(0, 0, 0.35f);
                var renderer = boardArt.AddComponent<SpriteRenderer>();
                renderer.sprite = boardSprite;
                renderer.sortingOrder = -20;

                Vector2 targetSize = new Vector2(11.25f, 21.35f);
                Vector2 spriteSize = boardSprite.bounds.size;
                boardArt.transform.localScale = new Vector3(targetSize.x / spriteSize.x, targetSize.y / spriteSize.y, 1f);
                CreateAlignedPlayfieldOverlay();
                return;
            }

            var stage = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stage.name = "Board Stage";
            stage.transform.SetParent(boardRoot);
            stage.transform.position = new Vector3(0, 0, 0.55f);
            stage.transform.localScale = new Vector3(11.75f, 21.85f, 0.06f);
            stage.GetComponent<MeshRenderer>().sharedMaterial = RuntimeArt.Material(new Color(0.045f, 0.018f, 0.008f, 0.70f));

            var frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frame.name = "Board Frame";
            frame.transform.SetParent(boardRoot);
            frame.transform.position = new Vector3(0, 0, 0.35f);
            frame.transform.localScale = new Vector3(10.35f, 20.35f, 0.08f);
            frame.GetComponent<MeshRenderer>().sharedMaterial = RuntimeArt.Material(new Color(0.12f, 0.065f, 0.035f, 0.95f));
        }

        void CreateAlignedPlayfieldOverlay()
        {
            var playfieldSprite = RuntimeArt.CreateRoundedWoodSprite();
            if (playfieldSprite != null)
            {
                var playfield = new GameObject("Aligned Wood Playfield");
                playfield.transform.SetParent(boardRoot);
                playfield.transform.position = new Vector3(0, 0, 0.18f);
                var renderer = playfield.AddComponent<SpriteRenderer>();
                renderer.sprite = playfieldSprite;
                renderer.color = new Color(0.23f, 0.10f, 0.05f, 0.98f);
                renderer.sortingOrder = -10;
                Vector2 spriteSize = playfieldSprite.bounds.size;
                playfield.transform.localScale = new Vector3(10.05f / spriteSize.x, 20.05f / spriteSize.y, 1f);
            }
            else
            {
                var playfield = GameObject.CreatePrimitive(PrimitiveType.Cube);
                playfield.name = "Aligned Wood Playfield";
                playfield.transform.SetParent(boardRoot);
                playfield.transform.position = new Vector3(0, 0, 0.18f);
                playfield.transform.localScale = new Vector3(10.05f, 20.05f, 0.03f);
                playfield.GetComponent<MeshRenderer>().sharedMaterial = RuntimeArt.Material(new Color(0.23f, 0.10f, 0.05f, 0.98f));
            }

            var verticalLineMaterial = RuntimeArt.Material(new Color(0.105f, 0.047f, 0.020f, 0.92f));
            for (int x = 0; x <= Width; x++)
            {
                var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                line.name = "Aligned Board Vertical Line";
                line.transform.SetParent(boardRoot);
                line.transform.position = new Vector3(x - Width * 0.5f, 0, 0.08f);
                line.transform.localScale = new Vector3(0.035f, Height, 0.02f);
                line.GetComponent<MeshRenderer>().sharedMaterial = verticalLineMaterial;
                line.GetComponent<MeshRenderer>().sortingOrder = -5;
            }

            var horizontalLineMaterial = RuntimeArt.Material(new Color(0.105f, 0.047f, 0.020f, 0.92f));
            for (int y = 0; y <= Height; y++)
            {
                var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
                line.name = "Aligned Board Horizontal Line";
                line.transform.SetParent(boardRoot);
                line.transform.position = new Vector3(0, y - Height * 0.5f, 0.07f);
                line.transform.localScale = new Vector3(Width, 0.035f, 0.02f);
                line.GetComponent<MeshRenderer>().sharedMaterial = horizontalLineMaterial;
                line.GetComponent<MeshRenderer>().sortingOrder = -5;
            }
        }

        void BuildUi()
        {
            if (TryBindSceneGameplayUi())
                return;

            var canvas = Ui.CreateCanvas("Game Canvas");
            var safe = Ui.Panel(canvas.transform, "Safe Area", new Color(0, 0, 0, 0));
            safeAreaRoot = safe.GetComponent<RectTransform>();
            Ui.ApplySafeArea(safeAreaRoot);
            safe.AddComponent<SafeAreaFitter>();

            var hudShadow = Ui.Panel(safe.transform, "Hud Shadow", new Color(0, 0, 0, 0));
            hudShadowRect = hudShadow.GetComponent<RectTransform>();
            Ui.Rect(hudShadow, new Vector2(0.055f, 0.858f), new Vector2(0.790f, 0.968f), new Vector2(0, 0));
            hudShadowRect.anchoredPosition = new Vector2(0, -6);
            hudShadow.GetComponent<Image>().color = new Color(0.025f, 0.008f, 0.002f, 0.72f);

            var hudPanel = Ui.Panel(safe.transform, "Hud Panel", new Color(0, 0, 0, 0));
            hudPanelRect = hudPanel.GetComponent<RectTransform>();
            Ui.Rect(hudPanel, new Vector2(0.055f, 0.866f), new Vector2(0.790f, 0.976f), new Vector2(0, 0));
            var hudImage = hudPanel.GetComponent<Image>();
            hudImage.sprite = RuntimeArt.CreateWoodButtonSprite();
            hudImage.type = Image.Type.Sliced;
            hudImage.color = new Color(0.35f, 0.18f, 0.07f, 0.98f);

            var hudTopAccent = Ui.Panel(hudPanel.transform, "Hud Top Accent", new Color(0, 0, 0, 0));
            Ui.Rect(hudTopAccent, new Vector2(0.035f, 0.89f), new Vector2(0.965f, 0.925f), new Vector2(0, 0));
            hudTopAccent.GetComponent<Image>().color = new Color(0.76f, 0.48f, 0.18f, 0.58f);

            var hudBottomLine = Ui.Panel(hudPanel.transform, "Hud Bottom Line", new Color(0, 0, 0, 0));
            Ui.Rect(hudBottomLine, new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.085f), new Vector2(0, 0));
            hudBottomLine.GetComponent<Image>().color = new Color(0.12f, 0.045f, 0.012f, 0.55f);

            linesText = Ui.Text(hudPanel.transform, "Cấp 1 - Điểm 0", font, 26, new Color(1f, 0.92f, 0.78f), TextAnchor.MiddleLeft);
            linesText.text = "Level: 1";
            Ui.Rect(linesText, new Vector2(0.04f, 0.12f), new Vector2(0.50f, 0.88f), new Vector2(0, 0));
            AddDarkWoodTextEdge(linesText, 0.95f, 0.86f);
            AddWarmTitleFinish(linesText, 0.35f);

            levelText = Ui.Text(hudPanel.transform, "Nhiệm vụ: 0", font, 26, new Color(1f, 0.78f, 0.52f), TextAnchor.MiddleLeft);
            levelText.alignment = TextAnchor.MiddleRight;
            Ui.Rect(levelText, new Vector2(0.50f, 0.12f), new Vector2(0.96f, 0.88f), new Vector2(0, 0));

            if (MultiplayerMatch.Active)
                BuildOpponentMiniBoard(safe.transform); // fallback path (không có scene canvas)

            bestText = Ui.Text(hudPanel.transform, "", font, 31, new Color(1f, 0.72f, 0.32f), TextAnchor.MiddleLeft);
            Ui.Rect(bestText, new Vector2(0.00f, 0.22f), new Vector2(0.98f, 0.58f), new Vector2(0, 0));
            AddDarkWoodTextEdge(bestText, 1.05f, 0.88f);
            AddWarmTitleFinish(bestText, 0.42f);
            bestText.gameObject.SetActive(false);

            scoreText = Ui.Text(hudPanel.transform, "", font, 1, new Color(1f, 1f, 1f, 0f), TextAnchor.MiddleRight);
            Ui.Rect(scoreText, new Vector2(0.96f, 0.92f), new Vector2(0.98f, 0.94f), new Vector2(0, 0));

            pauseButton = Ui.Button(safe.transform, "II", font, 24, () =>
            {
                RuntimeArt.PlayUiSwitchSound();
                TogglePause();
            });
            pauseButtonRect = pauseButton.GetComponent<RectTransform>();
            Ui.Rect(pauseButton.gameObject, new Vector2(0.855f, 0.905f), new Vector2(0.955f, 0.975f), new Vector2(0, 0));
            StylePauseButton(pauseButton);

            rotateButton = Ui.Button(safe.transform, "Xoay", font, 24, () =>
            {
                RuntimeArt.PlayUiSwitchSound();
                RotateFromButton();
            });
            rotateButtonRect = rotateButton.GetComponent<RectTransform>();
            Ui.Rect(rotateButton.gameObject, new Vector2(0.835f, 0.260f), new Vector2(0.945f, 0.340f), new Vector2(0, 0));
            StyleRoundWoodButton(rotateButton, "↻", 26);

            var holdWidgetShadow = Ui.Panel(safe.transform, "Hold Widget Shadow", new Color(0, 0, 0, 0));
            holdWidgetShadowRect = holdWidgetShadow.GetComponent<RectTransform>();
            Ui.Rect(holdWidgetShadow, new Vector2(0.790f, 0.470f), new Vector2(0.960f, 0.640f), new Vector2(0, 0));
            holdWidgetShadowRect.anchoredPosition = new Vector2(0, -5);

            var holdWidget = Ui.Panel(safe.transform, "Hold Widget", new Color(0, 0, 0, 0));
            holdWidgetRect = holdWidget.GetComponent<RectTransform>();
            Ui.Rect(holdWidget, new Vector2(0.790f, 0.477f), new Vector2(0.960f, 0.647f), new Vector2(0, 0));
            var holdButton = holdWidget.AddComponent<Button>();
            holdButton.onClick.AddListener(SwapHoldPiece);

            var holdLabel = Ui.Text(holdWidget.transform, "Giữ", font, 29, new Color(1f, 0.90f, 0.72f), TextAnchor.MiddleCenter);
            StyleSideWidgetTitle(holdLabel);
            Ui.Rect(holdLabel, new Vector2(0.00f, 0.76f), new Vector2(1.00f, 1.00f), new Vector2(0, 0));

            var holdPanel = Ui.Panel(holdWidget.transform, "Hold Piece Panel", new Color(1f, 1f, 1f, 1f));
            Ui.Rect(holdPanel, new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.73f), new Vector2(0, 0));
            ApplyBoardFrameToPreviewPanel(holdPanel.transform);
            var holdPanelButton = holdPanel.AddComponent<Button>();
            holdPanelButton.onClick.AddListener(SwapHoldPiece);
            holdPreviewCells = CreatePiecePreview(holdPanel.transform, new Vector2(0.5f, 0.48f), 15.0f);
            holdWidget.SetActive(false);
            holdWidgetShadow.SetActive(false);

            var nextWidgetShadow = Ui.Panel(safe.transform, "Next Widget Shadow", new Color(0, 0, 0, 0));
            nextWidgetShadowRect = nextWidgetShadow.GetComponent<RectTransform>();
            Ui.Rect(nextWidgetShadow, new Vector2(0.790f, 0.665f), new Vector2(0.960f, 0.835f), new Vector2(0, 0));
            nextWidgetShadowRect.anchoredPosition = new Vector2(0, -5);

            var nextWidget = Ui.Panel(safe.transform, "Next Widget", new Color(0, 0, 0, 0));
            nextWidgetRect = nextWidget.GetComponent<RectTransform>();
            Ui.Rect(nextWidget, new Vector2(0.790f, 0.672f), new Vector2(0.960f, 0.842f), new Vector2(0, 0));

            nextText = Ui.Text(nextWidget.transform, "Tiếp", font, 29, new Color(1f, 0.90f, 0.72f), TextAnchor.MiddleCenter);
            StyleSideWidgetTitle(nextText);
            nextText.text = "TIẾP";
            Ui.Rect(nextText, new Vector2(0.00f, 0.76f), new Vector2(1.00f, 1.00f), new Vector2(0, 0));

            var nextPanel = Ui.Panel(nextWidget.transform, "Next Piece Panel", new Color(1f, 1f, 1f, 1f));
            Ui.Rect(nextPanel, new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.73f), new Vector2(0, 0));
            ApplyBoardFrameToPreviewPanel(nextPanel.transform);
            nextPreviewCells = CreatePiecePreview(nextPanel.transform, new Vector2(0.5f, 0.48f), 15.0f);

            var moveHintShadow = Ui.Panel(safe.transform, "Move Hint Shadow", new Color(0.025f, 0.008f, 0.002f, 0.70f));
            moveHintPanelShadowRect = moveHintShadow.GetComponent<RectTransform>();
            Ui.Rect(moveHintShadow, new Vector2(0.790f, 0.060f), new Vector2(0.960f, 0.190f), new Vector2(0, -5));

            var moveHintPanel = Ui.Panel(safe.transform, "Move Hint Panel", new Color(0, 0, 0, 0));
            moveHintPanelRect = moveHintPanel.GetComponent<RectTransform>();
            Ui.Rect(moveHintPanel, new Vector2(0.790f, 0.067f), new Vector2(0.960f, 0.197f), Vector2.zero);
            var moveHintImage = moveHintPanel.GetComponent<Image>();
            moveHintImage.sprite = RuntimeArt.CreateWoodPanelSprite();
            moveHintImage.type = Image.Type.Sliced;
            moveHintImage.color = new Color(0.35f, 0.18f, 0.07f, 0.96f);

            var moveHintText = Ui.Text(moveHintPanel.transform, "Xóa dòng\nđể nhận\nlượt đi!", font, 17, new Color(1f, 0.88f, 0.62f), TextAnchor.MiddleCenter);
            Ui.Rect(moveHintText, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f), Vector2.zero);
            AddDarkWoodTextEdge(moveHintText, 0.7f, 0.76f);

            BuildTacticalBoardUi(safe.transform);

            statusText = Ui.Text(safe.transform, "", font, 34, Color.white, TextAnchor.MiddleCenter);
            Ui.Rect(statusText, new Vector2(0.12f, 0.34f), new Vector2(0.88f, 0.58f), new Vector2(0, 0));

            pauseOverlay = Ui.Panel(canvas.transform, "Pause Overlay", new Color(0, 0, 0, 0.65f));
            Ui.Stretch(pauseOverlay);
            BuildPausePopup(pauseOverlay.transform);
            pauseOverlay.SetActive(false);

            gameOverOverlay = Ui.Panel(canvas.transform, "Game Over Overlay", new Color(0, 0, 0, 0.68f));
            Ui.Stretch(gameOverOverlay);
            BuildGameOverPopup(gameOverOverlay.transform);
            gameOverOverlay.SetActive(false);

            gameLoseOverlay = Ui.Panel(canvas.transform, "Game Lose Overlay", new Color(0.01f, 0.02f, 0.05f, 0.992f));
            Ui.Stretch(gameLoseOverlay);
            BuildGameLosePopup(gameLoseOverlay.transform);
            gameLoseOverlay.SetActive(false);

            missionOverlay = Ui.Panel(canvas.transform, "Mission Overlay", new Color(0, 0, 0, 0.70f));
            Ui.Stretch(missionOverlay);
            BuildMissionPopup(missionOverlay.transform);
            missionOverlay.SetActive(false);

            levelClearOverlay = Ui.Panel(canvas.transform, "Level Clear Overlay", new Color(0.01f, 0.02f, 0.05f, 0.992f));
            Ui.Stretch(levelClearOverlay);
            BuildLevelClearPopup(levelClearOverlay.transform);
            levelClearOverlay.SetActive(false);

            LayoutGameplayChrome();
        }

        bool TryBindSceneGameplayUi()
        {
            Canvas[] canvases = FindObjectsOfType<Canvas>(true);
            Transform root = null;
            for (int i = 0; i < canvases.Length; i++)
            {
                Transform mobileRoot = FindChildLoose(canvases[i].transform, "GameplayRoot_Mobile");
                Transform tabletRoot = FindChildLoose(canvases[i].transform, "GameplayRoot_Tablet");
                Transform legacyRoot = FindChildLoose(canvases[i].transform, "GameplayRoot");
                root = SelectSceneGameplayRoot(mobileRoot, tabletRoot, legacyRoot);
                if (root != null)
                {
                    sceneGameplayCanvas = canvases[i];
                    sceneMobileGameplayRootRect = mobileRoot as RectTransform;
                    sceneTabletGameplayRootRect = tabletRoot as RectTransform;
                    break;
                }
            }

            if (sceneGameplayCanvas == null || root == null)
                return false;

            usingSceneGameplayCanvas = true;
            ConfigureSceneCanvasScaler(sceneGameplayCanvas);
            sceneGameplayRootRect = root as RectTransform;
            sceneContentAreaRect = EnsureSceneGameplayHierarchy(root, sceneUsingTabletGameplayRoot);
            safeAreaRoot = sceneContentAreaRect != null ? sceneContentAreaRect : sceneGameplayRootRect;
            if (sceneGameplayRootRect != null)
            {
                var oldFitter = sceneGameplayRootRect.GetComponent<RectTransformSafeAreaFitter>();
                if (oldFitter != null)
                    Destroy(oldFitter);
            }
            Transform contentRoot = safeAreaRoot != null ? safeAreaRoot : root;
            sceneBackgroundRect = GetSceneRect(root, "Background");
            sceneHeaderRect = GetSceneRect(contentRoot, "Header");
            sceneNextPanelRect = GetSceneRect(contentRoot, "NextPanel");
            sceneLevelText = FindTmpText(contentRoot, "LevelText");
            sceneMoveText = FindTmpText(contentRoot, "MoveText");
            sceneNextText = FindTmpText(contentRoot, "NextPanel");
            if (sceneNextText != null)
                sceneNextText.text = "TIẾP";

            scoreText = CreateHiddenSceneText(contentRoot, "Runtime Score Mirror");
            linesText = CreateHiddenSceneText(contentRoot, "Runtime Level Mirror");
            levelText = CreateHiddenSceneText(contentRoot, "Runtime Moves Mirror");
            bestText = CreateHiddenSceneText(contentRoot, "Runtime Best Mirror");
            nextText = CreateHiddenSceneText(contentRoot, "Runtime Next Mirror");

            var puzzleAnchor = FindChildLoose(contentRoot, "PuzzleBoardAnchor");
            scenePuzzleBoardAnchorRect = puzzleAnchor != null ? puzzleAnchor.GetComponent<RectTransform>() : null;
            BuildScenePuzzleGrid();

            pauseButton = EnsureSceneButton(FindChildLooseActive(contentRoot, "PauseButton") ?? FindChildInAnyCanvas("PauseButton"), TogglePause);
            pauseButtonRect = pauseButton != null ? pauseButton.GetComponent<RectTransform>() : null;

            rotateButton = EnsureSceneButton(FindChildLooseActive(contentRoot, "RotateButton") ?? FindChildInAnyCanvas("RotateButton"), RotateFromButton);
            rotateButtonRect = rotateButton != null ? rotateButton.GetComponent<RectTransform>() : null;

            Transform nextPreview = FindChildLoose(contentRoot, "NextPreview") ?? FindChildLoose(contentRoot, "NextPanel");
            if (nextPreview != null)
            {
                nextWidgetRect = nextPreview.GetComponent<RectTransform>();
                sceneNextPreviewRect = nextWidgetRect;
                nextWidgetShadowRect = nextWidgetRect;
                float previewCellSize = 24f;
                if (nextWidgetRect != null && nextWidgetRect.rect.width > 1f)
                    previewCellSize = Mathf.Clamp(nextWidgetRect.rect.width * 0.16f, 18f, 36f);
                nextPreviewCells = CreatePiecePreview(nextPreview, new Vector2(0.5f, 0.5f), previewCellSize);
            }

            BindSceneTacticalCells(contentRoot);
            BuildSceneTacticalGridIfNeeded(contentRoot);

            statusText = Ui.Text(contentRoot, "", font, 26, new Color(1f, 0.90f, 0.68f), TextAnchor.MiddleCenter);
            Ui.Rect(statusText, new Vector2(0.08f, 0.43f), new Vector2(0.92f, 0.52f), Vector2.zero);
            statusText.raycastTarget = false;
            AddDarkWoodTextEdge(statusText, 0.65f, 0.70f);

            if (MultiplayerMatch.Active)
                BuildOpponentMiniBoard(contentRoot); // vị trí do ApplyXGameplayRegionLayout đặt

            EnsureSceneEventSystem();

            pauseOverlay = Ui.Panel(sceneGameplayCanvas.transform, "Pause Overlay", new Color(0, 0, 0, 0.65f));
            Ui.Stretch(pauseOverlay);
            BuildPausePopup(pauseOverlay.transform);
            pauseOverlay.SetActive(false);

            gameOverOverlay = Ui.Panel(sceneGameplayCanvas.transform, "Game Over Overlay", new Color(0, 0, 0, 0.68f));
            Ui.Stretch(gameOverOverlay);
            BuildGameOverPopup(gameOverOverlay.transform);
            gameOverOverlay.SetActive(false);

            gameLoseOverlay = Ui.Panel(sceneGameplayCanvas.transform, "Game Lose Overlay", new Color(0.01f, 0.02f, 0.05f, 0.992f));
            Ui.Stretch(gameLoseOverlay);
            BuildGameLosePopup(gameLoseOverlay.transform);
            gameLoseOverlay.SetActive(false);

            missionOverlay = Ui.Panel(sceneGameplayCanvas.transform, "Mission Overlay", new Color(0, 0, 0, 0.70f));
            Ui.Stretch(missionOverlay);
            BuildMissionPopup(missionOverlay.transform);
            missionOverlay.SetActive(false);

            levelClearOverlay = Ui.Panel(sceneGameplayCanvas.transform, "Level Clear Overlay", new Color(0.01f, 0.02f, 0.05f, 0.992f));
            Ui.Stretch(levelClearOverlay);
            BuildLevelClearPopup(levelClearOverlay.transform);
            levelClearOverlay.SetActive(false);

            // Ensure the correct root is active for the current screen size.
            // This handles the case where Editor Game View resolution differs from the device simulator.
            bool correctTablet = ShouldUseTabletGameplayLayout();
            if (correctTablet != sceneUsingTabletGameplayRoot)
            {
                SwitchGameplayRoot(correctTablet);
                // Rebuild puzzle grid in the new anchor so cell sizes match the new local space.
                Canvas.ForceUpdateCanvases();
                BuildScenePuzzleGrid();
                BindSceneTacticalCells(sceneContentAreaRect != null ? sceneContentAreaRect : sceneGameplayRootRect);
                BuildSceneTacticalGridIfNeeded(sceneContentAreaRect != null ? sceneContentAreaRect : sceneGameplayRootRect);
            }

            RefreshSceneHud();
            RefreshTacticalBoardUi();
            ApplySceneGameplayResponsiveLayout(true);
            RefreshScenePuzzleBoardUi();
            ConfigureResponsiveCamera();
            DisableWorldPuzzleRenderingForSceneCanvas();
            return true;
        }

        void DisableWorldPuzzleRenderingForSceneCanvas()
        {
            if (boardRoot != null)
                boardRoot.gameObject.SetActive(false);
            if (settledRoot != null)
                settledRoot.gameObject.SetActive(false);
            if (activeRoot != null)
                activeRoot.gameObject.SetActive(false);
            if (ghostRoot != null)
                ghostRoot.gameObject.SetActive(false);
        }

        Transform SelectSceneGameplayRoot(Transform mobileRoot, Transform tabletRoot, Transform legacyRoot)
        {
            bool useTablet = ShouldUseTabletGameplayLayout();
            sceneUsingTabletGameplayRoot = useTablet && tabletRoot != null;
            if (mobileRoot != null)
            {
                StretchSceneRootToScreen(mobileRoot as RectTransform);
                StretchSceneBackgroundInRoot(mobileRoot);
                mobileRoot.gameObject.SetActive(!useTablet || tabletRoot == null);
            }
            if (tabletRoot != null)
            {
                StretchSceneRootToScreen(tabletRoot as RectTransform);
                StretchSceneBackgroundInRoot(tabletRoot);
                tabletRoot.gameObject.SetActive(useTablet);
            }

            sceneGameplayReferenceResolution = sceneUsingTabletGameplayRoot
                ? new Vector2(1668f, 2420f)
                : new Vector2(1284f, 2778f);

            if (useTablet && tabletRoot != null)
                return tabletRoot;
            if (mobileRoot != null)
                return mobileRoot;
            return legacyRoot;
        }

        void StretchSceneBackgroundInRoot(Transform root)
        {
            RectTransform background = FindChildLoose(root, "Background") as RectTransform;
            if (background == null)
                return;

            if (background.parent != root)
                background.SetParent(root, true);
            background.SetAsFirstSibling();
            StretchSceneRootToScreen(background);
            var backgroundImage = background.GetComponent<Image>();
            if (backgroundImage != null)
                backgroundImage.preserveAspect = false;
        }

        bool ShouldUseTabletGameplayLayout()
        {
            float aspect = Screen.height > 0 ? Screen.width / (float)Screen.height : 1284f / 2778f;
            return aspect >= 0.65f;
        }

        RectTransform EnsureSceneGameplayHierarchy(Transform root, bool tabletLayout)
        {
            if (root == null)
                return null;

            StretchSceneRootToScreen(root as RectTransform);

            // Use pre-built scene layout — find SafeAreaContainer without reparenting any elements.
            sceneSafeAreaContainerRect = FindChildLoose(root, "SafeAreaContainer") as RectTransform;
            if (sceneSafeAreaContainerRect == null)
            {
                var safeObject = new GameObject("SafeAreaContainer");
                safeObject.transform.SetParent(root, false);
                sceneSafeAreaContainerRect = safeObject.AddComponent<RectTransform>();
            }
            // Always reset any design-time scale/offset on SafeAreaContainer so it fills the root
            // with localScale=(1,1,1). The safe-area insets are applied later by ApplySceneSafeAreaContainer.
            StretchSceneRootToScreen(sceneSafeAreaContainerRect);

            // Elements live inside SafeAreaContainer in the pre-built scene hierarchy.
            return sceneSafeAreaContainerRect;
        }

        RectTransform GetSceneRect(Transform root, string targetName)
        {
            var child = FindChildLoose(root, targetName);
            return child != null ? child.GetComponent<RectTransform>() : null;
        }

        void ConfigureSceneCanvasScaler(Canvas canvas)
        {
            if (canvas == null)
                return;

            var cam = Camera.main;
            if (cam == null)
                cam = FindObjectOfType<Camera>();
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.02f, 0.06f, 0.08f, 1f);
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 10f;
            }
            else
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = sceneGameplayReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var dynamicScaler = canvas.GetComponent<ResponsiveCanvasScaler>();
            if (dynamicScaler != null)
                Destroy(dynamicScaler);

            if (canvas.GetComponent<GraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        Text CreateHiddenSceneText(Transform parent, string name)
        {
            var text = Ui.Text(parent, "", font, 1, new Color(1f, 1f, 1f, 0f), TextAnchor.MiddleCenter);
            text.name = name;
            text.raycastTarget = false;
            text.gameObject.SetActive(false);
            return text;
        }

        Transform FindChildInAnyCanvas(string targetName)
        {
            var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            Transform fallback = null;
            foreach (var c in canvases)
            {
                var found = FindChildLooseActive(c.transform, targetName);
                if (found != null) return found;
                if (fallback == null)
                {
                    var any = FindChildLoose(c.transform, targetName);
                    if (any != null) fallback = any;
                }
            }
            return fallback;
        }

        Transform FindChildLooseActive(Transform root, string targetName)
        {
            if (root == null) return null;
            string normalizedTarget = NormalizeObjectName(targetName);
            var children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].gameObject.activeInHierarchy && NormalizeObjectName(children[i].name) == normalizedTarget)
                    return children[i];
            }
            return null;
        }

        Transform FindChildLoose(Transform root, string targetName)
        {
            if (root == null)
                return null;

            string normalizedTarget = NormalizeObjectName(targetName);
            var children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (NormalizeObjectName(children[i].name) == normalizedTarget)
                    return children[i];
            }
            return null;
        }

        string NormalizeObjectName(string value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : value.Trim().Replace(" ", string.Empty).ToLowerInvariant();
        }

        TMP_Text FindTmpText(Transform root, string targetName)
        {
            var child = FindChildLoose(root, targetName);
            if (child == null) return null;
            return child.GetComponent<TMP_Text>() ?? child.GetComponentInChildren<TMP_Text>(true);
        }

        Button EnsureSceneButton(Transform target, Action action)
        {
            if (target == null)
                return null;

            var button = target.GetComponent<Button>();
            if (button == null)
                button = target.gameObject.AddComponent<Button>();

            var graphic = target.GetComponent<Graphic>();
            if (graphic == null)
                graphic = target.GetComponentInChildren<Graphic>();
            if (graphic != null)
            {
                graphic.raycastTarget = true;
                button.targetGraphic = graphic;
            }

            button.interactable = true;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                RuntimeArt.PlayUiSwitchSound();
                action?.Invoke();
            });

            if (target.TryGetComponent<PressScaleFeedback>(out var existing))
                Destroy(existing);

            return button;
        }

        void EnsureSceneEventSystem()
        {
            if (EventSystem.current != null || FindAnyObjectByType<EventSystem>() != null)
                return;

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
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
            // Phủ kín vùng lưới TRONG của khung frame-banco để chỉ còn 1 lưới game 8×8
            // sạch (lưới in trên khung là 9×9/không vuông → phải che).
            Ui.Rect(gridRoot, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f), Vector2.zero);
            gridRect.SetAsLastSibling();

            // Nền = màu ĐƯỜNG KẺ của khung (navy đậm), khe hở giữa ô lộ ra thành lưới —
            // trùng màu lưới khung nên liền mạch.
            var gridBg = Ui.Panel(gridRoot.transform, "Tactical Grid BG", new Color(0.0f, 0.055f, 0.33f, 1f));
            Ui.Stretch(gridBg);
            gridBg.GetComponent<Image>().raycastTarget = false;
            // Canvas con: 64 nút bàn cờ chỉ rebuild khi có nước đi, không bị kéo theo
            // mỗi lần khối gạch nhích (và ngược lại). Cần raycaster riêng cho nút.
            MakeIsolatedCanvas(gridRoot, true);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var cell = Ui.Panel(gridRoot.transform, "TacticalGrid (" + (y * width + x) + ")", Color.white);
                    float minX = x / (float)width;
                    float maxX = (x + 1) / (float)width;
                    float minY = y / (float)height;
                    float maxY = (y + 1) / (float)height;
                    Ui.Rect(cell, new Vector2(minX + 0.006f, minY + 0.006f), new Vector2(maxX - 0.006f, maxY - 0.006f), Vector2.zero);
                    SetupSceneTacticalCell(cell.GetComponent<RectTransform>(), x, y);
                }
            }
        }

        // Canvas con cô lập vùng UI hay thay đổi — canvas cha không phải rebuild
        // toàn bộ mesh mỗi khi vùng này đổi màu/sprite. KHÔNG dùng cho vùng nằm
        // trong RectMask2D (mask không clip được canvas con).
        static void MakeIsolatedCanvas(GameObject go, bool needRaycaster)
        {
            if (go.GetComponent<Canvas>() == null)
                go.AddComponent<Canvas>();
            if (needRaycaster && go.GetComponent<GraphicRaycaster>() == null)
                go.AddComponent<GraphicRaycaster>();
        }

        void BuildScenePuzzleGrid()
        {
            if (scenePuzzleBoardAnchorRect == null)
                return;

            var puzzleMask = scenePuzzleBoardAnchorRect.GetComponent<RectMask2D>();
            if (puzzleMask == null)
                puzzleMask = scenePuzzleBoardAnchorRect.gameObject.AddComponent<RectMask2D>();
            puzzleMask.padding = Vector4.zero;

            scenePuzzleCells = new Image[Width, Height];
            scenePuzzleSlots = new RectTransform[Width, Height];
            ClearRuntimeChild(scenePuzzleBoardAnchorRect, "Runtime Puzzle Grid");

            var gridRoot = new GameObject("Runtime Puzzle Grid", typeof(RectTransform));
            gridRoot.transform.SetParent(scenePuzzleBoardAnchorRect, false);
            var gridRect = gridRoot.GetComponent<RectTransform>();
            scenePuzzleGridRect = gridRect;
            FitScenePuzzleGridToAnchor();
            gridRect.SetAsLastSibling();

            // Nền lưới game 10×20 phủ kín lưới in trên khung (khung ~8 cột ≠ game 10 → lệch).
            // Màu = đường kẻ (navy nhạt hơn board); khe hở giữa ô lộ ra thành lưới sạch.
            var pGridBg = Ui.Panel(gridRoot.transform, "Puzzle Grid BG", new Color(0.10f, 0.20f, 0.38f, 1f));
            Ui.Stretch(pGridBg);
            pGridBg.GetComponent<Image>().raycastTarget = false;

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var cell = Ui.Panel(gridRoot.transform, "Puzzle Block " + x + "-" + y, new Color(1f, 1f, 1f, 0f)).GetComponent<Image>();
                    cell.sprite = blockSprite;
                    cell.type = Image.Type.Simple;
                    cell.preserveAspect = true;
                    cell.raycastTarget = false;
                    var rect = cell.rectTransform;
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.localScale = Vector3.one;
                    scenePuzzleSlots[x, y] = rect;
                    scenePuzzleCells[x, y] = cell;
                }
            }

            RefreshScenePuzzleCellSizes();
        }

        void ClearRuntimeChild(Transform parent, string childName)
        {
            if (parent == null)
                return;

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (child != null && child.name == childName)
                    Destroy(child.gameObject);
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
                tacticalBoard.LastMessage = "Cần lượt đi để di chuyển.";
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

        readonly List<Image> tacticalCellImageCache = new List<Image>();
        int monsterNextCellIndex = -1;
        Sprite obstacleBlueSprite, obstacleBushSprite;

        // Sprite chướng ngại vật (chuongngaivat) — xen kẽ khối xanh / bụi xanh lá như thiết kế.
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

                    // Ô game 8×8 màu XANH khớp ô của khung (135,181,246); khe hở lộ nền navy
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

        int hudCachedMoveBank = int.MinValue;
        int hudCachedLevel = -1;
        bool hudCachedMultiplayer;

        // Chạy mỗi frame — chỉ đụng vào Text khi giá trị đổi thật, tránh cấp phát
        // chuỗi + dirty canvas 60 lần/giây (WebGL mobile rất nhạy khoản này).
        void RefreshSceneHud()
        {
            if (!usingSceneGameplayCanvas)
                return;

            EnsureSceneRuntimeGameplayUi();

            if (sceneLevelText != null && (hudCachedLevel != journeyLevel || hudCachedMultiplayer != MultiplayerMatch.Active))
            {
                hudCachedLevel = journeyLevel;
                hudCachedMultiplayer = MultiplayerMatch.Active;
                sceneLevelText.text = hudCachedMultiplayer ? "1 vs 1" : "Màn: " + journeyLevel;
            }

            int moveBank = tacticalBoard != null ? tacticalBoard.MoveBank : 0;
            // Offline: kèm đồng hồ đếm ngược trước lượt tự đi của quái (design §2.7).
            int monsterSecond = -1;
            if (!MultiplayerMatch.Active && tacticalBoard != null && tacticalBoard.Status == TacticalBoardStatus.Running)
                monsterSecond = Mathf.CeilToInt(Mathf.Max(0f, tacticalBoard.MonsterTimer));

            if (sceneMoveText != null && (moveBank != hudCachedMoveBank || monsterSecond != hudCachedMonsterSecond))
            {
                hudCachedMoveBank = moveBank;
                hudCachedMonsterSecond = monsterSecond;
                sceneMoveText.text = monsterSecond >= 0
                    ? "Lượt đi: " + moveBank + "   Quái đi sau: " + monsterSecond + "s"
                    : "Lượt đi: " + moveBank;
            }

            if (sceneNextText != null && sceneNextText.text != "TIẾP")
                sceneNextText.text = "TIẾP";

            // HUD mới: banner MÀN X + panel điểm|lượt.
            if (hudTitleText != null)
            {
                string want = MultiplayerMatch.Active ? "1 VS 1" : "MÀN " + journeyLevel;
                if (hudTitleText.text != want) hudTitleText.text = want;
            }
            if (hudScoreText != null && hudScoreText.text != score.ToString())
                hudScoreText.text = score.ToString();
            if (hudTurnText != null)
            {
                string turn = monsterSecond >= 0 ? moveBank + " · " + monsterSecond + "s" : moveBank.ToString();
                if (hudTurnText.text != turn) hudTurnText.text = turn;
            }
        }

        int hudCachedMonsterSecond = int.MinValue;

        // Màu ô quái sắp bước tới — nhấp nháy nhanh dần khi timer gần 0 (design §2.7).
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

        // Mỗi frame: chỉ nhấp nháy ô quái-sắp-đi, không refresh toàn bàn (rẻ).
        void UpdateMonsterTimerHud()
        {
            if (monsterNextCellIndex < 0 || monsterNextCellIndex >= tacticalCellImageCache.Count)
                return;
            var image = tacticalCellImageCache[monsterNextCellIndex];
            if (image != null)
                image.color = MonsterNextCellColor();
        }

        // Quái tự đi (timer) khiến màn thắng/thua — đi cùng nhánh xử lý với khi player đi.
        void OnTacticalStatusResolved()
        {
            if (tacticalBoard == null)
                return;
            RefreshTacticalBoardUi();
            UpdateUi();
            if (tacticalBoard.Status == TacticalBoardStatus.Won)
                LevelComplete();
            else if (tacticalBoard.Status == TacticalBoardStatus.Failed)
                EndGame(false);
        }

        void EnsureSceneRuntimeGameplayUi()
        {
            if (!usingSceneGameplayCanvas)
                return;

            bool rebuiltPuzzle = false;
            bool rebuiltTactical = false;
            bool rebuiltPreview = false;
            Transform contentRoot = safeAreaRoot != null ? safeAreaRoot : sceneGameplayRootRect;
            if (scenePuzzleBoardAnchorRect == null && contentRoot != null)
            {
                var puzzleAnchor = FindChildLoose(contentRoot, "PuzzleBoardAnchor");
                scenePuzzleBoardAnchorRect = puzzleAnchor != null ? puzzleAnchor.GetComponent<RectTransform>() : null;
            }
            if ((scenePuzzleCells == null || scenePuzzleGridRect == null) && scenePuzzleBoardAnchorRect != null)
            {
                BuildScenePuzzleGrid();
                rebuiltPuzzle = true;
            }

            if (sceneTacticalBoardRect == null && contentRoot != null)
            {
                var board = FindChildLoose(contentRoot, "TacticalBoard");
                sceneTacticalBoardRect = board != null ? board.GetComponent<RectTransform>() : null;
            }
            if (sceneTacticalBoardRect != null && (tacticalCellButtons == null || tacticalCellButtons.Count == 0))
            {
                BuildSceneTacticalGridIfNeeded(contentRoot);
                rebuiltTactical = true;
            }

            if ((nextPreviewCells == null || nextPreviewCells.Count == 0) && contentRoot != null)
            {
                Transform nextPreview = FindChildLoose(contentRoot, "NextPreview") ?? FindChildLoose(contentRoot, "NextPanel");
                if (nextPreview != null)
                {
                    nextWidgetRect = nextPreview.GetComponent<RectTransform>();
                    sceneNextPreviewRect = nextWidgetRect;
                    nextPreviewCells = CreatePiecePreview(nextPreview, new Vector2(0.5f, 0.5f), CalculatePreviewCellSize(sceneNextPreviewRect));
                    rebuiltPreview = true;
                }
            }

            if (rebuiltPuzzle)
                RefreshScenePuzzleBoardUi();
            if (rebuiltTactical)
                RefreshTacticalBoardUi();
            if (rebuiltPreview && nextPreviewCells != null && nextPreviewCells.Count > 0 && nextBag.Count > 0)
                RenderPiecePreview(nextPreviewCells, PeekNext(0), true);
        }

        void ApplyBoardFrameToPreviewPanel(Transform parent)
        {
            var image = parent.GetComponent<Image>();
            var sprite = RuntimeArt.CreateBoardFrameSprite();
            if (image == null || sprite == null)
                return;

            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white;

            var inner = Ui.Panel(parent, "Preview Dark Interior", new Color(0.22f, 0.095f, 0.040f, 0.99f));
            Ui.Rect(inner, new Vector2(0.055f, 0.055f), new Vector2(0.945f, 0.945f), new Vector2(0, 0));

            var shade = Ui.Panel(parent, "Preview Inner Shade", new Color(0.040f, 0.015f, 0.006f, 0.30f));
            Ui.Rect(shade, new Vector2(0.055f, 0.055f), new Vector2(0.945f, 0.945f), new Vector2(0, 0));
        }

        void StyleSideWidgetTitle(Text label)
        {
            label.fontStyle = FontStyle.Bold;
            label.color = new Color(1f, 0.88f, 0.62f);
            label.resizeTextMinSize = 18;
            label.resizeTextMaxSize = label.fontSize;
            AddDarkWoodTextEdge(label, 1.05f, 0.90f);
            AddWarmTitleFinish(label, 0.52f);

            var warmEdge = label.gameObject.AddComponent<Shadow>();
            warmEdge.effectColor = new Color(0.22f, 0.085f, 0.025f, 0.48f);
            warmEdge.effectDistance = new Vector2(0.45f, -0.45f);

            var carvedDepth = label.gameObject.AddComponent<Shadow>();
            carvedDepth.effectColor = new Color(0.045f, 0.016f, 0.005f, 0.72f);
            carvedDepth.effectDistance = new Vector2(1.0f, -1.1f);
        }

        void StylePauseButton(Button button)
        {
            StyleRoundWoodButton(button, "II", 28);
        }

        void StyleRoundWoodButton(Button button, string labelText, int fontSize)
        {
            var image = button.GetComponent<Image>();
            bool useRotateArt = labelText != "II";
            image.sprite = useRotateArt ? RuntimeArt.CreateRotateButtonSprite() : RuntimeArt.CreatePauseButtonSprite();
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
            image.raycastTarget = true;

            var buttonShadow = button.gameObject.AddComponent<Shadow>();
            buttonShadow.effectColor = new Color(0.025f, 0.008f, 0.002f, 0.90f);
            buttonShadow.effectDistance = new Vector2(4.5f, -5.5f);

            var label = button.GetComponentInChildren<Text>();
            label.text = useRotateArt ? "" : labelText;
            label.fontSize = fontSize;
            label.fontStyle = FontStyle.Bold;
            label.color = new Color(0.22f, 0.095f, 0.035f, 1f);
            label.resizeTextMinSize = 18;
            label.resizeTextMaxSize = fontSize;

            var iconShadow = label.gameObject.AddComponent<Shadow>();
            iconShadow.effectColor = new Color(1f, 0.80f, 0.45f, 0.35f);
            iconShadow.effectDistance = new Vector2(-0.7f, 0.7f);

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.92f, 0.78f, 1f);
            colors.pressedColor = new Color(0.65f, 0.38f, 0.18f, 1f);
            colors.selectedColor = Color.white;
            colors.fadeDuration = 0.05f;
            button.colors = colors;
        }

        void StyleWoodPopupFrame(GameObject panel)
        {
            var image = panel.GetComponent<Image>();
            image.sprite = RuntimeArt.CreateWoodPanelSprite();
            image.type = Image.Type.Sliced;
            image.color = Color.white;

            var innerShade = Ui.Panel(panel.transform, "Popup Inner Shade", new Color(0.045f, 0.018f, 0.008f, 0.18f));
            Ui.Rect(innerShade, new Vector2(0.055f, 0.055f), new Vector2(0.945f, 0.945f), new Vector2(0, 0));

            var topShine = Ui.Panel(panel.transform, "Popup Top Shine", new Color(0.84f, 0.52f, 0.28f, 0.30f));
            Ui.Rect(topShine, new Vector2(0.075f, 0.905f), new Vector2(0.925f, 0.920f), new Vector2(0, 0));
        }

        void StyleWoodPopupShadow(GameObject panel)
        {
            var image = panel.GetComponent<Image>();
            image.sprite = RuntimeArt.CreateWoodPanelSprite();
            image.type = Image.Type.Sliced;
            image.color = new Color(0.045f, 0.018f, 0.008f, 0.82f);
        }

        void StyleWoodRectButton(Button button, int fontSize)
        {
            var image = button.GetComponent<Image>();
            image.sprite = RuntimeArt.CreateWoodButtonSprite();
            image.type = Image.Type.Sliced;
            image.color = Color.white;

            var text = button.GetComponentInChildren<Text>();
            text.color = new Color(1f, 0.90f, 0.68f);
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.resizeTextMinSize = Mathf.Min(16, fontSize);
            text.resizeTextMaxSize = fontSize;
            AddDarkWoodTextEdge(text, 0.95f, 0.88f);

            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.055f, 0.018f, 0.006f, 0.58f);
            shadow.effectDistance = new Vector2(0.8f, -0.9f);

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.92f, 0.74f, 1f);
            colors.pressedColor = new Color(0.78f, 0.52f, 0.30f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;
        }

        void AddDarkWoodTextEdge(Text text, float thickness, float alpha)
        {
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.13f, 0.050f, 0.014f, alpha);
            outline.effectDistance = new Vector2(thickness, thickness);
            outline.useGraphicAlpha = true;

            var depth = text.gameObject.AddComponent<Shadow>();
            depth.effectColor = new Color(0.045f, 0.016f, 0.005f, 0.62f);
            depth.effectDistance = new Vector2(0.75f, -0.85f);
            depth.useGraphicAlpha = true;
        }

        void AddWarmTitleFinish(Text text, float glowStrength)
        {
            text.fontStyle = FontStyle.Bold;

            var topGlow = text.gameObject.AddComponent<Shadow>();
            topGlow.effectColor = new Color(1f, 0.68f, 0.30f, 0.25f * glowStrength);
            topGlow.effectDistance = new Vector2(-0.50f, 0.62f);
            topGlow.useGraphicAlpha = true;

            var carvedDrop = text.gameObject.AddComponent<Shadow>();
            carvedDrop.effectColor = new Color(0.035f, 0.012f, 0.004f, 0.76f);
            carvedDrop.effectDistance = new Vector2(1.35f, -1.55f);
            carvedDrop.useGraphicAlpha = true;
        }

        TMP_FontAsset PopupTitleFont()
        {
            if (popupTitleFont != null)
                return popupTitleFont;

            popupTitleFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/fmp-Batangas-Bold-s7igzb SDF")
                ?? Resources.Load<TMP_FontAsset>("Fonts & Materials/Batangas_Bold SDF");
            return popupTitleFont;
        }

        TMP_Text CreatePopupTitle(Transform parent, string value, int size, Color color)
        {
            var go = new GameObject("Popup Title");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = PopupTitleFont();
            text.fontSize = size;
            text.fontSizeMin = Mathf.Max(24, size - 12);
            text.fontSizeMax = size;
            text.enableAutoSizing = true;
            text.color = color;
            text.alignment = TextAlignmentOptions.Center;
            text.fontStyle = FontStyles.Bold;
            text.raycastTarget = false;

            var outline = go.AddComponent<Outline>();
            outline.effectColor = new Color(0.13f, 0.050f, 0.014f, 0.82f);
            outline.effectDistance = new Vector2(1.0f, 1.0f);
            outline.useGraphicAlpha = true;

            var depth = go.AddComponent<Shadow>();
            depth.effectColor = new Color(0.035f, 0.012f, 0.004f, 0.78f);
            depth.effectDistance = new Vector2(1.2f, -1.35f);
            depth.useGraphicAlpha = true;

            var warm = go.AddComponent<Shadow>();
            warm.effectColor = new Color(1f, 0.68f, 0.30f, 0.14f);
            warm.effectDistance = new Vector2(-0.45f, 0.55f);
            warm.useGraphicAlpha = true;

            return text;
        }

        void BuildPausePopup(Transform parent)
        {
            const string DIR = "popup-pause/";

            // Bóng đổ nhẹ sau khung.
            var shadow = Ui.Panel(parent, "Pause Popup Shadow", new Color(0f, 0f, 0f, 0.32f));
            Ui.Rect(shadow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(810, 736));
            shadow.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -16);
            var shImg = shadow.GetComponent<Image>();
            shImg.sprite = RoundUiSprite(); shImg.type = Image.Type.Sliced;

            // Khung nền xanh (frame v3.0).
            var box = MakeSpriteImage(parent, "Pause Popup", DIR + "frame.png", new Rect(0.223f, 0.139f, 0.554f, 0.750f), true);
            var br = box.rectTransform;
            br.anchorMin = br.anchorMax = new Vector2(0.5f, 0.5f);
            br.pivot = new Vector2(0.5f, 0.5f);
            br.sizeDelta = new Vector2(800, 722);
            br.anchoredPosition = Vector2.zero;
            var boxT = box.transform;

            // Banner tiêu đề TẠM DỪNG (chữ baked sẵn) — đè mép trên khung.
            var title = MakeSpriteImage(boxT, "Pause Title", DIR + "title.png", new Rect(0.138f, 0.422f, 0.725f, 0.250f), false);
            Ui.Rect(title, new Vector2(0.085f, 0.815f), new Vector2(0.915f, 1.055f), Vector2.zero);

            // Nút đóng (X) góc trên-phải.
            var close = MakeSpriteButton(boxT, "Pause Close", DIR + "btn-close.png", new Rect(0.329f, 0.288f, 0.342f, 0.495f),
                new Vector2(0.5f, 0.5f), new Vector2(110, 110), () => { RuntimeArt.PlayUiSwitchSound(); TogglePause(); });
            Ui.Rect(close.gameObject, new Vector2(0.845f, 0.88f), new Vector2(1.03f, 1.075f), Vector2.zero);

            // 3 nút (icon + chữ baked): TIẾP TỤC / CHƠI LẠI / VỀ MENU.
            // preserveAspect=false để 3 nút lấp đúng khung → bằng nhau.
            var tiep = MakeSpriteButton(boxT, "Pause Continue", DIR + "btn-tieptuc.png", new Rect(0.201f, 0.422f, 0.598f, 0.237f),
                new Vector2(0.5f, 0.5f), new Vector2(540, 150), () => { RuntimeArt.PlayUiSwitchSound(); TogglePause(); });
            tiep.GetComponent<Image>().preserveAspect = false;
            Ui.Rect(tiep.gameObject, new Vector2(0.13f, 0.55f), new Vector2(0.87f, 0.775f), Vector2.zero);

            var choi = MakeSpriteButton(boxT, "Pause Restart", DIR + "btn-choilai.png", new Rect(0.193f, 0.411f, 0.633f, 0.295f),
                new Vector2(0.5f, 0.5f), new Vector2(540, 150), () => { RuntimeArt.PlayUiSwitchSound(); Restart(); });
            choi.GetComponent<Image>().preserveAspect = false;
            Ui.Rect(choi.gameObject, new Vector2(0.13f, 0.32f), new Vector2(0.87f, 0.545f), Vector2.zero);

            var menu = MakeSpriteButton(boxT, "Pause Menu", DIR + "btn-menu.png", new Rect(0.172f, 0.410f, 0.656f, 0.269f),
                new Vector2(0.5f, 0.5f), new Vector2(540, 150), () => { RuntimeArt.PlayUiSwitchSound(); BackToMenu(); });
            menu.GetComponent<Image>().preserveAspect = false;
            Ui.Rect(menu.gameObject, new Vector2(0.13f, 0.09f), new Vector2(0.87f, 0.315f), Vector2.zero);
        }

        void AddPauseButton(Transform parent, string label, Vector2 anchor, UnityEngine.Events.UnityAction action)
        {
            if (parent != null && parent.name == "Pause Popup")
            {
                if (anchor.y > 0.5f)
                    anchor.y = 0.570f;
                else if (anchor.y > 0.3f)
                    anchor.y = 0.370f;
                else
                    anchor.y = 0.195f;
            }

            var shadow = Ui.Panel(parent, label + " Shadow", new Color(0.055f, 0.022f, 0.01f, 0.65f));
            Ui.Rect(shadow, anchor, anchor, new Vector2(548, 124));
            shadow.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -5);

            var button = Ui.Button(parent, label, font, 56, () =>
            {
                RuntimeArt.PlayUiSwitchSound();
                action.Invoke();
            });
            Ui.Rect(button.gameObject, anchor, anchor, new Vector2(526, 112));
            StyleWoodRectButton(button, 34);
        }

        void BuildGameOverPopup(Transform parent)
        {
            var shadow = Ui.Panel(parent, "Game Over Popup Shadow", new Color(0.04f, 0.018f, 0.008f, 0.82f));
            Ui.Rect(shadow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(850, 750));
            shadow.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -12);
            StyleWoodPopupShadow(shadow);

            var box = Ui.Panel(parent, "Game Over Popup", Color.white);
            Ui.Rect(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(820, 720));
            StyleWoodPopupFrame(box);

            gameOverTitleText = CreatePopupTitle(box.transform, "THUA RỒI", 78, new Color(1f, 0.74f, 0.42f));
            Ui.Rect(gameOverTitleText, new Vector2(0.5f, 0.780f), new Vector2(0.5f, 0.780f), new Vector2(620, 128));

            var accent = Ui.Panel(box.transform, "Game Over Accent", new Color(0.80f, 0.48f, 0.24f, 0.58f));
            Ui.Rect(accent, new Vector2(0.5f, 0.706f), new Vector2(0.5f, 0.706f), new Vector2(660, 4));

            // Dark inset panel behind score for visual depth
            var scoreBg = Ui.Panel(box.transform, "Score BG", new Color(0.08f, 0.035f, 0.012f, 0.45f));
            Ui.Rect(scoreBg, new Vector2(0.5f, 0.618f), new Vector2(0.5f, 0.618f), new Vector2(690, 148));

            gameOverScoreText = Ui.Text(box.transform, "", font, 44, Color.white, TextAnchor.MiddleCenter);
            Ui.Rect(gameOverScoreText, new Vector2(0.5f, 0.618f), new Vector2(0.5f, 0.618f), new Vector2(648, 128));
            AddDarkWoodTextEdge(gameOverScoreText, 0.95f, 0.86f);

            var sep = Ui.Panel(box.transform, "GO Sep", new Color(0.75f, 0.48f, 0.22f, 0.35f));
            Ui.Rect(sep, new Vector2(0.5f, 0.492f), new Vector2(0.5f, 0.492f), new Vector2(600, 3));

            AddPauseButton(box.transform, "Chơi lại", new Vector2(0.5f, 0.390f), Restart);
            AddPauseButton(box.transform, "Trang chủ", new Vector2(0.5f, 0.225f), BackToMenu);
        }

        void BuildMissionPopup(Transform parent)
        {
            // Panel popup xanh (asset popup-start) — cao hơn tỉ lệ gốc chút cho chữ thoáng.
            float boxW = 900f, boxH = boxW / 2.1f;
            var box = Ui.Panel(parent, "Mission Popup", Color.white);
            Ui.Rect(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(boxW, boxH));
            var boxImg = box.GetComponent<Image>();
            var panelSpr = RuntimeArt.LoadV3SubSprite("popup-start/panel-popup.png", new Rect(0.083f, 0.293f, 0.833f, 0.453f));
            boxImg.sprite = panelSpr; boxImg.type = Image.Type.Simple; boxImg.preserveAspect = false; boxImg.color = Color.white;

            // Banner MÀN X — đè lên mép trên, chính giữa.
            var banner = MakeSpriteImage(box.transform, "Mission Banner", "popup-start/frame-title-man.png", new Rect(0.130f, 0.398f, 0.740f, 0.270f), false);
            var bRt = banner.rectTransform;
            bRt.anchorMin = bRt.anchorMax = new Vector2(0.5f, 1f);
            bRt.pivot = new Vector2(0.5f, 0.5f);
            bRt.sizeDelta = new Vector2(380f, 380f / 4.12f);
            bRt.anchoredPosition = new Vector2(0f, 6f);
            missionTitleText = CreatePopupTitle(banner.transform, "MÀN 1", 40, new Color(1f, 0.98f, 0.9f));
            Ui.Rect(missionTitleText, new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f), new Vector2(280, 70));

            // Icon nhân vật hai góc trên.
            BuildMissionIcon(box.transform, "popup-start/icon-player.png", new Rect(0.357f, 0.316f, 0.286f, 0.422f), new Vector2(0.045f, 0.80f));
            BuildMissionIcon(box.transform, "popup-start/icon-enemy.png", new Rect(0.367f, 0.340f, 0.266f, 0.383f), new Vector2(0.955f, 0.80f));

            // Phụ đề "Sẵn sàng chưa?" (chữ lớn, chính giữa phần thân popup).
            missionDescText = Ui.Text(box.transform, "Sẵn sàng chưa?", titleFont != null ? titleFont : font, 56, new Color(1f, 0.99f, 0.95f), TextAnchor.MiddleCenter);
            missionDescText.fontStyle = FontStyle.Bold;
            missionDescText.raycastTarget = false;
            Ui.Rect(missionDescText, new Vector2(0.5f, 0.56f), new Vector2(0.5f, 0.56f), new Vector2(700, 110));
            AddDarkWoodTextEdge(missionDescText, 1.0f, 0.5f);

            // Nút CHƠI (cam).
            var playBtn = Ui.Button(box.transform, "", font, 36, () =>
            {
                RuntimeArt.PlayUiSwitchSound();
                missionOverlay.SetActive(false);
                paused = false;
                Time.timeScale = 1f;
            });
            var pbRt = playBtn.GetComponent<RectTransform>();
            pbRt.anchorMin = pbRt.anchorMax = new Vector2(0.5f, 0.185f);
            pbRt.pivot = new Vector2(0.5f, 0.5f);
            pbRt.sizeDelta = new Vector2(380f, 380f / 3.536f);
            var pbImg = playBtn.GetComponent<Image>();
            var playSpr = RuntimeArt.LoadV3SubSprite("popup-start/btn-batdau.png", new Rect(0.242f, 0.414f, 0.516f, 0.219f));
            if (playSpr != null) { pbImg.sprite = playSpr; pbImg.type = Image.Type.Simple; pbImg.preserveAspect = false; pbImg.color = Color.white; }
            var playLabel = Ui.Text(playBtn.transform, "CHƠI", RuntimeArt.LoadMenuButtonFont(), 52, new Color(1f, 0.99f, 0.94f), TextAnchor.MiddleCenter);
            playLabel.fontStyle = FontStyle.Bold; playLabel.raycastTarget = false;
            playLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            playLabel.verticalOverflow = VerticalWrapMode.Overflow;
            // Căn giữa hẳn trong nút (nút cam có phần nổi ~ giữa; nhích lên nhẹ cho cân thị giác).
            var plRt = playLabel.rectTransform;
            plRt.anchorMin = Vector2.zero; plRt.anchorMax = Vector2.one;
            plRt.offsetMin = new Vector2(0f, 4f); plRt.offsetMax = new Vector2(0f, 4f);
            AddDarkWoodTextEdge(playLabel, 0.9f, 0.7f);
            var pf = playBtn.gameObject.GetComponent<PressScaleFeedback>() ?? playBtn.gameObject.AddComponent<PressScaleFeedback>();
            pf.PressedScale = 0.94f;
        }

        void BuildMissionIcon(Transform parent, string asset, Rect crop, Vector2 anchor)
        {
            var img = MakeSpriteImage(parent, "Mission Icon", asset, crop, false);
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(150f, 150f);
        }

        // Màn hình CHIẾN THẮNG (Image #16): overlay toàn màn hình, đè lên gameplay
        // (nền mờ để thấy bàn cờ), bố cục landscape — tiêu đề trên, 3 sao, phụ đề,
        // cụm nhân vật + bệ ở giữa, 3 nút TRANG CHỦ / CHƠI LẠI / TIẾP ở dưới.
        void BuildLevelClearPopup(Transform parent)
        {
            const string DIR = "screen-chienthang/";

            // --- Pháo hoa/confetti rải khắp nền (vẽ trước → nằm sau mọi thứ) ---
            BuildVictoryConfetti(parent);

            // --- Tiêu đề "CHIẾN THẮNG!" ---
            var title = MakeSpriteImage(parent, "Victory Title", DIR + "title.png", new Rect(0.137f, 0.418f, 0.771f, 0.402f), false);
            var tRt = title.rectTransform;
            tRt.anchorMin = tRt.anchorMax = new Vector2(0.5f, 0.855f);
            tRt.pivot = new Vector2(0.5f, 0.5f);
            tRt.sizeDelta = new Vector2(660f, 660f / 2.87f);

            // --- Hàng 3 sao ---
            var starRow = new GameObject("Victory Stars", typeof(RectTransform));
            starRow.transform.SetParent(parent, false);
            var srRt = starRow.GetComponent<RectTransform>();
            srRt.anchorMin = srRt.anchorMax = new Vector2(0.5f, 0.680f);
            srRt.pivot = new Vector2(0.5f, 0.5f);
            srRt.sizeDelta = new Vector2(560f, 200f);
            levelClearStarImgs = new Image[3];
            float[] starX = { -190f, 0f, 190f };
            float[] starY = { 14f, 40f, 14f };      // sao giữa cao hơn
            float[] starS = { 150f, 190f, 150f };    // sao giữa to hơn
            for (int i = 0; i < 3; i++)
            {
                var star = MakeSpriteImage(starRow.transform, "Star " + i, DIR + "star-inactive.png", new Rect(0.189f, 0.345f, 0.621f, 0.411f), false);
                var stRt = star.rectTransform;
                stRt.anchorMin = stRt.anchorMax = new Vector2(0.5f, 0.5f);
                stRt.pivot = new Vector2(0.5f, 0.5f);
                stRt.sizeDelta = new Vector2(starS[i], starS[i]);
                stRt.anchoredPosition = new Vector2(starX[i], starY[i]);
                levelClearStarImgs[i] = star;
            }

            // --- Phụ đề vui (LevelComplete cập nhật nội dung) ---
            levelClearBodyText = Ui.Text(parent, "", font, 46, new Color(1f, 0.99f, 0.86f), TextAnchor.MiddleCenter);
            levelClearBodyText.fontStyle = FontStyle.Bold;
            levelClearBodyText.raycastTarget = false;
            levelClearBodyText.horizontalOverflow = HorizontalWrapMode.Overflow;
            levelClearBodyText.verticalOverflow = VerticalWrapMode.Overflow;
            Ui.Rect(levelClearBodyText, new Vector2(0.5f, 0.575f), new Vector2(0.5f, 0.575f), new Vector2(900, 90));
            AddDarkWoodTextEdge(levelClearBodyText, 1.0f, 0.5f);

            // --- Cụm nhân vật giữa màn: bệ + hiệp sĩ, quái đỏ trái, khối tím phải ---
            var podium = MakeSpriteImage(parent, "Victory Podium", DIR + "decor-2.png", new Rect(0.111f, 0.219f, 0.775f, 0.160f), false);
            var pdRt = podium.rectTransform;
            pdRt.anchorMin = pdRt.anchorMax = new Vector2(0.5f, 0.236f);
            pdRt.pivot = new Vector2(0.5f, 0.5f);
            pdRt.sizeDelta = new Vector2(660f, 660f / 3.23f);

            var enemy = MakeSpriteImage(parent, "Victory Enemy", DIR + "decor-enemy.png", new Rect(0.229f, 0.348f, 0.541f, 0.335f), false);
            var enRt = enemy.rectTransform;
            enRt.anchorMin = enRt.anchorMax = new Vector2(0.431f, 0.350f);
            enRt.pivot = new Vector2(0.5f, 0.5f);
            enRt.sizeDelta = new Vector2(198f, 198f);

            var blocks = MakeSpriteImage(parent, "Victory Blocks", DIR + "decor-1.png", new Rect(0.211f, 0.339f, 0.574f, 0.383f), false);
            var blRt = blocks.rectTransform;
            blRt.anchorMin = blRt.anchorMax = new Vector2(0.580f, 0.242f);
            blRt.pivot = new Vector2(0.5f, 0.5f);
            blRt.sizeDelta = new Vector2(205f, 205f);

            var hero = MakeSpriteImage(parent, "Victory Hero", DIR + "decor-player.png", new Rect(0.178f, 0.264f, 0.633f, 0.577f), false);
            var hRt = hero.rectTransform;
            hRt.anchorMin = hRt.anchorMax = new Vector2(0.5f, 0.384f);
            hRt.pivot = new Vector2(0.5f, 0.5f);
            hRt.sizeDelta = new Vector2(402f * 0.73f, 402f);

            // --- 3 nút dưới: TRANG CHỦ (vuông vàng), CHƠI LẠI (vuông xanh), TIẾP (viên cam) ---
            // 3 nút cùng chiều cao, to hơn, xếp thành một hàng sát nhau căn giữa dưới màn.
            const float btnH = 142f;
            continueButton = MakeSpriteButton(parent, "Btn Home", DIR + "btn-home.png", new Rect(0.150f, 0.275f, 0.697f, 0.479f),
                new Vector2(0.388f, 0.118f), new Vector2(btnH * 0.97f, btnH), null);
            stopButton = MakeSpriteButton(parent, "Btn Retry", DIR + "btn-retry.png", new Rect(0.271f, 0.168f, 0.458f, 0.664f),
                new Vector2(0.446f, 0.118f), new Vector2(btnH * 1.04f, btnH), null);
            nextButton = MakeSpriteButton(parent, "Btn Next", DIR + "btn-next.png", new Rect(0.077f, 0.287f, 0.841f, 0.434f),
                new Vector2(0.553f, 0.118f), new Vector2(btnH * 2.91f, btnH), null);
        }

        // Màn hình THẤT BẠI (Image #5): giống màn thắng nhưng buồn — tiêu đề đỏ,
        // 3 sao xám, hiệp sĩ gục ngã, nút TIẾP bị khoá (xám).
        void BuildGameLosePopup(Transform parent)
        {
            const string DIR = "screen-thatbai/";

            // --- Tiêu đề "THẤT BẠI!" ---
            var title = MakeSpriteImage(parent, "Lose Title", DIR + "title.png", new Rect(0.133f, 0.414f, 0.757f, 0.336f), false);
            var tRt = title.rectTransform;
            tRt.anchorMin = tRt.anchorMax = new Vector2(0.5f, 0.855f);
            tRt.pivot = new Vector2(0.5f, 0.5f);
            tRt.sizeDelta = new Vector2(640f, 640f / 3.38f);

            // --- Hàng 3 sao xám ---
            var starRow = new GameObject("Lose Stars", typeof(RectTransform));
            starRow.transform.SetParent(parent, false);
            var srRt = starRow.GetComponent<RectTransform>();
            srRt.anchorMin = srRt.anchorMax = new Vector2(0.5f, 0.680f);
            srRt.pivot = new Vector2(0.5f, 0.5f);
            srRt.sizeDelta = new Vector2(560f, 200f);
            float[] starX = { -190f, 0f, 190f };
            float[] starY = { 14f, 40f, 14f };
            float[] starS = { 150f, 190f, 150f };
            for (int i = 0; i < 3; i++)
            {
                var star = MakeSpriteImage(starRow.transform, "Star " + i, DIR + "star.png", new Rect(0.353f, 0.389f, 0.293f, 0.422f), false);
                var stRt = star.rectTransform;
                stRt.anchorMin = stRt.anchorMax = new Vector2(0.5f, 0.5f);
                stRt.pivot = new Vector2(0.5f, 0.5f);
                stRt.sizeDelta = new Vector2(starS[i], starS[i]);
                stRt.anchoredPosition = new Vector2(starX[i], starY[i]);
            }

            // --- Phụ đề buồn (EndGame cập nhật nội dung ngẫu nhiên) ---
            gameLoseSubtitle = Ui.Text(parent, "", font, 46, new Color(1f, 0.99f, 0.9f), TextAnchor.MiddleCenter);
            gameLoseSubtitle.fontStyle = FontStyle.Bold;
            gameLoseSubtitle.raycastTarget = false;
            gameLoseSubtitle.horizontalOverflow = HorizontalWrapMode.Overflow;
            gameLoseSubtitle.verticalOverflow = VerticalWrapMode.Overflow;
            Ui.Rect(gameLoseSubtitle, new Vector2(0.5f, 0.575f), new Vector2(0.5f, 0.575f), new Vector2(900, 90));
            AddDarkWoodTextEdge(gameLoseSubtitle, 1.0f, 0.5f);

            // --- Cụm nhân vật: bệ (decor-1) + hiệp sĩ gục (decor-player), quái đỏ trái, khối tím phải ---
            var podium = MakeSpriteImage(parent, "Lose Podium", DIR + "decor-1.png", new Rect(0.146f, 0.236f, 0.694f, 0.201f), false);
            var pdRt = podium.rectTransform;
            pdRt.anchorMin = pdRt.anchorMax = new Vector2(0.5f, 0.236f);
            pdRt.pivot = new Vector2(0.5f, 0.5f);
            pdRt.sizeDelta = new Vector2(690f, 690f / 5.17f);

            var enemy = MakeSpriteImage(parent, "Lose Enemy", DIR + "decor-enemy.png", new Rect(0.352f, 0.223f, 0.293f, 0.490f), false);
            var enRt = enemy.rectTransform;
            enRt.anchorMin = enRt.anchorMax = new Vector2(0.418f, 0.322f);
            enRt.pivot = new Vector2(0.5f, 0.5f);
            enRt.sizeDelta = new Vector2(205f * 0.90f, 205f);

            var blocks = MakeSpriteImage(parent, "Lose Blocks", DIR + "decor-2.png", new Rect(0.324f, 0.303f, 0.280f, 0.439f), false);
            var blRt = blocks.rectTransform;
            blRt.anchorMin = blRt.anchorMax = new Vector2(0.583f, 0.300f);
            blRt.pivot = new Vector2(0.5f, 0.5f);
            blRt.sizeDelta = new Vector2(200f * 0.96f, 200f);

            var hero = MakeSpriteImage(parent, "Lose Hero", DIR + "decor-player.png", new Rect(0.285f, 0.160f, 0.401f, 0.623f), false);
            var hRt = hero.rectTransform;
            hRt.anchorMin = hRt.anchorMax = new Vector2(0.5f, 0.352f);
            hRt.pivot = new Vector2(0.5f, 0.5f);
            hRt.sizeDelta = new Vector2(360f * 0.966f, 360f);

            // --- 3 nút: TRANG CHỦ, CHƠI LẠI, TIẾP (khoá xám) ---
            const string DB = "screen-chienthang/";
            const float btnH = 142f;
            MakeSpriteButton(parent, "Btn Home", DB + "btn-home.png", new Rect(0.150f, 0.275f, 0.697f, 0.479f),
                new Vector2(0.388f, 0.118f), new Vector2(btnH * 0.97f, btnH), () =>
                {
                    RuntimeArt.PlayUiSwitchSound();
                    Time.timeScale = 1f;
                    SceneManager.LoadScene("BrickLevel");
                });
            MakeSpriteButton(parent, "Btn Retry", DB + "btn-retry.png", new Rect(0.271f, 0.168f, 0.458f, 0.664f),
                new Vector2(0.446f, 0.118f), new Vector2(btnH * 1.04f, btnH), () =>
                {
                    RuntimeArt.PlayUiSwitchSound();
                    Restart();
                });
            MakeSpriteButton(parent, "Btn Next", DB + "btn-next.png", new Rect(0.077f, 0.287f, 0.841f, 0.434f),
                new Vector2(0.553f, 0.118f), new Vector2(btnH * 2.91f, btnH), () =>
                {
                    RuntimeArt.PlayUiSwitchSound();
                    Time.timeScale = 1f;
                    if (journeyLevel < LevelProgress.MaxLevels)
                    {
                        int nl = journeyLevel + 1;
                        GameSession.SelectedLevel = nl;
                        GameSession.JourneyLevel = nl;
                        SceneManager.LoadScene("BrickGame");
                    }
                    else SceneManager.LoadScene("BrickLevel");
                });
        }

        // Rải pháo hoa/confetti (decor-phaohoa-1..7) ngẫu nhiên khắp nền màn thắng.
        void BuildVictoryConfetti(Transform parent)
        {
            Rect[] crops =
            {
                new Rect(0.417f, 0.422f, 0.176f, 0.244f),
                new Rect(0.405f, 0.389f, 0.180f, 0.311f),
                new Rect(0.421f, 0.457f, 0.164f, 0.195f),
                new Rect(0.393f, 0.379f, 0.211f, 0.334f),
                new Rect(0.380f, 0.363f, 0.237f, 0.330f),
                new Rect(0.408f, 0.357f, 0.185f, 0.332f),
                new Rect(0.341f, 0.322f, 0.310f, 0.445f),
            };
            float[] aspects = { 1.08f, 0.87f, 1.26f, 0.95f, 1.08f, 0.84f, 1.04f };
            // Vị trí rải quanh rìa + phần trên, né vùng giữa (hero/sao/tiêu đề).
            Vector2[] pos =
            {
                new Vector2(0.07f, 0.86f), new Vector2(0.19f, 0.72f), new Vector2(0.13f, 0.50f),
                new Vector2(0.09f, 0.28f), new Vector2(0.27f, 0.90f), new Vector2(0.25f, 0.34f),
                new Vector2(0.34f, 0.62f), new Vector2(0.63f, 0.90f), new Vector2(0.74f, 0.66f),
                new Vector2(0.82f, 0.84f), new Vector2(0.90f, 0.52f), new Vector2(0.86f, 0.30f),
                new Vector2(0.71f, 0.30f), new Vector2(0.60f, 0.16f), new Vector2(0.93f, 0.72f),
                new Vector2(0.05f, 0.65f), new Vector2(0.40f, 0.20f), new Vector2(0.96f, 0.90f),
            };
            var rng = new System.Random(20260802);
            foreach (var p in pos)
            {
                int i = rng.Next(0, crops.Length);
                var img = MakeSpriteImage(parent, "Confetti", DIR_CT + "decor-phaohoa-" + (i + 1) + ".png", crops[i], false);
                var rt = img.rectTransform;
                rt.anchorMin = rt.anchorMax = p;
                rt.pivot = new Vector2(0.5f, 0.5f);
                float h = 22f + (float)rng.NextDouble() * 26f;
                rt.sizeDelta = new Vector2(h * aspects[i], h);
                rt.localEulerAngles = new Vector3(0f, 0f, (float)rng.NextDouble() * 360f);
                var c = img.color; c.a = 0.42f; img.color = c;
            }
        }

        const string DIR_CT = "screen-chienthang/";

        // Nút hình sprite (crop từ asset v3.0) + phản hồi nhấn.
        Button MakeSpriteButton(Transform parent, string name, string asset, Rect crop, Vector2 anchor, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            var img = MakeSpriteImage(parent, name, asset, crop, true);
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            if (action != null) btn.onClick.AddListener(action);
            var pf = img.gameObject.GetComponent<PressScaleFeedback>() ?? img.gameObject.AddComponent<PressScaleFeedback>();
            pf.PressedScale = 0.93f;
            return btn;
        }

        void ConfigureResponsiveCamera()
        {
            if (cam == null)
                cam = Camera.main ?? FindAnyObjectByType<Camera>();
            if (cam == null)
                return;

            float aspect = Mathf.Max(0.35f, cam.aspect);
            bool portrait = aspect < 0.8f;
            float boardHalfHeight = Height * 0.5f;
            float boardHalfWidth = Width * 0.5f;

            if (portrait)
            {
                GetPortraitGameplayLayout(aspect, out float boardLeft, out float boardRight, out float boardBottom, out float boardTop, out _, out _);
                float boardWidthScreenFraction = boardRight - boardLeft;
                float boardHeightScreenFraction = boardTop - boardBottom;
                float sizeForHeight = boardHalfHeight / boardHeightScreenFraction;
                float sizeForWidth = boardHalfWidth / (boardWidthScreenFraction * aspect);
                cam.orthographicSize = Mathf.Max(sizeForHeight, sizeForWidth);

                float boardCenterScreenX = (boardLeft + boardRight) * 0.5f;
                float boardCenterScreenY = (boardBottom + boardTop) * 0.5f;
                float cameraX = -(boardCenterScreenX - 0.5f) * 2f * cam.orthographicSize * aspect;
                float cameraY = -(boardCenterScreenY - 0.5f) * 2f * cam.orthographicSize;
                cameraHome = new Vector3(cameraX, cameraY, -10f);
            }
            else
            {
                float sizeForHeight = boardHalfHeight + 0.9f;
                float sizeForWidth = (boardHalfWidth + 2.4f) / aspect;
                cam.orthographicSize = Mathf.Max(sizeForHeight, sizeForWidth);
                cameraHome = new Vector3(0f, 0.7f, -10f);
            }

            cam.transform.position = cameraHome;
            if (safeAreaRoot != null && !usingSceneGameplayCanvas)
                Ui.ApplySafeArea(safeAreaRoot);
            if (usingSceneGameplayCanvas)
                ApplySceneGameplayResponsiveLayout(false);
            LayoutGameplayChrome();
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            lastAppliedSafeArea = Screen.safeArea;
        }

        void SwitchGameplayRoot(bool useTablet)
        {
            RectTransform fromRoot = sceneUsingTabletGameplayRoot ? sceneTabletGameplayRootRect : sceneMobileGameplayRootRect;
            RectTransform toRoot   = useTablet                    ? sceneTabletGameplayRootRect : sceneMobileGameplayRootRect;
            if (fromRoot == null || toRoot == null) return;

            Transform fromContent = FindChildLoose(fromRoot, "SafeAreaContainer") ?? (Transform)fromRoot;
            Transform toContent   = FindChildLoose(toRoot,   "SafeAreaContainer") ?? (Transform)toRoot;

            // Move Runtime children inside SafeAreaContainer (e.g. Runtime Score Mirror, status text).
            var toMove = new System.Collections.Generic.List<Transform>();
            foreach (Transform child in fromContent)
                if (child.name.StartsWith("Runtime ") || child.name == statusText?.gameObject.name)
                    toMove.Add(child);
            foreach (var child in toMove)
                child.SetParent(toContent, false);

            // Move Runtime Puzzle Grid: it lives inside PuzzleBoardAnchor.
            Transform fromPuzzleAnchor = FindChildLoose(fromContent, "PuzzleBoardAnchor");
            Transform toPuzzleAnchor   = FindChildLoose(toContent,   "PuzzleBoardAnchor");
            if (fromPuzzleAnchor != null && toPuzzleAnchor != null)
            {
                var puzzleToMove = new System.Collections.Generic.List<Transform>();
                foreach (Transform child in fromPuzzleAnchor)
                    if (child.name.StartsWith("Runtime "))
                        puzzleToMove.Add(child);
                foreach (var child in puzzleToMove)
                    child.SetParent(toPuzzleAnchor, false);
            }

            // Move Runtime Tactical Grid: it lives inside TacticalBoard.
            Transform fromTactical = FindChildLoose(fromContent, "TacticalBoard");
            Transform toTactical   = FindChildLoose(toContent,   "TacticalBoard");
            if (fromTactical != null && toTactical != null)
            {
                var tacticalToMove = new System.Collections.Generic.List<Transform>();
                foreach (Transform child in fromTactical)
                    if (child.name.StartsWith("Runtime "))
                        tacticalToMove.Add(child);
                foreach (var child in tacticalToMove)
                    child.SetParent(toTactical, false);
            }

            sceneUsingTabletGameplayRoot = useTablet;
            fromRoot.gameObject.SetActive(false);
            toRoot.gameObject.SetActive(true);
            sceneGameplayRootRect      = toRoot;
            sceneContentAreaRect       = toContent as RectTransform ?? toRoot;
            // Reset any design-time scale on the new content root so children layout correctly.
            StretchSceneRootToScreen(sceneContentAreaRect);
            sceneSafeAreaContainerRect = sceneContentAreaRect;
            safeAreaRoot               = sceneContentAreaRect;
            sceneBackgroundRect        = GetSceneRect(toRoot, "Background");
            sceneHeaderRect            = GetSceneRect(toContent, "Header");
            sceneNextPanelRect         = GetSceneRect(toContent, "NextPanel");
            var newPuzzleAnchor        = FindChildLoose(toContent, "PuzzleBoardAnchor");
            scenePuzzleBoardAnchorRect = newPuzzleAnchor != null ? newPuzzleAnchor.GetComponent<RectTransform>() : null;
            if (scenePuzzleGridRect != null && newPuzzleAnchor != null)
                scenePuzzleGridRect = FindChildLoose(newPuzzleAnchor, "Runtime Puzzle Grid")?.GetComponent<RectTransform>();
            var newTactical            = FindChildLoose(toContent, "TacticalBoard");
            sceneTacticalBoardRect     = newTactical != null ? newTactical.GetComponent<RectTransform>() : null;
            sceneGameplayReferenceResolution = useTablet ? new Vector2(1668f, 2420f) : new Vector2(1284f, 2778f);

            // Rebind HUD texts to the new active layout's scene objects so score/level
            // updates reach the visible root instead of the deactivated one.
            sceneLevelText = FindTmpText(toContent, "LevelText");
            sceneMoveText = FindTmpText(toContent, "MoveText");
            // Text vừa bind lại còn nguyên chữ mẫu của scene ("Level:") — phải vô
            // hiệu cache HUD để RefreshSceneHud ghi đè ngay frame sau.
            hudCachedLevel = -1;
            hudCachedMoveBank = int.MinValue;

            // Rebind Next panel text and preview to the new active layout's scene objects.
            sceneNextText = FindTmpText(toContent, "NextPanel");
            if (sceneNextText != null)
                sceneNextText.text = "TIẾP";

            Transform oldNextPreview = sceneNextPreviewRect?.transform;
            Transform newNextPreview = FindChildLoose(toContent, "NextPreview") ?? FindChildLoose(toContent, "NextPanel");
            if (newNextPreview != null)
            {
                // Move "Preview Cell" children from the old preview parent to the new one.
                if (oldNextPreview != null && oldNextPreview != newNextPreview)
                {
                    var cellsToMove = new System.Collections.Generic.List<Transform>();
                    foreach (Transform child in oldNextPreview)
                        if (child.name == "Preview Cell")
                            cellsToMove.Add(child);
                    foreach (var cell in cellsToMove)
                        cell.SetParent(newNextPreview, false);
                }
                sceneNextPreviewRect = newNextPreview.GetComponent<RectTransform>();
                nextWidgetRect = sceneNextPreviewRect;
            }

            var newRotateGO = FindChildLoose(toContent, "RotateButton");
            if (newRotateGO != null)
            {
                rotateButtonRect = newRotateGO.GetComponent<RectTransform>();
                EnsureSceneButton(newRotateGO, RotateFromButton);
            }
            Transform newHeaderTransform = FindChildLoose(toContent, "Header");
            var newPauseGO = newHeaderTransform != null
                ? (FindChildLoose(newHeaderTransform, "PauseButton") ?? FindChildLoose(toContent, "PauseButton"))
                : FindChildLoose(toContent, "PauseButton");
            if (newPauseGO != null)
            {
                pauseButtonRect = newPauseGO.GetComponent<RectTransform>();
                EnsureSceneButton(newPauseGO, TogglePause);
            }

            // Force layout recalc so GetWorldCorners returns correct values for the new root.
            Canvas.ForceUpdateCanvases();
        }

        void ApplySceneGameplayResponsiveLayout(bool force)
        {
            if (!usingSceneGameplayCanvas || sceneGameplayRootRect == null)
                return;

            // Re-evaluate mobile/tablet visibility whenever screen size changes.
            // Note: sceneGameplayRootRect and all rect bindings always follow the root that was
            // active at startup (where runtime content was built). We never rebind — instead we
            // move the runtime content into whichever root is now visible.
            bool shouldUseTablet = ShouldUseTabletGameplayLayout();
            if (shouldUseTablet != sceneUsingTabletGameplayRoot)
                SwitchGameplayRoot(shouldUseTablet);

            // Stretch the active layout root to fill the screen.
            StretchSceneRootToScreen(sceneGameplayRootRect);

            // Also stretch the inactive root so it is ready if the layout switches.
            if (sceneUsingTabletGameplayRoot && sceneMobileGameplayRootRect != null)
                StretchSceneRootToScreen(sceneMobileGameplayRootRect);
            else if (!sceneUsingTabletGameplayRoot && sceneTabletGameplayRootRect != null)
                StretchSceneRootToScreen(sceneTabletGameplayRootRect);

            // Background fills the full screen behind everything.
            if (sceneBackgroundRect != null)
            {
                sceneBackgroundRect.SetAsFirstSibling();
                StretchSceneRootToScreen(sceneBackgroundRect);
                var backgroundImage = sceneBackgroundRect.GetComponent<Image>();
                if (backgroundImage != null)
                    backgroundImage.preserveAspect = false;
            }

            // Reposition all scene elements using normalized anchor values so layout is
            // correct on every screen size (not just the reference 1284×2778 design size).
            ApplyGameplayRegionLayout();

            // Force layout recalc so parent.rect reflects new anchor values before
            // computing cell sizes and safe-area insets.
            Canvas.ForceUpdateCanvases();

            // Apply safe-area inset so content stays clear of notch / home bar.
            ApplySceneSafeAreaContainer();

            Canvas.ForceUpdateCanvases();
            RefreshScenePreviewCellSizes();
            RefreshScenePuzzleCellSizes();
        }

        void ApplySceneSafeAreaContainer()
        {
            // Lazily resolve if not already set.
            if (sceneSafeAreaContainerRect == null)
            {
                // Try gameplay root first, then search all scene transforms.
                Transform searchRoot = sceneGameplayRootRect != null ? (Transform)sceneGameplayRootRect : null;
                if (searchRoot != null)
                    sceneSafeAreaContainerRect = FindChildLooseActive(searchRoot, "SafeAreaContainer") as RectTransform
                        ?? FindChildLoose(searchRoot, "SafeAreaContainer") as RectTransform;
                if (sceneSafeAreaContainerRect == null)
                    sceneSafeAreaContainerRect = FindChildInAnyCanvas("SafeAreaContainer") as RectTransform;
            }

            if (sceneSafeAreaContainerRect == null)
                return;

            var parent = sceneSafeAreaContainerRect.parent as RectTransform;
            if (parent == null)
                return;

            // Only push in safe-area insets + a small breathing margin using offsetMin/offsetMax.
            // Do NOT change anchorMin/Max, sizeDelta, anchoredPosition, or localScale — the pre-built
            // scene layout carries its own localScale (may be non-1) that positions all children.
            Rect safe = Ui.SafeArea();
            float screenWidth = Mathf.Max(1f, Screen.width);
            float screenHeight = Mathf.Max(1f, Screen.height);
            float parentW = Mathf.Max(1f, parent.rect.width);
            float parentH = Mathf.Max(1f, parent.rect.height);

            // Convert screen safe-area to parent-local canvas offsets.
            float safeL = parentW * Mathf.Clamp01(safe.xMin / screenWidth);
            float safeR = parentW * Mathf.Clamp01(1f - (safe.xMin + safe.width) / screenWidth);
            float safeB = parentH * Mathf.Clamp01(safe.yMin / screenHeight);
            float safeT = parentH * Mathf.Clamp01(1f - (safe.yMin + safe.height) / screenHeight);

            // Add a small breathing margin so content never kisses the screen edge.
            float margin = sceneUsingTabletGameplayRoot ? 20f : 16f;
            sceneSafeAreaContainerRect.offsetMin = new Vector2(safeL + margin, safeB + margin);
            sceneSafeAreaContainerRect.offsetMax = new Vector2(-(safeR + margin), -(safeT + margin));
            sceneSafeAreaContainerRect.SetAsLastSibling();
        }

        void ApplyGameplayRegionLayout()
        {
            float aspect = Screen.height > 0 ? Screen.width / (float)Screen.height : 9f / 16f;
            bool tablet = sceneUsingTabletGameplayRoot;
            if (tablet)
                ApplyTabletGameplayRegionLayout(aspect);
            else
                ApplyMobileGameplayRegionLayout(aspect);
        }

        void ApplyMobileGameplayRegionLayout(float aspect)
        {
            const float left = 0.042f;
            const float right = 0.958f;
            const float top = 0.982f;
            const float bottom = 0.018f;
            const float cx = 0.5f;
            float safeWidth = right - left;

            float headerW = Mathf.Clamp(safeWidth * 0.90f, 0.80f, 0.92f);
            float headerH = 0.070f;
            ApplySceneRect(sceneHeaderRect,
                new Vector2(cx - headerW * 0.5f, top - headerH),
                new Vector2(cx + headerW * 0.5f, top));
            LayoutHeaderChildren();

            // Compute the lower section first so the tactical board can align to its edges.
            // Use a fixed fraction of available height for the tactical board to break circularity.
            float available = (top - headerH - 0.012f) - bottom;
            float tacticalH = Mathf.Clamp(available * 0.375f, 0.280f, 0.400f);
            float lowerGap = 0.018f;
            float lowerH = available - tacticalH - lowerGap;

            float gap = Mathf.Clamp(safeWidth * 0.035f, 0.024f, 0.040f);
            float nextWidth = Mathf.Clamp(safeWidth * 0.310f, 0.275f, 0.345f);
            // PuzzleBoardAnchor: inner frame needs 2:1 ratio to match 10×20 grid.
            // factor 0.501 accounts for frame border (~8% each side); refAspect = 1284/2778 = 0.4622.
            float puzzleWidth = Mathf.Clamp(lowerH * 0.501f / 0.4622f, 0.44f, 0.72f);
            float lowerTotalW = puzzleWidth + gap + nextWidth;

            // Tactical board width = lower section width so all edges align vertically.
            float tacticalW = lowerTotalW;
            float tacticalTop = top - headerH - 0.012f;
            ApplySceneRect(sceneTacticalBoardRect,
                new Vector2(cx - tacticalW * 0.5f, tacticalTop - tacticalH),
                new Vector2(cx + tacticalW * 0.5f, tacticalTop));

            // Recompute lower section with exact height after placing tactical board.
            float lowerTop = tacticalTop - tacticalH - lowerGap;
            lowerH = lowerTop - bottom;
            puzzleWidth = Mathf.Clamp(lowerH * 0.501f / 0.4622f, 0.44f, 0.72f);
            lowerTotalW = puzzleWidth + gap + nextWidth;

            float puzzleLeft = cx - lowerTotalW * 0.5f;
            ApplySceneRect(scenePuzzleBoardAnchorRect,
                new Vector2(puzzleLeft, bottom),
                new Vector2(puzzleLeft + puzzleWidth, bottom + lowerH));

            float sideLeft = puzzleLeft + puzzleWidth + gap;
            float sideRight = sideLeft + nextWidth;
            float nextPanelH = Mathf.Clamp(lowerH * 0.38f, 0.155f, 0.240f);
            float nextPanelTop = bottom + lowerH - 0.015f;
            float rotateWidth = Mathf.Clamp(nextWidth * 0.68f, 0.100f, 0.135f);
            float rotateHeight = rotateWidth * aspect;
            float rotateCenter = (sideLeft + sideRight) * 0.5f;

            // Trận 1v1: ẩn ô TIẾP, dời nút Xoay xuống đáy — cả cột phải dành cho bàn đối thủ.
            bool opponentColumn = MultiplayerMatch.Active && opponentMiniPanelRect != null;
            if (sceneNextPanelRect != null)
                sceneNextPanelRect.gameObject.SetActive(!opponentColumn);

            if (opponentColumn)
            {
                float rotateBottom = bottom + 0.006f;
                if (rotateButtonRect != null)
                    ApplySceneRect(rotateButtonRect,
                        new Vector2(rotateCenter - rotateWidth * 0.5f, rotateBottom),
                        new Vector2(rotateCenter + rotateWidth * 0.5f, rotateBottom + rotateHeight));

                float attackBottom = rotateBottom + rotateHeight + 0.010f;
                const float attackH = 0.038f;
                if (attackButtonRect != null)
                    ApplySceneRect(attackButtonRect,
                        new Vector2(sideLeft + 0.010f, attackBottom),
                        new Vector2(sideRight - 0.010f, attackBottom + attackH));

                LayoutOpponentMiniBoard(sideLeft, sideRight,
                    nextPanelTop, attackBottom + attackH + 0.014f, aspect);
            }
            else
            {
                ApplySceneRect(sceneNextPanelRect,
                    new Vector2(sideLeft, nextPanelTop - nextPanelH),
                    new Vector2(sideRight, nextPanelTop));
                LayoutNextPreviewInPanel();

                if (rotateButtonRect != null)
                {
                    float rotateTop = nextPanelTop - nextPanelH - 0.028f;
                    ApplySceneRect(rotateButtonRect,
                        new Vector2(rotateCenter - rotateWidth * 0.5f, rotateTop - rotateHeight),
                        new Vector2(rotateCenter + rotateWidth * 0.5f, rotateTop));
                }
            }

            if (statusText != null)
                ApplySceneRect(statusText.rectTransform,
                    new Vector2(left, tacticalTop - tacticalH - 0.002f),
                    new Vector2(cx - lowerTotalW * 0.5f - 0.010f, tacticalTop));
        }

        void ApplyTabletGameplayRegionLayout(float aspect)
        {
            // Màn hình ngang thật (rộng hơn cao): bố cục 3 cột thay vì xếp chồng dọc.
            if (aspect >= 1f)
            {
                ApplyLandscapeGameplayRegionLayout(aspect);
                return;
            }

            const float top = 0.982f;
            const float bottom = 0.018f;
            const float cx = 0.5f;

            // Header — full-width strip at the top.
            float headerH = 0.075f;
            float headerW = 0.88f;
            ApplySceneRect(sceneHeaderRect,
                new Vector2(cx - headerW * 0.5f, top - headerH),
                new Vector2(cx + headerW * 0.5f, top));
            LayoutHeaderChildren();

            // Split the remaining height: tactical board gets ~30%, lower section gets the rest.
            float available = top - headerH - 0.012f - bottom;
            float tacticalH = available * 0.350f;
            float lowerGap = 0.018f;
            float lowerH = available - tacticalH - lowerGap;

            // Puzzle board: square-cell constraint (0.501 accounts for frame border).
            float colGap = 0.020f;
            float puzzleW = Mathf.Clamp(lowerH * 0.501f / Mathf.Max(0.55f, aspect), 0.25f, 0.50f);
            // Right panel is 68% of puzzle width so the two columns feel balanced.
            float nextPanelW = Mathf.Clamp(puzzleW * 0.68f, 0.18f, 0.28f);
            float lowerTotalW = puzzleW + colGap + nextPanelW;

            // Tactical board is slightly wider than the lower section for visual hierarchy.
            float tacticalW = Mathf.Clamp(lowerTotalW + 0.04f, 0.58f, 0.84f);

            // --- Position elements top-down ---
            float tacticalTop = top - headerH - 0.012f;
            ApplySceneRect(sceneTacticalBoardRect,
                new Vector2(cx - tacticalW * 0.5f, tacticalTop - tacticalH),
                new Vector2(cx + tacticalW * 0.5f, tacticalTop));

            float lowerTop = tacticalTop - tacticalH - lowerGap;
            // Recalculate with the exact lowerH after float arithmetic.
            lowerH = lowerTop - bottom;
            puzzleW = Mathf.Clamp(lowerH * 0.501f / Mathf.Max(0.55f, aspect), 0.25f, 0.50f);
            nextPanelW = Mathf.Clamp(puzzleW * 0.68f, 0.18f, 0.28f);
            lowerTotalW = puzzleW + colGap + nextPanelW;

            float puzzleLeft = cx - lowerTotalW * 0.5f;
            ApplySceneRect(scenePuzzleBoardAnchorRect,
                new Vector2(puzzleLeft, bottom),
                new Vector2(puzzleLeft + puzzleW, bottom + lowerH));

            float rLeft = puzzleLeft + puzzleW + colGap;
            float rRight = rLeft + nextPanelW;

            // Next panel: occupies the upper portion of the right column.
            float nextH = Mathf.Clamp(nextPanelW * 1.60f + 0.040f, 0.130f, 0.210f);
            float nextTop = bottom + lowerH;
            float rotW = Mathf.Clamp(nextPanelW * 0.55f, 0.075f, 0.115f);
            float rotH = rotW * aspect;
            float rotCx = (rLeft + rRight) * 0.5f;

            // Trận 1v1: ẩn ô TIẾP, dời nút Xoay xuống đáy — cả cột phải dành cho bàn đối thủ.
            bool opponentColumn = MultiplayerMatch.Active && opponentMiniPanelRect != null;
            if (sceneNextPanelRect != null)
                sceneNextPanelRect.gameObject.SetActive(!opponentColumn);

            if (opponentColumn)
            {
                float rotBottom = bottom + 0.006f;
                if (rotateButtonRect != null)
                    ApplySceneRect(rotateButtonRect,
                        new Vector2(rotCx - rotW * 0.5f, rotBottom),
                        new Vector2(rotCx + rotW * 0.5f, rotBottom + rotH));

                float attackBottom = rotBottom + rotH + 0.008f;
                const float attackH = 0.034f;
                if (attackButtonRect != null)
                    ApplySceneRect(attackButtonRect,
                        new Vector2(rLeft + 0.008f, attackBottom),
                        new Vector2(rRight - 0.008f, attackBottom + attackH));

                LayoutOpponentMiniBoard(rLeft, rRight, nextTop, attackBottom + attackH + 0.012f, aspect);
            }
            else
            {
                ApplySceneRect(sceneNextPanelRect,
                    new Vector2(rLeft, nextTop - nextH),
                    new Vector2(rRight, nextTop));
                LayoutNextPreviewInPanel();

                if (rotateButtonRect != null)
                {
                    float rotTop = nextTop - nextH - 0.022f;
                    ApplySceneRect(rotateButtonRect,
                        new Vector2(rotCx - rotW * 0.5f, rotTop - rotH),
                        new Vector2(rotCx + rotW * 0.5f, rotTop));
                }
            }

            if (statusText != null)
                ApplySceneRect(statusText.rectTransform,
                    new Vector2(0.022f, tacticalTop - tacticalH - 0.002f),
                    new Vector2(cx - lowerTotalW * 0.5f - 0.010f, tacticalTop));
        }

        // Landscape: puzzle board trái (tỉ lệ 2:1 dọc), bàn chiến thuật vuông giữa-phải,
        // cột phụ (TIẾP/Xoay hoặc bàn đối thủ 1v1) sát mép phải.
        Image gameplayBgImage;

        // Nền gameplay landscape mới (bg-gameplay) phủ kín màn hình. Tạo Image riêng ở
        // đáy canvas (không phụ thuộc element dựng sẵn) + ẩn backdrop gỗ world-space cũ.
        void EnsureGameplayBackground()
        {
            if (sceneGameplayCanvas == null)
                return;

            // Ẩn mọi backdrop gỗ world-space cũ.
            foreach (var woodGo in GameObject.FindObjectsOfType<GameObject>())
                if (woodGo.name.StartsWith("Warm Wood Backdrop") && woodGo.activeSelf)
                    woodGo.SetActive(false);

            // Ẩn nền gỗ dựng sẵn (RawImage "Background") để lộ nền mới.
            if (sceneBackgroundRect != null)
            {
                var rawBg = sceneBackgroundRect.GetComponent<RawImage>();
                if (rawBg != null) rawBg.enabled = false;
                var imgBg = sceneBackgroundRect.GetComponent<Image>();
                if (imgBg != null) imgBg.enabled = false;
            }

            if (gameplayBgImage != null)
                return;

            var bgSpr = RuntimeArt.LoadV3Sprite("screen-gameplay/bg-gameplay.png");
            if (bgSpr == null)
                return;

            var go = Ui.Panel(sceneGameplayCanvas.transform, "Gameplay BG", Color.white);
            Ui.Stretch(go);
            go.transform.SetAsFirstSibling();
            gameplayBgImage = go.GetComponent<Image>();
            gameplayBgImage.sprite = bgSpr;
            gameplayBgImage.type = Image.Type.Simple;
            gameplayBgImage.preserveAspect = false;
            gameplayBgImage.raycastTarget = false;
            var arf = go.AddComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            arf.aspectRatio = (float)bgSpr.texture.width / bgSpr.texture.height;
        }

        bool gameplayFramesApplied;
        bool gameplayHudApplied;
        RectTransform hudTitleRect, hudCoinRect;
        Text hudTitleText, hudScoreText, hudTurnText;

        Image MakeSpriteImage(Transform parent, string name, string asset, Rect crop, bool raycast)
        {
            var spr = RuntimeArt.LoadV3SubSprite(asset, crop);
            var go = Ui.Panel(parent, name, Color.white);
            var img = go.GetComponent<Image>();
            img.sprite = spr;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.raycastTarget = raycast;
            return img;
        }

        // HUD mới: ẩn dải header gỗ; banner MÀN X (frame-title), panel ĐIỂM|LƯỢT (frame-coin),
        // nút tạm dừng (btn-tamdung) + xoay (btn-xoay); bỏ hộp gỗ nhỏ trong NEXT.
        void EnsureGameplayHud()
        {
            if (gameplayHudApplied || safeAreaRoot == null)
                return;

            var titleSpr = RuntimeArt.LoadV3SubSprite("screen-gameplay/frame-title.png", new Rect(0.130f, 0.398f, 0.740f, 0.270f));
            if (titleSpr == null)
                return;
            gameplayHudApplied = true;

            // Ẩn dải header gỗ dựng sẵn.
            if (sceneHeaderRect != null)
            {
                var raw = sceneHeaderRect.GetComponent<RawImage>(); if (raw != null) raw.enabled = false;
                var im = sceneHeaderRect.GetComponent<Image>(); if (im != null) im.enabled = false;
            }
            if (sceneLevelText != null) sceneLevelText.gameObject.SetActive(false);
            if (sceneMoveText != null) sceneMoveText.gameObject.SetActive(false);

            // Banner MÀN X.
            var title = MakeSpriteImage(safeAreaRoot.transform, "HUD Title", "screen-gameplay/frame-title.png", new Rect(0.130f, 0.398f, 0.740f, 0.270f), false);
            hudTitleRect = title.rectTransform;
            hudTitleText = Ui.Text(title.transform, "MÀN " + journeyLevel, RuntimeArt.LoadMenuButtonFont(), 42, new Color(1f, 0.98f, 0.9f), TextAnchor.MiddleCenter);
            hudTitleText.fontStyle = FontStyle.Bold;
            hudTitleText.raycastTarget = false;
            hudTitleText.resizeTextForBestFit = false;
            var ttr = hudTitleText.rectTransform; ttr.anchorMin = new Vector2(0.14f, 0.14f); ttr.anchorMax = new Vector2(0.9f, 0.92f); ttr.offsetMin = ttr.offsetMax = Vector2.zero;
            AddDarkWoodTextEdge(hudTitleText, 0.9f, 0.9f);

            // Panel điểm | lượt.
            var coin = MakeSpriteImage(safeAreaRoot.transform, "HUD Coin", "screen-gameplay/frame-coin.png", new Rect(0.151f, 0.422f, 0.695f, 0.234f), false);
            hudCoinRect = coin.rectTransform;
            // Số điểm căn GIỮA khung con ĐIỂM (nửa trái), số lượt giữa khung con LƯỢT (nửa phải).
            hudScoreText = Ui.Text(coin.transform, "0", RuntimeArt.LoadMenuButtonFont(), 28, new Color(1f, 0.98f, 0.9f), TextAnchor.MiddleCenter);
            hudScoreText.fontStyle = FontStyle.Bold; hudScoreText.raycastTarget = false;
            var sr = hudScoreText.rectTransform; sr.anchorMin = new Vector2(0.135f, 0.06f); sr.anchorMax = new Vector2(0.475f, 0.55f); sr.offsetMin = sr.offsetMax = Vector2.zero;
            hudTurnText = Ui.Text(coin.transform, "0", RuntimeArt.LoadMenuButtonFont(), 28, new Color(1f, 0.98f, 0.9f), TextAnchor.MiddleCenter);
            hudTurnText.fontStyle = FontStyle.Bold; hudTurnText.raycastTarget = false;
            var tr = hudTurnText.rectTransform; tr.anchorMin = new Vector2(0.50f, 0.06f); tr.anchorMax = new Vector2(0.84f, 0.55f); tr.offsetMin = tr.offsetMax = Vector2.zero;

            // Nút tạm dừng: reparent về safeAreaRoot để ApplySceneRect đặt theo cả màn
            // (trước đây parent là dải header mỏng nên nút bị dẹp).
            if (pauseButtonRect != null)
                pauseButtonRect.SetParent(safeAreaRoot.transform, false);
            ReskinButton(pauseButtonRect, "screen-gameplay/btn-tamdung.png", new Rect(0.365f, 0.309f, 0.268f, 0.430f));
            ReskinButton(rotateButtonRect, "screen-gameplay/btn-xoay.png", new Rect(0.372f, 0.344f, 0.255f, 0.406f));

            // Bỏ hộp gỗ nhỏ trong ô NEXT (giữ preview khối), + ẩn chữ "TIẾP" cũ.
            if (sceneNextPreviewRect != null)
            {
                var raw = sceneNextPreviewRect.GetComponent<RawImage>(); if (raw != null) raw.enabled = false;
                var im = sceneNextPreviewRect.GetComponent<Image>(); if (im != null) im.enabled = false;
            }
            if (sceneNextText != null) sceneNextText.gameObject.SetActive(false);
        }

        // Thay nền nút bằng sprite mới (ẩn graphic gỗ cũ + icon con, đặt frame con fill).
        void ReskinButton(RectTransform rect, string asset, Rect crop)
        {
            if (rect == null)
                return;
            var im = rect.GetComponent<Image>(); if (im != null) im.enabled = false;
            var raw = rect.GetComponent<RawImage>(); if (raw != null) raw.enabled = false;
            // Ẩn icon/label con cũ (sprite mới đã có icon baked-in).
            foreach (Transform child in rect)
                if (child.name != "Runtime BtnSkin") child.gameObject.SetActive(false);

            var existing = FindChildLoose(rect, "Runtime BtnSkin");
            Image skin = existing != null ? existing.GetComponent<Image>() : MakeSpriteImage(rect, "Runtime BtnSkin", asset, crop, true);
            if (existing != null) skin.sprite = RuntimeArt.LoadV3SubSprite(asset, crop);
            Ui.Stretch(skin.gameObject);
            skin.transform.SetAsFirstSibling();
            // Nút vẫn bấm được: dùng skin làm targetGraphic.
            var btn = rect.GetComponent<Button>();
            if (btn != null) { btn.targetGraphic = skin; skin.raycastTarget = true; }
        }

        // Thay khung gỗ cũ của bàn cờ / xếp gạch / NEXT bằng khung mới (screen-gameplay).
        void EnsureGameplayFrames()
        {
            if (gameplayFramesApplied)
                return;

            bool anyReady = false;
            anyReady |= SetGraphicFrame(sceneTacticalBoardRect, "screen-gameplay/frame-banco.png", new Rect(0.202f, 0.089f, 0.563f, 0.802f));
            anyReady |= SetGraphicFrame(scenePuzzleBoardAnchorRect, "screen-gameplay/frame-xepgach.png", new Rect(0.238f, 0.112f, 0.520f, 0.786f));
            anyReady |= SetGraphicFrame(sceneNextPanelRect, "screen-gameplay/frame-next.png", new Rect(0.344f, 0.063f, 0.313f, 0.848f));
            if (anyReady)
                gameplayFramesApplied = true;
        }

        // Thêm 1 frame con (first-sibling, fill) làm nền khung cho rect; tắt graphic gỗ cũ
        // của rect (Image/RawImage) nhưng GIỮ text (TMP) nếu có. Tránh xung đột Graphic.
        bool SetGraphicFrame(RectTransform rect, string asset, Rect crop)
        {
            if (rect == null)
                return false;
            var cropped = RuntimeArt.LoadV3SubSprite(asset, crop);
            if (cropped == null)
                return false;

            var oldImg = rect.GetComponent<Image>();
            if (oldImg != null) oldImg.enabled = false;
            var oldRaw = rect.GetComponent<RawImage>();
            if (oldRaw != null) oldRaw.enabled = false;

            var existing = FindChildLoose(rect, "Runtime Frame");
            Image img;
            if (existing != null)
            {
                img = existing.GetComponent<Image>();
            }
            else
            {
                var go = Ui.Panel(rect, "Runtime Frame", Color.white);
                Ui.Stretch(go);
                img = go.GetComponent<Image>();
            }
            img.transform.SetAsFirstSibling();
            img.sprite = cropped;
            img.type = Image.Type.Simple;
            img.preserveAspect = false;
            img.raycastTarget = false;
            img.color = Color.white;
            return true;
        }

        void ApplyLandscapeGameplayRegionLayout(float aspect)
        {
            EnsureGameplayBackground();
            EnsureGameplayFrames();
            EnsureGameplayHud();

            // Trận 1v1 (design #7): bố cục riêng — bàn xếp gạch bên trái, cột giữa HUD,
            // bàn đối thủ mini bên phải.
            if (MultiplayerMatch.Active)
            {
                ApplyOnlineRegionLayout(aspect);
                return;
            }

            const float top = 0.975f;
            const float bottom = 0.02f;

            // Header — dải trên cùng (tạm dừng trái · MÀN X giữa · điểm phải).
            float headerH = 0.105f;
            ApplySceneRect(sceneHeaderRect,
                new Vector2(0.03f, top - headerH),
                new Vector2(0.97f, top));
            LayoutHeaderChildren();

            // Board dùng toàn bộ chiều cao dưới header (không chừa dải trống) để đỡ thưa.
            float contentTop = top - headerH - 0.012f;
            float boardTop = contentTop;
            float contentH = boardTop - bottom;

            // Tính bề rộng từng khối rồi CĂN GIỮA cả cụm theo chiều ngang.
            float tacMaxW = 0.44f;
            float tacH = contentH;
            float tacW = tacH / Mathf.Max(1f, aspect);
            if (tacW > tacMaxW) { tacW = tacMaxW; tacH = tacW * aspect; }
            float puzzleH = contentH;
            float puzzleW = puzzleH * 0.501f / Mathf.Max(1f, aspect);
            float sideW = 0.10f;           // NEXT + XOAY hẹp lại (cột dọc)
            float gap1 = 0.022f, gap2 = 0.026f;
            float totalW = tacW + gap1 + puzzleW + gap2 + sideW;
            float startX = (1f - totalW) * 0.5f;

            // Bàn chiến thuật (vuông) — đầu cụm, căn giữa dọc.
            float tacLeft = startX;
            float tacCy = (bottom + boardTop) * 0.5f;
            ApplySceneRect(sceneTacticalBoardRect,
                new Vector2(tacLeft, tacCy - tacH * 0.5f),
                new Vector2(tacLeft + tacW, tacCy + tacH * 0.5f));

            // Bàn xếp gạch ngay sau bàn cờ.
            float puzzleLeft = tacLeft + tacW + gap1;
            ApplySceneRect(scenePuzzleBoardAnchorRect,
                new Vector2(puzzleLeft, bottom),
                new Vector2(puzzleLeft + puzzleW, boardTop));

            // Cột phụ BÊN PHẢI (NEXT + XOAY hoặc bàn đối thủ) — ngay sau bàn xếp gạch.
            float sideLeft = puzzleLeft + puzzleW + gap2;
            float sideRight = sideLeft + sideW;
            sideLeft = sideRight - sideW;

            float rotW = Mathf.Clamp(sideW * 0.60f, 0.062f, 0.098f);
            float rotH = rotW * aspect;
            float rotCx = (sideLeft + sideRight) * 0.5f;

            // Trận 1v1: ẩn ô TIẾP, cột phải dành cho bàn đối thủ + nút.
            bool opponentColumn = MultiplayerMatch.Active && opponentMiniPanelRect != null;
            if (sceneNextPanelRect != null)
                sceneNextPanelRect.gameObject.SetActive(!opponentColumn);

            if (opponentColumn)
            {
                float rotBottom = bottom + 0.008f;
                if (rotateButtonRect != null)
                    ApplySceneRect(rotateButtonRect,
                        new Vector2(rotCx - rotW * 0.5f, rotBottom),
                        new Vector2(rotCx + rotW * 0.5f, rotBottom + rotH));

                float attackBottom = rotBottom + rotH + 0.012f;
                const float attackH = 0.052f;
                if (attackButtonRect != null)
                    ApplySceneRect(attackButtonRect,
                        new Vector2(sideLeft + 0.008f, attackBottom),
                        new Vector2(sideRight - 0.008f, attackBottom + attackH));

                LayoutOpponentMiniBoard(sideLeft, sideRight, boardTop, attackBottom + attackH + 0.016f, aspect);
            }
            else
            {
                // NEXT: dọc đúng tỉ lệ khung frame-next (~0.554 rộng/cao) → không bị bè.
                // Hạ xuống 1 chút để cách panel coin.
                float nextTop = boardTop - 0.035f;
                float nextW = sideW;
                float nextH = nextW * aspect / 0.554f;
                float nextCx = (sideLeft + sideRight) * 0.5f;
                ApplySceneRect(sceneNextPanelRect,
                    new Vector2(nextCx - nextW * 0.5f, nextTop - nextH),
                    new Vector2(nextCx + nextW * 0.5f, nextTop));
                LayoutNextPreviewInPanel();

                // XOAY: nút hẹp (vuông), dưới NEXT.
                float rw = Mathf.Clamp(sideW * 0.72f, 0.052f, 0.078f);
                float rh = rw * aspect;
                if (rotateButtonRect != null)
                {
                    float rotTop = nextTop - nextH - 0.045f;
                    ApplySceneRect(rotateButtonRect,
                        new Vector2(nextCx - rw * 0.5f, rotTop - rh),
                        new Vector2(nextCx + rw * 0.5f, rotTop));
                }
            }

            if (statusText != null)
                ApplySceneRect(statusText.rectTransform,
                    new Vector2(sideLeft, bottom),
                    new Vector2(sideRight, bottom + 0.14f));
        }

        // Dựng HUD online (design #7) một lần: thanh máu, nhân vật/VS, tấn công/phòng thủ,
        // thanh năng lượng, 3 nút kỹ năng. Vị trí do ApplyOnlineRegionLayout đặt.
        void BuildOnlineHud(Transform parent)
        {
            if (onlineHudBuilt) return;
            onlineHudBuilt = true;
            const string DIR = "screen-online/";
            var titleFont = RuntimeArt.LoadMenuButtonFont();
            var hpGreen = new Color(0.36f, 0.84f, 0.30f, 1f);
            var cupGold = new Color(1f, 0.86f, 0.35f);

            // ================= THANH TRÊN (avatar + tên + cúp + máu + VS) =================
            var topBar = Ui.Panel(parent, "Runtime Online TopBar", new Color(0, 0, 0, 0));
            onlineTopBarRect = topBar.GetComponent<RectTransform>();
            topBar.GetComponent<Image>().raycastTarget = false;
            var tb = topBar.transform;

            // Avatar + tên + cúp (người chơi bên trái).
            playerAvatarImg = BuildAvatar(tb, DIR + "khungavar-player.png", new Rect(0.262f, 0.108f, 0.475f, 0.784f),
                DIR + "player.png", new Rect(0.401f, 0.590f, 0.166f, 0.273f), new Vector2(0.098f, 0.12f), new Vector2(0.158f, 0.88f));
            playerNameText = MakeBarText(tb, "Player1", titleFont, 28, TextAnchor.UpperLeft, new Vector2(0.166f, 0.46f), new Vector2(0.260f, 0.83f));
            var cup1 = MakeSpriteImage(tb, "Runtime Cup1", DIR + "decor-cup.png", new Rect(0.319f, 0.261f, 0.366f, 0.518f), false);
            Ui.Rect(cup1, new Vector2(0.166f, 0.12f), new Vector2(0.185f, 0.40f), Vector2.zero);
            playerCupText = MakeBarText(tb, "1280", titleFont, 24, TextAnchor.MiddleLeft, new Vector2(0.190f, 0.06f), new Vector2(0.260f, 0.46f));
            playerCupText.color = cupGold;
            // Tim + thanh máu (bên trái, gần giữa).
            var heart1 = MakeSpriteImage(tb, "Runtime Heart1", DIR + "decor-tim.png", new Rect(0.327f, 0.298f, 0.348f, 0.461f), false);
            Ui.Rect(heart1, new Vector2(0.260f, 0.34f), new Vector2(0.288f, 0.72f), Vector2.zero);
            hpYouFill = MakeHpFill(tb, new Vector2(0.29f, 0.40f), new Vector2(0.428f, 0.70f), false);
            hpYouFill.color = hpGreen;
            hpYouText = MakeBarText(tb, "100/100", font, 22, TextAnchor.MiddleCenter, new Vector2(0.29f, 0.37f), new Vector2(0.428f, 0.73f));

            // VS giữa.
            var vs = MakeSpriteImage(tb, "Runtime Online VS", DIR + "decor-vs.png", new Rect(0.214f, 0.307f, 0.570f, 0.440f), false);
            Ui.Rect(vs, new Vector2(0.444f, 0.16f), new Vector2(0.556f, 0.92f), Vector2.zero);

            // Đối thủ (mirror bên phải).
            hpOppFill = MakeHpFill(tb, new Vector2(0.572f, 0.40f), new Vector2(0.710f, 0.70f), false);
            hpOppFill.color = hpGreen;
            hpOppText = MakeBarText(tb, "100/100", font, 22, TextAnchor.MiddleCenter, new Vector2(0.572f, 0.37f), new Vector2(0.710f, 0.73f));
            var heart2 = MakeSpriteImage(tb, "Runtime Heart2", DIR + "decor-tim.png", new Rect(0.327f, 0.298f, 0.348f, 0.461f), false);
            Ui.Rect(heart2, new Vector2(0.712f, 0.34f), new Vector2(0.740f, 0.72f), Vector2.zero);
            oppNameText = MakeBarText(tb, "Player2", titleFont, 28, TextAnchor.UpperRight, new Vector2(0.740f, 0.46f), new Vector2(0.834f, 0.83f));
            var cup2 = MakeSpriteImage(tb, "Runtime Cup2", DIR + "decor-cup.png", new Rect(0.319f, 0.261f, 0.366f, 0.518f), false);
            Ui.Rect(cup2, new Vector2(0.815f, 0.12f), new Vector2(0.834f, 0.40f), Vector2.zero);
            oppCupText = MakeBarText(tb, "1295", titleFont, 24, TextAnchor.MiddleRight, new Vector2(0.740f, 0.06f), new Vector2(0.810f, 0.46f));
            oppCupText.color = cupGold;
            oppAvatarImg = BuildAvatar(tb, DIR + "khungavar-doithu.png", new Rect(0.260f, 0.108f, 0.495f, 0.801f),
                DIR + "doithu.png", new Rect(0.542f, 0.588f, 0.197f, 0.319f), new Vector2(0.842f, 0.12f), new Vector2(0.902f, 0.88f));

            // ================= KHUNG BÀN (đặt sau lưng bàn chức năng ở layout) =================
            playerBoardFrameImg = MakeSpriteImage(parent, "Runtime Board Frame You", DIR + "board-player.png", new Rect(0.324f, 0.044f, 0.350f, 0.929f), false);
            playerBoardFrameImg.preserveAspect = false;
            oppBoardFrameImg = MakeSpriteImage(parent, "Runtime Board Frame Opp", DIR + "board-doithu.png", new Rect(0.324f, 0.041f, 0.327f, 0.932f), false);
            oppBoardFrameImg.preserveAspect = false;

            // ================= THANH NĂNG LƯỢNG (dưới mỗi bàn) =================
            onlineYouEnergyRect = BuildEnergyBar(parent, "You", DIR + "icon-nangluong-player.png", new Rect(0.335f, 0.292f, 0.331f, 0.438f), youEnergyColor, youEnergySeg, out onlineEnergyText);
            onlineOppEnergyRect = BuildEnergyBar(parent, "Opp", DIR + "icon-nangluong-doithu.png", new Rect(0.292f, 0.247f, 0.395f, 0.553f), oppEnergyColor, oppEnergySeg, out onlineOppEnergyText);

            // ================= CỘT GIỮA (nhân vật + 3 lá kỹ năng + ATTACK/SHIELD) =================
            var center = Ui.Panel(parent, "Runtime Online Center", new Color(0, 0, 0, 0));
            onlineCenterRect = center.GetComponent<RectTransform>();
            center.GetComponent<Image>().raycastTarget = false;
            var cc = center.transform;

            // Nhân vật đối đầu.
            // Hai nhân vật xích sát, chồng nhẹ ở giữa cho ra dáng lao vào đánh nhau.
            var enemy = MakeSpriteImage(cc, "Runtime Online Enemy", DIR + "doithu.png", new Rect(0.346f, 0.209f, 0.516f, 0.698f), false);
            Ui.Rect(enemy, new Vector2(0.42f, 0.50f), new Vector2(1.04f, 1.03f), Vector2.zero);
            var hero = MakeSpriteImage(cc, "Runtime Online Hero", DIR + "player.png", new Rect(0.283f, 0.271f, 0.475f, 0.593f), false);
            Ui.Rect(hero, new Vector2(-0.09f, 0.50f), new Vector2(0.58f, 1.03f), Vector2.zero); // rộng hơn để cao bằng nhân vật đỏ

            // 3 lá kỹ năng (THẢ RÁC / HÚT MÁU / CUỒNG NỘ) — số đếm trên, tên dưới.
            string[] skillAssets = { "btn-chieu1.png", "btn-chieu2.png", "btn-chieu3.png" };
            Rect[] skillCrops = { new Rect(0.271f, 0.138f, 0.459f, 0.783f), new Rect(0.288f, 0.146f, 0.423f, 0.774f), new Rect(0.301f, 0.152f, 0.399f, 0.712f) };
            string[] skillNames = { "THẢ RÁC", "HÚT MÁU", "CUỒNG NỘ" };
            OnlineSkill[] map = { OnlineSkill.Garbage, OnlineSkill.Attack, OnlineSkill.Shield };
            float[] cx = { 0.243f, 0.50f, 0.757f };
            float half = 0.123f;
            for (int i = 0; i < 3; i++)
            {
                var si = i;
                var b = MakeSpriteButton(cc, "Runtime Online Skill " + (i + 1), DIR + skillAssets[i], skillCrops[i],
                    new Vector2(0.5f, 0.5f), new Vector2(120, 120), () => TryUseSkill(map[si]));
                onlineSkillBtn[i] = b;
                onlineSkillRect[i] = b.GetComponent<RectTransform>();
                var bImg = b.GetComponent<Image>(); if (bImg != null) bImg.preserveAspect = false; // vuông hơn theo rect
                Ui.Rect(b.gameObject, new Vector2(cx[i] - half, 0.27f), new Vector2(cx[i] + half, 0.49f), Vector2.zero);
                var skLbl = MakeBarText(cc, skillNames[i], titleFont, 27, TextAnchor.UpperCenter, new Vector2(cx[i] - half - 0.02f, 0.175f), new Vector2(cx[i] + half + 0.02f, 0.25f));
                skLbl.horizontalOverflow = HorizontalWrapMode.Overflow;
                var skOutline = skLbl.gameObject.AddComponent<Outline>();
                skOutline.effectColor = new Color(0f, 0f, 0f, 0.95f);
                skOutline.effectDistance = new Vector2(2.2f, -2.2f);
            }

            // ATTACK / SHIELD.
            var atk = MakeSpriteButton(cc, "Runtime Online Atk", DIR + "btn-tancong.png", new Rect(0.105f, 0.338f, 0.771f, 0.369f),
                new Vector2(0.5f, 0.5f), new Vector2(100, 40), () => TryUseSkill(OnlineSkill.Attack));
            onlineAtkRect = atk.GetComponent<RectTransform>();
            Ui.Rect(atk.gameObject, new Vector2(0.13f, 0.055f), new Vector2(0.488f, 0.185f), Vector2.zero);
            onlineAtkText = MakeBarText(atk.transform, "TẤN CÔNG\nx3", titleFont, 24, TextAnchor.MiddleCenter, new Vector2(0.28f, 0.05f), new Vector2(0.98f, 0.95f));
            onlineAtkText.lineSpacing = 0.78f;
            onlineAtkText.horizontalOverflow = HorizontalWrapMode.Overflow;
            var def = MakeSpriteButton(cc, "Runtime Online Def", DIR + "btn-khien.png", new Rect(0.128f, 0.388f, 0.744f, 0.311f),
                new Vector2(0.5f, 0.5f), new Vector2(100, 40), () => TryUseSkill(OnlineSkill.Shield));
            onlineDefRect = def.GetComponent<RectTransform>();
            Ui.Rect(def.gameObject, new Vector2(0.512f, 0.055f), new Vector2(0.87f, 0.185f), Vector2.zero);
            onlineDefText = MakeBarText(def.transform, "KHIÊN\nx3", titleFont, 24, TextAnchor.MiddleCenter, new Vector2(0.28f, 0.05f), new Vector2(0.98f, 0.95f));
            onlineDefText.lineSpacing = 0.78f;
            onlineDefText.horizontalOverflow = HorizontalWrapMode.Overflow;
        }

        // Chân dung trong khung avatar (chân dung sau, khung trước).
        Image BuildAvatar(Transform parent, string frameAsset, Rect frameCrop, string portraitAsset, Rect portraitCrop, Vector2 min, Vector2 max)
        {
            var cont = Ui.Panel(parent, "Runtime Avatar", new Color(0.04f, 0.06f, 0.12f, 0.85f));
            cont.GetComponent<Image>().raycastTarget = false;
            Ui.Rect(cont, min, max, Vector2.zero);
            var portrait = MakeSpriteImage(cont.transform, "Portrait", portraitAsset, portraitCrop, false);
            Ui.Rect(portrait, new Vector2(0.14f, 0.12f), new Vector2(0.86f, 0.88f), Vector2.zero);
            var frame = MakeSpriteImage(cont.transform, "Frame", frameAsset, frameCrop, false);
            frame.preserveAspect = false;
            Ui.Stretch(frame.gameObject);
            return portrait;
        }

        // Text thanh HUD: đậm, viền tối, không chặn chuột.
        Text MakeBarText(Transform parent, string value, Font f, int size, TextAnchor anchor, Vector2 min, Vector2 max)
        {
            var t = Ui.Text(parent, value, f, size, new Color(1f, 0.98f, 0.92f), anchor);
            t.fontStyle = FontStyle.Bold; t.raycastTarget = false;
            Ui.Rect(t, min, max, Vector2.zero);
            AddDarkWoodTextEdge(t, 0.7f, 0.85f);
            return t;
        }

        // Thanh năng lượng: icon sét + 10 ô + "x/10".
        RectTransform BuildEnergyBar(Transform parent, string tag, string iconAsset, Rect iconCrop, Color onColor, Image[] segs, out Text valueText)
        {
            var cont = Ui.Panel(parent, "Runtime Energy " + tag, new Color(0, 0, 0, 0));
            cont.GetComponent<Image>().raycastTarget = false;
            var icon = MakeSpriteImage(cont.transform, "EnIcon", iconAsset, iconCrop, false);
            Ui.Rect(icon, new Vector2(0.0f, 0.06f), new Vector2(0.15f, 0.94f), Vector2.zero);
            float x0 = 0.17f, x1 = 0.72f, w = (x1 - x0) / segs.Length;
            for (int i = 0; i < segs.Length; i++)
            {
                var seg = Ui.Panel(cont.transform, "Seg", energyOffColor).GetComponent<Image>();
                seg.raycastTarget = false;
                seg.sprite = RoundUiSprite(); seg.type = Image.Type.Sliced;
                float sx = x0 + i * w;
                Ui.Rect(seg, new Vector2(sx + 0.06f * w, 0.34f), new Vector2(sx + 0.94f * w, 0.66f), Vector2.zero); // ô thấp lại, không dài quá
                segs[i] = seg;
            }
            valueText = Ui.Text(cont.transform, "0/10", font, 30, Color.white, TextAnchor.MiddleLeft);
            valueText.fontStyle = FontStyle.Bold; valueText.raycastTarget = false;
            valueText.horizontalOverflow = HorizontalWrapMode.Overflow;
            Ui.Rect(valueText, new Vector2(0.74f, -0.05f), new Vector2(1.12f, 1.05f), Vector2.zero);
            AddDarkWoodTextEdge(valueText, 0.6f, 0.8f);
            return cont.GetComponent<RectTransform>();
        }

        static Sprite _roundUiSprite;
        static Sprite RoundUiSprite()
        {
            if (_roundUiSprite == null)
                _roundUiSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
            return _roundUiSprite;
        }

        // Sprite bo góc lớn (9-slice) → khi thanh thấp thì 2 đầu thành nửa tròn (viên thuốc).
        static Sprite _pillSprite;
        static Sprite PillSprite()
        {
            if (_pillSprite != null)
                return _pillSprite;
            int s = 64;
            float r = 11f, c = (s - 1) * 0.5f, flat = c - r;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp; tex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = Mathf.Max(0f, Mathf.Abs(x - c) - flat);
                    float dy = Mathf.Max(0f, Mathf.Abs(y - c) - flat);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(r - dist + 0.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            tex.Apply();
            _pillSprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(r, r, r, r));
            return _pillSprite;
        }

        // Rãnh máu bo tròn (xanh-đậm hòa panel) + fill xanh bo tròn, thụt vào chút.
        Image MakeHpFill(Transform parent, Vector2 min, Vector2 max, bool rightOrigin)
        {
            var grooveImg = Ui.Panel(parent, "HpGroove", new Color(0.03f, 0.05f, 0.11f, 0.92f)).GetComponent<Image>();
            grooveImg.raycastTarget = false;
            grooveImg.sprite = PillSprite(); grooveImg.type = Image.Type.Sliced; // viên thuốc bo tròn 2 đầu
            Ui.Rect(grooveImg, min, max, Vector2.zero);
            var fill = Ui.Panel(grooveImg.transform, "HpFill", new Color(0.34f, 0.82f, 0.28f, 1f)).GetComponent<Image>();
            fill.raycastTarget = false;
            fill.sprite = PillSprite();
            fill.type = Image.Type.Sliced; // pill; điều khiển máu bằng bề rộng
            var fr = fill.rectTransform;
            fr.anchorMin = new Vector2(0.012f, 0.07f); fr.anchorMax = new Vector2(0.988f, 0.93f); // viền đen mỏng đều
            fr.offsetMin = Vector2.zero; fr.offsetMax = Vector2.zero;
            return fill;
        }

        // Bố cục online (design #7): xếp gạch trái, HUD giữa, đối thủ mini phải.
        void ApplyOnlineRegionLayout(float aspect)
        {
            // Ẩn phần offline không dùng: bàn cờ chiến thuật, ô TIẾP, bàn cờ mini đối thủ, thanh kỹ năng gỗ.
            if (sceneTacticalBoardRect != null) sceneTacticalBoardRect.gameObject.SetActive(false);
            if (sceneNextPanelRect != null) sceneNextPanelRect.gameObject.SetActive(false);
            if (opponentTacticalPanelRect != null) opponentTacticalPanelRect.gameObject.SetActive(false);
            if (attackButtonRect != null) attackButtonRect.gameObject.SetActive(false);

            BuildOnlineHud(safeAreaRoot != null ? safeAreaRoot : sceneGameplayRootRect);

            // Header tối giản: chỉ nút tạm dừng góc trái.
            if (sceneHeaderRect != null)
            {
                var raw = sceneHeaderRect.GetComponent<RawImage>(); if (raw != null) raw.enabled = false;
                var im = sceneHeaderRect.GetComponent<Image>(); if (im != null) im.enabled = false;
            }
            if (hudTitleRect != null) hudTitleRect.gameObject.SetActive(false);
            if (hudCoinRect != null) hudCoinRect.gameObject.SetActive(false);
            if (pauseButtonRect != null) ApplySceneRect(pauseButtonRect, new Vector2(0.024f, 0.888f), new Vector2(0.060f, 0.978f));

            // Thanh trên — chừa lề, không sát mép trên/hai bên.
            if (onlineTopBarRect != null) ApplySceneRect(onlineTopBarRect, new Vector2(0.015f, 0.842f), new Vector2(0.985f, 0.972f));

            // --- Hai bàn cùng cỡ, tỉ lệ 10x20 (to hơn + dời vào trong) ---
            float boardTop = 0.825f, boardBottom = 0.145f;
            float boardH = boardTop - boardBottom;
            float boardW = boardH * 0.560f / Mathf.Max(1f, aspect);

            // Bàn người chơi (trái) — dời vào trong.
            float youLeft = 0.138f, youRight = youLeft + boardW;
            ApplySceneRect(scenePuzzleBoardAnchorRect, new Vector2(youLeft, boardBottom), new Vector2(youRight, boardTop));
            DisablePuzzleAnchorFrame();
            PlaceBoardFrame(playerBoardFrameImg, scenePuzzleBoardAnchorRect, youLeft, boardBottom, youRight, boardTop, 0.030f, 0.016f, 0.939f, 0.971f);

            // Bỏ nút XOAY khỏi giao diện online.
            if (rotateButtonRect != null) rotateButtonRect.gameObject.SetActive(false);

            // Bàn đối thủ (phải) — dời vào trong.
            float oppRight = 0.862f, oppLeft = oppRight - boardW;
            if (opponentMiniPanelRect != null)
            {
                ApplySceneRect(opponentMiniPanelRect, new Vector2(oppLeft, boardBottom), new Vector2(oppRight, boardTop));
                PlaceBoardFrame(oppBoardFrameImg, opponentMiniPanelRect, oppLeft, boardBottom, oppRight, boardTop, 0.034f, 0.020f, 0.938f, 0.966f);
            }

            // Thanh năng lượng dưới mỗi bàn.
            float enTop = boardBottom - 0.010f, enBottom = enTop - 0.090f;
            if (onlineYouEnergyRect != null) ApplySceneRect(onlineYouEnergyRect, new Vector2(youLeft - 0.006f, enBottom), new Vector2(youRight + 0.038f, enTop));
            if (onlineOppEnergyRect != null) ApplySceneRect(onlineOppEnergyRect, new Vector2(oppLeft - 0.006f, enBottom), new Vector2(oppRight + 0.038f, enTop));

            // Cột giữa lấp khoảng trống giữa hai bàn.
            float cLeft = youRight + 0.028f, cRight = oppLeft - 0.028f;
            if (onlineCenterRect != null) ApplySceneRect(onlineCenterRect, new Vector2(cLeft, 0.035f), new Vector2(cRight, boardTop));

            UpdateOnlineHud();
        }

        // Đặt khung bàn (sprite) sau lưng bàn chức năng, bao quanh nó theo tỉ lệ inner của khung.
        void PlaceBoardFrame(Image frame, RectTransform board, float bl, float bb, float br, float bt,
            float innerX, float innerY, float innerW, float innerH)
        {
            if (frame == null || board == null) return;
            if (frame.transform.parent != board.parent) frame.transform.SetParent(board.parent, false);
            frame.transform.SetAsFirstSibling();
            float fw = (br - bl) / innerW, fh = (bt - bb) / innerH;
            float fl = bl - innerX * fw, fbm = bb - innerY * fh;
            ApplySceneRect(frame.rectTransform, new Vector2(fl, fbm), new Vector2(fl + fw, fbm + fh));
        }

        // Tắt khung gỗ offline trên bàn xếp gạch (online dùng khung board-player riêng).
        void DisablePuzzleAnchorFrame()
        {
            if (scenePuzzleBoardAnchorRect == null) return;
            var f = FindChildLoose(scenePuzzleBoardAnchorRect, "Runtime Frame");
            if (f != null) { var im = f.GetComponent<Image>(); if (im != null) im.enabled = false; }
        }

        void SetEnergySegments(Image[] segs, int on, Color onColor)
        {
            if (segs == null) return;
            for (int i = 0; i < segs.Length; i++)
                if (segs[i] != null) segs[i].color = i < on ? onColor : energyOffColor;
        }

        // Cập nhật máu/năng lượng/số lá kỹ năng cho HUD online.
        void UpdateOnlineHud()
        {
            if (!onlineHudBuilt) return;
            int maxHp = OnlineConfig.MaxHealth;
            int youHp = Mathf.Clamp(healthSystem.Health, 0, maxHp);
            int oppHp = Mathf.Clamp(MultiplayerMatch.OpponentHealth, 0, maxHp);
            float youFrac = maxHp > 0 ? (float)youHp / maxHp : 0f;
            float oppFrac = maxHp > 0 ? (float)oppHp / maxHp : 0f;
            if (hpYouFill != null) { var a = hpYouFill.rectTransform.anchorMax; a.x = 0.012f + youFrac * 0.976f; hpYouFill.rectTransform.anchorMax = a; hpYouFill.enabled = youFrac > 0.001f; }
            if (hpOppFill != null) { var a = hpOppFill.rectTransform.anchorMax; a.x = 0.012f + oppFrac * 0.976f; hpOppFill.rectTransform.anchorMax = a; hpOppFill.enabled = oppFrac > 0.001f; }
            const int hpDisplayMax = 100;
            if (hpYouText != null) hpYouText.text = Mathf.RoundToInt(youFrac * hpDisplayMax) + "/" + hpDisplayMax;
            if (hpOppText != null) hpOppText.text = Mathf.RoundToInt(oppFrac * hpDisplayMax) + "/" + hpDisplayMax;

            int maxEn = OnlineConfig.MaxEnergy;
            int youEn = Mathf.Clamp(energySystem.Energy, 0, maxEn);
            int oppEn = Mathf.Clamp(MultiplayerMatch.OpponentEnergy, 0, maxEn);
            SetEnergySegments(youEnergySeg, youEn, youEnergyColor);
            SetEnergySegments(oppEnergySeg, oppEn, oppEnergyColor);
            if (onlineEnergyText != null) onlineEnergyText.text = youEn + "/" + maxEn;
            if (onlineOppEnergyText != null) onlineOppEnergyText.text = oppEn + "/" + maxEn;

            // Số lá kỹ năng khả dụng (theo giá in trên lá: 4/4/7).
            int[] cardCost = { 4, 4, 7 };
            for (int i = 0; i < 3; i++)
                if (onlineSkillCount[i] != null) onlineSkillCount[i].text = (youEn / cardCost[i]).ToString();

            if (oppNameText != null && !string.IsNullOrEmpty(MultiplayerMatch.OpponentName))
                oppNameText.text = MultiplayerMatch.OpponentName;
        }

        void LayoutHeaderChildren()
        {
            // HUD mới: tạm dừng trái · MÀN X giữa · panel điểm|lượt phải.
            if (hudTitleRect != null)
            {
                // Nút tạm dừng: cách mép trái + viền trên thêm chút.
                ApplySceneRect(pauseButtonRect, new Vector2(0.045f, 0.858f), new Vector2(0.10f, 0.952f));
                // Banner MÀN X: to hơn.
                ApplySceneRect(hudTitleRect, new Vector2(0.40f, 0.878f), new Vector2(0.60f, 0.995f));
                // Panel điểm|lượt: to hơn.
                ApplySceneRect(hudCoinRect, new Vector2(0.755f, 0.878f), new Vector2(0.982f, 0.995f));
                return;
            }

            // Fallback (chưa dựng HUD mới): giữ header cũ.
            if (sceneLevelText != null)
            {
                sceneLevelText.alignment = TextAlignmentOptions.MidlineLeft;
                ApplySceneRect(sceneLevelText.rectTransform, new Vector2(0.045f, 0.10f), new Vector2(0.36f, 0.90f));
            }
            if (sceneMoveText != null)
            {
                sceneMoveText.alignment = TextAlignmentOptions.Midline;
                ApplySceneRect(sceneMoveText.rectTransform, new Vector2(0.39f, 0.10f), new Vector2(0.78f, 0.90f));
            }
            if (pauseButtonRect != null)
                ApplySceneRect(pauseButtonRect, new Vector2(0.82f, 0.08f), new Vector2(0.975f, 0.92f));
        }

        void LayoutNextPreviewInPanel()
        {
            if (sceneNextPreviewRect == null)
                return;

            if (sceneNextPanelRect != null && sceneNextPreviewRect != sceneNextPanelRect && sceneNextPreviewRect.transform.IsChildOf(sceneNextPanelRect.transform))
                ApplySceneRect(sceneNextPreviewRect, new Vector2(0.12f, 0.06f), new Vector2(0.88f, 0.76f));

            if (sceneNextText != null)
                sceneNextText.alignment = TextAlignmentOptions.Top;
        }

        Bounds CalculateScenePlayBounds(RectTransform root)
        {
            if (sceneBackgroundRect != null && sceneBackgroundRect.gameObject.activeInHierarchy)
            {
                Bounds backgroundBounds = CalculateSingleRectBounds(root, sceneBackgroundRect);
                if (backgroundBounds.size.x > 1f && backgroundBounds.size.y > 1f)
                    return backgroundBounds;
            }

            return CalculateSceneContentBounds(root);
        }

        Bounds CalculateSingleRectBounds(RectTransform root, RectTransform target)
        {
            var corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Bounds bounds = new Bounds(root.InverseTransformPoint(corners[0]), Vector3.zero);
            for (int i = 1; i < corners.Length; i++)
                bounds.Encapsulate(root.InverseTransformPoint(corners[i]));
            return bounds;
        }

        Bounds CalculateSceneContentBounds(RectTransform root)
        {
            var children = root.GetComponentsInChildren<RectTransform>(true);
            var corners = new Vector3[4];
            bool hasBounds = false;
            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
            for (int i = 0; i < children.Length; i++)
            {
                var child = children[i];
                if (child == null || child == root || !child.gameObject.activeInHierarchy || child == sceneBackgroundRect)
                    continue;

                child.GetWorldCorners(corners);
                for (int c = 0; c < corners.Length; c++)
                {
                    Vector3 local = root.InverseTransformPoint(corners[c]);
                    if (!hasBounds)
                    {
                        bounds = new Bounds(local, Vector3.zero);
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(local);
                    }
                }
            }

            if (!hasBounds)
                bounds = new Bounds(root.rect.center, root.rect.size);
            return bounds;
        }

        void StretchSceneRootToScreen(RectTransform rect)
        {
            if (rect == null)
                return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            rect.anchoredPosition = Vector2.zero;
        }

        void ApplySceneRect(RectTransform rect, Vector2 min, Vector2 max)
        {
            if (rect == null)
                return;

            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        void RefreshScenePreviewCellSizes()
        {
            if (nextPreviewCells == null || nextPreviewCells.Count == 0 || sceneNextPreviewRect == null)
                return;

            nextPreviewCellSize = CalculatePreviewCellSize(sceneNextPreviewRect);
            float step = PreviewCellStep(nextPreviewCellSize);
            for (int i = 0; i < nextPreviewCells.Count; i++)
            {
                if (nextPreviewCells[i] == null)
                    continue;
                int x = i % 4;
                int y = i / 4;
                var rect = nextPreviewCells[i].rectTransform;
                rect.sizeDelta = new Vector2(nextPreviewCellSize, nextPreviewCellSize);
                rect.anchoredPosition = new Vector2((x - 1.5f) * step, (1.5f - y) * step);
            }
        }

        float CalculatePreviewCellSize(RectTransform previewRect)
        {
            if (previewRect == null)
                return 18f;

            float width = previewRect.rect.width > 1f ? previewRect.rect.width : 120f;
            float height = previewRect.rect.height > 1f ? previewRect.rect.height : width;
            // Khối preview to hơn (chia nhỏ hơn → cell lớn hơn; khối 4 ô ~87% vùng).
            return Mathf.Max(0.5f, Mathf.Min(width, height) / 4.6f);
        }

        float PreviewCellStep(float cellSize)
        {
            return cellSize;
        }

        void RefreshScenePuzzleCellSizes()
        {
            if (scenePuzzleCells == null || scenePuzzleSlots == null)
                return;

            FitScenePuzzleGridToAnchor();
            if (scenePuzzleGridRect != null && scenePuzzleGridRect.sizeDelta.x > 0.01f && scenePuzzleGridRect.sizeDelta.y > 0.01f)
            {
                puzzleCellSizeX = Mathf.Max(0.0001f, scenePuzzleGridRect.sizeDelta.x / Width);
                puzzleCellSize  = Mathf.Max(0.0001f, scenePuzzleGridRect.sizeDelta.y / Height);
            }

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    var slot = scenePuzzleSlots[x, y];
                    var cell = scenePuzzleCells[x, y];
                    if (slot == null || cell == null)
                        continue;

                    var rect = cell.rectTransform;
                    rect.anchorMin = new Vector2(0.5f, 0.5f);
                    rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = ScenePuzzleGridToUiPosition(x, y);
                    rect.sizeDelta = new Vector2(puzzleCellSizeX * 0.96f, puzzleCellSize * 0.96f);
                    rect.localScale = Vector3.one;
                    cell.preserveAspect = false;
                }
            }
        }

        Vector2 ScenePuzzleGridToUiPosition(int gridX, int gridY)
        {
            if (scenePuzzleGridRect == null)
                return Vector2.zero;

            float originX = -scenePuzzleGridRect.rect.width  * 0.5f + puzzleCellSizeX * 0.5f;
            float originY = -scenePuzzleGridRect.rect.height * 0.5f + puzzleCellSize  * 0.5f;
            return new Vector2(originX + gridX * puzzleCellSizeX, originY + gridY * puzzleCellSize);
        }

        void FitScenePuzzleGridToAnchor()
        {
            if (scenePuzzleBoardAnchorRect == null || scenePuzzleGridRect == null)
                return;

            Rect anchorRect = scenePuzzleBoardAnchorRect.rect;
            if (anchorRect.width <= 1f || anchorRect.height <= 1f)
                return;

            // PuzzleBoardAnchor has non-uniform localScale (e.g. x=6.16, y=9.55) due to CanvasScaler.
            // Work in screen pixels to get visually uniform results, then convert back to local units.
            Vector3 localSc = scenePuzzleBoardAnchorRect.localScale;
            float lsx = Mathf.Max(0.0001f, Mathf.Abs(localSc.x));
            float lsy = Mathf.Max(0.0001f, Mathf.Abs(localSc.y));

            // Border: use separate X/Y border fractions to match the frame sprite visually.
            float anchorScreenW = anchorRect.width  * lsx;
            float anchorScreenH = anchorRect.height * lsy;
            float borderFrac = 0.08f; // ~8% matches the frame sprite border thickness
            float borderPx   = Mathf.Min(anchorScreenW * borderFrac, anchorScreenH * borderFrac);

            float availWpx = anchorScreenW - borderPx * 2f;
            float availHpx = anchorScreenH - borderPx * 2f;

            float cellPxByW = availWpx / Width;
            float cellPxByH = availHpx / Height;

            float localCellW, localCellH;
            if (cellPxByH >= cellPxByW)
            {
                // Anchor has room: fill width, cells square (height = width in px).
                localCellW = Mathf.Max(0.0001f, cellPxByW / lsx);
                localCellH = Mathf.Max(0.0001f, Mathf.Min(cellPxByW * 1.55f, cellPxByH) / lsy);
            }
            else
            {
                // Anchor too short to keep cells square at full width (mobile 10×20 case).
                // Fill width, let cells be slightly wider than tall — better than side gaps.
                localCellW = Mathf.Max(0.0001f, cellPxByW / lsx);
                localCellH = Mathf.Max(0.0001f, cellPxByH / lsy); // clamp height so grid fits
            }
            float gridWidth  = localCellW * Width;
            float gridHeight = localCellH * Height;
            puzzleCellSizeX  = localCellW;
            puzzleCellSize   = localCellH;

            float borderLocalY = borderPx / lsy;
            // Align grid bottom to inner border edge, but clamp so grid never exits the anchor.
            float idealBottom = anchorRect.yMin + borderLocalY;
            float idealCenter = idealBottom + gridHeight * 0.5f;
            float maxCenter   = anchorRect.yMax - borderLocalY - gridHeight * 0.5f;
            float gridCenterY = Mathf.Min(idealCenter, maxCenter);

            scenePuzzleGridRect.anchorMin        = new Vector2(0.5f, 0.5f);
            scenePuzzleGridRect.anchorMax        = new Vector2(0.5f, 0.5f);
            scenePuzzleGridRect.pivot            = new Vector2(0.5f, 0.5f);
            scenePuzzleGridRect.sizeDelta        = new Vector2(gridWidth, gridHeight);
            scenePuzzleGridRect.anchoredPosition = new Vector2(0f, gridCenterY);
            scenePuzzleGridRect.localScale       = Vector3.one;
            scenePuzzleGridRect.localRotation    = Quaternion.identity;
        }

        void LayoutGameplayChrome()
        {
            if (usingSceneGameplayCanvas)
                return;

            if (nextWidgetRect == null || nextWidgetShadowRect == null)
                return;

            float aspect = Screen.height > 0 ? Mathf.Max(0.35f, (float)Screen.width / Screen.height) : 0.56f;
            if (aspect >= 0.8f)
            {
                if (hudPanelRect != null)
                    ApplyAnchoredRect(hudPanelRect, new Vector2(0.035f, 0.895f), new Vector2(0.965f, 0.975f), Vector2.zero);
                if (hudShadowRect != null)
                    ApplyAnchoredRect(hudShadowRect, new Vector2(0.035f, 0.887f), new Vector2(0.965f, 0.967f), new Vector2(0, -6));
                if (pauseButtonRect != null)
                    ApplyAnchoredRect(pauseButtonRect, new Vector2(0.885f, 0.902f), new Vector2(0.955f, 0.968f), Vector2.zero);

                ApplyAnchoredRect(nextWidgetRect, new Vector2(0.805f, 0.315f), new Vector2(0.970f, 0.485f), Vector2.zero);
                ApplyAnchoredRect(nextWidgetShadowRect, new Vector2(0.805f, 0.308f), new Vector2(0.970f, 0.478f), new Vector2(0, -5));
                if (holdWidgetRect != null)
                    holdWidgetRect.gameObject.SetActive(false);
                if (holdWidgetShadowRect != null)
                    holdWidgetShadowRect.gameObject.SetActive(false);
                if (rotateButtonRect != null)
                    ApplyAnchoredRect(rotateButtonRect, new Vector2(0.835f, 0.220f), new Vector2(0.955f, 0.300f), Vector2.zero);
                if (moveHintPanelRect != null)
                    ApplyAnchoredRect(moveHintPanelRect, new Vector2(0.805f, 0.065f), new Vector2(0.970f, 0.200f), Vector2.zero);
                if (moveHintPanelShadowRect != null)
                    ApplyAnchoredRect(moveHintPanelShadowRect, new Vector2(0.805f, 0.058f), new Vector2(0.970f, 0.193f), new Vector2(0, -5));
                if (tacticalWidgetRect != null)
                    ApplyAnchoredRect(tacticalWidgetRect, new Vector2(0.055f, 0.520f), new Vector2(0.945f, 0.875f), Vector2.zero);
                if (tacticalWidgetShadowRect != null)
                    ApplyAnchoredRect(tacticalWidgetShadowRect, new Vector2(0.055f, 0.512f), new Vector2(0.945f, 0.867f), new Vector2(0, -5));
                return;
            }

            GetPortraitGameplayLayout(aspect, out _, out _, out _, out float boardTop, out float sideMin, out float sideMax);
            float gap = aspect < 0.5f ? 0.022f : 0.028f;
            float widgetHeight = Mathf.Clamp(0.135f + (0.56f - Mathf.Min(aspect, 0.56f)) * 0.09f, 0.130f, 0.155f);
            float nextTop = boardTop - 0.002f;
            float nextBottom = nextTop - widgetHeight;
            float rotateBottom = 0.145f;
            float hudBottom = 0.915f;
            float hudTop = 0.982f;
            float hudShadowBottom = hudBottom - 0.008f;
            float hudShadowTop = hudTop - 0.008f;
            float pauseBottom = hudBottom;
            float pauseTop = hudTop;

            if (hudPanelRect != null)
                ApplyAnchoredRect(hudPanelRect, new Vector2(0.030f, hudBottom), new Vector2(0.965f, hudTop), Vector2.zero);
            if (hudShadowRect != null)
                ApplyAnchoredRect(hudShadowRect, new Vector2(0.030f, hudShadowBottom), new Vector2(0.965f, hudShadowTop), new Vector2(0, -6));
            if (pauseButtonRect != null)
                ApplyAnchoredRect(pauseButtonRect, new Vector2(sideMax - 0.090f, pauseBottom + 0.004f), new Vector2(sideMax - 0.010f, pauseTop - 0.004f), Vector2.zero);

            ApplyAnchoredRect(nextWidgetRect, new Vector2(sideMin, nextBottom), new Vector2(sideMax, nextTop), Vector2.zero);
            ApplyAnchoredRect(nextWidgetShadowRect, new Vector2(sideMin, nextBottom - 0.007f), new Vector2(sideMax, nextTop - 0.007f), new Vector2(0, -5));

            if (holdWidgetRect != null)
                holdWidgetRect.gameObject.SetActive(false);
            if (holdWidgetShadowRect != null)
                holdWidgetShadowRect.gameObject.SetActive(false);

            if (rotateButtonRect != null)
            {
                float rotateHeight = Mathf.Clamp(widgetHeight * 0.48f, 0.095f, 0.112f);
                float rotateTop = nextBottom - gap * 0.70f;
                rotateBottom = Mathf.Max(0.055f, rotateTop - rotateHeight);
                float rotateInset = Mathf.Clamp((sideMax - sideMin) * 0.18f, 0.020f, 0.034f);
                ApplyAnchoredRect(rotateButtonRect, new Vector2(sideMin + rotateInset, rotateBottom), new Vector2(sideMax - rotateInset, rotateTop), Vector2.zero);
            }

            float hintTop = Mathf.Clamp(rotateBottom - gap * 0.55f, 0.125f, 0.175f);
            float hintBottom = Mathf.Max(0.030f, hintTop - 0.115f);
            if (moveHintPanelRect != null)
                ApplyAnchoredRect(moveHintPanelRect, new Vector2(sideMin, hintBottom), new Vector2(sideMax, hintTop), Vector2.zero);
            if (moveHintPanelShadowRect != null)
                ApplyAnchoredRect(moveHintPanelShadowRect, new Vector2(sideMin, hintBottom - 0.007f), new Vector2(sideMax, hintTop - 0.007f), new Vector2(0, -5));

            float tacticalWidth = Mathf.Lerp(0.86f, 0.90f, Mathf.InverseLerp(0.62f, 0.42f, aspect));
            float tacticalHeight = Mathf.Clamp(tacticalWidth * aspect, 0.370f, 0.485f);
            float tacticalTop = 0.895f;
            float tacticalBottom = tacticalTop - tacticalHeight;
            float tacticalLeft = 0.5f - tacticalWidth * 0.5f;
            float tacticalRight = 0.5f + tacticalWidth * 0.5f;
            if (tacticalWidgetRect != null)
                ApplyAnchoredRect(tacticalWidgetRect, new Vector2(tacticalLeft, tacticalBottom), new Vector2(tacticalRight, tacticalTop), Vector2.zero);
            if (tacticalWidgetShadowRect != null)
                ApplyAnchoredRect(tacticalWidgetShadowRect, new Vector2(tacticalLeft, tacticalBottom - 0.008f), new Vector2(tacticalRight, tacticalTop - 0.008f), new Vector2(0, -5));
        }

        void GetPortraitGameplayLayout(float aspect, out float boardLeft, out float boardRight, out float boardBottom, out float boardTop, out float sideMin, out float sideMax)
        {
            if (usingSceneGameplayCanvas && TryGetNormalizedScreenRect(scenePuzzleBoardAnchorRect, out boardLeft, out boardRight, out boardBottom, out boardTop))
            {
                float narrowFromAnchor = Mathf.InverseLerp(0.62f, 0.42f, aspect);
                sideMin = Mathf.Clamp(boardRight + Mathf.Lerp(0.035f, 0.025f, narrowFromAnchor), 0.70f, 0.88f);
                sideMax = Mathf.Lerp(0.960f, 0.980f, narrowFromAnchor);
                return;
            }

            float narrow = Mathf.InverseLerp(0.62f, 0.42f, aspect);
            sideMax = Mathf.Lerp(0.965f, 0.980f, narrow);
            sideMin = Mathf.Lerp(0.760f, 0.785f, narrow);
            boardLeft = Mathf.Lerp(0.045f, 0.035f, narrow);
            boardRight = sideMin - Mathf.Lerp(0.040f, 0.030f, narrow);
            boardBottom = Mathf.Lerp(0.035f, 0.045f, narrow);
            boardTop = Mathf.Lerp(0.400f, 0.385f, narrow);
        }

        bool TryGetNormalizedScreenRect(RectTransform rect, out float left, out float right, out float bottom, out float top)
        {
            left = right = bottom = top = 0f;
            if (rect == null || Screen.width <= 0 || Screen.height <= 0)
                return false;

            var canvas = rect.GetComponentInParent<Canvas>();
            Camera uiCamera = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                uiCamera = canvas.worldCamera;

            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            for (int i = 0; i < corners.Length; i++)
            {
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(uiCamera, corners[i]);
                minX = Mathf.Min(minX, screen.x);
                maxX = Mathf.Max(maxX, screen.x);
                minY = Mathf.Min(minY, screen.y);
                maxY = Mathf.Max(maxY, screen.y);
            }

            if (maxX - minX < 20f || maxY - minY < 20f)
                return false;

            left = Mathf.Clamp01(minX / Screen.width);
            right = Mathf.Clamp01(maxX / Screen.width);
            bottom = Mathf.Clamp01(minY / Screen.height);
            top = Mathf.Clamp01(maxY / Screen.height);
            return right > left && top > bottom;
        }

        void ApplyAnchoredRect(RectTransform rect, Vector2 min, Vector2 max, Vector2 offset)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = offset;
            rect.sizeDelta = Vector2.zero;
        }

        List<Image> CreatePiecePreview(Transform parent, Vector2 center, float cellSize)
        {
            var cells = new List<Image>();
            for (int i = 0; i < 16; i++)
            {
                int x = i % 4;
                int y = i / 4;
                var cell = Ui.Panel(parent, "Preview Cell", new Color(1, 1, 1, 0)).GetComponent<Image>();
                cell.sprite = blockSprite;
                cell.type = Image.Type.Simple;
                cell.preserveAspect = false;
                Ui.Rect(cell.gameObject, center, center, new Vector2(cellSize, cellSize));
                float step = PreviewCellStep(cellSize);
                cell.rectTransform.anchoredPosition = new Vector2((x - 1.5f) * step, (1.5f - y) * step);
                cells.Add(cell);
            }
            return cells;
        }

        void HandleInput()
        {
            HandleGestureInput();

            if (KeyPressed(KeyCode.LeftArrow, Key.LeftArrow) || KeyPressed(KeyCode.A, Key.A)) TryMove(Vector2Int.left);
            if (KeyPressed(KeyCode.RightArrow, Key.RightArrow) || KeyPressed(KeyCode.D, Key.D)) TryMove(Vector2Int.right);
            if (KeyPressed(KeyCode.DownArrow, Key.DownArrow) || KeyPressed(KeyCode.S, Key.S)) SoftDrop();
            if (KeyPressed(KeyCode.UpArrow, Key.UpArrow) || KeyPressed(KeyCode.W, Key.W)) TryRotate(1);
            if (KeyPressed(KeyCode.Z, Key.Z)) TryRotate(-1);
            if (KeyPressed(KeyCode.Space, Key.Space)) HardDrop();
            if (KeyPressed(KeyCode.C, Key.C) || KeyPressed(KeyCode.LeftShift, Key.LeftShift)) Hold();
            if (KeyPressed(KeyCode.R, Key.R)) Restart();
        }

        void HandleGestureInput()
        {
            bool pressed;
            bool released;
            Vector2 position;
            if (!ReadPointer(out pressed, out released, out position))
                return;

            if (pressed && PointerOverInteractiveUi(position))
                return;

            if (pressed)
            {
                gestureStart = position;
                gestureLastPosition = position;
                gestureStartTime = Time.unscaledTime;
                gestureTracking = true;
                gestureMoved = false;
                gestureMovedHorizontally = false;
                return;
            }

            if (!gestureTracking)
                return;

            float dragStep = Mathf.Min(Screen.width, Screen.height) * 0.055f;
            Vector2 dragDelta = position - gestureLastPosition;

            while (Mathf.Abs(dragDelta.x) >= dragStep && Mathf.Abs(dragDelta.x) > Mathf.Abs(dragDelta.y) * 0.55f)
            {
                int direction = dragDelta.x < 0 ? -1 : 1;
                TryMove(direction < 0 ? Vector2Int.left : Vector2Int.right);
                gestureLastPosition.x += dragStep * direction;
                dragDelta = position - gestureLastPosition;
                gestureMoved = true;
                gestureMovedHorizontally = true;
            }

            if (!released)
                return;

            gestureTracking = false;
            Vector2 delta = position - gestureStart;
            float distance = delta.magnitude;
            float elapsed = Mathf.Max(0.01f, Time.unscaledTime - gestureStartTime);
            float swipeThreshold = Mathf.Min(Screen.width, Screen.height) * 0.08f;

            if (!gestureMoved && distance < swipeThreshold && elapsed < 0.35f)
            {
                TryRotate(1);
                return;
            }

            // Allow hard drop even after horizontal drags, as long as the net gesture is clearly downward.
            if (delta.y < -swipeThreshold && Mathf.Abs(delta.y) > Mathf.Abs(delta.x) * 1.5f)
            {
                HardDrop();
            }
        }

        bool PointerOverInteractiveUi(Vector2 position)
        {
            if (EventSystem.current == null)
                return false;

            var eventData = new PointerEventData(EventSystem.current) { position = position };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, hits);
            for (int i = 0; i < hits.Count; i++)
            {
                var button = hits[i].gameObject.GetComponentInParent<Button>();
                if (button != null && button.isActiveAndEnabled && button.interactable)
                    return true;
            }
            return false;
        }

        bool ReadPointer(out bool pressed, out bool released, out Vector2 position)
        {
            pressed = false;
            released = false;
            position = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null)
            {
                var touch = Touchscreen.current.primaryTouch;
                if (touch.press.isPressed || touch.press.wasPressedThisFrame || touch.press.wasReleasedThisFrame)
                {
                    position = touch.position.ReadValue();
                    pressed = touch.press.wasPressedThisFrame;
                    released = touch.press.wasReleasedThisFrame;
                    return true;
                }
            }

            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.isPressed || Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.leftButton.wasReleasedThisFrame)
                {
                    position = Mouse.current.position.ReadValue();
                    pressed = Mouse.current.leftButton.wasPressedThisFrame;
                    released = Mouse.current.leftButton.wasReleasedThisFrame;
                    return true;
                }
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                position = touch.position;
                pressed = touch.phase == TouchPhase.Began;
                released = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
                return true;
            }

            position = Input.mousePosition;
            pressed = Input.GetMouseButtonDown(0);
            released = Input.GetMouseButtonUp(0);
            return Input.GetMouseButton(0) || pressed || released;
#else
            return false;
#endif
        }

        bool KeyPressed(KeyCode legacyKey, Key inputSystemKey)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current[inputSystemKey].wasPressedThisFrame)
                return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(legacyKey);
#else
            return false;
#endif
        }

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

        Vector2Int SpawnOriginForCurrentPiece()
        {
            if (currentPieceIsSpecial)
                return new Vector2Int(Width / 2, Height - 1);

            int maxLocalY = 0;
            foreach (var cell in shapes[currentType])
                maxLocalY = Mathf.Max(maxLocalY, cell.y);
            return new Vector2Int(Width / 2, Height - 1 - maxLocalY);
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

        List<int> FindFullRows()
        {
            var rows = new List<int>();
            for (int y = 0; y < Height; y++)
            {
                bool full = true;
                for (int x = 0; x < Width; x++)
                    full &= grid[x, y] > 0;
                if (full)
                    rows.Add(y);
            }
            return rows;
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

        void AwardTacticalMoves(int clearedLines, bool comboBonus)
        {
            if (tacticalBoard == null || tacticalBoard.Status != TacticalBoardStatus.Running || clearedLines <= 0)
                return;

            tacticalBoard.AddMovesForClearedLines(clearedLines, comboBonus);
            RefreshTacticalBoardUi();
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

        IEnumerable<Vector2Int> Cells(Vector2Int testOrigin, int testRotation)
        {
            if (currentPieceIsSpecial)
            {
                yield return testOrigin;
                yield break;
            }

            foreach (var cell in shapes[currentType])
                yield return CellFromLocal(cell, testOrigin, testRotation);
        }

        Vector2Int CellFromLocal(Vector2Int cell, Vector2Int testOrigin, int testRotation)
        {
            var rotated = cell;
            if (currentType != 3)
            {
                for (int i = 0; i < testRotation; i++)
                    rotated = new Vector2Int(rotated.y, -rotated.x);
            }
            return testOrigin + rotated;
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

        void UpdateUi()
        {
            if (score > bestScore)
            {
                bestScore = score;
                PlayerPrefs.SetInt(BestScoreKey(), bestScore);
                PlayerPrefs.Save();
            }

            scoreText.text = "";
            linesText.text = MultiplayerMatch.Active ? "1 vs 1" : "Màn: " + journeyLevel;
            levelText.text = tacticalBoard != null ? "Lượt đi: " + tacticalBoard.MoveBank : MissionProgressText();
            bestText.text = "";
            int nextType = PeekNext(0);
            nextText.text = "TIẾP";
            RenderPiecePreview(nextPreviewCells, nextType, true);
            RenderPiecePreview(holdPreviewCells, holdType, holdType >= 0);
            if (MultiplayerMatch.Active)
            {
                if (opponentText != null)
                {
                    string oppName = string.IsNullOrEmpty(MultiplayerMatch.OpponentName) ? "Đối thủ" : MultiplayerMatch.OpponentName;
                    opponentText.text = oppName + " · Máu " + MultiplayerMatch.OpponentHealth + "/" + OnlineConfig.MaxHealth
                        + " · NL " + MultiplayerMatch.OpponentEnergy;
                }
                if (MultiplayerManager.Instance != null)
                    MultiplayerManager.Instance.SendState(score, lines, 0, healthSystem.Health, energySystem.Energy);
            }
            RefreshTacticalBoardUi();
            RefreshSceneHud();
            RefreshScenePuzzleBoardUi();
        }

        string MissionProgressText()
        {
            if (tacticalBoard == null)
                return "";
            return "Mục tiêu: dụ quái bắt đối thủ  |  Lượt " + tacticalBoard.MoveBank + "  Đã đi " + tacticalBoard.MovesUsed;
        }

        bool IsMissionComplete()
        {
            return tacticalBoard != null && tacticalBoard.Status == TacticalBoardStatus.Won;
        }

        string BestScoreKey()
        {
            return LevelProgress.BestScoreKeyForLevel(journeyLevel);
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

        void BeginLevelMission(bool showPopup)
        {
            rules = LevelRules.CreateJourney(journeyLevel);
            SetupTacticalBoard();
            GameSession.JourneyLevel = journeyLevel;
            levelLines = 0;
            levelStartScore = score;
            maxComboThisLevel = 0;
            combo = 0;
            rotationsThisLevel = 0;
            holdsThisLevel = 0;
            maxLinesClearedAtOnce = 0;
            lastRisingDangerTick = 0;
            starsEarned = 0;
            gameplayTime = 0f;
            fallTimer = 0f;
            piecesLocked = 0;
            RefreshTacticalBoardUi();

            // 1v1: khởi tạo máu + năng lượng cho trận mới (design §6-8).
            if (MultiplayerMatch.Active)
            {
                energySystem.Reset();
                healthSystem.Reset();
                nextAttackTime = 0f;
                RefreshSkillBar();
            }

            ApplyLevelStartEffects();
            UpdateUi();

            if (showPopup && missionOverlay != null)
            {
                paused = true;
                Time.timeScale = 0f;
                missionTitleText.text = "MÀN " + journeyLevel;
                if (missionDescText != null) missionDescText.text = "Sẵn sàng chưa?";
                UpdateMissionStarRows();
                missionOverlay.SetActive(true);
            }
            else if (MultiplayerMatch.Active)
            {
                StartCoroutine(MultiplayerCountdownRoutine());
            }
        }

        // Đếm ngược 3-2-1 trước trận 1v1 — hai bên nhận START gần như cùng lúc nên
        // cùng vào trận một nhịp, không ai bị khối rơi bất ngờ.
        System.Collections.IEnumerator MultiplayerCountdownRoutine()
        {
            paused = true;
            Time.timeScale = 0f;

            Transform overlayParent = missionOverlay != null
                ? missionOverlay.transform.parent
                : (safeAreaRoot != null ? safeAreaRoot.transform : transform);
            var overlay = Ui.Panel(overlayParent, "Countdown Overlay", new Color(0f, 0f, 0f, 0.60f));
            Ui.Stretch(overlay);

            var label = Ui.Text(overlay.transform, "3", font, 220, new Color(1f, 0.84f, 0.40f), TextAnchor.MiddleCenter);
            label.fontStyle = FontStyle.Bold;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            Ui.Stretch(label.gameObject);
            AddDarkWoodTextEdge(label, 1.2f, 0.9f);

            // Ẩn 1 frame đầu — CanvasScaler chưa layout xong, chữ sẽ chớp sai vị trí.
            overlay.SetActive(false);
            yield return null;
            overlay.SetActive(true);

            for (int i = 3; i >= 1; i--)
            {
                label.text = i.ToString();
                RuntimeArt.PlayUiSwitchSound();
                yield return new WaitForSecondsRealtime(0.8f);
            }

            label.fontSize = 100;
            label.text = "BẮT ĐẦU!";
            RuntimeArt.PlayUiSwitchSound();
            yield return new WaitForSecondsRealtime(0.6f);

            Destroy(overlay);
            // Chỉ resume nếu không có popup khác chen vào giữa chừng
            // (đối thủ thoát ngay trong lúc đếm → popup kết thúc trận đã mở).
            bool otherPopupOpen = (pauseOverlay != null && pauseOverlay.activeSelf)
                || (gameOverOverlay != null && gameOverOverlay.activeSelf)
                || (levelClearOverlay != null && levelClearOverlay.activeSelf);
            if (!otherPopupOpen)
            {
                paused = false;
                Time.timeScale = 1f;
            }
        }

        void ApplyLevelStartEffects()
        {
            if (rules.HasFixedObstacles && journeyLevel % 5 == 0)
                AddFixedObstaclePattern();
            else if (rules.HasStoneBlocks && journeyLevel % 4 == 0)
                AddGarbageRow();
        }

        void AddFixedObstaclePattern()
        {
            int baseY = Mathf.Clamp(1 + (journeyLevel / 5) % 4, 1, 5);
            int startX = UnityEngine.Random.Range(1, Width - 3);
            for (int i = 0; i < 3; i++)
            {
                int x = startX + i;
                int y = baseY + (i == 1 ? 1 : 0);
                if (grid[x, y] == 0)
                    grid[x, y] = 7;
            }
            RedrawLocked();
        }

        void LevelComplete()
        {
            if (MultiplayerMatch.Active)
            {
                // Trận 1v1: không có sao — thưởng thắng cố định, không lưu tiến trình solo.
                const int mpBonus = 500;
                score += mpBonus; // gửi cho đối thủ cùng cờ kết thúc trong MultiplayerEndMatch
                MultiplayerEndMatch(true, "Bạn hoàn thành bàn cờ trước đối thủ!\nĐiểm thưởng +" + mpBonus);
                return;
            }

            resolving = true;
            ClearActive();
            starsEarned = CalculateStars();
            // Điểm thưởng thắng bàn cờ — cộng chung vào điểm xóa hàng; bàn cờ chỉ
            // sinh điểm khi thắng, di chuyển không cho điểm.
            int winBonus = WinScoreBonus(starsEarned);
            score += winBonus;
            LevelProgress.SaveLevelResult(journeyLevel, starsEarned);
            LevelProgress.SaveLevelBestScore(journeyLevel, score);
            LevelProgress.AddCoins(rules.CoinReward);
            PlayerPrefs.Save();
            CloudSaveSync.Push();
            LeaderboardsSync.SubmitScore(score);

            // Sao đạt sáng, sao chưa đạt mờ.
            SetLevelClearStars(starsEarned);

            // Phụ đề vui — chọn ngẫu nhiên theo số sao.
            levelClearBodyText.text = RandomVictorySubtitle(starsEarned);
            levelClearOverlay.SetActive(true);

            // TRANG CHỦ → bản đồ màn.
            continueButton.interactable = true;
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(() =>
            {
                RuntimeArt.PlayUiSwitchSound();
                Time.timeScale = 1f;
                SceneManager.LoadScene("BrickLevel");
            });

            // CHƠI LẠI → chơi lại màn hiện tại.
            stopButton.interactable = true;
            stopButton.onClick.RemoveAllListeners();
            stopButton.onClick.AddListener(() =>
            {
                RuntimeArt.PlayUiSwitchSound();
                Restart();
            });

            // TIẾP → màn kế; nếu đã là màn cuối thì về bản đồ.
            if (nextButton != null)
            {
                bool hasNext = journeyLevel < LevelProgress.MaxLevels;
                nextButton.gameObject.SetActive(true);
                nextButton.interactable = true;
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(() =>
                {
                    RuntimeArt.PlayUiSwitchSound();
                    Time.timeScale = 1f;
                    if (hasNext)
                    {
                        int nl = journeyLevel + 1;
                        GameSession.SelectedLevel = nl;
                        GameSession.JourneyLevel = nl;
                        SceneManager.LoadScene("BrickGame");
                    }
                    else SceneManager.LoadScene("BrickLevel");
                });
            }
            Beep(1180f, 0.22f, 0.35f);
        }

        // Câu phụ đề vui theo số sao (chọn ngẫu nhiên mỗi lần thắng).
        static readonly string[] Subtitle3Star =
        {
            "Eo ôi giỏi thíiiiii",
            "Đỉnh nóc, kịch trần!",
            "Bạn là nhất, nhất bạn rồi!",
            "Không phải dạng vừa đâu, vừa vừa vừa đâuu!",
            "Tuyệt đối điện ảnh!",
        };
        static readonly string[] Subtitle2Star =
        {
            "Thế mà lại hay",
            "Thấy là cũng có nghề đó!",
            "Gần chạm nóc rồi đó!",
            "Chưa hoàn hảo, nhưng đủ gây thương nhớ!",
            "Được á, chơi lại phát nữa là đẹp!",
        };
        static readonly string[] Subtitle1Star =
        {
            "Tui đau đớn, tui gục ngã",
            "Còn thở là còn gỡ!",
            "Qua là được, đừng hỏi cách qua!",
            "Phong độ là nhất thời, chơi lại là mãi mãi!",
            "Thôi thì thôi thì thôi đành thôi!",
        };

        static string RandomVictorySubtitle(int stars)
        {
            var pool = stars >= 3 ? Subtitle3Star : stars == 2 ? Subtitle2Star : Subtitle1Star;
            return pool[UnityEngine.Random.Range(0, pool.Length)];
        }

        // Đặt trạng thái 3 sao trên màn thắng: đủ sao thì sáng, thiếu thì mờ.
        void SetLevelClearStars(int stars)
        {
            if (levelClearStarImgs == null) return;
            var active = RuntimeArt.LoadV3SubSprite("screen-chienthang/star-active.png", new Rect(0.203f, 0.355f, 0.582f, 0.383f));
            var inactive = RuntimeArt.LoadV3SubSprite("screen-chienthang/star-inactive.png", new Rect(0.189f, 0.345f, 0.621f, 0.411f));
            for (int i = 0; i < levelClearStarImgs.Length; i++)
            {
                if (levelClearStarImgs[i] == null) continue;
                levelClearStarImgs[i].sprite = i < stars ? active : inactive;
            }
        }

        // Điểm thưởng thắng bàn cờ: 300 gốc + 200/sao (3 sao = 900).
        static int WinScoreBonus(int stars)
        {
            return 300 + Mathf.Clamp(stars, 0, 3) * 200;
        }

        // Hàng sao bằng rich text: sao đạt màu vàng, sao chưa đạt màu nâu mờ.
        // Dùng ★ cho cả hai (font UI không chắc có ☆).

        int CalculateStars()
        {
            // Design §5: sao theo thời gian; 3 sao còn cần dùng ít lượt (ít kỹ năng hỗ trợ).
            if (tacticalBoard != null && rules.TacticalData != null)
            {
                var d = rules.TacticalData;
                if (gameplayTime <= d.ThreeStarTime && tacticalBoard.MovesUsed <= d.ThreeStarMoveLimit)
                    return 3;
                if (gameplayTime <= d.TwoStarTime)
                    return 2;
            }
            return 1;
        }

        static string FormatSeconds(float seconds)
        {
            int total = Mathf.Max(0, Mathf.RoundToInt(seconds));
            return (total / 60) + ":" + (total % 60).ToString("00");
        }

        void UpdateMissionStarRows()
        {
            if (missionStar1CondText == null) return;
            missionStar1CondText.text = "Hoàn thành";
            if (rules != null && rules.TacticalData != null)
            {
                // Design §5: hiển thị mốc thời gian; 3 sao còn giới hạn số lượt.
                missionStar2CondText.text = "≤ " + FormatSeconds(rules.TacticalData.TwoStarTime);
                missionStar3CondText.text = "≤ " + FormatSeconds(rules.TacticalData.ThreeStarTime) + " · ≤ " + rules.TacticalData.ThreeStarMoveLimit + " lượt";
                return;
            }
            missionStar2CondText.text = "—";
            missionStar3CondText.text = "—";
        }

        void EndGame(bool won)
        {
            if (MultiplayerMatch.Active && !won)
            {
                MultiplayerEndMatch(false, GameOverMessage());
                return;
            }

            gameOver = true;
            resolving = false;
            StopBackgroundMusic();
            ClearActive();
            statusText.text = "";

            // Thua (không phải 1v1): màn hình THẤT BẠI mới.
            if (!won && gameLoseOverlay != null)
            {
                if (gameLoseSubtitle != null) gameLoseSubtitle.text = RandomVictorySubtitle(1);
                gameLoseOverlay.transform.SetAsLastSibling();
                gameLoseOverlay.SetActive(true);
                shake = 0.2f;
                RuntimeArt.PlayGameOverSound();
                return;
            }

            gameOverTitleText.text = won ? "HOÀN THÀNH" : "THUA RỒI";
            gameOverTitleText.color = won ? new Color(1f, 0.86f, 0.56f) : new Color(1f, 0.62f, 0.36f);
            gameOverScoreText.text = won ? "Điểm  " + score + "\nHàng  " + lines : GameOverMessage();
            gameOverOverlay.SetActive(true);
            shake = won ? 0.35f : 0.2f;
            if (won)
                Beep(1180f, 0.22f, 0.35f);
            else
                RuntimeArt.PlayGameOverSound();
        }

        // Bàn cờ + bàn xếp gạch thu nhỏ của đối thủ, xếp chồng ở cột phải dưới nút Xoay.
        void BuildOpponentMiniBoard(Transform parent)
        {
            // --- Bàn cờ chiến thuật mini (tường vẽ sẵn — cùng level nên giống mình) ---
            var tacticalData = rules != null ? rules.TacticalData : null;
            int tacticalW = tacticalData != null ? Mathf.Max(1, tacticalData.BoardWidth) : 8;
            int tacticalH = tacticalData != null ? Mathf.Max(1, tacticalData.BoardHeight) : 8;

            // Prefix "Runtime " để SwitchGameplayRoot tự dời panel khi đổi root mobile/tablet.
            var tacticalPanel = Ui.Panel(parent, "Runtime Opponent Mini Tactical", new Color(0.14f, 0.06f, 0.022f, 0.90f));
            opponentTacticalPanelRect = tacticalPanel.GetComponent<RectTransform>();
            Ui.Rect(tacticalPanel, new Vector2(0.815f, 0.760f), new Vector2(0.960f, 0.850f), Vector2.zero);
            tacticalPanel.GetComponent<Image>().raycastTarget = false;
            MakeIsolatedCanvas(tacticalPanel, false); // repaint 2Hz không kéo cả canvas chính

            opponentText = Ui.Text(tacticalPanel.transform, "Đối thủ", font, 34, new Color(0.62f, 0.92f, 1f), TextAnchor.MiddleCenter);
            opponentText.fontStyle = FontStyle.Bold;
            opponentText.resizeTextForBestFit = true;
            opponentText.resizeTextMaxSize = 34;
            opponentText.resizeTextMinSize = 18;
            Ui.Rect(opponentText, new Vector2(-0.35f, 1.03f), new Vector2(1.35f, 1.30f), Vector2.zero);
            opponentText.raycastTarget = false;
            AddDarkWoodTextEdge(opponentText, 0.6f, 0.85f);

            opponentTacticalCells = new Image[tacticalW, tacticalH];
            var emptyColor = new Color(0.30f, 0.16f, 0.07f, 0.55f);
            for (int x = 0; x < tacticalW; x++)
            {
                for (int y = 0; y < tacticalH; y++)
                {
                    var cell = Ui.Panel(tacticalPanel.transform, "MiniTacCell", emptyColor).GetComponent<Image>();
                    Ui.Rect(cell,
                        new Vector2((x + 0.08f) / tacticalW, (y + 0.08f) / tacticalH),
                        new Vector2((x + 0.92f) / tacticalW, (y + 0.92f) / tacticalH),
                        Vector2.zero);
                    cell.raycastTarget = false;
                    opponentTacticalCells[x, y] = cell;
                }
            }

            if (tacticalData != null)
            {
                var wallColor = new Color(0.48f, 0.30f, 0.14f, 0.95f);
                foreach (var wall in tacticalData.WallPositions)
                    if (wall.x >= 0 && wall.x < tacticalW && wall.y >= 0 && wall.y < tacticalH)
                        opponentTacticalCells[wall.x, wall.y].color = wallColor;
            }

            // --- Bàn xếp gạch mini ---
            var panel = Ui.Panel(parent, "Runtime Opponent Mini Board", new Color(0.14f, 0.06f, 0.022f, 0.90f));
            opponentMiniPanelRect = panel.GetComponent<RectTransform>();
            // Anchor mặc định cho fallback path; scene layout sẽ đặt lại mỗi lần responsive chạy.
            Ui.Rect(panel, new Vector2(0.815f, 0.520f), new Vector2(0.960f, 0.745f), Vector2.zero);
            panel.GetComponent<Image>().raycastTarget = false;
            MakeIsolatedCanvas(panel, false); // repaint 2Hz không kéo cả canvas chính

            opponentMiniCells = new Image[Width, Height];
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    var cell = Ui.Panel(panel.transform, "MiniCell", Color.white).GetComponent<Image>();
                    Ui.Rect(cell,
                        new Vector2((x + 0.06f) / Width, (y + 0.06f) / Height),
                        new Vector2((x + 0.94f) / Width, (y + 0.94f) / Height),
                        Vector2.zero);
                    cell.raycastTarget = false;
                    cell.enabled = false;
                    opponentMiniCells[x, y] = cell;
                }
            }

            // Thanh 3 kỹ năng 1v1 (design §7): Đánh / Khiên / Rác. Container giữ tên
            // cũ (attackButtonRect) để code layout đặt vị trí không phải đổi.
            var skillBar = Ui.Panel(parent, "Runtime Skill Bar", new Color(0, 0, 0, 0));
            attackButtonRect = skillBar.GetComponent<RectTransform>();
            Ui.Rect(skillBar, new Vector2(0.805f, 0.44f), new Vector2(0.965f, 0.54f), Vector2.zero);

            // Chữ năng lượng/máu gọn phía trên thanh kỹ năng.
            skillInfoText = Ui.Text(skillBar.transform, "", font, 18, new Color(1f, 0.9f, 0.66f), TextAnchor.LowerCenter);
            skillInfoText.raycastTarget = false;
            Ui.Rect(skillInfoText, new Vector2(0f, 1.02f), new Vector2(1f, 1.42f), Vector2.zero);
            AddDarkWoodTextEdge(skillInfoText, 0.5f, 0.7f);

            skillAttackButton = BuildSkillButton(skillBar.transform, "ĐÁNH", 0f, 0.32f, OnlineSkill.Attack);
            skillShieldButton = BuildSkillButton(skillBar.transform, "KHIÊN", 0.34f, 0.66f, OnlineSkill.Shield);
            skillGarbageButton = BuildSkillButton(skillBar.transform, "RÁC", 0.68f, 1f, OnlineSkill.Garbage);
            attackButton = skillGarbageButton; // giữ tham chiếu cũ cho code layout legacy
        }

        Button skillAttackButton, skillShieldButton, skillGarbageButton;
        Text skillInfoText;

        Button BuildSkillButton(Transform parent, string label, float xMin, float xMax, OnlineSkill skill)
        {
            var btn = Ui.Button(parent, "", font, 20, () => TryUseSkill(skill));
            btn.gameObject.name = "Skill " + skill;
            StyleWoodRectButton(btn, 20);
            Ui.Rect(btn.gameObject, new Vector2(xMin + 0.01f, 0f), new Vector2(xMax - 0.01f, 1f), Vector2.zero);
            var txt = Ui.Text(btn.transform, label, font, 20, new Color(1f, 0.92f, 0.72f), TextAnchor.MiddleCenter);
            txt.fontStyle = FontStyle.Bold;
            txt.raycastTarget = false;
            Ui.Rect(txt, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.98f), Vector2.zero);
            AddDarkWoodTextEdge(txt, 0.6f, 0.8f);
            return btn;
        }

        // Cột phải: bàn cờ mini (vuông) chồng trên bàn gạch mini (1:2), cell giữ tỉ lệ vuông.
        void LayoutOpponentMiniBoard(float sideLeft, float sideRight, float areaTop, float areaBottom, float aspect)
        {
            if (opponentMiniPanelRect == null)
                return;

            int tacticalCols = opponentTacticalCells != null ? opponentTacticalCells.GetLength(0) : 8;
            int tacticalRows = opponentTacticalCells != null ? opponentTacticalCells.GetLength(1) : 8;
            float tacticalRatio = aspect * ((float)tacticalRows / tacticalCols); // height = width * ratio
            float puzzleRatio = aspect * ((float)Height / Width);
            const float stackGap = 0.010f;

            float miniTop = areaTop - 0.034f; // chừa chỗ nhãn "Đối thủ"
            float maxH = miniTop - areaBottom - 0.006f;
            // Tổng chiều cao theo bề rộng w: w*tacticalRatio + gap + w*puzzleRatio
            float maxW = (sideRight - sideLeft) * 0.94f;
            float width = Mathf.Min(maxW, (maxH - stackGap) / (tacticalRatio + puzzleRatio));
            float totalH = width * (tacticalRatio + puzzleRatio) + stackGap;
            bool visible = totalH > 0.08f && width > 0.03f;

            opponentMiniPanelRect.gameObject.SetActive(visible);
            if (opponentTacticalPanelRect != null)
                opponentTacticalPanelRect.gameObject.SetActive(visible);
            if (!visible)
                return;

            float center = (sideLeft + sideRight) * 0.5f;
            float tacticalHFrac = width * tacticalRatio;
            if (opponentTacticalPanelRect != null)
                ApplySceneRect(opponentTacticalPanelRect,
                    new Vector2(center - width * 0.5f, miniTop - tacticalHFrac),
                    new Vector2(center + width * 0.5f, miniTop));

            float puzzleTop = miniTop - tacticalHFrac - stackGap;
            ApplySceneRect(opponentMiniPanelRect,
                new Vector2(center - width * 0.5f, puzzleTop - width * puzzleRatio),
                new Vector2(center + width * 0.5f, puzzleTop));
        }

        // Gửi ảnh chụp bàn của mình cho đối thủ, tối đa 2 lần/giây và chỉ khi thay đổi.
        void SendBoardSnapshotIfNeeded()
        {
            var manager = MultiplayerManager.Instance;
            if (manager == null || Time.unscaledTime < nextBoardSendTime)
                return;
            nextBoardSendTime = Time.unscaledTime + 0.5f;

            if (boardSnapshot == null || boardSnapshot.Length != Width * Height)
                boardSnapshot = new byte[Width * Height];

            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    boardSnapshot[x + y * Width] = (byte)Mathf.Clamp(grid[x, y], 0, 255);

            if (!gameOver && !resolving)
            {
                byte activeValue = (byte)(currentType >= 0 ? currentType + 1 : 8);
                foreach (var cell in Cells(origin, rotation))
                    if (cell.x >= 0 && cell.x < Width && cell.y >= 0 && cell.y < Height)
                        boardSnapshot[cell.x + cell.y * Width] = activeValue;
            }

            if (tacticalSnapshot == null)
                tacticalSnapshot = new byte[6];
            if (tacticalBoard != null)
            {
                WriteTacticalPos(0, tacticalBoard.PlayerPosition);
                WriteTacticalPos(2, tacticalBoard.EnemyPosition);
                WriteTacticalPos(4, tacticalBoard.MonsterPosition);
            }
            else
            {
                for (int i = 0; i < 6; i++)
                    tacticalSnapshot[i] = 255;
            }

            int hash = 17;
            for (int i = 0; i < boardSnapshot.Length; i++)
                hash = hash * 31 + boardSnapshot[i];
            for (int i = 0; i < tacticalSnapshot.Length; i++)
                hash = hash * 31 + tacticalSnapshot[i];
            if (hash == lastBoardHash)
                return;
            lastBoardHash = hash;

            manager.SendBoard(boardSnapshot, (byte)Width, (byte)Height, tacticalSnapshot);
        }

        void WriteTacticalPos(int index, Vector2Int pos)
        {
            bool valid = pos.x >= 0 && pos.x < 255 && pos.y >= 0 && pos.y < 255;
            tacticalSnapshot[index] = valid ? (byte)pos.x : (byte)255;
            tacticalSnapshot[index + 1] = valid ? (byte)pos.y : (byte)255;
        }

        void RepaintOpponentMiniBoard()
        {
            MultiplayerMatch.OpponentBoardDirty = false;
            var board = MultiplayerMatch.OpponentBoard;
            if (opponentMiniCells != null && board != null)
            {
                int cols = MultiplayerMatch.OpponentBoardCols;
                int rows = MultiplayerMatch.OpponentBoardRows;
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        byte value = x < cols && y < rows ? board[x + y * cols] : (byte)0;
                        var cell = opponentMiniCells[x, y];
                        cell.enabled = value > 0;
                        if (value > 0)
                            cell.color = value - 1 < palette.Length ? palette[value - 1] : new Color(0.9f, 0.9f, 0.9f);
                    }
                }
            }

            RepaintOpponentTacticalBoard();
        }

        void RepaintOpponentTacticalBoard()
        {
            if (opponentTacticalCells == null)
                return;

            int cols = opponentTacticalCells.GetLength(0);
            int rows = opponentTacticalCells.GetLength(1);
            var emptyColor = new Color(0.30f, 0.16f, 0.07f, 0.55f);
            var wallColor = new Color(0.48f, 0.30f, 0.14f, 0.95f);

            // Vẽ lại nền + tường (tường lấy từ level của mình — hai bên giống nhau).
            for (int x = 0; x < cols; x++)
                for (int y = 0; y < rows; y++)
                    opponentTacticalCells[x, y].color = emptyColor;

            if (rules != null && rules.TacticalData != null)
                foreach (var wall in rules.TacticalData.WallPositions)
                    if (wall.x >= 0 && wall.x < cols && wall.y >= 0 && wall.y < rows)
                        opponentTacticalCells[wall.x, wall.y].color = wallColor;

            PaintTacticalEntity(MultiplayerMatch.OpponentTacticalMonster, new Color(0.75f, 0.35f, 0.90f)); // quái tím
            PaintTacticalEntity(MultiplayerMatch.OpponentTacticalEnemy, new Color(0.95f, 0.30f, 0.25f));   // địch đỏ
            PaintTacticalEntity(MultiplayerMatch.OpponentTacticalPlayer, new Color(0.30f, 0.85f, 0.40f));  // người chơi xanh
        }

        void PaintTacticalEntity(Vector2Int pos, Color color)
        {
            if (opponentTacticalCells == null || pos.x < 0 || pos.y < 0)
                return;
            if (pos.x >= opponentTacticalCells.GetLength(0) || pos.y >= opponentTacticalCells.GetLength(1))
                return;
            opponentTacticalCells[pos.x, pos.y].color = color;
        }

        // Áp hàng rác đối thủ gửi sang: nhấp nháy cảnh báo 1 giây rồi mới chèn rác
        // (hàng rác 1 lỗ, chèn đáy). Chờ lúc "yên" để không phá dở animation.
        void ApplyPendingGarbage()
        {
            if (MultiplayerMatch.PendingGarbage <= 0)
            {
                if (garbageWarnUntil > 0f)
                    HideGarbageWarning();
                return;
            }

            if (gameOver || resolving || paused || puzzlePausedForTacticalTurn)
                return;

            if (garbageWarnUntil <= 0f)
            {
                // Bắt đầu pha cảnh báo — chưa chèn rác vội.
                garbageWarnUntil = Time.unscaledTime + 1f;
                ShowGarbageWarning();
                Beep(660f, 0.10f, 0.25f);
                return;
            }

            if (Time.unscaledTime < garbageWarnUntil)
            {
                FlashGarbageWarning();
                return;
            }

            HideGarbageWarning();
            int rows = Mathf.Min(MultiplayerMatch.PendingGarbage, 4);
            MultiplayerMatch.PendingGarbage = 0;
            for (int i = 0; i < rows; i++)
                AddGarbageRow();

            shake = 0.3f;
            RuntimeArt.PlayUiSwitchSound();
            Beep(220f, 0.18f, 0.3f);
            if (tacticalBoard != null)
            {
                string senderName = string.IsNullOrEmpty(MultiplayerMatch.OpponentName) ? "Đối thủ" : MultiplayerMatch.OpponentName;
                tacticalBoard.LastMessage = senderName + " gửi " + rows + " hàng rác cho bạn!";
                RefreshTacticalBoardUi();
            }
        }

        // Người chơi bấm 1 kỹ năng (design §7). Tiêu năng lượng, gửi hiệu ứng sang đối thủ.
        // Giãn 0,4s chống bấm dồn.
        void TryUseSkill(OnlineSkill skill)
        {
            if (!MultiplayerMatch.Active || gameOver)
                return;
            if (Time.unscaledTime < nextAttackTime)
                return;
            if (!energySystem.CanAfford(skill))
            {
                if (tacticalBoard != null)
                {
                    tacticalBoard.LastMessage = "Chưa đủ năng lượng cho " + SkillName(skill) + ".";
                    RefreshTacticalBoardUi();
                }
                return;
            }

            energySystem.TrySpend(skill);
            nextAttackTime = Time.unscaledTime + 0.4f;
            RuntimeArt.PlayUiSwitchSound();
            string msg;
            switch (skill)
            {
                case OnlineSkill.Attack:
                    MultiplayerManager.Instance?.SendSkill(OnlineSkill.Attack, (byte)OnlineConfig.AttackDamage);
                    shake = 0.18f;
                    msg = "Tung đòn tấn công!";
                    break;
                case OnlineSkill.Shield:
                    healthSystem.AddShield();
                    msg = "Dựng khiên (chặn 1 đòn).";
                    break;
                case OnlineSkill.Garbage:
                default:
                    MultiplayerManager.Instance?.SendSkill(OnlineSkill.Garbage, (byte)OnlineConfig.GarbageLines);
                    shake = 0.15f;
                    msg = "Thả " + OnlineConfig.GarbageLines + " hàng rác sang đối thủ!";
                    break;
            }
            RefreshSkillBar();
            SendMultiplayerState();
            if (tacticalBoard != null)
            {
                tacticalBoard.LastMessage = msg;
                RefreshTacticalBoardUi();
            }
        }

        static string SkillName(OnlineSkill s) => s == OnlineSkill.Attack ? "Đánh" : s == OnlineSkill.Shield ? "Khiên" : "Rác";

        void RefreshSkillBar()
        {
            if (skillAttackButton != null)
                skillAttackButton.interactable = MultiplayerMatch.Active && !gameOver && energySystem.CanAfford(OnlineSkill.Attack);
            if (skillShieldButton != null)
                skillShieldButton.interactable = MultiplayerMatch.Active && !gameOver && energySystem.CanAfford(OnlineSkill.Shield);
            if (skillGarbageButton != null)
                skillGarbageButton.interactable = MultiplayerMatch.Active && !gameOver && energySystem.CanAfford(OnlineSkill.Garbage);
            if (skillInfoText != null)
                skillInfoText.text = "NL " + energySystem.Energy + "/" + energySystem.Max
                    + "   Máu " + healthSystem.Health + "/" + healthSystem.Max
                    + (healthSystem.ShieldCharges > 0 ? " [Khiên]" : "");

            // HUD online mới.
            if (onlineSkillBtn[0] != null) onlineSkillBtn[0].interactable = MultiplayerMatch.Active && !gameOver && energySystem.CanAfford(OnlineSkill.Garbage);
            if (onlineSkillBtn[1] != null) onlineSkillBtn[1].interactable = MultiplayerMatch.Active && !gameOver && energySystem.CanAfford(OnlineSkill.Attack);
            if (onlineSkillBtn[2] != null) onlineSkillBtn[2].interactable = MultiplayerMatch.Active && !gameOver && energySystem.CanAfford(OnlineSkill.Shield);
            UpdateOnlineHud();
        }

        // Gửi trạng thái điểm/hàng kèm máu+năng lượng cho đối thủ (HUD).
        void SendMultiplayerState()
        {
            if (!MultiplayerMatch.Active || MultiplayerManager.Instance == null)
                return;
            MultiplayerManager.Instance.SendState(score, lines, 0, healthSystem.Health, energySystem.Energy);
        }

        void ShowGarbageWarning()
        {
            if (garbageWarningText == null)
            {
                var parent = safeAreaRoot != null ? safeAreaRoot.transform : transform;
                garbageWarningText = Ui.Text(parent, "!! SẮP CÓ HÀNG RÁC !!", font, 38, new Color(1f, 0.35f, 0.2f), TextAnchor.MiddleCenter);
                garbageWarningText.fontStyle = FontStyle.Bold;
                garbageWarningText.raycastTarget = false;
                Ui.Rect(garbageWarningText, new Vector2(0.5f, 0.44f), new Vector2(0.5f, 0.44f), new Vector2(640, 60));
                AddDarkWoodTextEdge(garbageWarningText, 0.9f, 0.9f);
            }
            garbageWarningText.gameObject.SetActive(true);
        }

        void FlashGarbageWarning()
        {
            if (garbageWarningText == null)
                return;
            float alpha = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 12f));
            garbageWarningText.color = new Color(1f, 0.35f, 0.2f, alpha);
        }

        void HideGarbageWarning()
        {
            garbageWarnUntil = 0f;
            if (garbageWarningText != null)
                garbageWarningText.gameObject.SetActive(false);
        }

        // Áp các đòn tấn công đối thủ gửi tới vào máu mình (design §7.1). Khiên chặn 1 đòn.
        // Peer tự quản máu của chính mình (thay server authority §14 vì kiến trúc P2P).
        void ProcessIncomingAttacks()
        {
            int pending = MultiplayerMatch.PendingIncomingAttacks;
            if (pending <= 0)
                return;
            MultiplayerMatch.PendingIncomingAttacks = 0;
            MultiplayerMatch.AttackWarningActive = false;

            bool anyBlocked = false, anyHit = false;
            for (int i = 0; i < pending; i++)
            {
                bool blocked = healthSystem.TakeAttack(OnlineConfig.AttackDamage);
                anyBlocked |= blocked;
                anyHit |= !blocked;
            }
            RefreshSkillBar();
            SendMultiplayerState();
            if (tacticalBoard != null)
            {
                tacticalBoard.LastMessage = anyBlocked && !anyHit ? "Khiên đã chặn đòn tấn công!"
                    : "Trúng đòn! Máu còn " + healthSystem.Health + "/" + healthSystem.Max;
                RefreshTacticalBoardUi();
            }
            shake = 0.2f;

            // Design §9: máu về 0 → thua. Nếu đối thủ cũng vừa hết máu → luật hòa §15.
            if (healthSystem.IsDead)
            {
                if (MultiplayerMatch.OpponentHealth <= 0)
                    MultiplayerEndMatch(ResolveByLines(), "Cả hai cùng hết máu!");
                else
                    MultiplayerEndMatch(-1, "Bạn đã hết máu!");
            }
        }

        // Design §15: trận vượt quá thời gian tối đa → phân định theo máu rồi số hàng.
        void CheckMatchTimeLimit()
        {
            if (gameOver || gameplayTime < OnlineConfig.MaxMatchSeconds)
                return;
            int outcome;
            if (healthSystem.Health != MultiplayerMatch.OpponentHealth)
                outcome = healthSystem.Health > MultiplayerMatch.OpponentHealth ? 1 : -1;
            else
                outcome = ResolveByLines();
            MultiplayerEndMatch(outcome, "Hết giờ trận đấu!");
        }

        // Đối thủ báo kết thúc hoặc rời trận — xử ở đầu Update mỗi frame.
        void CheckOpponentMatchEvents()
        {
            if (MultiplayerMatch.OpponentLost)
            {
                // Đối thủ hết máu — nếu mình cũng vừa chết thì phân định bằng số hàng (§15).
                if (healthSystem.IsDead)
                    MultiplayerEndMatch(ResolveByLines(), "Cả hai cùng hết máu!");
                else
                    MultiplayerEndMatch(1, "Đối thủ đã hết máu!");
            }
            else if (MultiplayerMatch.OpponentFinished)
                MultiplayerEndMatch(-1, "Đối thủ đã hoàn thành bàn cờ trước!");
            else if (MultiplayerMatch.OpponentLeft)
                MultiplayerEndMatch(1, "Đối thủ đã rời trận.");
        }

        // Design §15: khi hai bên cùng thua (máu 0 / bảng đầy) trong cùng nhịp,
        // người xóa nhiều hàng hơn thắng; bằng nhau → hòa. outcome: 1 thắng, 0 hòa, -1 thua.
        int ResolveByLines()
        {
            if (lines > MultiplayerMatch.OpponentLines) return 1;
            if (lines < MultiplayerMatch.OpponentLines) return -1;
            return 0;
        }

        void MultiplayerEndMatch(bool won, string reason)
        {
            MultiplayerEndMatch(won ? 1 : -1, reason);
        }

        void MultiplayerEndMatch(int outcome, string reason)
        {
            if (gameOver)
                return;
            gameOver = true;
            resolving = false;
            Time.timeScale = 1f;
            StopBackgroundMusic();
            ClearActive();
            statusText.text = "";

            var manager = MultiplayerManager.Instance;
            if (manager != null)
                manager.SendState(score, lines, outcome > 0 ? MultiplayerManager.FlagFinished : MultiplayerManager.FlagLost,
                    healthSystem.Health, energySystem.Energy);

            if (outcome > 0) { gameOverTitleText.text = "THẮNG TRẬN!"; gameOverTitleText.color = new Color(1f, 0.86f, 0.56f); }
            else if (outcome < 0) { gameOverTitleText.text = "THUA TRẬN"; gameOverTitleText.color = new Color(1f, 0.62f, 0.36f); }
            else { gameOverTitleText.text = "HÒA"; gameOverTitleText.color = new Color(0.92f, 0.88f, 0.66f); }

            string opponentLabel = string.IsNullOrEmpty(MultiplayerMatch.OpponentName) ? "Đối thủ" : MultiplayerMatch.OpponentName;
            gameOverScoreText.text = reason + "\nBạn  " + score + " điểm · " + lines + " hàng"
                + "\n" + opponentLabel + "  " + MultiplayerMatch.OpponentScore + " điểm · " + MultiplayerMatch.OpponentLines + " hàng";
            gameOverOverlay.SetActive(true);
            shake = outcome > 0 ? 0.35f : 0.2f;
            if (outcome > 0)
                Beep(1180f, 0.22f, 0.35f);
            else
                RuntimeArt.PlayGameOverSound();

            // Chờ chút cho cờ kết thúc kịp đến đối thủ rồi mới rời phòng.
            StartCoroutine(LeaveMatchAfterDelay(1.5f));
        }

        System.Collections.IEnumerator LeaveMatchAfterDelay(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            if (MultiplayerManager.Instance != null)
                _ = MultiplayerManager.Instance.LeaveAsync();
            else
                MultiplayerMatch.Reset();
        }

        void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("BrickGame");
        }

        string GameOverMessage()
        {
            if (tacticalBoard != null && tacticalBoard.Status == TacticalBoardStatus.Failed)
                return "Quái đã bắt được bạn\nLượt đã dùng  " + tacticalBoard.MovesUsed + "\nXóa dòng để kiếm lượt và dụ quái tốt hơn";
            return "Điểm  " + score;
        }

        void BackToMenu()
        {
            Time.timeScale = 1f;
            // Bỏ trận giữa chừng: rời phòng để đối thủ được xử thắng vắng mặt.
            if (MultiplayerMatch.Active && MultiplayerManager.Instance != null)
                _ = MultiplayerManager.Instance.LeaveAsync();
            else if (MultiplayerMatch.Active)
                MultiplayerMatch.Reset(); // thoát chế độ xem thử (không có mạng)
            SceneManager.LoadScene("BrickMenu");
        }

        void TogglePause()
        {
            if (gameOver)
                return;
            paused = !paused;
            Time.timeScale = paused ? 0f : 1f;
            if (pauseOverlay != null)
                pauseOverlay.SetActive(paused);
            var pauseText = pauseButton != null ? pauseButton.GetComponentInChildren<Text>() : null;
            if (pauseText != null)
                pauseText.text = "II";
            if (musicSource != null)
            {
                if (paused)
                    musicSource.Pause();
                else
                    musicSource.UnPause();
            }
        }

        void UpdateCameraShake()
        {
            if (shake <= 0)
            {
                cam.transform.position = Vector3.Lerp(cam.transform.position, cameraHome, Time.deltaTime * 8f);
                return;
            }

            shake -= Time.deltaTime;
            cam.transform.position = cameraHome + new Vector3(UnityEngine.Random.Range(-shake, shake), UnityEngine.Random.Range(-shake, shake), 0);
        }

        void PulseAndDestroy(GameObject target)
        {
            if (target != null)
                StartCoroutine(PulseRoutine(target));
        }

        IEnumerator PulseRoutine(GameObject target)
        {
            float t = 0;
            var renderer = target.GetComponent<SpriteRenderer>();
            while (t < 0.14f && target != null)
            {
                t += Time.deltaTime;
                target.transform.localScale = Vector3.one * Mathf.Lerp(0.9f, 1.35f, t / 0.14f);
                renderer.color = Color.Lerp(renderer.color, Color.white, 0.2f);
                yield return null;
            }
            if (target != null)
                Destroy(target);
        }

        void Beep(float frequency, float seconds, float volume)
        {
            var clip = AudioClip.Create("beep", Mathf.CeilToInt(44100 * seconds), 1, 44100, false);
            var data = new float[clip.samples];
            for (int i = 0; i < data.Length; i++)
            {
                float fade = 1f - i / (float)data.Length;
                data[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / 44100f) * fade * volume;
            }
            clip.SetData(data, 0);
            audioSource.PlayOneShot(clip);
        }

        void StartBackgroundMusic()
        {
            if (musicSource == null)
                return;

            var clip = Resources.Load<AudioClip>("BrickStacker/gameplay_music");
            if (clip == null)
            {
                Debug.LogWarning("BLOCKFALL audio missing: Resources/BrickStacker/gameplay_music");
                return;
            }

            if (clip.loadState == AudioDataLoadState.Unloaded)
                clip.LoadAudioData();

            musicSource.clip = clip;
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.mute = false;
            musicSource.volume = 0.18f;
            musicSource.spatialBlend = 0f;
            musicSource.Play();
        }

        void StopBackgroundMusic()
        {
            if (musicSource != null)
                musicSource.Stop();
        }
    }

    public class FloatyBrick : MonoBehaviour
    {
        public float Speed = 0.25f;
        float seed;

        void Awake()
        {
            seed = UnityEngine.Random.Range(0f, 10f);
        }

        void Update()
        {
            transform.position += Vector3.up * Mathf.Sin(Time.time * Speed + seed) * Time.deltaTime * 0.24f;
            transform.Rotate(0, 0, Speed * 12f * Time.deltaTime);
        }
    }

    public static class RuntimeArt
    {
        public static readonly Color GridColor = new Color(0.20f, 0.12f, 0.065f, 0.92f);
        public static readonly Color SpecialBlockColor = new Color(1f, 0.92f, 0.34f, 1f);
        static Sprite boardFrameSprite;
        static Sprite roundedWoodSprite;
        static Sprite woodBackdropSprite;
        static Sprite blurredWoodBackdropSprite;
        static Sprite pauseButtonSprite;
        static Sprite rotateButtonSprite;
        static Sprite tacticalCellSprite;
        static Sprite tacticalHighlightSprite;
        static Sprite tacticalWallSprite;
        static Sprite tacticalPlayerSprite;
        static Sprite tacticalEnemySprite;
        static Sprite tacticalMonsterSprite;

        public static void ResetTacticalSpriteCache()
        {
            tacticalPlayerSprite  = null;
            tacticalEnemySprite   = null;
            tacticalMonsterSprite = null;
        }
        static Sprite woodPanelSprite;
        static Sprite woodButtonSprite;
        static Sprite backArrowSprite;
        static Sprite solidSprite;
        static Font displayFont;
        static Font uiFont;
        static Font menuButtonFont;
        static AudioSource oneShotSource;
        static AudioClip uiSwitchClip;
        static AudioClip gameOverClip;

        public static void PlayUiSwitchSound()
        {
            if (uiSwitchClip == null)
                uiSwitchClip = Resources.Load<AudioClip>("BrickStacker/ui_switch");
            PrepareAudioClip(uiSwitchClip, "Resources/BrickStacker/ui_switch");
            PlayGlobalClip(uiSwitchClip, 0.42f);
        }

        public static void PlayGameOverSound()
        {
            if (gameOverClip == null)
                gameOverClip = Resources.Load<AudioClip>("BrickStacker/game_over_negative");
            PrepareAudioClip(gameOverClip, "Resources/BrickStacker/game_over_negative");
            PlayGlobalClip(gameOverClip, 0.70f);
        }

        static void PrepareAudioClip(AudioClip clip, string path)
        {
            if (clip == null)
            {
                Debug.LogWarning("BLOCKFALL audio missing: " + path);
                return;
            }

            if (clip.loadState == AudioDataLoadState.Unloaded)
                clip.LoadAudioData();
        }

        static void PlayGlobalClip(AudioClip clip, float volume)
        {
            if (clip == null)
                return;

            if (oneShotSource == null)
            {
                var audioObject = new GameObject("Blockfall One Shot Audio");
                UnityEngine.Object.DontDestroyOnLoad(audioObject);
                oneShotSource = audioObject.AddComponent<AudioSource>();
                oneShotSource.playOnAwake = false;
                oneShotSource.spatialBlend = 0f;
            }

            oneShotSource.PlayOneShot(clip, volume);
        }

        public static Font LoadDisplayFont()
        {
            if (displayFont != null)
                return displayFont;

            displayFont = Resources.Load<Font>("BrickStacker/Batangas_Bold");
            if (displayFont != null)
                return displayFont;

            displayFont = Resources.Load<Font>("BrickStacker/DFVN_Moju_Light");
            if (displayFont != null)
                return displayFont;

            displayFont = Resources.Load<Font>("BrickStacker/VietnameseArial");
            if (displayFont != null)
                return displayFont;

            displayFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (displayFont != null)
                return displayFont;

            return displayFont;
        }

        // Font riêng cho 4 nút chính màn menu (Paytone One). Fallback về UI font nếu thiếu.
        public static Font LoadMenuButtonFont()
        {
            if (menuButtonFont != null)
                return menuButtonFont;

            menuButtonFont = Resources.Load<Font>("BrickStacker/BTDanta-Bold");
            if (menuButtonFont != null)
                return menuButtonFont;

            return LoadUiFont();
        }

        public static Font LoadUiFont()
        {
            if (uiFont != null)
                return uiFont;

            uiFont = Resources.Load<Font>("BrickStacker/DFVN_Moju_Light");
            if (uiFont != null)
                return uiFont;

            uiFont = Resources.Load<Font>("BrickStacker/VietnameseArial");
            if (uiFont != null)
                return uiFont;

            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (uiFont != null)
                return uiFont;

            return LoadDisplayFont();
        }

        public static Sprite[] LoadPieceBlockSprites()
        {
            var sprites = new Sprite[7];
            for (int i = 0; i < sprites.Length; i++)
            {
                string assetName = "BrickStacker/SlicedAssets/block_pieces_" + (i + 1).ToString("00");
                sprites[i] = Resources.Load<Sprite>(assetName);
                if (sprites[i] == null)
                {
                    var texture = Resources.Load<Texture2D>(assetName);
                    if (texture != null)
                    {
                        texture.filterMode = FilterMode.Bilinear;
                        texture.wrapMode = TextureWrapMode.Clamp;
                        sprites[i] = CreateFullRectSpriteSafe(texture);
                    }
                }
            }
            return sprites;
        }

        static Sprite CreateFullRectSpriteSafe(Texture2D texture)
        {
            if (texture == null)
                return null;

            try
            {
                return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static Sprite CreateBlockSprite()
        {
            const int size = 40;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float u = x / (float)(size - 1);
                    float v = y / (float)(size - 1);
                    float edgeDistance = Mathf.Min(Mathf.Min(x, y), Mathf.Min(size - 1 - x, size - 1 - y));
                    float cornerDistance = Mathf.Min(
                        Vector2.Distance(new Vector2(x, y), new Vector2(4f, 4f)),
                        Mathf.Min(
                            Vector2.Distance(new Vector2(x, y), new Vector2(size - 5f, 4f)),
                            Mathf.Min(
                                Vector2.Distance(new Vector2(x, y), new Vector2(4f, size - 5f)),
                                Vector2.Distance(new Vector2(x, y), new Vector2(size - 5f, size - 5f)))));

                    bool roundedCorner = (x < 5 || x > size - 6) && (y < 5 || y > size - 6) && cornerDistance > 5.4f;
                    if (roundedCorner)
                    {
                        texture.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    float bevel = Mathf.Clamp01(edgeDistance / 7f);
                    float topLeftLight = Mathf.Clamp01((1f - u) * 0.42f + v * 0.34f);
                    float bottomRightShade = Mathf.Clamp01(u * 0.32f + (1f - v) * 0.38f);
                    float fineGrain = (Mathf.PerlinNoise(x * 0.17f, y * 0.19f) - 0.5f) * 0.055f;
                    float softStreak = Mathf.Sin((x * 0.28f + y * 0.11f) + Mathf.PerlinNoise(y * 0.05f, x * 0.04f) * 1.4f) * 0.018f;
                    float centerGlow = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(u, v), new Vector2(0.42f, 0.58f)) * 1.65f) * 0.08f;
                    float value = 0.86f + fineGrain + softStreak + centerGlow + topLeftLight * 0.10f - bottomRightShade * 0.08f;

                    if (edgeDistance < 1.5f)
                        value = 0.34f;
                    else if (edgeDistance < 3.0f)
                        value = Mathf.Lerp(0.42f, value, 0.38f);
                    else
                        value = Mathf.Lerp(0.55f, value, bevel);

                    if (x > 6 && x < size - 7 && y > size - 11 && y < size - 5)
                        value = Mathf.Lerp(value, 1f, 0.16f);

                    Color color = new Color(Mathf.Clamp01(value), Mathf.Clamp01(value), Mathf.Clamp01(value), 1f);

                    texture.SetPixel(x, y, color);
                }
            }
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        public static Sprite CreateBackArrowSprite()
        {
            if (backArrowSprite != null)
                return backArrowSprite;

            var texture = Resources.Load<Texture2D>("BrickStacker/back_arrow");
            if (texture == null)
                return CreateWoodButtonSprite();

            backArrowSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            return backArrowSprite;
        }

        // Load sprite từ thư mục Assets-v3.0.
        // Editor: đọc trực tiếp từ đĩa qua Application.dataPath.
        // Build runtime: cần copy file vào Assets/Resources/Assets-v3.0/ trước khi build.
        // Cache sprite v3.0 theo đường dẫn: mỗi ảnh chỉ nạp texture MỘT lần, tránh nạp lại
        // nhiều lần (vd 10 dòng bảng xếp hạng) gây áp lực bộ nhớ khiến texture nạp lỗi -> trắng.
        static readonly System.Collections.Generic.Dictionary<string, Sprite> v3SpriteCache = new System.Collections.Generic.Dictionary<string, Sprite>();

        public static Sprite LoadV3Sprite(string relativePath)
        {
            if (v3SpriteCache.TryGetValue(relativePath, out var cached) && cached != null)
                return cached;

            Sprite result = null;
            string resourceKey = "Assets-v3.0/" + relativePath.Replace(".png", "");
            var tex = Resources.Load<Texture2D>(resourceKey);
            if (tex != null)
                result = CreateFullRectSpriteSafe(tex);

#if UNITY_EDITOR
            if (result == null)
            {
                string fullPath = Application.dataPath + "/Assets-v3.0/" + relativePath;
                if (System.IO.File.Exists(fullPath))
                {
                    var bytes = System.IO.File.ReadAllBytes(fullPath);
                    var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (texture.LoadImage(bytes))
                    {
                        texture.filterMode = FilterMode.Bilinear;
                        texture.wrapMode = TextureWrapMode.Clamp;
                        result = CreateFullRectSpriteSafe(texture);
                    }
                }
            }
#endif
            if (result != null)
                v3SpriteCache[relativePath] = result;
            return result;
        }

        // Cắt một vùng (theo tọa độ chuẩn hóa 0..1, gốc dưới-trái) từ ảnh v3.0 thành sprite riêng.
        // Dùng cho landscape: tách logo từ bg-menu.png dọc thay vì cần ảnh ngang mới.
        public static Sprite LoadV3SubSprite(string relativePath, Rect normalizedRect)
        {
            var full = LoadV3Sprite(relativePath);
            if (full == null)
                return null;

            var tex = full.texture;
            var pixelRect = new Rect(
                Mathf.Clamp(normalizedRect.x * tex.width, 0, tex.width - 1),
                Mathf.Clamp(normalizedRect.y * tex.height, 0, tex.height - 1),
                Mathf.Clamp(normalizedRect.width * tex.width, 1, tex.width),
                Mathf.Clamp(normalizedRect.height * tex.height, 1, tex.height));
            if (pixelRect.xMax > tex.width) pixelRect.width = tex.width - pixelRect.x;
            if (pixelRect.yMax > tex.height) pixelRect.height = tex.height - pixelRect.y;
            return Sprite.Create(tex, pixelRect, new Vector2(0.5f, 0.5f), 100f);
        }

        public static Sprite CreatePauseButtonSprite()
        {
            if (pauseButtonSprite != null)
                return pauseButtonSprite;

            pauseButtonSprite = CreateSpriteFromAtlas("BrickStacker/ui_wood_buttons_atlas", 0.105f, 0.160f, 0.165f, 0.255f, 100f);
            if (pauseButtonSprite != null)
                return pauseButtonSprite;

            const int size = 112;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.47f;
            float innerRadius = size * 0.34f;
            var woodData = Resources.Load<TextAsset>("BrickStacker/wood_background_source");
            Texture2D woodTexture = null;
            if (woodData != null)
            {
                woodTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!woodTexture.LoadImage(woodData.bytes))
                    woodTexture = null;
            }

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    float distance = Vector2.Distance(p, center);
                    if (distance > radius)
                    {
                        texture.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    float t = distance / radius;
                    Color wood = woodTexture != null
                        ? woodTexture.GetPixelBilinear(0.30f + x / (float)size * 0.34f, 0.18f + y / (float)size * 0.34f)
                        : new Color(0.58f, 0.31f, 0.13f, 1f);
                    float grain = Mathf.PerlinNoise(x * 0.070f, y * 0.020f) * 0.10f;
                    float stripe = Mathf.Sin((x + y * 0.18f) * 0.18f) * 0.035f;
                    Color color = Color.Lerp(wood, new Color(0.26f, 0.11f, 0.040f, 1f), 0.34f + t * 0.16f);
                    color = Color.Lerp(color, new Color(0.76f, 0.45f, 0.20f, 1f), 0.22f);
                    color += new Color(grain + stripe, (grain + stripe) * 0.50f, (grain + stripe) * 0.22f, 0f);

                    if (distance > radius - 7f)
                        color = Color.Lerp(color, new Color(0.035f, 0.012f, 0.004f, 1f), 0.96f);
                    else if (distance > radius - 13f)
                        color = Color.Lerp(color, new Color(0.18f, 0.070f, 0.022f, 1f), 0.72f);
                    else if (Mathf.Abs(distance - innerRadius) < 3.2f)
                    {
                        float ringLight = Mathf.Clamp01(1f - distance / radius);
                        Color ringColor = Color.Lerp(new Color(0.18f, 0.070f, 0.024f, 1f), new Color(0.74f, 0.47f, 0.24f, 1f), ringLight);
                        color = Color.Lerp(color, ringColor, 0.62f);
                    }

                    float highlight = Mathf.Clamp01(1f - Vector2.Distance(p, center + new Vector2(-18f, 20f)) / 54f);
                    color = Color.Lerp(color, new Color(1f, 0.78f, 0.42f, 1f), highlight * 0.28f);
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            pauseButtonSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return pauseButtonSprite;
        }

        public static Sprite CreateRotateButtonSprite()
        {
            if (rotateButtonSprite != null)
                return rotateButtonSprite;

            rotateButtonSprite = CreateSpriteFromAtlas("BrickStacker/ui_wood_buttons_atlas", 0.305f, 0.160f, 0.165f, 0.255f, 100f);
            return rotateButtonSprite ?? CreatePauseButtonSprite();
        }

        public static Sprite CreateTacticalCellSprite()
        {
            if (tacticalCellSprite != null)
                return tacticalCellSprite;

            tacticalCellSprite = CreateSpriteFromAtlas("BrickStacker/tactical_tiles_atlas", 0.050f, 0.105f, 0.145f, 0.205f, 100f);
            return tacticalCellSprite ?? CreateWoodButtonSprite();
        }

        public static Sprite CreateTacticalHighlightSprite()
        {
            if (tacticalHighlightSprite != null)
                return tacticalHighlightSprite;

            tacticalHighlightSprite = CreateSpriteFromAtlas("BrickStacker/tactical_tiles_atlas", 0.395f, 0.095f, 0.175f, 0.230f, 100f);
            return tacticalHighlightSprite ?? CreateTacticalCellSprite();
        }

        public static Sprite CreateTacticalWallSprite()
        {
            if (tacticalWallSprite != null)
                return tacticalWallSprite;

            tacticalWallSprite = CreateSpriteFromAtlas("BrickStacker/tactical_tiles_atlas", 0.610f, 0.095f, 0.170f, 0.230f, 100f);
            return tacticalWallSprite ?? CreateTacticalCellSprite();
        }

        public static Sprite CreateTacticalPlayerSprite()
        {
            if (tacticalPlayerSprite != null)
                return tacticalPlayerSprite;

            tacticalPlayerSprite = CreateSpriteFromAtlas("BrickStacker/tactical_pieces_atlas", 0.2307f, 0.1335f, 0.1278f, 0.2339f, 100f);
            return tacticalPlayerSprite;
        }

        public static Sprite CreateTacticalEnemySprite()
        {
            if (tacticalEnemySprite != null)
                return tacticalEnemySprite;

            tacticalEnemySprite = CreateSpriteFromAtlas("BrickStacker/tactical_pieces_atlas", 0.4406f, 0.1400f, 0.1312f, 0.2256f, 100f);
            return tacticalEnemySprite;
        }

        public static Sprite CreateTacticalMonsterSprite()
        {
            if (tacticalMonsterSprite != null)
                return tacticalMonsterSprite;

            tacticalMonsterSprite = CreateSpriteFromAtlas("BrickStacker/tactical_pieces_atlas", 0.6457f, 0.1538f, 0.1506f, 0.2155f, 100f);
            return tacticalMonsterSprite;
        }

        static Sprite CreateSpriteFromAtlas(string resourcePath, float normalizedX, float normalizedTop, float normalizedWidth, float normalizedHeight, float pixelsPerUnit)
        {
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null)
                return null;

            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            float x = Mathf.Clamp01(normalizedX) * texture.width;
            float width = Mathf.Clamp01(normalizedWidth) * texture.width;
            float height = Mathf.Clamp01(normalizedHeight) * texture.height;
            float y = texture.height - (Mathf.Clamp01(normalizedTop) * texture.height) - height;
            var rect = new Rect(Mathf.Round(x), Mathf.Round(Mathf.Clamp(y, 0, texture.height - height)), Mathf.Round(width), Mathf.Round(height));
            return Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), pixelsPerUnit, 0, SpriteMeshType.FullRect);
        }

        public static Sprite CreateWoodPanelSprite()
        {
            if (woodPanelSprite != null)
                return woodPanelSprite;

            woodPanelSprite = CreateWoodUiSprite(192, 192, 30, 20, true);
            return woodPanelSprite;
        }

        public static Sprite CreateWoodButtonSprite()
        {
            if (woodButtonSprite != null)
                return woodButtonSprite;

            woodButtonSprite = CreateWoodUiSprite(192, 72, 12, 10, false);
            return woodButtonSprite;
        }

        // Nút vuông bóng (glossy) màu tùy chọn — dùng cho hàng nút menu để đồng bộ với btn-bxh.

        static Sprite CreateWoodUiSprite(int width, int height, int radius, int border, bool deepPanel)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            var woodData = Resources.Load<TextAsset>("BrickStacker/wood_background_source");
            Texture2D woodTexture = null;
            if (woodData != null)
            {
                woodTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!woodTexture.LoadImage(woodData.bytes))
                    woodTexture = null;
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (OutsideRoundedRect(x, y, width, height, radius))
                    {
                        texture.SetPixel(x, y, Color.clear);
                        continue;
                    }

                    int edgeDistance = Mathf.Min(Mathf.Min(x, width - 1 - x), Mathf.Min(y, height - 1 - y));
                    Color wood = woodTexture != null
                        ? woodTexture.GetPixelBilinear(0.18f + x / (float)width * 0.48f, 0.18f + y / (float)height * 0.42f)
                        : new Color(0.48f, 0.24f, 0.095f, 1f);

                    float grain = Mathf.PerlinNoise(x * 0.055f, y * 0.025f) * 0.08f;
                    Color color = Color.Lerp(wood, deepPanel ? new Color(0.18f, 0.070f, 0.024f, 1f) : new Color(0.28f, 0.12f, 0.045f, 1f), deepPanel ? 0.55f : 0.36f);
                    color = Color.Lerp(color, new Color(0.72f, 0.42f, 0.20f, 1f), deepPanel ? 0.08f : 0.18f);
                    color += new Color(grain, grain * 0.45f, grain * 0.18f, 0f);

                    if (edgeDistance < border)
                    {
                        float edge = 1f - edgeDistance / (float)Mathf.Max(1, border);
                        color = Color.Lerp(color, new Color(0.045f, 0.016f, 0.006f, 1f), edge * 0.88f);
                    }
                    else if (edgeDistance < border + 5)
                    {
                        color = Color.Lerp(color, new Color(0.80f, 0.50f, 0.27f, 1f), 0.18f);
                    }

                    if (y > height - border - 8 && edgeDistance >= border)
                        color = Color.Lerp(color, new Color(0.95f, 0.65f, 0.36f, 1f), 0.10f);

                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        static bool OutsideRoundedRect(int x, int y, int width, int height, int radius)
        {
            if (x >= radius && x < width - radius)
                return false;
            if (y >= radius && y < height - radius)
                return false;

            int cx = x < radius ? radius : width - radius - 1;
            int cy = y < radius ? radius : height - radius - 1;
            return DistanceSq(x, y, cx, cy) > radius * radius;
        }

        public static bool HasBoardFrameSprite()
        {
            return Resources.Load<TextAsset>("BrickStacker/board_frame_source") != null;
        }

        public static Sprite CreateBoardFrameSprite()
        {
            if (boardFrameSprite != null)
                return boardFrameSprite;

            var data = Resources.Load<TextAsset>("BrickStacker/board_frame_source");
            if (data == null)
                return null;

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(data.bytes))
                return null;

            texture = CreateRoundedTexture(texture, Mathf.RoundToInt(Mathf.Min(texture.width, texture.height) * 0.085f));
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            boardFrameSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            return boardFrameSprite;
        }

        public static Sprite CreateRoundedWoodSprite()
        {
            if (roundedWoodSprite != null)
                return roundedWoodSprite;

            var data = Resources.Load<TextAsset>("BrickStacker/wood_background_source");
            if (data == null)
                return null;

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(data.bytes))
                return null;

            var rounded = CreateRoundedTexture(texture, Mathf.RoundToInt(Mathf.Min(texture.width, texture.height) * 0.055f));
            rounded.filterMode = FilterMode.Bilinear;
            rounded.wrapMode = TextureWrapMode.Clamp;
            roundedWoodSprite = Sprite.Create(rounded, new Rect(0, 0, rounded.width, rounded.height), new Vector2(0.5f, 0.5f), 100f);
            return roundedWoodSprite;
        }

        static Texture2D CreateRoundedTexture(Texture2D source, int radius)
        {
            var output = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            var pixels = source.GetPixels32();
            int width = source.width;
            int height = source.height;
            int radiusSq = radius * radius;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool outside = false;
                    if (x < radius && y < radius)
                        outside = DistanceSq(x, y, radius, radius) > radiusSq;
                    else if (x >= width - radius && y < radius)
                        outside = DistanceSq(x, y, width - radius - 1, radius) > radiusSq;
                    else if (x < radius && y >= height - radius)
                        outside = DistanceSq(x, y, radius, height - radius - 1) > radiusSq;
                    else if (x >= width - radius && y >= height - radius)
                        outside = DistanceSq(x, y, width - radius - 1, height - radius - 1) > radiusSq;

                    var color = pixels[y * width + x];
                    if (outside)
                        color.a = 0;
                    pixels[y * width + x] = color;
                }
            }

            output.SetPixels32(pixels);
            output.Apply();
            return output;
        }

        static int DistanceSq(int x, int y, int cx, int cy)
        {
            int dx = x - cx;
            int dy = y - cy;
            return dx * dx + dy * dy;
        }

        public static Material Material(Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
            material.color = color;
            return material;
        }

        public static void CreateWoodBackdrop(string name, Camera cam, float z, Color overlayColor)
        {
            if (cam == null)
                cam = Camera.main ?? UnityEngine.Object.FindAnyObjectByType<Camera>();

            Vector3 center = cam != null ? new Vector3(cam.transform.position.x, cam.transform.position.y, z) : new Vector3(0, 0, z);

            var sprite = CreateWoodBackdropSprite();
            var back = new GameObject(name);
            back.name = name;
            back.transform.position = center;
            var backRenderer = back.AddComponent<SpriteRenderer>();
            backRenderer.sprite = sprite;
            backRenderer.sortingOrder = -1000;
            var backFitter = back.AddComponent<CameraSpriteFitter>();
            backFitter.Target = cam;
            backFitter.Depth = z;
            backFitter.Overscan = 2.18f;

            var blurSprite = CreateBlurredWoodBackdropSprite();
            var blur = new GameObject(name + " Soft Dark Blur");
            blur.name = name + " Soft Dark Blur";
            blur.transform.position = center + new Vector3(0, 0, -0.03f);
            var blurRenderer = blur.AddComponent<SpriteRenderer>();
            blurRenderer.sprite = blurSprite;
            blurRenderer.color = new Color(0.18f, 0.075f, 0.025f, 0.38f);
            blurRenderer.sortingOrder = -999;
            var blurFitter = blur.AddComponent<CameraSpriteFitter>();
            blurFitter.Target = cam;
            blurFitter.Depth = z - 0.03f;
            blurFitter.Overscan = 2.18f;

            var overlay = new GameObject(name + " Shade");
            overlay.name = name + " Shade";
            overlay.transform.position = center + new Vector3(0, 0, -0.04f);
            var overlayRenderer = overlay.AddComponent<SpriteRenderer>();
            overlayRenderer.sprite = CreateSolidSprite();
            overlayRenderer.color = overlayColor;
            overlayRenderer.sortingOrder = -998;
            var overlayFitter = overlay.AddComponent<CameraSpriteFitter>();
            overlayFitter.Target = cam;
            overlayFitter.Depth = z - 0.04f;
            overlayFitter.Overscan = 2.18f;
        }

        static Sprite CreateWoodBackdropSprite()
        {
            if (woodBackdropSprite != null)
                return woodBackdropSprite;

            var texture = Resources.Load<Texture2D>("BrickStacker/wood_background");
            if (texture == null)
                return CreateSolidSprite();

            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            woodBackdropSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            return woodBackdropSprite;
        }

        static Sprite CreateBlurredWoodBackdropSprite()
        {
            if (blurredWoodBackdropSprite != null)
                return blurredWoodBackdropSprite;

            var data = Resources.Load<TextAsset>("BrickStacker/wood_background_source");
            if (data == null)
                return CreateWoodBackdropSprite();

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(data.bytes))
                return CreateWoodBackdropSprite();

            int width = Mathf.Min(128, texture.width);
            int height = Mathf.Max(1, Mathf.RoundToInt(texture.height * (width / (float)texture.width)));
            var small = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float u = (x + 0.5f) / width;
                    float v = (y + 0.5f) / height;
                    small.SetPixel(x, y, texture.GetPixelBilinear(u, v));
                }
            }

            small.Apply();
            for (int i = 0; i < 3; i++)
                small = BoxBlur(small);

            small.filterMode = FilterMode.Bilinear;
            small.wrapMode = TextureWrapMode.Clamp;
            blurredWoodBackdropSprite = Sprite.Create(small, new Rect(0, 0, small.width, small.height), new Vector2(0.5f, 0.5f), 100f);
            return blurredWoodBackdropSprite;
        }

        static Texture2D BoxBlur(Texture2D source)
        {
            int width = source.width;
            int height = source.height;
            var output = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color sum = Color.clear;
                    int count = 0;
                    for (int oy = -1; oy <= 1; oy++)
                    {
                        int py = Mathf.Clamp(y + oy, 0, height - 1);
                        for (int ox = -1; ox <= 1; ox++)
                        {
                            int px = Mathf.Clamp(x + ox, 0, width - 1);
                            sum += source.GetPixel(px, py);
                            count++;
                        }
                    }

                    output.SetPixel(x, y, sum / count);
                }
            }

            output.Apply();
            return output;
        }

        static Sprite CreateSolidSprite()
        {
            if (solidSprite != null)
                return solidSprite;

            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            for (int x = 0; x < 4; x++)
                for (int y = 0; y < 4; y++)
                    texture.SetPixel(x, y, Color.white);
            texture.Apply();
            solidSprite = Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 1f);
            return solidSprite;
        }

        static Material TransparentMaterial(Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
            material.color = color;
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = 3000;
            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 1f);
            return material;
        }

        public static ParticleSystem CreateLineParticles(Transform parent, Color color)
        {
            var go = new GameObject("Wood Dust Particles");
            go.transform.SetParent(parent);
            var particles = go.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.32f, 0.78f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 1.45f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.105f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.88f, 0.84f, 0.76f, 0.42f), new Color(0.52f, 0.52f, 0.50f, 0.16f));
            main.maxParticles = 180;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.03f;
            main.startRotation = new ParticleSystem.MinMaxCurve(-0.35f, 0.35f);
            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 46) });
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(9.6f, 0.28f, 0.08f);
            var velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.28f, 0.28f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.16f, 0.78f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.02f, 0.02f);
            var noise = particles.noise;
            noise.enabled = true;
            noise.strength = 0.18f;
            noise.frequency = 0.65f;

            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = TransparentMaterial(new Color(0.78f, 0.74f, 0.66f, 0.55f));

            particles.Stop();
            return particles;
        }
    }

    public static class Ui
    {
        public static Canvas CreateCanvas(string name)
        {
            EnsureEventSystem();
            var canvasObject = new GameObject(name);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            float screenAspect = Screen.height > 0 ? Screen.width / (float)Screen.height : scaler.referenceResolution.x / scaler.referenceResolution.y;
            float referenceAspect = scaler.referenceResolution.x / scaler.referenceResolution.y;
            scaler.matchWidthOrHeight = screenAspect >= referenceAspect ? 1f : 0f;
            canvasObject.AddComponent<ResponsiveCanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        static void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() != null)
                return;

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
        }

        public static GameObject Panel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            go.AddComponent<Image>().color = color;
            return go;
        }

        public static Text Text(Transform parent, string value, Font font, int size, Color color, TextAnchor anchor)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Min(12, size);
            text.resizeTextMaxSize = size;
            return text;
        }

        public static Button Button(Transform parent, string label, Font font, int size, UnityEngine.Events.UnityAction action)
        {
            var go = Panel(parent, "Button", new Color(0.12f, 0.24f, 0.28f, 0.92f));
            var button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            button.onClick.AddListener(action);
            var colors = button.colors;
            colors.highlightedColor = new Color(0.25f, 0.65f, 0.72f);
            colors.pressedColor = new Color(0.12f, 0.9f, 0.7f);
            button.colors = colors;

            var text = Text(go.transform, label, font, size, Color.white, TextAnchor.MiddleCenter);
            Stretch(text.gameObject);
            return button;
        }

        public static void Rect(Component component, Vector2 min, Vector2 max, Vector2 size)
        {
            Rect(component.gameObject, min, max, size);
        }

        public static void Rect(GameObject go, Vector2 min, Vector2 max, Vector2 size)
        {
            var rect = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }

        public static void Stretch(GameObject go)
        {
            var rect = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        // Screen.safeArea can be stale in the Editor when the Game View size changes
        // (it may still describe the previous resolution). On devices the safe area is
        // always consistent with the screen, so implausible values are treated as
        // full-screen instead of blindly trusted.
        public static Rect SafeArea()
        {
            Rect safe = Screen.safeArea;
            float w = Mathf.Max(1, Screen.width);
            float h = Mathf.Max(1, Screen.height);
            // Real devices only inset one axis at a time (top/bottom in portrait,
            // left/right in landscape), so the safe area spans the full screen on at
            // least one axis. Editor game views often report the raw panel pixel size
            // instead, which shrinks both axes — reject those too.
            bool valid = safe.width > 0f && safe.height > 0f
                && safe.xMin >= 0f && safe.yMin >= 0f
                && safe.xMax <= w + 0.5f && safe.yMax <= h + 0.5f
                && safe.width >= w * 0.7f && safe.height >= h * 0.7f
                && (Mathf.Abs(safe.width - w) <= 1f || Mathf.Abs(safe.height - h) <= 1f);
            return valid ? safe : new Rect(0f, 0f, w, h);
        }

        public static void ApplySafeArea(RectTransform rect)
        {
            Rect safe = SafeArea();
            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;
            min.x /= Mathf.Max(1, Screen.width);
            min.y /= Mathf.Max(1, Screen.height);
            max.x /= Mathf.Max(1, Screen.width);
            max.y /= Mathf.Max(1, Screen.height);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }

    public class ResponsiveCanvasScaler : MonoBehaviour
    {
        CanvasScaler scaler;

        void Awake()
        {
            scaler = GetComponent<CanvasScaler>();
            Apply();
        }

        void LateUpdate()
        {
            Apply();
        }

        void Apply()
        {
            if (scaler == null)
                return;

            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            if (scaler.referenceResolution.x <= 0f || scaler.referenceResolution.y <= 0f)
                scaler.referenceResolution = new Vector2(1920f, 1080f);

            float screenAspect = Screen.height > 0 ? Screen.width / (float)Screen.height : scaler.referenceResolution.x / scaler.referenceResolution.y;
            float referenceAspect = scaler.referenceResolution.x / scaler.referenceResolution.y;
            scaler.matchWidthOrHeight = screenAspect >= referenceAspect ? 1f : 0f;
        }
    }

    public class RectTransformSafeAreaFitter : MonoBehaviour
    {
        public float Padding = 24f;
        public float MaxScale = 1f;
        public bool UseOwnRectWhenLarge = true;

        RectTransform rect;
        RectTransform parentRect;
        Vector3 baseScale;
        int lastWidth;
        int lastHeight;
        Rect lastSafeArea;
        bool initialized;

        void Awake()
        {
            Initialize();
            ApplyNow();
        }

        void LateUpdate()
        {
            if (Screen.width == lastWidth && Screen.height == lastHeight && Screen.safeArea == lastSafeArea)
                return;
            ApplyNow();
        }

        void Initialize()
        {
            if (initialized)
                return;

            rect = transform as RectTransform;
            parentRect = rect != null ? rect.parent as RectTransform : null;
            baseScale = rect != null ? rect.localScale : transform.localScale;
            initialized = true;
        }

        public void ApplyNow()
        {
            Initialize();
            if (rect == null || parentRect == null || Screen.width <= 0 || Screen.height <= 0)
                return;

            Bounds contentBounds = CalculateContentBounds();
            if (contentBounds.size.x <= 1f || contentBounds.size.y <= 1f)
                return;

            Rect safe = Ui.SafeArea();
            Rect parent = parentRect.rect;
            float safeWidth = parent.width * Mathf.Clamp01(safe.width / Screen.width);
            float safeHeight = parent.height * Mathf.Clamp01(safe.height / Screen.height);
            float safeCenterX = parent.xMin + parent.width * Mathf.Clamp01((safe.xMin + safe.width * 0.5f) / Screen.width);
            float safeCenterY = parent.yMin + parent.height * Mathf.Clamp01((safe.yMin + safe.height * 0.5f) / Screen.height);

            float availableWidth = Mathf.Max(1f, safeWidth - Padding * 2f);
            float availableHeight = Mathf.Max(1f, safeHeight - Padding * 2f);
            float fit = Mathf.Min(availableWidth / contentBounds.size.x, availableHeight / contentBounds.size.y);
            float scale = Mathf.Min(MaxScale, fit);

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = baseScale * scale;
            rect.anchoredPosition = new Vector2(safeCenterX, safeCenterY) - (Vector2)contentBounds.center * scale;

            lastWidth = Screen.width;
            lastHeight = Screen.height;
            lastSafeArea = Screen.safeArea;
        }

        Bounds CalculateContentBounds()
        {
            Rect ownRect = rect.rect;
            if (UseOwnRectWhenLarge && ownRect.width > 120f && ownRect.height > 120f)
                return new Bounds(ownRect.center, ownRect.size);

            var children = rect.GetComponentsInChildren<RectTransform>(true);
            bool hasBounds = false;
            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
            var corners = new Vector3[4];

            for (int i = 0; i < children.Length; i++)
            {
                var child = children[i];
                if (child == null || child == rect || !child.gameObject.activeInHierarchy)
                    continue;

                child.GetWorldCorners(corners);
                for (int c = 0; c < corners.Length; c++)
                {
                    Vector3 local = rect.InverseTransformPoint(corners[c]);
                    if (!hasBounds)
                    {
                        bounds = new Bounds(local, Vector3.zero);
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(local);
                    }
                }
            }

            if (!hasBounds)
                bounds = new Bounds(ownRect.center, ownRect.size);

            return bounds;
        }
    }

    public class CameraSpriteFitter : MonoBehaviour
    {
        public Camera Target;
        public float Depth;
        public float Overscan = 2.18f;
        SpriteRenderer spriteRenderer;
        int lastWidth;
        int lastHeight;
        Vector3 lastCameraPosition;
        float lastCameraSize;

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            Apply(true);
        }

        void LateUpdate()
        {
            Apply(false);
        }

        void Apply(bool force)
        {
            if (Target == null)
                Target = Camera.main ?? FindAnyObjectByType<Camera>();
            if (Target == null || spriteRenderer == null || spriteRenderer.sprite == null)
                return;

            bool changed = force || Screen.width != lastWidth || Screen.height != lastHeight ||
                Target.transform.position != lastCameraPosition || !Mathf.Approximately(Target.orthographicSize, lastCameraSize);
            if (!changed)
                return;

            float height = Target.orthographic ? Target.orthographicSize * Overscan : 14f;
            float width = height * Mathf.Max(0.35f, Target.aspect);
            Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
            transform.position = new Vector3(Target.transform.position.x, Target.transform.position.y, Depth);
            transform.localScale = new Vector3(width / spriteSize.x, height / spriteSize.y, 1f);

            lastWidth = Screen.width;
            lastHeight = Screen.height;
            lastCameraPosition = Target.transform.position;
            lastCameraSize = Target.orthographicSize;
        }
    }

    public class SafeAreaFitter : MonoBehaviour
    {
        RectTransform rect;
        int lastWidth;
        int lastHeight;
        Rect lastSafeArea;

        void Awake()
        {
            rect = GetComponent<RectTransform>();
            Apply();
        }

        void LateUpdate()
        {
            if (Screen.width == lastWidth && Screen.height == lastHeight && Screen.safeArea == lastSafeArea)
                return;

            Apply();
        }

        void Apply()
        {
            if (rect == null)
                return;

            Ui.ApplySafeArea(rect);
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            lastSafeArea = Screen.safeArea;
        }
    }

    public class LevelNodePulse : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public float Amount = 0.035f;
        public float Speed = 2.2f;
        public float PressedScale = 0.92f;
        RectTransform rect;
        Vector3 baseScale;
        bool pressed;

        void Awake()
        {
            rect = GetComponent<RectTransform>();
            baseScale = rect != null ? rect.localScale : transform.localScale;
        }

        void LateUpdate()
        {
            if (rect == null)
                rect = GetComponent<RectTransform>();
            if (rect == null)
                return;

            float scale = (1f + Mathf.Sin(Time.unscaledTime * Speed) * Amount) * (pressed ? PressedScale : 1f);
            rect.localScale = baseScale * scale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pressed = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pressed = false;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pressed = false;
        }
    }

    public class TacticalCellInput : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public BrickGameController Controller;
        public int X;
        public int Y;

        public void OnPointerDown(PointerEventData eventData)
        {
            Controller?.HandleTacticalPointerDown(X, Y, eventData.position);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Controller?.HandleTacticalPointerUp(X, Y, eventData.position);
        }
    }

    public class PressScaleFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public float PressedScale = 0.94f;
        // When set, scale this target instead of self (use to avoid shrinking the hit area).
        public Transform VisualTarget;

        // Scale phải nhân tương đối với scale gốc bắt tại lúc nhấn — node map level
        // có scale scene ≈ 2.26, ghi đè tuyệt đối làm node co vĩnh viễn khi bấm ô khóa.
        Vector3 baseScale;
        bool pressed;

        public void OnPointerDown(PointerEventData eventData)
        {
            var r = TargetRect();
            if (r == null)
                return;
            if (!pressed)
            {
                baseScale = r.localScale;
                pressed = true;
            }
            r.localScale = new Vector3(baseScale.x * PressedScale, baseScale.y * PressedScale, baseScale.z);
        }

        public void OnPointerUp(PointerEventData eventData) => Release();
        public void OnPointerExit(PointerEventData eventData) => Release();

        void Release()
        {
            if (!pressed)
                return;
            pressed = false;
            var r = TargetRect();
            if (r != null)
                r.localScale = baseScale;
        }

        RectTransform TargetRect()
        {
            var t = VisualTarget != null ? VisualTarget : transform;
            return t as RectTransform;
        }
    }
}
