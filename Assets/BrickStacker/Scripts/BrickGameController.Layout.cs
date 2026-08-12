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

        void DisablePuzzleAnchorFrame()
        {
            if (scenePuzzleBoardAnchorRect == null) return;
            var f = FindChildLoose(scenePuzzleBoardAnchorRect, "Runtime Frame");
            if (f != null) { var im = f.GetComponent<Image>(); if (im != null) im.enabled = false; }
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
    }
}
