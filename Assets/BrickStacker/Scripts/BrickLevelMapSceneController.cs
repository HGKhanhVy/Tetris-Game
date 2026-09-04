using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BrickStacker
{
    // Màn chọn level — lưới 5 cột theo thiết kế screen-level (asset Assets-v3.0/screen-level).
    // Tile xanh = đã qua, cam = level hiện tại, xám = khóa. Dựng lại toàn bộ bằng code,
    // bỏ hệ path cuộn cũ.
    public class BrickLevelMapSceneController : MonoBehaviour
    {
        const int Columns = 5;
        int LevelCount => LevelProgress.MaxLevels;

        Canvas canvas;
        Text coinText;

        void Awake()
        {
            GameAudio.PlayMusic(GameAudio.MusicMenu);   // cùng bài với menu -> đi qua lại không bị cắt nhạc
            BuildCameraBackdrop();
            BuildUi();
        }

        void OnEnable()
        {
            CloudSaveSync.ProgressPulled += OnCloudProgressPulled;
        }

        void OnDisable()
        {
            CloudSaveSync.ProgressPulled -= OnCloudProgressPulled;
        }

        void OnCloudProgressPulled()
        {
            // Tiến trình mới từ cloud → dựng lại lưới.
            if (canvas != null)
                Destroy(canvas.gameObject);
            BuildUi();
        }

        void BuildCameraBackdrop()
        {
            var cam = Camera.main;
            if (cam == null)
                cam = FindAnyObjectByType<Camera>();
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.11f, 0.41f, 0.96f, 1f);
            }
        }

        void BuildUi()
        {
            canvas = Ui.CreateCanvas("Level Select Canvas");
            canvas.sortingOrder = 100; // phủ lên nội dung scene dựng sẵn (nếu còn)

            // Nền phủ kín màn hình.
            var bgSpr = RuntimeArt.LoadV3Sprite("screen-level/bg-level.png");
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
                var arf = bg.AddComponent<AspectRatioFitter>();
                arf.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                arf.aspectRatio = (float)bgSpr.texture.width / bgSpr.texture.height;
            }

            var root = Ui.Panel(canvas.transform, "Root", new Color(0, 0, 0, 0));
            Ui.Stretch(root);
            root.AddComponent<SafeAreaFitter>();

            BuildBackButton(root.transform);
            BuildCoinPanel(root.transform);
            BuildTitle(root.transform);
            BuildGrid(root.transform);
        }

        void BuildBackButton(Transform parent)
        {
            var btn = Ui.Button(parent, "", RuntimeArt.LoadUiFont(), 1, () =>
            {
                RuntimeArt.PlayUiSwitchSound();
                SceneManager.LoadScene("BrickMenu");
            });
            var rt = btn.GetComponent<RectTransform>();
            // Neo góc TRÊN-TRÁI của vùng safe area (root đã fit safe area) rồi lùi vào bằng pixel
            // — lề đều 28/26px như panel xu, không dính mép/notch dù màn hình nào.
            const float sizePx = 96f, marginX = 52f, marginY = 30f;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(sizePx, sizePx);
            rt.anchoredPosition = new Vector2(marginX + sizePx * 0.5f, -(marginY + sizePx * 0.5f));

            var img = btn.GetComponent<Image>();
            var spr = RuntimeArt.LoadV3SubSprite("screen-level/btn-back.png", new Rect(0.345f, 0.300f, 0.309f, 0.435f));
            if (spr != null) { img.sprite = spr; img.type = Image.Type.Simple; img.preserveAspect = true; img.color = Color.white; }
            else img.color = new Color(0.2f, 0.5f, 0.95f);
            // Mũi tên gốc chỉ sang PHẢI → lật ngang để chỉ sang TRÁI (quay lại).
            rt.localScale = new Vector3(-1f, 1f, 1f);

            AddPress(btn.gameObject, 0.90f);
        }

        void BuildCoinPanel(Transform parent)
        {
            var panel = Ui.Panel(parent, "Coin Panel", Color.white);
            var img = panel.GetComponent<Image>();
            var spr = RuntimeArt.LoadV3SubSprite("screen-level/panel-coin.png", new Rect(0.118f, 0.438f, 0.768f, 0.238f));
            if (spr != null) { img.sprite = spr; img.type = Image.Type.Simple; img.preserveAspect = true; img.color = Color.white; }
            else img.color = new Color(0.1f, 0.2f, 0.5f);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(1f, 1f); // neo góc phải-trên, chừa lề để nút + không bị cắt
            float wPx = 300f;
            rt.sizeDelta = new Vector2(wPx, wPx / 4.84f);
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-28f, -26f);

            // Số xu nằm trên thanh xanh (crown coin bên trái, dấu + bên phải đã có sẵn trong ảnh).
            coinText = Ui.Text(panel.transform, FormatCoins(LevelProgress.Coins), RuntimeArt.LoadMenuButtonFont(), 30, new Color(1f, 0.98f, 0.9f), TextAnchor.MiddleCenter);
            coinText.fontStyle = FontStyle.Bold;
            coinText.raycastTarget = false;
            var ctRt = coinText.rectTransform;
            ctRt.anchorMin = new Vector2(0.20f, 0.18f);
            ctRt.anchorMax = new Vector2(0.82f, 0.82f);
            ctRt.offsetMin = ctRt.offsetMax = Vector2.zero;
        }

        void BuildTitle(Transform parent)
        {
            var spr = RuntimeArt.LoadV3SubSprite("screen-level/title.png", new Rect(0.167f, 0.474f, 0.661f, 0.186f));
            if (spr == null)
                return;
            var title = Ui.Panel(parent, "Title", Color.white);
            var img = title.GetComponent<Image>();
            img.sprite = spr; img.type = Image.Type.Simple; img.preserveAspect = true; img.raycastTarget = false;
            var rt = title.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.885f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            float wPx = 320f;
            rt.sizeDelta = new Vector2(wPx, wPx / 5.35f);
        }

        void BuildGrid(Transform parent)
        {
            int count = LevelCount;
            int rows = Mathf.CeilToInt(count / (float)Columns);
            const float tile = 116f, gapX = 24f, gapY = 18f;
            float gridW = Columns * tile + (Columns - 1) * gapX;
            float gridH = rows * tile + (rows - 1) * gapY;

            var container = new GameObject("Grid", typeof(RectTransform));
            container.transform.SetParent(parent, false);
            var crt = container.GetComponent<RectTransform>();
            // Hạ lưới xuống để chừa khoảng cách với banner CHỌN LEVEL phía trên.
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.415f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(gridW, gridH);

            int unlocked = LevelProgress.CurrentUnlockedLevel;
            for (int i = 0; i < count; i++)
            {
                int level = i + 1;
                int col = i % Columns;
                int row = i / Columns;
                float x = -gridW * 0.5f + tile * 0.5f + col * (tile + gapX);
                float y = gridH * 0.5f - tile * 0.5f - row * (tile + gapY);
                BuildTile(container.transform, level, new Vector2(x, y), tile, unlocked);
            }
        }

        void BuildTile(Transform parent, int level, Vector2 pos, float size, int unlocked)
        {
            bool isLocked = level > unlocked;
            bool isCurrent = level == unlocked;
            int stars = Mathf.Clamp(LevelProgress.StarsForLevel(level), 0, 3);

            string asset; Rect crop;
            if (isLocked) { asset = "screen-level/level-inactive.png"; crop = new Rect(0.307f, 0.226f, 0.382f, 0.586f); }
            else if (isCurrent) { asset = "screen-level/level-active-2.png"; crop = new Rect(0.279f, 0.185f, 0.44f, 0.64f); }
            else { asset = "screen-level/level-active-1.png"; crop = new Rect(0.279f, 0.185f, 0.44f, 0.64f); }

            var btn = Ui.Button(parent, "", RuntimeArt.LoadUiFont(), 1, () =>
            {
                if (isLocked) { RuntimeArt.PlayUiSwitchSound(); return; }
                RuntimeArt.PlayUiSwitchSound();
                GameSession.SelectedLevel = level;
                GameSession.JourneyLevel = level;
                SceneManager.LoadScene("BrickGame");
            });
            var rt = btn.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = pos;

            var img = btn.GetComponent<Image>();
            var spr = RuntimeArt.LoadV3SubSprite(asset, crop);
            if (spr != null) { img.sprite = spr; img.type = Image.Type.Simple; img.preserveAspect = true; img.color = Color.white; }
            else img.color = isLocked ? new Color(0.5f, 0.5f, 0.5f) : new Color(0.2f, 0.6f, 0.95f);
            AddPress(btn.gameObject, isLocked ? 0.97f : 0.92f);

            // Số level — nửa trên tile (locked: ổ khóa đã có sẵn ở giữa/dưới ảnh).
            var num = Ui.Text(btn.transform, level.ToString(), RuntimeArt.LoadDisplayFont(), 50,
                isLocked ? new Color(0.93f, 0.95f, 0.98f) : Color.white, TextAnchor.MiddleCenter);
            num.fontStyle = FontStyle.Bold;
            num.raycastTarget = false;
            // Ô có sao: hạ số xuống cho ngang tầm ô khóa (trước đây bị đẩy cao vì chừa chỗ sao).
            var nr = num.rectTransform;
            nr.anchorMin = new Vector2(0.08f, isLocked ? 0.44f : 0.46f);
            nr.anchorMax = new Vector2(0.92f, isLocked ? 0.94f : 0.92f);
            nr.offsetMin = nr.offsetMax = Vector2.zero;
            AddTextEdge(num, isLocked ? new Color(0.3f, 0.32f, 0.34f) : new Color(0.05f, 0.25f, 0.5f));

            if (!isLocked)
                BuildStars(btn.transform, stars);
        }

        void BuildStars(Transform tile, int stars)
        {
            // Sao gọn trong ô, cân giữa, sao giữa nhô cao — cụm sao hẹp hơn bề ngang tile.
            const float starSize = 40f, starStep = 28f;
            var row = new GameObject("Stars", typeof(RectTransform));
            row.transform.SetParent(tile, false);
            var rrt = row.GetComponent<RectTransform>();
            rrt.anchorMin = rrt.anchorMax = new Vector2(0.5f, 0.235f);
            rrt.pivot = new Vector2(0.5f, 0.5f);
            rrt.sizeDelta = new Vector2(starStep * 2 + starSize, starSize);

            for (int i = 0; i < 3; i++)
            {
                bool filled = i < stars;
                var asset = filled ? "screen-level/start-active.png" : "screen-level/start-inactive.png";
                var crop = filled ? new Rect(0.300f, 0.298f, 0.395f, 0.537f) : new Rect(0.329f, 0.296f, 0.342f, 0.481f);
                var star = Ui.Panel(row.transform, "Star" + i, Color.white);
                var img = star.GetComponent<Image>();
                var spr = RuntimeArt.LoadV3SubSprite(asset, crop);
                if (spr != null) { img.sprite = spr; img.type = Image.Type.Simple; img.preserveAspect = true; img.color = Color.white; }
                else img.color = filled ? new Color(1f, 0.84f, 0.2f) : new Color(0.35f, 0.35f, 0.38f);
                img.raycastTarget = false;
                var srt = star.GetComponent<RectTransform>();
                srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.5f);
                srt.pivot = new Vector2(0.5f, 0.5f);
                srt.sizeDelta = new Vector2(starSize, starSize);
                // Sao giữa nhô cao hơn + vẽ đè lên 2 sao bên (SetAsLastSibling).
                srt.anchoredPosition = new Vector2((i - 1) * starStep, i == 1 ? 5f : 0f);
                if (i == 1) star.transform.SetAsLastSibling();
            }
        }

        static string FormatCoins(int coins)
        {
            // Định dạng nghìn kiểu Việt Nam: 1250 -> "1.250".
            return coins.ToString("#,0", System.Globalization.CultureInfo.GetCultureInfo("de-DE"));
        }

        void AddPress(GameObject target, float pressedScale)
        {
            var f = target.GetComponent<PressScaleFeedback>() ?? target.AddComponent<PressScaleFeedback>();
            f.PressedScale = pressedScale;
        }

        void AddTextEdge(Text text, Color color)
        {
            var outline = text.gameObject.GetComponent<Outline>() ?? text.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(2.4f, -2.4f);
        }

        void EnsureEventSystem()
        {
            if (EventSystem.current != null || FindAnyObjectByType<EventSystem>() != null)
                return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
