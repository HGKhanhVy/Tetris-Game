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
            // Trạng thái Khiên đối thủ ngay dưới thanh máu (biết đối thủ đang thủ hay có sẵn khiên).
            oppShieldText = MakeBarText(tb, "", font, 19, TextAnchor.MiddleCenter, new Vector2(0.560f, 0.02f), new Vector2(0.722f, 0.34f));
            oppShieldText.color = new Color(0.62f, 0.86f, 1f);
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

            // 3 lá kỹ năng (THẢ RÁC / HÚT MÁU / CUỒNG NỘ) — icon + badge cost baked; tên dưới.
            string[] skillAssets = { "btn-chieu1-tharac.png", "btn-chieu2-hoimau.png", "btn-chieu3-cuongno.png" };
            Rect[] skillCrops = { new Rect(0.115f, 0.109f, 0.770f, 0.829f), new Rect(0.089f, 0.082f, 0.821f, 0.891f), new Rect(0.104f, 0.077f, 0.790f, 0.885f) };
            string[] skillNames = { "THẢ RÁC", "HÚT MÁU", "CUỒNG NỘ" };
            OnlineSkill[] map = { OnlineSkill.GarbageDrop, OnlineSkill.LifeDrain, OnlineSkill.OverloadBlast };
            float[] cx = { 0.243f, 0.50f, 0.757f };
            float half = 0.104f; // hẹp lại cho lá chiêu đỡ bè
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

            // GD §12: Đánh TỰ ĐỘNG (không nút). Chỉ còn 1 nút KHIÊN (btn-khienbv, đã baked chữ+icon)
            // để bấm bật Active Shield (§13). Số Shield Charge / trạng thái BẬT hiện ở phần phải nút.
            var def = MakeSpriteButton(cc, "Runtime Online Def", DIR + "btn-khienbv.png", new Rect(0.134f, 0.327f, 0.728f, 0.402f),
                new Vector2(0.5f, 0.5f), new Vector2(100, 40), () => TryActivateShield());
            var defImg = def.GetComponent<Image>(); if (defImg != null) defImg.preserveAspect = true;
            onlineDefRect = def.GetComponent<RectTransform>();
            Ui.Rect(def.gameObject, new Vector2(0.235f, 0.035f), new Vector2(0.765f, 0.225f), Vector2.zero);
            onlineDefText = MakeBarText(def.transform, "", RuntimeArt.LoadMenuButtonFont(), 46, TextAnchor.MiddleLeft, new Vector2(0.72f, 0.08f), new Vector2(1.10f, 0.92f));
            onlineDefText.horizontalOverflow = HorizontalWrapMode.Overflow;
            // Viền dày hơn để chữ "x0" khớp với chữ "Khiên" đã baked trên nút.
            var defOutline = onlineDefText.GetComponent<Outline>();
            if (defOutline != null)
            {
                defOutline.effectColor = new Color(0.16f, 0.065f, 0.02f, 1f);
                defOutline.effectDistance = new Vector2(1.7f, -1.7f);
            }
        }

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

            // --- Hai bàn cùng cỡ. Bàn hẹp/cao hơn (0.500) để ô 7×17 không bị bè (cao ~ bằng rộng). ---
            float boardTop = 0.825f, boardBottom = 0.145f;
            float boardH = boardTop - boardBottom;
            float boardW = boardH * 0.500f / Mathf.Max(1f, aspect);

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

        void SetEnergySegments(Image[] segs, int on, Color onColor)
        {
            if (segs == null) return;
            for (int i = 0; i < segs.Length; i++)
                if (segs[i] != null) segs[i].color = i < on ? onColor : energyOffColor;
        }

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

            // GD v3: nút Khiên (chữ baked) chỉ hiện số Shield Charge / trạng thái BẬT ở phần phải.
            if (onlineDefText != null)
                onlineDefText.text = healthSystem.IsShieldActive
                    ? "BẬT " + healthSystem.ActiveShieldHP
                    : "x" + healthSystem.ShieldCharges;

            // Khiên đối thủ (§13): đang bật → "KHIÊN {HP}" xanh sáng; else số charge; hết thì ẩn.
            if (oppShieldText != null)
            {
                if (MultiplayerMatch.OpponentActiveShieldHP > 0)
                {
                    oppShieldText.text = "KHIÊN " + MultiplayerMatch.OpponentActiveShieldHP;
                    oppShieldText.color = new Color(0.5f, 0.95f, 1f);
                }
                else if (MultiplayerMatch.OpponentShieldCharges > 0)
                {
                    oppShieldText.text = "Khiên x" + MultiplayerMatch.OpponentShieldCharges;
                    oppShieldText.color = new Color(0.62f, 0.82f, 1f, 0.9f);
                }
                else
                {
                    oppShieldText.text = "";
                }
            }

            if (oppNameText != null && !string.IsNullOrEmpty(MultiplayerMatch.OpponentName))
                oppNameText.text = MultiplayerMatch.OpponentName;
        }

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
            var panel = Ui.Panel(parent, "Runtime Opponent Mini Board", new Color(0.035f, 0.075f, 0.16f, 1f));
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

            skillAttackButton = BuildSkillButton(skillBar.transform, "RÁC", 0f, 0.32f, OnlineSkill.GarbageDrop);
            skillShieldButton = BuildSkillButton(skillBar.transform, "HÚT", 0.34f, 0.66f, OnlineSkill.LifeDrain);
            skillGarbageButton = BuildSkillButton(skillBar.transform, "NỘ", 0.68f, 1f, OnlineSkill.OverloadBlast);
            attackButton = skillGarbageButton; // giữ tham chiếu cũ cho code layout legacy
        }

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
                // Cụm v3: ghi ĐÚNG tài nguyên từng ô (Encode = resource+1) để bàn đối thủ hiện đúng
                // sprite; chế độ cũ giữ shape index+1. Thứ tự ô khớp activeResources như lúc khóa.
                bool clusters = rules != null && rules.UseResourceClusters && activeResources != null;
                byte fallback = (byte)(currentType >= 0 ? currentType + 1 : 8);
                int i = 0;
                foreach (var cell in Cells(origin, rotation))
                {
                    if (cell.x >= 0 && cell.x < Width && cell.y >= 0 && cell.y < Height)
                        boardSnapshot[cell.x + cell.y * Width] = clusters && i < activeResources.Length
                            ? (byte)Puzzle.GridBridge.Encode(activeResources[i])
                            : fallback;
                    i++;
                }
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

        void RepaintOpponentMiniBoard()
        {
            MultiplayerMatch.OpponentBoardDirty = false;
            var board = MultiplayerMatch.OpponentBoard;
            if (opponentMiniCells != null && board != null)
            {
                int cols = MultiplayerMatch.OpponentBoardCols;
                int rows = MultiplayerMatch.OpponentBoardRows;
                bool clusters = rules != null && rules.UseResourceClusters;
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        byte value = x < cols && y < rows ? board[x + y * cols] : (byte)0;
                        var cell = opponentMiniCells[x, y];
                        cell.enabled = true;
                        if (clusters && value >= 1 && value <= 4)
                        {
                            // Tài nguyên v3 (sprite giày/kiếm/khiên/sấm) lấp đầy ô.
                            cell.sprite = GetPieceBlockSprite(value - 1);
                            cell.preserveAspect = false;
                            cell.color = Color.white;
                        }
                        else if (clusters && value >= 5)
                        {
                            cell.sprite = blockSprite; cell.preserveAspect = false;
                            cell.color = new Color(0.40f, 0.43f, 0.50f, 1f); // rác xám
                        }
                        else if (value > 0)
                        {
                            cell.sprite = null;
                            cell.color = value - 1 < palette.Length ? palette[value - 1] : new Color(0.9f, 0.9f, 0.9f);
                        }
                        else
                        {
                            // Ô trống = tile navy nhạt → thấy lưới ô như bàn mình.
                            cell.sprite = null;
                            cell.color = new Color(0.10f, 0.17f, 0.31f, 1f);
                        }
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
                case OnlineSkill.LifeDrain:
                    // §15.2 P2P: gây damage lên đối thủ; hồi ĐÚNG lượng máu họ thực mất — chờ họ gửi
                    // DrainHeal xác nhận (xử ở ProcessIncomingAttacks) thay vì hồi ước lượng tức thì.
                    MultiplayerManager.Instance?.SendSkill(OnlineSkill.LifeDrain, (byte)OnlineConfig.LifeDrainDamage);
                    shake = 0.18f;
                    msg = "Hút máu đối thủ!";
                    break;
                case OnlineSkill.OverloadBlast:
                    // §15.3: 25 damage (phá Active Shield + 8 xuyên do đối thủ tự xử lý).
                    MultiplayerManager.Instance?.SendSkill(OnlineSkill.OverloadBlast, (byte)OnlineConfig.OverloadDamage);
                    shake = 0.22f;
                    msg = "Cuồng nộ!";
                    break;
                case OnlineSkill.GarbageDrop:
                default:
                    MultiplayerManager.Instance?.SendSkill(OnlineSkill.GarbageDrop, (byte)OnlineConfig.GarbageDropLines);
                    shake = 0.15f;
                    msg = "Thả " + OnlineConfig.GarbageDropLines + " hàng rác sang đối thủ!";
                    break;
            }
            FireAttackProjectile(skill); // đòn/skill bay sang bàn đối thủ
            RefreshSkillBar();
            SendMultiplayerState();
            if (tacticalBoard != null)
            {
                tacticalBoard.LastMessage = msg;
                RefreshTacticalBoardUi();
            }
        }

        // GD §13: bấm bật Active Shield (tiêu 1 Shield Charge → HP 16, 4 giây).
        void TryActivateShield()
        {
            if (!MultiplayerMatch.Active || gameOver)
                return;
            if (!healthSystem.ActivateShield())
            {
                if (tacticalBoard != null)
                {
                    tacticalBoard.LastMessage = healthSystem.IsShieldActive ? "Khiên đang bật." : "Chưa có lớp khiên.";
                    RefreshTacticalBoardUi();
                }
                return;
            }
            RuntimeArt.PlayUiSwitchSound();
            RefreshSkillBar();
            SendMultiplayerState();
        }

        void RefreshSkillBar()
        {
            bool live = MultiplayerMatch.Active && !gameOver;
            if (skillAttackButton != null)
                skillAttackButton.interactable = live && energySystem.CanAfford(OnlineSkill.GarbageDrop);
            if (skillShieldButton != null)
                skillShieldButton.interactable = live && energySystem.CanAfford(OnlineSkill.LifeDrain);
            if (skillGarbageButton != null)
                skillGarbageButton.interactable = live && energySystem.CanAfford(OnlineSkill.OverloadBlast);
            if (skillInfoText != null)
                skillInfoText.text = "NL " + energySystem.Energy + "/" + energySystem.Max
                    + "   Máu " + healthSystem.Health + "/" + healthSystem.Max
                    + (healthSystem.IsShieldActive ? " [Khiên " + healthSystem.ActiveShieldHP + "]" : (healthSystem.ShieldCharges > 0 ? " [x" + healthSystem.ShieldCharges + "]" : ""));

            // HUD online v3: 3 lá = GarbageDrop / LifeDrain / OverloadBlast.
            if (onlineSkillBtn[0] != null) onlineSkillBtn[0].interactable = live && energySystem.CanAfford(OnlineSkill.GarbageDrop);
            if (onlineSkillBtn[1] != null) onlineSkillBtn[1].interactable = live && energySystem.CanAfford(OnlineSkill.LifeDrain);
            if (onlineSkillBtn[2] != null) onlineSkillBtn[2].interactable = live && energySystem.CanAfford(OnlineSkill.OverloadBlast);
            UpdateOnlineHud();
        }

        void SendMultiplayerState()
        {
            if (!MultiplayerMatch.Active || MultiplayerManager.Instance == null)
                return;
            MultiplayerManager.Instance.SendState(score, lines, 0, healthSystem.Health, energySystem.Energy,
                healthSystem.ShieldCharges, healthSystem.IsShieldActive ? healthSystem.ActiveShieldHP : 0);
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

        // GD v3 GĐ4: chuyển cụm tài nguyên online thành hiệu ứng. Kiếm → đòn TỰ ĐỘNG gửi đối thủ
        // (Basic 8 / Strong 16), Khiên → Shield Charge, Sấm sét → Energy. Cộng dồn mọi cụm/chain.
        void ApplyOnlineClusterEffects(Puzzle.ResolutionOutcome outcome)
        {
            if (!MultiplayerMatch.Active || outcome == null)
                return;

            int attackDamage = 0;
            foreach (var step in outcome.Steps)
            {
                foreach (var cluster in step.Clusters)
                {
                    bool strong = cluster.Tier == Puzzle.ClusterTier.Strong;
                    switch (cluster.Resource)
                    {
                        case Puzzle.ResourceType.Attack:
                            attackDamage += OnlineConfig.AttackDamage(strong ? AttackTier.Strong : AttackTier.Basic);
                            break;
                        case Puzzle.ResourceType.Shield:
                            healthSystem.AddShieldCharge(strong);
                            break;
                        case Puzzle.ResourceType.Energy:
                            energySystem.GainFromCluster(strong);
                            break;
                    }
                }
            }

            if (attackDamage > 0)
            {
                MultiplayerManager.Instance?.SendSkill(OnlineSkill.Attack, (byte)Mathf.Clamp(attackDamage, 1, 255));
                shake = 0.15f;
                FireAttackProjectile(OnlineSkill.Attack); // đòn Kiếm bay sang bàn đối thủ
            }

            RefreshSkillBar();
            SendMultiplayerState();
        }

        // Một đòn tấn công đã lên lịch áp — giữ loại skill để xử lý xuyên khiên / hút máu (§12.5).
        struct PendingAttack
        {
            public OnlineSkill Skill;
            public int Damage;
            public float ApplyAt;
        }

        void ProcessIncomingAttacks()
        {
            // 1) Kéo đòn mới từ hàng đợi mạng → lịch áp RIÊNG từng đòn (§12.5): warning dài hơn nếu
            //    đòn mạnh, các đòn liên tiếp giãn tối thiểu MinAttackInterval để kịp phản ứng.
            var incoming = MultiplayerMatch.IncomingAttacks;
            while (incoming.Count > 0)
            {
                var atk = incoming.Dequeue();
                bool strong = atk.Amount >= OnlineConfig.AttackDamage(AttackTier.Strong);
                float warn = OnlineConfig.AttackWarning(strong ? AttackTier.Strong : AttackTier.Basic);
                float earliest = Mathf.Max(Time.unscaledTime + warn, lastAttackScheduledAt + OnlineConfig.MinAttackInterval);
                lastAttackScheduledAt = earliest;
                pendingAttacks.Enqueue(new PendingAttack { Skill = atk.Skill, Damage = Mathf.Max(1, atk.Amount), ApplyAt = earliest });
                shake = Mathf.Max(shake, 0.12f);
                PlayIncomingSkillSound(atk.Skill, strong); // âm cảnh báo RIÊNG theo loại đòn tới
            }
            MultiplayerMatch.AttackWarningActive = pendingAttacks.Count > 0;

            // Hồi máu Hút máu đã được đối thủ xác nhận đúng lượng (§15.2 P2P).
            if (MultiplayerMatch.PendingDrainHeal > 0)
            {
                int healed = healthSystem.Heal(MultiplayerMatch.PendingDrainHeal);
                MultiplayerMatch.PendingDrainHeal = 0;
                if (healed > 0)
                {
                    RefreshSkillBar();
                    SendMultiplayerState();
                    if (tacticalBoard != null)
                    {
                        tacticalBoard.LastMessage = "Hút được " + healed + " máu từ đối thủ!";
                        RefreshTacticalBoardUi();
                    }
                }
            }

            UpdateAttackFlash();

            // 2) Áp lần lượt các đòn tới hạn (hàng đợi đã theo thời gian tăng dần).
            while (pendingAttacks.Count > 0 && Time.unscaledTime >= pendingAttacks.Peek().ApplyAt)
            {
                ApplyIncomingAttack(pendingAttacks.Dequeue());
                if (gameOver)
                    return;
            }
        }

        // Áp một đòn: Cuồng nộ (§15.3) phá Active Shield rồi xuyên; Hút máu (§15.2) báo lại lượng
        // máu thực mất để bên gây hồi đúng; còn lại trừ máu qua Active Shield như thường.
        void ApplyIncomingAttack(PendingAttack atk)
        {
            bool shieldWasActive = healthSystem.IsShieldActive;
            int lost = atk.Skill == OnlineSkill.OverloadBlast
                ? healthSystem.TakeOverload(OnlineConfig.OverloadDamage, OnlineConfig.OverloadShieldPierce)
                : healthSystem.TakeDamage(atk.Damage);

            if (atk.Skill == OnlineSkill.LifeDrain && lost > 0)
                MultiplayerManager.Instance?.SendSkill(OnlineSkill.DrainHeal, (byte)Mathf.Clamp(lost, 1, 255));

            bool strong = atk.Skill == OnlineSkill.OverloadBlast
                || atk.Damage >= OnlineConfig.AttackDamage(AttackTier.Strong);
            bool blocked = shieldWasActive && lost < atk.Damage;
            bool hit = lost > 0;
            if (hit)
            {
                VibrateOnHit(strong);
                // Nháy viền ĐẬM đúng lúc trúng: đòn mạnh đậm & lâu hơn.
                impactFlashUntil = Time.unscaledTime + (strong ? 0.42f : 0.26f);
                impactFlashStrong = strong;
            }
            RefreshSkillBar();
            SendMultiplayerState();
            if (tacticalBoard != null)
            {
                tacticalBoard.LastMessage = blocked && !hit ? "Khiên đã chặn đòn tấn công!"
                    : "Trúng đòn! Máu còn " + healthSystem.Health + "/" + healthSystem.Max;
                RefreshTacticalBoardUi();
            }
            shake = strong ? 0.35f : 0.18f;

            if (healthSystem.IsDead)
            {
                if (MultiplayerMatch.OpponentHealth <= 0)
                    MultiplayerEndMatch(ResolveByLines(), "Cả hai cùng hết máu!");
                else
                    MultiplayerEndMatch(-1, "Bạn đã hết máu!");
            }
        }

        // Rung khi trúng đòn, phân biệt rõ nặng/nhẹ (chỉ thiết bị thật, bỏ qua Editor).
        // Đòn mạnh: rung dài + biên độ cao + 2 nhịp; đòn nhẹ: một nhịp ngắn.
        static void VibrateOnHit(bool strong)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                {
                    if (vibrator == null || !vibrator.Call<bool>("hasVibrator"))
                    {
                        Handheld.Vibrate();
                        return;
                    }
                    int sdk = new AndroidJavaClass("android.os.Build$VERSION").GetStatic<int>("SDK_INT");
                    if (sdk >= 26)
                    {
                        using (var effectClass = new AndroidJavaClass("android.os.VibrationEffect"))
                        {
                            AndroidJavaObject effect = strong
                                // đòn mạnh: rung dài & liên hồi, biên độ tối đa (dồn dập, đô rõ)
                                ? effectClass.CallStatic<AndroidJavaObject>("createWaveform",
                                    new long[] { 0, 380, 90, 380, 90, 260 }, new int[] { 0, 255, 0, 255, 0, 255 }, -1)
                                : effectClass.CallStatic<AndroidJavaObject>("createOneShot", 300L, 255);
                            vibrator.Call("vibrate", effect);
                        }
                    }
                    else if (strong)
                        vibrator.Call("vibrate", new long[] { 0, 380, 90, 380, 90, 260 }, -1);
                    else
                        vibrator.Call("vibrate", 300L);
                }
            }
            catch
            {
                Handheld.Vibrate();
            }
#elif UNITY_IOS && !UNITY_EDITOR
            Handheld.Vibrate();
            if (strong)
                Handheld.Vibrate(); // iOS chỉ có một kiểu rung — đòn mạnh rung 2 lần cho nặng hơn
#endif
        }

        // Quầng đỏ LAN TỎA từ mép màn vào (vignette mềm, không viền vuông) khi có đòn chờ và
        // đậm hẳn lúc trúng (GD §12.3). Đòn càng mạnh quầng càng đỏ đậm + phập phồng mạnh.
        void UpdateAttackFlash()
        {
            UpdateIncomingWarnBanner(); // banner tên đòn/skill tới (luôn cập nhật kể cả khi hết flash)
            EnsureAttackFlashOverlay();
            if (attackFlashOverlay == null)
                return;

            float alpha = 0f;
            bool strongState = false;
            // Cảnh báo đòn đang chờ: quầng thở nhẹ, đòn mạnh đậm hơn, gần trúng đậm hơn.
            if (pendingAttacks.Count > 0)
            {
                var next = pendingAttacks.Peek();
                float remain = Mathf.Max(0f, next.ApplyAt - Time.unscaledTime);
                bool strong = next.Damage >= OnlineConfig.AttackDamage(AttackTier.Strong);
                strongState |= strong;
                float baseA = strong ? 0.55f : 0.30f;
                float pulse = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * (strong ? 20f : 14f));
                alpha = baseA * pulse * (remain < 1.2f ? 1f : 0.8f);
            }
            // Bùng đậm đúng lúc trúng (mờ dần), đè lên cảnh báo.
            bool impacting = Time.unscaledTime < impactFlashUntil;
            if (impacting)
            {
                strongState |= impactFlashStrong;
                float span = impactFlashStrong ? 0.42f : 0.26f;
                float k = Mathf.Clamp01((impactFlashUntil - Time.unscaledTime) / span);
                alpha = Mathf.Max(alpha, (impactFlashStrong ? 1f : 0.72f) * k);
            }

            bool on = alpha > 0.01f;
            attackFlashOverlay.enabled = on;
            if (!on)
                return;

            attackFlashOverlay.color = new Color(strongState ? 0.82f : 0.95f, 0.10f, 0.08f, Mathf.Clamp01(alpha));
            // "Bay bay": quầng phập phồng nhẹ về kích thước cho cảm giác lan tỏa, mạnh hơn khi đòn mạnh.
            float breathe = 1f + (strongState ? 0.06f : 0.035f) * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 9f));
            attackFlashOverlay.rectTransform.localScale = new Vector3(breathe, breathe, 1f);
        }

        void EnsureAttackFlashOverlay()
        {
            if (attackFlashOverlay != null)
                return;
            Transform parent = sceneGameplayCanvas != null ? sceneGameplayCanvas.transform
                : (safeAreaRoot != null ? (Transform)safeAreaRoot : null);
            if (parent == null)
                return;

            // Quầng vignette full-screen, tràn ra ngoài mép một chút để mép ngoài không bị cắt cứng.
            var go = Ui.Panel(parent, "Runtime Attack Flash", new Color(0.95f, 0.10f, 0.08f, 0f));
            var img = go.GetComponent<Image>();
            Ui.Rect(go, new Vector2(-0.08f, -0.08f), new Vector2(1.08f, 1.08f), Vector2.zero);
            img.sprite = VignetteSprite();
            img.type = Image.Type.Simple;
            img.raycastTarget = false;
            img.enabled = false;
            go.transform.SetAsLastSibling();
            attackFlashOverlay = img;
        }

        // Banner nêu RÕ đòn/skill đang tới để người chơi biết đường bấm Khiên (§12.4).
        void UpdateIncomingWarnBanner()
        {
            if (pendingAttacks.Count == 0)
            {
                if (incomingWarnPanel != null && incomingWarnPanel.gameObject.activeSelf)
                    incomingWarnPanel.gameObject.SetActive(false);
                return;
            }
            EnsureIncomingWarnText();
            if (incomingWarnText == null || incomingWarnPanel == null)
                return;

            var next = pendingAttacks.Peek();
            bool strong = next.Damage >= OnlineConfig.AttackDamage(AttackTier.Strong);
            string label;
            Color baseCol;
            switch (next.Skill)
            {
                case OnlineSkill.OverloadBlast:
                    label = "CUỒNG NỘ — XUYÊN KHIÊN!";
                    baseCol = new Color(1f, 0.45f, 1f);
                    break;
                case OnlineSkill.LifeDrain:
                    label = "ĐỐI THỦ HÚT MÁU!";
                    baseCol = new Color(0.6f, 1f, 0.5f);
                    break;
                default:
                    label = strong ? "ĐÒN MẠNH TỚI!" : "ĐÒN TẤN CÔNG!";
                    baseCol = new Color(1f, 0.62f, 0.28f);
                    break;
            }
            incomingWarnText.text = label + "  BẤM KHIÊN!";
            incomingWarnText.color = baseCol;
            // Phập phồng to/nhỏ + nền đỏ đậm nhạt để đập vào mắt.
            float pulse = 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 8f));
            float s = 1f + 0.06f * pulse;
            incomingWarnPanel.localScale = new Vector3(s, s, 1f);
            var bg = incomingWarnPanel.GetComponent<Image>();
            if (bg != null)
                bg.color = new Color(0.16f, 0.02f, 0.02f, 0.70f + 0.22f * pulse);
            if (!incomingWarnPanel.gameObject.activeSelf)
                incomingWarnPanel.gameObject.SetActive(true);
        }

        void EnsureIncomingWarnText()
        {
            if (incomingWarnText != null)
                return;
            Transform parent = safeAreaRoot != null ? (Transform)safeAreaRoot
                : (sceneGameplayCanvas != null ? sceneGameplayCanvas.transform : transform);
            if (parent == null)
                return;

            // Dải nền tối bo tròn để chữ nổi hẳn trên nền bàn.
            var panel = Ui.Panel(parent, "Runtime Incoming Warn", new Color(0.16f, 0.02f, 0.02f, 0.78f));
            var panelImg = panel.GetComponent<Image>();
            panelImg.sprite = PillSprite();
            panelImg.type = Image.Type.Sliced;
            panelImg.raycastTarget = false;
            Ui.Rect(panel, new Vector2(0.085f, 0.700f), new Vector2(0.915f, 0.792f), Vector2.zero);
            incomingWarnPanel = panel.GetComponent<RectTransform>();
            panel.transform.SetAsLastSibling();

            incomingWarnText = Ui.Text(panel.transform, "", RuntimeArt.LoadMenuButtonFont(), 50, new Color(1f, 0.62f, 0.28f), TextAnchor.MiddleCenter);
            incomingWarnText.fontStyle = FontStyle.Bold;
            incomingWarnText.raycastTarget = false;
            incomingWarnText.horizontalOverflow = HorizontalWrapMode.Overflow;
            incomingWarnText.verticalOverflow = VerticalWrapMode.Overflow;
            Ui.Rect(incomingWarnText, Vector2.zero, Vector2.one, Vector2.zero);
            AddDarkWoodTextEdge(incomingWarnText, 1.7f, 1f);
            panel.SetActive(false);
        }

        // Âm cảnh báo riêng theo loại đòn tới: Cuồng nộ trầm/đe dọa, Hút máu vừa, đòn thường/mạnh cao dần.
        void PlayIncomingSkillSound(OnlineSkill skill, bool strong)
        {
            switch (skill)
            {
                case OnlineSkill.OverloadBlast:
                    Beep(140f, 0.20f, 0.32f);
                    break;
                case OnlineSkill.LifeDrain:
                    Beep(300f, 0.14f, 0.26f);
                    break;
                default:
                    Beep(strong ? 200f : 260f, 0.14f, 0.26f);
                    break;
            }
        }

        // Đòn bay từ bàn mình sang bàn đối thủ khi mình tấn công/dùng skill (phản hồi "đã đánh trúng").
        void FireAttackProjectile(OnlineSkill skill)
        {
            if (scenePuzzleBoardAnchorRect == null || opponentMiniPanelRect == null)
                return;
            if (attackProjectileRoutine != null)
                StopCoroutine(attackProjectileRoutine);
            attackProjectileRoutine = StartCoroutine(FlyAttackProjectile(skill));
        }

        System.Collections.IEnumerator FlyAttackProjectile(OnlineSkill skill)
        {
            EnsureAttackProjectile();
            if (attackProjectile == null)
                yield break;

            Color color = skill == OnlineSkill.OverloadBlast ? new Color(1f, 0.4f, 0.95f)
                : skill == OnlineSkill.LifeDrain ? new Color(0.55f, 1f, 0.45f)
                : skill == OnlineSkill.GarbageDrop ? new Color(0.7f, 0.72f, 0.8f)
                : new Color(1f, 0.7f, 0.3f);

            var rt = attackProjectile.rectTransform;
            Vector3 from = scenePuzzleBoardAnchorRect.position;
            Vector3 to = opponentMiniPanelRect.position;
            attackProjectile.gameObject.SetActive(true);
            attackProjectile.color = color;

            const float dur = 0.42f;
            for (float t = 0f; t < dur; t += Time.deltaTime)
            {
                float k = Mathf.Clamp01(t / dur);
                rt.position = Vector3.Lerp(from, to, k);
                float s = Mathf.Lerp(1.1f, 0.5f, k);
                rt.localScale = new Vector3(s, s, 1f);
                var c = color; c.a = 1f - 0.3f * k; attackProjectile.color = c;
                yield return null;
            }
            attackProjectile.gameObject.SetActive(false);
            attackProjectileRoutine = null;
        }

        void EnsureAttackProjectile()
        {
            if (attackProjectile != null)
                return;
            Transform parent = sceneGameplayCanvas != null ? sceneGameplayCanvas.transform
                : (safeAreaRoot != null ? (Transform)safeAreaRoot : null);
            if (parent == null)
                return;
            var go = Ui.Panel(parent, "Runtime Attack Projectile", Color.white);
            var img = go.GetComponent<Image>();
            img.sprite = GetPieceBlockSprite((int)Puzzle.ResourceType.Attack);
            img.preserveAspect = true;
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.sizeDelta = new Vector2(70, 70);
            go.transform.SetAsLastSibling();
            go.SetActive(false);
            attackProjectile = img;
        }

        // Popup kết quả online chỉ cần 1 nút TRANG CHỦ (Chơi lại/Tiếp không hợp 1v1 P2P): ẩn 2 nút kia,
        // căn giữa nút Trang chủ và đổi hành động sang rời phòng + về menu chính.
        void WireOnlineHomeOnly(Button home, Button hide1, Button hide2, System.Action onHome)
        {
            if (hide1 != null) hide1.gameObject.SetActive(false);
            if (hide2 != null) hide2.gameObject.SetActive(false);
            if (home == null)
                return;
            home.gameObject.SetActive(true);
            var rt = home.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, rt.anchorMin.y);
            rt.anchorMax = new Vector2(0.5f, rt.anchorMax.y);
            rt.anchoredPosition = new Vector2(0f, rt.anchoredPosition.y);
            home.interactable = true;
            home.onClick.RemoveAllListeners();
            home.onClick.AddListener(() => onHome());
        }

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
            if (incomingWarnPanel != null) incomingWarnPanel.gameObject.SetActive(false);
            if (attackProjectile != null) attackProjectile.gameObject.SetActive(false);

            var manager = MultiplayerManager.Instance;
            if (manager != null)
                manager.SendState(score, lines, outcome > 0 ? MultiplayerManager.FlagFinished : MultiplayerManager.FlagLost,
                    healthSystem.Health, energySystem.Energy);

            // Popup thắng/thua ONLINE dùng CHUNG giao diện với offline (CHIẾN THẮNG / THẤT BẠI).
            string opponentLabel = string.IsNullOrEmpty(MultiplayerMatch.OpponentName) ? "Đối thủ" : MultiplayerMatch.OpponentName;
            string body = reason + "\nBạn " + score + " điểm · " + lines + " hàng"
                + "\n" + opponentLabel + " " + MultiplayerMatch.OpponentScore + " điểm · " + MultiplayerMatch.OpponentLines + " hàng";

            System.Action goHome = () =>
            {
                RuntimeArt.PlayUiSwitchSound();
                Time.timeScale = 1f;
                var m = MultiplayerManager.Instance;
                if (m != null && m.InSession)
                    _ = m.LeaveAsync();
                SceneManager.LoadScene("BrickMenu");
            };

            if (outcome > 0)
            {
                SetLevelClearStars(3); // thắng: 3 sao ăn mừng
                if (levelClearBodyText != null) levelClearBodyText.text = body;
                WireOnlineHomeOnly(continueButton, stopButton, nextButton, goHome);
                if (levelClearOverlay != null) { levelClearOverlay.transform.SetAsLastSibling(); levelClearOverlay.SetActive(true); }
                shake = 0.35f;
                Beep(1180f, 0.22f, 0.35f);
            }
            else
            {
                if (gameLoseSubtitle != null) gameLoseSubtitle.text = (outcome == 0 ? "HÒA!\n" : "") + body;
                WireOnlineHomeOnly(loseHomeButton, loseRetryButton, loseNextButton, goHome);
                if (gameLoseOverlay != null) { gameLoseOverlay.transform.SetAsLastSibling(); gameLoseOverlay.SetActive(true); }
                shake = 0.2f;
                RuntimeArt.PlayGameOverSound();
            }

            // Chờ chút cho cờ kết thúc kịp đến đối thủ rồi mới rời phòng.
            StartCoroutine(LeaveMatchAfterDelay(1.5f));
        }
    }
}
