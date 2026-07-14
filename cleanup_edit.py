# -*- coding: utf-8 -*-
import io

p = r"U:\AI\Assets\BrickStacker\Scripts\BrickStackerGame.cs"
s = io.open(p, encoding='utf-8').read()

def method_span(s, sig):
    a = s.index(sig)
    line_start = s.rfind('\n', 0, a) + 1
    brace = s.index('{', a)
    depth = 0
    i = brace
    while True:
        c = s[i]
        if c == '{':
            depth += 1
        elif c == '}':
            depth -= 1
            if depth == 0:
                break
        i += 1
    end = i + 1
    while end < len(s) and s[end] in '\r\n':
        end += 1
    return line_start, end

def remove_method(s, sig):
    a, b = method_span(s, sig)
    return s[:a] + s[b:]

def replace_method(s, sig, new_text):
    a, b = method_span(s, sig)
    return s[:a] + new_text + s[b:]

def rep(s, old, new, count=1):
    assert old in s, "NOT FOUND: " + old[:80]
    return s.replace(old, new, count)

# --- fields ---
s = rep(s, """        readonly List<Button> chestButtons = new List<Button>();
        readonly List<Image> chestIcons = new List<Image>();
        readonly List<string> chestRewards = new List<string>();
""", "")
s = rep(s, """        int checkpointLevel;
        int pendingRewardCoins;
        int pendingChestPoints;
        int selectedChests;
        int allowedChestChoices;
""", "")
s = rep(s, "        readonly List<string> pendingMonumentPieceKeys = new List<string>();\n", "")

# --- Start() ---
s = rep(s, """            journeyLevel = Mathf.Max(1, GameSession.JourneyLevel);
            checkpointLevel = Mathf.Max(1, GameSession.JourneyCheckpoint);
            if (GameSession.ResumeFromCheckpoint)
            {
                journeyLevel = checkpointLevel;
                GameSession.ResumeFromCheckpoint = false;
            }
            GameSession.JourneyLevel = journeyLevel;""",
"""            journeyLevel = Mathf.Max(1, GameSession.JourneyLevel);
            GameSession.JourneyLevel = journeyLevel;""")
s = rep(s, """            pendingRewardCoins = 0;
            pendingChestPoints = 0;
            pendingMonumentPieceKeys.Clear();
""", "")
s = rep(s, "BeginLevelMission(!GameSession.IsClassicMode);", "BeginLevelMission(true);")

# --- SetupModeRules ---
s = replace_method(s, "void SetupModeRules()", """        void SetupModeRules()
        {
            journeyLevel = Mathf.Clamp(GameSession.SelectedLevel > 0 ? GameSession.SelectedLevel : journeyLevel, 1, TowerProgress.MaxLevels);
            rules = LevelRules.CreateJourney(journeyLevel);
        }

""")

# --- SetupTacticalBoard ---
s = rep(s, """            if (GameSession.IsClassicMode)
            {
                tacticalBoard = null;
                return;
            }

            if (rules.TacticalData == null)""", "            if (rules.TacticalData == null)")

# --- Update() mission checks ---
s = rep(s, """                if (tacticalBoard == null && !GameSession.IsClassicMode && rules.MissionKind == MissionKind.TimeAttack && gameplayTime > rules.TimeLimitSeconds && !IsMissionComplete())
                {
                    EndGame(false);
                    return;
                }
                if (tacticalBoard == null && !GameSession.IsClassicMode && rules.MissionKind == MissionKind.SurviveSeconds && IsMissionComplete())
                {
                    LevelComplete();
                    return;
                }

""", "")

# --- HUD texts ---
s = rep(s, 'sceneLevelText.text = GameSession.IsClassicMode ? "Chơi tự do" : "Màn: " + journeyLevel;', 'sceneLevelText.text = "Màn: " + journeyLevel;')
s = rep(s, 'linesText.text = GameSession.IsClassicMode ? "Chơi tự do" : "Level: " + journeyLevel;', 'linesText.text = "Level: " + journeyLevel;')

# --- chest button styling + popup chest grid ---
s = remove_method(s, "void StyleChestButton(Button button)")
s = rep(s, """            for (int i = 0; i < 15; i++)
            {
                int index = i;
                float col = i % 5;
                float row = i / 5;
                var button = Ui.Button(box.transform, "", font, 16, () => SelectChest(index));
                Ui.Rect(button.gameObject, new Vector2(0.18f + col * 0.16f, 0.600f - row * 0.118f), new Vector2(0.18f + col * 0.16f, 0.600f - row * 0.118f), new Vector2(152, 128));
                StyleChestButton(button);
                var icon = Ui.Panel(button.transform, "Chest Icon", Color.white).GetComponent<Image>();
                icon.sprite = RuntimeArt.CreateRewardChestSprite();
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                Ui.Rect(icon, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(138, 104));
                chestIcons.Add(icon);
                chestButtons.Add(button);
            }

""", "")
s = rep(s, 'continueButton = Ui.Button(box.transform, "Đi tiếp", font, 24, ContinueJourney);',
          'continueButton = Ui.Button(box.transform, "Bản đồ màn", font, 24, () => { });')
s = rep(s, 'stopButton = Ui.Button(box.transform, "Nhận thưởng", font, 23, StopAndClaim);',
          'stopButton = Ui.Button(box.transform, "Chơi lại", font, 23, () => { });')

# --- mission text methods ---
s = replace_method(s, "string MissionProgressText()", """        string MissionProgressText()
        {
            if (tacticalBoard == null)
                return "";
            return "Mục tiêu: dụ quái bắt đối thủ  |  Lượt " + tacticalBoard.MoveBank + "  Đã đi " + tacticalBoard.MovesUsed;
        }

""")
s = remove_method(s, "string ModeHeaderText()")
s = replace_method(s, "string MissionDescription()", """        string MissionDescription()
        {
            return "Thắng khi quái bắt được kẻ địch.";
        }

""")
s = replace_method(s, "bool IsMissionComplete()", """        bool IsMissionComplete()
        {
            return tacticalBoard != null && tacticalBoard.Status == TacticalBoardStatus.Won;
        }

""")
s = replace_method(s, "string BestScoreKey()", """        string BestScoreKey()
        {
            return TowerProgress.BestScoreKeyForLevel(journeyLevel);
        }

""")

# --- BeginLevelMission ---
s = rep(s, """            if (GameSession.IsClassicMode)
                rules = LevelRules.CreateClassic();
            else if (GameSession.IsDailyMode)
                rules = LevelRules.CreateDaily(DateTime.Now);
            else
                rules = LevelRules.CreateJourney(journeyLevel);""",
"            rules = LevelRules.CreateJourney(journeyLevel);")
s = rep(s, "            GameSession.JourneyCheckpoint = checkpointLevel;\n", "")
s = rep(s, 'missionTitleText.text = GameSession.IsDailyMode ? "THỬ THÁCH HÔM NAY" : "MÀN " + journeyLevel;',
          'missionTitleText.text = "MÀN " + journeyLevel;')

# --- LevelComplete ---
s = replace_method(s, "void LevelComplete()", """        void LevelComplete()
        {
            resolving = true;
            ClearActive();
            starsEarned = CalculateStars();
            TowerProgress.SaveFloorResult(journeyLevel, starsEarned);
            TowerProgress.SaveLevelBestScore(journeyLevel, score);
            TowerProgress.AddCoins(rules.CoinReward);
            PlayerPrefs.SetInt("BLOCKFALL_JOURNEY_LEVEL", TowerProgress.CurrentUnlockedLevel);
            PlayerPrefs.Save();

            levelClearTitleText.text = "HOÀN THÀNH MÀN " + journeyLevel + "\\n" + StarText(starsEarned);
            levelClearBodyText.color = new Color(1f, 0.91f, 0.74f);
            levelClearBodyText.fontStyle = FontStyle.Normal;
            levelClearBodyText.text = tacticalBoard != null && rules.TacticalData != null
                ? "Quái đã bắt được đối thủ\\nĐã dùng " + tacticalBoard.MovesUsed + " lượt\\n3 sao ≤ " + rules.TacticalData.ThreeStarMoveLimit + " lượt   2 sao ≤ " + rules.TacticalData.TwoStarMoveLimit + " lượt\\nXu +" + rules.CoinReward
                : "Nhiệm vụ hoàn thành\\nXu +" + rules.CoinReward + "\\nĐã mở khóa màn tiếp theo";
            levelClearOverlay.SetActive(true);

            continueButton.interactable = true;
            continueButton.GetComponentInChildren<Text>().text = "Bản đồ màn";
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(() =>
            {
                RuntimeArt.PlayUiSwitchSound();
                Time.timeScale = 1f;
                SceneManager.LoadScene("BrickLevel");
            });

            stopButton.interactable = true;
            stopButton.GetComponentInChildren<Text>().text = "Chơi lại";
            stopButton.onClick.RemoveAllListeners();
            stopButton.onClick.AddListener(() =>
            {
                RuntimeArt.PlayUiSwitchSound();
                Restart();
            });
            Beep(1180f, 0.22f, 0.35f);
        }

""")

# --- CalculateStars ---
s = replace_method(s, "int CalculateStars()", """        int CalculateStars()
        {
            if (tacticalBoard != null && rules.TacticalData != null)
            {
                if (tacticalBoard.MovesUsed <= rules.TacticalData.ThreeStarMoveLimit)
                    return 3;
                if (tacticalBoard.MovesUsed <= rules.TacticalData.TwoStarMoveLimit)
                    return 2;
            }
            return 1;
        }

""")

# --- star criteria helpers ---
s = remove_method(s, "string FormatStarCriteria(LevelRules r)")
s = remove_method(s, "string MissionDescriptionWithStars()")
s = replace_method(s, "void UpdateMissionStarRows()", """        void UpdateMissionStarRows()
        {
            if (missionStar1CondText == null) return;
            missionStar1CondText.text = "Hoàn thành";
            if (rules != null && rules.TacticalData != null)
            {
                missionStar2CondText.text = "≤ " + rules.TacticalData.TwoStarMoveLimit + " lượt";
                missionStar3CondText.text = "≤ " + rules.TacticalData.ThreeStarMoveLimit + " lượt";
                return;
            }
            missionStar2CondText.text = "—";
            missionStar3CondText.text = "—";
        }

""")

# --- chest / monument / journey reward methods ---
for sig in ["void PrepareRewardChests()", "List<string> BuildChestPool(int level)", "int ChestChoicesForLevel(int level)",
            "void SelectChest(int index)", "void SetChestClosed(int index)", "void SetChestOpened(int index, string labelText)",
            "string ApplyPendingReward(string reward)", "string AddPendingRandomMonumentPiece()",
            "int PendingPieceCount(string key)", "void ClaimPendingMonumentPieces()",
            "void ContinueJourney()", "void StopAndClaim()"]:
    s = remove_method(s, sig)

# --- EndGame ---
s = rep(s, """            statusText.text = "";
            if (!won)
            {
                if (GameSession.IsTowerMode)
                {
                    GameSession.JourneyLevel = Mathf.Max(1, checkpointLevel);
                    GameSession.JourneyCheckpoint = Mathf.Max(1, checkpointLevel);
                    GameSession.ResumeFromCheckpoint = true;
                    PlayerPrefs.SetInt("BLOCKFALL_JOURNEY_LEVEL", Mathf.Max(1, checkpointLevel));
                    PlayerPrefs.SetInt("BLOCKFALL_JOURNEY_CHECKPOINT", Mathf.Max(1, checkpointLevel));
                }
                pendingRewardCoins = 0;
                pendingChestPoints = 0;
                pendingMonumentPieceKeys.Clear();
                PlayerPrefs.Save();
            }
""", '            statusText.text = "";\n')

# --- Restart ---
s = rep(s, """            Time.timeScale = 1f;
            if (gameOver)
            {
                if (GameSession.IsTowerMode)
                {
                    GameSession.JourneyLevel = Mathf.Max(1, checkpointLevel);
                    GameSession.JourneyCheckpoint = Mathf.Max(1, checkpointLevel);
                    GameSession.ResumeFromCheckpoint = true;
                }
            }
            SceneManager.LoadScene("BrickGame");""",
"""            Time.timeScale = 1f;
            SceneManager.LoadScene("BrickGame");""")

# --- GameOverMessage ---
s = replace_method(s, "string GameOverMessage()", """        string GameOverMessage()
        {
            if (tacticalBoard != null && tacticalBoard.Status == TacticalBoardStatus.Failed)
                return "Quái đã bắt được bạn\\nLượt đã dùng  " + tacticalBoard.MovesUsed + "\\nXóa dòng để kiếm lượt và dụ quái tốt hơn";
            return "Điểm  " + score;
        }

""")

# --- RuntimeArt ---
for sig in ["public static Sprite CreateVietnamMapSprite()", "public static Sprite CreateHoangSaLabelSprite()",
            "public static Sprite CreateVietNamLabelSprite()", "public static Sprite CreateTruongSaLabelSprite()",
            "public static Sprite CreateRewardChestSprite()"]:
    s = remove_method(s, sig)
s = rep(s, "        static Sprite vietnamMapSprite;\n", "")
s = rep(s, "        static Sprite vietNamLabelSprite;\n", "")
s = rep(s, "        static Sprite hoangSaLabelSprite;\n", "")
s = rep(s, "        static Sprite truongSaLabelSprite;\n", "")
s = rep(s, """            displayFont = Resources.Load<Font>("BrickStacker/Moment Vintage");
            return displayFont;""", "            return displayFont;")
s = rep(s, """            uiFont = Resources.Load<Font>("BrickStacker/Moment Vintage");
            if (uiFont != null)
                return uiFont;

            return LoadDisplayFont();""", "            return LoadDisplayFont();")

io.open(p, 'w', encoding='utf-8', newline='\n').write(s)
print("all edits applied")
