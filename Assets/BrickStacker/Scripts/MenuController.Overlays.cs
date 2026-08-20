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
    }
}
