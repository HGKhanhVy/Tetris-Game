using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BrickStacker
{
    // Lớp phủ của tutorial: làm tối màn hình, chừa một "ô sáng" quanh thứ cần chú ý, viền nhấp
    // nháy quanh ô đó, thẻ chữ hướng dẫn, và bàn tay mô phỏng thao tác.
    //
    // Bước chỉ cần đọc rồi đi tiếp KHÔNG có nút bấm: một tấm trong suốt phủ kín màn hình nhận cú
    // chạm ở bất cứ đâu. Tấm này mang Button nên PointerOverInteractiveUi thấy được -> gameplay
    // tự bỏ qua cú chạm đó, không lo vừa "tiếp" vừa xoay khối. Bước chờ thao tác thật thì tấm này
    // TẮT, cú chạm rơi thẳng xuống gameplay.
    //
    // Canvas KHÔNG dùng CanvasScaler nên toạ độ canvas = pixel màn hình: mọi vùng highlight lấy
    // trực tiếp từ RectTransformUtility.WorldToScreenPoint, khỏi quy đổi.
    public class TutorialOverlayView : MonoBehaviour
    {
        const float DimAlpha = 0.74f;
        const float CellHighlightPad = 3f;
        static readonly Color CardFill = new Color(0.055f, 0.098f, 0.235f, 0.88f);
        static readonly Color CardBorder = new Color(0.35f, 0.72f, 1f, 0.95f);
        static readonly Color SpotFrameColor = new Color(0.35f, 0.72f, 1f);
        static readonly Color TitleColor = new Color(1f, 0.88f, 0.58f);
        static readonly Color BodyColor = new Color(0.86f, 0.92f, 1f);
        static readonly Color HintColor = new Color(0.72f, 0.82f, 0.96f);

        Canvas canvas;
        // Lớp tối ghép từ nhiều tấm chữ nhật, số lượng tuỳ số lỗ phải khoét -> dùng pool.
        readonly List<Image> dimQuads = new List<Image>();
        readonly List<Rect> holes = new List<Rect>();
        readonly List<float> bandEdges = new List<float>();
        readonly List<Rect> bandHoles = new List<Rect>();
        static readonly Comparison<Rect> ByLeftEdge = (a, b) => a.xMin.CompareTo(b.xMin);
        bool dimVisible;
        Image spotFrame;
        TutorialHandCue hand;

        GameObject card;
        Text titleText;
        Text bodyText;
        Text hintText;
        Image cardGlow;
        GameObject tapCatcher;

        Rect spotlight;
        bool hasSpotlight;
        Rect cardAnchor;      // vùng để thẻ nép vào; ưu tiên hơn ô sáng khi được đặt
        bool hasCardAnchor;

        // Khung khoanh từng ô lẻ. Dùng pool: một đợt cụm có thể tới vài chục ô và tutorial hiện
        // đi hiện lại nhiều lần, tạo/huỷ mỗi lần là phí.
        readonly List<Image> cellHighlights = new List<Image>();
        float screenW, screenH;

        // Lề trong thẻ và khoảng cách giữa các dòng, tính theo chiều cao màn hình.
        float padX, padTop, padBottom, gapTitle, gapHint;
        float maxInnerWidth, minInnerWidth;

        public event Action ContinuePressed;

        public static TutorialOverlayView Create()
        {
            var go = new GameObject("Tutorial Overlay");
            var view = go.AddComponent<TutorialOverlayView>();
            view.Build();
            return view;
        }

        void Build()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 26000;   // trên HUD gameplay và canvas con của bàn cờ
            gameObject.AddComponent<GraphicRaycaster>();   // chỉ để nút TIẾP bấm được

            screenW = Screen.width;
            screenH = Screen.height;


            var frameGo = new GameObject("Spot Frame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            frameGo.transform.SetParent(transform, false);
            spotFrame = frameGo.GetComponent<Image>();
            spotFrame.sprite = TutorialSprites.RoundedFrame();
            spotFrame.type = Image.Type.Sliced;
            spotFrame.raycastTarget = false;
            spotFrame.color = new Color(SpotFrameColor.r, SpotFrameColor.g, SpotFrameColor.b, 0.95f);
            var frameRect = spotFrame.rectTransform;
            frameRect.anchorMin = frameRect.anchorMax = Vector2.zero;
            frameRect.pivot = new Vector2(0.5f, 0.5f);
            spotFrame.enabled = false;

            BuildTapCatcher();
            BuildCard();

            hand = gameObject.AddComponent<TutorialHandCue>();
            hand.Build(transform, screenH);

            ClearSpotlight();
            SetCardVisible(false);
        }

        void BuildTapCatcher()
        {
            tapCatcher = new GameObject("Tap Catcher", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            tapCatcher.transform.SetParent(transform, false);
            var img = tapCatcher.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0f);   // vô hình nhưng vẫn ăn raycast
            img.raycastTarget = true;
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var button = tapCatcher.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => ContinuePressed?.Invoke());
            tapCatcher.SetActive(false);
        }

        Image GetDimQuad(int index)
        {
            while (dimQuads.Count <= index)
                dimQuads.Add(CreateDim("Dim " + dimQuads.Count));
            return dimQuads[index];
        }

        Image CreateDim(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(transform, false);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.01f, 0.03f, 0.09f, DimAlpha);
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = Vector2.zero;
            go.transform.SetAsFirstSibling();   // luôn nằm dưới ô sáng, khung ô, thẻ chữ và bàn tay
            img.enabled = false;
            return img;
        }

        void BuildCard()
        {
            var font = RuntimeArt.LoadTutorialFont();
            var boldFont = RuntimeArt.LoadTutorialBoldFont();
            padX = Mathf.Round(screenH * 0.018f);
            padTop = Mathf.Round(screenH * 0.014f);
            padBottom = Mathf.Round(screenH * 0.013f);
            gapTitle = Mathf.Round(screenH * 0.008f);
            gapHint = Mathf.Round(screenH * 0.010f);
            maxInnerWidth = Mathf.Min(screenW * 0.92f, 900f) - padX * 2f;
            minInnerWidth = Mathf.Min(screenW * 0.42f, 340f);

            // Cỡ chữ tính THẲNG từ chiều cao màn hình, không phụ thuộc bề rộng thẻ: trước đây
            // resizeTextForBestFit tự co chữ cho vừa khung nên câu dài là chữ bé tí.
            int titleSize = Mathf.RoundToInt(screenH * 0.036f);
            int bodySize = Mathf.RoundToInt(screenH * 0.030f);
            int hintSize = Mathf.RoundToInt(screenH * 0.020f);

            // Thẻ = nền navy mờ + một đường viền mảnh. Cố tình KHÔNG dùng khung hoạ tiết của popup:
            // hoạ tiết dày làm chữ hướng dẫn nặng nề, còn nền mờ vẫn cho thấy bàn chơi phía sau.
            card = new GameObject("Tutorial Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            card.transform.SetParent(transform, false);
            var cardImg = card.GetComponent<Image>();
            cardImg.sprite = TutorialSprites.RoundedPanel();
            cardImg.type = Image.Type.Sliced;
            cardImg.color = CardFill;
            cardImg.raycastTarget = false;
            var cardRect = cardImg.rectTransform;
            cardRect.anchorMin = cardRect.anchorMax = Vector2.zero;
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            // Kích thước thật do LayoutCard tính lại mỗi lần hiện, theo đúng lượng chữ của bước đó.
            cardRect.sizeDelta = new Vector2(minInnerWidth + padX * 2f, padTop + padBottom);

            const float glowSpread = 16f;   // quầng loang ra ngoài mép thẻ bao nhiêu pixel
            var glowGo = new GameObject("Card Glow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            glowGo.transform.SetParent(card.transform, false);
            cardGlow = glowGo.GetComponent<Image>();
            cardGlow.sprite = TutorialSprites.RoundedGlow();
            cardGlow.type = Image.Type.Sliced;
            cardGlow.color = CardBorder;
            cardGlow.raycastTarget = false;
            var glowRect = cardGlow.rectTransform;
            glowRect.anchorMin = Vector2.zero;
            glowRect.anchorMax = Vector2.one;
            glowRect.offsetMin = new Vector2(-glowSpread, -glowSpread);
            glowRect.offsetMax = new Vector2(glowSpread, glowSpread);

            var borderGo = new GameObject("Card Border", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            borderGo.transform.SetParent(card.transform, false);
            var borderImg = borderGo.GetComponent<Image>();
            borderImg.sprite = TutorialSprites.RoundedFrame();
            borderImg.type = Image.Type.Sliced;
            borderImg.color = CardBorder;
            borderImg.raycastTarget = false;
            var borderRect = borderImg.rectTransform;
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = borderRect.offsetMax = Vector2.zero;

            // Bold thật của bộ font; chỉ khi thiếu file Bold mới nhờ Unity bôi đậm hộ.
            titleText = MakeText(card.transform, boldFont, titleSize, TitleColor, TextAnchor.MiddleCenter);
            if (boldFont == font)
                titleText.fontStyle = FontStyle.Bold;

            // Tự wrap: chỗ xuống dòng do layout chọn theo TỪ, không cắt cứng giữa cụm từ.
            bodyText = MakeText(card.transform, font, bodySize, BodyColor, TextAnchor.MiddleCenter);

            hintText = MakeText(card.transform, font, hintSize, HintColor, TextAnchor.MiddleCenter);
            hintText.text = "Chạm màn hình để tiếp tục";
        }

        static Text MakeText(Transform parent, Font font, int size, Color color, TextAnchor anchor)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = font;
            text.fontSize = Mathf.Max(12, size);
            text.color = color;
            text.alignment = anchor;
            text.raycastTarget = false;
            // Neo mép trên-trái: LayoutCard xếp các dòng từ trên xuống bằng anchoredPosition.
            var rt = text.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            text.resizeTextForBestFit = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            // Không Outline: nền navy đã đủ tương phản, thêm viền chỉ làm nét chữ dày và bệt.
            return text;
        }

        // === API dùng từ BrickGameController ===

        public void SetDimVisible(bool visible)
        {
            dimVisible = visible;
            if (!visible)
                spotFrame.enabled = false;
            LayoutDim();
        }

        // Chừa một ô sáng quanh vùng cần chú ý (toạ độ pixel màn hình).
        public void SetSpotlight(Rect target, float padding = 14f)
        {
            holes.Clear();
            holes.Add(Expand(target, padding));
            spotlight = holes[0];
            hasSpotlight = true;
            dimVisible = true;
            ClearCellHighlights();
            LayoutDim();

            spotFrame.enabled = true;
            var rt = spotFrame.rectTransform;
            rt.sizeDelta = new Vector2(spotlight.width, spotlight.height);
            rt.anchoredPosition = new Vector2(spotlight.center.x, spotlight.center.y);
        }

        // Chừa sáng ĐÚNG từng ô rời rạc: lớp tối khoét nhiều lỗ, mỗi lỗ thêm một khung viền riêng.
        // Khác SetSpotlight ở chỗ không có khung bao chung — khung bao sẽ ôm cả ô không liên quan.
        public void SetSpotlightCells(IList<Rect> cells, float padding = 3f)
        {
            holes.Clear();
            int count = cells != null ? cells.Count : 0;
            for (int i = 0; i < count; i++)
                holes.Add(Expand(cells[i], padding));

            hasSpotlight = holes.Count > 0;
            spotlight = BoundsOf(holes);
            dimVisible = true;
            spotFrame.enabled = false;
            SetCellHighlights(holes);
            LayoutDim();
        }

        public void ClearSpotlight()
        {
            holes.Clear();
            hasSpotlight = false;
            spotFrame.enabled = false;
            ClearCellHighlights();
            LayoutDim();
        }

        static Rect Expand(Rect r, float padding)
        {
            return new Rect(r.xMin - padding, r.yMin - padding,
                r.width + padding * 2f, r.height + padding * 2f);
        }

        static Rect BoundsOf(List<Rect> rects)
        {
            if (rects.Count == 0)
                return new Rect(0f, 0f, 0f, 0f);

            float minX = rects[0].xMin, minY = rects[0].yMin;
            float maxX = rects[0].xMax, maxY = rects[0].yMax;
            for (int i = 1; i < rects.Count; i++)
            {
                minX = Mathf.Min(minX, rects[i].xMin);
                minY = Mathf.Min(minY, rects[i].yMin);
                maxX = Mathf.Max(maxX, rects[i].xMax);
                maxY = Mathf.Max(maxY, rects[i].yMax);
            }
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        void LayoutDim()
        {
            int used = dimVisible ? BuildDimQuads() : 0;
            for (int i = used; i < dimQuads.Count; i++)
                dimQuads[i].enabled = false;
        }

        // Phủ tối phần màn hình NGOÀI các lỗ. Quét theo dải ngang: cắt màn hình tại mọi mép trên
        // và mép dưới của lỗ, trong mỗi dải thì các lỗ đều là cột nguyên vẹn nên chỉ việc lấp
        // khoảng trống giữa chúng. Nhờ vậy khoét bao nhiêu lỗ cũng được, kể cả lỗ chồng nhau.
        int BuildDimQuads()
        {
            float w = Screen.width;
            float h = Screen.height;
            if (holes.Count == 0)
            {
                Place(GetDimQuad(0), 0f, 0f, w, h);
                return 1;
            }

            bandEdges.Clear();
            bandEdges.Add(0f);
            bandEdges.Add(h);
            for (int i = 0; i < holes.Count; i++)
            {
                bandEdges.Add(Mathf.Clamp(holes[i].yMin, 0f, h));
                bandEdges.Add(Mathf.Clamp(holes[i].yMax, 0f, h));
            }
            bandEdges.Sort();

            int used = 0;
            for (int b = 0; b + 1 < bandEdges.Count; b++)
            {
                float y0 = bandEdges[b];
                float y1 = bandEdges[b + 1];
                if (y1 - y0 <= 0.5f)
                    continue;

                bandHoles.Clear();
                for (int i = 0; i < holes.Count; i++)
                {
                    if (holes[i].yMin <= y0 + 0.01f && holes[i].yMax >= y1 - 0.01f)
                        bandHoles.Add(holes[i]);
                }
                bandHoles.Sort(ByLeftEdge);

                float x = 0f;
                for (int i = 0; i < bandHoles.Count; i++)
                {
                    if (bandHoles[i].xMin > x)
                        Place(GetDimQuad(used++), x, y0, bandHoles[i].xMin - x, y1 - y0);
                    x = Mathf.Max(x, bandHoles[i].xMax);
                }
                if (x < w)
                    Place(GetDimQuad(used++), x, y0, w - x, y1 - y0);
            }
            return used;
        }

        static void Place(Image img, float x, float y, float w, float h)
        {
            var rt = img.rectTransform;
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(Mathf.Max(0f, w), Mathf.Max(0f, h));
            img.enabled = true;
        }

        // tapToContinue = bước chỉ cần đọc: phủ tấm nhận chạm toàn màn + hiện dòng gợi ý.
        public void ShowCard(string title, string body, bool tapToContinue, TutorialCardPlacement placement)
        {
            SetCardVisible(true);
            // Tiêu đề in hoa + in đậm để tách hẳn khỏi phần nội dung đọc.
            titleText.text = (title ?? "").ToUpperInvariant();
            bodyText.text = body ?? "";
            hintText.enabled = tapToContinue;
            tapCatcher.SetActive(tapToContinue);

            LayoutCard(tapToContinue);

            var rt = (RectTransform)card.transform;
            Vector2 size = rt.sizeDelta;
            Rect safe = Screen.safeArea;

            if (placement == TutorialCardPlacement.Center)
                rt.anchoredPosition = new Vector2(safe.center.x, safe.center.y);
            else if (hasCardAnchor)
                rt.anchoredPosition = PositionNextTo(cardAnchor, size);
            else if (hasSpotlight)
                rt.anchoredPosition = PositionNextTo(spotlight, size);
            else
                rt.anchoredPosition = TopLeftPosition(size);
        }

        // Bám safeArea chứ không bám mép màn: máy có tai thỏ sẽ không cắt mất góc thẻ.
        Vector2 TopLeftPosition(Vector2 size)
        {
            Rect safe = Screen.safeArea;
            const float edge = 26f;
            return new Vector2(safe.xMin + edge + size.x * 0.5f, safe.yMax - edge - size.y * 0.5f);
        }

        // Thẻ nép ngay cạnh vùng đang được chỉ tới, chọn phía đầu tiên còn đủ chỗ nguyên vẹn.
        // Ưu tiên dưới/trên trước hai bên: thẻ rộng ngang nên trái/phải hiếm khi lọt.
        Vector2 PositionNextTo(Rect target, Vector2 size)
        {
            Rect safe = Screen.safeArea;
            float gap = Mathf.Round(screenH * 0.014f);
            float halfW = size.x * 0.5f;
            float halfH = size.y * 0.5f;

            // Canh giữa theo trục còn lại, nhưng kẹp lại để thẻ không thò ra ngoài safeArea.
            float alignX = Mathf.Clamp(target.center.x, safe.xMin + halfW, safe.xMax - halfW);
            float alignY = Mathf.Clamp(target.center.y, safe.yMin + halfH, safe.yMax - halfH);

            float below = target.yMin - gap - halfH;
            if (below - halfH >= safe.yMin)
                return new Vector2(alignX, below);

            float above = target.yMax + gap + halfH;
            if (above + halfH <= safe.yMax)
                return new Vector2(alignX, above);

            float right = target.xMax + gap + halfW;
            if (right + halfW <= safe.xMax)
                return new Vector2(right, alignY);

            float left = target.xMin - gap - halfW;
            if (left - halfW >= safe.xMin)
                return new Vector2(left, alignY);

            // Ô sáng quá to, không phía nào lọt: nép hẳn về phía còn nhiều chỗ hơn.
            bool moreRoomBelow = target.yMin - safe.yMin >= safe.yMax - target.yMax;
            return new Vector2(alignX, moreRoomBelow ? safe.yMin + halfH : safe.yMax - halfH);
        }

        // Thẻ ôm lấy chữ chứ không có kích thước cố định: bước ít chữ ra thẻ nhỏ, bước nhiều chữ
        // ra thẻ to. Phải đo bề RỘNG trước rồi mới đo được chiều CAO, vì Text.preferredHeight phụ
        // thuộc bề rộng ô chứa (chữ xuống dòng khác nhau thì số dòng khác nhau).
        void LayoutCard(bool showHint)
        {
            // Số dòng ÍT NHẤT mà mỗi đoạn cần khi được nới hết cỡ.
            int titleLines = LineCount(titleText, maxInnerWidth);
            int bodyLines = LineCount(bodyText, maxInnerWidth);
            int hintLines = showHint ? LineCount(hintText, maxInnerWidth) : 1;

            // Rồi thu bề rộng lại đúng bằng phần chia đều cho từng ấy dòng. Nếu cứ dùng bề rộng
            // tối đa, câu chỉ dài hơn một dòng vài chữ sẽ đẩy đúng một từ rớt xuống dòng dưới.
            float inner = Mathf.Max(EvenWidth(titleText, titleLines), EvenWidth(bodyText, bodyLines));
            if (showHint)
                inner = Mathf.Max(inner, EvenWidth(hintText, hintLines));
            inner = Mathf.Clamp(inner, minInnerWidth, maxInnerWidth);

            // Chia đều tính theo pixel, nhưng chữ chỉ ngắt được ở ranh giới TỪ nên có thể đội
            // thêm một dòng. Nới dần tới khi về đúng số dòng đã chốt ở trên.
            inner = FitWithoutExtraLine(bodyText, inner, bodyLines);
            inner = FitWithoutExtraLine(titleText, inner, titleLines);
            if (showHint)
                inner = FitWithoutExtraLine(hintText, inner, hintLines);

            float titleH = MeasureRow(titleText, inner);
            float bodyH = MeasureRow(bodyText, inner);
            float hintH = showHint ? MeasureRow(hintText, inner) : 0f;

            float height = padTop + titleH + gapTitle + bodyH + padBottom;
            if (showHint)
                height += gapHint + hintH;

            var rt = (RectTransform)card.transform;
            rt.sizeDelta = new Vector2(inner + padX * 2f, height);

            float top = padTop;
            PlaceRow(titleText, inner, titleH, top);
            top += titleH + gapTitle;
            PlaceRow(bodyText, inner, bodyH, top);
            if (showHint)
            {
                top += bodyH + gapHint;
                PlaceRow(hintText, inner, hintH, top);
            }
        }

        static float MeasureRow(Text text, float width)
        {
            text.rectTransform.sizeDelta = new Vector2(width, 0f);
            return text.preferredHeight;
        }

        // Bề rộng nếu chia đều lượng chữ cho từng ấy dòng.
        static float EvenWidth(Text text, int lines)
        {
            return text.preferredWidth / Mathf.Max(1, lines);
        }

        int LineCount(Text text, float width)
        {
            float lineHeight = MeasureRow(text, maxInnerWidth * 8f);   // rộng thênh thang = đúng 1 dòng
            if (lineHeight <= 0f)
                return 1;
            return Mathf.Max(1, Mathf.RoundToInt(MeasureRow(text, width) / lineHeight));
        }

        float FitWithoutExtraLine(Text text, float width, int targetLines)
        {
            for (int i = 0; i < 10 && width < maxInnerWidth; i++)
            {
                if (LineCount(text, width) <= targetLines)
                    break;
                width = Mathf.Min(width * 1.05f, maxInnerWidth);
            }
            return width;
        }

        void PlaceRow(Text text, float width, float height, float top)
        {
            var rt = text.rectTransform;
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = new Vector2(padX, -top);
        }

        public void SetCardVisible(bool visible)
        {
            if (card != null)
                card.SetActive(visible);
            if (!visible && tapCatcher != null)
                tapCatcher.SetActive(false);
        }

        // Vùng để thẻ bám vào khi bước không có ô sáng; Vector2.zero ở aim = tự chọn hướng tay.
        public void SetCardAnchor(Rect anchor)
        {
            cardAnchor = anchor;
            hasCardAnchor = true;
        }

        public void ClearCardAnchor() => hasCardAnchor = false;

        // Khoanh riêng từng ô (toạ độ pixel màn hình). Truyền danh sách rỗng = tắt hết.
        public void SetCellHighlights(IList<Rect> cells)
        {
            int count = cells != null ? cells.Count : 0;
            for (int i = 0; i < count; i++)
            {
                var img = GetCellHighlight(i);
                var rt = img.rectTransform;
                rt.sizeDelta = new Vector2(cells[i].width + CellHighlightPad * 2f,
                    cells[i].height + CellHighlightPad * 2f);
                rt.anchoredPosition = new Vector2(cells[i].center.x, cells[i].center.y);
                img.enabled = true;
            }
            for (int i = count; i < cellHighlights.Count; i++)
                cellHighlights[i].enabled = false;
        }

        public void ClearCellHighlights()
        {
            for (int i = 0; i < cellHighlights.Count; i++)
                cellHighlights[i].enabled = false;
        }

        Image GetCellHighlight(int index)
        {
            while (cellHighlights.Count <= index)
            {
                var go = new GameObject("Cell Highlight", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(transform, false);
                // Nằm ngay trên lớp tối, dưới thẻ chữ.
                go.transform.SetSiblingIndex(spotFrame.transform.GetSiblingIndex() + 1);
                var img = go.GetComponent<Image>();
                img.sprite = TutorialSprites.RoundedFrame();
                img.type = Image.Type.Sliced;
                img.raycastTarget = false;
                var rt = img.rectTransform;
                rt.anchorMin = rt.anchorMax = Vector2.zero;
                rt.pivot = new Vector2(0.5f, 0.5f);
                img.enabled = false;
                cellHighlights.Add(img);
            }
            return cellHighlights[index];
        }

        public void ShowTapHand(Vector2 screenPoint) => hand.ShowTap(screenPoint);

        public void ShowTapHand(Vector2 screenPoint, Vector2 aim)
        {
            if (aim == Vector2.zero)
                hand.ShowTap(screenPoint);
            else
                hand.ShowTap(screenPoint, aim);
        }

        public void ShowSwipeHand(Vector2 from, Vector2 to) => hand.ShowSwipe(from, to);

        public void HideHand() => hand.Hide();

        public void HideAll()
        {
            SetDimVisible(false);
            ClearSpotlight();
            ClearCellHighlights();
            SetCardVisible(false);
            HideHand();
        }

        void Update()
        {
            // Viền ô sáng nhấp nháy để mắt bị kéo về đúng chỗ.
            if (spotFrame != null && spotFrame.enabled)
            {
                float pulse = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 6f);
                spotFrame.color = new Color(SpotFrameColor.r, SpotFrameColor.g, SpotFrameColor.b, 0.45f + 0.5f * pulse);
            }

            for (int i = 0; i < cellHighlights.Count; i++)
            {
                if (!cellHighlights[i].enabled)
                    continue;
                float pulse = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 6f);
                cellHighlights[i].color = new Color(SpotFrameColor.r, SpotFrameColor.g, SpotFrameColor.b,
                    0.45f + 0.5f * pulse);
            }

            // Quầng viền thẻ sáng lên rồi dịu xuống, cùng nhịp với viền ô sáng.
            if (cardGlow != null && cardGlow.isActiveAndEnabled)
            {
                float shine = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.6f);
                cardGlow.color = new Color(CardBorder.r, CardBorder.g, CardBorder.b, 0.30f + 0.55f * shine);
            }

            // Dòng "chạm màn hình" thở nhẹ để người chơi biết là đang chờ mình.
            if (hintText != null && hintText.enabled)
            {
                float breathe = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 3.2f);
                hintText.color = new Color(HintColor.r, HintColor.g, HintColor.b, 0.45f + 0.55f * breathe);
            }

            // Màn hình xoay / đổi kích thước thì dựng lại mảng tối cho khớp.
            if (!Mathf.Approximately(screenW, Screen.width) || !Mathf.Approximately(screenH, Screen.height))
            {
                screenW = Screen.width;
                screenH = Screen.height;
                LayoutDim();
            }
        }
    }
}
