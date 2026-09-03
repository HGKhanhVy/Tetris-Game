using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace BrickStacker
{
    public partial class MenuController : MonoBehaviour
    {
        bool namePromptRunning;

        // Cổng "đặt tên trước khi chơi": máy mới, lần đầu bấm BẮT ĐẦU / 1 VS 1 thì hỏi tên rồi mới
        // chạy tiếp hành động. Đã có tên (hoặc đã từng bỏ qua) thì chạy thẳng.
        // Mất mạng / đăng nhập lâu → KHÔNG chặn người chơi: cho vào chơi, để lần sau hỏi lại.
        async void RunWithPlayerName(Transform panel, Action action)
        {
            if (namePromptRunning)
                return;

            if (PlayerPrefs.GetInt(NamePromptedKey, 0) == 1 || !string.IsNullOrEmpty(ServicesManager.PlayerName))
            {
                action?.Invoke();
                return;
            }

            namePromptRunning = true;
            try
            {
                var signIn = ServicesManager.EnsureSignedInAsync();
                bool signedIn = await System.Threading.Tasks.Task.WhenAny(
                    signIn, System.Threading.Tasks.Task.Delay(2500)) == signIn && signIn.Result;
                if (this == null)
                    return;

                if (!signedIn || !string.IsNullOrEmpty(ServicesManager.PlayerName))
                {
                    action?.Invoke();
                    return;
                }

                ShowNamePopup(panel, action);
            }
            finally
            {
                namePromptRunning = false;
            }
        }

        // onDone: chạy sau khi đặt tên xong HOẶC người chơi bỏ qua — không chặn đường vào game.
        // Giao diện dựng bằng bộ asset v3: khung xanh (popup-pause), banner tiêu đề + nút cam
        // (popup-start), ô nhập kiểu thanh xanh (popup-1vs1), nút X đỏ — đồng bộ với menu/BXH/1vs1.
        void ShowNamePopup(Transform parent, Action onDone)
        {
            if (mapOverlay != null) Destroy(mapOverlay);
            // Gắn vào canvas gốc + sorting cao (giống popup 1vs1): nền mờ phủ TRỌN màn hình và
            // chặn chạm, không cho bấm nhầm nút menu phía sau khi đang nhập tên.
            var rootCanvas = parent.GetComponentInParent<Canvas>();
            Transform overlayParent = rootCanvas != null ? rootCanvas.rootCanvas.transform : parent;
            mapOverlay = Ui.Panel(overlayParent, "Name Overlay", new Color(0.015f, 0.045f, 0.13f, 0.80f));
            Ui.Stretch(mapOverlay);
            var ovCanvas = mapOverlay.AddComponent<Canvas>();
            ovCanvas.overrideSorting = true;
            ovCanvas.sortingOrder = 5000;
            mapOverlay.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Khung xanh v3 — vùng cắt 851x768 nên giữ đúng tỉ lệ đó.
            const float boxW = 520f;   // gọn hơn, banner nhô lên vẫn nằm trong màn 1280x720
            float boxH = boxW * 768f / 851f;
            var box = MakeV3Image(mapOverlay.transform, "Name Box", PAUSEUI + "frame.png", new Rect(0.223f, 0.139f, 0.554f, 0.750f), true);
            box.preserveAspect = false;
            var boxRt = box.rectTransform;
            boxRt.anchorMin = boxRt.anchorMax = new Vector2(0.5f, 0.5f);
            boxRt.pivot = new Vector2(0.5f, 0.5f);
            boxRt.sizeDelta = new Vector2(boxW, boxH);
            Transform boxT = box.transform;

            // Banner tiêu đề đè mép trên khung (banner rỗng, chữ vẽ đè lên).
            var banner = MakeV3Image(boxT, "Name Title Banner", STARTUI + "frame-title-man.png", new Rect(0.130f, 0.398f, 0.740f, 0.270f), false);
            banner.preserveAspect = false;
            var bnRt = banner.rectTransform;
            bnRt.anchorMin = bnRt.anchorMax = new Vector2(0.5f, 1f);
            bnRt.pivot = new Vector2(0.5f, 0.5f);
            bnRt.sizeDelta = new Vector2(boxW * 0.62f, boxW * 0.62f / 4.12f);
            bnRt.anchoredPosition = new Vector2(0f, 4f);

            bool hasName = !string.IsNullOrEmpty(ServicesManager.PlayerName);   // đã có tên -> điền sẵn để sửa
            var titleLabel = Ui.Text(banner.transform, "Hé nhô", RuntimeArt.LoadMenuButtonFont(), 34, new Color(1f, 0.98f, 0.90f), TextAnchor.MiddleCenter);
            titleLabel.fontStyle = FontStyle.Bold;
            titleLabel.raycastTarget = false;
            Ui.Rect(titleLabel, new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.52f), new Vector2(boxW * 0.46f, 52f));
            AddV3TextEdge(titleLabel, 2.0f);

            // Hiệp sĩ nhô ra góc trên-trái khung (giống popup nhiệm vụ trong màn chơi).
            var hero = MakeV3Image(boxT, "Name Hero", STARTUI + "icon-player.png", new Rect(0.357f, 0.316f, 0.286f, 0.422f), false);
            var heRt = hero.rectTransform;
            heRt.anchorMin = heRt.anchorMax = new Vector2(0.055f, 0.90f);
            heRt.pivot = new Vector2(0.5f, 0.5f);
            heRt.sizeDelta = new Vector2(108f, 108f);

            var descLabel = Ui.Text(boxT, "Tên bạn là gì đâyyy?", font, 30, new Color(0.86f, 0.94f, 1f), TextAnchor.MiddleCenter);
            descLabel.raycastTarget = false;
            descLabel.lineSpacing = 1.15f;
            Ui.Rect(descLabel, new Vector2(0.5f, 0.665f), new Vector2(0.5f, 0.665f), new Vector2(boxW * 0.86f, 80f));
            AddV3TextEdge(descLabel, 1.2f);

            var nameInput = BuildNameInput(boxT, new Vector2(0.5f, 0.47f), new Vector2(boxW * 0.80f, 74f));
            if (hasName)
                nameInput.text = ServicesManager.PlayerName;   // sửa trên tên cũ (còn dấu) cho tiện

            var statusLabel = Ui.Text(boxT, "", font, 21, new Color(1f, 0.86f, 0.42f), TextAnchor.MiddleCenter);
            statusLabel.fontStyle = FontStyle.Bold;
            statusLabel.raycastTarget = false;
            Ui.Rect(statusLabel, new Vector2(0.5f, 0.335f), new Vector2(0.5f, 0.335f), new Vector2(boxW * 0.90f, 38f));
            AddV3TextEdge(statusLabel, 1.2f);

            // Nút XÁC NHẬN (plate cam v3, chữ vẽ đè).
            var confirmBtn = BuildV1V1Button(boxT, STARTUI + "btn-batdau.png", new Rect(0.242f, 0.414f, 0.516f, 0.219f),
                new Vector2(0.5f, 0.185f), new Vector2(boxW * 0.60f, boxW * 0.60f / 3.536f));
            confirmBtn.GetComponent<Image>().preserveAspect = false;
            var confirmLabel = Ui.Text(confirmBtn.transform, "XÁC NHẬN", RuntimeArt.LoadMenuButtonFont(), 34, new Color(1f, 0.99f, 0.94f), TextAnchor.MiddleCenter);
            confirmLabel.fontStyle = FontStyle.Bold;
            confirmLabel.raycastTarget = false;
            confirmLabel.resizeTextForBestFit = true;
            confirmLabel.resizeTextMinSize = 18;
            confirmLabel.resizeTextMaxSize = 34;
            var clRt = confirmLabel.rectTransform;
            clRt.anchorMin = new Vector2(0.10f, 0f);
            clRt.anchorMax = new Vector2(0.90f, 1f);
            clRt.offsetMin = new Vector2(0f, 4f);
            clRt.offsetMax = new Vector2(0f, 4f);
            AddV3TextEdge(confirmLabel, 1.8f);
            confirmBtn.onClick.AddListener(() =>
            {
                RuntimeArt.PlayUiSwitchSound();
                ConfirmFirstName(nameInput, statusLabel, confirmBtn, onDone);
            });
            // Bấm Enter / nút Done của bàn phím ảo = xác nhận luôn, khỏi phải với tay xuống nút.
            nameInput.onEndEdit.AddListener(_ =>
            {
                if (!nameInput.wasCanceled && confirmBtn.interactable)
                    ConfirmFirstName(nameInput, statusLabel, confirmBtn, onDone);
            });

            // Nút X đỏ góc trên-phải = bỏ qua: vẫn vào chơi, không hỏi lại nữa (đặt tên sau ở BXH).
            var closeBtn = BuildV1V1Button(boxT, V1V1 + "btn-close.png", new Rect(0.061f, 0.069f, 0.876f, 0.888f),
                new Vector2(0.945f, 0.925f), new Vector2(64f, 64f));
            closeBtn.onClick.AddListener(() =>
            {
                RuntimeArt.PlayUiSwitchSound();
                PlayerPrefs.SetInt(NamePromptedKey, 1);
                PlayerPrefs.Save();
                Destroy(mapOverlay);
                onDone?.Invoke();
            });
        }

        // Ô nhập tên kiểu v3: dùng thanh xanh của popup 1vs1 nhưng GHÉP 2 NỬA ĐỐI XỨNG lấy từ
        // phần bên phải của ảnh — nhờ vậy bỏ được biểu tượng chìa khoá (vốn dành cho mã phòng)
        // mà vẫn giữ nguyên hai đầu bo tròn và dải sáng của thanh gốc.
        InputField BuildNameInput(Transform parent, Vector2 anchor, Vector2 size)
        {
            // Cấu trúc 2 tầng là BẮT BUỘC: Unity gắn con trỏ nhập (caret) vào ĐỨNG ĐẦU danh sách con
            // của object chứa Text. Nếu ảnh nền nằm chung chỗ đó thì caret + vệt bôi đen bị vẽ chìm
            // dưới nền, nhìn như không có. Nên tách: [nền] rồi mới đến [ô nhập + text].
            var root = Ui.Panel(parent, "Name Field", new Color(1f, 1f, 1f, 0f));
            Ui.Rect(root, anchor, anchor, size);
            root.GetComponent<Image>().raycastTarget = false;

            BuildNameInputBar(root.transform);

            var field = Ui.Panel(root.transform, "Name Input", new Color(1f, 1f, 1f, 0f));
            Ui.Stretch(field);
            var fieldImg = field.GetComponent<Image>();   // vùng bắt chạm trong suốt

            var inputText = Ui.Text(field.transform, "", font, 34, new Color(1f, 0.99f, 0.94f), TextAnchor.MiddleCenter);
            inputText.fontStyle = FontStyle.Bold;
            inputText.supportRichText = false;
            var itRt = inputText.rectTransform;
            itRt.anchorMin = new Vector2(0.10f, 0.18f);
            itRt.anchorMax = new Vector2(0.90f, 0.82f);
            itRt.offsetMin = itRt.offsetMax = Vector2.zero;

            var placeholder = Ui.Text(field.transform, "Tên của bạn...", font, 30, new Color(0.72f, 0.85f, 1f, 0.65f), TextAnchor.MiddleCenter);
            placeholder.fontStyle = FontStyle.BoldAndItalic;
            var phRt = placeholder.rectTransform;
            phRt.anchorMin = itRt.anchorMin;
            phRt.anchorMax = itRt.anchorMax;
            phRt.offsetMin = phRt.offsetMax = Vector2.zero;

            var input = field.AddComponent<InputField>();
            field.AddComponent<InputSelectAllOnFocus>().Field = input;   // chạm vào là bôi đen sẵn cả tên
            input.textComponent = inputText;
            input.placeholder = placeholder;
            input.targetGraphic = fieldImg;
            input.lineType = InputField.LineType.SingleLine;
            // Cho gõ THOẢI MÁI (tiếng Việt có dấu, khoảng trắng); việc bỏ dấu / lọc ký tự để
            // PlayerNameFormatter lo lúc bấm lưu — chặn ngay khi gõ sẽ làm chữ biến mất khó hiểu.
            input.contentType = InputField.ContentType.Standard;
            // Vệt bôi đen + con trỏ phải nổi trên nền thanh xanh (màu mặc định gần như chìm).
            input.selectionColor = new Color(1f, 0.82f, 0.30f, 0.55f);
            input.caretColor = new Color(1f, 0.99f, 0.94f, 1f);
            input.customCaretColor = true;
            input.caretWidth = 3;
            input.caretBlinkRate = 1.4f;
            input.characterLimit = PlayerNameFormatter.MaxLength * 2;
            return input;
        }

        // Thanh xanh làm nền ô nhập: ghép 2 NỬA ĐỐI XỨNG lấy từ phần bên phải của ảnh
        // input-maphong — nhờ vậy bỏ được biểu tượng chìa khoá (vốn dành cho mã phòng) mà vẫn giữ
        // nguyên hai đầu bo tròn và dải sáng của thanh gốc.
        void BuildNameInputBar(Transform parent)
        {
            var bar = Ui.Panel(parent, "Name Bar", new Color(1f, 1f, 1f, 0f));
            Ui.Stretch(bar);
            bar.GetComponent<Image>().raycastTarget = false;

            var rightHalf = MakeV3Image(bar.transform, "Bar R", V1V1 + "input-maphong.png", new Rect(0.550f, 0.200f, 0.420f, 0.588f), false);
            rightHalf.preserveAspect = false;
            var rhRt = rightHalf.rectTransform;
            rhRt.anchorMin = new Vector2(0.5f, 0f);
            rhRt.anchorMax = new Vector2(1f, 1f);
            rhRt.offsetMin = rhRt.offsetMax = Vector2.zero;

            var leftHalf = MakeV3Image(bar.transform, "Bar L", V1V1 + "input-maphong.png", new Rect(0.550f, 0.200f, 0.420f, 0.588f), false);
            leftHalf.preserveAspect = false;
            var lhRt = leftHalf.rectTransform;
            lhRt.anchorMin = new Vector2(0f, 0f);
            lhRt.anchorMax = new Vector2(0.5f, 1f);
            lhRt.offsetMin = lhRt.offsetMax = Vector2.zero;
            lhRt.localScale = new Vector3(-1f, 1f, 1f);   // lật ngang -> đầu bo tròn nằm bên trái
        }

        async void ConfirmFirstName(InputField nameInput, Text statusLabel, Button confirmBtn, Action onDone)
        {
            if (!confirmBtn.interactable)
                return;   // đang lưu dở, tránh bấm/Enter hai lần

            // Máy chủ Unity từ chối tên có khoảng trắng -> báo để người chơi tự sửa (dùng "_" hoặc
            // viết liền), không tự ý đổi tên của họ. Dấu tiếng Việt thì giữ nguyên.
            if (PlayerNameFormatter.HasSpace(nameInput.text))
            {
                statusLabel.text = "Tên không được có khoảng trắng.";
                return;
            }

            string name = PlayerNameFormatter.Sanitize(nameInput.text);
            if (!PlayerNameFormatter.IsValid(name))
            {
                statusLabel.text = "Tên cần ít nhất 2 chữ hoặc số.";
                return;
            }
            if (name != nameInput.text)
                nameInput.text = name;

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
                onDone?.Invoke();
            }
            else
            {
                // Tên đã lưu ở máy rồi (SetPlayerNameAsync lưu trước khi gọi mạng) nên chỉ còn
                // hai khả năng: chưa kết nối được, hoặc máy chủ không nhận tên này.
                statusLabel.text = ServicesManager.IsSignedIn
                    ? "Tên này không dùng được, thử tên khác nhé."
                    : "Chưa kết nối được máy chủ — tên đã lưu ở máy, sẽ tự đồng bộ sau.";
                confirmBtn.interactable = true;
            }
        }

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

            // Vùng safe area cho nút back (overlay không tự fit safe area) → tránh tai thỏ.
            var safeArea = Ui.Panel(mapOverlay.transform, "Safe Area", new Color(0, 0, 0, 0));
            Ui.Stretch(safeArea);
            safeArea.GetComponent<Image>().raycastTarget = false;
            safeArea.AddComponent<SafeAreaFitter>();

            // Nút back dùng chung (btn-back của trang level), đóng overlay. Neo góc trên-trái
            // vùng safe area rồi lùi vào bằng pixel (đồng bộ với màn chọn level).
            var backBtn = Ui.Button(safeArea.transform, "", font, 1, () =>
            {
                RuntimeArt.PlayUiSwitchSound();
                Destroy(mapOverlay);
            });
            const float bkSize = 92f, bkMarginX = 52f, bkMarginY = 30f;
            var bkRt = backBtn.GetComponent<RectTransform>();
            bkRt.anchorMin = bkRt.anchorMax = new Vector2(0f, 1f);
            bkRt.pivot = new Vector2(0.5f, 0.5f);
            bkRt.sizeDelta = new Vector2(bkSize, bkSize);
            bkRt.anchoredPosition = new Vector2(bkMarginX + bkSize * 0.5f, -(bkMarginY + bkSize * 0.5f));
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

            // Nút ĐỔI TÊN ngay dưới ô cúp: đặt tên xong trước đây không có cách nào sửa, và người
            // đã bỏ qua ở lần đầu cũng cần một đường để đặt. Đóng popup đặt tên thì quay lại BXH
            // (dựng lại để đọc số cúp + tên mới).
            var renameBtn = BuildV1V1Button(mapOverlay.transform, STARTUI + "btn-batdau.png", new Rect(0.242f, 0.414f, 0.516f, 0.219f),
                new Vector2(0.5f, 0.5f), new Vector2(160f, 160f / 3.536f));
            renameBtn.GetComponent<Image>().preserveAspect = false;
            var rnRt = renameBtn.GetComponent<RectTransform>();
            rnRt.anchorMin = rnRt.anchorMax = new Vector2(0.088f, 0.755f);   // cột trống bên trái, dưới nút quay lại
            rnRt.pivot = new Vector2(0.5f, 0.5f);
            rnRt.anchoredPosition = Vector2.zero;
            var renameLabel = Ui.Text(renameBtn.transform, "ĐỔI TÊN",
                RuntimeArt.LoadMenuButtonFont(), 26, new Color(1f, 0.99f, 0.94f), TextAnchor.MiddleCenter);
            renameLabel.fontStyle = FontStyle.Bold;
            renameLabel.raycastTarget = false;
            renameLabel.resizeTextForBestFit = true;
            renameLabel.resizeTextMinSize = 14;
            renameLabel.resizeTextMaxSize = 26;
            var rlRt = renameLabel.rectTransform;
            rlRt.anchorMin = new Vector2(0.12f, 0f);
            rlRt.anchorMax = new Vector2(0.88f, 1f);
            rlRt.offsetMin = new Vector2(0f, 3f);
            rlRt.offsetMax = new Vector2(0f, 3f);
            AddV3TextEdge(renameLabel, 1.4f);
            renameBtn.onClick.AddListener(() =>
            {
                RuntimeArt.PlayUiSwitchSound();
                ShowNamePopup(parent, () => ShowLeaderboardOverlay(parent));
            });

            PopulateLeaderboard(mapOverlay, ctRt, cupText, rowH, statusLabel, renameLabel);
        }

        // Cắt tên quá dài cho vừa cột NGƯỜI CHƠI (tên Unity tự sinh kiểu "ShortOrganizedRiver").
        static string ShortLbName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "Người chơi";
            const int max = 12;
            return name.Length <= max ? name : name.Substring(0, max) + "…";
        }

        async void PopulateLeaderboard(GameObject overlay, RectTransform content, Text cupText, float rowH, Text statusLabel, Text renameLabel)
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
                if (await System.Threading.Tasks.Task.WhenAny(loadTask, System.Threading.Tasks.Task.Delay(6000)) == loadTask)
                {
                    var (top, me) = loadTask.Result;
                    if (overlay == null) return; // đã đóng trong lúc chờ
                    loaded = true;
                    // Nhận diện dòng CỦA MÌNH bằng PlayerId của tài khoản đang đăng nhập. Trước đây
                    // chỉ dựa vào `me` (GetPlayerScoreAsync) — lệnh này thỉnh thoảng lỗi/timeout, khi
                    // đó dòng của chính mình bị coi là người lạ: hiện tên Unity tự sinh, không tô
                    // sáng, nên nhìn như "cúp của top 1" chứ không phải cúp của mình.
                    string myId = ServicesManager.PlayerId;
                    if (string.IsNullOrEmpty(myId) && me != null)
                        myId = me.PlayerId;

                    if (top != null)
                    {
                        for (int i = 0; i < top.Count && i < LbTopCount; i++)
                        {
                            string nm = ShortLbName(LeaderboardsSync.DisplayName(top[i]));
                            bool isOwn = !string.IsNullOrEmpty(myId) && top[i].PlayerId == myId;
                            if (isOwn)
                            {
                                ownIndex = i;
                                // Dòng của mình phải NHẬN RA ĐƯỢC: có tên thì dùng tên, chưa đặt tên
                                // thì ghi "Bạn" thay cho tên Unity tự sinh (kiểu ShortOrganizedRiver)
                                // — nếu không người chơi tưởng dòng đó là của người lạ.
                                nm = !string.IsNullOrEmpty(ServicesManager.PlayerName)
                                    ? ShortLbName(ServicesManager.PlayerName) + " (Bạn)"
                                    : "Bạn";
                                ownScore = System.Math.Max(ownScore, (long)top[i].Score);
                                hasOwn = true;
                            }
                            names.Add(nm);
                            scores.Add((long)top[i].Score);
                        }
                    }
                    if (me != null) { ownScore = System.Math.Max(ownScore, (long)me.Score); hasOwn = true; }
                }
                if (overlay == null) return;
            }
            catch (Exception) { if (overlay == null) return; loaded = false; }

            // Tên chỉ có sau khi đăng nhập xong -> chốt nhãn nút ở đây, không phải lúc dựng overlay.
            if (renameLabel != null)
                renameLabel.text = string.IsNullOrEmpty(ServicesManager.PlayerName) ? "ĐẶT TÊN" : "ĐỔI TÊN";

            // Cúp của CHÍNH mình = MAX(tổng điểm cao nhất các màn ở máy, điểm đang có trên server).
            // Lấy local để cập nhật TỨC THÌ sau khi qua màn (không đợi độ trễ gửi điểm); lấy thêm
            // server để không bị mất cúp cũ khi chơi trên máy mới / vừa xoá dữ liệu máy.
            long ownCup = System.Math.Max(LevelProgress.TotalScore, hasOwn ? ownScore : 0L);
            if (cupText != null)
                cupText.text = ownCup > 0 ? FormatLbScore(ownCup) : "--";

            // Local cao hơn server (lần gửi điểm trước bị rớt mạng) → gửi lại cho khớp.
            if (loaded && LevelProgress.TotalScore > (hasOwn ? ownScore : 0L))
                LeaderboardsSync.SubmitScore(LevelProgress.TotalScore);

            // Bảng trống (chưa có ai hoặc chưa tải được) → luôn hiện minh hoạ "trống vắng",
            // KHÔNG hiện dữ liệu giả và KHÔNG báo lỗi mạng.
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
            {
                // Dòng của chính mình: hiển thị đúng con số ở ô CÚP phía trên (cập nhật tức thì).
                long shownScore = i == ownIndex ? ownCup : scores[i];
                BuildLeaderboardRow(content, i, names[i], shownScore, i == ownIndex, rowH);
            }
        }

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

            // Avatar: chỉ đầu nhân vật (bỏ khung input-number nền trắng theo yêu cầu).
            var avatar = MakeV3Image(row.transform, "Avatar", "character/" + LbAvatars[i % LbAvatars.Length] + "_09_icon.png", new Rect(0.04f, 0.05f, 0.92f, 0.90f), false);
            avatar.raycastTarget = false;
            var avRt = avatar.rectTransform;
            avRt.anchorMin = avRt.anchorMax = new Vector2(0.35f, 0.5f);
            avRt.pivot = new Vector2(0.5f, 0.5f);
            avRt.sizeDelta = new Vector2(40f, 40f);

            // Tên người chơi. KHÔNG cho tràn ngang: tên dài (tên Unity tự sinh có thể rất dài)
            // sẽ tự co chữ cho vừa cột, không đè lên cột CÚP bên phải.
            var nameT = Ui.Text(row.transform, name, RuntimeArt.LoadMenuButtonFont(), 22, txtColor, TextAnchor.MiddleLeft);
            nameT.fontStyle = FontStyle.Bold;
            nameT.raycastTarget = false;
            nameT.horizontalOverflow = HorizontalWrapMode.Wrap;
            nameT.verticalOverflow = VerticalWrapMode.Truncate;
            nameT.resizeTextForBestFit = true;
            nameT.resizeTextMinSize = 12;
            nameT.resizeTextMaxSize = 22;
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

            // Panel nền MỚI (khung + vương miện + "1 VS 1" + "Nhập mã phòng" baked sẵn).
            float boxW = 620f, boxH = boxW / 1.195f;
            var box = Ui.Panel(mapOverlay.transform, "Multiplayer Box", Color.white);
            Ui.Rect(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(boxW, boxH));
            var boxImg = box.GetComponent<Image>();
            var panelSpr = RuntimeArt.LoadV3SubSprite(V1V1 + "panel.png", new Rect(0.003f, 0.078f, 0.992f, 0.906f));
            if (panelSpr != null) { boxImg.sprite = panelSpr; boxImg.type = Image.Type.Simple; boxImg.preserveAspect = false; boxImg.color = Color.white; }

            // Ô NHẬP MÃ PHÒNG (input-maphong.png có sẵn icon chìa khoá) + InputField 4 số.
            var inputImg = MakeV3Image(box.transform, "Room Input BG", V1V1 + "input-maphong.png", new Rect(0.016f, 0.200f, 0.969f, 0.588f), true);
            inputImg.preserveAspect = false;
            var inRt = inputImg.rectTransform;
            inRt.anchorMin = inRt.anchorMax = new Vector2(0.5f, 0.465f);
            inRt.pivot = new Vector2(0.5f, 0.5f);
            inRt.sizeDelta = new Vector2(boxW * 0.80f, boxW * 0.80f / 4.94f);
            var codeInput = inputImg.gameObject.AddComponent<InputField>();
            var inputText = Ui.Text(inputImg.transform, "", font, 46, Color.white, TextAnchor.MiddleLeft);
            inputText.fontStyle = FontStyle.Bold;
            var itRt = inputText.rectTransform;
            itRt.anchorMin = new Vector2(0.22f, 0.14f); itRt.anchorMax = new Vector2(0.95f, 0.86f);
            itRt.offsetMin = Vector2.zero; itRt.offsetMax = Vector2.zero;
            var placeholder = Ui.Text(inputImg.transform, "Mã phòng...", font, 46, new Color(0.82f, 0.90f, 1f, 0.72f), TextAnchor.MiddleLeft);
            placeholder.fontStyle = FontStyle.BoldAndItalic;
            var phRt = placeholder.rectTransform;
            phRt.anchorMin = new Vector2(0.22f, 0.14f); phRt.anchorMax = new Vector2(0.95f, 0.86f);
            phRt.offsetMin = Vector2.zero; phRt.offsetMax = Vector2.zero;
            codeInput.textComponent = inputText;
            codeInput.placeholder = placeholder;
            codeInput.contentType = InputField.ContentType.IntegerNumber;
            codeInput.characterLimit = 4;

            // Dòng trạng thái (giữa ô mã và hàng nút).
            var statusLabel = Ui.Text(box.transform, "", font, 22, new Color(1f, 0.96f, 0.82f), TextAnchor.MiddleCenter);
            statusLabel.fontStyle = FontStyle.Bold;
            statusLabel.raycastTarget = false;
            Ui.Rect(statusLabel, new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.35f), new Vector2(720, 60));
            AddDarkWoodTextEdge(statusLabel, 0.6f, 0.6f);

            // 3 nút (chữ + icon đã baked trong sprite): Tạo phòng / Vào phòng / Ghép nhanh.
            float btnW = boxW * 0.25f, btnH = btnW / 1.8f;
            const float by = 0.225f;
            var createBtn = BuildV1V1Button(box.transform, V1V1 + "btn-taophong.png", new Rect(0.013f, 0.184f, 0.974f, 0.668f), new Vector2(0.205f, by), new Vector2(btnW, btnH));
            var joinBtn = BuildV1V1Button(box.transform, V1V1 + "btn-vaophong.png", new Rect(0.023f, 0.184f, 0.954f, 0.653f), new Vector2(0.5f, by), new Vector2(btnW, btnH));
            var quickBtn = BuildV1V1Button(box.transform, V1V1 + "btn-ghepnhanh.png", new Rect(0.009f, 0.155f, 0.979f, 0.703f), new Vector2(0.795f, by), new Vector2(btnW, btnH));

            // Nút X đỏ (btn-close) góc phải trên — nằm trên chữ X in sẵn của panel.
            var closeBtn = BuildV1V1Button(box.transform, V1V1 + "btn-close.png", new Rect(0.061f, 0.069f, 0.876f, 0.888f), new Vector2(0.935f, 0.792f), new Vector2(58f, 58f));
            closeBtn.onClick.AddListener(() =>
            {
                RuntimeArt.PlayUiSwitchSound();
                var manager = MultiplayerManager.Instance;
                if (manager != null && manager.InSession)
                    _ = manager.LeaveAsync();
                Destroy(mapOverlay);
            });

            var buttons = new[] { quickBtn, createBtn, joinBtn };
            quickBtn.onClick.AddListener(() => { RuntimeArt.PlayUiSwitchSound(); QuickMatch(box, statusLabel, buttons); });
            createBtn.onClick.AddListener(() => { RuntimeArt.PlayUiSwitchSound(); CreateRoom(box, codeInput, statusLabel, buttons); });
            joinBtn.onClick.AddListener(() => { RuntimeArt.PlayUiSwitchSound(); JoinRoom(box, codeInput, statusLabel, buttons); });
        }

        // Nút ảnh 1vs1: sprite (đã baked chữ+icon) lấp khung, có phản hồi nhấn.
        Button BuildV1V1Button(Transform parent, string asset, Rect crop, Vector2 anchor, Vector2 size)
        {
            var btn = Ui.Button(parent, "", font, 1, () => { });
            var rt = btn.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            var img = btn.GetComponent<Image>();
            var spr = RuntimeArt.LoadV3SubSprite(asset, crop);
            if (spr != null) { img.sprite = spr; img.type = Image.Type.Simple; img.preserveAspect = true; img.color = Color.white; }
            AddPressScaleFeedback(btn.gameObject, 0.93f);
            return btn;
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

        // Hướng dẫn cũ (overlay gỗ 5 trang) đã bỏ. Thay bằng TUTORIAL TƯƠNG TÁC:
        // nút "Hướng dẫn" ở menu vào thẳng màn chơi thật (chế độ GameSession.IsTutorial),
        // đến mốc thì dừng + hiện thẻ hướng dẫn — xem BrickGameController.Tutorial.cs.
    }
}
