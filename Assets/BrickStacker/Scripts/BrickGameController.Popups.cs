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

            // Phụ đề "Sẵn sàng chưa?" — dùng font chính (đủ dấu tiếng Việt); titleFont trang trí thiếu glyph.
            missionDescText = Ui.Text(box.transform, "Sẵn sàng chưa?", font, 56, new Color(1f, 0.99f, 0.95f), TextAnchor.MiddleCenter);
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

        void BuildGameLosePopup(Transform parent)
        {
            const string DIR = "screen-thatbai/";

            // Đốm mờ trôi nhẹ (buồn) cho popup đỡ trống — vẽ TRƯỚC để nằm sau mọi thứ.
            BuildLoseAmbiance(parent);

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
            loseHomeButton = MakeSpriteButton(parent, "Btn Home", DB + "btn-home.png", new Rect(0.150f, 0.275f, 0.697f, 0.479f),
                new Vector2(0.388f, 0.118f), new Vector2(btnH * 0.97f, btnH), () =>
                {
                    RuntimeArt.PlayUiSwitchSound();
                    Time.timeScale = 1f;
                    SceneManager.LoadScene("BrickLevel");
                });
            loseRetryButton = MakeSpriteButton(parent, "Btn Retry", DB + "btn-retry.png", new Rect(0.271f, 0.168f, 0.458f, 0.664f),
                new Vector2(0.446f, 0.118f), new Vector2(btnH * 1.04f, btnH), () =>
                {
                    RuntimeArt.PlayUiSwitchSound();
                    Restart();
                });
            loseNextButton = MakeSpriteButton(parent, "Btn Next", DB + "btn-next.png", new Rect(0.077f, 0.287f, 0.841f, 0.434f),
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

        // Đốm sáng mờ xanh lam trôi/lấp lánh êm dịu quanh rìa popup THẤT BẠI (không né vùng giữa nhiều
        // vì hero gục nằm giữa; đặt quanh mép). Dùng chung ConfettiTwinkle nhưng êm hơn.
        void BuildLoseAmbiance(Transform parent)
        {
            Vector2[] pos =
            {
                new Vector2(0.08f, 0.86f), new Vector2(0.16f, 0.66f), new Vector2(0.10f, 0.44f),
                new Vector2(0.14f, 0.24f), new Vector2(0.30f, 0.80f), new Vector2(0.24f, 0.50f),
                new Vector2(0.90f, 0.84f), new Vector2(0.84f, 0.62f), new Vector2(0.92f, 0.40f),
                new Vector2(0.78f, 0.26f), new Vector2(0.70f, 0.82f), new Vector2(0.88f, 0.20f),
                new Vector2(0.05f, 0.66f), new Vector2(0.95f, 0.70f), new Vector2(0.20f, 0.90f), new Vector2(0.80f, 0.90f),
            };
            var rng = new System.Random(770231);
            var tint = new Color(0.5f, 0.62f, 0.9f);
            foreach (var p in pos)
            {
                var img = Ui.Panel(parent, "Lose Ember", tint).GetComponent<Image>();
                img.sprite = RoundUiSprite();
                img.raycastTarget = false;
                var rt = img.rectTransform;
                rt.anchorMin = rt.anchorMax = p;
                rt.pivot = new Vector2(0.5f, 0.5f);
                float s = 8f + (float)rng.NextDouble() * 14f;
                rt.sizeDelta = new Vector2(s, s);
                img.gameObject.AddComponent<ConfettiTwinkle>().Init(0.5f, rng, 0.5f, 1.7f);
            }
        }

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
                float h = 24f + (float)rng.NextDouble() * 30f;
                rt.sizeDelta = new Vector2(h * aspects[i], h);
                rt.localEulerAngles = new Vector3(0f, 0f, (float)rng.NextDouble() * 360f);
                img.raycastTarget = false;
                var c = img.color; c.a = 0.6f; img.color = c;
                // Tự lấp lánh + đung đưa + xoay khi popup CHIẾN THẮNG hiện.
                img.gameObject.AddComponent<ConfettiTwinkle>().Init(0.75f, rng);
            }
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
            RenderPiecePreview(nextPreviewCells, nextType, true, nextResources, nextBlank);
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
            if (rules.UseResourceClusters)
            {
                resourceBag = null; // tạo lại túi theo mode Offline/Online của ván này
                activeResources = null;
                nextResources = null;
                EnsureResourcePuzzle();
            }
            RefreshTacticalBoardUi();

            // 1v1: khởi tạo máu + năng lượng cho trận mới (design §6-8).
            if (MultiplayerMatch.Active)
            {
                energySystem.Reset();
                healthSystem.Reset();
                nextAttackTime = 0f;
                pendingAttacks.Clear();
                lastAttackScheduledAt = 0f;
                impactFlashUntil = 0f;
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

        static string RandomVictorySubtitle(int stars)
        {
            var pool = stars >= 3 ? Subtitle3Star : stars == 2 ? Subtitle2Star : Subtitle1Star;
            return pool[UnityEngine.Random.Range(0, pool.Length)];
        }

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

        static int WinScoreBonus(int stars)
        {
            return 300 + Mathf.Clamp(stars, 0, 3) * 200;
        }

        int CalculateStars()
        {
            // Sao theo SỐ BƯỚC di chuyển (hiệu quả) — thời gian đã là điều kiện THUA riêng.
            // Ít bước hơn = nhiều sao hơn: 3 sao ≤ ThreeStarMoveLimit, 2 sao ≤ TwoStarMoveLimit.
            if (tacticalBoard != null && rules.TacticalData != null)
            {
                int moves = tacticalBoard.MovesUsed;
                var d = rules.TacticalData;
                if (moves <= d.ThreeStarMoveLimit)
                    return 3;
                if (moves <= d.TwoStarMoveLimit)
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
                // Sao theo SỐ BƯỚC di chuyển (ít bước hơn = nhiều sao hơn).
                missionStar2CondText.text = "≤ " + rules.TacticalData.TwoStarMoveLimit + " lượt";
                missionStar3CondText.text = "≤ " + rules.TacticalData.ThreeStarMoveLimit + " lượt";
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

            // BXH: gửi điểm NGAY CẢ KHI THUA (offline) để bảng có dữ liệu (cúp = điểm cao nhất tuần).
            if (!MultiplayerMatch.Active && score > 0)
            {
                LevelProgress.SaveLevelBestScore(journeyLevel, score);
                LeaderboardsSync.SubmitScore(score);
            }

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
    }
}
