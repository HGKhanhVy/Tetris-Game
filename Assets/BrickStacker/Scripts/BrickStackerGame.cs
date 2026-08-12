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

    public partial class BrickGameController : MonoBehaviour
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

        readonly List<Image> tacticalCellImageCache = new List<Image>();
        int monsterNextCellIndex = -1;
        Sprite obstacleBlueSprite, obstacleBushSprite;

        // Sprite chướng ngại vật (chuongngaivat) — xen kẽ khối xanh / bụi xanh lá như thiết kế.

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

        static string SkillName(OnlineSkill s) => s == OnlineSkill.Attack ? "Đánh" : s == OnlineSkill.Shield ? "Khiên" : "Rác";

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
