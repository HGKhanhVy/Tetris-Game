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

    public partial class MenuController : MonoBehaviour
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

            // Bố cục v3.0: tiêu đề BLOCK FALL + trang trí nổi bên trái, khung nút xanh bên phải.
            BuildMenuTitle(panel.transform);
            BuildMenuButtonFrame(panel.transform);
        }

        // Tiêu đề game bên trái + 4 khối trang trí nổi (kiếm/sét/khiên/ủng) bồng bềnh quanh nó.
        void BuildMenuTitle(Transform panel)
        {
            var title = MakeV3Image(panel, "Title", "screen-menu/title-game.png", FullCrop, false);
            PlaceV3(title, new Vector2(0.360f, 0.55f), new Vector2(580f, 435f));

            // Gán theo nội dung ảnh: kiếm đỏ trên-trái, sét vàng trên, khiên xanh dưới-trái, ủng xanh dưới-phải.
            AddMenuDecor(panel, "screen-menu/decor-2.png", new Vector2(0.125f, 0.72f), 82f, 0.9f);
            AddMenuDecor(panel, "screen-menu/decor-3.png", new Vector2(0.510f, 0.83f), 96f, 1.2f);
            AddMenuDecor(panel, "screen-menu/decor-1.png", new Vector2(0.175f, 0.26f), 100f, 1.1f);
            AddMenuDecor(panel, "screen-menu/decor-4.png", new Vector2(0.485f, 0.20f), 84f, 1.0f);
        }

        // Khung nút xanh bên phải (khung-btn) chứa 4 nút dọc đã có sẵn chữ.
        void BuildMenuButtonFrame(Transform panel)
        {
            var frame = MakeV3Image(panel, "Button Frame", "screen-menu/khung-btn.png", FullCrop, false);
            const float frameHeight = 620f;
            const float frameAspect = 1067f / 1474f; // tỉ lệ ngang/dọc của ảnh khung mới
            PlaceV3(frame, new Vector2(0.735f, 0.5f), new Vector2(frameHeight * frameAspect, frameHeight));

            // 4 nút chia đều trong lòng khung (tâm theo chiều dọc, fraction từ đáy).
            // Mỗi ảnh có vùng plate đục lệch nhau → cắt về đúng plate rồi đặt vào cùng
            // một kích thước cố định để 4 nút bằng nhau tuyệt đối (crop chuẩn hóa gốc dưới-trái).
            BuildFrameButton(frame.transform, "screen-menu/btn-batdau.png", new Rect(0.0488f, 0.1285f, 0.9015f, 0.7804f), 0.725f, true,
                () => { RuntimeArt.PlayUiSwitchSound(); SceneManager.LoadScene("BrickLevel"); });
            BuildFrameButton(frame.transform, "screen-menu/btn-1vs1.png", new Rect(0.0488f, 0.1050f, 0.9052f, 0.8232f), 0.575f, false,
                () => { RuntimeArt.PlayUiSwitchSound(); MultiplayerManager.PrewarmQuickQuery(); ShowMultiplayerOverlay(panel); });
            BuildFrameButton(frame.transform, "screen-menu/btn-huongdan.png", new Rect(0.0345f, 0.0801f, 0.9346f, 0.8066f), 0.425f, false,
                () => { RuntimeArt.PlayUiSwitchSound(); ShowTutorialOverlay(panel); });
            BuildFrameButton(frame.transform, "screen-menu/btn-bxh.png", new Rect(0.0580f, 0.1091f, 0.8840f, 0.7831f), 0.275f, false,
                () => { RuntimeArt.PlayUiSwitchSound(); ShowLeaderboardOverlay(panel); });
        }

        static readonly Rect FullCrop = new Rect(0f, 0f, 1f, 1f);

        // Đặt một Image theo anchor giữa (fraction màn/khung) và kích thước cố định.
        static void PlaceV3(Image img, Vector2 anchor, Vector2 size)
        {
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = size;
        }

        // Khối trang trí nổi + bồng bềnh nhẹ (không bắt click) tạo chiều sâu cho tiêu đề.
        void AddMenuDecor(Transform panel, string spriteAsset, Vector2 anchor, float sizePx, float bobSpeed)
        {
            var decor = MakeV3Image(panel, "Decor", spriteAsset, FullCrop, false);
            PlaceV3(decor, anchor, new Vector2(sizePx, sizePx));
            StartCoroutine(BobDecor(decor.rectTransform, bobSpeed, sizePx * 0.06f));
        }

        // Kích thước plate hiển thị dùng chung cho cả 4 nút (fraction bề ngang khung + tỉ lệ ngang/dọc).
        const float ButtonPlateWidthFrac = 0.66f;
        const float ButtonPlateAspect = 3.4f;

        // Nút trong khung: cắt sprite về đúng vùng plate rồi ép vào một kích thước cố định
        // (preserveAspect=false) để mọi nút bằng nhau bất kể lề trong ảnh gốc lệch nhau.
        Button BuildFrameButton(Transform frame, string spriteAsset, Rect plateCrop, float centerYFrac, bool pulse, UnityEngine.Events.UnityAction action)
        {
            var button = Ui.Button(frame, "", font, 1, action);
            var text = button.GetComponentInChildren<Text>();
            if (text != null) Destroy(text.gameObject);

            var rt = button.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, centerYFrac);
            rt.pivot = new Vector2(0.5f, 0.5f);
            var frameSize = ((RectTransform)frame).sizeDelta;
            float buttonWidth = frameSize.x * ButtonPlateWidthFrac;
            rt.sizeDelta = new Vector2(buttonWidth, buttonWidth / ButtonPlateAspect);

            var img = button.GetComponent<Image>();
            var spr = RuntimeArt.LoadV3SubSprite(spriteAsset, plateCrop);
            if (spr != null) { img.sprite = spr; img.type = Image.Type.Simple; img.preserveAspect = false; img.color = Color.white; }
            else img.color = new Color(0.72f, 0.48f, 0.14f);

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.94f, 0.80f, 1f);
            colors.pressedColor = new Color(0.80f, 0.80f, 0.85f, 1f);
            colors.selectedColor = Color.white;
            button.colors = colors;

            AddPressScaleFeedback(button.gameObject, 0.95f);
            if (pulse) StartCoroutine(PulseButton(button.transform, null));
            return button;
        }

        // Bồng bềnh lên xuống nhẹ cho khối trang trí menu.
        System.Collections.IEnumerator BobDecor(RectTransform rt, float speed, float amplitude)
        {
            Vector2 basePos = rt.anchoredPosition;
            float phase = UnityEngine.Random.value * Mathf.PI * 2f;
            while (rt != null)
            {
                float y = amplitude * Mathf.Sin(Time.time * speed * Mathf.PI + phase);
                rt.anchoredPosition = basePos + new Vector2(0f, y);
                yield return null;
            }
        }

        // Nút bảng xếp hạng góc phải dưới, đối xứng với nút Hướng dẫn góc trái.

        // Màn BẢNG XẾP HẠNG (thiết kế screen-bxh): full màn ngang, nền dùng chung trang level,
        // nút back dùng chung, tiêu đề gỗ + panel CÚP góc phải, bảng xanh 10 dòng
        // (top1/2/3 khung vàng/bạc/đồng + huy hiệu; còn lại khung xanh + số hạng).
        const string BXH = "screen-bxh/";

        // Avatar mẫu (icon nhân vật) xoay vòng cho từng dòng — leaderboard thật không kèm avatar.
        static readonly string[] LbAvatars = { "red_demon", "purple_monster", "green_knight", "red_robot", "blue_knight", "purple_bat" };

        static string FormatLbScore(long v) => v.ToString("#,0").Replace(',', '.');

        const int LbTopCount = 20;

        // Sprite thanh bo tròn (góc trong suốt, 9-slice) — vẽ 1 lần, tô màu theo hạng.
        static Sprite lbRoundedBar;

        // Màu thanh dòng lấy từ chính asset frame-topX (điểm giữa) để đúng vàng/bạc/đồng/xanh.

        // Popup 1 vs 1 online — dựng theo thiết kế popup-1vs1 (ONLINE / JOIN ROOM):
        // panel gỗ-xanh có sẵn tiêu đề + "ENTER ROOM CODE", 4 ô nhập mã, nút JOIN ROOM (xanh)
        // và QUICK MATCH (vàng), nút X đỏ góc phải, hai nhân vật xanh/đỏ hai bên.
        // Vẫn giữ Tạo phòng + Xem thử dạng nút phụ nhỏ dưới panel để không mất tính năng.
        const string V1V1 = "popup-1vs1/";

        // 4 ô nhập mã: mỗi ô là sprite input-number với 1 chữ số; một InputField ẩn bắt phím,
        // onValueChanged đổ từng chữ số vào 4 ô. Bấm bất kỳ ô nào cũng focus để gõ.

        Coroutine statusDotsRoutine;

        // Chấm động "Đang tạo phòng." → ".." → "..." trong lúc chờ dịch vụ Unity
        // (tạo phòng mất vài giây do lobby + relay — cho người chơi thấy game còn sống).

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

    public partial class BrickGameController : MonoBehaviour
    {
        // Khớp lưới IN của khung frame-xepgach: 7 cột (đo chắc). Hàng = 17: Height=19 làm khối thấp
        // hơn ô khung, Height=15 làm cao hơn → 17 khớp (ô vuông trong sprite 1165/68≈17). Khác GD.
        const int Width = 7;
        const int Height = 17;
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
        // GD §12.3+§12.5: mỗi đòn tới xếp hàng đợi riêng, trễ warning trước khi trúng để kịp bật Khiên.
        readonly System.Collections.Generic.Queue<PendingAttack> pendingAttacks = new System.Collections.Generic.Queue<PendingAttack>();
        float lastAttackScheduledAt; // giãn cách MinAttackInterval giữa các đòn liên tiếp
        Image attackFlashOverlay;   // viền đỏ nháy toàn màn
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
        // GD v3 – cơ chế ghép cụm tài nguyên (bật qua rules.UseResourceClusters).
        Sprite[] resourceSprites;
        Puzzle.PuzzleBoard puzzleBoard;
        Puzzle.ClusterResolutionSystem clusterResolver;
        Puzzle.ResourceBagService resourceBag;
        Puzzle.ResourceType[] activeResources;
        Puzzle.ResourceType[] nextResources; // tài nguyên đã sinh trước cho mô hình kế (khớp ô Next)
        readonly System.Random puzzleRng = new System.Random();
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
            resourceSprites = RuntimeArt.LoadResourceSprites();
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

        void Update()
        {
            if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight || Screen.safeArea != lastAppliedSafeArea)
                ConfigureResponsiveCamera();

            RefreshSceneHud();

            if (MultiplayerMatch.Active)
            {
                healthSystem.Tick(Time.deltaTime); // GD §13: đếm giờ Active Shield
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
                nextPreviewCells = CreatePiecePreview(nextPreview, new Vector2(0.5f, 0.60f), previewCellSize);
                AddNextPreviewBg(nextPreview);
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

            // GD v3: VẼ LƯỚI Ô CỦA RIÊNG MÌNH — che lưới in mờ của khung để mô hình rơi LUÔN khít
            // ô hiển thị (lưới hiển thị = lưới runtime). Nền navy đậm lấp vùng trong khung; khe hở
            // giữa các ô lộ nền = đường kẻ lưới rõ ràng.
            ClearRuntimeChild(scenePuzzleBoardAnchorRect, "Puzzle Board Cover");

            var gridRoot = new GameObject("Runtime Puzzle Grid", typeof(RectTransform));
            gridRoot.transform.SetParent(scenePuzzleBoardAnchorRect, false);
            var gridRect = gridRoot.GetComponent<RectTransform>();
            scenePuzzleGridRect = gridRect;
            FitScenePuzzleGridToAnchor(); // neo lưới lấp vùng trong khung theo tỉ lệ
            gridRect.SetAsLastSibling();

            // Nền lưới (đường kẻ): navy đậm, phủ kín vùng lưới → che lưới in của khung.
            var gridBg = Ui.Panel(gridRoot.transform, "Puzzle Grid BG", new Color(0.035f, 0.075f, 0.16f, 1f));
            Ui.Stretch(gridBg);
            gridBg.GetComponent<Image>().raycastTarget = false;

            // Ô neo theo TỈ LỆ, có khe hở nhỏ → gaps lộ nền = đường lưới. Ô trống = tile navy nhạt
            // (nhìn rõ từng ô để di chuyển mô hình); ô có tài nguyên = khối lấp đầy.
            const float cellGap = 0.035f;
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
                    rect.anchorMin = new Vector2((x + cellGap) / Width, (y + cellGap) / Height);
                    rect.anchorMax = new Vector2((x + 1f - cellGap) / Width, (y + 1f - cellGap) / Height);
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.localScale = Vector3.one;
                    scenePuzzleSlots[x, y] = rect;
                    scenePuzzleCells[x, y] = cell;
                }
            }
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

        readonly List<Image> tacticalCellImageCache = new List<Image>();
        int monsterNextCellIndex = -1;
        Sprite obstacleBlueSprite, obstacleBushSprite;

        // Sprite chướng ngại vật (chuongngaivat) — xen kẽ khối xanh / bụi xanh lá như thiết kế.

        int hudCachedMoveBank = int.MinValue;
        int hudCachedShield = int.MinValue;
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
            int shieldLayers = tacticalBoard != null ? tacticalBoard.ShieldLayers : 0;
            // Offline: kèm đồng hồ đếm ngược trước lượt tự đi của quái (design §2.7).
            int monsterSecond = -1;
            if (!MultiplayerMatch.Active && tacticalBoard != null && tacticalBoard.Status == TacticalBoardStatus.Running)
                monsterSecond = Mathf.CeilToInt(Mathf.Max(0f, tacticalBoard.MonsterTimer));

            if (sceneMoveText != null && (moveBank != hudCachedMoveBank || monsterSecond != hudCachedMonsterSecond || shieldLayers != hudCachedShield))
            {
                hudCachedMoveBank = moveBank;
                hudCachedMonsterSecond = monsterSecond;
                hudCachedShield = shieldLayers;
                string shieldPart = shieldLayers > 0 ? "   Khiên: " + shieldLayers : "";
                sceneMoveText.text = monsterSecond >= 0
                    ? "Lượt đi: " + moveBank + shieldPart + "   Quái đi sau: " + monsterSecond + "s"
                    : "Lượt đi: " + moveBank + shieldPart;
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

        // Mỗi frame: chỉ nhấp nháy ô quái-sắp-đi, không refresh toàn bàn (rẻ).

        // Quái tự đi (timer) khiến màn thắng/thua — đi cùng nhánh xử lý với khi player đi.

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
                    nextPreviewCells = CreatePiecePreview(nextPreview, new Vector2(0.5f, 0.60f), CalculatePreviewCellSize(sceneNextPreviewRect));
                    AddNextPreviewBg(nextPreview);
                    rebuiltPreview = true;
                }
            }

            if (rebuiltPuzzle)
                RefreshScenePuzzleBoardUi();
            if (rebuiltTactical)
                RefreshTacticalBoardUi();
            if (rebuiltPreview && nextPreviewCells != null && nextPreviewCells.Count > 0 && nextBag.Count > 0)
                RenderPiecePreview(nextPreviewCells, PeekNext(0), true, nextResources);
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

        // Màn hình CHIẾN THẮNG (Image #16): overlay toàn màn hình, đè lên gameplay
        // (nền mờ để thấy bàn cờ), bố cục landscape — tiêu đề trên, 3 sao, phụ đề,
        // cụm nhân vật + bệ ở giữa, 3 nút TRANG CHỦ / CHƠI LẠI / TIẾP ở dưới.

        // Màn hình THẤT BẠI (Image #5): giống màn thắng nhưng buồn — tiêu đề đỏ,
        // 3 sao xám, hiệp sĩ gục ngã, nút TIẾP bị khoá (xám).

        // Rải pháo hoa/confetti (decor-phaohoa-1..7) ngẫu nhiên khắp nền màn thắng.

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

        // Landscape: puzzle board trái (tỉ lệ 2:1 dọc), bàn chiến thuật vuông giữa-phải,
        // cột phụ (TIẾP/Xoay hoặc bàn đối thủ 1v1) sát mép phải.
        Image gameplayBgImage;

        // Nền gameplay landscape mới (bg-gameplay) phủ kín màn hình. Tạo Image riêng ở
        // đáy canvas (không phụ thuộc element dựng sẵn) + ẩn backdrop gỗ world-space cũ.

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

        // Thay nền nút bằng sprite mới (ẩn graphic gỗ cũ + icon con, đặt frame con fill).

        // Thay khung gỗ cũ của bàn cờ / xếp gạch / NEXT bằng khung mới (screen-gameplay).

        // Thêm 1 frame con (first-sibling, fill) làm nền khung cho rect; tắt graphic gỗ cũ
        // của rect (Image/RawImage) nhưng GIỮ text (TMP) nếu có. Tránh xung đột Graphic.

        // Dựng HUD online (design #7) một lần: thanh máu, nhân vật/VS, tấn công/phòng thủ,
        // thanh năng lượng, 3 nút kỹ năng. Vị trí do ApplyOnlineRegionLayout đặt.

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

        // Cập nhật máu/năng lượng/số lá kỹ năng cho HUD online.

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

        // Nền navy che lưới in mờ của khung NEXT (giống bàn chính) — đặt SAU các ô preview để nằm
        // dưới. Idempotent: chỉ tạo 1 lần.
        void AddNextPreviewBg(Transform nextPreview)
        {
            if (nextPreview == null)
                return;

            // Neo nền vào KHUNG NEXT (parent của NextPreview) để lấp đúng vùng lưới in TRONG khung
            // (không tràn ra ngoài). Chừa đỉnh cho chữ "NEXT". Chỉnh 4 số nếu còn hở/che chữ.
            Transform host = nextPreview.parent != null ? nextPreview.parent : nextPreview;
            if (FindChildLoose(host, "Next Grid BG") != null)
                return;

            var bg = Ui.Panel(host, "Next Grid BG", new Color(0.035f, 0.075f, 0.16f, 1f));
            var rt = bg.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.065f, 0.045f);
            rt.anchorMax = new Vector2(0.935f, 0.875f); // sát dưới chữ NEXT (header ở đỉnh khung)
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            bg.GetComponent<Image>().raycastTarget = false;
            // Dưới các ô preview (NextPreview) nhưng trên khung: chèn ngay trước NextPreview.
            bg.transform.SetSiblingIndex(nextPreview.GetSiblingIndex());
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

        Vector2Int SpawnOriginForCurrentPiece()
        {
            if (currentPieceIsSpecial)
                return new Vector2Int(Width / 2, Height - 1);

            int maxLocalY = 0;
            foreach (var cell in shapes[currentType])
                maxLocalY = Mathf.Max(maxLocalY, cell.y);
            return new Vector2Int(Width / 2, Height - 1 - maxLocalY);
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

        // Đặt trạng thái 3 sao trên màn thắng: đủ sao thì sáng, thiếu thì mờ.

        // Điểm thưởng thắng bàn cờ: 300 gốc + 200/sao (3 sao = 900).

        // Hàng sao bằng rich text: sao đạt màu vàng, sao chưa đạt màu nâu mờ.
        // Dùng ★ cho cả hai (font UI không chắc có ☆).

        // Bàn cờ + bàn xếp gạch thu nhỏ của đối thủ, xếp chồng ở cột phải dưới nút Xoay.

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

        // Gửi ảnh chụp bàn của mình cho đối thủ, tối đa 2 lần/giây và chỉ khi thay đổi.

        // Áp hàng rác đối thủ gửi sang: nhấp nháy cảnh báo 1 giây rồi mới chèn rác
        // (hàng rác 1 lỗ, chèn đáy). Chờ lúc "yên" để không phá dở animation.

        // Người chơi bấm 1 kỹ năng (design §7). Tiêu năng lượng, gửi hiệu ứng sang đối thủ.
        // Giãn 0,4s chống bấm dồn.

        static string SkillName(OnlineSkill s) => s == OnlineSkill.LifeDrain ? "Hút máu" : s == OnlineSkill.OverloadBlast ? "Cuồng nộ" : "Thả rác";

        // Gửi trạng thái điểm/hàng kèm máu+năng lượng cho đối thủ (HUD).

        // Áp các đòn tấn công đối thủ gửi tới vào máu mình (design §7.1). Khiên chặn 1 đòn.
        // Peer tự quản máu của chính mình (thay server authority §14 vì kiến trúc P2P).

        // Design §15: trận vượt quá thời gian tối đa → phân định theo máu rồi số hàng.

        // Đối thủ báo kết thúc hoặc rời trận — xử ở đầu Update mỗi frame.

        // Design §15: khi hai bên cùng thua (máu 0 / bảng đầy) trong cùng nhịp,
        // người xóa nhiều hàng hơn thắng; bằng nhau → hòa. outcome: 1 thắng, 0 hòa, -1 thua.

        System.Collections.IEnumerator LeaveMatchAfterDelay(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            if (MultiplayerManager.Instance != null)
                _ = MultiplayerManager.Instance.LeaveAsync();
            else
                MultiplayerMatch.Reset();
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

}
