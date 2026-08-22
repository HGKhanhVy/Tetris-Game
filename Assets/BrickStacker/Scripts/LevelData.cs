using System;
using UnityEngine;

namespace BrickStacker
{
    public static class GameSession
    {
        public static int SelectedLevel = 1;
        public static int JourneyLevel = 1;
    }

    public static class LevelProgress
    {
        public const int MaxLevels = 20;
        // Giá trị chuỗi key giữ nguyên tên "TOWER" cũ để không mất save hiện có.
        public const string UnlockedLevelKey = "BLOCKFALL_TOWER_UNLOCKED_FLOOR";
        public const string CoinsKey = "BLOCKFALL_COINS";
        public const string TotalLinesClearedKey = "BLOCKFALL_TOTAL_LINES_CLEARED";

        // Mở hết mọi màn (kể cả bản build) — bỏ khóa theo tiến trình để người chơi tự do chọn màn.
        public static int CurrentUnlockedLevel => MaxLevels;

        public static string StarKey(int level)
        {
            return "BLOCKFALL_TOWER_FLOOR_" + Mathf.Clamp(level, 1, MaxLevels) + "_STARS";
        }

        public static string BestScoreKeyForLevel(int level)
        {
            return "BLOCKFALL_TOWER_LEVEL_" + Mathf.Clamp(level, 1, MaxLevels) + "_BEST_SCORE";
        }

        public static int Coins => PlayerPrefs.GetInt(CoinsKey, 0);

        public static void AddCoins(int amount)
        {
            PlayerPrefs.SetInt(CoinsKey, Mathf.Max(0, Coins + amount));
        }

        public static int StarsForLevel(int level)
        {
            return Mathf.Clamp(PlayerPrefs.GetInt(StarKey(level), 0), 0, 3);
        }

        public static void SaveLevelResult(int level, int stars)
        {
            level = Mathf.Clamp(level, 1, MaxLevels);
            stars = Mathf.Clamp(stars, 1, 3);
            if (stars > StarsForLevel(level))
                PlayerPrefs.SetInt(StarKey(level), stars);

            PlayerPrefs.SetInt(UnlockedLevelKey, Mathf.Max(CurrentUnlockedLevel, Mathf.Min(MaxLevels, level + 1)));
            PlayerPrefs.Save();
        }

        public static void SaveLevelBestScore(int level, int score)
        {
            string key = BestScoreKeyForLevel(level);
            if (score > PlayerPrefs.GetInt(key, 0))
                PlayerPrefs.SetInt(key, score);
        }
    }

    [Serializable]
    public class LevelRules
    {
        public Color BackgroundB;
        public float FallInterval;
        public float SpeedRampSeconds;
        public float MaxFallSpeedMultiplier = 1f;
        public int GarbageEveryPieces;
        public float SurpriseGarbageChance;
        public int ScoreMultiplier;
        public int ForcedPieceType = -1;
        public bool AllowSpecialBlocks = true;
        // GD v3: bật cơ chế ghép cụm tài nguyên thay cho xóa hàng ngang. Mặc định tắt để
        // giữ nguyên game hiện tại cho tới khi cơ chế mới được playtest.
        public bool UseResourceClusters = false;
        public bool GhostPreview = true;
        public bool FastBlocks;
        public bool HasStoneBlocks;
        public bool HasFixedObstacles;
        public int RotationLimit;
        public int RisingDangerSeconds;
        public int CoinReward = 50;
        public TacticalLevelData TacticalData;

        public static LevelRules CreateJourney(int level)
        {
            int stage = Mathf.Max(1, level);
            var rules = new LevelRules
            {
                BackgroundB = new Color(0.08f, 0.22f, 0.18f),
                FallInterval = Mathf.Max(0.34f, 0.80f - Mathf.Min(stage - 1, 30) * 0.010f),
                // Every level ramps up gently over time; pattern levels may override
                // with a stronger ramp in ApplyLevelConfig.
                SpeedRampSeconds = 150f,
                MaxFallSpeedMultiplier = 1.6f,
                GarbageEveryPieces = 0,
                SurpriseGarbageChance = 0f,
                ScoreMultiplier = 1,
                AllowSpecialBlocks = true,
                // Shown only for the first GhostPreviewPieces drops of each game.
                GhostPreview = true,
                FastBlocks = stage >= 15,
                RotationLimit = 0,
                RisingDangerSeconds = 0,
                CoinReward = 45 + stage * 5
            };

            ApplyLevelConfig(rules, stage);
            rules.TacticalData = TacticalLevelData.Create(stage);
            rules.FallInterval = rules.TacticalData.InitialFallSpeed;
            rules.CoinReward = rules.TacticalData.CoinReward;
            // GD v3: bật ghép cụm tài nguyên cho MỌI level (offline + online 1v1) — cơ chế cụm v3
            // đã thay hẳn xóa-hàng-ngang cũ.
            rules.UseResourceClusters = true;
            return rules;
        }

        static void ApplyLevelConfig(LevelRules rules, int level)
        {
            int pattern = (level - 1) % 10;
            if (pattern == 4)
                rules.GhostPreview = false;
            else if (pattern == 5)
            {
                rules.SpeedRampSeconds = Mathf.Max(80f, 180f - level * 3f);
                rules.MaxFallSpeedMultiplier = Mathf.Min(2.8f, 1.35f + level * 0.04f);
            }
            else if (pattern == 6)
                rules.SurpriseGarbageChance = Mathf.Min(0.20f, 0.08f + level * 0.004f);
            else if (pattern == 8)
            {
                rules.SpeedRampSeconds = 150f;
                rules.MaxFallSpeedMultiplier = Mathf.Min(2.7f, 1.45f + level * 0.035f);
            }
            else if (pattern == 9)
                rules.HasStoneBlocks = true;

            if (level >= 12 && level % 4 == 0)
                rules.RotationLimit = Mathf.Max(10, 24 - level / 2);

            if (level >= 18 && level % 6 == 0)
                rules.RisingDangerSeconds = Mathf.Clamp(34 - level / 2, 16, 34);

            if (level >= 24 && level % 8 == 0)
                rules.HasFixedObstacles = true;
        }
    }
}
