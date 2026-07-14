using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace BrickStacker
{
    public class BrickLevelMapSceneController : MonoBehaviour
    {
        const int MaxSceneLevels = 10;
        static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();
        static Sprite currentLevelHaloSprite;
        static Sprite currentLevelRingSprite;
        static Sprite currentLevelSparkleSprite;
        static Sprite pathDotSprite;

        Canvas canvas;
        RectTransform rootRect;
        RectTransform backgroundRect;
        RectTransform boardRect;
        RectTransform titleRect;
        RectTransform coinRect;
        ScrollRect scrollRect;
        RectTransform contentRect;
        RectTransform backButtonRect;
        readonly Dictionary<Transform, Vector3> nodeBaseScales = new Dictionary<Transform, Vector3>();
        Vector2 lastScreenSize;
        Rect lastSafeArea;

        void Awake()
        {
            rootRect = transform as RectTransform;
            canvas = GetComponentInParent<Canvas>();
            ConfigureCameraBackdrop();
            ConfigureCanvas();
            EnsureEventSystem();
            SetupScrollView();
            BuildBackButton();
            CacheLayoutRects();
            BindLevelNodes();
            BuildPathTrail();
            RefreshHeaderText();
            ApplyResponsiveLayout(true);
        }

        void OnEnable()
        {
            CloudSaveSync.ProgressPulled += OnCloudProgressPulled;
        }

        void OnDisable()
        {
            CloudSaveSync.ProgressPulled -= OnCloudProgressPulled;
        }

        // Cloud Save kéo về tiến trình mới hơn khi đang ở màn chọn level → vẽ lại node + header.
        void OnCloudProgressPulled()
        {
            BindLevelNodes();
            BuildPathTrail();
            RefreshHeaderText();
        }

        void LateUpdate()
        {
            ApplyResponsiveLayout(false);
        }

        void ConfigureCanvas()
        {
            if (canvas == null)
                canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
                return;

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

            // Set matchWidthOrHeight immediately so Canvas.ForceUpdateCanvases() in ApplyScrollLayout
            // uses the correct canvas dimensions. ResponsiveCanvasScaler only fixes this in LateUpdate,
            // which is too late for the Awake layout pass.
            float screenAspect = Screen.height > 0 ? Screen.width / (float)Screen.height : 0f;
            scaler.matchWidthOrHeight = screenAspect >= (scaler.referenceResolution.x / scaler.referenceResolution.y) ? 1f : 0f;

            if (canvas.GetComponent<ResponsiveCanvasScaler>() == null)
                canvas.gameObject.AddComponent<ResponsiveCanvasScaler>();

            if (canvas.GetComponent<GraphicRaycaster>() == null)
                canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        void ConfigureCameraBackdrop()
        {
            Camera cam = Camera.main;
            if (cam == null)
                cam = FindAnyObjectByType<Camera>();
            if (cam == null)
                return;

            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.13f, 0.065f, 0.025f, 1f);
        }

        void CacheLayoutRects()
        {
            backgroundRect = GetRect("Background");
            boardRect = GetRect("Board");
            titleRect = GetRect("Title");
            coinRect = GetRect("SumLine");

            if (backgroundRect != null && canvas != null)
            {
                backgroundRect.SetParent(canvas.transform, false);
                backgroundRect.anchorMin = Vector2.zero;
                backgroundRect.anchorMax = Vector2.one;
                backgroundRect.offsetMin = Vector2.zero;
                backgroundRect.offsetMax = Vector2.zero;
                backgroundRect.pivot = new Vector2(0.5f, 0.5f);
                backgroundRect.SetAsFirstSibling();
            }
        }

        void EnsureEventSystem()
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

        void ApplyResponsiveLayout(bool force)
        {
            if (rootRect == null)
                return;

            Vector2 screenSize = new Vector2(Screen.width, Screen.height);
            Rect safeArea = Screen.safeArea;
            if (!force && screenSize == lastScreenSize && safeArea == lastSafeArea)
                return;

            lastScreenSize = screenSize;
            lastSafeArea = safeArea;

            if (backgroundRect != null)
                backgroundRect.SetAsFirstSibling();

            ApplyScrollLayout();

            // Re-anchor the scroll position whenever the layout is recomputed —
            // a stale normalized position from the previous screen size can leave
            // the board pushed off-screen after a resize/rotation.
            ScrollToCurrentLevel();

            SyncCurrentLevelGlowToNode();

            LayoutBackButton();
        }

        void SetupScrollView()
        {
            if (canvas == null) return;

            var scrollGO = new GameObject("LevelScrollView", typeof(RectTransform));
            scrollGO.transform.SetParent(canvas.transform, false);
            var scrollRT = scrollGO.GetComponent<RectTransform>();
            scrollRT.anchorMin = Vector2.zero;
            scrollRT.anchorMax = Vector2.one;
            scrollRT.offsetMin = Vector2.zero;
            scrollRT.offsetMax = Vector2.zero;

            var viewportGO = new GameObject("Viewport", typeof(RectTransform));
            viewportGO.transform.SetParent(scrollGO.transform, false);
            var viewportRT = viewportGO.GetComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.offsetMin = Vector2.zero;
            viewportRT.offsetMax = Vector2.zero;
            viewportGO.AddComponent<RectMask2D>();

            var contentGO = new GameObject("ScrollContent", typeof(RectTransform));
            contentGO.transform.SetParent(viewportGO.transform, false);
            contentRect = contentGO.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 0f);
            contentRect.anchorMax = new Vector2(1f, 0f);
            contentRect.pivot = new Vector2(0.5f, 0f);
            contentRect.sizeDelta = new Vector2(0f, 1920f);
            contentRect.anchoredPosition = Vector2.zero;

            rootRect.SetParent(contentGO.transform, false);
            rootRect.anchorMin = new Vector2(0.5f, 0f);
            rootRect.anchorMax = new Vector2(0.5f, 0f);
            rootRect.pivot = new Vector2(0.5f, 0f);
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.localScale = Vector3.one;

            scrollRect = scrollGO.AddComponent<ScrollRect>();
            scrollRect.viewport = viewportRT;
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.elasticity = 0.1f;
            scrollRect.inertia = true;
            scrollRect.decelerationRate = 0.135f;
            scrollRect.scrollSensitivity = 50f;
        }

        // Nút quay lại menu — neo góc trái dưới canvas (không cuộn theo bản đồ),
        // vị trí né safe area trong ApplyResponsiveLayout.
        void BuildBackButton()
        {
            if (canvas == null)
                return;

            var font = RuntimeArt.LoadUiFont();
            var go = new GameObject("BackToMenuButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(canvas.transform, false);
            backButtonRect = go.GetComponent<RectTransform>();
            backButtonRect.anchorMin = Vector2.zero;
            backButtonRect.anchorMax = Vector2.zero;
            backButtonRect.pivot = Vector2.zero;
            backButtonRect.sizeDelta = new Vector2(252f, 92f);
            backButtonRect.anchoredPosition = new Vector2(24f, 20f);

            var image = go.GetComponent<Image>();
            image.sprite = RuntimeArt.CreateWoodPanelSprite();
            image.type = Image.Type.Sliced;
            image.color = new Color(0.35f, 0.18f, 0.07f, 0.96f);

            var label = Ui.Text(go.transform, "‹  Trang chủ", font, 32, new Color(1f, 0.86f, 0.60f), TextAnchor.MiddleCenter);
            var labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            label.raycastTarget = false;

            var button = go.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = image;
            button.onClick.AddListener(() =>
            {
                RuntimeArt.PlayUiSwitchSound();
                SceneManager.LoadScene("BrickMenu");
            });
            go.AddComponent<PressScaleFeedback>().PressedScale = 0.93f;
        }

        void LayoutBackButton()
        {
            if (backButtonRect == null || canvas == null)
                return;

            var canvasRT = canvas.GetComponent<RectTransform>();
            float sw = Mathf.Max(1f, Screen.width);
            float sh = Mathf.Max(1f, Screen.height);
            Rect safe = Ui.SafeArea();
            float left = Mathf.Clamp(safe.xMin, 0f, sw) / sw * canvasRT.rect.width;
            float bottom = Mathf.Clamp(safe.yMin, 0f, sh) / sh * canvasRT.rect.height;
            backButtonRect.anchoredPosition = new Vector2(left + 24f, bottom + 20f);
            backButtonRect.SetAsLastSibling();
        }

        void ApplyScrollLayout()
        {
            if (rootRect == null || canvas == null || contentRect == null) return;

            Canvas.ForceUpdateCanvases();
            rootRect.localScale = Vector3.one;

            Bounds bounds = CalculateContentBounds(rootRect);
            if (bounds.size.x <= 1f || bounds.size.y <= 1f) return;

            float sw = Mathf.Max(1f, Screen.width);
            Rect safe = Ui.SafeArea();
            float safeL = Mathf.Clamp(safe.xMin, 0f, sw);
            float safeR = Mathf.Clamp(safe.xMax, 0f, sw);
            var canvasRT = canvas.GetComponent<RectTransform>();
            float availWidth = canvasRT.rect.width * (safeR - safeL) / sw - 24f;

            float scale = Mathf.Clamp(availWidth / bounds.size.x, 0.1f, 3f);
            rootRect.localScale = Vector3.one * scale;

            // Visual gap (canvas units) between content edge and safe-area boundary.
            // ~10pt below notch at top, ~8pt above home-indicator at bottom.
            const float topPadVisual = 28f;
            const float botPadVisual = 22f;

            // Add safe-area vertical insets so the board clears the notch/home-indicator
            // on real devices. In the Editor (no notch) these are zero.
            float sh = Mathf.Max(1f, Screen.height);
            float viewportH = scrollRect != null ? scrollRect.viewport.rect.height : 0f;
            float safeTopCanvas  = Mathf.Clamp(sh - safe.yMax, 0f, sh) / sh * viewportH;
            float safeBotCanvas  = Mathf.Clamp(safe.yMin,      0f, sh) / sh * viewportH;

            float topPad = topPadVisual + safeTopCanvas;
            float botPad = botPadVisual + safeBotCanvas;

            float contentHeight = Mathf.Max(bounds.size.y * scale + topPad + botPad, viewportH);
            contentRect.sizeDelta = new Vector2(0f, contentHeight);

            // botPad shifts board bottom away from content y=0
            rootRect.anchoredPosition = new Vector2(0f, -bounds.min.y * scale + botPad);
        }

        void ScrollToCurrentLevel()
        {
            if (scrollRect == null || contentRect == null) return;

            Canvas.ForceUpdateCanvases();

            float contentHeight = contentRect.sizeDelta.y;
            float viewportHeight = scrollRect.viewport.rect.height;
            float scrollRange = contentHeight - viewportHeight;

            // normalizedPos=1 shows content top; topPad in ApplyScrollLayout ensures
            // ~60 canvas units of clear space above the board frame on all screen sizes.
            scrollRect.verticalNormalizedPosition = scrollRange > 0f ? 1f : 0.5f;
        }

        void FitRootToScreen(RectTransform rect, float padding, float maxScale)
        {
            if (rect == null)
                return;

            var parent = rect.parent as RectTransform;
            if (parent == null)
                return;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;

            Bounds bounds = CalculateContentBounds(rect);
            if (bounds.size.x <= 1f || bounds.size.y <= 1f)
                return;

            float sw = Mathf.Max(1f, Screen.width);
            float sh = Mathf.Max(1f, Screen.height);
            Rect safe = Ui.SafeArea();
            float safeL = Mathf.Clamp(safe.xMin, 0f, sw);
            float safeB = Mathf.Clamp(safe.yMin, 0f, sh);
            float safeR = Mathf.Clamp(safe.xMax, 0f, sw);
            float safeT = Mathf.Clamp(safe.yMax, 0f, sh);
            Rect parentRect = parent.rect;
            float safeWidth  = parentRect.width  * (safeR - safeL) / sw;
            float safeHeight = parentRect.height * (safeT - safeB) / sh;
            float safeCenterX = parentRect.xMin + parentRect.width  * (safeL + safeR) * 0.5f / sw;
            float safeCenterY = parentRect.yMin + parentRect.height * (safeB + safeT) * 0.5f / sh;
            float scale = Mathf.Min((safeWidth - padding * 2f) / bounds.size.x, (safeHeight - padding * 2f) / bounds.size.y);
            scale = Mathf.Clamp(scale, 0.2f, maxScale);

            rect.localScale = Vector3.one * scale;
            rect.anchoredPosition = new Vector2(safeCenterX, safeCenterY) - (Vector2)bounds.center * scale;
        }

        Bounds CalculateContentBounds(RectTransform root)
        {
            var children = root.GetComponentsInChildren<RectTransform>(true);
            var corners = new Vector3[4];
            bool hasBounds = false;
            Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
            for (int i = 0; i < children.Length; i++)
            {
                var child = children[i];
                if (child == null || child == root || !child.gameObject.activeInHierarchy || child == backgroundRect)
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

        void StretchRootToScreen(RectTransform rect)
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

        RectTransform GetRect(string targetName)
        {
            var child = FindChildLoose(transform, targetName);
            return child != null ? child.GetComponent<RectTransform>() : null;
        }

        void ApplyRect(RectTransform rect, Vector2 min, Vector2 max)
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

        void LayoutLevelNodes(float boardLeft, float boardRight, float boardBottom, float boardTop)
        {
            Vector2[] positions =
            {
                new Vector2(0.30f, 0.10f),
                new Vector2(0.66f, 0.18f),
                new Vector2(0.34f, 0.31f),
                new Vector2(0.63f, 0.42f),
                new Vector2(0.42f, 0.54f),
                new Vector2(0.58f, 0.64f),
                new Vector2(0.38f, 0.74f),
                new Vector2(0.68f, 0.82f),
                new Vector2(0.46f, 0.90f),
                new Vector2(0.28f, 0.965f)
            };

            float boardWidth = boardRight - boardLeft;
            float boardHeight = boardTop - boardBottom;
            for (int i = 0; i < positions.Length; i++)
            {
                var node = FindChildLoose(transform, "Level" + (i + 1));
                var rect = node != null ? node.GetComponent<RectTransform>() : null;
                if (rect == null)
                    continue;

                Vector2 p = positions[i];
                float x = boardLeft + boardWidth * p.x;
                float y = boardBottom + boardHeight * p.y;
                float size = Mathf.Clamp(boardWidth * 0.145f, 0.070f, 0.105f);
                rect.anchorMin = new Vector2(x, y);
                rect.anchorMax = new Vector2(x, y);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(88f, 88f);
                rect.localScale = Vector3.one * (size * 10.0f);
            }
        }

        void SyncCurrentLevelGlowToNode()
        {
            int currentLevel = Mathf.Clamp(LevelProgress.CurrentUnlockedLevel, 1, MaxSceneLevels);
            var node = FindChildLoose(transform, "Level" + currentLevel) as RectTransform;
            var glow = FindChildLoose(transform, "CurrentLevelGlow") as RectTransform;
            if (node == null || glow == null)
                return;

            glow.anchorMin = node.anchorMin;
            glow.anchorMax = node.anchorMax;
            glow.pivot = node.pivot;
            glow.anchoredPosition = node.anchoredPosition;
            glow.sizeDelta = node.sizeDelta;
            glow.localRotation = node.localRotation;
            glow.localScale = node.localScale;
        }

        void BindLevelNodes()
        {
            int unlockedLevel = Mathf.Clamp(LevelProgress.CurrentUnlockedLevel, 1, MaxSceneLevels);
            ClearCurrentLevelGlowObjects();
            for (int level = 1; level <= MaxSceneLevels; level++)
            {
                var node = FindChildLoose(transform, "Level" + level);
                if (node == null)
                    continue;

                int levelNumber = level;
                int stars = Mathf.Clamp(LevelProgress.StarsForLevel(level), 0, 3);
                bool unlocked = level <= unlockedLevel;

                var nodeImage = node.GetComponent<Image>();
                if (nodeImage != null)
                {
                    nodeImage.sprite = ResolveNodeSprite(unlocked, stars);
                    nodeImage.preserveAspect = true;
                    nodeImage.raycastTarget = true;
                    nodeImage.color = Color.white;
                    NormalizeNodeVisualScale(node as RectTransform, nodeImage.sprite);
                }

                var digit = FindChildLoose(node, "SoLevel");
                var digitImage = digit != null ? digit.GetComponent<Image>() : null;
                if (digitImage != null)
                {
                    digitImage.sprite = ResolveDigitSprite(level, unlocked);
                    digitImage.preserveAspect = true;
                    digitImage.raycastTarget = false;
                    digitImage.color = Color.white;
                    ApplyDigitLayout(digitImage.rectTransform, level, unlocked, stars);
                }

                var button = node.GetComponent<Button>();
                if (button == null)
                    button = node.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.targetGraphic = nodeImage;
                button.onClick.RemoveAllListeners();
                button.interactable = unlocked;
                button.onClick.AddListener(() => StartLevel(levelNumber));

                var feedback = node.GetComponent<PressScaleFeedback>();
                if (feedback == null)
                    feedback = node.gameObject.AddComponent<PressScaleFeedback>();
                feedback.PressedScale = 0.93f;

                var pulse = node.GetComponent<LevelNodePulse>();
                if (level == unlockedLevel && unlocked && stars == 0)
                {
                    if (pulse == null)
                        pulse = node.gameObject.AddComponent<LevelNodePulse>();
                    pulse.Amount = 0.028f;
                    pulse.Speed = 2.0f;
                }
                else if (pulse != null)
                {
                    pulse.enabled = false;
                }

                if (level == unlockedLevel && unlocked)
                    AddCurrentLevelGlow(node, stars);
            }
        }

        // Các sprite node cao thấp khác nhau (node khóa cao hơn vì ổ khóa treo dưới) —
        // preserveAspect trong rect vuông làm bề ngang node khóa hụt ~7% so với node
        // thường. Bù lại scale để mọi ô vẽ cùng bề ngang, lấy sprite hoàn thành làm mốc.
        void NormalizeNodeVisualScale(RectTransform rect, Sprite sprite)
        {
            if (rect == null || sprite == null || sprite.rect.height <= 1f)
                return;

            if (!nodeBaseScales.TryGetValue(rect, out var baseScale))
            {
                baseScale = rect.localScale;
                nodeBaseScales[rect] = baseScale;
            }

            const float referenceRatio = 253f / 288f; // level_glow_node_completed
            float ratio = sprite.rect.width / sprite.rect.height;
            rect.localScale = baseScale * (referenceRatio / Mathf.Max(0.01f, ratio));
        }

        // Đường mòn nối các node vẽ runtime — thay cho các sprite "line" đặt tay trong
        // scene (ẩn đi, không xóa). Mỗi đoạn là chuỗi chấm dọc theo bezier hơi cong,
        // đoạn dẫn tới level đã mở màu vàng sáng, đoạn còn khóa màu xỉn. Thêm level
        // mới chỉ cần thêm node — đường tự nối theo.
        void BuildPathTrail()
        {
            var old = FindChildLoose(transform, "RuntimePathTrail");
            if (old != null)
                Destroy(old.gameObject);

            foreach (var img in GetComponentsInChildren<Image>(true))
                if (img.name.StartsWith("line"))
                    img.gameObject.SetActive(false);

            var trailGo = new GameObject("RuntimePathTrail", typeof(RectTransform));
            trailGo.transform.SetParent(transform, false);
            var sumLine = FindChildLoose(transform, "SumLine");
            trailGo.transform.SetSiblingIndex(sumLine != null ? sumLine.GetSiblingIndex() + 1 : 1);
            var trailRect = trailGo.GetComponent<RectTransform>();
            trailRect.anchorMin = new Vector2(0.5f, 0.5f);
            trailRect.anchorMax = new Vector2(0.5f, 0.5f);
            trailRect.pivot = new Vector2(0.5f, 0.5f);
            trailRect.anchoredPosition = Vector2.zero;
            trailRect.sizeDelta = Vector2.zero;

            int unlockedLevel = LevelProgress.CurrentUnlockedLevel;
            for (int i = 1; i < MaxSceneLevels; i++)
            {
                var a = FindChildLoose(transform, "Level" + i) as RectTransform;
                var b = FindChildLoose(transform, "Level" + (i + 1)) as RectTransform;
                if (a == null || b == null)
                    continue;

                Vector2 p0 = trailRect.InverseTransformPoint(a.position);
                Vector2 p1 = trailRect.InverseTransformPoint(b.position);
                DrawDottedSegment(trailRect, p0, p1, i, i + 1 <= unlockedLevel);
            }
        }

        void DrawDottedSegment(RectTransform parent, Vector2 p0, Vector2 p1, int index, bool reached)
        {
            Vector2 dir = p1 - p0;
            float len = dir.magnitude;
            if (len < 1f)
                return;

            Vector2 mid = (p0 + p1) * 0.5f;
            Vector2 perp = new Vector2(-dir.y, dir.x) / len;
            float bulge = (index % 2 == 0 ? 1f : -1f) * Mathf.Min(64f, len * 0.18f);
            Vector2 ctrl = mid + perp * bulge;

            Color color = reached
                ? new Color(1f, 0.80f, 0.38f, 0.95f)
                : new Color(0.60f, 0.48f, 0.34f, 0.72f);

            int dots = Mathf.Max(3, Mathf.RoundToInt(len / 46f));
            for (int d = 0; d <= dots; d++)
            {
                // chừa mép hai đầu để chấm không chui vào dưới node
                float t = Mathf.Lerp(0.16f, 0.84f, d / (float)dots);
                Vector2 pos = QuadraticBezier(p0, ctrl, p1, t);

                var dot = new GameObject("dot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                dot.transform.SetParent(parent, false);
                var rect = dot.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = pos;
                float size = d % 2 == 0 ? 17f : 12f;
                rect.sizeDelta = new Vector2(size, size);

                var image = dot.GetComponent<Image>();
                image.sprite = CreatePathDotSprite();
                image.color = color;
                image.raycastTarget = false;
            }
        }

        static Vector2 QuadraticBezier(Vector2 p0, Vector2 p1, Vector2 p2, float t)
        {
            float u = 1f - t;
            return u * u * p0 + 2f * u * t * p1 + t * t * p2;
        }

        static Sprite CreatePathDotSprite()
        {
            if (pathDotSprite != null)
                return pathDotSprite;

            int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            float center = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center) / center;
                    float dy = (y - center) / center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    // lõi đặc, mép mềm 15% — chấm tròn gọn thay vì quầng mờ
                    float alpha = Mathf.Clamp01((0.92f - dist) / 0.15f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply(false, true);
            pathDotSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            return pathDotSprite;
        }

        void ClearCurrentLevelGlowObjects()
        {
            var glows = new List<GameObject>();
            var children = transform.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null && children[i].name == "CurrentLevelGlow")
                    glows.Add(children[i].gameObject);
            }

            for (int i = 0; i < glows.Count; i++)
                Destroy(glows[i]);
        }

        void AddCurrentLevelGlow(Transform node, int stars)
        {
            if (node == null || node.parent == null)
                return;

            var nodeRect = node as RectTransform;
            if (nodeRect == null)
                return;

            var glowObject = new GameObject("CurrentLevelGlow", typeof(RectTransform));
            glowObject.transform.SetParent(node.parent, false);
            glowObject.transform.SetSiblingIndex(Mathf.Max(0, node.GetSiblingIndex()));

            var glowRect = glowObject.GetComponent<RectTransform>();
            glowRect.anchorMin = nodeRect.anchorMin;
            glowRect.anchorMax = nodeRect.anchorMax;
            glowRect.pivot = nodeRect.pivot;
            glowRect.anchoredPosition = nodeRect.anchoredPosition;
            glowRect.sizeDelta = nodeRect.sizeDelta;
            glowRect.localRotation = nodeRect.localRotation;
            glowRect.localScale = nodeRect.localScale;

            Vector2 nodeSize = GetRectSize(nodeRect);
            float baseSize = Mathf.Max(nodeSize.x, nodeSize.y, 80f);

            AddGlowLayer(glowObject.transform, "Outer Aura", CreateCurrentLevelHaloSprite(), new Vector2(baseSize + 122f, baseSize + 122f), new Color(1f, 0.46f, 0.03f, 0.34f), 0.055f, 1.65f, 0f);
            AddGlowLayer(glowObject.transform, "Warm Aura", CreateCurrentLevelHaloSprite(), new Vector2(baseSize + 82f, baseSize + 82f), new Color(1f, 0.62f, 0.08f, 0.48f), 0.045f, 2.05f, 0.7f);
            AddGlowLayer(glowObject.transform, "Inner Glow", CreateCurrentLevelHaloSprite(), new Vector2(baseSize + 44f, baseSize + 44f), new Color(1f, 0.82f, 0.22f, 0.36f), 0.025f, 2.45f, 1.3f);
        }

        Vector2 GetRectSize(RectTransform rect)
        {
            if (rect == null)
                return new Vector2(100f, 100f);

            Vector2 size = rect.rect.size;
            if (size.x <= 1f || size.y <= 1f)
                size = rect.sizeDelta;
            if (size.x <= 1f || size.y <= 1f)
                size = new Vector2(100f, 100f);
            return size;
        }

        void AddGlowLayer(Transform parent, string name, Sprite sprite, Vector2 size, Color color, float amount, float speed, float phase)
        {
            var layer = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            layer.transform.SetParent(parent, false);
            var rect = layer.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;

            var image = layer.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.color = color;

            var pulse = layer.AddComponent<LevelMapGlowPulse>();
            pulse.Amount = amount;
            pulse.Speed = speed;
            pulse.BaseAlpha = Mathf.Max(0.04f, color.a * 0.62f);
            pulse.PeakAlpha = color.a;
            pulse.Phase = phase;
        }

        void AddSparkles(Transform parent, float baseSize)
        {
            Vector2[] normalizedPositions =
            {
                new Vector2(-0.62f, 0.50f), new Vector2(-0.72f, 0.06f), new Vector2(-0.40f, 0.77f),
                new Vector2(0.28f, 0.78f), new Vector2(0.68f, 0.38f), new Vector2(0.74f, -0.20f),
                new Vector2(-0.08f, 0.92f), new Vector2(0.12f, -0.82f), new Vector2(-0.58f, -0.46f),
                new Vector2(0.54f, -0.58f)
            };

            float radius = baseSize * 0.58f;
            for (int i = 0; i < normalizedPositions.Length; i++)
            {
                var sparkle = new GameObject("Glow Sparkle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                sparkle.transform.SetParent(parent, false);
                var rect = sparkle.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = normalizedPositions[i] * radius;
                float size = i % 3 == 0 ? 10f : 6f;
                rect.sizeDelta = new Vector2(size, size);

                var image = sparkle.GetComponent<Image>();
                image.sprite = CreateCurrentLevelSparkleSprite();
                image.raycastTarget = false;
                image.color = new Color(1f, 0.86f, 0.25f, 0.82f);

                var pulse = sparkle.AddComponent<LevelMapSparklePulse>();
                pulse.Speed = 1.7f + i * 0.23f;
                pulse.Delay = i * 0.41f;
                pulse.Drift = 1.4f + i % 3;
            }
        }

        static Sprite CreateCurrentLevelHaloSprite()
        {
            if (currentLevelHaloSprite != null)
                return currentLevelHaloSprite;

            currentLevelHaloSprite = CreateRadialSprite(144, 2.25f, 1f);
            return currentLevelHaloSprite;
        }

        static Sprite CreateCurrentLevelRingSprite()
        {
            if (currentLevelRingSprite != null)
                return currentLevelRingSprite;

            int size = 144;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            float center = (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center) / center;
                    float dy = (y - center) / center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float sharpRing = Mathf.Clamp01(1f - Mathf.Abs(dist - 0.72f) / 0.045f);
                    float softBloom = Mathf.Clamp01(1f - Mathf.Abs(dist - 0.72f) / 0.16f) * 0.42f;
                    float innerGlow = Mathf.Clamp01(1f - dist / 0.70f) * 0.10f;
                    float alpha = Mathf.Clamp01(Mathf.Pow(sharpRing, 1.4f) + softBloom + innerGlow);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply(false, true);
            currentLevelRingSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            return currentLevelRingSprite;
        }

        static Sprite CreateCurrentLevelSparkleSprite()
        {
            if (currentLevelSparkleSprite != null)
                return currentLevelSparkleSprite;

            currentLevelSparkleSprite = CreateRadialSprite(32, 1.8f, 1f);
            return currentLevelSparkleSprite;
        }

        static Sprite CreateRadialSprite(int size, float power, float maxAlpha)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            float center = (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center) / center;
                    float dy = (y - center) / center;
                    float dist = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                    float alpha = Mathf.Pow(1f - dist, power) * maxAlpha;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        void RefreshHeaderText()
        {
            var title = FindChildLoose(transform, "Title");
            var titleText = title != null ? title.GetComponent<TMP_Text>() : null;
            if (titleText != null)
                titleText.text = "CẤP ĐỘ";

            var coins = FindChildLoose(transform, "SumLine");
            var coinText = coins != null ? coins.GetComponent<TMP_Text>() : null;
            if (coinText != null)
                coinText.text = "Xu: " + LevelProgress.Coins;
        }

        Sprite ResolveNodeSprite(bool unlocked, int stars)
        {
            if (!unlocked)
                return LoadSprite("level_glow_node_locked");
            if (stars <= 0)
                return LoadSprite("level_glow_node_unlocked");
            if (stars == 1)
                return LoadSprite("level_glow_node_completed_1_star");
            if (stars == 2)
                return LoadSprite("level_glow_node_completed_2_stars");
            return LoadSprite("level_glow_node_completed_3_stars");
        }

        Sprite ResolveDigitSprite(int level, bool unlocked)
        {
            string tone = unlocked ? "gold" : "silver";
            return LoadSprite("level_glow_digit_" + tone + "_" + Mathf.Clamp(level, 1, 10).ToString("00"));
        }

        void ApplyDigitLayout(RectTransform digitRect, int level, bool unlocked, int stars)
        {
            if (digitRect == null)
                return;

            digitRect.anchorMin = new Vector2(0.5f, 0.5f);
            digitRect.anchorMax = new Vector2(0.5f, 0.5f);
            digitRect.pivot = new Vector2(0.5f, 0.5f);

            if (!unlocked)
                return;

            bool doubleDigit = level >= 10;
            digitRect.sizeDelta = doubleDigit ? new Vector2(106f, 82f) : new Vector2(76f, 86f);
            digitRect.localScale = Vector3.one * (doubleDigit ? 0.36f : 0.40f);
            digitRect.anchoredPosition = Vector2.zero;
        }

        Sprite LoadSprite(string assetName)
        {
            if (SpriteCache.TryGetValue(assetName, out var cached))
                return cached;

            string path = "BrickStacker/SlicedAssets/LevelMap/" + assetName;
            var sprite = Resources.Load<Sprite>(path);
            if (sprite == null)
            {
                var texture = Resources.Load<Texture2D>(path);
                if (texture != null)
                {
                    texture.filterMode = FilterMode.Bilinear;
                    texture.wrapMode = TextureWrapMode.Clamp;
                    sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                }
            }

            if (sprite != null)
                SpriteCache[assetName] = sprite;
            return sprite;
        }

        void StartLevel(int level)
        {
            if (level > LevelProgress.CurrentUnlockedLevel)
                return;

            RuntimeArt.PlayUiSwitchSound();
            GameSession.SelectedLevel = level;
            GameSession.JourneyLevel = level;
            SceneManager.LoadScene("BrickGame");
        }

        Transform FindChildLoose(Transform root, string targetName)
        {
            if (root == null)
                return null;

            string normalizedTarget = NormalizeName(targetName);
            var children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (NormalizeName(children[i].name) == normalizedTarget)
                    return children[i];
            }
            return null;
        }

        string NormalizeName(string value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : value.Trim().Replace(" ", string.Empty).ToLowerInvariant();
        }
    }

    public class LevelMapGlowPulse : MonoBehaviour
    {
        public float Amount = 0.08f;
        public float Speed = 2.0f;
        public float BaseAlpha = 0.36f;
        public float PeakAlpha = 0.66f;
        public float Phase;

        RectTransform rect;
        Image image;
        Vector3 baseScale;
        Color baseColor;

        void Awake()
        {
            rect = GetComponent<RectTransform>();
            image = GetComponent<Image>();
            baseScale = rect != null ? rect.localScale : transform.localScale;
            baseColor = image != null ? image.color : Color.white;
        }

        void LateUpdate()
        {
            float wave = (Mathf.Sin(Time.unscaledTime * Speed + Phase) + 1f) * 0.5f;
            if (rect != null)
                rect.localScale = baseScale * (1f + wave * Amount);
            if (image != null)
            {
                baseColor.a = Mathf.Lerp(BaseAlpha, PeakAlpha, wave);
                image.color = baseColor;
            }
        }
    }

    public class LevelMapSparklePulse : MonoBehaviour
    {
        public float Speed = 2f;
        public float Delay;
        public float Drift = 2f;

        RectTransform rect;
        Image image;
        Vector2 basePosition;
        Vector3 baseScale;
        Color baseColor;

        void Awake()
        {
            rect = GetComponent<RectTransform>();
            image = GetComponent<Image>();
            if (rect != null)
            {
                basePosition = rect.anchoredPosition;
                baseScale = rect.localScale;
            }
            else
            {
                baseScale = transform.localScale;
            }

            baseColor = image != null ? image.color : Color.white;
        }

        void LateUpdate()
        {
            float wave = (Mathf.Sin(Time.unscaledTime * Speed + Delay) + 1f) * 0.5f;
            float blink = Mathf.Pow(wave, 2.2f);

            if (rect != null)
            {
                rect.localScale = baseScale * Mathf.Lerp(0.72f, 1.38f, blink);
                rect.anchoredPosition = basePosition + new Vector2(0f, Mathf.Lerp(-Drift, Drift, wave));
            }

            if (image != null)
            {
                baseColor.a = Mathf.Lerp(0.18f, 0.95f, blink);
                image.color = baseColor;
            }
        }
    }
}
